using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebSocket.Portable.Internal;
using WebSocket.Portable.Resources;
using WebSocket.Portable.Tasks;

namespace WebSocket.Portable
{
    public class WebSocketResponseHandshake : HttpResponseMessage, ICanLog
    {
        public static WebSocketResponseHandshake Parse(IList<string> responseLines)
        {
            if (responseLines == null || responseLines.Count < 1)
                throw new ArgumentException(ErrorMessages.NoHeaderLines, "responseLines");

            var responseLine = responseLines[0].Split(' ');
            if (responseLine.Length < 3)
                throw new ArgumentException(ErrorMessages.InvalidResponseLine + responseLines[0], "responseLines");

            var response = new WebSocketResponseHandshake
            {
                StatusCode = (HttpStatusCode)Convert.ToInt32(responseLine[1]),
                ReasonPhrase = string.Join(" ", responseLine.Skip(2)),
                Version = new Version(responseLine[0].Substring(5)), // "HTTP/x.x"            
            };            

            foreach (var line in responseLines.Skip(1))
            {
                if (string.IsNullOrEmpty(line))
                    break;

                var pos = line.IndexOf(':');
                if (pos < 0)
                    continue;

                var key = line.Substring(0, pos).Trim().ToLowerInvariant();
                var value = line.Substring(pos + 1).Trim();
                var values = value.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0);

                if (key == "date")
                {
                    response.Headers.Add(key, value);
                }
                else if (key.StartsWith("content-") || key == "expires" || key == "last-modified")
                {
                    if (response.Content == null)
                        response.Content = new CustomHttpContent();
                    if (key == "expires")
                    {
                        int offset;
                        if (int.TryParse(value, out offset)) // "0" or "-1"
                            response.Content.Headers.Add(key, DateTime.UtcNow.AddSeconds(offset).ToString("R"));
                        else
                            response.Content.Headers.Add(key, value);
                    }
                    else
                    {
                        response.Content.Headers.Add(key, values);
                    }                    
                }
                else
                {
                    try
                    {
                        response.Headers.Add(key, values);
                    }
                    catch
                    {
                        try
                        {
                            response.Headers.Add(key, value);
                        }
                        catch (Exception ex)
                        {
                            response.LogWarning("Failed to add header '{0}': {1}", key, ex);
                        }
                    }
                }
            }


            return response;
        }

        public string SecWebSocketAccept
        {
            get { return this.GetHeader(Consts.HeaderSecWebSocketAccept); }
            set { this.SetHeader(Consts.HeaderSecWebSocketAccept, value); }
        }

        public IList<string> SecWebSocketProtocol
        {
            get { return this.GetHeaders(Consts.HeaderSecWebSocketProtocol); }
            set { this.SetHeaders(Consts.HeaderSecWebSocketProtocol, value); }
        }

        public IList<string> SecWebSocketVersion
        {
            get { return this.GetHeaders(Consts.HeaderSecWebSocketVersion); }
            set { this.SetHeaders(Consts.HeaderSecWebSocketVersion, value); }
        }

        public string Upgrade
        {
            get
            {
                var upgrade = this.Headers.Upgrade.FirstOrDefault();
                return upgrade != null ? upgrade.Name : null;
            }
            set
            {
                this.Headers.Upgrade.Clear();
                if (!string.IsNullOrEmpty(value))
                    this.Headers.Upgrade.Add(new ProductHeaderValue(value));
            }
        }

        private string GetHeader(string key)
        {
            IEnumerable<string> values;
            return !this.Headers.TryGetValues(key, out values) ? null : values.FirstOrDefault();
        }

        private IList<string> GetHeaders(string key)
        {
            IEnumerable<string> values;
            return !this.Headers.TryGetValues(key, out values) ? null : values.ToList();
        }

        private void SetHeader(string key, string value)
        {
            this.Headers.Remove(key);
            if (!string.IsNullOrEmpty(value))
                this.Headers.Add(key, value);
        }

        private void SetHeaders(string key, IEnumerable<string> values)
        {
            this.Headers.Remove(key);
            if (values == null)
                return;

            foreach (var value in values)
                this.Headers.Add(key, value);
        }

        private class CustomHttpContent : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return TaskAsyncHelper.Empty;                
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
