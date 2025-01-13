using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class CommandPacket : Packet
    {
        public CommandPacket(byte version,
              EPacketOwner owner,
              uint totalCount,
              uint index,
              uint totalLength,
              uint contentLength,
              byte[] messageId,
              byte[] reserved,
              byte[] content) :
        base(version, owner, EPacketType.CMD, totalCount, index, totalLength,
             contentLength, messageId, reserved, content)
        {
        }

    }
}
