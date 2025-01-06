using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class SynDataSender
    {
        private readonly SendRequest _request;

        // 取消信号
        private readonly CancellationToken _token;

        // 具体的对拷线控制实例
        private readonly IUsbCopyline _usbCopyline;

        public SynDataSender(SendRequest request, CancellationToken token, IUsbCopyline usbCopyline)
        {
            _request = request;
            _token = token;
            _usbCopyline = usbCopyline;
        }

        public Result<string> RunSendData()
        {
            Result<string> sendResult;
            SendRequest request = _request;
            try
            {
                // (1) 检查拷贝线状态是否可用
                CopylineStatus status = _usbCopyline.ReadCopylineStatus(false);
                if (status.Usable == ECopylineUsable.OK)
                {
                    // 看是否有待发送数据
                    if (_request == null)
                    {
                        string errStr = $"[DataSender] 发送Request内容为空，无数据可发送.";
                        Console.WriteLine(errStr);
                        sendResult = Result<string>.Failure(1204, errStr);
                    }
                    else
                    {
                        // 取出要发送的数据包
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
                                    // 等待一定超时时间 & 或收到取消消息
                                    bool signaled = PlUsbBridgeManager._ackEvent.Wait(Constants.ACK_TIMEOUT_MS, _token);
                                    if (!signaled)
                                    {
                                        // --> 超时
                                        string errStr = $"[DataSender] 等待ACK超时: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.";
                                        Console.WriteLine(errStr);
                                        sendResult = Result<string>.Failure(1200, errStr);
                                    }
                                    else
                                    {
                                        // --> 成功收到Ack包
                                        Console.WriteLine("[DataSender] 收到接收数据线程通知，已收到一个ACK包。");
                                    }
                                }
                                else
                                {
                                    // 如果是 ACK 包, 这是特殊发送（不需要等待）
                                    sendResult = Result<string>.Success("[DataSender] ACK包发送成功: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.");
                                }
                            }
                            sendResult = Result<string>.Success("[DataSender] 本次发送全部成功: [{packet.TotalCount}]个包，内容长度：{packet.ContentLength}.");
                        }
                        else
                        {
                            // 可能 Packet[] 为空, 向主线程返回异常情况
                            sendResult = Result<string>.Failure(1201, "[DataSender] Packet中没有可发送的数据存在.");
                        }
                    }
                }
                else
                {
                    string errStr = "[DataSender] USB设备不可用, 无法发送数据.";
                    Console.WriteLine(errStr);
                    sendResult = Result<string>.Failure(1202, errStr);
                }
            }
            catch (Exception ex)
            {
                string errStr = $"[DataSender] 发生异常: {ex.Message}";
                Console.WriteLine(errStr);
                sendResult = Result<string>.Failure(1203, errStr);
            }
            finally
            {
            }
            return sendResult;
        }

        public void OnAckReceived(Packet packet)
        {
            _request.SetAck(packet);
        }
    }
}
