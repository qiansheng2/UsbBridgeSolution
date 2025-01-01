using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Utils
{
    internal class CommonUtil
    {
        // 将字节数组转换为结构体
        public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            T structure = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return structure;
        }
    }
}
