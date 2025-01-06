using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Exceptions;
using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Devices;

namespace Isc.Yft.UsbBridge.Threading
{
    internal class DataMonitor
    {
        private readonly PlUsbBridgeManager _manager;
        private readonly CancellationToken _token;
        // 具体的对拷线控制实例
        private readonly IUsbCopyline _usbCopyline;

        public DataMonitor(PlUsbBridgeManager manager, CancellationToken token, 
                           IUsbCopyline usbCopyline)
        {
            _manager = manager;
            _token = token;
            _usbCopyline = usbCopyline;
        }

        public Task RunAsync()
        {
            return RunLoopAsync(); 
        }

        public async Task RunLoopAsync()
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    // 1) 获得互斥锁，确保同一时间仅一个线程在执行
                    await PlUsbBridgeManager._oneThreadAtATime.WaitAsync(_token);
                    Console.WriteLine("[DataMonitor] 获得互斥锁, 开始监控状态...");

                    // 获取拷贝线的硬件信息
                    _usbCopyline.ReadCopylineInfo(true);

                    // 获取并保存对拷线最新状态
                    CopylineStatus status = _usbCopyline.ReadCopylineStatus(true);

                    if (status.Usable == ECopylineUsable.OK) {
                        // 打开usb对拷线设备
                        _usbCopyline.OpenDevice();

                        USBMode mode = _manager.GetCurrentMode();
                        Console.WriteLine($"[DataMonitor] 当前USB模式：{mode}.");
                    }
                    else
                    {
                        Console.WriteLine($"[DataMonitor] USB设备不可用，无法监控其状态。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[DataMonitor] 任务收到取消信号.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataMonitor] 发生预期外异常: {ex.Message}");
                    // 根据需要可决定是否 break 或继续循环
                }
                finally
                {
                    // 3) 释放互斥锁 + 稍作延时，避免空转
                    Console.WriteLine("[DataMonitor] 释放锁, 资源清理完毕.");
                    PlUsbBridgeManager._oneThreadAtATime.Release();
                    await Task.Delay(Constants.THREAD_SWITCH_SLEEP_TIME, _token);
                }
            }
        }
    }
}
