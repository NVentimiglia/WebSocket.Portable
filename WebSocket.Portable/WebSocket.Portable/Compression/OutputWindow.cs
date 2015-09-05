using System;
using System.Diagnostics;

namespace WebSocket.Portable.Compression
{
    internal class OutputWindow
    {
        private const int WindowSize = 32768;
        private const int WindowMask = 32767;

        private readonly byte[] _window = new byte[WindowSize]; //The window is 2^15 bytes
        private int _end;       // this is the position to where we should write next byte 

        // Add a byte to output window
        public void Write(byte b)
        {
            Debug.Assert(this.AvailableBytes < WindowSize, "Can't add byte when window is full!");
            _window[_end++] = b;
            _end &= WindowMask;
            ++AvailableBytes;
        }

        public void WriteLengthDistance(int length, int distance)
        {
            Debug.Assert((AvailableBytes + length) <= WindowSize, "Not enough space");

            // move backwards distance bytes in the output stream, 
            // and copy length bytes from this position to the output stream.
            this.AvailableBytes += length;
            var copyStart = (_end - distance) & WindowMask;  // start position for coping.

            var border = WindowSize - length;
            if (copyStart <= border && _end < border)
            {
                if (length <= distance)
                {
                    Array.Copy(_window, copyStart, _window, _end, length);
                    _end += length;
                }
                else
                {
                    // The referenced string may overlap the current
                    // position; for example, if the last 2 bytes decoded have values
                    // X and Y, a string reference with <length = 5, distance = 2>
                    // adds X,Y,X,Y,X to the output stream.
                    while (length-- > 0)
                    {
                        _window[_end++] = _window[copyStart++];
                    }
                }
            }
            else
            { // copy byte by byte
                while (length-- > 0)
                {
                    _window[_end++] = _window[copyStart++];
                    _end &= WindowMask;
                    copyStart &= WindowMask;
                }
            }
        }

        // Copy up to length of bytes from input directly.
        // This is used for uncompressed block.
        public int CopyFrom(InputBuffer input, int length)
        {
            length = Math.Min(Math.Min(length, WindowSize - this.AvailableBytes), input.AvailableBytes);
            int copied;

            // We might need wrap around to copy all bytes.
            var tailLen = WindowSize - _end;
            if (length > tailLen)
            {
                // copy the first part     
                copied = input.CopyTo(_window, _end, tailLen);
                if (copied == tailLen)
                {
                    // only try to copy the second part if we have enough bytes in input
                    copied += input.CopyTo(_window, 0, length - tailLen);
                }
            }
            else
            {
                // only one copy is needed if there is no wrap around.
                copied = input.CopyTo(_window, _end, length);
            }

            _end = (_end + copied) & WindowMask;
            this.AvailableBytes += copied;
            return copied;
        }

        // Free space in output window
        public int FreeBytes
        {
            get { return WindowSize - this.AvailableBytes; }
        }

        // bytes not consumed in output window
        public int AvailableBytes { get; private set; }

        // copy the decompressed bytes to output array.        
        public int CopyTo(byte[] output, int offset, int length)
        {
            int copyEnd;

            if (length > this.AvailableBytes)
            {   // we can copy all the decompressed bytes out
                copyEnd = _end;
                length = this.AvailableBytes;
            }
            else
            {
                copyEnd = (_end - this.AvailableBytes + length) & WindowMask;  // copy length of bytes
            }

            var copied = length;

            var tailLen = length - copyEnd;
            if (tailLen > 0)
            {    // this means we need to copy two parts seperately
                // copy tailLen bytes from the end of output window
                System.Array.Copy(_window, WindowSize - tailLen, output, offset, tailLen);
                offset += tailLen;
                length = copyEnd;
            }
            
            Array.Copy(_window, copyEnd - length, output, offset, length);
            this.AvailableBytes -= copied;
            Debug.Assert(this.AvailableBytes >= 0, "check this function and find why we copied more bytes than we have");
            return copied;
        }
    }
}
