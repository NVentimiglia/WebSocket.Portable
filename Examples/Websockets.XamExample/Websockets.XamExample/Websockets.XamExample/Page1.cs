using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using WebSocket.Portable;
using WebSocket.Portable.Interfaces;
using Xamarin.Forms;

namespace Websockets.XamExample
{
    public class Page1 : ContentPage
    {
        private Label mlbl;

        public Page1()
        {
            mlbl = new Label { Text = "Hello ContentPage" };

            Content = new StackLayout
            {

                Children =
                {
                    mlbl
                }
            };

            Tests();
        }


        private async Task Tests()
        {
            // socket Test
            await TestRda();

            // Websocket Test
            await TestWebsocketPortable();

           // await TestBigString();
        }


        private async Task TestRda()
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
            mlbl.Text = "Tested Rda";
        }


        private async Task TestWebsocketPortable()
        {
            var client = new WebSocketClient();
            client.Opened += websocket_Opened;
            client.Closed += websocket_Closed;
            client.MessageReceived += websocket_MessageReceived;
            client.Error += Client_Error;
            
            await client.OpenAsync("ws://echo.websocket.org");
            

            await client.SendAsync("Hello World");

            await client.SendAsync("Hello World2");

            await Task.Delay(1000);

            await client.CloseAsync();

            await Task.Delay(100);

            Debug.WriteLine("TestWebsocketPortable");
        }

        private async Task TestBigString()
        {
            var client = new WebSocketClient();
            client.Opened += websocket_Opened;
            client.Closed += websocket_Closed;
            client.MessageReceived += websocket_MessageReceived;
            client.Error += Client_Error;

            //Never Ending
            await client.OpenAsync("ws://echo.websocket.org");
            

            var sb = new StringBuilder();
            for (int i = 0;i < 1000;i++)
            {
                sb.Append("A");
            }

            for (int i = 0; i < 5; i++)
            {
                await client.SendAsync(sb.ToString());

            }
            await Task.Delay(1000);

            await client.CloseAsync();

            await Task.Delay(100);

            Debug.WriteLine("TestWebsocketPortable");
        }

        private void Client_Error(Exception obj)
        {

            Debug.WriteLine("Error "+obj.Message);
        }

        private void websocket_MessageReceived(IWebSocketMessage obj)
        {
            Debug.WriteLine(obj.ToString());
            mlbl.Text = obj.ToString();
        }

        private void websocket_Closed()
        {
            Debug.WriteLine("websocket_Closed");
            mlbl.Text = "websocket_Closed";
        }

        private void websocket_Opened()
        {
            Debug.WriteLine("websocket_Opened");
            mlbl.Text = "websocket_Opened";
        }
    }
}