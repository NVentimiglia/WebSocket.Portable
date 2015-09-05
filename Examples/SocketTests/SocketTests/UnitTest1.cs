using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSocket.Portable;
using WebSocket.Portable.Interfaces;

namespace SocketTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestRDASockets()
        {
            var socket = new Sockets.Plugin.TcpSocketClient();

            await socket.ConnectAsync("echo.websocket.org", 80, false);

            // Send HS
            var handshake =
                "GET / HTTP/1.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Version: 13\r\nSec-WebSocket-Key: p2z/MFplfpRzjsVywqRQTg==\r\nHost: echo.websocket.org\r\nOrigin: http://echo.websocket.org/\r\n\r\n";
            var bytes = Encoding.UTF8.GetBytes(handshake);

            await socket.WriteStream.FlushAsync();
            await socket.WriteStream.WriteAsync(bytes, 0, bytes.Length);

            // Read HS, Never Ending
            var b = socket.ReadStream.ReadByte();

            Debug.WriteLine("TestRda");
        }


        public static int counter = 0;

        [TestMethod]
        public async Task TestWebsocketPortable()
        {
            counter = 0;

            var client = new WebSocketClient();
            client.Opened += websocket_Opened;
            client.Closed += websocket_Closed;
            client.MessageReceived += websocket_MessageReceived;

            //Never Ending
            await client.OpenAsync("ws://echo.websocket.org");

            await client.SendAsync("Hello World");

            await client.SendAsync("Hello World2");

            await Task.Delay(500);

            Assert.IsTrue(counter == 2);

            Debug.WriteLine("TestWebsocketPortable");
        }

        void websocket_MessageReceived(IWebSocketMessage obj)
        {
            Debug.WriteLine(obj.ToString());
            counter++;
        }

        void websocket_Closed()
        {
            Debug.WriteLine("websocket_Closed");
        }

        void websocket_Opened()
        {
            Debug.WriteLine("websocket_Opened");
        }
    }
}
