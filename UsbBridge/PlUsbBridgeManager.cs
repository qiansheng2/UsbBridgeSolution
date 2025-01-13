using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Devices;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Threading;
using Isc.Yft.UsbBridge.Utils;
using System.Linq;
using System.Text;
using Isc.Yft.UsbBridge.Exceptions;
using Isc.Yft.UsbBridge.Handler;
using Newtonsoft.Json;

namespace Isc.Yft.UsbBridge
{
    internal class PlUsbBridgeManager:IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // 用于记录当前USB工作模式
        public USBMode CurrentMode { get; set; } = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);

        private bool _disposed = false;

        // 三个后台任务引用
        private Task _receiverTask;
        private Task _monitorTask;

        private DataMonitor _dataMonitor;       // 监控线程
        private DataReceiver _dataReceiver;     // 读取线程

        private SendRequest _sendRequest;       // 发送数据包数组+ACK确认包数组

        // One线程执行控制信号
        internal static SemaphoreSlim _oneThreadAtATime = new SemaphoreSlim(1, 1);

        // 监控和接收数据任务取消信号
        private CancellationTokenSource _backend_cts;
        // Ack包等待取消信号
        private CancellationTokenSource _waitAckToken;

        // 订阅核心错误发生状况，用于往更上一层报告错误信息
        public event EventHandler<InvalidHardwareException> FatalErrorOccurred;

        // 具体的对拷线控制实例 (PL25A1,PL27A1等)
        private readonly ICopyline _usbCopyline;

        // 数据包处理器
        private CommandPacketHandler _commandPacketHandler;
        private CommandAckPacketHandler _commandAckPacketHandler;
        private DataAckPacketHandler _dataAckPacketHandler;

        public PlUsbBridgeManager(USBMode mode)
        {
            // 这里决定用哪个芯片控制类
            // _usbCopyline = new Pl25A1UsbCopyline();
            _usbCopyline = new Pl27A7UsbCopyline();

            // 模式设置
            CurrentMode = mode;

            // 初始化包处理器，注册处理器
            _commandPacketHandler = new CommandPacketHandler(this);
            PacketHandlerFactory.RegisterHandler(EPacketType.CMD, _commandPacketHandler);
            _commandAckPacketHandler = new CommandAckPacketHandler();
            PacketHandlerFactory.RegisterHandler(EPacketType.CMD_ACK, _commandAckPacketHandler);
            _dataAckPacketHandler = new DataAckPacketHandler();
            PacketHandlerFactory.RegisterHandler(EPacketType.DATA_ACK, _dataAckPacketHandler);
        }

        // 析构函数
        ~PlUsbBridgeManager()
        {
            Dispose(false); // 仅清理非托管资源
        }

        public void Dispose()
        {
            Dispose(true); // 清理托管和非托管资源
            GC.SuppressFinalize(this); // 禁止调用析构函数
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    StopThreads();

                    _backend_cts.Dispose();
                    _waitAckToken.Dispose();
                    _usbCopyline.Dispose();
                    Logger.Info($"[Main] 调用了Dispose(),清理了类中的非托管资源。");
                }

                // 清理非托管资源（当前没有直接持有非托管资源）
                // 如果有非托管资源，请在这里释放

                _disposed = true;
            }
        }

        public void Initialize()
        {
            // 设定取消信号
            _backend_cts = new CancellationTokenSource();
            _waitAckToken = new CancellationTokenSource();

            // 先初始化 & 打开设备
            _usbCopyline.Initialize();
            _usbCopyline.OpenCopyline();
            if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.ONLINE)
            {
                Logger.Info($"[Main] USB设备已打开, 且设备状态为ONLINE!");
            }
            else
            {
                Logger.Info($"[Main] USB设备不在线: {_usbCopyline.Status}");
            }
        }

        public void StartThreads()
        {
            try
            {
                // 实例化三个后台角色，并将 _syncUSBLock 传入
                _dataReceiver = new DataReceiver(_backend_cts.Token, _usbCopyline);
                _dataReceiver.FatalErrorOccurred += Receiver_FatalErrorOccurred;
                IPacketHandler handler = PacketHandlerFactory.GetHandler(EPacketType.CMD_ACK);

                _dataMonitor = new DataMonitor(this, _backend_cts.Token, _usbCopyline);
                _dataMonitor.FatalErrorOccurred += Monitor_FatalErrorOccurred;

                // 运行读取和监控两个任务
                _receiverTask = _dataReceiver.RunAsync();
                _monitorTask = _dataMonitor.RunAsync();

                Logger.Info("[Main] 监控和读取后台任务已启动。");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Main] 启动后台任务异常: {ex.Message}");
            }
        }

        private void Monitor_FatalErrorOccurred(object sender, InvalidHardwareException ex)
        {
            Logger.Fatal("[PIUsbBridgeManager] 收到Monitor的致命错误事件: " + ex.Message);

            // 这里可以先做一层处理，比如先释放一些资源

            // 把此异常通过Manager自己的事件“往上层”抛
            FatalErrorOccurred?.Invoke(this, ex);
        }

        private void Receiver_FatalErrorOccurred(object sender, InvalidHardwareException ex)
        {
            Logger.Fatal("[PIUsbBridgeManager] 收到Receiver的致命错误事件: " + ex.Message);

            // 这里可以先做一层处理，比如先释放一些资源

            // 把此异常通过Manager自己的事件“往上层”抛
            FatalErrorOccurred?.Invoke(this, ex);
        }

        public async void StopThreads()
        {
            try
            {
                // 发出取消信号
                _backend_cts.Cancel();
                _waitAckToken.Cancel();

                // 异步等待两个个后台任务结束
                var tasks = new List<Task>();
                if (_receiverTask != null) tasks.Add(_receiverTask);
                if (_monitorTask != null) tasks.Add(_monitorTask);

                // 给一个时间限制，比如5秒
                Task allTask = Task.WhenAll(tasks);
                if (await Task.WhenAny(allTask, Task.Delay(Constants.STOP_THREAD_WAIT_TIME)) == allTask)
                {
                    // 所有后台任务成功结束
                    Logger.Info("[Main] 所有后台任务已退出。");
                }
                else
                {
                    // 超时
                    Logger.Warn("[Main] 停止线程时等待超时, 后台线程可能卡住。");
                }
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    if (ex is OperationCanceledException)
                    {
                        Logger.Info($"[Main] 任务正常取消: {ex.Message}");
                    }
                    else
                    {
                        Logger.Error($"[Main] 任务运行异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Main] StopThreads 异常: {ex.Message}");
            }
            Logger.Info("[Main] StopThreads 已完成清理。");
        }

        public async Task<Result<string>> SendBigData(EPacketOwner owner, byte[] data)
        {
            if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.OFFLINE)
            {
                return Result<String>.Failure(1001, "本机的USB设备不可用。");
            }

            if (!_backend_cts.IsCancellationRequested)
            {
                try
                {
                    // 分块切分
                    int chunkSize = Constants.CONTENT_MAX_SIZE; // 每包可用负载
                    int totalLen = data.Length;
                    SynchronizedCollection<Packet> allPackets = new SynchronizedCollection<Packet>(); // 需要传输的总包数

                    // 附加2个包：HEAD包、TAIL包
                    int totalCount = 2 + (int)Math.Ceiling(totalLen / (double)chunkSize);
                    byte[] messageId = Encoding.UTF8.GetBytes(TimeStampIdUtil.GenerateId()); // 带时间戳的13+3位唯一ID

                    // 组装HEAD包
                    byte[] reservedData = new byte[16];
                    for (int i = 0; i < reservedData.Length; i++)
                        reservedData[i] = 0x00; // 将每个字节设置为 0x00
                    Packet headPacket = new Packet
                    {
                        Version = Constants.VER1,
                        Owner = owner,
                        Type = EPacketType.HEAD,
                        TotalCount = (uint)totalCount,
                        Index = (uint)1,
                        TotalLength = (uint)data.Length,
                        ContentLength = (uint)0,
                        MessageId = messageId,
                        Reserved = new byte[16],
                        Content = new byte[(uint)0]
                    };
                    // 准备发送头数据
                    allPackets.Add(headPacket);

                    // 组装业务数据包
                    int offset = 0;
                    for (int i = 1; i < totalCount - 1; i++)
                    {
                        int bytesLeft = totalLen - offset;
                        int sendSize = Math.Min(chunkSize, bytesLeft);

                        Packet sendPacket = new Packet
                        {
                            Version = Constants.VER1,
                            Owner = owner,
                            Type = EPacketType.DATA,
                            TotalCount = (uint)totalCount,
                            Index = (uint)i+1,
                            TotalLength = (uint)data.Length,
                            ContentLength = (uint)sendSize,
                            MessageId = messageId,
                            Reserved = new byte[16],
                            Content = data
                        };

                        // 准备发送业务数据
                        allPackets.Add(sendPacket);
                        offset += sendSize;
                    }

                    // 组装TAIL包（生成摘要数据）

                    // 32位摘要进行base64以后，是固定的44字节
                    String digest = Sha256DigestUtil.ComputeSha256Digest(data);
                    byte[] digestBytes = Convert.FromBase64String(digest);
                    Packet tailPacket = new Packet
                    {
                        Version = Constants.VER1,
                        Owner = owner,
                        Type = EPacketType.TAIL,
                        TotalCount = (uint)totalCount,
                        Index = (uint)totalCount-1,
                        TotalLength = (uint)data.Length,
                        ContentLength = (uint)digestBytes.Length,
                        MessageId = messageId,
                        Reserved = new byte[16],
                        Content = digestBytes
                    };
                    // 准备发送尾数据
                    allPackets.Add(tailPacket);

                    // 待发送数据作为一个整体，一次性全部放入发送队列
                    Result<string> ret = await SendAllPackets(allPackets.ToArray());
                    return ret;

                }
                catch (Exception ex)
                {
                    string errStr = $"预期外错误: {ex.Message}...";
                    Logger.Error($"[Main] {errStr}");
                    return Result<string>.Failure(1002, $"{errStr}");
                }
            }
            string msg = $"任务取消";
            return Result<string>.Success($"[Main] {msg}");
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="allPackets"></param>
        /// <returns>任务</returns>
        private async Task<Result<string>> SendAllPackets(Packet[] allPackets)
        {
            // 构造发送Task

            // 1) 构造发送请求
            _sendRequest = new SendRequest(allPackets);

            // 2) 构造发送器
            DataSender dataSender = new DataSender(_sendRequest, _waitAckToken.Token, _usbCopyline);

            Result<string> ret = Result<String>.Success("发送中...");
            try
            {
                // 3) 等待信号量
                await _oneThreadAtATime.WaitAsync(_backend_cts.Token);
                if (!_backend_cts.IsCancellationRequested)
                {
                    // 4) 发送
                    Result<string>  result = dataSender.RunSendData();

                    if (!result.IsSuccess)
                    {
                        // 说明发送线程那边用 SetResult(false) 代表失败
                        // 也可能用 SetException(...) -> 走catch
                        string msg = $"发送失败！信息:{result.ErrorMessage}";
                        Logger.Error($"[Main] {msg}");
                        ret = Result<string>.Failure(1003, msg);
                    }
                    else
                    {
                        string msg = "发送成功！";
                        Logger.Info($"[Main] {msg}");
                        ret = Result<string>.Success($"{msg}");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                string msg = $"发送超时: {tex.Message}...";
                Logger.Error($"[Main] {msg}");
                ret = Result<string>.Failure(1004, $"{msg}");
            }
            catch (OperationCanceledException)
            {
                string msg = $"[Main] 任务收到取消信号.";
                Logger.Info($"[Main] {msg}");
                ret = Result<string>.Failure(1005, $"{msg}");
            }
            catch (Exception ex)
            {
                string msg = $"预期外错误: {ex.Message}...";
                Logger.Error($"[Main] {msg}");
                ret = Result<string>.Failure(1006, $"{msg}");
            }
            finally
            {
                // 5) 释放信号量
                _oneThreadAtATime.Release();
            }
            return ret;
        }

        public async Task<Result<String>> SendCommand(CommandFormat command)
        {

            if (CurrentMode.Position == EUSBPosition.INSIDE)
            {
                return Result<String>.Failure(1007, "内网电脑不能发送命令。");
            }

            //if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.OFFLINE)
            //{
            //    return Result<String>.Failure(1008, "本机的USB设备不可用。");
            //}

            byte[] messageId = Encoding.UTF8.GetBytes(TimeStampIdUtil.GenerateId()); // 带时间戳的13+3位唯一ID

            // 设置content
            byte[] commandBytes = ComUtil.Truncate(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command)), Constants.CONTENT_MAX_SIZE);

            CommandPacket commandPacket = new CommandPacket
            {
                Version = Constants.VER1,
                Owner = EPacketOwner.OUTERNET,
                TotalCount = (uint)1,
                Index = (uint)1,
                TotalLength = (uint)commandBytes.Length,
                ContentLength = (uint)commandBytes.Length,
                MessageId = messageId,
                Reserved = new byte[16],
                Content = commandBytes
            };
            Logger.Debug($"{commandPacket}");

            SynchronizedCollection<Packet> allPackets = new SynchronizedCollection<Packet>
            {
                commandPacket
            }; // 需要传输的命令包

            // 待发送数据作为一个整体，一次性全部放入发送队列
            Result<string> ret = await SendAllPackets(allPackets.ToArray());
            return ret;
        }

        public Result<String> SendAckPacket(Packet packet)
        {

            if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.OFFLINE)
            {
                return Result<String>.Failure(1009, "本机的USB设备不可用。");
            }

            SynchronizedCollection<Packet> packets = new SynchronizedCollection<Packet>
            {
                packet
            }; // 需要传输的命令包

            // 待发送数据作为一个整体，一次性全部放入发送队列
            Result<string> ret = SynSendPackets(packets.ToArray());
            return ret;
        }

        /// <summary>
        /// 同步发送数据
        /// </summary>
        /// <param name="packets">数据包</param>
        /// <returns>发送结果</returns>
        private Result<string> SynSendPackets(Packet[] packets)
        {
            // 1) 构造发送请求
            _sendRequest = new SendRequest(packets);

            // 2) 构造发送器
            DataSender dataSender = new DataSender(_sendRequest, _waitAckToken.Token, _usbCopyline);

            Result<string> ret = Result<string>.Success("数据发送中...");
            try
            {
                if (!_backend_cts.IsCancellationRequested)
                {
                    // 3) 同步发送
                    Result<string> result = dataSender.RunSendData();

                    if (!result.IsSuccess)
                    {
                        // 发送失败
                        string msg = $"发送失败！信息:{result.ErrorMessage}。";
                        Logger.Error($"{msg}");
                        ret = Result<string>.Failure(1010, msg);
                    }
                    else
                    {
                        string msg = "发送成功！";
                        Logger.Info($"[Main] {msg}");
                        ret = Result<string>.Success($"{msg}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                string msg = $"发送取消，数据发送时收到取消信号。";
                Logger.Info($"{msg}");
                ret = Result<string>.Failure(1012, $"{msg}");
            }
            catch (Exception ex)
            {
                string msg = $"发送失败，预期外错误: {ex.Message}。";
                Logger.Error($"{msg}");
                ret = Result<string>.Failure(1013, $"{msg}");
            }
            finally
            {
            }
            return ret;
        }

    }
}

