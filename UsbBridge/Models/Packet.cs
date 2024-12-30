using System;
using System.IO;
using Isc.Yft.UsbBridge.Utils;

/// <summary>
/// 单块数据 Packet 物理结构：
/// ┌────────────────┬──────────────┐
/// │ 1字节  │ 数据包版本           │ Version                    │
/// │ 1字节  │ 数据包拥有者         │ Owner                      │
/// │ 1字节  │ 数据包类型           │ Type                       │
/// │ 4字节  │ 数据包总个数         │ TotalCount                 │
/// │ 4字节  │ 当前数据包位置索引   │ Index                      │
/// │ 4字节  │ 数据包总字节数       │ TotalLength                │
/// │ 4字节  │ 当前数据包实际字节数 │ ContentLength              │
/// │ 16字节 │ 消息唯一ID           │ MessageId                  │
/// │ 16字节 │ 备用(0x00填充）      │ Reserved                   │
/// │ 969字节│ 可变长数据包内容     │ Content                    │
/// │ 4字节  │ CRC-32校验           │ Crc32                      │
/// └────────────────┴──────────────┘
/// 合计: 1+1+1+4+4+4+4+16+16备用+969（最大，可变长）+4 = 1024 字节
/// </summary>
/// 
namespace Isc.Yft.UsbBridge.Models
{
    public class Packet
    {
        /// <summary>
        /// [1字节] 数据包版本
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// [1字节] 数据包拥有者
        /// </summary>
        public PacketOwner Owner { get; set; }

        /// <summary>
        /// [1字节] 数据包类型
        /// </summary>
        public PacketType Type { get; set; }

        /// <summary>
        /// [4字节] 数据包总个数
        /// </summary>
        public uint TotalCount { get; set; }

        /// <summary>
        /// [4字节] 当前数据包位置索引
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// [4字节] 数据包总长度（大文件总字节数，或本消息总长度）
        /// </summary>
        public uint TotalLength { get; set; }

        /// <summary>
        /// [4字节] 当前数据包的“实际”长度（data的有效字节数）
        /// </summary>
        public uint ContentLength { get; set; }

        /// <summary>
        /// [16字节] 消息唯一ID（可用GUID或自定义16字节）
        /// </summary>
        public byte[] MessageId { get; set; } = new byte[16];

        /// <summary>
        /// [16字节] 备用（16字节）
        /// </summary>
        public byte[] Reserved { get; set; } = new byte[16];

        /// <summary>
        /// [可变字节数] 数据包内容，长度为0到ContentMaxLength
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// [4字节] CRC-32 校验
        /// </summary>
        public byte[] Crc32 { get; set; } = new byte[4];

        // ============== 构造函数 ==============
        public Packet() { }

        public Packet(byte version,
                      PacketOwner owner,
                      PacketType type,
                      uint totalCount,
                      uint index,
                      uint totalLength,
                      uint contentLength,
                      byte[] messageId,
                      byte[] reserved,
                      byte[] content)
        {
            Version = version;
            Owner = owner;
            Type = type;
            TotalCount = totalCount;
            Index = index;
            TotalLength = totalLength;
            ContentLength = contentLength;

            // 2. 校验并拷贝 messageId
            if (messageId != null && messageId.Length == 16)
            {
                // 注意 Array.Copy 的参数顺序 (source, destination, length)
                Array.Copy(messageId, 0, this.MessageId, 0, 16);
            }
            else
            {
                throw new ArgumentOutOfRangeException("messageId数据为空或长度不等于16！");
            }

            // 3. 校验并拷贝 reserved
            if (reserved != null && reserved.Length == 16)
            {
                Array.Copy(reserved, 0, this.Reserved, 0, 16);
            }
            else
            {
                throw new ArgumentOutOfRangeException("reserved数据为空或长度不等于16！");
            }

            // 4. 分配并拷贝 content (可变长)
            if (content == null)
                throw new ArgumentOutOfRangeException("content数据为空！");

            if (contentLength > Constants.ContentMaxLength)
                throw new ArgumentOutOfRangeException($"currentPacketLength大于长度上限({Constants.ContentMaxLength})！");

            // 根据 contentLength 分配本地 Content
            this.Content = new byte[contentLength];
            Array.Copy(content, 0, this.Content, 0, contentLength);

            // 计算 CRC
            byte[] buffer = BuildBytesForCrc(this);
            uint crcValue = Crc32Util.ComputeCrc32(buffer);
            Console.WriteLine($"CRC-32: 0x{crcValue:X8}");
            byte[] crcBytes = BitConverter.GetBytes(crcValue);
            Array.Copy(crcBytes, 0, this.Crc32, 0, 4);
        }

