using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;

namespace Isc.Yft.UsbBridge.Interfaces
{
    /// <summary>
    /// 对拷线控制的通用接口
    /// </summary>
    public interface ICopyline : IDisposable
    {
        CopylineInfo Info { get; }

        CopylineStatus Status { get; }

        /// <summary>
        /// libusb_init 初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 从设备读取设备信息，更新Info
        /// </summary>
        void UpdateCopylineInfo();

        /// <summary>
        /// 从设备读取当前工作状态，更新Status
        /// </summary>
        void UpdateCopylineStatus();

        /// <summary>
        /// 打开设备,声明端口
        /// </summary>
        void OpenCopyline();

        /// <summary>
        /// 写数据 (调用 libusb_bulk_transfer 向设备发送数据)
        /// </summary>
        /// <param name="data">待发送字节</param>
        /// <returns>实际写入的字节数</returns>
        int WriteDataToDevice(byte[] data);

        /// <summary>
        /// 读数据 (调用 libusb_bulk_transfer 从设备读取数据)
        /// </summary>
        /// <param name="buffer">读取到的缓冲区</param>
        /// <returns>实际读取的字节数</returns>
        int ReadDataFromDevice(byte[] buffer);

        /// <summary>
        /// libusb_close 关闭设备
        /// </summary>
        void CloseCopyline();

        /// <summary>
        /// libusb_exit 退出libusb
        /// </summary>
        void Exit();
    }
}
