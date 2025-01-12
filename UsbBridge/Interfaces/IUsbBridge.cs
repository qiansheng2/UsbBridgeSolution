using System;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;

namespace Isc.Yft.UsbBridge.Interfaces
{
    /// <summary>
    /// 定义对 USB 数据线的对外操作接口
    /// </summary>
    public interface IUsbBridge : IDisposable
    {
        /// <summary>
        /// USB 模式
        /// </summary>
        USBMode CurrentMode { get; set; }

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
        Task<Result<string>> SendBigData(EPacketOwner owner, byte[] data);

        /// <summary>
        /// 外网向内网机器发送命令，并获取命令执行结果
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<Result<string>> SendCommand(string command);
    }
}
