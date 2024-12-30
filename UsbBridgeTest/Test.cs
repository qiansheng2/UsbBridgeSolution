using System;
using System.Threading;
using Isc.Yft.UsbBridge;
using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;

namespace Isc.Yft.UsbBridgeTest
{
    internal class Test
    {
        static void Main(string[] args)
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
                bridge.SendBigData(PacketOwner.OuterNet, dummyData);

                // 等待若干秒，让接收 & 监控 任务输出一些日志
                Thread.Sleep(8000);

                // 切换模式
                bridge.SetMode(USBMode.DOWNLOAD);
                Console.WriteLine($"[Main] 模式已切换为: [{USBMode.DOWNLOAD}].");

                // 再等待一段时间
                Thread.Sleep(8000);

                // 主程序结束前，停止桥接
                Console.WriteLine("[Main] 即将停止桥接...");
            }

            Console.WriteLine("[Main] 已退出,按任意键结束.");
            Console.ReadKey();
        }
    }
}
