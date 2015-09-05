using System;
using System.Diagnostics;

namespace WebSocket.Portable.Compression
{
    internal class CopyEncoder
    {
        // padding for copy encoder formatting
        //  - 1 byte for header
        //  - 4 bytes for len, nlen
        private const int PaddingSize = 5;

        // max uncompressed deflate block size is 64K.
        private const int MaxUncompressedBlockSize = 65536;

        // null input means write an empty payload with formatting info. This is needed for the final block.
        public void GetBlock(DeflateInput input, OutputBuffer output, bool isFinal)
        {
            Debug.Assert(output != null);
            Debug.Assert(output.FreeBytes >= PaddingSize);

            // determine number of bytes to write
            var count = 0;
            if (input != null)
            {

                // allow space for padding and bits not yet flushed to buffer
                count = Math.Min(input.Count, output.FreeBytes - PaddingSize - output.BitsInBuffer);

                // we don't expect the output buffer to ever be this big (currently 4K), but we'll check this
                // just in case that changes.
                if (count > MaxUncompressedBlockSize - PaddingSize)
                {
                    count = MaxUncompressedBlockSize - PaddingSize;
                }
            }

            // write header and flush bits
            if (isFinal)
            {
                output.WriteBits(FastEncoderStatics.BFinalNoCompressionHeaderBitCount, FastEncoderStatics.BFinalNoCompressionHeader);
            }
            else
            {
                output.WriteBits(FastEncoderStatics.NoCompressionHeaderBitCount, FastEncoderStatics.NoCompressionHeader);
            }

            // now we're aligned
            output.FlushBits();

            // write len, nlen
            WriteLenNLen((ushort) count, output);
            if (input == null || count <= 0) 
                return;

            // write uncompressed bytes            
            output.WriteBytes(input.Buffer, input.StartIndex, count);
            input.ConsumeBytes(count);
        }

        private static void WriteLenNLen(ushort len, OutputBuffer output)
        {

            // len
            output.WriteUInt16(len);

            // nlen
            var onesComp = (ushort) (~len);
            output.WriteUInt16(onesComp);
        }
    }
}