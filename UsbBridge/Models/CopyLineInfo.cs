﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    public class CopylineInfo
    {
        // 锁对象，用于同步
        private readonly object _lock = new object();

        // 是否读取过硬件设备
        private bool _fromDevice = false;
        public bool FromDevice
        {
            get
            {
                lock (_lock)
                {
                    return _fromDevice;
                }
            }
            set
            {
                lock (_lock)
                {
                    _fromDevice = value;
                }
            }
        }

        // Name / VendorID / ProductID (需要根据实际对拷线数据覆盖)
        // 拷贝线名称
        private String _name;
        public String Name
        {
            get
            {
                lock (_lock)
                {
                    return _name;
                }
            }
            set
            {
                lock (_lock)
                {
                    _name = value;
                }
            }
        }

        // VenderID
        private ushort _vid = 0x0000;
        public ushort Vid
        {
            get
            {
                lock (_lock)
                {
                    return _vid;
                }
            }
            set
            {
                lock (_lock)
                {
                    _vid = value;
                }
            }
        }

        // ProductID
        private ushort _pid = 0x0000;
        public ushort Pid
        {
            get
            {
                lock (_lock)
                {
                    return _pid;
                }
            }
            set
            {
                lock (_lock)
                {
                    _pid = value;
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

        // Bulk transfer Timeout in millisecond 
        private uint _BULK_USB3_TIMEOUT = 1000;
        public uint BULK_USB3_TIMEOUT
        {
            get
            {
                lock (_lock)
                {
                    return _BULK_USB3_TIMEOUT;
                }
            }
            set
            {
                lock (_lock)
                {
                    _BULK_USB3_TIMEOUT = value;
                }
            }
        }

        // FIFO size in PL27A7 USB device is dependent on the firmware branch the customer got
        private int _BULK_EP1_FIFO_SIZE = 512;
        public int BULK_EP1_FIFO_SIZE
        {
            get
            {
                lock (_lock)
                {
                    return _BULK_EP1_FIFO_SIZE;
                }
            }
            set
            {
                lock (_lock)
                {
                    _BULK_EP1_FIFO_SIZE = value;
                }
            }
        }

        // 重写 ToString 方法
        public override string ToString()
        {
            lock (_lock) // 加锁，确保多个变量读取时的一致性
            {
                return $"名称/VID/PID = [{Name}/0x{Pid:X}/0x{Vid:X}] 从设备获取 = [{FromDevice}], " +
                       $"地址 = [In: BulkInAddress={BulkInAddress}, Out:BulkOutAddress={BulkOutAddress}], " +
                       $"接口番号 = [{BulkInterfaceNo}]";
            }
        }
    }
}
