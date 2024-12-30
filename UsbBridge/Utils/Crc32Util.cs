using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Utils
{
    /// <summary>
    /// 提供对字节数组进行 CRC-32 计算与验证的工具类。
    /// </summary>
    public static class Crc32Util
    {
        // CRC-32 表 (0xEDB88320 多项式), 预先生成以提高性能
        private static readonly uint[] _crcTable = new uint[256];

        static Crc32Util()
        {
            // 生成 CRC 表
            const uint polynomial = 0xEDB88320u;
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    // 如果最低位为1，则与多项式异或, 否则右移
                    if ((crc & 1) == 1)
                        crc = polynomial ^ (crc >> 1);
                    else
                        crc >>= 1;
                }
                _crcTable[i] = crc;
            }
        }

        /// <summary>
        /// 计算指定数组的整个区间[0..data.Length]的 CRC-32。
        /// </summary>
        /// <param name="data">待计算的字节数组</param>
        /// <returns>返回计算得到的 CRC 值</returns>
        /// <exception cref="ArgumentNullException">当 data 为 null 时抛出</exception>
        public static uint ComputeCrc32(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return ComputeCrc32(data, 0, data.Length);
        }

        /// <summary>
        /// 计算数组中指定片段的 CRC-32。
        /// </summary>
        /// <param name="data">源字节数组</param>
        /// <param name="offset">计算起始偏移</param>
        /// <param name="length">计算的字节数量</param>
        /// <returns>返回计算得到的 CRC 值</returns>
        /// <exception cref="ArgumentNullException">当 data 为 null 时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当 offset/length 超出范围时抛出</exception>
        public static uint ComputeCrc32(byte[] data, int offset, int length)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException("offset/length 超出 data 数组范围。");

            // 初始化寄存器(全1)
            uint crc = 0xFFFFFFFFu;

            for (int i = offset; i < offset + length; i++)
            {
                // 与当前字节异或后取表
                byte b = data[i];
                uint index = (crc ^ b) & 0xFF;
                crc = _crcTable[index] ^ (crc >> 8);
            }

            // 取反得到最终结果
            return crc ^ 0xFFFFFFFFu;
        }

        /// <summary>
        /// 验证指定数组整个区间的 CRC-32 是否与期望值相符。
        /// </summary>
        /// <param name="data">待校验的字节数组</param>
        /// <param name="expectedCrc">期望的 CRC-32 值</param>
        /// <returns>若计算值与期望值相同，返回 true，否则 false</returns>
        public static bool ValidateCrc32(byte[] data, uint expectedCrc)
        {
            if (data == null)
                return false; // 或者抛异常, 视需求而定

            uint actual = ComputeCrc32(data, 0, data.Length);
            return (actual == expectedCrc);
        }

        /// <summary>
        /// 验证数组指定片段的 CRC-32 是否与期望值相符。
        /// </summary>
        /// <param name="data">源字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">要计算的字节数</param>
        /// <param name="expectedCrc">期望的 CRC-32</param>
        /// <returns>若计算值与期望值相同，返回 true，否则 false</returns>
        public static bool ValidateCrc32(byte[] data, int offset, int length, uint expectedCrc)
        {
            // 若需要更严格的错误处理,可加 try-catch
            // 这里仅简单地做参数判断, 返回true/false

            if (data == null) return false;
            if (offset < 0 || length < 0 || offset + length > data.Length) return false;

            uint actual = ComputeCrc32(data, offset, length);
            return (actual == expectedCrc);
        }

        /// <summary>
        /// 尝试计算 CRC-32，不抛出异常，而是通过返回值指示成功或失败。
        /// </summary>
        /// <param name="data">源字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">要计算的字节数</param>
        /// <param name="crc">输出计算结果</param>
        /// <returns>若成功计算，返回 true；若参数无效或发生异常，返回 false</returns>
        public static bool TryComputeCrc32(byte[] data, int offset, int length, out uint crc)
        {
            crc = 0;
            if (data == null || offset < 0 || length < 0 || offset + length > data.Length)
                return false;

            try
            {
                crc = ComputeCrc32(data, offset, length);
                return true;
            }
            catch
            {
                // 捕获所有异常, 返回 false
                return false;
            }
        }
    }
}
