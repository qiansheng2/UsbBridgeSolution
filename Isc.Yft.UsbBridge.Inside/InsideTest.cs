using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Exceptions;

namespace Isc.Yft.UsbBridge.Inside
{
    internal class InsideTest
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static async Task Main()
        {
            try
            {
                Result<string> result = new Result<string>();

                Logger.Info("=== USB Bridge === 内网端 ===");

                // 创建并启动桥接
                USBMode usbMode = new USBMode(EUSBPosition.INSIDE, EUSBDirection.UPLOAD);
                using (IUsbBridge bridge = new PlUsbBridge(usbMode))
                {
                    bridge.Start();
                    Logger.Info($"[Main] 桥接已启动...{bridge.CurrentMode}");

                    // 等待一段时间
                    await Task.Delay(4000000);
                    // 主程序结束前，停止桥接
                    Logger.Info("[Main] 停止桥接...");
                }
            }
            catch (InvalidHardwareException ex)
            {
                Logger.Error($"[Main] USB硬件通讯中发生致命错误，退出...{ex.Message}");
            }
            catch (CopylineNotFoundException ex)
            {
                Logger.Error($"[Main] USB设备硬件未找到，退出...{ex.Message}");
            }
            catch (PacketMismatchException ex)
            {
                Logger.Error($"[Main] USB通讯中，发生数据包匹配错误，退出...{ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Main] Main程序中发生致命错误，退出...{ex.Message}");
            }
        }
    }
}
