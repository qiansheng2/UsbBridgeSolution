using Isc.Yft.UsbBridge.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// 执行指定的命令行，并返回执行结果。
        /// </summary>
        /// <param name="command">要执行的命令。</param>
        /// <param name="timeout">命令的超时时间（毫秒）。</param>
        /// <returns>返回命令执行的标准输出结果字符串。</returns>
        public static string ExecuteCommand(string command, int timeout)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("命令不能为空或仅包含空白字符。", nameof(command));
            }

            // 初始化结果变量
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            if (timeout == 0)
            {
                // 如果Timeout时间设置为0，则为最大等待时间
                timeout = Constants.PROCESS_MAX_EXECUTE_MS;
            }

            try
            {
                using (Process process = new Process())
                {
                    // 设置命令行进程信息
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",                  // 使用 cmd.exe 执行命令
                        Arguments = $"/c {command}",          // /c 表示执行完命令后关闭
                        RedirectStandardOutput = true,        // 重定向标准输出
                        RedirectStandardError = true,         // 重定向标准错误
                        UseShellExecute = false,              // 不使用操作系统外壳程序
                        CreateNoWindow = true,                // 不创建窗口
                        StandardOutputEncoding = Encoding.UTF8, // 确保输出是 UTF-8 编码
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    // 订阅输出和错误流的事件
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) output.AppendLine(args.Data);
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) error.AppendLine(args.Data);
                    };

                    // 启动进程
                    process.Start();

                    // 异步读取标准输出和错误流
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 等待命令执行完成或超时
                    if (!process.WaitForExit(timeout))
                    {
                        // 如果超时则终止进程
                        process.Kill();
                        throw new TimeoutException($"命令执行超时，命令: {command}");
                    }

                    // 检查进程的退出码
                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException(
                            $"命令执行失败，退出码: {process.ExitCode}, 错误信息: {error.ToString().Trim()}");
                    }

                    // 返回标准输出结果
                    return output.ToString().Trim();
                }
            }
            catch (TimeoutException tex)
            {
                // 捕获超时异常
                Console.Error.WriteLine($"[CommandExecutor] 超时: {tex.Message}");
                throw;
            }
            catch (InvalidOperationException iex)
            {
                // 捕获命令执行失败异常
                Console.Error.WriteLine($"[CommandExecutor] 命令执行失败: {iex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // 捕获所有其他异常
                Console.Error.WriteLine($"[CommandExecutor] 未知错误: {ex.Message}");
                throw new InvalidOperationException($"执行命令时发生未知错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取libusb的错误信息
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <returns>错误信息</returns>
        public static string get_libusb_error_name(int errorCode)
        {
            string errMsg;
            IntPtr errorNamePtr = LibusbInterop.libusb_error_name(errorCode);
            if (errorNamePtr != IntPtr.Zero)
            {
                // 将非托管指针转换为托管字符串
                errMsg = Marshal.PtrToStringAnsi(errorNamePtr);
            }
            else
            {
                errMsg = "未知的libusb错误";
            }
            return errMsg;
        }
    }
}
