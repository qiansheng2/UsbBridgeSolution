using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Exceptions;
using Isc.Yft.UsbBridge.Handler;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataReceiver
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly CancellationToken _token;

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
                        if ( readCount == 0)
                        {
                            Logger.Info($"[DataReceiver] 没有从设备中读取到数据。");
                        }
                        else if (readCount < Constants.PACKET_MIN_SIZE || readCount > Constants.PACKET_MAX_SIZE) {
                            throw new InvalidCastException($"[DataReceiver] 读取到的数据不符合预期，数据长度[{readCount}] > 1024，直接抛弃。");
                        }
                        else if(readCount > 0)
                        {
                            Logger.Info($"[DataReceiver] 接收到 {readCount} 字节: {BitConverter.ToString(buffer, 0, readCount)}");

                            // 4) 进行解析: 将 raw bytes 转换为 Packet(s)
                            try
                            {
                                Packet packet = TryParsePacket(buffer, readCount);
                                if (packet == null)
                                {
                                    Logger.Warn("[DataReceiver] 无法解析为Packet, 忽略或等待更多数据");
                                }
                                else
                                {
                                    Logger.Info($"[DataReceiver] 解析到包: Type={packet.Type}, Index={packet.Index}/{packet.TotalCount}, Length={packet.ContentLength}");
                                    IPacketHandler handler = PacketHandlerFactory.GetHandler(packet.Type);
                                    if (handler != null)
                                    {
                                        try
                                        {
                                            await handler.Handle(packet); // 调用对应的处理方法
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Error($"[DataReceiver] 数据包处理过程中出错 {packet.Type}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error($"[DataReceiver] 未知的包类型: {packet.Type}");
                                    }
                                }
                            }
                            catch (Exception parseEx)
                            {
                                Logger.Error($"[DataReceiver] 解析数据包时发生异常: {parseEx.Message}");
                            }
                        }
                        else
                        {
                            Logger.Warn($"[DataReceiver] 没有从拷贝线读到任何数据,数据长度:[{readCount}]。");
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
                    Logger.Info("[DataReceiver] -----------------R End-------------------");
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
            byte[] flushBuf = new byte[1024 * 10];
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
            // readCount=1024 正好是一个Packet
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

    }
}
