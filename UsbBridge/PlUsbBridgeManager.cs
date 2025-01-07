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
using static System.Net.Mime.MediaTypeNames;

namespace Isc.Yft.UsbBridge
{
    internal class PlUsbBridgeManager:IDisposable
    {
        private bool _disposed = false;

        // 三个后台任务引用
        private Task _receiverTask;
        private Task _monitorTask;

        private DataMonitor _dataMonitor;       // 监控线程
        private DataReceiver _dataReceiver;     // 读取线程

        private SendRequest _sendRequest;       // 发送数据包数组+ACK确认包数组

        // One线程执行控制信号
        internal static SemaphoreSlim _oneThreadAtATime = new SemaphoreSlim(1, 1);
        // ACK 事件
        internal static ManualResetEventSlim _ackEvent = new ManualResetEventSlim(false);
        // 监控和接收数据任务取消信号
        private CancellationTokenSource _backend_cts;
        // Ack包等待取消信号
        private CancellationTokenSource _waitAckToken;

        // 事件，用于往更上一层报告错误信息
        public event EventHandler<InvalidHardwareException> FatalErrorOccurred;

        // 用于记录当前USB工作模式
        private USBMode _currentMode = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);

        // 具体的对拷线控制实例 (PL25A1,PL27A1等)
        private readonly ICopyline _usbCopyline;

