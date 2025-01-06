using System;
using System.Runtime.InteropServices;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Exceptions;

public sealed class LibusbDeviceSafeHandle : SafeHandle
{
    /// <summary>
    /// 私有构造函数，设置初始句柄为 IntPtr.Zero，并表明需要释放资源 (ownsHandle=true)
    /// </summary>
    private LibusbDeviceSafeHandle() : base(IntPtr.Zero, true)
    {
    }

    /// <summary>
    /// 判断当前句柄是否无效
    /// </summary>
    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero; }
    }

    /// <summary>
    /// 实际释放设备指针时，调用 libusb_close() 关闭设备
    /// </summary>
    /// <returns></returns>
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            // 调用 libusb_close(deviceHandle)
            LibusbInterop.libusb_close(handle);
        }
        return true;
    }

    /// <summary>
    /// 工厂方法: 通过 libusb_open_device_with_vid_pid() 打开指定 VID/PID 的设备并得到安全包装
    /// </summary>
    /// <param name="libUsbContext">已初始化好的 libusb 上下文指针 (可来自你的 LibUsbContextSafeHandle)</param>
    /// <param name="vendorId">设备的 VID</param>
    /// <param name="productId">设备的 PID</param>
    /// <returns>包装后的 SafeHandle 对象</returns>
    /// <exception cref="Exception">当打开设备失败时抛出异常</exception>
    public static LibusbDeviceSafeHandle OpenDevice(IntPtr libUsbContext, ushort vendorId, ushort productId)
    {
        // 调用 libusb_open_device_with_vid_pid()
        IntPtr devicePtr = LibusbInterop.libusb_open_device_with_vid_pid(libUsbContext, vendorId, productId);
        if (devicePtr == IntPtr.Zero)
        {
            throw new CopylineNotFoundException($"无法打开 USB 设备 (VID=0x{vendorId:X4}, PID=0x{productId:X4}).");
        }

        // 将获得的原生指针包装到 SafeHandle
        var safeHandle = new LibusbDeviceSafeHandle();
        safeHandle.SetHandle(devicePtr);
        return safeHandle;
    }

    /// <summary>
    /// 如果你已经从其它地方拿到了 devicePtr (例如事先调用过 libusb_open_device_with_vid_pid)，
    /// 也可使用这个函数直接封装已有指针。注意要确保该指针来自 libusb_open_device_with_vid_pid。
    /// </summary>
    /// <param name="devicePtr">已获取到的设备指针</param>
    /// <returns>SafeHandle</returns>
    public static LibusbDeviceSafeHandle FromExisting(IntPtr devicePtr)
    {
        if (devicePtr == IntPtr.Zero)
        {
            throw new ArgumentException("devicePtr不可为Zero。");
        }

        var safeHandle = new LibusbDeviceSafeHandle();
        safeHandle.SetHandle(devicePtr);
        return safeHandle;
    }
}