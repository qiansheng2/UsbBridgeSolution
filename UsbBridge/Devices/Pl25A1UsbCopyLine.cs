using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using System.Runtime.Remoting.Contexts;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;

namespace Isc.Yft.UsbBridge.Devices
{
    internal class Pl25A1UsbCopyLine : PIUsbCopyLine
    {

        // ========== [2] 内部字段 ==========

        // 示例 VendorID / ProductID (需要根据实际对拷线确认)
        protected override string USB_NAME { get; set; } = "PL25A1";
        protected override ushort USB_VID { get; set; } = 0x067B;
        protected override ushort USB_PID { get; set; } = 0x25A1;

        // 根据设备描述符，EP2 OUT = 0x02, EP1 IN = 0x81
        private const byte BULK_OUT_ENDPOINT = 0x02;
        private const byte BULK_IN_ENDPOINT = 0x81;
        private const uint TIMEOUT_MS = 3000;

        // ========== [3] 实现 IUsbCopyLine 接口 ==========
        public bool OpenDevice2222()
        {
            _deviceHandle = libusb_open_device_with_vid_pid(_context, USB_VID, USB_PID);
            if (_deviceHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[{USB_NAME}] 打开设备失败.");
                return false;
            }
            Console.WriteLine($"[{USB_NAME}] 已打开设备.");

            IntPtr configDescPtr;
            int ret = libusb_get_active_config_descriptor(_deviceHandle, out configDescPtr);     //获取活动配置描述
            if (ret != 0)
            {
                Console.WriteLine($"[{USB_NAME}] 获取硬件信息描述失败.");
                return false;
            }
            Console.WriteLine($"[{USB_NAME}] 已获取硬件信息描述.");

            // 将描述符指针转换为托管结构体
            SLibusbConfigDescriptor configDesc = Marshal.PtrToStructure<SLibusbConfigDescriptor>(configDescPtr);
            Console.WriteLine($"Number of Interfaces: {configDesc.bNumInterfaces}");
            Console.WriteLine($"Total Length: {configDesc.wTotalLength}");
            Console.WriteLine($"Max Power: {configDesc.MaxPower}");

            // 声明usb设备的端口
            ret = libusb_claim_interface(_deviceHandle, 0);
            if (ret != 0)
            {
                Console.WriteLine($"[{USB_NAME}] 声明硬件接口失败.");
                return false;
            }
            // 检查本地设备和远程设备的各种状态
            byte[] devStatusBuffer = new byte[16];
            ret = libusb_control_transfer(_deviceHandle, 0xC0, 0xF1, 0, 0, devStatusBuffer, 2, 500);
            if (ret != 0)
            {
                Console.WriteLine("[{USB_NAME}] 获取设备状态失败.");
            }

            // 将字节数组转换为结构体
            SDEV_STATUS devStatus = CommonUtil.ByteArrayToStructure<SDEV_STATUS>(devStatusBuffer);

            // 打印设备状态
            Console.WriteLine($"Local device status: {(devStatus.localSuspend ? "Suspend" : "Active")}, " +
                              $"{(devStatus.localAttached ? "Attached" : "Unplug")}, " +
                              $"{(devStatus.localSpeed ? "Super speed" : "High speed")}");
            Console.WriteLine($"Remote device status: {(devStatus.remoteSuspend ? "Suspend" : "Active")}, " +
                              $"{(devStatus.remoteAttached ? "Attached" : "Unplug")}, " +
                              $"{(devStatus.remoteSpeed ? "Super speed" : "High speed")}");

            // 释放描述符内存
            libusb_free_config_descriptor(configDescPtr);
            Console.WriteLine("Configuration descriptor memory freed.");

            return true;
        }

        public override int WriteDataToDevice(byte[] data)
        {
            if (_deviceHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[{USB_NAME}] 设备尚未打开，无法写入数据.");
                return 0;
            }

            int ret = libusb_bulk_transfer(
                _deviceHandle,
                BULK_OUT_ENDPOINT,
                data,
                data.Length,
                out int transferred,
                TIMEOUT_MS);

            if (ret < 0)
            {
                Console.WriteLine($"[{USB_NAME}] 写数据失败，libusb_bulk_transfer 返回: {ret}");
                return 0;
            }

            Console.WriteLine($"[{USB_NAME}] 已写入 {transferred} 字节.");
            return transferred;
        }

        public override int ReadDataFromDevice(byte[] buffer)
        {
            if (_deviceHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[{USB_NAME}] 设备尚未打开，无法读取数据.");
                return 0;
            }

            int ret = libusb_bulk_transfer(
                _deviceHandle,
                BULK_IN_ENDPOINT,
                buffer,
                buffer.Length,
                out int transferred,
                TIMEOUT_MS);

            if (ret < 0)
            {
                Console.WriteLine($"[{USB_NAME}] 读数据失败，libusb_bulk_transfer 返回: {ret}");
                return 0;
            }

            Console.WriteLine($"[{USB_NAME}] 已读取 {transferred} 字节.");
            return transferred;
        }
    }
}






