using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using WebSocket.Portable.Interfaces;
using WebSocket.Portable.Internal;
using WebSocket.Portable.Resources;

namespace WebSocket.Portable
{
    public class WebSocketRequestHandshake : HttpRequestMessage
    {
        private WebSocketRequestHandshake(HttpMethod method, IEnumerable<string> protocol)
        {
            this.Upgrade = "websocket";
            this.Connection = "Upgrade";
            this.SecWebSocketVersion = Consts.SupportedClientVersions[0];
            this.SecWebSocketKey = WebSocketHelper.CreateClientKey();
            if (protocol != null)
                this.SecWebSocketProtocol = protocol.ToArray();
            
            this.Method = method;
        }

        public WebSocketRequestHandshake(Uri uri, IEnumerable<string> protocol = null)
            : this(uri, uri, protocol) { }

        public WebSocketRequestHandshake(HttpMethod method, Uri uri, IEnumerable<string> protocol = null)
            : this(method, uri, uri, protocol) { }

        public WebSocketRequestHandshake(Uri uri, Uri originUri, IEnumerable<string> protocol = null)
            : this(HttpMethod.Get, uri, originUri, protocol) { }

        public WebSocketRequestHandshake(HttpMethod method, Uri uri, Uri originUri, IEnumerable<string> protocol = null)
            : this(method, protocol)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException(ErrorMessages.NotAnAbsoluteUri, "uri");
            if (originUri == null)
                throw new ArgumentNullException("originUri");
            if (!originUri.IsAbsoluteUri)
                throw new ArgumentException(ErrorMessages.NotAnAbsoluteUri, "originUri");   
            
            // uri
            this.RequestUri = new Uri(uri.AbsolutePath + uri.Query, UriKind.Relative);
            this.Host = uri.Host;

            // orgin
            var scheme = originUri.Scheme == Consts.SchemeHttp || originUri.Scheme == Consts.SchemeWs
                ? Consts.SchemeHttp : originUri.Scheme == Consts.SchemeHttps || originUri.Scheme == Consts.SchemeWss
                ? Consts.SchemeHttps : null;
            if (scheme == null)
                throw new ArgumentException(ErrorMessages.InvalidScheme + originUri.Scheme, "originUri");

            var origin = new StringBuilder();
            origin.AppendFormat("{0}://{1}", scheme, originUri.Host);
            if (scheme == Consts.SchemeHttp && originUri.Port != 80 || scheme == Consts.SchemeHttps && originUri.Port != 443)
                origin.AppendFormat("{0}", originUri.Port);
            origin.Append(originUri.AbsolutePath);
            
            this.Origin = origin.ToString();            
        }

        public string Connection
        {
            get
            {
                return this.Headers.Connection.FirstOrDefault();
            }
            set
            {
                this.Headers.Connection.Clear();
                if (!string.IsNullOrEmpty(value))
                    this.Headers.Connection.Add(value);
            }
        }

        public string Host
        {
            get { return this.Headers.Host; }
            set { this.Headers.Host = value; }
        }

        public string Origin
        {
            get { return this.GetHeader(Consts.HeaderOrigin); }
            set { this.SetHeader(Consts.HeaderOrigin, value); }
        }

        public IList<string> SecWebSocketExtensions
        {
            get { return this.GetHeaders(Consts.HeaderSecWebSocketExtensions); }
            set { this.SetHeaders(Consts.HeaderSecWebSocketExtensions, value); }
        }

        public string SecWebSocketKey
        {
            get { return this.GetHeader(Consts.HeaderSecWebSocketKey); }
            set { this.SetHeader(Consts.HeaderSecWebSocketKey, value); }
        }

        public IList<string> SecWebSocketProtocol
        {
            get { return this.GetHeaders(Consts.HeaderSecWebSocketProtocol); }
            set { this.SetHeaders(Consts.HeaderSecWebSocketProtocol, value); }
        }

        public string SecWebSocketVersion
        {
            get { return this.GetHeader(Consts.HeaderSecWebSocketVersion); }
            set { this.SetHeader(Consts.HeaderSecWebSocketVersion, value); }
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

        public void AddExtension(IWebSocketExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException("extension");

            var value = new StringBuilder(extension.Name);
            if (extension.Parameter != null)
            {
                value.AppendFormat("; {0}", extension.Parameter.Key);
                if (!string.IsNullOrWhiteSpace(extension.Parameter.Value))
                {
                    var param = extension.Parameter.Value;
                    var fmt = param.IndexOfAny(Consts.ExtensionParmeterValueNeedQuotesChars) < 0 || param.StartsAndEndsWith("\"")
                        ? "={0}"
                        : "=\"{0}\"";
                    value.AppendFormat(fmt, param);
                }
                    
            }
            this.AddHeader(Consts.HeaderSecWebSocketExtensions, value.ToString());
        }

        private void AddHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            var headers = this.GetHeaders(key) ?? new List<string>();
            headers.Add(value);
            this.SetHeaders(key, headers);
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            // TODO: Proxy support
            sb.AppendFormat("{0} {1} HTTP/{2}\r\n",
                this.Method.Method, this.RequestUri, this.Version);
            foreach (var header in this.Headers)
            {
                sb.AppendFormat("{0}: ", header.Key);
                var valueCount = 0;
                foreach (var value in header.Value)
                {
                    if (valueCount > 0)
                        sb.Append(", ");
                    sb.Append(value);
                    ++valueCount;
                }
                sb.Append("\r\n");
            }
            sb.Append("\r\n");

            return sb.ToString();
        }
    }
}