        /// <summary>
        /// 将 Packet 转换为二进制数组（序列化）
        /// </summary>
        public byte[] ToBytes()
        {
            // 注意：这里 ContentLength 表示内容的字节数
            // 数据总大小 = 
            //   1(Version) + 1(Owner) + 1(Type) 
            // + 4(TotalCount) + 4(Index) + 4(TotalLength) + 4(ContentLength)
            // + 16(MessageId) + 16(Reserved) + ContentLength + 4(CRC)
            byte[] buffer = new byte[1 + 1 + 1
                                   + 4 + 4 + 4 + 4
                                   + 16 + 16
                                   + ContentLength
                                   + 4];
            uint offset = 0;

            // 写 1字节版本
            buffer[offset++] = Version;

            // 写 1字节Owner
            buffer[offset++] = (byte)Owner;

            // 写 1字节Type
            buffer[offset++] = (byte)Type;

            // 写 4字节TotalCount
            Array.Copy(BitConverter.GetBytes(TotalCount), 0, buffer, offset, 4);
            offset += 4;

            // 写 4字节Index
            Array.Copy(BitConverter.GetBytes(Index), 0, buffer, offset, 4);
            offset += 4;

            // 写 4字节TotalLength
            Array.Copy(BitConverter.GetBytes(TotalLength), 0, buffer, offset, 4);
            offset += 4;

            // 写 4字节ContentLength
            Array.Copy(BitConverter.GetBytes(ContentLength), 0, buffer, offset, 4);
            offset += 4;  // 注意这里要 += 4，而不是 += ContentLength

            // 写 16字节MessageId
            Array.Copy(MessageId, 0, buffer, offset, 16);
            offset += 16;

            // 写 16字节Reserved
            Array.Copy(Reserved, 0, buffer, offset, 16);
            offset += 16;

            // 写真正的内容
            if (ContentLength > 0) 
                Array.Copy(Content, 0, buffer, offset, ContentLength);
                offset += ContentLength;

            // 写 4字节的CRC
            Array.Copy(Crc32, 0, buffer, offset, 4);
            offset += 4;

            return buffer;
        }

        /// <summary>
        /// 从读入的二进制数组中解析出Packet
        /// </summary>
        public Packet FromBytes(byte[] buf)
        {
            if (buf == null)
                throw new ArgumentException("数据为空，无法转换为数据包！");

            Packet packet = new Packet();
            uint offset = 0;

            packet.Version = buf[offset++];

            // 验证字节值是否为有效的 PacketOwner 枚举值
            byte byteValue = buf[offset++];
            if (Enum.IsDefined(typeof(PacketOwner), byteValue))
            {
                packet.Owner = (PacketOwner)byteValue;
            }
            else
            {
                throw new ArgumentOutOfRangeException("buf中的Owner字段数据不合规，无法转换为数据包！");
            }

            // 验证字节值是否为有效的 Type 枚举值
            byteValue = buf[offset++];
            if (Enum.IsDefined(typeof(PacketType), byteValue))
            {
                packet.Type = (PacketType)byteValue;
            }
            else
            {
                throw new ArgumentOutOfRangeException("buf中的Type字段数据不合规，无法转换为数据包！");
            }

            packet.TotalCount = BitConverter.ToUInt32(buf, (int)offset);
            offset += 4;

            packet.Index = BitConverter.ToUInt32(buf, (int)offset);
            offset += 4;

            packet.TotalLength = BitConverter.ToUInt32(buf, (int)offset);
            offset += 4;

            packet.ContentLength = BitConverter.ToUInt32(buf, (int)offset);
            offset += 4;
            uint len = 1 + 1 + 1 + 4 + 4 + 4 + 4 + 16 + 16 + packet.ContentLength + 4;
            if ( buf.Length != len )
                throw new ArgumentException($"数据长度不正确, 当前包的数据长度有{buf.Length}字节，应该是{len}字节，因此无法正确转换为数据包！");

            packet.MessageId = new byte[16];
            Array.Copy(buf, offset, packet.MessageId, 0, 16);
            offset += 16;

            packet.Reserved = new byte[16];
            Array.Copy(buf, offset, packet.Reserved, 0, 16);
            offset += 16;

            packet.Content = new byte[packet.ContentLength];
            Array.Copy(buf, offset, packet.Content, 0, packet.ContentLength);
            offset += packet.ContentLength;

            packet.Crc32 = new byte[4];
            Array.Copy(buf, offset, packet.Crc32, 0, 4); 
            offset += 4;

            return packet;
        }

        /// <summary>
        /// 为本数据包附加CRC校验
        /// </summary>
        public void AddCRC()
        {
            // 计算 CRC-32
            byte[] buffer = BuildBytesForCrc(this);
            uint crcValue = Crc32Util.ComputeCrc32(buffer);
            Console.WriteLine($"CRC-32: 0x{crcValue:X8}");
            byte[] crcBytes = BitConverter.GetBytes(crcValue);
            Array.Copy(crcBytes, 0, this.Crc32, 0, 4);
        }

        /// <summary>
        /// 序列化自身，用于生成crc-32
        /// </summary>
        private byte[] BuildBytesForCrc(Packet packet)
        {
            // 下面使用 MemoryStream + BinaryWriter
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // 1 字节: Version
                bw.Write(packet.Version);

                // 1 字节: Owner (是 PacketOwner 枚举, 可先强转 byte)
                bw.Write((byte)packet.Owner);

                // 1 字节: Type (同上, PacketType 枚举 -> byte)
                bw.Write((byte)packet.Type);

                // 4 字节: TotalCount
                bw.Write(packet.TotalCount);

                // 4 字节: Index
                bw.Write(packet.Index);

                // 4 字节: TotalLength
                bw.Write(packet.TotalLength);

                // 4 字节: ContentLength
                bw.Write(packet.ContentLength);

                // 16 字节: MessageId
                bw.Write(packet.MessageId);         // 确保是16字节

                // 16 字节: Reserved
                bw.Write(packet.Reserved);          // 确保是16字节

                // 可变长的内容: Content
                if (packet.ContentLength > 0)
                    bw.Write(packet.Content);       // 可变长的内容字节

                bw.Flush();
                return ms.ToArray();
            }
        }
    }
}
