using System;
using System.Security.Cryptography;

namespace Isc.Yft.UsbBridge.Utils
{
    internal class Sha256DigestUtil
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 使用 SHA-256 计算指定数据的摘要（Base64 编码）。
        /// </summary>
        /// <param name="data">要计算摘要的原始字节数组</param>
        /// <returns>Base64 编码的 SHA-256 摘要字符串</returns>
        public static string ComputeSha256Digest(byte[] data)
        {
            // 1. 参数校验
            if (data == null || data.Length == 0)
            {
                // 在生产环境中可换成记录日志 + 抛出自定义异常
                throw new ArgumentException("待计算摘要的数据不能为空或 null。");
            }

            try
            {
                // 2. 创建 SHA256 实例
                //    在 .NET 6 或 .NET Framework 4.6+ 都可以使用 SHA256.Create()
                using (SHA256 sha256 = SHA256.Create())
                {
                    // 3. 计算哈希值（返回 32 字节）
                    byte[] hashBytes = sha256.ComputeHash(data);

                    // 4. 将哈希值转为 Base64（也可选择转 Hex）
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (CryptographicException ce)
            {
                // 捕获加密类异常，理论上少见，但仍要考虑
                Logger.Error("[Error] 计算 SHA-256 摘要时出现加密异常：" + ce.Message);
                throw;  // 或者记录日志后重抛，防止信息丢失
            }
            catch (Exception ex)
            {
                // 捕获其他未知异常
                Logger.Error("[Error] 计算 SHA-256 摘要时出现异常：" + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 校验给定数据与提供的 Base64 摘要值是否一致。
        /// </summary>
        /// <param name="data">接收的原始字节数组</param>
        /// <param name="providedDigestBase64">发送端提供的 Base64 编码的 SHA-256 摘要</param>
        /// <returns>是否验证成功</returns>
        public static bool VerifySha256Digest(byte[] data, string providedDigestBase64)
        {
            // 1. 参数校验
            if (data == null || data.Length == 0)
            {
                Logger.Warn("[Warning] 被验证的原始数据为空或 null。");
                return false;
            }
            if (string.IsNullOrWhiteSpace(providedDigestBase64))
            {
                Logger.Warn("[Warning] 提供的摘要值为空或 null。");
                return false;
            }

            try
            {
                // 2. 先计算当前 data 的摘要
                string currentDigest = ComputeSha256Digest(data);

                // 3. 与传入的摘要进行比较
                //    如果二者相等，则说明数据在传输过程中未被篡改
                return string.Equals(currentDigest, providedDigestBase64, StringComparison.Ordinal);
            }
            catch (FormatException fe)
            {
                // 如果 providedDigestBase64 解码失败，或其他格式问题
                Logger.Error("[Error] 验证摘要时出现格式异常：" + fe.Message);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("[Error] 验证摘要时出现异常：" + ex.Message);
                return false;
            }
        }
    }
}

