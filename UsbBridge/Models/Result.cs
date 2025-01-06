using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isc.Yft.UsbBridge.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; } // 表示操作是否成功
        public T Data { get; set; }         // 操作成功时返回的数据
        public int ErrorCode { get; set; }  // 错误码
        public string ErrorMessage { get; set; } // 错误信息

        // 静态方法快速创建成功或失败结果
        public static Result<T> Success(T data) => new Result<T> { IsSuccess = true, Data = data };
        public static Result<T> Failure(int errorCode, string errorMessage) =>
            new Result<T> { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
    }
}
