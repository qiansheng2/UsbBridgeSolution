using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class CopyLineInfo
    {
        // USB设备控制信息
        public byte BulkInAddress { get; set; }         // IN地址
        public byte BulkOutAddress { get; set; }        // OUT地址
        public int BulkInterfaceNo { get; set; }        // 接口番号

        // USB设备当前连接状态
        public SDEV_STATUS DeviceStatus { get; set; }   // 状态

        // 对拷线设备是否可用
        public ECopyLineUsable Usable { get; set; }

        // 重写 ToString 方法
        public override string ToString()
        {
            return $"可用={Usable}, 地址 = [In: BulkInAddress={BulkInAddress}, Out:BulkOutAddress={BulkOutAddress}], " +
                   $"接口番号={BulkInterfaceNo}, 状态={DeviceStatus}";
        }

    }
}
