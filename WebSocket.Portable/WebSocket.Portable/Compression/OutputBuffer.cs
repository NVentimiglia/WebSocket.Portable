using System;
using System.Diagnostics;

namespace WebSocket.Portable.Compression
{
    internal class OutputBuffer
    {
        private byte[] _byteBuffer;  // buffer for storing bytes
        private uint _bitBuffer;        // store uncomplete bits 
        private int _bitCount;       // number of bits in bitBuffer 

        public int BytesWritten { get; private set; }

        public int FreeBytes
        {
            get { return _byteBuffer.Length - this.BytesWritten; }
        }

        public int BitsInBuffer
        {
            get { return (_bitCount / 8) + 1; }
        }

        public BufferState DumpState()
        {
            BufferState savedState;
            savedState.Pos = this.BytesWritten;
            savedState.BitBuffer = _bitBuffer;
            savedState.BitCount = _bitCount;
            return savedState;
        }

        public void RestoreState(BufferState state)
        {
            this.BytesWritten = state.Pos;
            _bitBuffer = state.BitBuffer;
            _bitCount = state.BitCount;
        }

        public void WriteUInt16(ushort value)
        {
            Debug.Assert(FreeBytes >= 2, "No enough space in output buffer!");

            _byteBuffer[this.BytesWritten++] = (byte)value;
            _byteBuffer[this.BytesWritten++] = (byte)(value >> 8);
        }

        public void WriteBits(int n, uint bits)
        {
            Debug.Assert(n <= 16, "length must be larger than 16!");
            _bitBuffer |= bits << _bitCount;
            _bitCount += n;
            if (_bitCount < 16) 
                return;

            Debug.Assert(_byteBuffer.Length - this.BytesWritten >= 2, "No enough space in output buffer!");
            _byteBuffer[this.BytesWritten++] = unchecked((byte)_bitBuffer);
            _byteBuffer[this.BytesWritten++] = unchecked((byte)(_bitBuffer >> 8));
            _bitCount -= 16;
            _bitBuffer >>= 16;
        }

        // write the bits left in the output as bytes. 
        public void FlushBits()
        {
            // flush bits from bit buffer to output buffer
            while (_bitCount >= 8)
            {
                _byteBuffer[this.BytesWritten++] = unchecked((byte)_bitBuffer);
                _bitCount -= 8;
                _bitBuffer >>= 8;
            }

            if (_bitCount <= 0) 
                return;

            _byteBuffer[this.BytesWritten++] = unchecked((byte)_bitBuffer);
            _bitBuffer = 0;
            _bitCount = 0;
        }

        public void WriteBytes(byte[] byteArray, int offset, int count)
        {
            Debug.Assert(FreeBytes >= count, "Not enough space in output buffer!");
            // faster 
            if (_bitCount == 0)
            {
                Array.Copy(byteArray, offset, _byteBuffer, BytesWritten, count);
                this.BytesWritten += count;
            }
            else
            {
                this.WriteBytesUnaligned(byteArray, offset, count);
            }
        }

        // set the output buffer we will be using
        public void UpdateBuffer(byte[] output)
        {
            _byteBuffer = output;
            this.BytesWritten = 0;
        }        

        private void WriteBytesUnaligned(byte[] byteArray, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var b = byteArray[offset + i];
                this.WriteByteUnaligned(b);
            }
        }

        private void WriteByteUnaligned(byte b)
        {
            this.WriteBits(8, b);
        }        

        internal struct BufferState
        {
            internal int Pos;            // position
            internal uint BitBuffer;        // store uncomplete bits 
            internal int BitCount;       // number of bits in bitBuffer 
        }
    }

}
