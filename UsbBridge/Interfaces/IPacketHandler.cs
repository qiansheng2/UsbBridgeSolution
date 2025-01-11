using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Interfaces
{
    internal interface IPacketHandler
    {
        Task<Result<String>> Handle(Packet packet);
    }
}
