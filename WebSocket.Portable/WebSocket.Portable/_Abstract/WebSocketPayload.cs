using System;
using System.Text;
using WebSocket.Portable.Interfaces;

namespace WebSocket.Portable
{
    internal class WebSocketPayload : IWebSocketPayload
    {
        private readonly IWebSocketFrame _frame;
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _length;

        public WebSocketPayload(IWebSocketFrame frame, byte[] data = null)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");
            _frame = frame;
            _data = data;
            if (_data != null)
                _length = _data.Length;
        }

        public WebSocketPayload(IWebSocketFrame frame, string data)
            : this(frame, Encoding.UTF8.GetBytes(data ?? string.Empty)) { }

        public WebSocketPayload(IWebSocketFrame frame, byte[] data, int offset, int length)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");
            _frame = frame;
            _data = data;
            _offset = offset;
            _length = length;
        }

        public bool IsBinary
        {
            get { return _frame.Opcode == WebSocketOpcode.Binary; }
        }

        public bool IsText
        {
            get { return _frame.Opcode == WebSocketOpcode.Text; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int Offset
        {
            get { return _offset; }
        }

        public int Length
        {
            get { return _length; }
        }
    }
}
