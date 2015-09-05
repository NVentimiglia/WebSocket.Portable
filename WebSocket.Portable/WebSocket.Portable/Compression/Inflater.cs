using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using WebSocket.Portable.Compression;

namespace WebSocket.Portable.Compression
{
    // ==++==
    //
    //  Copyright (c) Microsoft Corporation.  All rights reserved.
    //
    //  zlib.h -- interface of the 'zlib' general purpose compression library
    //  version 1.2.1, November 17th, 2003
    //
    //  Copyright (C) 1995-2003 Jean-loup Gailly and Mark Adler
    //
    //  This software is provided 'as-is', without any express or implied
    //  warranty.  In no event will the authors be held liable for any damages
    //  arising from the use of this software.
    //
    //  Permission is granted to anyone to use this software for any purpose,
    //  including commercial applications, and to alter it and redistribute it
    //  freely, subject to the following restrictions:
    //
    //  1. The origin of this software must not be misrepresented; you must not
    //     claim that you wrote the original software. If you use this software
    //     in a product, an acknowledgment in the product documentation would be
    //     appreciated but is not required.
    //  2. Altered source versions must be plainly marked as such, and must not be
    //     misrepresented as being the original software.
    //  3. This notice may not be removed or altered from any source distribution.
    //
    //
    // ==--==

    internal class Inflater
    {
        // const tables used in decoding:

        // Extra bits for length code 257 - 285.  
        private static readonly byte[] _extraLengthBits = {
            0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0};

        // The base length for length code 257 - 285.
        // The formula to get the real length for a length code is lengthBase[code - 257] + (value stored in extraBits)
        private static readonly int[] _lengthBase = {
            3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258};

        // The base distance for distance code 0 - 29    
        // The real distance for a distance code is  distanceBasePosition[code] + (value stored in extraBits)
        private static readonly int[] _distanceBasePosition = {
            1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577,0,0};

        // code lengths for code length alphabet is stored in following order
        private static readonly byte[] _codeOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        private static readonly byte[] _staticDistanceTreeTable = { 
            0x00,0x10,0x08,0x18,0x04,0x14,0x0c,0x1c,0x02,0x12,0x0a,0x1a,
            0x06,0x16,0x0e,0x1e,0x01,0x11,0x09,0x19,0x05,0x15,0x0d,0x1d,
            0x03,0x13,0x0b,0x1b,0x07,0x17,0x0f,0x1f
        };

        private readonly OutputWindow _output;
        private readonly InputBuffer _input;
        private HuffmanTree _literalLengthTree;
        private HuffmanTree _distanceTree;

        private InflaterState _state;
        private bool _hasFormatReader;
        private int _bfinal;
        private BlockType _blockType;

        // uncompressed block
        private readonly byte[] _blockLengthBuffer = new byte[4];
        private int _blockLength;

        // compressed block
        private int _length;
        private int _distanceCode;
        private int _extraBits;

        private int _loopCounter;
        private int _literalLengthCodeCount;
        private int _distanceCodeCount;
        private int _codeLengthCodeCount;
        private int _codeArraySize;
        private int _lengthCode;

        private readonly byte[] _codeList;        // temporary array to store the code length for literal/Length and distance
        private readonly byte[] _codeLengthTreeCodeLength;
        private HuffmanTree _codeLengthTree;

        private IFileFormatReader _formatReader;  // class to decode header and footer (e.g. gzip)

        public Inflater()
        {
            _output = new OutputWindow();
            _input = new InputBuffer();

            _codeList = new byte[HuffmanTree.MaxLiteralTreeElements + HuffmanTree.MaxDistTreeElements];
            _codeLengthTreeCodeLength = new byte[HuffmanTree.NumberOfCodeLengthTreeElements];
            Reset();
        }

        internal void SetFileFormatReader(IFileFormatReader reader)
        {
            _formatReader = reader;
            _hasFormatReader = true;
            this.Reset();
        }

        private void Reset()
        {
            _state = _hasFormatReader ? InflaterState.ReadingHeader : InflaterState.ReadingBFinal;
        }

        public void SetInput(byte[] inputBytes, int offset, int length)
        {
            _input.SetInput(inputBytes, offset, length);    // append the bytes
        }


        public bool Finished()
        {
            return (_state == InflaterState.Done || _state == InflaterState.VerifyingFooter);
        }

        public int AvailableOutput
        {
            get { return _output.AvailableBytes; }
        }

