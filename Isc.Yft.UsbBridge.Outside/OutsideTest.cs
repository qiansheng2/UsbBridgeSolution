using System;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Exceptions;

namespace Isc.Yft.UsbBridge.Outside
{
    internal class OutsideTest
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        static async Task Main()
        {
            try
            {
                Result<string> result = new Result<string>();

                Logger.Info("=== USB Bridge === 外网端 ===");

                // 创建并启动桥接
                using (IUsbBridge bridge = new PlUsbBridge(new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD)))
                {
                    bridge.Start();
                    Logger.Info("[Main] Bridge 已启动.");

                    try
                    {

                        // 等待 SendCommand 完成并获取返回值
                        result = await bridge.SendCommand("dir");

                        // 判断返回结果
                        if (result.IsSuccess)
                        {
                            Logger.Info($"[Main] SendCommand 执行成功，返回数据: {result.Data}");
                        }
                        else
                        {
                            Logger.Warn($"[Main] SendCommand 执行失败，错误信息: [{result.ErrorCode}] {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获异常
                        Logger.Error($"[Main] SendCommand 执行时发生异常: {ex.Message}");
                    }

                    // 再等待一段时间
                    await Task.Delay(4000000);

                    // 发送一些测试数据
                    byte[] dummyData = { 0x01, 0x02, 0x03, 0x04, 0x05 };
                    try
                    {
                        // 等待 SendBigData 完成并获取返回值
                        result = await bridge.SendBigData(EPacketOwner.OUTERNET, dummyData);

                        // 判断返回结果
                        if (result.IsSuccess)
                        {
                            Logger.Info($"[Main] SendBigData 执行成功，返回数据: {result.Data}");
                        }
                        else
                        {
                            Logger.Warn($"[Main] SendBigData 执行失败，错误信息: [{result.ErrorCode}] {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获异常
                        Logger.Error($"[Main] SendBigData 执行时发生异常: {ex.Message}");
                    }

                    // 等待若干秒，让接收 & 监控任务输出一些日志
                    await Task.Delay(4000);

                    // 切换模式
                    USBMode mode = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);
                    bridge.CurrentMode = mode;
                    Logger.Info($"[Main] 模式已切换为: [{mode}].");

                    for (int i = 0; i < 100; i++)
                    {
                        Logger.Info($"发送第{i + 1}次数据...");
                        // 发送一些测试数据
                        byte[] dummyData2 = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
                        try
                        {
                            // 等待 SendBigData 完成并获取返回值
                            result = await bridge.SendBigData(EPacketOwner.OUTERNET, dummyData);

                            // 判断返回结果
                            if (result.IsSuccess)
                            {
                                Logger.Info($"[Main] SendBigData 执行成功，返回数据: {result.Data}");
                            }
                            else
                            {
                                Logger.Warn($"[Main] SendBigData 执行失败，错误信息: [{result.ErrorCode}] {result.ErrorMessage}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 捕获异常
                            Logger.Error($"[Main] SendBigData 执行时发生异常: {ex.Message}");
                        }
                    }

                    // 再等待一段时间
                    await Task.Delay(4000);

                    try
                    {

                        // 等待 SendCommand 完成并获取返回值
                        result = await bridge.SendCommand("dir");

                        // 判断返回结果
                        if (result.IsSuccess)
                        {
                            Logger.Info($"[Main] SendCommand 执行成功，返回数据: {result.Data}");
                        }
                        else
                        {
                            Logger.Warn($"[Main] SendCommand 执行失败，错误信息: [{result.ErrorCode}] {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获异常
                        Logger.Error($"[Main] SendCommand 执行时发生异常: {ex.Message}");
                    }

                    // 再等待一段时间
                    await Task.Delay(40000);

                    // 主程序结束前，停止桥接
                    Logger.Info("[Main] 即将停止桥接...");
                }

                Logger.Info("[Main] 已退出,按任意键结束.");
                Console.ReadKey();
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
