using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using System.Runtime.Remoting.Contexts;

namespace Isc.Yft.UsbBridge.Devices
{
    internal class Pl25A1UsbCopyLine : PIUsbCopyLine
    {

        // ========== [2] 内部字段 ==========

        // 示例 VendorID / ProductID (需要根据实际对拷线确认)
        private const string USB_NAME = "PL25A1";
        private const ushort USB_VID = 0x067B;
        private const ushort USB_PID = 0x25A1;

        // 根据设备描述符，EP2 OUT = 0x02, EP1 IN = 0x81
        private const byte BULK_OUT_ENDPOINT = 0x02;
        private const byte BULK_IN_ENDPOINT = 0x81;
        private const uint TIMEOUT_MS = 3000;

        // ========== [3] 实现 IUsbCopyLine 接口 ==========
        public override bool OpenDevice()
        {
            _deviceHandle = libusb_open_device_with_vid_pid(_context, USB_VID, USB_PID);
            if (_deviceHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[{USB_NAME}] 打开设备失败.");
                return false;
            }
            Console.WriteLine($"[{USB_NAME}] 已打开设备.");
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






