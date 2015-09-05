using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocket.Portable.Interfaces
{
    public interface IDataLayer : IDisposable
    {
        /// <summary>
        /// Writes data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        Task WriteAsync(byte[] buffer, int offset, int length);

        /// <summary>
        /// Writes data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken);

        /// <summary>
        /// Reads data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        Task<int> ReadAsync(byte[] buffer, int offset, int length);

        /// <summary>
        /// Reads data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken);

        /// <summary>
        /// Reads a line asynchronous.
        /// </summary>
        /// <returns></returns>
        Task<string> ReadLineAsync();

        /// <summary>
        /// Reads a line asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<string> ReadLineAsync(CancellationToken cancellationToken);
    }
}
