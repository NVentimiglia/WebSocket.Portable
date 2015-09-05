using System;
using System.Diagnostics;

namespace WebSocket.Portable.Compression
{
    internal class FastEncoder
    {
        private readonly FastEncoderWindow _inputWindow; // input history window
        private readonly Match _currentMatch; // current match in history window

        public FastEncoder()
        {
            _inputWindow = new FastEncoderWindow();
            _currentMatch = new Match();
        }

        public int BytesInHistory
        {
            get { return _inputWindow.BytesAvailable; }
        }

        public DeflateInput UnprocessedInput
        {
            get { return _inputWindow.UnprocessedInput; }
        }

        public void FlushInput()
        {
            _inputWindow.FlushWindow();
        }

        public double LastCompressionRatio { get; private set; }

        // Copy the compressed bytes to output buffer as a block. maxBytesToCopy limits the number of 
        // bytes we can copy from input. Set to any value < 1 if no limit
        public void GetBlock(DeflateInput input, OutputBuffer output, int maxBytesToCopy)
        {
            Debug.Assert(InputAvailable(input), "call SetInput before trying to compress!");

            WriteDeflatePreamble(output);
            GetCompressedOutput(input, output, maxBytesToCopy);
            WriteEndOfBlock(output);
        }

        // Compress data but don't format as block (doesn't have header and footer)
        public void GetCompressedData(DeflateInput input, OutputBuffer output)
        {
            this.GetCompressedOutput(input, output, -1);
        }

        public void GetBlockHeader(OutputBuffer output)
        {
            WriteDeflatePreamble(output);
        }

        public void GetBlockFooter(OutputBuffer output)
        {
            WriteEndOfBlock(output);
        }

        // maxBytesToCopy limits the number of bytes we can copy from input. Set to any value < 1 if no limit
        private void GetCompressedOutput(DeflateInput input, OutputBuffer output, int maxBytesToCopy)
        {
            // snapshot for compression ratio stats
            var bytesWrittenPre = output.BytesWritten;
            var bytesConsumedFromInput = 0;
            var inputBytesPre = BytesInHistory + input.Count;

            do
            {
                // read more input data into the window if there is space available
                var bytesToCopy = (input.Count < _inputWindow.FreeWindowSpace)
                    ? input.Count
                    : _inputWindow.FreeWindowSpace;
                if (maxBytesToCopy >= 1)
                {
                    bytesToCopy = Math.Min(bytesToCopy, maxBytesToCopy - bytesConsumedFromInput);
                }
                if (bytesToCopy > 0)
                {
                    // copy data into history window
                    _inputWindow.CopyBytes(input.Buffer, input.StartIndex, bytesToCopy);
                    input.ConsumeBytes(bytesToCopy);
                    bytesConsumedFromInput += bytesToCopy;
                }

                this.GetCompressedOutput(output);

            } 
            while (SafeToWriteTo(output) && InputAvailable(input) && (maxBytesToCopy < 1 || bytesConsumedFromInput < maxBytesToCopy));

            // determine compression ratio, save
            var bytesWrittenPost = output.BytesWritten;
            var bytesWritten = bytesWrittenPost - bytesWrittenPre;
            var inputBytesPost = BytesInHistory + input.Count;
            var totalBytesConsumed = inputBytesPre - inputBytesPost;
            if (bytesWritten != 0)
                this.LastCompressionRatio = bytesWritten / (double)totalBytesConsumed;            

        }

        // compress the bytes in input history window
        private void GetCompressedOutput(OutputBuffer output)
        {
            while (_inputWindow.BytesAvailable > 0 && SafeToWriteTo(output))
            {

                // Find next match. A match can be a symbol, 
                // a distance/length pair, a symbol followed by a distance/Length pair
                _inputWindow.GetNextSymbolOrMatch(_currentMatch);

                if (_currentMatch.State == MatchState.HasSymbol)
                {
                    WriteChar(_currentMatch.Symbol, output);
                }
                else if (_currentMatch.State == MatchState.HasMatch)
                {
                    WriteMatch(_currentMatch.Length, _currentMatch.Position, output);
                }
                else
                {
                    WriteChar(_currentMatch.Symbol, output);
                    WriteMatch(_currentMatch.Length, _currentMatch.Position, output);
                }
            }
        }

        private bool InputAvailable(DeflateInput input)
        {
            return input.Count > 0 || BytesInHistory > 0;
        }

        private static bool SafeToWriteTo(OutputBuffer output)
        {
            // can we safely continue writing to output buffer
            return output.FreeBytes > FastEncoderStatics.MaxCodeLen;
        }

        private static void WriteEndOfBlock(OutputBuffer output)
        {
            // The fast encoder outputs one long block, so it just needs to terminate this block
            const int endOfBlockCode = 256;
            var codeInfo = FastEncoderStatics.FastEncoderLiteralCodeInfo[endOfBlockCode];
            var codeLen = (int)(codeInfo & 31);
            output.WriteBits(codeLen, codeInfo >> 5);
        }

        public static void WriteMatch(int matchLen, int matchPos, OutputBuffer output)
        {
            Debug.Assert(matchLen >= FastEncoderWindow.MinMatch && matchLen <= FastEncoderWindow.MaxMatch,
                "Illegal currentMatch length!");

            // Get the code information for a match code
            var codeInfo =
                FastEncoderStatics.FastEncoderLiteralCodeInfo[
                    (FastEncoderStatics.NumChars + 1 - FastEncoderWindow.MinMatch) + matchLen];
            var codeLen = (int)codeInfo & 31;
            Debug.Assert(codeLen != 0, "Invalid Match Length!");
            if (codeLen <= 16)
            {
                output.WriteBits(codeLen, codeInfo >> 5);
            }
            else
            {
                output.WriteBits(16, (codeInfo >> 5) & 65535);
                output.WriteBits(codeLen - 16, codeInfo >> (5 + 16));
            }

            // Get the code information for a distance code
            codeInfo = FastEncoderStatics.FastEncoderDistanceCodeInfo[FastEncoderStatics.GetSlot(matchPos)];
            output.WriteBits((int)(codeInfo & 15), codeInfo >> 8);
            var extraBits = (int)(codeInfo >> 4) & 15;
            if (extraBits != 0)
            {
                output.WriteBits(extraBits, (uint)matchPos & FastEncoderStatics.BitMask[extraBits]);
            }
        }

        public static void WriteChar(byte b, OutputBuffer output)
        {
            var code = FastEncoderStatics.FastEncoderLiteralCodeInfo[b];
            output.WriteBits((int)code & 31, code >> 5);
        }

        // Output the block type and tree structure for our hard-coded trees.
        // Contains following data:
        //  "final" block flag 1 bit
        //  BLOCKTYPE_DYNAMIC 2 bits
        //  FastEncoderLiteralTreeLength
        //  FastEncoderDistanceTreeLength
        //
        public static void WriteDeflatePreamble(OutputBuffer output)
        {
            //Debug.Assert( bitCount == 0, "bitCount must be zero before writing tree bit!");

            output.WriteBytes(FastEncoderStatics.FastEncoderTreeStructureData, 0,
                FastEncoderStatics.FastEncoderTreeStructureData.Length);
            output.WriteBits(FastEncoderStatics.FastEncoderPostTreeBitCount,
                FastEncoderStatics.FastEncoderPostTreeBitBuf);
        }
    }
}