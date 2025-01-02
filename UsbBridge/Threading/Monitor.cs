using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class Monitor
    {
        private readonly PlUsbBridgeManager _manager;
        private readonly CancellationToken _token;
        // 具体的对拷线控制实例
        private readonly IUsbCopyline _usbCopyline;

        public Monitor(PlUsbBridgeManager manager, CancellationToken token, 
                           IUsbCopyline usbCopyline)
        {
            _manager = manager;
            _token = token;
            _usbCopyline = usbCopyline;
        }

        public Task RunAsync()
        {
            return Task.Run(() => RunLoop(), _token);
        }

        private void RunLoop()
        {
            Console.WriteLine("[Monitor] 开始监控线程循环...");
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // 获取监控线程开始许可
                    PlUsbBridgeManager._monitorSemaphore.Wait(_token);
                    Console.WriteLine("[Monitor] 开始监控状态...");

                    // 获取并保存对拷线最新状态
                    CopylineStatus status = _usbCopyline.ReadCopylineStatus(true);
                    _usbCopyline.SetCopylineStatus(status);

                    if (status.Usable == ECopylineUsable.OK) {
                        USBMode mode = _manager.GetCurrentMode();
                        Console.WriteLine($"[Monitor] 当前USB模式：{mode}.");
                        // 模拟监控耗时
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Console.WriteLine($"[Monitor] USB设备不可用，无法监控其状态。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[Monitor] 任务收到取消信号.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Monitor] 发生预期外异常: {ex.Message}.");
                }
                finally
                {
                    // 唤醒发送线程
                    Console.WriteLine("[Monitor] 资源清理完毕.");
                    PlUsbBridgeManager._senderSemaphore.Release();
                    Thread.Sleep(Constants.THREAD_SWITCH_SLEEP_TIME); // 释放 CPU
                }
            }
        }
    }
}
