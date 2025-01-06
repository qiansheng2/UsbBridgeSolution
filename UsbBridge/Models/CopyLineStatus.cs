using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    public class CopylineStatus
    {
        // 用于锁定的对象
        private readonly object _lock = new object();

        // 对拷线设备是否可用（自动计算）
        public ECopylineStatus RealtimeStatus
        {
            get
            {
                lock (_lock) // 锁定访问 DeviceStatus
                {
                    if (DeviceStatus.LocalAttached && DeviceStatus.RemoteAttached)
                    {
                        return ECopylineStatus.ONLINE;
                    }
                    else
                    {
                        return ECopylineStatus.OFFLINE;
                    }
                }
            }
        }

        // USB设备当前连接状态
        private SDEV_STATUS _deviceStatus;
        public SDEV_STATUS DeviceStatus
        {
            get
            {
                lock (_lock)
                {
                    return _deviceStatus;
                }
            }
            set
            {
                lock (_lock)
                {
                    _deviceStatus = value;
                }
            }
        }

        // 清理，回到初始状态
        public void Clear()
        {
            lock (_lock)
            {
                DeviceStatus = new SDEV_STATUS();
            }
        }

        // 重写 ToString 方法
        public override string ToString()
        {
            lock (_lock) // 确保在多线程下访问 Usable 和 DeviceStatus 的一致性
            {
                return $"设备是否可用 = [{RealtimeStatus}], 设备状态详细 = [{DeviceStatus}]";
            }
        }
    }
}
