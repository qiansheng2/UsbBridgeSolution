using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    public class CopyLineInfo
    {
        // 锁对象，用于同步
        private readonly object _lock = new object();

        // 是否进行了初始化
        private bool _deviceExist = false;
        public bool DeviceExist
        {
            get
            {
                lock (_lock)
                {
                    return _deviceExist;
                }
            }
            set
            {
                lock (_lock)
                {
                    _deviceExist = value;
                }
            }
        }

        // USB设备控制信息
        private byte _bulkInAddress;
        public byte BulkInAddress
        {
            get
            {
                lock (_lock)
                {
                    return _bulkInAddress;
                }
            }
            set
            {
                lock (_lock)
                {
                    _bulkInAddress = value;
                }
            }
        }

        private byte _bulkOutAddress;
        public byte BulkOutAddress
        {
            get
            {
                lock (_lock)
                {
                    return _bulkOutAddress;
                }
            }
            set
            {
                lock (_lock)
                {
                    _bulkOutAddress = value;
                }
            }
        }

        private int _bulkInterfaceNo;
        public int BulkInterfaceNo
        {
            get
            {
                lock (_lock)
                {
                    return _bulkInterfaceNo;
                }
            }
            set
            {
                lock (_lock)
                {
                    _bulkInterfaceNo = value;
                }
            }
        }

        // 重写 ToString 方法
        public override string ToString()
        {
            lock (_lock) // 加锁，确保多个变量读取时的一致性
            {
                return $"设备存在 = [{DeviceExist}], 地址 = [In: BulkInAddress={BulkInAddress}, Out:BulkOutAddress={BulkOutAddress}], " +
                       $"接口番号 = [{BulkInterfaceNo}]";
            }
        }
    }
}
