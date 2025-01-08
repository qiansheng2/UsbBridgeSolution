﻿using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Exceptions;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataReceiver
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly CancellationToken _token;

        // 定义事件，用于通知其他线程接收到的 ACK
        public event Action<Packet> AckReceived;

        // 具体的对拷线控制实例
        private readonly ICopyline _usbCopyline;

        // 当监控出现致命错误时触发
        public event EventHandler<InvalidHardwareException> FatalErrorOccurred;

        public DataReceiver(SendRequest sendRequest, CancellationToken token, ICopyline usbCopyline)
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
                    Logger.Info("[DataReceiver] -----------------R Start-----------------");
                    if (_token.IsCancellationRequested)
                    {
                        Logger.Info("取消信号在拿到锁后立即生效，不执行任何业务操作。");
                        break;
                    }

                    // 2) 检查USB对拷线状态
                    _usbCopyline.OpenCopyline();
                    _usbCopyline.UpdateCopylineStatus();
                    if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.ONLINE)
                    {
                        // 3) 正式读数据
                        byte[] buffer = new byte[Constants.PACKET_MAX_SIZE]; // 缓冲
                        Array.Clear(buffer, 0, buffer.Length); // 将 buffer 的所有元素设置为 0x00
                        readCount = _usbCopyline.ReadDataFromDevice(buffer); // 调用对拷线的 ReadDataFromDevice
                        if (readCount < Constants.PACKET_MIN_SIZE || readCount > Constants.PACKET_MAX_SIZE) {
                            throw new InvalidCastException($"[DataReceiver] 读取到的数据不符合预期，数据长度[{readCount}] > 1024，直接抛弃。");
                        }
                        else if(readCount > 0)
                        {
                            Logger.Info($"[DataReceiver] 接收到 {readCount} 字节: {BitConverter.ToString(buffer, 0, readCount)}");

                            // 4) 此处可进行解析: 将 raw bytes 转换为 Packet(s)
                            // （示例: 只处理单包; 若有粘包/分包, 需更复杂逻辑）
                            try
                            {
                                Packet packet = TryParsePacket(buffer, readCount);
                                if (packet != null)
                                {
                                    Logger.Info($"[DataReceiver] 解析到包: Type={packet.Type}, Index={packet.Index}/{packet.TotalCount}, Length={packet.ContentLength}");
                                    if (packet.Type == EPacketType.ACK)
                                    {
                                        // 触发事件，通知发送线程
                                        Logger.Info("[DataReceiver] 收到ACK, 即将把Ack包交给发送线程处理。");
                                        AckReceived?.Invoke(packet);
                                        Logger.Info("[DataReceiver] 收到ACK, 即将唤醒发送线程。");
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
                                    Logger.Warn($"[DataReceiver] 无法解析为Packet, 忽略或等待更多数据");
                                }
                            }
                            catch (Exception parseEx)
                            {
                                Logger.Error($"[DataReceiver] 解析数据包时发生异常: {parseEx.Message}");
                            }
                        }
                        else
                        {
                            Logger.Warn($"[DataReceiver] 本轮没有从拷贝线读到数据,数据长度:[{readCount}]。");
                        }
                    }
                    else
                    {
                        Logger.Warn($"[DataReceiver] USB设备不可用，无法接收数据。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Info($"[DataReceiver] 任务收到取消信号.");
                }
                catch (InvalidHardwareException hex)
                {
                    Logger.Fatal($"[DataReceiver] 发生致命错误,接收线程退出...{hex.Message}");
                    // 触发事件，通知外部
                    FatalErrorOccurred?.Invoke(this, hex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DataReceiver] 发生非致命异常: {ex.Message}.");
                }
                finally
                {
                    // 5) 释放互斥锁 + 让出CPU
                    Logger.Warn("[DataReceiver] -----------------R End-------------------");
                    PlUsbBridgeManager._oneThreadAtATime.Release();
                    await Task.Delay(Constants.THREAD_SWITCH_SLEEP_TIME, _token);
                }
            }
            Logger.Info("[DataReceiver] 接收循环结束.");
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
                Logger.Warn($"[DataReceiver] flush操作接收到 {flushCount} 字节, 已抛弃.");
            }
        }

        /// <summary>
        /// 尝试将原始字节解析为一个 Packet
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
            Logger.Info($"[DataReceiver] 收到业务包, Type={packet.Type}, Index={packet.Index}/{packet.TotalCount}");
        }
    }
}
