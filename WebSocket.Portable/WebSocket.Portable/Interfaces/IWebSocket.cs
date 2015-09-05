using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocket.Portable.Interfaces
{
    public interface IWebSocket : IDisposable
    {
        /// <summary>
        /// Adds an extension to this websocket instance.
        /// </summary>
        /// <param name="extension">The extension.</param>
        void RegisterExtension(IWebSocketExtension extension);

        /// <summary>
        /// Closes the socket asynchronous.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync(WebSocketErrorCode errorCode);
        
        /// <summary>
        /// Connects asynchronous.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="port">80, 443, whatever</param>
        /// <returns></returns>
        Task ConnectAsync(string uri, int port, bool useSSL);

        /// <summary>
        /// Connects asynchronous.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="port">80, 443, whatever</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Cannot connect because current state is  + _state</exception>
        Task ConnectAsync(string uri, int port, bool useSSl, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the default handshake asynchronous.
        /// </summary>
        /// <returns></returns>
        Task<WebSocketResponseHandshake> SendHandshakeAsync();

        /// <summary>
        /// Sends the default handshake asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<WebSocketResponseHandshake> SendHandshakeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the handshake asynchronous.
        /// </summary>
        /// <param name="handshake">The handshake.</param>
        /// <returns></returns>
        Task<WebSocketResponseHandshake> SendHandshakeAsync(WebSocketRequestHandshake handshake);

        /// <summary>
        /// Sends the handshake asynchronous.
        /// </summary>
        /// <param name="handshake">The handshake.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<WebSocketResponseHandshake> SendHandshakeAsync(WebSocketRequestHandshake handshake, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a frame asynchronous.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns></returns>
        Task SendFrameAsync(IWebSocketFrame frame);

        /// <summary>
        /// Sends a frame asynchronous.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SendFrameAsync(IWebSocketFrame frame, CancellationToken cancellationToken);

        /// <summary>
        /// Receives a frame asynchronous.
        /// </summary>
        /// <returns></returns>
        Task<IWebSocketFrame> ReceiveFrameAsync();

        /// <summary>
        /// Receives a frame asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IWebSocketFrame> ReceiveFrameAsync(CancellationToken cancellationToken);
    }
}