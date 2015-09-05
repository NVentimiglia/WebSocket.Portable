using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Interfaces;

namespace WebSocket.Portable.Internal
{
    internal static class DataLayerExtensions
    {
        public static Task<byte[]> ReadAsync(this IDataLayer layer, int length, CancellationToken cancellationToken)
        {
            return layer.ReadAsync(length, cancellationToken, WebSocketErrorCode.CloseInvalidData);
        }

        public static async Task<byte[]> ReadAsync(this IDataLayer layer, int length, CancellationToken cancellationToken, WebSocketErrorCode errorCode)
        {
            var buffer = new byte[length];
            var read = await layer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (read != buffer.Length)
                throw new WebSocketException(errorCode);

            return buffer;
        }
    }
}
