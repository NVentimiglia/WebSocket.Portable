using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocket.Portable.Interfaces;

namespace WebSocket.Portable
{
    internal class WebSocketMessage : IWebSocketMessage
    {
        private readonly List<IWebSocketFrame> _frames;

        public WebSocketMessage()
        {
            _frames = new List<IWebSocketFrame>();
        }

        public int FrameCount
        {
            get { return _frames.Count; }
        }

        public bool IsBinaryData
        {
            get { return this.FrameCount > 0 && _frames.First().IsBinaryData; }
        }

        public bool IsComplete
        {
            get { return this.FrameCount > 0 && _frames.Last().IsFin; }
        }

        public bool IsText
        {
            get { return this.FrameCount > 0 && _frames.First().IsTextData; }
        }

        public void AddFrame(IWebSocketFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            if (this.IsComplete)
                throw new InvalidOperationException("Message already complete.");

            _frames.Add(frame);
        }

        public IEnumerator<IWebSocketFrame> GetEnumerator()
        {
            return _frames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (!this.IsText)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var frame in _frames)
            {
                if (frame.Payload == null || frame.Payload.Length == 0)
                    continue;                
                var text = Encoding.UTF8.GetString(frame.Payload.Data, frame.Payload.Offset, frame.Payload.Length);
                sb.Append(text);
            }

            return sb.ToString();
        }
    }
}
