using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Interfaces;
using WebSocket.Portable.Internal;
using WebSocket.Portable.Resources;
using WebSocket.Portable.Security;
using WebSocket.Portable.Tasks;

namespace WebSocket.Portable
{
    public abstract class WebSocketBase : ICanLog, IWebSocket
    {
        private readonly List<IWebSocketExtension> _extensions;
        private Uri _uri;
        private int _state;
        private ITcpConnection _tcp;

        /// <summary>
        /// Prevents a default instance of the <see cref="WebSocketBase"/> class from being created.
        /// </summary>
        protected WebSocketBase()
        {
            _extensions = new List<IWebSocketExtension>();
            _state = WebSocketState.Closed;
        }

        public void RegisterExtension(IWebSocketExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException("extension");

            if (_extensions.Contains(extension))
                throw new ArgumentException(ErrorMessages.ExtensionsAlreadyRegistered + extension.Name, "extension");

            var oldState = Interlocked.CompareExchange(ref _state, _state, _state);
            if (oldState != WebSocketState.Closed)
                throw new InvalidOperationException(ErrorMessages.InvalidState + _state);

            _extensions.Add(extension);
        }

        public virtual Task CloseAsync(WebSocketErrorCode errorCode)
        {
            _state = WebSocketState.Closed;
            _tcp.Dispose();
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Connects asynchronous.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="port"></param>
        /// <returns></returns>
        public Task ConnectAsync(string uri,int port, bool useSSL)
        {
            return this.ConnectAsync(uri, port, useSSL, CancellationToken.None);
        }

        /// <summary>
        /// Connects asynchronous.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="port"></param>
        /// <param name="useSSL"></param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Cannot connect because current state is  + _state</exception>
        public async Task ConnectAsync(string uri, int port, bool useSSL, CancellationToken cancellationToken)
        {
            Debug.WriteLine("Websockets.Portable:WebsocketBase ConnectAsync");
            var oldState = Interlocked.CompareExchange(ref _state, WebSocketState.Connecting, WebSocketState.Closed);
            if (oldState != WebSocketState.Closed)
                throw new InvalidOperationException(ErrorMessages.InvalidState + _state);

            if (uri == null)
                throw new ArgumentNullException("uri");

            _uri = WebSocketHelper.CreateWebSocketUri(uri);
            
            _tcp = await this.ConnectAsyncInternal(_uri.DnsSafeHost, port, useSSL, cancellationToken);
            Interlocked.Exchange(ref _state, WebSocketState.Connected);
        }

        /// <summary>
        /// Connects asynchronous.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="useSsl">if set to <c>true</c> [use SSL].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        protected abstract Task<ITcpConnection> ConnectAsyncInternal(string host, int port, bool useSsl, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the default handshake asynchronous.
        /// </summary>
        /// <returns></returns>
        public Task<WebSocketResponseHandshake> SendHandshakeAsync()
        {
            return this.SendHandshakeAsync(CancellationToken.None);
        }

        /// <summary>
        /// Sends the default handshake asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<WebSocketResponseHandshake> SendHandshakeAsync(CancellationToken cancellationToken)
        {
            var handshake = new WebSocketRequestHandshake(_uri);
            foreach (var extension in _extensions)
                handshake.AddExtension(extension);

            return this.SendHandshakeAsync(handshake, cancellationToken);
        }

        /// <summary>
        /// Sends the handshake asynchronous.
        /// </summary>
        /// <param name="handshake">The handshake.</param>
        /// <returns></returns>
        public Task<WebSocketResponseHandshake> SendHandshakeAsync(WebSocketRequestHandshake handshake)
        {
            return this.SendHandshakeAsync(handshake, CancellationToken.None);
        }

        /// <summary>
        /// Sends the handshake asynchronous.
        /// </summary>
        /// <param name="handshake">The handshake.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<WebSocketResponseHandshake> SendHandshakeAsync(WebSocketRequestHandshake handshake, CancellationToken cancellationToken)
        {
            var oldState = Interlocked.CompareExchange(ref _state, WebSocketState.Opening, WebSocketState.Connected);
            if (oldState != WebSocketState.Connected)
                throw new InvalidOperationException(ErrorMessages.InvalidState + _state);

            var data = handshake.ToString();
            await this.SendAsync(data, Encoding.UTF8, cancellationToken);

            var responseHeaders = new List<string>();
            var line = await _tcp.ReadLineAsync(cancellationToken);
            while (!String.IsNullOrEmpty(line))
            {
                responseHeaders.Add(line);
                line = await _tcp.ReadLineAsync(cancellationToken);
            }

            var response = WebSocketResponseHandshake.Parse(responseHeaders);
            if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                var versions = response.SecWebSocketVersion;
                if (versions != null && !versions.Intersect(Consts.SupportedClientVersions).Any())
                    throw new WebSocketException(WebSocketErrorCode.HandshakeVersionNotSupported);

                throw new WebSocketException(WebSocketErrorCode.HandshakeInvalidStatusCode);
            }

            var challenge = Encoding.UTF8.GetBytes(handshake.SecWebSocketKey + Consts.ServerGuid);
            var hash = Sha1Digest.ComputeHash(challenge);
            var calculatedAccept = Convert.ToBase64String(hash);

            if (response.SecWebSocketAccept != calculatedAccept)
                throw new WebSocketException(WebSocketErrorCode.HandshakeInvalidSecWebSocketAccept);

            response.RequestMessage = handshake;

            Interlocked.Exchange(ref _state, WebSocketState.Open);


            return response;
        }

        public Task SendFrameAsync(IWebSocketFrame frame)
        {
            return this.SendFrameAsync(frame, CancellationToken.None);
        }

        public Task SendFrameAsync(IWebSocketFrame frame, CancellationToken cancellationToken)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            return frame.WriteToAsync(_tcp, cancellationToken);
        }

        public Task<IWebSocketFrame> ReceiveFrameAsync()
        {
            return ReceiveFrameAsync(CancellationToken.None);
        }

        public async Task<IWebSocketFrame> ReceiveFrameAsync(CancellationToken cancellationToken)
        {
            var frame = new WebSocketServerFrame();
            await frame.ReadFromAsync(_tcp, cancellationToken);
            return frame;
        }

        /// <summary>
        /// Sends data asynchronous.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task SendAsync(string data, Encoding encoding, CancellationToken cancellationToken)
        {
            var bytes = encoding.GetBytes(data);
            return SendAsync(bytes, 0, bytes.Length, cancellationToken);
        }

        /// <summary>
        /// Sends data asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task SendAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken)
        {
            return _tcp.WriteAsync(buffer, offset, length, cancellationToken);
        }

        public virtual void Dispose()
        {
            _tcp.Dispose();
        }
    }
}
