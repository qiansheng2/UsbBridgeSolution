using System;
using System.Runtime.InteropServices;

namespace Isc.Yft.UsbBridge.Models
{
    public static class LibusbInterop
    {
        // ========== 导入 libusb-1.0 的函数  ==========

        /// <summary>
        /// 初始化 libusb 库，并创建一个 libusb 上下文。
        /// </summary>
        /// <param name="context">初始化后的 libusb 上下文指针。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_init(out IntPtr context);

        /// <summary>
        /// 释放 libusb 库，并销毁相关的 libusb 上下文。
        /// </summary>
        /// <param name="context">要释放的 libusb 上下文指针。</param>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libusb_exit(IntPtr context);

        /// <summary>
        /// 获取当前 libusb 上下文中的设备列表。
        /// </summary>
        /// <param name="ctx">libusb 上下文指针。</param>
        /// <param name="list">设备列表的指针数组。</param>
        /// <returns>返回设备数量（包括所有设备），负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_device_list(IntPtr ctx, out IntPtr list);

        /// <summary>
        /// 获取指定 USB 设备的设备描述符。
        /// </summary>
        /// <param name="device">目标 USB 设备的指针。</param>
        /// <param name="descriptor">设备描述符结构体的输出参数。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_device_descriptor(IntPtr device, out SLibusbDeviceDescriptor descriptor);

        /// <summary>
        /// 根据供应商 ID 和产品 ID 打开指定的 USB 设备。
        /// </summary>
        /// <param name="context">libusb 上下文指针。</param>
        /// <param name="vendor_id">目标设备的供应商 ID（VID）。</param>
        /// <param name="product_id">目标设备的产品 ID（PID）。</param>
        /// <returns>返回设备句柄指针，如果失败则返回 IntPtr.Zero。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libusb_open_device_with_vid_pid(IntPtr context, ushort vendor_id, ushort product_id);

        /// <summary>
        /// 关闭之前打开的 USB 设备句柄。
        /// </summary>
        /// <param name="deviceHandle">要关闭的设备句柄指针。</param>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libusb_close(IntPtr deviceHandle);

        /// <summary>
        /// 获取 USB 设备的端口号。
        /// </summary>
        /// <param name="device">目标 USB 设备的指针。</param>
        /// <returns>返回设备所在的端口号，如果设备未连接到端口则返回 0。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern byte libusb_get_port_number(IntPtr device);

        /// <summary>
        /// 获取 USB 设备的所有端口号。
        /// </summary>
        /// <param name="dev">目标 USB 设备的指针。</param>
        /// <param name="portNumbers">用于接收端口号的字节数组。</param>
        /// <param name="portNumbersLen">端口号数组的长度。</param>
        /// <returns>返回实际获取到的端口号数量，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_port_numbers(IntPtr dev, [Out] byte[] portNumbers, int portNumbersLen);

        /// <summary>
        /// 打开指定的 USB 设备并获取其设备句柄。
        /// </summary>
        /// <param name="dev">目标 USB 设备的指针。</param>
        /// <param name="dev_handle">输出参数，返回设备句柄指针。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_open(IntPtr dev, out IntPtr dev_handle);

        /// <summary>
        /// 释放之前获取的 USB 设备列表，并选择是否取消对设备的引用。
        /// </summary>
        /// <param name="list">设备列表的指针数组。</param>
        /// <param name="unrefDevices">如果设置为非零值，则取消对设备的引用。</param>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libusb_free_device_list(IntPtr list, int unrefDevices);

        /// <summary>
        /// 获取设备配置描述
        /// </summary>
        /// <param name="dev">设备</param>
        /// <param name="config">标准设备配置描述</param>
        /// <returns>0：成功 非0：失败</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_active_config_descriptor(IntPtr dev, out IntPtr config);
        //int libusb_get_active_config_descriptor(libusb_device * dev,struct libusb_config_descriptor ** config )	

        /// <summary>
        /// 释放设备配置描述
        /// </summary>
        /// <param name="dev">设备</param>
        /// <param name="config">标准设备配置描述</param>
        /// <returns>0：成功 非0：失败</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libusb_free_config_descriptor(IntPtr config_desc);

        /// <summary>
        /// 声明接口
        /// </summary>
        /// <param name="dev_handle">设备句柄</param>
        /// <param name="interface_number">设备接口号</param>
        /// <returns>0：成功 非0：失败</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_claim_interface(IntPtr dev_handle, int interface_number);
        //int libusb_claim_interface(libusb_device_handle * dev_handle,int interface_number)	

        /// <summary>
        /// 释放接口声明
        /// </summary>
        /// <param name="dev_handle">设备句柄</param>
        /// <param name="interface_number">接口地址</param>
        /// <returns>0：成功 非0：失败</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_release_interface(IntPtr dev_handle, int interface_number);

        /// <summary>
        /// 执行指定 USB 设备和端点的控制传输。
        /// </summary>
        /// <param name="dev_handle">目标 USB 设备的句柄。</param>
        /// <param name="bmRequestType">请求类型位图，定义传输方向、请求类型和接收端点。</param>
        /// <param name="bRequest">请求代码，指定具体的控制请求。</param>
        /// <param name="wValue">请求值，取决于具体的请求。</param>
        /// <param name="wIndex">请求索引，通常用于指定接口或端点。</param>
        /// <param name="data">用于发送或接收的数据缓冲区。</param>
        /// <param name="wLength">数据缓冲区的长度（字节数）。</param>
        /// <param name="timeout">传输超时时间（以毫秒为单位）。</param>
        /// <returns>
        /// 返回实际传输的字节数，如果出错则返回负值的错误代码。
        /// </returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_control_transfer(IntPtr dev_handle, byte bmRequestType, byte bRequest, ushort wValue, ushort wIndex,
                                                         byte[] data, ushort wLength, int timeout);

        /// <summary>
        /// 执行指定 USB 设备和端点的批量传输。
        /// </summary>
        /// <param name="deviceHandle">目标 USB 设备的句柄。</param>
        /// <param name="endpoint">端点地址（必须是批量端点）。</param>
        /// <param name="data">用于发送或接收的数据缓冲区。</param>
        /// <param name="length">数据缓冲区的长度（字节数）。</param>
        /// <param name="transferred">实际传输的字节数。</param>
        /// <param name="timeout">传输超时时间（以毫秒为单位）。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_bulk_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, out int transferred, uint timeout);

        /// <summary>
        /// 获取错误信息
        /// </summary>
        /// <param name="dev_handle"></param>
        /// <returns>错误信息</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libusb_error_name(int errorCode);

        /// <summary>
        /// 获取设备句柄的基础设备
        /// </summary>
        /// <param name="dev_handle"></param>
        /// <returns>错误信息</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr libusb_get_device(IntPtr devHandle);
    }
}
