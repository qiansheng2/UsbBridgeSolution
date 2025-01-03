using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    /// <summary>
    /// 表示主线程要求发送的一次请求，
    /// 包含要发送的 Packet[] 以及一个 TaskCompletionSource 用于异步结果。
    /// </summary>
    internal class SendRequest
    {
        public Packet[] Packets { get; }
        public TaskCompletionSource<bool> Tcs { get; }

        public SendRequest(Packet[] packets)
        {
            Packets = packets;
            Tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // RunContinuationsAsynchronously => 启用，避免死锁
        }
    }
}