        public PlUsbBridgeManager()
        {
            // 这里决定用哪个芯片控制类
            // _usbCopyline = new Pl25A1UsbCopyline();
            _usbCopyline = new Pl27A7UsbCopyline();
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
                    Console.WriteLine($"[Main] 调用了Dispose(),清理了类中的非托管资源。");
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
                Console.WriteLine($"[Main] USB设备已打开, 且设备状态为ONLINE!");
            }
            else
            {
                Console.WriteLine($"[Main] USB设备不在线: {_usbCopyline.Status}");
            }
        }

        public void StartThreads()
        {
            try
            {
                // 实例化三个后台角色，并将 _syncUSBLock 传入
                _dataReceiver = new DataReceiver(_sendRequest, _backend_cts.Token, _usbCopyline);
                _dataReceiver.FatalErrorOccurred += Receiver_FatalErrorOccurred;

                _dataMonitor = new DataMonitor(this, _backend_cts.Token, _usbCopyline);
                _dataMonitor.FatalErrorOccurred += Monitor_FatalErrorOccurred;

                // 运行读取和监控两个任务
                _receiverTask = _dataReceiver.RunAsync();
                _monitorTask = _dataMonitor.RunAsync();

                Console.WriteLine("[Main] 监控和读取后台任务已启动。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] 启动后台任务异常: {ex.Message}");
            }
        }

        private void Monitor_FatalErrorOccurred(object sender, InvalidHardwareException ex)
        {
            Console.WriteLine("[PIUsbBridgeManager] 收到Monitor的致命错误事件: " + ex.Message);

            // 这里可以先做一层处理，比如先释放一些资源

            // 把此异常通过Manager自己的事件“往上层”抛
            FatalErrorOccurred?.Invoke(this, ex);
        }

        private void Receiver_FatalErrorOccurred(object sender, InvalidHardwareException ex)
        {
            Console.WriteLine("[PIUsbBridgeManager] 收到Receiver的致命错误事件: " + ex.Message);

            // 这里可以先做一层处理，比如先释放一些资源

            // 把此异常通过Manager自己的事件“往上层”抛
            FatalErrorOccurred?.Invoke(this, ex);
        }

        private void _dataReceiver_AckReceived(Packet obj)
        {
            throw new NotImplementedException();
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
                if (await Task.WhenAny(allTask, Task.Delay(5000)) == allTask)
                {
                    // 所有后台任务成功结束
                    Console.WriteLine("[Main] 所有后台任务已退出。");
                }
                else
                {
                    // 超时
                    Console.WriteLine("[Main] 停止线程时等待超时, 后台线程可能卡住。");
                }
                Console.WriteLine("[Main] 所有后台任务已退出。");
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    if (ex is OperationCanceledException)
                    {
                        Console.WriteLine($"[Main] 任务正常取消: {ex.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"[Main] 任务运行异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] StopThreads 异常: {ex.Message}");
            }
            Console.WriteLine("[Main] StopThreads 已完成清理。");
        }

        public async Task<Result<string>> SendBigData(EPacketOwner owner, byte[] data)
        {
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
                    byte[] messageId = Encoding.ASCII.GetBytes(TimeStampIdUtil.GenerateId()); // 带时间戳的13+4位唯一ID

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
                        ContentLength = (uint)0
                    };
                    Array.Copy(messageId, 0, headPacket.MessageId, 0, 17);
                    Array.Copy(reservedData, 0, headPacket.Reserved, 0, 16);
                    headPacket.AddCRC();
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
                            ContentLength = (uint)sendSize
                        };
                        // 需要先为 Content 分配空间
                        sendPacket.Content = new byte[(uint)sendSize];
                        
                        Array.Copy(messageId, 0, sendPacket.MessageId, 0, 16);
                        Array.Copy(reservedData, 0, sendPacket.Reserved, 0, 16);
                        Array.Copy(data, offset, sendPacket.Content, 0, sendSize);
                        sendPacket.AddCRC();

                        // 准备发送业务数据
                        allPackets.Add(sendPacket);
                        offset += sendSize;
                    }

                    // 组装TAIL包（生成摘要数据）
                    Packet tailPacket = new Packet
                    {
                        Version = Constants.VER1,
                        Owner = owner,
                        Type = EPacketType.TAIL,
                        TotalCount = (uint)totalCount,
                        Index = (uint)totalCount-1,
                        TotalLength = (uint)data.Length,
                    };
                    // 32位摘要进行base64以后，是固定的44字节
                    String digest = Sha256DigestUtil.ComputeSha256Digest(data);
                    byte[] digestBytes = Convert.FromBase64String(digest);
                    tailPacket.ContentLength = (uint)digestBytes.Length;
                    // 需要先为 Content 分配空间
                    tailPacket.Content = new byte[(uint)digestBytes.Length];
                    Array.Copy(digestBytes, 0, tailPacket.Content, 0, tailPacket.ContentLength);

                    Array.Copy(messageId, 0, tailPacket.MessageId, 0, 17);
                    Array.Copy(reservedData, 0, tailPacket.Reserved, 0, 16);
                    tailPacket.AddCRC();
                    // 准备发送尾数据
                    allPackets.Add(tailPacket);

                    // 待发送数据作为一个整体，一次性全部放入发送队列
                    Result<string> ret = await SendAllPackets(allPackets.ToArray());
                    return ret;

                }
                catch (Exception ex)
                {
                    string errStr = $"预期外错误: {ex.Message}...";
                    Console.WriteLine($"[Main] {errStr}");
                    return Result<string>.Failure(1004, $"{errStr}");
                }
            }
            string msg = $"任务取消";
            return Result<string>.Success($"[Main] {msg}");
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="allPackets"></param>
        /// <returns>任务</returns>
        public async Task<Result<string>> SendAllPackets(Packet[] allPackets)
        {
            // 构造发送Task

            // 1) 构造发送请求
            _sendRequest = new SendRequest(allPackets);

            // 2) 构造发送器
            DataSender dataSender = new DataSender(_sendRequest, _waitAckToken.Token, _usbCopyline);
            _dataReceiver.AckReceived += dataSender.OnAckReceived;

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
                        Console.WriteLine($"[Main] {msg}");
                        ret = Result<string>.Failure(1001, msg);
                    }
                    else
                    {
                        string msg = "发送成功！";
                        Console.WriteLine($"[Main] {msg}");
                        ret = Result<string>.Success($"{msg}");
                    }
                }
            }
            catch (TimeoutException tex)
            {
                string msg = $"发送超时: {tex.Message}...";
                Console.WriteLine($"[Main] {msg}");
                ret = Result<string>.Failure(1002, $"{msg}");
            }
            catch (OperationCanceledException)
            {
                string msg = $"[Main] 任务收到取消信号.";
                Console.WriteLine($"[Main] {msg}");
                ret = Result<string>.Failure(1004, $"{msg}");
            }
            catch (Exception ex)
            {
                string msg = $"预期外错误: {ex.Message}...";
                Console.WriteLine($"[Main] {msg}");
                ret = Result<string>.Failure(1003, $"{msg}");
            }
            finally
            {
                // 5) 释放信号量
                _oneThreadAtATime.Release();
            }
            return ret;
        }

        public void SetMode(USBMode status)
        {
            _currentMode = status;
            Console.WriteLine($"[Main] SetMode = {status}");
        }

        public USBMode GetCurrentMode()
        {
            return _currentMode;
        }
    }
}
