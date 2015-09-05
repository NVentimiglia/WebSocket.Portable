using WebSocket.Portable.Resources;

namespace WebSocket.Portable
{
    public enum WebSocketErrorCode
    {
        None = 10000,
        HandshakeInvalidStatusCode = 10001,
        HandshakeInvalidSecWebSocketAccept = 10002,
        HandshakeVersionNotSupported = 10003,

        
        // Close-Frame-Status-Codes as defined in http://tools.ietf.org/html/rfc6455#section-7.4.1
        CloseNormal = 1000,
        CloseGoingAway = 1001,
        CloseProtocolError = 1002,
        CloseInvalidData = 1003,
        CloseReserved = 1004,
        CloseNoCode = 1005,
        CloseNoCloseReceived = 1006,
        CloseInconstistentData = 1007,
        ClosePolicyValidation = 1008,
        CloseMessageTooBig = 1009,
        CloseExtensionsMissing = 1010,
        CloseUnexpectedCondition = 1011,
        CloseTlsError = 1015,

        // Reserved-Status-Codes as defined in http://tools.ietf.org/html/rfc6455#section-7.4.2
        // 0-999

        //   Status codes in the range 0-999 are not used.

        // 1000-2999

        //   Status codes in the range 1000-2999 are reserved for definition by
        //   this protocol, its future revisions, and extensions specified in a
        //   permanent and readily available public specification.

        // 3000-3999

        //   Status codes in the range 3000-3999 are reserved for use by
        //   libraries, frameworks, and applications.  These status codes are
        //   registered directly with IANA.  The interpretation of these codes
        //   is undefined by this protocol.

        // 4000-4999

        //   Status codes in the range 4000-4999 are reserved for private use
        //   and thus can't be registered.  Such codes can be used by prior
        //   agreements between WebSocket applications.  The interpretation of
        //   these codes is undefined by this protocol.
    }

    internal static class WebSocketErrorCodeExtensions
    {
        public static string GetDescription(this WebSocketErrorCode errorCode)
        {
            var description = ErrorCodes.ResourceManager.GetString(errorCode.ToString());
            if (!string.IsNullOrEmpty(description))
                return description;
            description = errorCode.ToString();
            return description;
        }
    }
}
