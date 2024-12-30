using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// USB对拷设备状态
/// </summary>
/// 
namespace Isc.Yft.UsbBridge.Models
{
    internal class USBStatus
    {
        /// <summary>
        /// USB工作环境
        /// </summary>
        public USBPosition UsbPosition { get; set; }

        /// <summary>
        /// USB工作状态（上传模式，下行模式）
        /// </summary>
        public USBMode UsbMode{ get; set; }

    }
}
