using Isc.Yft.UsbBridge.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class DataPacket : Packet
    {
        public DataPacket():base()
        {
            Type = EPacketType.DATA;
        }

        public DataPacket(byte version,
              EPacketOwner owner,
              uint totalCount,
              uint index,
              uint totalLength,
              uint contentLength,
              byte[] messageId,
              byte[] reserved,
              byte[] content) : 
        base(version, owner, EPacketType.DATA, totalCount, index, totalLength, 
             contentLength, messageId, reserved, content)
        { 
        }
    }
}
