﻿using System;
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

        /// <summary>
        /// 将字节数组转换为连续的二进制字符串。
        /// 例如，{0x83, 0x9C} 将被转换为 "1000001110011100"
        /// </summary>
        /// <param name="bytes">要转换的字节数组。</param>
        /// <returns>表示字节数组的二进制字符串。</returns>
        public static string ByteArrayToBinaryString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            return string.Concat(bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        }
    }
}
