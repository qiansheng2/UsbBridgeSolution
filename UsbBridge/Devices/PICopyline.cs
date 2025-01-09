using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;
using Isc.Yft.UsbBridge.Exceptions;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Isc.Yft.UsbBridge.Devices
{
    internal abstract class PICopyline : ICopyline
    {
        protected static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // ========== 内部字段 ==========
        protected LibUsbContextSafeHandle _usbContext;
        protected LibusbDeviceSafeHandle _deviceHandle;

        // ========== 实现 IUsbCopyline 接口 ==========
        public abstract int WriteDataToDevice(byte[] data);

        public abstract int ReadDataFromDevice(byte[] buffer);
        public virtual CopylineInfo Info { get; }  // VendorID / ProductID (需要根据实际对拷线数据覆盖)
        public virtual CopylineStatus Status { get; }

        public void Initialize()
        {
            // 初始化usb库
            _usbContext = LibUsbContextSafeHandle.Create();
            if (_usbContext.IsInvalid)
            {
                throw new InvalidOperationException($"USB库初始化(libusb_init())失败。");
            }

            UpdateCopylineInfo();
            Logger.Info($"USB库初始化(libusb_init())成功。");
        }

        /// <summary>
        /// 根据VID和PID，获取拷贝线的硬件信息
        /// </summary>
        public void UpdateCopylineInfo()
        {
            IntPtr _localDeviceList = IntPtr.Zero;
            IntPtr _localCopylineConfigPtr = IntPtr.Zero;
            IntPtr _localDeviceHandle = IntPtr.Zero;
            CopylineInfo _temp_info = new CopylineInfo();
            try
            {
                // 获取设备列表
                int deviceCount = LibusbInterop.libusb_get_device_list(_usbContext.DangerousGetHandle(), out _localDeviceList);
                if (deviceCount < 1)
                {
                    throw new CopylineNotFoundException($"没有发现USB设备，libusb_get_device_list()调用失败.");
                }
                else
                    Logger.Info($"发现了{deviceCount}个USB设备.");

                // 获取所需设备
                for (int i = 0; i < deviceCount; i++)
                {
                    // 读取设备指针
                    IntPtr devicePtr = Marshal.ReadIntPtr(_localDeviceList, i * IntPtr.Size);
                    if (devicePtr == IntPtr.Zero)
                        continue;

                    // 获取设备描述符
                    SLibusbDeviceDescriptor deviceDesc;
                    int result = LibusbInterop.libusb_get_device_descriptor(devicePtr, out deviceDesc);
                    if (result != 0)
                    {
                        throw new InvalidOperationException($"获取设备描述符失败: {get_libusb_error_name(result)}");
                    }
                    // 打印设备信息
                    // Logger.Info($"VID:{deviceDesc.idVendor:X4},PID:{deviceDesc.idProduct:X4}");

                    // 获取端口路径
                    //byte[] path = new byte[8];
                    //int portCount = libusb_get_port_numbers(devicePtr, path, path.Length);
                    //if (portCount > 0)
                    //{
                    //    Logger.Info(" path: " + path[0]);
                    //    for (int k = 1; k < portCount; k++)
                    //    {
                    //        Logger.Info($".{path[k]}");
                    //    }
                    //}
                    // Logger.Info();

                    // 检查是否是目标设备
                    if (deviceDesc.idVendor == Info.Vid && deviceDesc.idProduct == Info.Pid)
                    {
                        Logger.Info("--------------------------------------------------------------");
                        Logger.Info($"    {Info.Name} 设备信息");

                        _temp_info.FromDevice = true;

                        // 打开设备
                        result = LibusbInterop.libusb_open(devicePtr, out _localDeviceHandle);
                        if (result != 0 || _localDeviceHandle == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"[libusb_open 打开目标USB设备失败: {get_libusb_error_name(result)}");
                        }

                        // 获取配置描述符
                        result = LibusbInterop.libusb_get_active_config_descriptor(devicePtr, out _localCopylineConfigPtr);
                        if (result != 0 || _localCopylineConfigPtr == IntPtr.Zero)
                        {
                            throw new InvalidOperationException($"libusb_get_active_config_descriptor获取设备描述符信息失败: {get_libusb_error_name(result)}");
                        }
                        SLibusbConfigDescriptor configDesc = Marshal.PtrToStructure<SLibusbConfigDescriptor>(_localCopylineConfigPtr);
                        Logger.Info($"    NumInterfaces = {configDesc.bNumInterfaces}");
                        Logger.Info($"    Total Length = {configDesc.wTotalLength}");
                        Logger.Info($"    Max Power = {configDesc.MaxPower}");

                        // 遍历接口
                        for (int j = 0; j < configDesc.bNumInterfaces; j++)
                        {
                            // 计算接口描述符的指针偏移量
                            // 注意：libusb_interface_descriptor 实际上是 libusb_interface 结构体中的 altsetting 数组

                            // 获取接口描述符指针
                            IntPtr interfacePtr = Marshal.ReadIntPtr(configDesc.interface_, j * IntPtr.Size);
                            SLibusbInterfaceDescriptor interfaceDesc = Marshal.PtrToStructure<SLibusbInterfaceDescriptor>(interfacePtr);
                            Logger.Info($"    Interface {j}: No. of endpoints = {interfaceDesc.bNumEndpoints}");

                            // 遍历端点
                            for (int k = 0; k < interfaceDesc.bNumEndpoints; k++)
                            {
                                // 获取端点描述符指针
                                IntPtr endpointArrayPtr = interfaceDesc.endpoint;
                                IntPtr endpointPtr = IntPtr.Add(endpointArrayPtr, k * Marshal.SizeOf<SLibusbEndpointDescriptor>());
                                SLibusbEndpointDescriptor endpoint = Marshal.PtrToStructure<SLibusbEndpointDescriptor>(endpointPtr);
                                Logger.Info($"        Endpoint {k}: bmAttributes = {endpoint.bmAttributes:X2}");

                                // 检查控制端点Bulk endpoint: LIBUSB_TRANSFER_TYPE_CONTROL = 0
                                if (endpoint.bmAttributes == 0)
                                {
                                    Logger.Info($"        找到控制端口 No. {k}");
                                }

                                // 检查批量端点 Bulk endpoint: LIBUSB_TRANSFER_TYPE_BULK = 2
                                if (endpoint.bmAttributes == 2)
                                {
                                    Logger.Info($"        批量传输接口番号 = {j}");
                                    Logger.Info($"        找到批量传输端口 No. {k}");

                                    _temp_info.BulkInterfaceNo = j;

                                    // 检查方向
                                    if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_IN)
                                    {
                                        Logger.Info($" at address = 0x{endpoint.bEndpointAddress:X2} (IN)");
                                        _temp_info.BulkInAddress = endpoint.bEndpointAddress;
                                    }
                                    else if ((endpoint.bEndpointAddress & Constants.LIBUSB_ENDPOINT_DIR_MASK) == Constants.LIBUSB_ENDPOINT_OUT)
                                    {
                                        Logger.Info($" at address = 0x{endpoint.bEndpointAddress:X2} (OUT)");
                                        _temp_info.BulkOutAddress = endpoint.bEndpointAddress;
                                    }
                                    else
                                    {
                                        Logger.Info($" at address = 0x{endpoint.bEndpointAddress:X2} (Unknown Direction)");
                                    }
                                }
                            }

                            // 存储从设备中获取的硬件信息
                            Info.FromDevice = _temp_info.FromDevice;
                            Info.BulkInterfaceNo = _temp_info.BulkInterfaceNo;
                            Info.BulkInAddress = _temp_info.BulkInAddress;
                            Info.BulkOutAddress = _temp_info.BulkOutAddress;
                        }
                        Logger.Info("--------------------------------------------------------------");

                        // 释放配置描述符
                        LibusbInterop.libusb_free_config_descriptor(_localCopylineConfigPtr);
                        _localCopylineConfigPtr = IntPtr.Zero;

                        // 关闭usb
                        LibusbInterop.libusb_close(_localDeviceHandle);
                        Logger.Info("UpdateCopylineInfo()[1]中调用了libusb_close()。");
                        _localDeviceHandle = IntPtr.Zero;
                    }
                }

                // 释放设备列表
                LibusbInterop.libusb_free_device_list(_localDeviceList, 1);
                _localDeviceList = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}");
            }
            finally
            {
                // 释放配置描述符
                if (_localCopylineConfigPtr != IntPtr.Zero)
                {
                    LibusbInterop.libusb_free_config_descriptor(_localCopylineConfigPtr);
                }

                // 关闭usb
                if (_localDeviceHandle != IntPtr.Zero)
                {
                    LibusbInterop.libusb_close(_localDeviceHandle);
                    Logger.Info("UpdateCopylineInfo()[2]中调用了libusb_close()。");
                }

                // 释放设备列表
                if (_localDeviceList != IntPtr.Zero)
                {
                    LibusbInterop.libusb_free_device_list(_localDeviceList, 1);
                    _localDeviceList = IntPtr.Zero;
                }
            }

            if(Info.FromDevice == false)
            {
                Logger.Warn($"当前系统中没有发现[{Info.Name}]USB设备: 0x{Info.Vid:X} / 0x{Info.Pid:X}。");
            }
            else
            {
                Logger.Info($"成功从设备中获取了[{Info.Name}]设备信息：端口号 = {Info.BulkInterfaceNo}, PID = 0x{Info.Pid:X},VID = 0x{Info.Vid:X}。");
            }

        }

        /// <summary>
        /// 执行libusb_open_device_with_vid_pid(),打开usb对拷线设备
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void OpenCopyline()
        {

            // 判断是否已经libusb初始化[已经调用libusb_init()]
            if (_usbContext.IsInvalid)
            {
                throw new CopylineNotFoundException($"Libusb环境尚未初始化，无法继续.");
            }

            // 判断设备端口号是否更新
            if (Info.FromDevice == false)
            {
                // 从硬件设备中读取并保存设备信息
                UpdateCopylineInfo();
            }

            // 如果读取到了硬件信息，调用libusb_open_device_with_vid_pid()
            if (Info.FromDevice == true) {
                try
                {
                    if (_deviceHandle == null || _deviceHandle.IsInvalid)
                    {
                        _deviceHandle = LibusbDeviceSafeHandle.OpenDevice(_usbContext.DangerousGetHandle(), Info.Vid, Info.Pid);
                        if (_deviceHandle.IsInvalid)
                        {
                            throw new InvalidOperationException($"USB设备打开失败：0x{Info.Vid:X},0x{Info.Pid:X}。");
                        }
                        else
                        {
                            // 获取到USB设备的有效句柄
                            Logger.Info($"USB设备打开成功(libusb_open_device_with_vid_pid())。");
                        }
                    }
                    else
                    {
                        Logger.Info($"继续沿用既有的usb设备句柄(_deviceHandle)。");
                    }
                }
                catch (AccessViolationException ave)
                {
                    Logger.Error("捕获到AccessViolationException: " + ave.Message);
                    throw new InvalidHardwareException($"Open设备时，发现USB数据传输通路无效，无法进行读写操作...{ave.Message}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"无法打开设备，发生错误：{ex.Message}");
                }

                // 获取对拷线当前最新状态
                UpdateCopylineStatus();
            }
            else
            {
                Logger.Warn($"没有找到USB硬件设备，等待USB插入后再试。");
            }

            // 输出当前设备状态
            Logger.Info($"[{Info.Name}]设备状态：{Status}。");
        }

        /// <summary>
        /// 获取拷贝线的当前活动状态
        /// </summary>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void UpdateCopylineStatus()
        {
            try
            {
                if (_deviceHandle == null)
                {
                    // 对拷线设备尚未打开
                    Status.Clear();
                    return;
                }

                // 从usb硬件获取设备活动状态
                if (_deviceHandle.IsInvalid)
                {
                    throw new InvalidOperationException($"USB设备没有打开，无法继续。");
                }
                if (Info.BulkInterfaceNo < 0)
                {
                    throw new InvalidOperationException($"[{Info.Name}]设备的BulkInterfaceNo接口番号[{Info.BulkInterfaceNo}]内容错误, 无法继续。");
                }

                int result = LibusbInterop.libusb_claim_interface(_deviceHandle.DangerousGetHandle(), Info.BulkInterfaceNo);
                if (result != 0)
                {
                    throw new InvalidOperationException($"硬件接口声明失败: {get_libusb_error_name(result)}。");
                }
                // 检查本地设备和远程设备的各种状态
                byte[] devStatusBuffer = new byte[2];
                int byteCounts = LibusbInterop.libusb_control_transfer(_deviceHandle.DangerousGetHandle(), 0xC0, 0xF1, 0, 0, devStatusBuffer, 2, Constants.BULK_TIMEOUT_MS);
                if (byteCounts < 0)
                {
                    throw new InvalidOperationException($"获取[{Info.Name}]设备状态失败: {get_libusb_error_name(byteCounts)}。");
                }
                else
                {
                    string binaryString = ComUtil.ByteArrayToBinaryString(devStatusBuffer);
                    // Logger.Info($"成功获取[{Info.Name}]设备{byteCounts}字节的状态信息: {binaryString}");
                }
                // 将字节数组转换为结构体
                SDEV_STATUS devStatus = ComUtil.ByteArrayToStructure<SDEV_STATUS>(devStatusBuffer);

                // 设置对拷线活动状态 和 可用状态
                Status.DeviceStatus = devStatus;
            }
            catch(AccessViolationException ave)
            {
                Logger.Fatal("捕获到AccessViolationException: " + ave.Message);
                throw new InvalidHardwareException($"更新状态时，发现USB数据传输通路损毁，无法进行读写操作...{ave.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"获取拷贝线的当前活动状态时发生错误: {ex.Message}");

                if (!_deviceHandle.IsInvalid)
                {
                    LibusbInterop.libusb_release_interface(_deviceHandle.DangerousGetHandle(), Info.BulkInterfaceNo);
                }

                // 释放usb设备的端口句柄
                CloseCopyline();

                // 把错误抛到外部
                throw;
            }
        }
        public void CloseCopyline()
        {
            _deviceHandle?.Dispose();
            Status.Clear();
        }

        public void Exit()
        {
            _usbContext?.Dispose();
            Status.Clear();
            Logger.Info("已退出USB设备操作(libusb_exit())。");
        }

        public void Dispose()
        {
            // Dispose中做最终清理
            CloseCopyline();
            Exit();
        }

        private string get_libusb_error_name(int errorCode)
        {
            string errMsg;
            IntPtr errorNamePtr = LibusbInterop.libusb_error_name(errorCode);
            if (errorNamePtr != IntPtr.Zero)
            {
                // 将非托管指针转换为托管字符串
                errMsg = Marshal.PtrToStringAnsi(errorNamePtr); 
            }
            else
            {
                errMsg = "未知的libusb错误";
            }
            return errMsg;
        }
    }
}
