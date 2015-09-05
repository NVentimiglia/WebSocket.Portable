using System;

namespace WebSocket.Portable
{
    public class WebSocketException : Exception
    {
        private readonly WebSocketErrorCode _errorCode;

        public WebSocketException(WebSocketErrorCode code)
            : this(code, (string)null)
        {
            _errorCode = code;
        }

        public WebSocketException(WebSocketErrorCode code, string message)
            : base(message ?? code.GetDescription())
        {
            _errorCode = code;
        }

        public WebSocketException(WebSocketErrorCode code, Exception innerException)
            : this(code, null, innerException)
        {
            _errorCode = code;
        }

        public WebSocketException(WebSocketErrorCode code, string message, Exception innerException)
            : base(message ?? code.GetDescription(), innerException)
        {
            _errorCode = code;
        }

        public WebSocketErrorCode ErrorCode
        {
            get { return _errorCode; }            
        }
    }
}
