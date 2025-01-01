using System;
using System.ServiceModel;
using Isc.Yft.UsbBridge.Models;

namespace Isc.Yft.UsbBridge.Interfaces
{
    /// <summary>
    /// 定义对 USB 数据线的对外操作接口
    /// </summary>
    public interface IUsbBridge : IDisposable
    {
        /// <summary>
        /// 初始化并启动所有线程（发送、接收、监控）
        /// </summary>
        void Start();

        /// <summary>
        /// 停止所有线程，并进行资源清理
        /// </summary>
        void Stop();

        /// <summary>
        /// 发送需要分解为多个数据包的大数据
        /// </summary>
        /// <param name="data">待发送的字节数组</param>
        void SendBigData(EPacketOwner owner, byte[] data);

        /// <summary>
        /// 设定或更改数据线的工作模式
        /// （例如切换到某种特殊协议或速率）
        /// </summary>
        /// <param name="mode">可以是一个枚举，或字符串等</param>
        void SetMode(USBMode mode);

        /// <summary>
        /// 获取当前工作模式
        /// </summary>
        /// <returns>返回当前模式的描述</returns>
        USBMode GetCurrentMode();
    }
}
