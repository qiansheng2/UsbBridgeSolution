using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// USB对拷设备工作模式
/// </summary>
/// 
namespace Isc.Yft.UsbBridge.Models
{
    public class USBMode
    {
        /// <summary>
        /// USB工作环境
        /// </summary>
        public EUSBPosition Position { get; set; } = EUSBPosition.OUTSIDE;

        /// <summary>
        /// USB工作状态（上传，下行）
        /// </summary>
        public EUSBDirection Mode { get; set; } = EUSBDirection.UPLOAD;

        /// <summary>
        /// USB工作状态（上传，下行）
        /// </summary>
        public bool FoundDevice { get; set; } = false;

        /// <summary>
        /// 空的构造函数
        /// </summary>
        public USBMode() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="usbPosition">USB 工作环境</param>
        /// <param name="usbMode">USB 工作状态</param>
        public USBMode(EUSBPosition usbPosition, EUSBDirection usbMode,
                       bool foundDevice = false)
        {
            Position = usbPosition;
            Mode = usbMode;
            FoundDevice = foundDevice;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="usbPosition">USB 工作环境</param>
        /// <param name="usbMode">USB 工作状态</param>
        public void SetUSBMode(EUSBPosition usbPosition, EUSBDirection usbMode)
        {
            Position = usbPosition;
            Mode = usbMode;
        }

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        /// <returns>返回对象的字符串表示形式</returns>
        public override string ToString()
        {
            return $"本机位置: [{Position}], 数据传输模式: [{Mode}]";
        }
    }
}
