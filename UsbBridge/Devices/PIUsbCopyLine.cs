using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;
using System.Runtime.Remoting.Contexts;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Isc.Yft.UsbBridge.Devices
{
    internal abstract class PIUsbCopyLine : IUsbCopyLine
    {
        // ========== [1] 导入 libusb-1.0 的函数  ==========
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int libusb_init(out IntPtr context);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libusb_get_device_list(IntPtr ctx, out IntPtr list);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_device_descriptor(IntPtr device, out SLibusbDeviceDescriptor descriptor );

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
        public static extern byte libusb_get_port_number(IntPtr device);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libusb_get_port_numbers(IntPtr dev, [Out] byte[] portNumbers, int portNumbersLen);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libusb_open(IntPtr dev, out IntPtr dev_handle);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void libusb_free_device_list(IntPtr list, int unrefDevices);

        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int libusb_bulk_transfer(
            IntPtr deviceHandle,
            byte endpoint,
            byte[] data,
            int length,
            out int transferred,
            uint timeout);

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
        /// 传输控制
        /// </summary>
        /// <param name="dev_handle"></param>
        /// <param name="bmRequestType"></param>
        /// <param name="bRequest"></param>
        /// <param name="wValue"></param>
        /// <param name="wIndex"></param>
        /// <param name="data"></param>
        /// <param name="wLength"></param>
        /// <param name="timeout"></param>
        /// <returns>传输数据</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_control_transfer(IntPtr dev_handle,
                                                    byte bmRequestType,
                                                    byte bRequest,
                                                    ushort wValue,
                                                    ushort wIndex,
                                                    byte[] data,
                                                    ushort wLength,
                                                    int timeout);
        //int libusb_control_transfer(libusb_device_handle* dev_handle,
        //    uint8_t bmRequestType,
        //    uint8_t bRequest,
        //    uint16_t wValue,
        //    uint16_t wIndex,
        //    unsigned char* data,
        //    uint16_t wLength,
        //    unsigned int timeout)

        /// <summary>
        /// 传输控制
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

        // ========== [2] 内部字段 ==========
        protected IntPtr _context = IntPtr.Zero;
        protected IntPtr _deviceList = IntPtr.Zero;
        protected IntPtr _deviceHandle = IntPtr.Zero;
        protected int  _bulk_interface_no = 0;
        protected byte _bulk_in_address = 0x00;
        protected byte _bulk_out_address = 0x00;
        IntPtr copylineConfigPtr = IntPtr.Zero;

        // VendorID / ProductID (需要根据实际对拷线数据覆盖)
        protected virtual string USB_NAME { get; set; } = "";
        protected virtual ushort USB_VID { get; set; } = 0x0000;
        protected virtual ushort USB_PID { get; set; } = 0x0000;


        // ========== [3] 实现 IUsbCopyLine 接口 ==========
        public abstract int WriteDataToDevice(byte[] data);

        public abstract int ReadDataFromDevice(byte[] buffer);

        public void Initialize()
        {
            int ret = libusb_init(out _context);
            if (ret < 0)
            {
                throw new Exception($"[{USB_NAME}]libusb_init 失败: {get_libusb_error_name(ret)}");
            }
            Console.WriteLine($"[{USB_NAME}] libusb_init 成功, _context:0x{_context.ToInt64():X}");
        }

        public bool OpenDevice()
        {
            try
            {
                int result = 0;

                // 获取设备列表
                int deviceCount = libusb_get_device_list(_context, out _deviceList);
                if (deviceCount < 1)
                {
                    throw new InvalidOperationException($"[{USB_NAME}]没有发现USB设备，或libusb_get_device_list 调用失败.");
                }
                else
                    Console.WriteLine($"[{USB_NAME}]发现了{deviceCount}个USB设备.");

                // 获取所需设备
                for (int i = 0; i < deviceCount; i++)
                {
                    // 读取设备指针
                    IntPtr devicePtr = Marshal.ReadIntPtr(_deviceList, i * IntPtr.Size);
                    if (devicePtr == IntPtr.Zero)
                        continue;

                    // 获取设备描述符
                    SLibusbDeviceDescriptor deviceDesc;
                    result = libusb_get_device_descriptor(devicePtr, out deviceDesc);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"[{USB_NAME}]获取设备描述符失败: {get_libusb_error_name(result)}");
                    }
                    // 打印设备信息
                    Console.Write($"VID:{deviceDesc.idVendor:X4},PID:{deviceDesc.idProduct:X4}");

                    // 获取端口路径
                    byte[] path = new byte[8];
                    int portCount = libusb_get_port_numbers(devicePtr, path, path.Length);
                    if (portCount > 0)
                    {
                        Console.WriteLine(" path: " + path[0]);
                        for (int k = 1; k < portCount; k++)
                        {
                            Console.Write($".{path[k]}");
                        }
                    }
                    Console.WriteLine();

                    // 检查是否是目标设备
                    if (deviceDesc.idVendor == USB_VID && deviceDesc.idProduct == USB_PID)
                    {
                        Console.WriteLine("--------------------------------------------------------------");
                        Console.WriteLine($"    {USB_NAME} 设备信息");

                        // 打开设备
                        result = libusb_open(devicePtr, out _deviceHandle);
                        if (result != 0 || _deviceHandle == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}]libusb_open 打开目标USB设备失败: {get_libusb_error_name(result)}");
                        }

                        // 获取配置描述符
                        result = libusb_get_active_config_descriptor(devicePtr, out copylineConfigPtr);
                        if (result != 0 || copylineConfigPtr == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}]libusb_get_active_config_descriptor获取设备描述符信息失败: {get_libusb_error_name(result)}");
                        }
                        SLibusbConfigDescriptor configDesc = Marshal.PtrToStructure<SLibusbConfigDescriptor>(copylineConfigPtr);
                        Console.WriteLine($"    NumInterfaces = {configDesc.bNumInterfaces}");
                        Console.WriteLine($"    Total Length = {configDesc.wTotalLength}");
                        Console.WriteLine($"    Max Power = {configDesc.MaxPower}");

                        // 遍历接口
                        for (int j = 0; j < configDesc.bNumInterfaces; j++)
                        {
                            // 计算接口描述符的指针偏移量
                            // 注意：libusb_interface_descriptor 实际上是 libusb_interface 结构体中的 altsetting 数组
                            // 这里需要更多的 P/Invoke 调用来正确解析接口和端点描述符

                            // 获取接口描述符指针
                            IntPtr interfacePtr = Marshal.ReadIntPtr(configDesc.interface_, j * IntPtr.Size);
                            SLibusbInterfaceDescriptor interfaceDesc = Marshal.PtrToStructure<SLibusbInterfaceDescriptor>(interfacePtr);
                            Console.WriteLine($"    Interface {j}: No. of endpoints = {interfaceDesc.bNumEndpoints}");

                            // 遍历端点
                            for (int k = 0; k < interfaceDesc.bNumEndpoints; k++)
                            {
                                // 获取端点描述符指针
                                IntPtr endpointArrayPtr = interfaceDesc.endpoint;
                                IntPtr endpointPtr = IntPtr.Add(endpointArrayPtr, k * Marshal.SizeOf<SLibusbEndpointDescriptor>());
                                SLibusbEndpointDescriptor endpoint = Marshal.PtrToStructure<SLibusbEndpointDescriptor>(endpointPtr);
                                Console.WriteLine($"        Endpoint {k}: bmAttributes = {endpoint.bmAttributes:X2}");

                                // 检查控制端点Bulk endpoint: LIBUSB_TRANSFER_TYPE_CONTROL = 0
                                if (endpoint.bmAttributes == 0)
                                {
                                    Console.WriteLine($"        Found control endpoint No. {k}");
                                }

                                // 检查批量端点 Bulk endpoint: LIBUSB_TRANSFER_TYPE_BULK = 2
                                if (endpoint.bmAttributes == 2)
                                {
                                    _bulk_interface_no = j;
                                    Console.WriteLine($"        bulk_interface_no = {_bulk_interface_no}");
                                    Console.Write($"        Found bulk endpoint No. {k}");

                                    // 检查方向
                                    if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_IN)
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (IN)");
                                        _bulk_in_address = endpoint.bEndpointAddress;
                                    }
                                    else if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_OUT)
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (OUT)");
                                        _bulk_out_address = endpoint.bEndpointAddress;
                                    }
                                    else
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (Unknown Direction)");
                                    }
                                }
                            }
                        }
                        Console.WriteLine("--------------------------------------------------------------");

                        // 获取设备活动状态
                        // 声明usb设备的端口
                        result = libusb_claim_interface(_deviceHandle, _bulk_interface_no);
                        if (result != 0)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}] 声明硬件接口失败: {get_libusb_error_name(result)}");
                        }
                        // 检查本地设备和远程设备的各种状态
                        byte[] devStatusBuffer = new byte[16];
                        result = libusb_control_transfer(_deviceHandle, 0xC0, 0xF1, 0, 0, devStatusBuffer, 2, 500);
                        if (result != 0)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}] 获取设备状态失败: {get_libusb_error_name(result)}");
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

                        // 释放配置描述符
                        libusb_free_config_descriptor(copylineConfigPtr);
                        copylineConfigPtr = IntPtr.Zero;
                    }
                }
                // 释放设备列表
                libusb_free_device_list(_deviceList, 1);
                _deviceList = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{USB_NAME}] {ex}");
            }
            finally
            {
                if (copylineConfigPtr != IntPtr.Zero)
                {
                    libusb_free_config_descriptor(copylineConfigPtr);
                }
                if (_deviceList != IntPtr.Zero)
                {
                    // 释放设备列表
                    libusb_free_device_list(_deviceList, 1);
                }
            }

            return true;
        }

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

        public string get_libusb_error_name(int errorCode)
        {
            string errMsg;
            IntPtr errorNamePtr = libusb_error_name(errorCode);
            if (errorNamePtr != IntPtr.Zero)
            {
                // 将非托管指针转换为托管字符串
                errMsg = Marshal.PtrToStringAnsi(errorNamePtr); 
            }
            else
            {
                errMsg = "Unknown error";
            }
            return errMsg;
        }
    }
}
