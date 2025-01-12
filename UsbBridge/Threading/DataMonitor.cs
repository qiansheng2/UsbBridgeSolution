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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly PlUsbBridgeManager _manager;
        private readonly CancellationToken _token;
  
        // 具体的对拷线控制实例
        private readonly ICopyline _usbCopyline;

        // 当监控出现致命错误时触发
        public event EventHandler<InvalidHardwareException> FatalErrorOccurred;

        public DataMonitor(PlUsbBridgeManager manager, CancellationToken token, 
                           ICopyline usbCopyline)
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
                    // 获得互斥锁，确保同一时间仅一个线程在执行
                    await PlUsbBridgeManager._oneThreadAtATime.WaitAsync(_token);
                    if (_token.IsCancellationRequested)
                    {
                        Logger.Info("取消信号在拿到锁后立即生效，不执行任何业务操作。");
                        break;
                    }

                    Logger.Info("[DataMonitor] -----------------M Start-----------------");

                    // 获取拷贝线的最新状态信息
                    _usbCopyline.OpenCopyline();
                    _usbCopyline.UpdateCopylineStatus();
                    if (_usbCopyline.Status.RealtimeStatus == ECopylineStatus.ONLINE) {
                        Logger.Info($"[DataMonitor] 当前USB模式：{_manager.CurrentMode}.");
                    }
                    else
                    {
                        Logger.Info($"[DataMonitor] USB设备不可用，无法监控其状态。");
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Info("[DataMonitor] 任务收到取消信号.");
                    break;
                }
                catch (InvalidHardwareException hex)
                {
                    Logger.Fatal($"[DataMonitor] 发生致命错误，监控线程退出...{hex.Message}");
                    // 触发事件，通知外部
                    FatalErrorOccurred?.Invoke(this, hex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[DataMonitor] 发生非致命异常: {ex.Message}");
                    // 根据需要可决定是否 break 或继续循环
                }
                finally
                {
                    // 释放互斥锁 + 稍作延时，避免空转
                    Logger.Info("[DataMonitor] -----------------M End-------------------");
                    PlUsbBridgeManager._oneThreadAtATime.Release();
                    await Task.Delay(Constants.THREAD_SWITCH_SLEEP_TIME, _token);
                }
            }
        }
    }
}
