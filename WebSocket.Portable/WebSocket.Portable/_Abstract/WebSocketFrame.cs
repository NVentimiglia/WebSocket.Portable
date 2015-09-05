using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Interfaces;
using WebSocket.Portable.Internal;
using WebSocket.Portable.Resources;

namespace WebSocket.Portable
{
    internal abstract class WebSocketFrame : IWebSocketFrame
    {
        public bool IsFin { get; set; }

        public bool IsMasked { get; protected set; }

        public bool IsRsv1 { get; set; }

        public bool IsRsv2 { get; set; }

        public bool IsRsv3 { get; set; }

        /// <summary>
        /// Gets a value indicating whether this frame is a control frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a control frame; otherwise, <c>false</c>.
        /// </value>
        public bool IsControlFrame
        {
            get { return this.Opcode.IsControl(); }
        }

        /// <summary>
        /// Gets a value indicating whether this frame is a data frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a data frame; otherwise, <c>false</c>.
        /// </value>
        public bool IsDataFrame
        {
            get { return this.Opcode.IsData(); }
        }

        public bool IsBinaryData
        {
            get { return this.Opcode == WebSocketOpcode.Binary; }
        }

        public bool IsTextData
        {
            get { return this.Opcode == WebSocketOpcode.Text; }
        }

        /// <summary>
        /// Gets a value indicating whether this ftame is a fragment.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a fragment; otherwise, <c>false</c>.
        /// </value>
        public bool IsFragment
        {
            get { return this.Opcode == WebSocketOpcode.Continuation && !this.IsFin; }
        }

        /// <summary>
        /// Gets a value indicating whether this frame is the first fragment in a series of fragments.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is the first fragment; otherwise, <c>false</c>.
        /// </value>
        public bool IsFirstFragment
        {
            get { return this.Opcode != WebSocketOpcode.Continuation && !this.IsFin; }
        }

        /// <summary>
        /// Gets a value indicating whether this frame is the last fragment in a series of fragments.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is the last fragment; otherwise, <c>false</c>.
        /// </value>
        public bool IsLastFragment
        {
            get { return this.Opcode == WebSocketOpcode.Continuation && this.IsFin; }
        }

        /// <summary>
        /// Gets the masking key.
        /// </summary>
        /// <value>
        /// The masking key.
        /// </value>
        public byte[] MaskingKey { get; protected set; }

