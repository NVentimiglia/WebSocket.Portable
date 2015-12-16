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
            var read = 0;

            while (read < length && !cancellationToken.IsCancellationRequested)
            {
                var chunkOffset = read;
                var chunkLength = length - chunkOffset;
                var chunkSize = await layer.ReadAsync(buffer, chunkOffset, chunkLength, cancellationToken);

                if (chunkSize == 0)
                {
                    break;
                }

                read += chunkSize;
            }

            if (read != buffer.Length)
                throw new WebSocketException(errorCode);

            return buffer;
        }
    }
}
