using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataReceiver
    {
        private readonly CancellationToken _token;

        // 具体的对拷线控制实例
        private readonly IUsbCopyLine _usbCopyLine;

        public DataReceiver(CancellationToken token, IUsbCopyLine usbCopyLine)
        {
            _token = token;
            _usbCopyLine = usbCopyLine;
        }

        public Task RunAsync()
        {
            return Task.Run(() => RunLoop(), _token);
        }

        private void RunLoop()
        {
            Console.WriteLine("[DataReceiver] 开始接收线程循环...");
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // 获取读取线程开始许可
                    PlUsbBridgeManager._receiverSemaphore.Wait(_token);
                    Console.WriteLine("[DataReceiver] 开始接收数据...");

                    CopyLineStatus status = _usbCopyLine.ReadCopyLineActiveStatus();
                    if (status.Usable == ECopyLineUsable.OK)
                    {
                        byte[] buffer = new byte[1024 * 1000]; // 读缓冲区, 初始化为1MB
                                                               // 调用对拷线的 ReadDataFromDevice
                        int readCount = _usbCopyLine.ReadDataFromDevice(buffer);
                        if (readCount > 0)
                        {
                            Console.WriteLine($"[DataReceiver] 接收到 {readCount} 字节: {BitConverter.ToString(buffer, 0, readCount)}");
                            // 这里可进一步把数据交给上层或做处理
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DataReceiver] USB设备不可用，无法接收数据。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[DataReceiver] 任务收到取消信号.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataReceiver] 发生预期外异常: {ex.Message}.");
                }
                finally
                {
                    // 唤醒监控线程
                    Console.WriteLine("[DataReceiver] 资源清理完毕.");
                    PlUsbBridgeManager._monitorSemaphore.Release();
                    Thread.Sleep(1000); // 释放 CPU
                }
            }
        }
    }
}
