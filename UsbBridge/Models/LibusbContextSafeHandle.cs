using System;
using System.Runtime.InteropServices;
using Isc.Yft.UsbBridge.Models;

public sealed class LibUsbContextSafeHandle : SafeHandle
{
    /// <summary>
    /// 私有构造函数，设置初始句柄为 IntPtr.Zero，并表明「需要」释放资源 (ownsHandle=true)
    /// </summary>
    private LibUsbContextSafeHandle() : base(IntPtr.Zero, true)
    {
    }

    /// <summary>
    /// 用于判断当前句柄是否无效
    /// </summary>
    public override bool IsInvalid
    {
        get { return handle == IntPtr.Zero; }
    }

    /// <summary>
    /// 释放资源时，自动调用 libusb_exit()
    /// </summary>
    /// <returns>释放成功返回 true</returns>
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            // 调用 libusb_exit(ctx)
            LibusbInterop.libusb_exit(handle);
        }
        return true;
    }

    /// <summary>
    /// 获取安全包装的 libusb Context
    /// </summary>
    /// <exception cref="Exception">当 libusb_init() 返回负值时抛出异常</exception>
    /// <returns>包装后的 SafeHandle</returns>
    public static LibUsbContextSafeHandle Create()
    {
        int ret = LibusbInterop.libusb_init(out IntPtr ctx);
        if (ret < 0)
        {
            throw new Exception($"libusb_init() 失败, 错误码: {LibusbInterop.libusb_error_name(ret)}");
        }

        // 创建SafeHandle实例
        var safeHandle = new LibUsbContextSafeHandle();
        // 将 libusb_init() 得到的上下文指针设置给 SafeHandle
        safeHandle.SetHandle(ctx);
        return safeHandle;
    }
}