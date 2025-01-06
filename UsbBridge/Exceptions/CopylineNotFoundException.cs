using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Exceptions
{
    internal class CopylineNotFoundException: Exception
    {
        public CopylineNotFoundException(string message) : base(message) { }
    }
}
