using Isc.Yft.UsbBridge;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataSender
    {
        private readonly BlockingCollection<Packet[]> _sendQueue;
        private readonly CancellationToken _token;

        // 具体的对拷线控制实例
        private readonly IUsbCopyLine _usbCopyLine;

        public DataSender(BlockingCollection<Packet[]> sendQueue,
                          CancellationToken token,
                          IUsbCopyLine usbCopyLine)
        {
            _sendQueue = sendQueue;
            _token = token;
            _usbCopyLine = usbCopyLine;
        }

        public Task RunAsync()
        {
            return Task.Run(() => RunLoop(), _token);
        }

        private void RunLoop()
        {
            Console.WriteLine("[DataSender] 开始发送线程循环...");
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // 获取发送线程开始许可
                    PlUsbBridgeManager._senderSemaphore.Wait(_token);
                    Console.WriteLine("[DataSender] 检查是否有待发送数据...");

                    // 检查队列是否有数据，避免长时间阻塞
                    if (_sendQueue.Count == 0)
                    {
                        Console.WriteLine("[DataSender] 队列为空，当前没有待发送数据.");
                        Thread.Sleep(500); // 避免忙等，释放 CPU
                    }
                    else
                    {
                        // 阻塞获取下一段要发送的数据
                        Packet[] packets = _sendQueue.Take(_token);

                        if (packets != null)
                        {
                            Console.WriteLine($"[DataSender] 开始发送数据，总包数：{packets[0].TotalCount}，总字节数（含32位摘要）： {packets[0].TotalLength + 32}.");
                            foreach (Packet packet in packets)
                            {
                                // 实际调用对拷线的 WriteDataToDevice
                                Console.WriteLine($"[DataSender] 应发送{packet.Type}包，包数：{packet.Index}/{packet.TotalCount}，字节数： {packet.ContentLength}.");
                                int written = _usbCopyLine.WriteDataToDevice(packet.ToBytes());
                                Console.WriteLine($"[DataSender] 已发送 {written} 字节.");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[DataSender] 任务收到取消信号。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataSender] 未处理异常: {ex}.");
                }
                finally
                {
                    // 唤醒读取线程
                    Console.WriteLine("[DataSender] 资源清理完毕.");
                    PlUsbBridgeManager._receiverSemaphore.Release();
                    Thread.Sleep(1000); // 释放 CPU
                }
            }
        }
    }
}