        public bool NeedsInput()
        {
            return _input.NeedsInput();
        }

        public int Inflate(byte[] bytes, int offset, int length)
        {
            // copy bytes from output to outputbytes if we have aviable bytes 
            // if buffer is not filled up. keep decoding until no input are available
            // if decodeBlock returns false. Throw an exception.
            var count = 0;
            do
            {
                var copied = _output.CopyTo(bytes, offset, length);
                if (copied > 0)
                {
                    if (_hasFormatReader)
                    {
                        _formatReader.UpdateWithBytesRead(bytes, offset, copied);
                    }

                    offset += copied;
                    count += copied;
                    length -= copied;
                }

                if (length == 0) // filled in the bytes array
                    break;
                // Decode will return false when more input is needed
            }
            while (!this.Finished() && this.Decode());

            if (_state != InflaterState.VerifyingFooter)
                return count; // finished reading CRC

            // In this case finished is true and output window has all the data.
            // But some data in output window might not be copied out.
            if (_output.AvailableBytes == 0)
                _formatReader.Validate();

            return count;
        }

        //Each block of compressed data begins with 3 header bits
        // containing the following data:
        //    first bit       BFINAL
        //    next 2 bits     BTYPE
        // Note that the header bits do not necessarily begin on a byte
        // boundary, since a block does not necessarily occupy an integral
        // number of bytes.
        // BFINAL is set if and only if this is the last block of the data
        // set.
        // BTYPE specifies how the data are compressed, as follows:
        //    00 - no compression
        //    01 - compressed with fixed Huffman codes
        //    10 - compressed with dynamic Huffman codes
        //    11 - reserved (error)
        // The only difference between the two compressed cases is how the
        // Huffman codes for the literal/length and distance alphabets are
        // defined.
        //
        // This function returns true for success (end of block or output window is full,) 
        // false if we are short of input
        //
        private bool Decode()
        {
            var eob = false;
            var result = false;

            if (Finished())
            {
                return true;
            }

            if (_hasFormatReader)
            {
                switch (_state)
                {
                    case InflaterState.ReadingHeader:
                        if (!_formatReader.ReadHeader(_input))
                        {
                            return false;
                        }
                        _state = InflaterState.ReadingBFinal;
                        break;

                    case InflaterState.ReadingFooter:
                    case InflaterState.StartReadingFooter:
                        if (!_formatReader.ReadFooter(_input))
                            return false;
                        _state = InflaterState.VerifyingFooter;
                        return true;
                }
            }

            if (_state == InflaterState.ReadingBFinal)
            {   // reading bfinal bit
                // Need 1 bit
                if (!_input.EnsureBitsAvailable(1))
                    return false;

                _bfinal = _input.GetBits(1);
                _state = InflaterState.ReadingBType;
            }

            if (_state == InflaterState.ReadingBType)
            {
                // Need 2 bits
                if (!_input.EnsureBitsAvailable(2))
                {
                    _state = InflaterState.ReadingBType;
                    return false;
                }

                _blockType = (BlockType)_input.GetBits(2);
                switch (_blockType)
                {
                    case BlockType.Dynamic:
                        _state = InflaterState.ReadingNumLitCodes;
                        break;
                    case BlockType.Static:
                        _literalLengthTree = HuffmanTree.StaticLiteralLengthTree;
                        _distanceTree = HuffmanTree.StaticDistanceTree;
                        _state = InflaterState.DecodeTop;
                        break;
                    case BlockType.Uncompressed:
                        _state = InflaterState.UncompressedAligning;
                        break;
                    default:
                        throw new InvalidDataContractException("Unknown block type.");
                }
            }

            switch (_blockType)
            {
                case BlockType.Dynamic:
                    result = _state < InflaterState.DecodeTop ? DecodeDynamicBlockHeader() : DecodeBlock(out eob);
                    break;
                case BlockType.Static:
                    result = DecodeBlock(out eob);
                    break;
                case BlockType.Uncompressed:
                    result = DecodeUncompressedBlock(out eob);
                    break;
                default:
                    throw new InvalidDataContractException("Unknown block type.");
            }

            //
            // If we reached the end of the block and the block we were decoding had
            // bfinal=1 (final block)
            //
            if (!eob || (_bfinal == 0))
                return result;
            _state = _hasFormatReader ? InflaterState.StartReadingFooter : InflaterState.Done;
            return result;
        }


