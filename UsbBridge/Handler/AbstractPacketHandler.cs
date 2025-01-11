using Isc.Yft.UsbBridge.Interfaces;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Handler;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Handler
{
    internal abstract class AbstractPacketHandler : IPacketHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public abstract Task<Result<string>> Handle(Packet packet);

        // 预处理或日志记录
        protected virtual void LogHandling(Packet packet)
        {
            Logger.Info($"[PacketHandler] Handling packet of type {packet.Type}");
        }
    }
}
