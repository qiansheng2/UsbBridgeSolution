using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Exceptions
{
    internal class UsbCopylineNotFoundException: Exception
    {
        public UsbCopylineNotFoundException(string message) : base(message) { }
    }
}
