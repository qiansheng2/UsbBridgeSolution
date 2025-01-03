using System;
using System.Collections.Concurrent;
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
        private Task _senderTask;
        private Task _receiverTask;
        private Task _monitorTask;

        private DataMonitor _dataMonitor;       // 监控线程
        private DataSender _dataSender;         // 写入线程
        private DataReceiver _dataReceiver;     // 读取线程

        // 单线程执行控制信号
        internal static SemaphoreSlim _oneThreadAtATime = new SemaphoreSlim(1, 1);
        // ACK 事件
        internal static ManualResetEventSlim _ackEvent = new ManualResetEventSlim(false);


        private BlockingCollection<SendRequest> _sendQueue;
        private CancellationTokenSource _cts;

        // 用于记录当前USB工作模式
        private USBMode _currentMode = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);

        // 具体的对拷线控制实例 (PL25A1,PL27A1等)
        private readonly IUsbCopyline _usbCopyline;

        public PlUsbBridgeManager()
        {
            _sendQueue = new BlockingCollection<SendRequest>();
            _cts = new CancellationTokenSource();

            // 这里决定用哪个芯片控制类
            // _usbCopyline = new Pl25A1UsbCopyline();
            _usbCopyline = new Pl27A7UsbCopyline();

            // 先初始化 & 打开设备
            _usbCopyline.Initialize();
            bool opened = _usbCopyline.OpenDevice();
            if (!opened)
            {
                Console.WriteLine($"[Main] 警告: USB设备打开失败, 后续无法通信!");
            }
            else
            {
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
        }

        public void StartThreads()
        {
            try
            {
                // 实例化三个后台角色，并将 _syncUSBLock 传入
                _dataSender = new DataSender(_sendQueue, _cts.Token, _usbCopyline);
                _dataReceiver = new DataReceiver(_cts.Token, _usbCopyline);
                _dataMonitor = new Threading.DataMonitor(this, _cts.Token, _usbCopyline);

                // 运行三个任务
                _senderTask = _dataSender.RunAsync();
                _receiverTask = _dataReceiver.RunAsync();
                _monitorTask = _dataMonitor.RunAsync();

                Console.WriteLine("[Main] 后台任务已启动。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] 启动后台任务异常: {ex.Message}");
            }
        }

        public void StopThreads()
        {
            try
            {
                // 发出取消信号
                _cts.Cancel();

                // 等待三个后台任务结束
                Task.WaitAll(_senderTask, _receiverTask, _monitorTask);

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
                _sendQueue.Dispose();
                _sendQueue = new BlockingCollection<SendRequest>();

                _cts.Dispose();
                _cts = new CancellationTokenSource();

                // 最后关闭对拷线
                _usbCopyline.CloseDevice();
                _usbCopyline.Exit();

                Console.WriteLine("[Main] StopThreads 已完成清理。");
            }
        }

        public async Task SendBigData(EPacketOwner owner, byte[] data)
        {
            if (!_cts.IsCancellationRequested)
            {
                try
                {
                    // 分块切分
                    int chunkSize = Constants.ContentMaxLength; // 每包可用负载
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

                    Array.Copy(messageId, 0, tailPacket.MessageId, 0, 16);
                    Array.Copy(reservedData, 0, tailPacket.Reserved, 0, 16);
                    tailPacket.AddCRC();
                    // 准备发送尾数据
                    allPackets.Add(tailPacket);

                    // 待发送数据作为一个整体，一次性全部放入发送队列
                    await SendAllPacketsAsync(allPackets.ToArray());

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Main] 添加数据到发送队列异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="allPackets"></param>
        /// <returns>任务</returns>
        public async Task SendAllPacketsAsync(Packet[] allPackets)
        {
            // 1) 构造发送请求
            SendRequest request = new SendRequest(allPackets);

            // 2) 扔到队列
            _sendQueue.Add(request);

            try
            {
                // 3) 等待发送线程的处理结果
                bool success = await request.Tcs.Task;

                if (!success)
                {
                    // 说明发送线程那边用 SetResult(false) 代表失败
                    // 也可能用 SetException(...) -> 走catch
                    Console.WriteLine("[Main] 本次发送失败...");
                }
                else
                {
                    Console.WriteLine("[Main] 发送成功，并成功读取到ACK!");
                }
            }
            catch (TimeoutException tex)
            {
                Console.WriteLine($"[Main] 发送超时: {tex.Message}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] 数据发送中发生了其他异常: {ex.Message}...");
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
