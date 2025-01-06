﻿using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge
{
    public class PlUsbBridge : Interfaces.IUsbBridge
    {
        // 简单起见，把管理逻辑放到 PlUsbBridgeManager
        private readonly PlUsbBridgeManager _manager;

        public PlUsbBridge()
        {
            _manager = new PlUsbBridgeManager();
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _manager.StartThreads();
        }

        public void Stop()
        {
            _manager.Dispose();
        }

        public Result<string> SendBigData(EPacketOwner owner, byte[] data)
        {
            Result<string> ret = _manager.SendBigData(owner, data);
            return ret;
        }

        public void SetMode(USBMode mode)
        {
            _manager.SetMode(mode);
        }

        public USBMode GetCurrentMode()
        {
            return _manager.GetCurrentMode();
        }
    }
}
