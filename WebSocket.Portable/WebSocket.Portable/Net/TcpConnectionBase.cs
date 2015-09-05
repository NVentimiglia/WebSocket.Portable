using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Interfaces;

namespace WebSocket.Portable.Net
{
    public abstract class TcpConnectionBase : ITcpConnection
    {

        /// <summary>
        /// Gets a value indicating whether this tcp connection is secure.
        /// </summary>
        /// <value>
        /// <c>true</c> if this tcp connection is secure; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsSecure { get; }
        
        /// <summary>
        /// Sends data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public Task WriteAsync(byte[] buffer, int offset, int length)
        {
            return this.WriteAsync(buffer, offset, length, CancellationToken.None);
        }

        /// <summary>
        /// Sends data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public abstract Task WriteAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken);

        /// <summary>
        /// Receives data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public Task<int> ReadAsync(byte[] buffer, int offset, int length)
        {
            return ReadAsync(buffer, offset, length, CancellationToken.None);
        }

        /// <summary>
        /// Receives data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public abstract Task<int> ReadAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken);

        /// <summary>
        /// Receives a line asynchronous.
        /// </summary>
        /// <returns></returns>
        public Task<string> ReadLineAsync()
        {
            return this.ReadLineAsync(CancellationToken.None);
        }

        /// <summary>
        /// Receives a line asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public abstract Task<string> ReadLineAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }
    }
}
