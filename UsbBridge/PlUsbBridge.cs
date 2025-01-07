using Isc.Yft.UsbBridge.Exceptions;
using Isc.Yft.UsbBridge.Models;
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
        private PlUsbBridgeManager _manager;

        public PlUsbBridge()
        {
            _manager = new PlUsbBridgeManager();
            // 订阅 Manager 的致命错误事件
            _manager.FatalErrorOccurred += Manager_FatalErrorOccurred;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            _manager.Initialize();
            _manager.StartThreads();
        }

        public void Restart()
        {
            //   - 释放bridge资源
            //   - 重启桥接
            Stop();
            _manager = new PlUsbBridgeManager();
            _manager.FatalErrorOccurred += Manager_FatalErrorOccurred;
            Start();
        }
        public void Stop()
        {
            _manager.Dispose();
        }

        private void Manager_FatalErrorOccurred(object sender, InvalidHardwareException ex)
        {
            Console.WriteLine("[PIUsbBridge] 收到Manager的InvalidHardwareException: " + ex.Message);

            // 让上层 UI 提示用户

            // 重启
            Restart();
        }

        public async Task<Result<string>> SendBigData(EPacketOwner owner, byte[] data)
        {
            Result<string> ret = await _manager.SendBigData(owner, data);
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
