using System.Text;

namespace WebSocket.Portable.Interfaces
{
    public interface IWebSocketPayload
    {
        byte[] Data { get; }

        int Offset { get; }

        int Length { get; }

        bool IsBinary { get; }

        bool IsText { get; }
    }

    public static class WebSocketPayloadExtensions
    {
        public static string GetText(this IWebSocketPayload payload)
        {
            if (payload == null || !payload.IsText)
                return null;

            return payload.Data == null 
                ? string.Empty 
                : Encoding.UTF8.GetString(payload.Data, payload.Offset, payload.Length);
        }
    }
}