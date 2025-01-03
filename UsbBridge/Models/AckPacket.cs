using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class AckPacket : Packet
    {

        public AckPacket():base()
        { }

        /// <summary>
        /// 根据标准业务packet生成ack packet
        /// </summary>
        public AckPacket GenerateAckPacket(Packet generalPacket)
        {
            AckPacket ack = new AckPacket();
            ack.Version = generalPacket.Version;
            ack.Owner = generalPacket.Owner;
            ack.Type = EPacketType.ACK;
            ack.TotalCount = 1;
            ack.Index = generalPacket.Index;
            ack.TotalLength = 0;
            ack.ContentLength = 200;        // 内容区长度
            ack.MessageId = generalPacket.MessageId;
            ack.Content = new byte[200];    // 内容：信息
            ack.AddCRC();

            return ack;
        }
    }
}
