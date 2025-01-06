using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;
using Isc.Yft.UsbBridge.Exceptions;
using System.Runtime.Remoting.Contexts;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Isc.Yft.UsbBridge.Devices
{
    internal abstract class PICopyline : ICopyline
    {
        // ========== [1] 导入 libusb-1.0 的函数  ==========
        /// <summary>
        /// 初始化 libusb 库，并创建一个 libusb 上下文。
        /// </summary>
        /// <param name="context">初始化后的 libusb 上下文指针。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int libusb_init(out IntPtr context);

        /// <summary>
        /// 获取当前 libusb 上下文中的设备列表。
        /// </summary>
        /// <param name="ctx">libusb 上下文指针。</param>
        /// <param name="list">设备列表的指针数组。</param>
        /// <returns>返回设备数量（包括所有设备），负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libusb_get_device_list(IntPtr ctx, out IntPtr list);

        /// <summary>
        /// 获取指定 USB 设备的设备描述符。
        /// </summary>
        /// <param name="device">目标 USB 设备的指针。</param>
        /// <param name="descriptor">设备描述符结构体的输出参数。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int libusb_get_device_descriptor(IntPtr device, out SLibusbDeviceDescriptor descriptor );

        /// <summary>
        /// 释放 libusb 库，并销毁相关的 libusb 上下文。
        /// </summary>
        /// <param name="context">要释放的 libusb 上下文指针。</param>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void libusb_exit(IntPtr context);

        /// <summary>
        /// 根据供应商 ID 和产品 ID 打开指定的 USB 设备。
        /// </summary>
        /// <param name="context">libusb 上下文指针。</param>
        /// <param name="vendor_id">目标设备的供应商 ID（VID）。</param>
        /// <param name="product_id">目标设备的产品 ID（PID）。</param>
        /// <returns>返回设备句柄指针，如果失败则返回 IntPtr.Zero。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern IntPtr libusb_open_device_with_vid_pid(IntPtr context, ushort vendor_id, ushort product_id);

        /// <summary>
        /// 关闭之前打开的 USB 设备句柄。
        /// </summary>
        /// <param name="deviceHandle">要关闭的设备句柄指针。</param>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void libusb_close(IntPtr deviceHandle);

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
        private static extern int libusb_get_port_numbers(IntPtr dev, [Out] byte[] portNumbers, int portNumbersLen);

        /// <summary>
        /// 打开指定的 USB 设备并获取其设备句柄。
        /// </summary>
        /// <param name="dev">目标 USB 设备的指针。</param>
        /// <param name="dev_handle">输出参数，返回设备句柄指针。</param>
        /// <returns>返回 0 表示成功，负值表示错误代码。</returns>
        [DllImport("libusb-1.0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int libusb_open(IntPtr dev, out IntPtr dev_handle);

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
        protected static extern int libusb_bulk_transfer(IntPtr deviceHandle, byte endpoint, byte[] data, int length, out int transferred, uint timeout);

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

        // ========== [2] 内部字段 ==========
        protected IntPtr _context = IntPtr.Zero;
        protected IntPtr _deviceHandle = IntPtr.Zero;

        // VendorID / ProductID (需要根据实际对拷线数据覆盖)
        protected virtual string USB_NAME { get; set; } = "";
        protected virtual ushort USB_VID { get; set; } = 0x0000;
        protected virtual ushort USB_PID { get; set; } = 0x0000;

        // ========== [3] 实现 IUsbCopyline 接口 ==========
        public abstract int WriteDataToDevice(byte[] data);

        public abstract int ReadDataFromDevice(byte[] buffer);

        public void Initialize()
        {
            // 初始化usb库
            int ret = libusb_init(out _context);
            if (ret < 0)
            {
                throw new InvalidOperationException($"[Main] libusb_init 失败, [{USB_NAME}]: {get_libusb_error_name(ret)}");
            }
            Console.WriteLine($"[Main] libusb_init 成功, [{USB_NAME}]:_context:0x{_context.ToInt64():X}");

            // 获取usb设备信息,包括输入输出、状态等信息
            ReadCopylineInfo(true);
            Console.WriteLine($"[Main] ReadCopylineInfo 完成, [{USB_NAME}]: {_copylineInfo}");

        }

        /// <summary>
        /// 执行libusb_open_device_with_vid_pid(),打开usb对拷线设备
        /// </summary>
        /// <returns>成功：true，失败：false</returns>
        public void OpenDevice()
        {
            // 判断设备是否存在
            if (_copylineInfo.DeviceExist == false) {
                throw new UsbCopylineNotFoundException($"[{USB_NAME}]设备尚未正确初始化，无法打开设备句柄.");
            }

            // 打开对拷线USB设备
            _deviceHandle = libusb_open_device_with_vid_pid(_context, USB_VID, USB_PID);
            if (_deviceHandle == IntPtr.Zero )
            {
                throw new InvalidOperationException($"[{USB_NAME}]libusb_open_device_with_vid_pid 失败.");
            }
            Console.WriteLine($"[Main] [{USB_NAME}] libusb_open_device_with_vid_pid 成功, _deviceHandle:0x{_deviceHandle.ToInt64():X}");

            // 获取对拷线设备接口番号
            int interface_no = _copylineInfo.BulkInterfaceNo;
            if( interface_no < 0)
            {
                throw new InvalidOperationException($"[{USB_NAME}]错误的usb接口番号.");
            }

            // 获取对拷线当前最新状态
            _copylineStatus = ReadCopylineStatus(true);
            // 输出当前设备状态
            if (_copylineStatus.Usable == ECopylineUsable.OK)
            {
                Console.WriteLine($"[Main] [{USB_NAME}] 设备状态OK，可以传输数据.");
            }
            else
            {
                Console.WriteLine($"[Main] [{USB_NAME}] 设备状态NG，不能传输数据!");
                throw new InvalidOperationException($"[Main] [{USB_NAME}] 设备状态NG，不能传输数据!");
            }
        }

        /// <summary>
        /// 获取拷贝线的基本信息
        /// </summary>
        /// <returns>拷贝线硬件信息</returns>
        public CopylineInfo ReadCopylineInfo(bool fromHardware = true)
        {
            if (!fromHardware)
            {
                return _copylineInfo;
            }

            CopylineInfo return_info = new CopylineInfo();
            IntPtr _localDeviceList = IntPtr.Zero;            
            IntPtr _localCopylineConfigPtr = IntPtr.Zero;
            IntPtr _localDeviceHandle = IntPtr.Zero;
            try
            {
                // 获取设备列表
                int deviceCount = libusb_get_device_list(_context, out _localDeviceList);
                if (deviceCount < 1)
                {
                    throw new InvalidOperationException($"[{USB_NAME}]没有发现USB设备，或libusb_get_device_list 调用失败.");
                }
                else
                    Console.WriteLine($"[Main] [{USB_NAME}]发现了{deviceCount}个USB设备.");
                                                                                                                
                // 获取所需设备
                for (int i = 0; i < deviceCount; i++)
                {
                    // 读取设备指针
                    IntPtr devicePtr = Marshal.ReadIntPtr(_localDeviceList, i * IntPtr.Size);
                    if (devicePtr == IntPtr.Zero)
                        continue;

                    // 获取设备描述符
                    SLibusbDeviceDescriptor deviceDesc;
                    int result = libusb_get_device_descriptor(devicePtr, out deviceDesc);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"[{USB_NAME}]获取设备描述符失败: {get_libusb_error_name(result)}");
                    }
                    // 打印设备信息
                    // Console.Write($"VID:{deviceDesc.idVendor:X4},PID:{deviceDesc.idProduct:X4}");

                    // 获取端口路径
                    //byte[] path = new byte[8];
                    //int portCount = libusb_get_port_numbers(devicePtr, path, path.Length);
                    //if (portCount > 0)
                    //{
                    //    Console.WriteLine(" path: " + path[0]);
                    //    for (int k = 1; k < portCount; k++)
                    //    {
                    //        Console.Write($".{path[k]}");
                    //    }
                    //}
                    // Console.WriteLine();

                    // 检查是否是目标设备
                    if (deviceDesc.idVendor == USB_VID && deviceDesc.idProduct == USB_PID)
                    {
                        Console.WriteLine("--------------------------------------------------------------");
                        Console.WriteLine($"    {USB_NAME} 设备信息");

                        return_info.DeviceExist = true;

                        // 打开设备
                        result = libusb_open(devicePtr, out _localDeviceHandle);
                        if (result != 0 || _localDeviceHandle == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}]libusb_open 打开目标USB设备失败: {get_libusb_error_name(result)}");
                        }

                        // 获取配置描述符
                        result = libusb_get_active_config_descriptor(devicePtr, out _localCopylineConfigPtr);
                        if (result != 0 || _localCopylineConfigPtr == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"[{USB_NAME}]libusb_get_active_config_descriptor获取设备描述符信息失败: {get_libusb_error_name(result)}");
                        }
                        SLibusbConfigDescriptor configDesc = Marshal.PtrToStructure<SLibusbConfigDescriptor>(_localCopylineConfigPtr);
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
                                    return_info.BulkInterfaceNo = j;
                                    Console.WriteLine($"        bulk_interface_no = {j}");
                                    Console.Write($"        Found bulk endpoint No. {k}");

                                    // 检查方向
                                    if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_IN)
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (IN)");
                                        return_info.BulkInAddress = endpoint.bEndpointAddress;
                                    }
                                    else if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_OUT)
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (OUT)");
                                        return_info.BulkOutAddress = endpoint.bEndpointAddress;
                                    }
                                    else
                                    {
                                        Console.WriteLine($" at address = 0x{endpoint.bEndpointAddress:X2} (Unknown Direction)");
                                    }
                                }
                            }
                        }
                        Console.WriteLine("--------------------------------------------------------------");

                        // 释放配置描述符
                        libusb_free_config_descriptor(_localCopylineConfigPtr);
                        _localCopylineConfigPtr = IntPtr.Zero;

                        // 关闭usb
                        libusb_close(_localDeviceHandle);
                        _localDeviceHandle = IntPtr.Zero;
                    }
                }

                // 释放设备列表
                libusb_free_device_list(_localDeviceList, 1);
                _localDeviceList = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] {ex.Message}");
            }
            finally
            {
                // 释放配置描述符
                if (_localCopylineConfigPtr != IntPtr.Zero)
                {
                    libusb_free_config_descriptor(_localCopylineConfigPtr);
                }

                // 关闭usb
                if (_localDeviceHandle != IntPtr.Zero)
                {
                    libusb_close(_localDeviceHandle);
                }

                // 释放设备列表
                if (_localDeviceList != IntPtr.Zero)
                {
                    libusb_free_device_list(_localDeviceList, 1);
                    _localDeviceList = IntPtr.Zero;
                }
            }

            // 更新对拷线硬件信息
            SetCopylineInfo(return_info);
            // 返回
            return return_info;
        }

        /// <summary>
        /// 获取拷贝线的当前活动状态
        /// </summary>
        /// <param name="fromHardware">是否真正读取usb硬件状态</param>
        /// <returns>对拷线状态</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public CopylineStatus ReadCopylineStatus(bool fromHardware = true)
        {
            // 声明usb设备的端口
            CopylineStatus return_status = new CopylineStatus();

            try
            {
                // 如果不需要读取usb硬件，就直接返回类变量
                if (!fromHardware)
                {
                    return _copylineStatus;
                }

                // 从usb硬件获取设备活动状态
                if (_deviceHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"[{USB_NAME}] USB设备没有打开，无法继续.");
                }
                if (_copylineInfo.BulkInterfaceNo < 0)
                {
                    throw new InvalidOperationException($"[{USB_NAME}] BulkInterfaceNo接口番号[{_copylineInfo.BulkInterfaceNo}]内容错误, 无法继续.");
                }

                int result = libusb_claim_interface(_deviceHandle, _copylineInfo.BulkInterfaceNo);
                if (result != 0)
                {
                    throw new InvalidOperationException($"[{USB_NAME}] 声明硬件接口失败: {get_libusb_error_name(result)}");
                }
                // 检查本地设备和远程设备的各种状态
                byte[] devStatusBuffer = new byte[2];
                result = libusb_control_transfer(_deviceHandle, 0xC0, 0xF1, 0, 0, devStatusBuffer, 2, 500);
                if (result < 0)
                {
                    throw new InvalidOperationException($"[{USB_NAME}] 获取设备状态失败: {get_libusb_error_name(result)}");
                }
                else
                {
                    string binaryString = CommonUtil.ByteArrayToBinaryString(devStatusBuffer);
                    Console.WriteLine($"[{USB_NAME}] 获取了{result}字节的状态信息: {binaryString}");
                }
                // 将字节数组转换为结构体
                SDEV_STATUS devStatus = CommonUtil.ByteArrayToStructure<SDEV_STATUS>(devStatusBuffer);

                // 设置对拷线活动状态 和 可用状态
                return_status.DeviceStatus = devStatus;
                SetCopylineStatus(return_status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Main] {ex.Message},重置系统硬件信息和状态。");
                ResetInfoAndStatus();
            }
            finally
            {
                // 释放usb设备的端口
                if(_deviceHandle != IntPtr.Zero)
                {
                    libusb_release_interface(_deviceHandle, _copylineInfo.BulkInterfaceNo);
                }
            }
            return return_status;
        }

        public void CloseDevice()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                libusb_close(_deviceHandle);
                _deviceHandle = IntPtr.Zero;
                Console.WriteLine("[Main] 已关闭 USB 设备.");
            }
        }

        public void Exit()
        {
            if (_context != IntPtr.Zero)
            {
                libusb_exit(_context);
                _context = IntPtr.Zero;
                Console.WriteLine("[Main] libusb_exit 已完成.");
            }
        }

        public void Dispose()
        {
            // Dispose中做最终清理
            CloseDevice();
            Exit();
        }

        private string get_libusb_error_name(int errorCode)
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

        private void ResetInfoAndStatus()
        {
            _deviceHandle = IntPtr.Zero;
            _copylineInfo = new CopylineInfo();
            _copylineStatus = new CopylineStatus();
        }
    }
}
