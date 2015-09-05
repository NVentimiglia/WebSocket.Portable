using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSocket.Portable.Internal;

namespace WebSocket.Portable.Tests
{
    [TestClass]
    public class WebSocketVariousTests
    {
        [TestMethod]
        public void CreateWebSocketUriValid()
        {
            var expected = new Uri("ws://some.server.ext");

            var actual = WebSocketHelper.CreateWebSocketUri(expected.AbsoluteUri);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WebSocketDescription()
        {
            foreach (WebSocketErrorCode errorCode in Enum.GetValues(typeof(WebSocketErrorCode)))
            {
                try
                {
                    var description = errorCode.GetDescription();
                    Assert.IsFalse(string.IsNullOrEmpty(description), "Description for '{0}' is not set.", errorCode);
                    Console.WriteLine("{0}: {1}", errorCode, description);
                }
                catch (Exception)
                {
                    Assert.Fail("Description for '{0}' is not accessible.", errorCode);
                }
                
            }            
        }

        [TestMethod]
        public void CopyReverse()
        {
            var actual = Encoding.ASCII.GetBytes("abcdefg").CopyReverse();
            Assert.AreEqual("gfedcba", Encoding.ASCII.GetString(actual));

            actual = Encoding.ASCII.GetBytes("abcdefgh").CopyReverse();
            Assert.AreEqual("hgfedcba", Encoding.ASCII.GetString(actual));
        }
    }
}
