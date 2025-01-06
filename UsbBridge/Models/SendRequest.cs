using System.Collections.Concurrent;
using System.Linq;
using Isc.Yft.UsbBridge.Exceptions;

namespace Isc.Yft.UsbBridge.Models
{
    /// <summary>
    /// 主线程要求发送的一次请求，
    /// 包含要发送的 Packet[] 以及一个 Acks[] 用于确认结果。
    /// </summary>
    internal class SendRequest
    {
        public Packet[] Packets { get; }

        // 使用 ConcurrentDictionary 确保线程安全
        private readonly ConcurrentDictionary<int, bool> _acks;

        public SendRequest(Packet[] packets)
        {
            Packets = packets;
            _acks = new ConcurrentDictionary<int, bool>();

            for (int i = 0; i < packets.Length; i++)
            {
                _acks[i] = false; // 初始化为 false
            }
        }

        public bool GetAck(int index)
        {
            return _acks[index];
        }

        public void SetAck(int index, bool value)
        {
            _acks[index] = value;
        }

        public void SetAck(Packet packet)
        {
            for( int i=0; i<Packets.Length; i++)
            {
                if(Packets[i].MessageId == packet.MessageId)
                {
                    // 设置位已收到Ack包
                    SetAck(i, true);
                    break;
                }
            }
            // 没有找到与ack匹配的数据包
            throw new PacketMismatchException($"收到的Packet包不符合期望，MessageId={packet.MessageId}。");
        }

        /// <summary>
        /// 检查是否所有的数据包都已确认
        /// </summary>
        /// <returns></returns>
        public bool AreAllAcksReceived()
        {
            return _acks.Values.All(v => v); // 检查所有值是否为 true
        }
    }
}