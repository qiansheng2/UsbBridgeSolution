using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataReceiver
    {
        private readonly CancellationToken _token;

        // 具体的对拷线控制实例
        private readonly IUsbCopyline _usbCopyline;
        private bool _firstRun = true;

        public DataReceiver(CancellationToken token, IUsbCopyline usbCopyline)
        {
            _token = token;
            _usbCopyline = usbCopyline;
        }

        public Task RunAsync()
        {
            return RunLoopAsync();
        }

        private async Task RunLoopAsync()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    int readCount = 0;

                    // 1) 先请求单线程互斥锁，保证此时只有本线程在工作
                    await PlUsbBridgeManager._oneThreadAtATime.WaitAsync(_token);
                    Console.WriteLine("[DataReceiver] 获得互斥锁, 开始接收数据...");

                    // 2) 检查USB对拷线状态
                    CopylineStatus status = _usbCopyline.ReadCopylineStatus(false);
                    if (status.Usable == ECopylineUsable.OK)
                    {
                        if (_firstRun)
                        {
                            _firstRun = false;
                            // flush操作后，再从缓冲区正式读取数据
                            FlushOnce();
                        }

                        // 3) 正式读数据
                        byte[] buffer = new byte[Constants.PACKET_MAX_SIZE]; // 缓冲
                        Array.Clear(buffer, 0, buffer.Length); // 将 buffer 的所有元素设置为 0x00
                        readCount = _usbCopyline.ReadDataFromDevice(buffer); // 调用对拷线的 ReadDataFromDevice
                        if (readCount < Constants.PACKET_MIN_SIZE || readCount > Constants.PACKET_MAX_SIZE) {
                            throw new InvalidCastException($"[DataReceiver] 读取到的数据不符合预期，数据长度[{readCount}] > 1024，直接抛弃。");
                        }
                        else if(readCount > 0)
                        {
                            Console.WriteLine($"[DataReceiver] 接收到 {readCount} 字节: {BitConverter.ToString(buffer, 0, readCount)}");

                            // 4) 此处可进行解析: 将 raw bytes 转换为 Packet(s)
                            // （示例: 只处理单包; 若有粘包/分包, 需更复杂逻辑）
                            try
                            {
                                Packet packet = TryParsePacket(buffer, readCount);
                                if (packet != null)
                                {
                                    Console.WriteLine($"[DataReceiver] 解析到包: Type={packet.Type}, Index={packet.Index}/{packet.TotalCount}, Length={packet.ContentLength}");

                                    if (packet.Type == EPacketType.ACK)
                                    {
                                        // 通知发送线程
                                        Console.WriteLine("[DataReceiver] 收到ACK, 即将Set事件唤醒DataSender");
                                        PlUsbBridgeManager._ackEvent.Set();
                                    }
                                    else
                                    {
                                        // 其他业务包 => 做后续处理
                                        // 例如存入某个队列, 或上层回调
                                        HandleBusinessPacket(packet);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[DataReceiver] 无法解析为Packet, 忽略或等待更多数据");
                                }
                            }
                            catch (Exception parseEx)
                            {
                                Console.WriteLine($"[DataReceiver] 解析数据包时发生异常: {parseEx.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[DataReceiver] 本轮没有从拷贝线读到数据,数据长度:[{readCount}]。");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DataReceiver] USB设备不可用，无法接收数据。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[DataReceiver] 任务收到取消信号.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataReceiver] 发生预期外异常: {ex.Message}.");
                    break;
                }
                finally
                {
                    // 5) 释放互斥锁 + 让出CPU
                    Console.WriteLine("[DataReceiver] 释放锁, 资源清理完毕.");
                    PlUsbBridgeManager._oneThreadAtATime.Release();
                    await Task.Delay(Constants.THREAD_SWITCH_SLEEP_TIME, _token);
                }
            }
            Console.WriteLine("[DataReceiver] 接收循环结束.");
        }

        /// <summary>
        /// 首次 flush: 读取并抛弃缓冲区中残留数据
        /// </summary>
        private void FlushOnce()
        {
            byte[] flushBuf = new byte[1024 * 1000];
            int flushCount = _usbCopyline.ReadDataFromDevice(flushBuf);
            if (flushCount > 0)
            {
                Console.WriteLine($"[DataReceiver] flush操作接收到 {flushCount} 字节, 已抛弃.");
            }
        }

        /// <summary>
        /// 尝试将原始字节解析为一个 Packet, 这里只是示例
        /// </summary>
        private Packet TryParsePacket(byte[] buffer, int readCount)
        {
            // 简单示例: 假设 readCount=1024 正好是一个Packet
            // 实际中可能需要更多判断, 或循环多次解析(粘包场景)
            if (readCount < 32)
            {
                // 不足以构成最小头部? 
                return null;
            }

            // 调用 Packet.FromBytes() 解析
            Packet packet = new Packet().FromBytes(SubArray(buffer, readCount));
            return packet;
        }

        /// <summary>
        /// 示例: 截取数组中前 readCount 字节
        /// </summary>
        private byte[] SubArray(byte[] buffer, int readCount)
        {
            byte[] result = new byte[readCount];
            Array.Copy(buffer, 0, result, 0, readCount);
            return result;
        }

        /// <summary>
        /// 对于非ACK包的业务处理 (示例)
        /// </summary>
        private void HandleBusinessPacket(Packet packet)
        {
            // TODO: 你可以存入某队列, 交给上层去拼装, or 其他逻辑
            Console.WriteLine($"[DataReceiver] 收到业务包, Type={packet.Type}, Index={packet.Index}/{packet.TotalCount}");
        }
    }
}
