using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataSender
    {
        private readonly BlockingCollection<SendRequest> _sendQueue;
        private readonly CancellationToken _token;

        // 具体的对拷线控制实例
        private readonly IUsbCopyline _usbCopyline;

        public DataSender(BlockingCollection<SendRequest> sendQueue,
                          CancellationToken token,
                          IUsbCopyline usbCopyline)
        {
            _sendQueue = sendQueue;
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
                SendRequest request = null;
                try
                {
                    // （1）请求单线程互斥锁
                    await PlUsbBridgeManager._oneThreadAtATime.WaitAsync(_token);
                    Console.WriteLine("[DataSender] 获得互斥锁, 开始发送数据...");

                    // 检查拷贝线状态是否可用
                    CopylineStatus status = _usbCopyline.ReadCopylineStatus(false);
                    if (status.Usable == ECopylineUsable.OK)
                    {
                        // 看是否有待发送数据
                        if (_sendQueue.Count == 0)
                        {
                            Console.WriteLine("[DataSender] 队列为空，暂无数据可发送.");
                        }
                        else
                        {
                            // 取下一组要发送的数据包
                            request = _sendQueue.Take(_token);
                            if (request.Packets != null && request.Packets.Length > 0)
                            {
                                Console.WriteLine($"[DataSender] 准备发送 {request.Packets.Length} 个包; " +
                                                  $"总包数: {request.Packets[0].TotalCount}, " +
                                                  $"总字节数(含32字节摘要): {request.Packets[0].TotalLength + 32}");

                                // (2) 逐个发送
                                foreach (Packet packet in request.Packets)
                                {
                                    Console.WriteLine($"[DataSender] 发送包 => " +
                                                      $"Type={packet.Type}, " +
                                                      $"Index={packet.Index}/{packet.TotalCount}, " +
                                                      $"Length={packet.ContentLength}.");

                                    int written = _usbCopyline.WriteDataToDevice(packet.ToBytes());
                                    Console.WriteLine($"[DataSender] 已发送[{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.");
                                    
                                    // (3) 如果是业务数据包 => 等待对端 ACK
                                    //     (可根据第一包的Type或其它方式判断)
                                    if (packet.Type != EPacketType.ACK)
                                    {
                                        // 重置 ackEvent
                                        PlUsbBridgeManager._ackEvent.Reset();
                                        Console.WriteLine("[DataSender] 等待ACK...");
                                        // 等待一定超时时间
                                        bool signaled = PlUsbBridgeManager._ackEvent.Wait(Constants.ACK_TIMEOUT_MS, _token);
                                        if (!signaled)
                                        {
                                            // --> 超时
                                            string errStr = $"[DataSender] 等待ACK超时: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.";
                                            Console.WriteLine(errStr);
                                            request.Tcs.SetException(new TimeoutException(errStr));
                                            // 中断后续发送
                                            break;
                                        }
                                        else
                                        {
                                            // --> 成功
                                            request.Tcs.SetResult(true);
                                        }
                                    }
                                    else
                                    {
                                        // 如果是 ACK 包, 说明这是特殊发送
                                        request.Tcs.SetResult(true);
                                    }
                                }
                            }
                            else
                            {
                                // 可能 Packet[] 为空, 向主线程返回异常情况
                                request.Tcs.SetResult(false);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("[DataSender] USB设备不可用, 无法发送数据");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[DataSender] 取消信号, 即将退出发送循环.");
                    // 线程被取消
                    if (request != null)
                        request.Tcs.SetCanceled();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataSender] 发生异常: {ex.Message}");
                    // 其他异常，告知主线程异常
                    if (request != null)
                        request.Tcs.SetException(ex);
                }
                finally
                {
                    // (4) 释放互斥锁
                    Console.WriteLine("[DataSender] 释放锁, 资源清理完毕.");
                    PlUsbBridgeManager._oneThreadAtATime.Release();
                    // 稍作sleep, 避免空转
                    await Task.Delay(Constants.THREAD_SWITCH_SLEEP_TIME, _token);
                }

            }
        }


    }
}
