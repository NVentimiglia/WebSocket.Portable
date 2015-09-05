using System;

namespace WebSocket.Portable.Interfaces
{
    public interface IWebSocketExtension : IComparable<IWebSocketExtension>
    {
        string Name { get; }

        IWebSocketExtensionParameter Parameter { get; }
    }
}
