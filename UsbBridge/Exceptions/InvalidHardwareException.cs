using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Exceptions
{
    public class InvalidHardwareException : Exception
    {
        public InvalidHardwareException(string message) : base(message) { }
    }
}

