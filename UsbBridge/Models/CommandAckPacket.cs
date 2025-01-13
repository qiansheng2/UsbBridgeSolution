using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Utils;

namespace Isc.Yft.UsbBridge.Models
{
    internal class CommandAckPacket : Packet
    {
        public CommandAckPacket(
                byte version,
                EPacketOwner owner,
                uint totalCount,
                uint index,
                uint totalLength,
                uint contentLength,
                byte[] messageId,
                byte[] reserved,
                byte[] content ) :
        base(version, owner, EPacketType.CMD_ACK, totalCount, index, totalLength,
             contentLength, messageId, reserved, content)
        {
        }

        /// <summary>
        /// 生成Command的AckPacket
        /// </summary>
        /// <param name="originalPacket">命令数据包</param>
        /// <param name="commandResult">命令执行结果</param>
        /// <returns></returns>
        public static CommandAckPacket CreateAck(Packet originalPacket, String commandResult)
        {
            byte[] resultBytes = ComUtil.Truncate(Encoding.UTF8.GetBytes(commandResult), Constants.CONTENT_MAX_SIZE);

            CommandAckPacket ack = new CommandAckPacket
            (
                originalPacket.Version,
                originalPacket.Owner,
                1,
                1,
                (uint)resultBytes.Length,
                (uint)resultBytes.Length,
                originalPacket.MessageId,
                new byte[16],
                resultBytes
            );

            return ack;
        }
    }
}
