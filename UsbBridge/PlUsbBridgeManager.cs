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

namespace Isc.Yft.UsbBridge
{
    internal class PlUsbBridgeManager
    {
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
        // 后台任务取消信号
        private CancellationTokenSource _backend_cts;
        // 数据发送任务取消信号
        private CancellationTokenSource _send_data_cts;

        // 用于记录当前USB工作模式
        private USBMode _currentMode = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);

        // 具体的对拷线控制实例 (PL25A1,PL27A1等)
        private readonly IUsbCopyline _usbCopyline;

        public PlUsbBridgeManager()
        {
            _backend_cts = new CancellationTokenSource();
            _send_data_cts = new CancellationTokenSource();

            // 这里决定用哪个芯片控制类
            // _usbCopyline = new Pl25A1UsbCopyline();
            _usbCopyline = new Pl27A7UsbCopyline();

            try
            {
                // 先初始化 & 打开设备
                _usbCopyline.Initialize();
                _usbCopyline.OpenDevice();
                CopylineStatus info = _usbCopyline.ReadCopylineStatus(true);
                if (info.Usable == ECopylineUsable.OK)
                {
                    Console.WriteLine($"[Main] USB设备已打开, 且设备状态为可用!");
                }
                else
                {
                    Console.WriteLine($"[Main] USB设备已打开, 但设备不可用: {info}");
                }
            }
            catch
            {
                Console.WriteLine($"[Main] 警告: USB设备打开失败, 后续无法通信!");
            }
        }

        public void StartThreads()
        {
            try
            {
                // 实例化三个后台角色，并将 _syncUSBLock 传入
                _dataReceiver = new DataReceiver(_sendRequest, _backend_cts.Token, _usbCopyline);
                _dataMonitor = new DataMonitor(this, _backend_cts.Token, _usbCopyline);

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

        private void _dataReceiver_AckReceived(Packet obj)
        {
            throw new NotImplementedException();
        }

        public void StopThreads()
        {
            try
            {
                // 发出取消信号
                _backend_cts.Cancel();
                _send_data_cts.Cancel();

                // 等待两个个后台任务结束
                Task.WaitAll(_receiverTask, _monitorTask);

                Console.WriteLine("[Main] 所有后台任务已退出。");
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    if (ex is OperationCanceledException)
                    {
                        Console.WriteLine($"[Main] 任务取消: {ex.Message}");
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
            finally
            {

                _backend_cts.Dispose();
                _backend_cts = new CancellationTokenSource();

                _send_data_cts.Dispose();
                _send_data_cts = new CancellationTokenSource();

                // 最后关闭对拷线
                _usbCopyline.CloseDevice();
                _usbCopyline.Exit();

                Console.WriteLine("[Main] StopThreads 已完成清理。");
            }
        }

        public Result<string> SendBigData(EPacketOwner owner, byte[] data)
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
                    Result<string> ret = SendAllPackets(allPackets.ToArray());
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
        public Result<string> SendAllPackets(Packet[] allPackets)
        {
            // 构造发送Task

            // 1) 构造发送请求
            _sendRequest = new SendRequest(allPackets);

            // 2) 构造发送器
            SynDataSender dataSender = new SynDataSender(_sendRequest, _send_data_cts.Token, _usbCopyline);
            _dataReceiver.AckReceived += dataSender.OnAckReceived;
            try
            {
                // 3) 发送
                Result<string> result = dataSender.RunSendData();

                if (!result.IsSuccess)
                {
                    // 说明发送线程那边用 SetResult(false) 代表失败
                    // 也可能用 SetException(...) -> 走catch
                    string msg = $"发送失败！信息:{result.ErrorMessage}";
                    Console.WriteLine($"[Main] {msg}");
                    return Result<string>.Failure(1001, msg);
                }
                else
                {
                    string msg = "发送成功！";
                    Console.WriteLine($"[Main] {msg}");
                    return Result<string>.Success($"{msg}");
                }
            }
            catch (TimeoutException tex)
            {
                string msg = $"发送超时: {tex.Message}...";
                Console.WriteLine($"[Main] {msg}");
                return Result<string>.Failure(1002, $"{msg}");
            }
            catch (Exception ex)
            {
                string msg = $"预期外错误: {ex.Message}...";
                Console.WriteLine($"[Main] {msg}");
                return Result<string>.Failure(1003, $"{msg}");
            }
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
