using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Threading;
using NLog;

namespace Isc.Yft.UsbBridge.Handler
{
    /// <summary>
    /// 工厂类，用于管理不同类型的包处理器
    /// </summary>
    internal static class PacketHandlerFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // 存储包类型与处理器的映射
        private static readonly Dictionary<EPacketType, IPacketHandler> Handlers = new Dictionary<EPacketType, IPacketHandler>();

        /// <summary>
        /// 静态构造函数，用于初始化默认处理器
        /// </summary>
        static PacketHandlerFactory()
        {
        }

        /// <summary>
        /// 注册处理器
        /// </summary>
        /// <param name="packetType">包类型</param>
        /// <param name="handler">处理器实例</param>
        public static void RegisterHandler(EPacketType packetType, IPacketHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "处理器不能为空。");
            }

            lock (Handlers)
            {
                if (Handlers.ContainsKey(packetType))
                {
                    Logger.Warn($"包类型 {packetType} 的处理器已存在，将覆盖现有处理器。");
                }

                Handlers[packetType] = handler;
                Logger.Info($"包类型 {packetType} 的处理器已成功注册。");
            }
        }

        /// <summary>
        /// 获取处理器
        /// </summary>
        /// <param name="packetType">包类型</param>
        /// <returns>处理器实例</returns>
        public static IPacketHandler GetHandler(EPacketType packetType)
        {
            lock (Handlers)
            {
                if (Handlers.TryGetValue(packetType, out var handler))
                {
                    return handler;
                }

                Logger.Warn($"[PacketHandlerFactory] 未找到包类型 {packetType} 的处理器。返回默认处理器。");
                return new DefaultPacketHandler(); // 返回默认处理器
            }
        }
    }
}
