using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class CommandFormat
    {
        public string Command { get; set; } // 命令
        public int Timeout { get; set; }    // 命令执行的超时时间（毫秒）
    }
}
