using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Interfaces;

namespace WebSocket.Portable
{
    public class WebSocket : WebSocketBase
    {
        internal TcpConnection InnerConnection;
        protected override async Task<ITcpConnection> ConnectAsyncInternal(string host, int port, bool useSsl, CancellationToken cancellationToken)
        {
            InnerConnection = new TcpConnection(useSsl);
            await InnerConnection.ConnectAsync(host, port, cancellationToken);
            return InnerConnection;
        }
    }
}