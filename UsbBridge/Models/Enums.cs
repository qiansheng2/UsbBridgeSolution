using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    /// <summary>单块数据包拥有者</summary>
    public enum PacketOwner : byte
    {
        OuterNet = 1,
        IntranNet = 2
    }

    /// <summary>单块数据包类型</summary>
    public enum PacketType : byte
    {
        // 业务包头packet
        HEAD = 1,
        // 业务包packet
        GENERAL = 2,
        // 业务包尾packet
        TAIL = 3,
        // ACK packet
        ACK = 4,
        // 命令 packet
        CMD = 5,
        // 心跳 packet
        HEARTBEAT = 6
    }

    /// <summary>数据传输模式</summary>
    public enum USBMode : int
    {
        // 上传模式（内网->外网）
        UPLOAD = 1,
        // 下行模式（外网->内网）
        DOWNLOAD = 2
    }

}
