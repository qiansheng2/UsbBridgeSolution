using Isc.Yft.UsbBridge.Handler;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataSender
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SendRequest _request;

        // 取消信号
        private readonly CancellationToken _waitAckToken;

        // 具体的对拷线控制实例
        private readonly ICopyline _usbCopyline;

        // ACK 事件
        internal static ManualResetEventSlim _ackEvent = new ManualResetEventSlim(false);

        // 事件处理器
        private readonly DataAckPacketHandler _dataAckPacketHandler;

        public DataSender(SendRequest request, CancellationToken token, ICopyline usbCopyline)
        {
            _request = request;
            _waitAckToken = token;
            _usbCopyline = usbCopyline;
            _ackEvent = new ManualResetEventSlim(false);

            // DATA_ACK 包处理器定义和注册
            _dataAckPacketHandler = new DataAckPacketHandler();
            //_dataAckPacketHandler.AckReceived += OnAckReceived;
            PacketHandlerFactory.RegisterHandler(EPacketType.DATA_ACK, _dataAckPacketHandler);
        }

        public Result<string> RunSendData()
        {
            Result<string> sendResult;
            SendRequest request = _request;
            try
            {
                Logger.Info("[DataSender] --------------S Start--------------------");

                // (1) 检查拷贝线状态是否可用
                _usbCopyline.OpenCopyline();
                _usbCopyline.UpdateCopylineStatus();
                if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.ONLINE)
                {
                    // 看是否有待发送数据
                    if (_request == null)
                    {
                        string errStr = $"[DataSender] 发送Request内容为空，无数据可发送.";
                        Logger.Warn(errStr);
                        sendResult = Result<string>.Failure(1204, errStr);
                    }
                    else
                    {
                        // 取出要发送的数据包
                        if (request.Packets != null && request.Packets.Length > 0)
                        {
                            Logger.Info($"[DataSender] 准备发送 {request.Packets.Length} 个包; " +
                                                $"总包数: {request.Packets[0].TotalCount}, " +
                                                $"总字节数(含32字节摘要): {request.Packets[0].TotalLength + 32}");

                            // (2) 逐个发送
                            foreach (Packet packet in request.Packets)
                            {
                                Logger.Info($"[DataSender] 发送包 => {packet}");
                                int written = _usbCopyline.WriteDataToDevice(packet.ToBytes());
                                Logger.Info($"[DataSender] 已发送[{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.");
                                    
                                // (3) 如果是业务数据包 => 等待对端 ACK
                                //     (可根据第一包的Type或其它方式判断)
                                if (packet.Type != EPacketType.DATA_ACK)
                                {
                                    // 重置 ackEvent
                                    _ackEvent.Reset();
                                    Logger.Info("[DataSender] 等待ACK...");
                                    // 等待一定超时时间 & 或收到取消消息
                                    bool signaled = _ackEvent.Wait(Constants.ACK_TIMEOUT_MS, _waitAckToken);
                                    if (!signaled)
                                    {
                                        // --> 超时
                                        string errStr = $"[DataSender] 等待ACK超时: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.";
                                        Logger.Error(errStr);
                                        sendResult = Result<string>.Failure(1200, errStr);
                                    }
                                    else
                                    {
                                        // --> 成功收到Ack包
                                        Logger.Info("[DataSender] 收到接收数据线程通知，已收到一个ACK包。");
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
                    Logger.Error(errStr);
                    sendResult = Result<string>.Failure(1202, errStr);
                }
            }
            catch (Exception ex)
            {
                string errStr = $"[DataSender] 发生异常: {ex.Message}";
                Logger.Error(errStr);
                sendResult = Result<string>.Failure(1203, errStr);
            }
            finally
            {
                Logger.Info("[DataSender] --------------S End----------------------");
            }
            return sendResult;
        }

        public void OnAckReceived(Packet packet)
        {
            _request.SetAck(packet);
        }
    }
}
