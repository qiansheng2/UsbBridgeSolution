using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Exceptions
{
    internal class PacketMismatchException: Exception
    {
        public PacketMismatchException(string message) : base(message) { }
    }
}
