using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;

namespace Isc.Yft.UsbBridge.Devices
{
    internal abstract class PIUsbCopyLine : IUsbCopyLine
    {
        // ========== [1] 导入 libusb-1.0 的函数  ==========
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int libusb_init(out IntPtr context);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void libusb_exit(IntPtr context);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern IntPtr libusb_open_device_with_vid_pid(
            IntPtr context,
            ushort vendor_id,
            ushort product_id);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void libusb_close(IntPtr deviceHandle);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int libusb_bulk_transfer(
            IntPtr deviceHandle,
            byte endpoint,
            byte[] data,
            int length,
            out int transferred,
            uint timeout);

        // ========== [2] 内部字段 ==========
        protected IntPtr _context = IntPtr.Zero;
        protected IntPtr _deviceHandle = IntPtr.Zero;

        // ========== [3] 实现 IUsbCopyLine 接口 ==========
        public void Initialize()
        {
            int ret = libusb_init(out _context);
            if (ret < 0)
            {
                throw new Exception($"libusb_init failed with error code: {ret}");
            }
            Console.WriteLine($"[USB] libusb_init 成功, _context:0x{_context.ToInt64():X}");
        }

        public abstract bool OpenDevice();

        public abstract int WriteDataToDevice(byte[] data);

        public abstract int ReadDataFromDevice(byte[] buffer);

        public void CloseDevice()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                libusb_close(_deviceHandle);
                _deviceHandle = IntPtr.Zero;
                Console.WriteLine("[USB] 已关闭 USB 设备.");
            }
        }

        public void Exit()
        {
            if (_context != IntPtr.Zero)
            {
                libusb_exit(_context);
                _context = IntPtr.Zero;
                Console.WriteLine("[USB] libusb_exit 已完成.");
            }
        }

        public void Dispose()
        {
            // Dispose中做最终清理
            CloseDevice();
            Exit();
        }
    }
}
