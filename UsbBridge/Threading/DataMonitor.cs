using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataMonitor
    {
        private readonly PlUsbBridgeManager _manager;
        private readonly CancellationToken _token;
        // 具体的对拷线控制实例
        private readonly IUsbCopyLine _usbCopyLine;

        public DataMonitor(PlUsbBridgeManager manager, CancellationToken token, 
                           IUsbCopyLine usbCopyLine)
        {
            _manager = manager;
            _token = token;
            _usbCopyLine = usbCopyLine;
        }

        public Task RunAsync()
        {
            return Task.Run(() => RunLoop(), _token);
        }

        private void RunLoop()
        {
            Console.WriteLine("[DataMonitor] 开始监控线程循环...");
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // 获取监控线程开始许可
                    PlUsbBridgeManager._monitorSemaphore.Wait(_token);
                    Console.WriteLine("[DataMonitor] 开始监控状态...");

                    // 获取对拷线状态
                    CopyLineStatus status = _usbCopyLine.ReadCopyLineActiveStatus();
                    if (status.Usable == ECopyLineUsable.OK) {
                        USBMode mode = _manager.GetCurrentMode();
                        Console.WriteLine($"[DataMonitor] 当前USB模式：{mode}.");
                        // 模拟监控耗时
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Console.WriteLine($"[DataMonitor] USB设备不可用，无法监控其状态。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[DataMonitor] 任务收到取消信号.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataMonitor] 发生预期外异常: {ex.Message}.");
                }
                finally
                {
                    // 唤醒发送线程
                    Console.WriteLine("[DataMonitor] 资源清理完毕.");
                    PlUsbBridgeManager._senderSemaphore.Release();
                    Thread.Sleep(1000); // 释放 CPU
                }
            }
        }
    }
}
