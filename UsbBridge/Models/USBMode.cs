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
        /// USB数据传输方向（上传，下行）
        /// </summary>
        public EUSBDirection Direction { get; set; } = EUSBDirection.UPLOAD;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="usbPosition">USB 工作环境</param>
        /// <param name="usbDirection">USB 工作状态</param>
        public USBMode(EUSBPosition usbPosition, EUSBDirection usbDirection)
        {
            Position = usbPosition;
            Direction = usbDirection;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="usbPosition">USB 工作环境</param>
        /// <param name="usbDirection">USB 工作状态</param>
        public void SetUSBMode(EUSBPosition usbPosition, EUSBDirection usbDirection)
        {
            Position = usbPosition;
            Direction = usbDirection;
        }

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        /// <returns>返回对象的字符串表示形式</returns>
        public override string ToString()
        {
            return $"本机位置: [{Position}], 数据传输方向: [{Direction}]";
        }
    }
}
