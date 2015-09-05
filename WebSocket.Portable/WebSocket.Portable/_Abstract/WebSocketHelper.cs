using System;
using WebSocket.Portable.Resources;

namespace WebSocket.Portable
{
    internal static class WebSocketHelper
    {
        private static readonly Random _rnd = new Random();

        public static string CreateClientKey()
        {
            var bytes = new byte[16];
            lock (_rnd)
                _rnd.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static byte[] CreateMaskingKey()
        {
            var bytes = new byte[4];
            lock (_rnd)
                _rnd.NextBytes(bytes);
            return bytes;
        }

        public static Uri CreateWebSocketUri(string urlString)
        {
            if (string.IsNullOrEmpty(urlString))
                throw new ArgumentException(ErrorMessages.MustNotBeNullOrEmpty, "urlString");


            var uri = new Uri(urlString, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException(ErrorMessages.NotAnAbsoluteUri, "urlString");

            var scheme = uri.Scheme;
            if (scheme != "ws" && scheme != "wss")
                throw new ArgumentException(ErrorMessages.InvalidScheme + scheme, "urlString");

            var fragment = uri.Fragment;
            if (fragment.Length > 0)
                throw new ArgumentException(ErrorMessages.MustNotContainAFragment, "urlString");


            var port = uri.Port;
            if (port == 0)
            {
                port = scheme == "ws" ? 80 : 443;
                var url = String.Format("{0}://{1}:{2}{3}{4}", scheme, uri.Host, port, uri.LocalPath, uri.Query);
                uri = new Uri(url);
            }

            return uri;
        }

        public static void Compress(byte[] buffer, int offset, int length)
        {
            //DeflateStream
        }        


    }
}
