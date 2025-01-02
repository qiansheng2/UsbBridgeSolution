using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    public class CopyLineStatus
    {
        // 用于锁定的对象
        private readonly object _lock = new object();

        // 是否进行了初始花
        private bool _initialized = false; // 初始值设置为 false
        public bool Initialized
        {
            get
            {
                lock (_lock)
                {
                    return _initialized;
                }
            }
            set
            {
                lock (_lock)
                {
                    _initialized = value;
                }
            }
        }

        // 对拷线设备是否可用（自动计算）
        public ECopyLineUsable Usable
        {
            get
            {
                lock (_lock) // 锁定访问 DeviceStatus
                {
                    if (DeviceStatus.LocalAttached && DeviceStatus.RemoteAttached)
                    {
                        return ECopyLineUsable.OK;
                    }
                    else
                    {
                        return ECopyLineUsable.NG;
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

        // 重写 ToString 方法
        public override string ToString()
        {
            lock (_lock) // 确保在多线程下访问 Usable 和 DeviceStatus 的一致性
            {
                return $"设备是否可用 = [{Usable}], 设备状态详细 = [{DeviceStatus}]";
            }
        }
    }
}