        // Format of Non-compressed blocks (BTYPE=00):
        //
        // Any bits of input up to the next byte boundary are ignored.
        // The rest of the block consists of the following information:
        //
        //     0   1   2   3   4...
        //   +---+---+---+---+================================+
        //   |  LEN  | NLEN  |... LEN bytes of literal data...|
        //   +---+---+---+---+================================+
        // 
        // LEN is the number of data bytes in the block.  NLEN is the
        // one's complement of LEN.

        private bool DecodeUncompressedBlock(out bool endOfBlock)
        {
            endOfBlock = false;
            while (true)
            {
                switch (_state)
                {

                    case InflaterState.UncompressedAligning: // intial state when calling this function
                        // we must skip to a byte boundary
                        _input.SkipToByteBoundary();
                        _state = InflaterState.UncompressedByte1;
                        goto case InflaterState.UncompressedByte1;

                    case InflaterState.UncompressedByte1:   // decoding block length 
                    case InflaterState.UncompressedByte2:
                    case InflaterState.UncompressedByte3:
                    case InflaterState.UncompressedByte4:
                        var bits = _input.GetBits(8);
                        if (bits < 0)
                            return false;

                        _blockLengthBuffer[_state - InflaterState.UncompressedByte1] = (byte)bits;
                        if (_state == InflaterState.UncompressedByte4)
                        {

                            _blockLength = _blockLengthBuffer[0] + _blockLengthBuffer[1] * 256;
                            var blockLengthComplement = _blockLengthBuffer[2] + _blockLengthBuffer[3] * 256;

                            // make sure complement matches
                            if ((ushort)_blockLength != (ushort)(~blockLengthComplement))
                                throw new InvalidDataContractException("Invalid block length.");
                        }

                        _state += 1;
                        break;

                    case InflaterState.DecodingUncompressed: // copying block data 

                        // Directly copy bytes from input to output. 
                        var bytesCopied = _output.CopyFrom(_input, _blockLength);
                        _blockLength -= bytesCopied;

                        if (_blockLength == 0)
                        {
                            // Done with this block, need to re-init bit buffer for next block
                            _state = InflaterState.ReadingBFinal;
                            endOfBlock = true;
                            return true;
                        }

                        // We can fail to copy all bytes for two reasons:
                        //    Running out of Input 
                        //    running out of free space in output window
                        return _output.FreeBytes == 0;

                    default:
                        throw new InvalidDataContractException("Unknown state.");
                }
            }
        }

        private bool DecodeBlock(out bool endOfBlockCodeSeen)
        {
            endOfBlockCodeSeen = false;

            var freeBytes = _output.FreeBytes;   // it is a little bit faster than frequently accessing the property 
            while (freeBytes > 258)
            {
                // 258 means we can safely do decoding since maximum repeat length is 258

                switch (_state)
                {
                    case InflaterState.DecodeTop:
                        // decode an element from the literal tree

                        // 
                        var symbol = _literalLengthTree.GetNextSymbol(_input);
                        if (symbol < 0)
                            return false;

                        if (symbol < 256)
                        {        // literal
                            _output.Write((byte)symbol);
                            --freeBytes;
                        }
                        else if (symbol == 256)
                        { // end of block
                            endOfBlockCodeSeen = true;
                            // Reset state
                            _state = InflaterState.ReadingBFinal;
                            return true;           // ***********
                        }
                        else
                        {                 // length/distance pair
                            symbol -= 257;     // length code started at 257
                            if (symbol < 8)
                            {
                                symbol += 3;   // match length = 3,4,5,6,7,8,9,10
                                _extraBits = 0;
                            }
                            else if (symbol == 28)
                            { // extra bits for code 285 is 0 
                                symbol = 258;             // code 285 means length 258    
                                _extraBits = 0;
                            }
                            else
                            {
                                if (symbol < 0 || symbol >= _extraLengthBits.Length)
                                    throw new InvalidDataContractException();
                                _extraBits = _extraLengthBits[symbol];
                                Debug.Assert(_extraBits != 0, "We handle other cases seperately!");
                            }
                            _length = symbol;
                            goto case InflaterState.HaveInitialLength;
                        }
                        break;

                    case InflaterState.HaveInitialLength:
                        if (_extraBits > 0)
                        {
                            _state = InflaterState.HaveInitialLength;
                            var bits = _input.GetBits(_extraBits);
                            if (bits < 0)
                            {
                                return false;
                            }

                            if (_length < 0 || _length >= _lengthBase.Length)
                                throw new InvalidDataContractException();
                            _length = _lengthBase[_length] + bits;
                        }
                        _state = InflaterState.HaveFullLength;
                        goto case InflaterState.HaveFullLength;

                    case InflaterState.HaveFullLength:
                        if (_blockType == BlockType.Dynamic)
                        {
                            _distanceCode = _distanceTree.GetNextSymbol(_input);
                        }
                        else
                        {   // get distance code directly for static block
                            _distanceCode = _input.GetBits(5);
                            if (_distanceCode >= 0)
                            {
                                _distanceCode = _staticDistanceTreeTable[_distanceCode];
                            }
                        }

                        if (_distanceCode < 0)
                        { // running out input                    
                            return false;
                        }

                        _state = InflaterState.HaveDistCode;
                        goto case InflaterState.HaveDistCode;

                    case InflaterState.HaveDistCode:
                        // To avoid a table lookup we note that for distanceCode >= 2,
                        // extra_bits = (distanceCode-2) >> 1
                        int offset;
                        if (_distanceCode > 3)
                        {
                            _extraBits = (_distanceCode - 2) >> 1;
                            int bits = _input.GetBits(_extraBits);
                            if (bits < 0)
                            {
                                return false;
                            }
                            offset = _distanceBasePosition[_distanceCode] + bits;
                        }
                        else
                        {
                            offset = _distanceCode + 1;
                        }

                        Debug.Assert(freeBytes >= 258, "following operation is not safe!");
                        _output.WriteLengthDistance(_length, offset);
                        freeBytes -= _length;
                        _state = InflaterState.DecodeTop;
                        break;

                    default:
                        throw new InvalidDataContractException("Unknown state.");
                }
            }

            return true;
        }


