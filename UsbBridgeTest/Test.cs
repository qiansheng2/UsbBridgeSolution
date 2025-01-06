using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridgeTest
{
    internal class Test
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== USB Bridge Test Program ===");

                // 创建并启动桥接
                using (IUsbBridge bridge = new PlUsbBridge())
                {
                    bridge.Start();
                    Console.WriteLine("[Main] Bridge 已启动.");

                    // 显示当前USB运行模式
                    Console.WriteLine($"[Main] 当前USB运行模式为: [{bridge.GetCurrentMode()}].");

                    // 发送一些测试数据
                    byte[] dummyData = { 0x01, 0x02, 0x03, 0x04, 0x05 };
                    try
                    {
                        // 等待 SendBigData 完成并获取返回值
                        Result<string> result = bridge.SendBigData(EPacketOwner.OUTERNET, dummyData);

                        // 判断返回结果
                        if (result.IsSuccess)
                        {
                            Console.WriteLine($"[Main] SendBigData 执行成功，返回数据: {result.Data}");
                        }
                        else
                        {
                            Console.WriteLine($"[Main] SendBigData 执行失败，错误信息: [{result.ErrorCode}] {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 捕获异常
                        Console.WriteLine($"[Main] SendBigData 执行时发生异常: {ex.Message}");
                    }

                    // 等待若干秒，让接收 & 监控任务输出一些日志
                    await Task.Delay(40000);

                    // 切换模式
                    USBMode mode = new USBMode(EUSBPosition.OUTSIDE, EUSBDirection.UPLOAD);
                    bridge.SetMode(mode);
                    Console.WriteLine($"[Main] 模式已切换为: [{mode}].");

                    // 再等待一段时间
                    await Task.Delay(40000);

                    // 主程序结束前，停止桥接
                    Console.WriteLine("[Main] 即将停止桥接...");
                }

                Console.WriteLine("[Main] 已退出,按任意键结束.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Main] Main程序中发生致命错误，退出......");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}