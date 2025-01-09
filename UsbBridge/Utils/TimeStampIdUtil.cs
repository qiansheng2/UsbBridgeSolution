using System;
using NLog;

namespace Isc.Yft.UsbBridge.Utils
{
    internal class TimeStampIdUtil
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static long _lastTimestamp = -1; // 记录上一次的时间戳（毫秒）
        private static int _sequence = 0;       // 同一毫秒内的自增序号(3位)
        private static readonly object _lock = new object(); // 用于线程同步

        /// <summary>
        /// 生成带时间戳的ID（13位UTC时间戳+3位自增序号），例如：1693567201456000
        /// </summary>
        /// <returns>带时间戳的ID（13位UTC时间戳+3位自增序号）</returns>
        public static string GenerateId()
        {
            lock (_lock) // 保证线程安全
            {
                long currentTimestamp = GetCurrentTimestamp();

                // 如果当前时间戳和上次相同，递增序号
                if (currentTimestamp == _lastTimestamp)
                {
                    _sequence++;

                    // 如果序号超过 999，则等待下一毫秒
                    if (_sequence > 999)
                    {
                        currentTimestamp = WaitForNextTimestamp(_lastTimestamp);
                        _sequence = 0; // 序号重置
                    }
                }
                else
                {
                    // 如果是新的时间戳，重置序号
                    _sequence = 0;
                    _lastTimestamp = currentTimestamp;
                }

                // 返回带时间戳的唯一 ID
                string id = $"{currentTimestamp}{_sequence:D3}";
                Logger.Debug($"Generated ID: {id}");
                return id; // 序号填充为 3 位
            }
        }

        private static long GetCurrentTimestamp()
        {
            // 获取当前时间的时间戳（毫秒）
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static long WaitForNextTimestamp(long lastTimestamp)
        {
            long timestamp = GetCurrentTimestamp();
            while (timestamp <= lastTimestamp)
            {
                timestamp = GetCurrentTimestamp();
            }
            return timestamp;
        }
    }
}