using Isc.Yft.UsbBridge.Interfaces;
using System.Runtime.InteropServices;
using System;
using System.Runtime.Remoting.Contexts;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;
using Isc.Yft.UsbBridge.Exceptions;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Isc.Yft.UsbBridge.Devices
{
    internal sealed class Pl25A1UsbCopyline : PICopyline
    {
        // ========== 覆盖PICopyline virtual 属性 ==========
        public override CopylineInfo Info { get; }
        public override CopylineStatus Status { get; }

        // ========== 构造函数 ==========
        public Pl25A1UsbCopyline()
        {
            Info = new CopylineInfo
            {
                Name = "PL25A1",
                Vid = 0x067B,
                Pid = 0x25A1,
                BulkInterfaceNo = 0,
                BulkInAddress = 0x00,
                BulkOutAddress = 0x00
            };

            Status = new CopylineStatus();
        }

        // ========== 实现 IUsbCopyline 接口 ==========
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public override int WriteDataToDevice(byte[] data)
        {
            if (_deviceHandle.IsInvalid)
            {
                Logger.Error($"[{Info.Name}] 设备尚未打开，无法写入数据.");
                return 0;
            }

            try
            {
                int ret = LibusbInterop.libusb_bulk_transfer(
                    _deviceHandle.DangerousGetHandle(),
                    Info.BulkOutAddress,
                    data,
                    data.Length,
                    out int transferred,
                    Constants.BULK_TIMEOUT_MS);
                if (ret < 0)
                {
                    Logger.Error($"[{Info.Name}] 写数据失败，libusb_bulk_transfer 返回: {ret}");
                    return 0;
                }
                Logger.Info($"[{Info.Name}] 已写入 {transferred} 字节.");
                return transferred;
            }
            catch (AccessViolationException ave)
            {
                Logger.Fatal("WriteDataToDevice()捕获到AccessViolationException: " + ave.Message);
                throw new InvalidHardwareException($"批量写入数据时，发现USB数据传输通路损毁，无法写入...{ave.Message}");
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public override int ReadDataFromDevice(byte[] buffer)
        {
            if (_deviceHandle.IsInvalid)
            {
                Logger.Error($"[{Info.Name}] 设备尚未打开，无法读取数据.");
                return 0;
            }

            try
            {
                int ret = LibusbInterop.libusb_bulk_transfer(
                _deviceHandle.DangerousGetHandle(),
                Info.BulkInAddress,
                buffer,
                buffer.Length,
                out int transferred,
                Constants.BULK_TIMEOUT_MS);

                if (ret < 0)
                {
                    Logger.Error($"[{Info.Name}] 读数据失败，libusb_bulk_transfer 返回: {ret}");
                    return 0;
                }

                Logger.Info($"[{Info.Name}] 已读取 {transferred} 字节.");
                return transferred;
            }
            catch (AccessViolationException ave)
            {
                Logger.Fatal("ReadDataFromDevice()捕获到AccessViolationException: " + ave.Message);
                throw new InvalidHardwareException($"批量读取数据时，发现USB数据传输通路损毁，无法读取...{ave.Message}");
            }
        }
    }
}






