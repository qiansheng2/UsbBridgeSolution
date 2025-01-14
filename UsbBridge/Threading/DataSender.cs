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

        public DataSender(SendRequest request, CancellationToken token, ICopyline usbCopyline)
        {
            _request = request;
            _waitAckToken = token;
            _usbCopyline = usbCopyline;
            _ackEvent = new ManualResetEventSlim(false);
        }

        public Result<string> RunSendData()
        {
            Result<string> sendResult;
            SendRequest request = _request;
            try
            {
                Logger.Info("[DataSender] --------------S Start--------------------");

                // 检查拷贝线状态是否可用
                _usbCopyline.OpenCopyline();
                _usbCopyline.UpdateCopylineStatus();
                if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.ONLINE)
                {
                    // 看是否有待发送数据
                    if (_request == null)
                    {
                        string errStr = $"[DataSender] 发送Request内容为空，无数据可发送.";
                        Logger.Warn(errStr);
                        sendResult = Result<string>.Failure(1201, errStr);
                    }
                    else
                    {
                        // 取出要发送的数据包
                        if (request.Packets != null && request.Packets.Length > 0)
                        {
                            Logger.Info($"[DataSender] 准备发送 {request.Packets.Length} 个包; " +
                                                $"总包数: {request.Packets[0].TotalCount}, " +
                                                $"总字节数(含32字节摘要): {request.Packets[0].TotalLength + 32}");

                            // (2) 逐个packet发送
                            foreach (Packet packet in request.Packets)
                            {
                                Logger.Info($"[DataSender] 发送包 => {packet}");
                                int written = _usbCopyline.WriteDataToDevice(packet.ToBytes());
                                Logger.Info($"[DataSender] 已发送[{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.");
                                    
                                // (3) 如果是业务数据包 => 等待对端 ACK
                                if (packet.Type == EPacketType.DATA || packet.Type == EPacketType.CMD ||
                                    packet.Type == EPacketType.HEAD || packet.Type == EPacketType.TAIL)
                                {
                                    // 【同步】等待ack数据包到来
                                    Logger.Info("[DataSender] 等待ACK...");
                                    bool gotAck = TryReadAckPacket(Constants.ACK_TIMEOUT_MS, out Packet ackPacket);
                                    if (!gotAck)
                                    {
                                        // --> 成功收到Ack包
                                        Logger.Info("[DataSender] 收到一个ACK包。");
                                        // 设置发送包的已收到ACK标志位
                                        request.SetAck(ackPacket);
                                        return Result<string>.Success($"成功收到ACK包:{packet}");
                                    }
                                    else
                                    {
                                        // --> 超时或其他失败
                                        string errStr = $"[DataSender] 等待ACK超时: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.";
                                        Logger.Error(errStr);
                                        sendResult = Result<string>.Failure(120, errStr);
                                    }
                                }
                                else if (packet.Type == EPacketType.DATA_ACK || packet.Type == EPacketType.CMD_ACK ||
                                         packet.Type == EPacketType.HEAD_ACK || packet.Type == EPacketType.TAIL_ACK   )
                                {
                                    // ACK 包,直接发送（不需要等待）
                                    sendResult = Result<string>.Success("[DataSender] ACK包发送成功: [{packet.Index}/{packet.TotalCount}]{packet.Type}包，内容长度：{packet.ContentLength}，写入了{written} 字节.");
                                }
                            }
                            if (request.AreAllAcksReceived())
                            {
                                sendResult = Result<string>.Success("[DataSender] 本次发送全部成功: [{packet.TotalCount}]个包，内容长度：{packet.ContentLength}.");
                            }
                            else
                            {
                                string errStr = "已成功发送所有的包，但是没有获取到所有的Ack包。";
                                Logger.Error(errStr);
                                sendResult = Result<string>.Failure(1203, errStr);
                            }
                        }
                        else
                        {
                            // 可能 Packet[] 为空, 向主线程返回异常情况
                            sendResult = Result<string>.Failure(1204, "[DataSender] Packet中没有可发送的数据存在.");
                        }
                    }
                }
                else
                {
                    string errStr = "[DataSender] USB设备不可用, 无法发送数据.";
                    Logger.Error(errStr);
                    sendResult = Result<string>.Failure(1205, errStr);
                }
            }
            catch (Exception ex)
            {
                string errStr = $"[DataSender] 发生预期外异常: {ex.Message}";
                Logger.Error(errStr);
                sendResult = Result<string>.Failure(1206, errStr);
            }
            finally
            {
                Logger.Info("[DataSender] --------------S End----------------------");
            }
            return sendResult;
        }

        public bool TryReadAckPacket(int timeoutMs, out Packet ackPacket)
        {
            // 1) 记录开始时间，用于超时判断
            var startTime = DateTime.UtcNow;

            // 2) 准备一个循环，不断尝试读取数据
            //    直到超时，或者解析到正确的ACK包
            while (true)
            {

                Packet maybeAck = null;

                // 2.1) 是否已经超时？
                double elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                if (elapsedMs > timeoutMs)
                {
                    // 已到超时，读取失败
                    ackPacket = null;
                    return false;
                }

                // 2.2) 调用对拷线的同步读取方法
                byte[] buffer = new byte[Constants.PACKET_MAX_SIZE * 5];     // 缓冲
                Array.Clear(buffer, 0, buffer.Length);

                int bytesRead = _usbCopyline.ReadDataFromDevice(buffer);
                if (bytesRead == 0)
                {
                    Logger.Info($"没有从设备中读取到数据......等待后再次读取");
                }
                else if (bytesRead < Constants.PACKET_MIN_SIZE || bytesRead > Constants.PACKET_MAX_SIZE)
                {
                    Logger.Error($"读取到的数据不符合预期，读取到的数据长度[{bytesRead}]过大或国小，返回失败。");
                }
                else if (bytesRead > 0)
                {
                    try
                    {
                        maybeAck = Packet.FromBytes(buffer);
                        if (maybeAck.Type == EPacketType.CMD_ACK)
                        {
                            // 解析成功
                            ackPacket = maybeAck;
                            return true;
                        }
                    }
                    catch( Exception ex)
                    {
                        Logger.Error($"包解析中发生错误：{ex.Message}。");
                        ackPacket = null;
                        return false;
                    }
                }
                // 2.4) 若没读到、或不是ACK，则稍微歇一下再重试
                Thread.Sleep(100);
            }
        }
    }
}
