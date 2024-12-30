﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    /// <summary>本机所在位置</summary>
    public enum USBPosition : int
    {
        // 外网用
        OUTSIDE = 1,
        // 内网用
        INSIDE = 2
    }

    /// <summary>单块数据包拥有者</summary>
    public enum PacketOwner : byte
    {
        // 外网数据包
        OUTERNET = 1,
        // 内网数据包
        INTRANNET = 2
    }

    /// <summary>单块数据包类型</summary>
    public enum PacketType : byte
    {
        // 业务包头packet
        HEAD = 1,
        // 数据包packet
        DATA = 2,
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
