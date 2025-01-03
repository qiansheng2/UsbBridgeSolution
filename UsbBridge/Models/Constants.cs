using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    internal class Constants
    {
        #region 系统运行常量
        public const int THREAD_SWITCH_SLEEP_TIME = 1000;   // 线程切换等待常量
        public const int ACK_TIMEOUT_MS = 10000;            // 发送数据后，等待ack返回的时间
        #endregion

        #region 数据包常量
        public const int VER1 = 1;
        public const int ContentMaxLength = 968;
        public const int PACKET_MIN_SIZE = 56;
        public const int PACKET_MAX_SIZE = 1024;
        #endregion

        #region LIBUSB常量
        public const byte LIBUSB_ENDPOINT_DIR_MASK = 0x80;
        public const byte LIBUSB_ENDPOINT_IN = 0x80;
        public const byte LIBUSB_ENDPOINT_OUT = 0x00;
        #endregion

        #region 设备常量
        #endregion

        #region 消息常量
        #endregion
    }
}