        // Format of the dynamic block header:
        //      5 Bits: HLIT, # of Literal/Length codes - 257 (257 - 286)
        //      5 Bits: HDIST, # of Distance codes - 1        (1 - 32)
        //      4 Bits: HCLEN, # of Code Length codes - 4     (4 - 19)
        //
        //      (HCLEN + 4) x 3 bits: code lengths for the code length
        //          alphabet given just above, in the order: 16, 17, 18,
        //          0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15
        //
        //          These code lengths are interpreted as 3-bit integers
        //          (0-7); as above, a code length of 0 means the
        //          corresponding symbol (literal/length or distance code
        //          length) is not used.
        //
        //      HLIT + 257 code lengths for the literal/length alphabet,
        //          encoded using the code length Huffman code
        //
        //       HDIST + 1 code lengths for the distance alphabet,
        //          encoded using the code length Huffman code
        //
        // The code length repeat codes can cross from HLIT + 257 to the
        // HDIST + 1 code lengths.  In other words, all code lengths form
        // a single sequence of HLIT + HDIST + 258 values.
        private bool DecodeDynamicBlockHeader()
        {
            switch (_state)
            {
                case InflaterState.ReadingNumLitCodes:
                    _literalLengthCodeCount = _input.GetBits(5);
                    if (_literalLengthCodeCount < 0)
                    {
                        return false;
                    }
                    _literalLengthCodeCount += 257;
                    _state = InflaterState.ReadingNumDistCodes;
                    goto case InflaterState.ReadingNumDistCodes;

                case InflaterState.ReadingNumDistCodes:
                    _distanceCodeCount = _input.GetBits(5);
                    if (_distanceCodeCount < 0)
                    {
                        return false;
                    }
                    _distanceCodeCount += 1;
                    _state = InflaterState.ReadingNumCodeLengthCodes;
                    goto case InflaterState.ReadingNumCodeLengthCodes;

                case InflaterState.ReadingNumCodeLengthCodes:
                    _codeLengthCodeCount = _input.GetBits(4);
                    if (_codeLengthCodeCount < 0)
                    {
                        return false;
                    }
                    _codeLengthCodeCount += 4;
                    _loopCounter = 0;
                    _state = InflaterState.ReadingCodeLengthCodes;
                    goto case InflaterState.ReadingCodeLengthCodes;

                case InflaterState.ReadingCodeLengthCodes:
                    while (_loopCounter < _codeLengthCodeCount)
                    {
                        var bits = _input.GetBits(3);
                        if (bits < 0)
                        {
                            return false;
                        }
                        _codeLengthTreeCodeLength[_codeOrder[_loopCounter]] = (byte)bits;
                        ++_loopCounter;
                    }

                    for (var i = _codeLengthCodeCount; i < _codeOrder.Length; i++)
                    {
                        _codeLengthTreeCodeLength[_codeOrder[i]] = 0;
                    }

                    // create huffman tree for code length
                    _codeLengthTree = new HuffmanTree(_codeLengthTreeCodeLength);
                    _codeArraySize = _literalLengthCodeCount + _distanceCodeCount;
                    _loopCounter = 0;     // reset loop count

                    _state = InflaterState.ReadingTreeCodesBefore;
                    goto case InflaterState.ReadingTreeCodesBefore;

                case InflaterState.ReadingTreeCodesBefore:
                case InflaterState.ReadingTreeCodesAfter:
                    while (_loopCounter < _codeArraySize)
                    {
                        if (_state == InflaterState.ReadingTreeCodesBefore)
                        {
                            if ((_lengthCode = _codeLengthTree.GetNextSymbol(_input)) < 0)
                            {
                                return false;
                            }
                        }

                        // The alphabet for code lengths is as follows:
                        //  0 - 15: Represent code lengths of 0 - 15
                        //  16: Copy the previous code length 3 - 6 times.
                        //  The next 2 bits indicate repeat length
                        //         (0 = 3, ... , 3 = 6)
                        //      Example:  Codes 8, 16 (+2 bits 11),
                        //                16 (+2 bits 10) will expand to
                        //                12 code lengths of 8 (1 + 6 + 5)
                        //  17: Repeat a code length of 0 for 3 - 10 times.
                        //    (3 bits of length)
                        //  18: Repeat a code length of 0 for 11 - 138 times
                        //    (7 bits of length)
                        if (_lengthCode <= 15)
                        {
                            _codeList[_loopCounter++] = (byte)_lengthCode;
                        }
                        else
                        {
                            if (!_input.EnsureBitsAvailable(7))
                            { // it doesn't matter if we require more bits here
                                _state = InflaterState.ReadingTreeCodesAfter;
                                return false;
                            }

                            int repeatCount;
                            switch (_lengthCode)
                            {
                                case 16:
                                    {
                                        if (_loopCounter == 0)
                                            throw new InvalidDataContractException();

                                        var previousCode = _codeList[_loopCounter - 1];
                                        repeatCount = _input.GetBits(2) + 3;

                                        if (_loopCounter + repeatCount > _codeArraySize)
                                            throw new InvalidDataContractException();

                                        for (var j = 0; j < repeatCount; j++)
                                            _codeList[_loopCounter++] = previousCode;
                                    }
                                    break;
                                case 17:
                                    repeatCount = _input.GetBits(3) + 3;
                                    if (_loopCounter + repeatCount > _codeArraySize)
                                        throw new InvalidDataContractException();

                                    for (var j = 0; j < repeatCount; j++)
                                        _codeList[_loopCounter++] = 0;
                                    break;
                                default:
                                    repeatCount = _input.GetBits(7) + 11;
                                    if (_loopCounter + repeatCount > _codeArraySize)
                                        throw new InvalidDataContractException();

                                    for (var j = 0; j < repeatCount; j++)
                                        _codeList[_loopCounter++] = 0;
                                    break;
                            }
                        }
                        _state = InflaterState.ReadingTreeCodesBefore; // we want to read the next code.
                    }
                    break;

                default:
                    throw new InvalidDataContractException("Unknown state.");
            }

            var literalTreeCodeLength = new byte[HuffmanTree.MaxLiteralTreeElements];
            var distanceTreeCodeLength = new byte[HuffmanTree.MaxDistTreeElements];

            // Create literal and distance tables
            Array.Copy(_codeList, literalTreeCodeLength, _literalLengthCodeCount);
            Array.Copy(_codeList, _literalLengthCodeCount, distanceTreeCodeLength, 0, _distanceCodeCount);

            // Make sure there is an end-of-block code, otherwise how could we ever end?
            if (literalTreeCodeLength[HuffmanTree.EndOfBlockCode] == 0)
                throw new InvalidDataContractException();

            _literalLengthTree = new HuffmanTree(literalTreeCodeLength);
            _distanceTree = new HuffmanTree(distanceTreeCodeLength);
            _state = InflaterState.DecodeTop;
            return true;
        }
    }
}
