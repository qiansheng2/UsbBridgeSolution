using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class DataAckPacket : Packet
    {
        private DataAckPacket():base()
        { }

        public DataAckPacket(byte version,
                            EPacketOwner owner,
                            uint totalCount,
                            uint index,
                            uint totalLength,
                            uint contentLength,
                            byte[] messageId,
                            byte[] reserved,
                            byte[] content) :
        base(version, owner, EPacketType.DATA_ACK, totalCount, index, totalLength,
            contentLength, messageId, reserved, content)
        {
        }
        public DataAckPacket(DataPacket dataPacket) :
        base(dataPacket.Version, dataPacket.Owner, EPacketType.DATA_ACK, dataPacket.TotalCount, 
            dataPacket.Index, dataPacket.TotalLength, 0, 
            dataPacket.MessageId, dataPacket.Reserved, Encoding.UTF8.GetBytes(""))
        {
        }

        public DataAckPacket Create(DataPacket dataPacket)
        {
            DataAckPacket dataAckPacket = new DataAckPacket
            {
                Version = dataPacket.Version,
                Owner = dataPacket.Owner,
                Type = EPacketType.DATA_ACK,
                TotalCount = 1,
                Index = 1,
                TotalLength = dataPacket.TotalLength,
                ContentLength = dataPacket.ContentLength,
                MessageId = dataPacket.MessageId,
                Reserved = dataPacket.Reserved,
                Content = Encoding.UTF8.GetBytes("")
            };

            return dataAckPacket;
        }

    }
}
