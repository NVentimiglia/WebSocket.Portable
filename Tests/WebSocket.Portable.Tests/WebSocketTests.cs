using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSocket.Portable.Tests.Internal;

namespace WebSocket.Portable.Tests
{
    [TestClass]
    public class WebSocketTests
    {
        private WebSocket _target;

        [TestInitialize]
        public void Initialize()
        {
            _target = new WebSocket();
        }

        [TestMethod]
        public void ConnectAsync()
        {
            AsyncPump.Run(() => _target.ConnectAsync("ws://echo.websocket.org"));
        }

        [TestMethod]
        public void SendHandshakeAsync()
        {
            AsyncPump.Run(() => _target.ConnectAsync("ws://echo.websocket.org"));
            AsyncPump.Run(() => _target.SendHandshakeAsync());
        }

        [TestMethod]
        public void ValidateHandshakeAsync()
        {
            AsyncPump.Run(() => _target.ConnectAsync("ws://echo.websocket.org"));
            var response = AsyncPump.Run(() => _target.SendHandshakeAsync());

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.SwitchingProtocols, response.StatusCode, "Unexpected status code");
        }
    }
}
