using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Threading;
using NLog;

namespace Isc.Yft.UsbBridge.Handler
{
    internal class DataAckPacketHandler : AbstractPacketHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override async Task<Result<string>> Handle(Packet packet)
        {
            Result<string> result = Result<string>.Success("处理完毕");
            return result;
        }
    }
}
