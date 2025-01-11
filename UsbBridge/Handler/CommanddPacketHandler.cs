using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isc.Yft.UsbBridge.Models;
using Isc.Yft.UsbBridge.Utils;
using Isc.Yft.UsbBridge;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Isc.Yft.UsbBridge.Threading;

namespace Isc.Yft.UsbBridge.Handler
{
    internal class CommandPacketHandler : AbstractPacketHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // 用于处理收到Command包时向外部发送CommandAck包消息
        private readonly PlUsbBridgeManager _usbBridgeManager;

        public CommandPacketHandler(PlUsbBridgeManager usbBridgeManager): base() 
        {
            _usbBridgeManager = usbBridgeManager;
        }

        public override async Task<Result<string>> Handle(Packet packet)
        {
            string commandResult;
            try
            {
                string strCommandFormat = Encoding.UTF8.GetString(packet.Content);

                if (string.IsNullOrWhiteSpace(strCommandFormat))
                {
                    throw new ArgumentException("命令的 JSON 字符串不能为空。");
                }

                // 解析 JSON
                CommandFormat commandFormat = JsonConvert.DeserializeObject<CommandFormat>(strCommandFormat);

                // 验证解析结果
                if (commandFormat == null ||
                    string.IsNullOrWhiteSpace(commandFormat.Command) ||
                    commandFormat.Timeout < 0 ||
                    commandFormat.Timeout > Constants.PROCESS_MAX_EXECUTE_MS
                )
                {
                    Logger.Error($"收到了错误的命令：{commandFormat.Command},{commandFormat.Timeout}。");
                    throw new ArgumentException($"JSON 数据无效：必须包含合法的命令和正数的超时时间(0-{Constants.PROCESS_MAX_EXECUTE_MS /1000}秒)。");
                }
                commandResult = ComUtil.ExecuteCommand(commandFormat.Command, commandFormat.Timeout);
            }
            catch (JsonException ex)
            {
                commandResult = $"JSON 格式错误: {ex.Message}";
            }
            catch (Exception ex)
            {
                commandResult = $"命令执行失败: {ex.Message}";
            }

            Logger.Info($"CMD命令执行结果：[{commandResult}]。");
            CommandAckPacket commandAckPacket = CommandAckPacket.CreateAck(packet, commandResult);

            Result<string> ret = await _usbBridgeManager.SendCommandAck(commandAckPacket);
            return ret;

        }
    }
}
