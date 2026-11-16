using System;

namespace LibraryMS.Common
{
    /// <summary>业务异常：携带错误码与用户可读消息，UI 层统一捕获展示。</summary>
    public class BizException : Exception
    {
        public string Code { get; private set; }

        public BizException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public BizException(string code, string message, Exception inner)
            : base(message, inner)
        {
            Code = code;
        }
    }
}
