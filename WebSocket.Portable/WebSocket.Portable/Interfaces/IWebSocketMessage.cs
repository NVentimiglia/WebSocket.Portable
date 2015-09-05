using System.Collections.Generic;

namespace WebSocket.Portable.Interfaces
{
    public interface IWebSocketMessage : IEnumerable<IWebSocketFrame>
    {
        int FrameCount { get; }
        bool IsBinaryData { get; }
        bool IsComplete { get; }
        bool IsText { get; }
    }
}