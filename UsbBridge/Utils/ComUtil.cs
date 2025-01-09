using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Utils
{
    internal static class ComUtil
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

        /// <summary>
        /// 扩充或截取byte数组到规定长度
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] Resize(this byte[] source, uint length)
        {
            if (source == null) return new byte[length];
            byte[] result = new byte[length];
            Array.Copy(source, result, Math.Min(source.Length, length));
            return result;
        }

        /// <summary>
        /// 截取source，最大长度不超过规定的长度
        /// </summary>
        /// <param name="source"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static byte[] Truncate(this byte[] source, int maxLength)
        {
            // 如果 source 为 null，返回空数组
            if (source == null) return new byte[0];

            // 如果 source 的长度超过 maxLength，截取数组；否则返回原数组
            if (source.Length > maxLength)
            {
                byte[] truncated = new byte[maxLength];
                Array.Copy(source, truncated, maxLength);
                return truncated;
            }

            return source;
        }
    }
}