        /// <summary>
        /// Gets the opcode.
        /// </summary>
        /// <value>
        /// The opcode.
        /// </value>
        public WebSocketOpcode Opcode { get; set; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        public IWebSocketPayload Payload { get; set; }


        byte[] DoRead(TcpConnection con, int bytes)
        {
            var headerBytes = new byte[bytes];
            con._client.ReadStream.Read(headerBytes, 0, bytes);
            return headerBytes;
        }
		    
        public async Task ReadFromAsync(IDataLayer layer, CancellationToken cancellationToken)
        {
            var headerBytes = await layer.ReadAsync(2, cancellationToken);
            this.IsFin = (headerBytes[0] & 0x80) == 0x80;
            this.IsRsv1 = (headerBytes[0] & 0x40) == 0x40;
            this.IsRsv2 = (headerBytes[0] & 0x20) == 0x20;
            this.IsRsv3 = (headerBytes[0] & 0x10) == 0x10;
            this.Opcode = (WebSocketOpcode)(headerBytes[0] & 0x0f);

            if (this.IsControlFrame && !this.IsFin)
                throw new WebSocketException(WebSocketErrorCode.CloseInvalidData, ErrorMessages.FragmentedControlFrame);

            if (!this.IsDataFrame && this.IsRsv1)
                throw new WebSocketException(WebSocketErrorCode.CloseInvalidData, ErrorMessages.CompressedNonDataFrame);

            this.IsMasked = (headerBytes[1] & 0x80) == 0x80;
            
            if (this.IsMasked)
                this.MaskingKey = await layer.ReadAsync(4, cancellationToken);
            else
                this.MaskingKey = null;

            ulong payloadLength = (byte)(headerBytes[1] & 0x7f);
            if (this.IsControlFrame && payloadLength > 125)
                throw new WebSocketException(WebSocketErrorCode.CloseInconstistentData, ErrorMessages.PayloadLengthControlFrame);

            
            var extendedPayloadLengthSize = payloadLength < 126 ? 0 : payloadLength < 127 ? 2 : 8;
            if (extendedPayloadLengthSize > 0)
            {
                var extendedPayloadLength = await layer.ReadAsync(extendedPayloadLengthSize, cancellationToken);
                payloadLength = extendedPayloadLengthSize == 2
                    ? extendedPayloadLength.ToUInt16(ByteOrder.BigEndian)
                    : extendedPayloadLength.ToUInt64(ByteOrder.BigEndian);

                if (payloadLength > int.MaxValue)
                    throw new WebSocketException(WebSocketErrorCode.CloseMessageTooBig);
            }

            if (payloadLength > 0)
            {
                this.Payload = new WebSocketPayload(this, await layer.ReadAsync((int)payloadLength, cancellationToken));
                if (this.IsMasked)
                {
                    this.MaskPayload(
                        this.Payload.Data, this.Payload.Offset, this.Payload.Length,
                        this.Payload.Data, this.Payload.Offset);
                }
            }
            else
            {
                this.Payload = null;
            }
        }

        public async Task WriteToAsync(IDataLayer layer, CancellationToken cancellationToken)
        {
            int payloadLength;
            byte[] extendedPayloadLength;

            var payload = this.Payload ?? new WebSocketPayload(this);
            if (payload.Length < 126)
            {
                payloadLength = payload.Length;
                extendedPayloadLength = null;
            }
            // Extended payload (16bit)
            else if (payload.Length < 65536)
            {
                payloadLength = 126;
                extendedPayloadLength = ((ushort)payload.Length).ToByteArray(ByteOrder.BigEndian);
            }
            // Extended payload (64bit)
            else
            {
                payloadLength = 127;
                extendedPayloadLength = ((long)payload.Length).ToByteArray(ByteOrder.BigEndian);
            }

            var header = this.IsFin.ToBit();
            header = (header << 1) + this.IsRsv1.ToBit();
            header = (header << 1) + this.IsRsv2.ToBit();
            header = (header << 1) + this.IsRsv3.ToBit();
            header = (header << 4) + (int)this.Opcode;
            header = (header << 1) + this.IsMasked.ToBit();
            header = (header << 7) + payloadLength;

            var headerBytes = ((ushort)header).ToByteArray(ByteOrder.BigEndian);
            await layer.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);

            if (extendedPayloadLength != null)
                await layer.WriteAsync(extendedPayloadLength, 0, extendedPayloadLength.Length, cancellationToken);

            if (this.IsMasked)
            {
                await layer.WriteAsync(this.MaskingKey, 0, this.MaskingKey.Length, cancellationToken);

                // try to keep the memory footprint to a minimum
                var offset = 0;
                var buffer = new byte[Math.Min(payload.Length, 1024 * this.MaskingKey.Length)];
                while (offset < payload.Length)
                {
                    var count = Math.Min(buffer.Length, payload.Length - offset);
                    this.MaskPayload(payload.Data, payload.Offset + offset, count, buffer, 0);
                    await layer.WriteAsync(buffer, 0, count, cancellationToken);
                    offset += count;
                }
            }
            else
            {
                await layer.WriteAsync(payload.Data, payload.Offset, payload.Length, cancellationToken);
            }
        }


        private void MaskPayload(IList<byte> input, int inputOffset, int inputLength, IList<byte> output, int outputOffset)
        {
            for (var i = 0;i < inputLength;i++)
                output[i + outputOffset] = (byte)(input[i + inputOffset] ^ this.MaskingKey[i % this.MaskingKey.Length]);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (this.IsControlFrame)
            {
                sb.AppendFormat("Control Frame {0}", this.Opcode);
            }
            else if (this.IsDataFrame)
            {
                sb.AppendFormat("Data Frame {0}", this.Opcode);
                if (this.IsBinaryData)
                {
                    sb.AppendFormat(", Length {0} bytes", this.Payload == null ? 0 : this.Payload.Length);
                }
                else if (this.IsTextData)
                {
                    sb.AppendFormat(", '{0}'", this.Payload == null ? string.Empty : this.Payload.GetText());
                }
                if (this.IsFirstFragment)
                {
                    sb.Append(", first frame in a series of fragments.");
                }
            }
            else if (this.Opcode == WebSocketOpcode.Continuation)
            {
                sb.Append("Continuation Frame");
                if (this.IsLastFragment)
                {
                    sb.Append(", last frame in a series of fragments");
                }
            }
            else
            {
                sb.AppendFormat("Error: Unknown opcode: {0}", this.Opcode);
            }

            return sb.ToString();
        }
    }
}
