using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebSocket.Portable.Compression;

namespace WebSocket.Portable.Compression
{
    internal class DeflateStream// : Stream
    {
        internal const int DefaultBufferSize = 8192;

    //    internal delegate void AsyncWriteDelegate(byte[] array, int offset, int count, bool isAsync);

    //    private readonly CompressionMode _mode;
    //    private readonly bool _leaveOpen;
    //    private readonly Inflater _inflater;
    //    private Deflater _deflater;
    //    private readonly byte[] _buffer;

    //    private int _asyncOperations;
    //    private readonly AsyncCallback _callBack;
    //    private readonly AsyncWriteDelegate _asyncWriterDelegate;

    //    private IFileFormatWriter _formatWriter;
    //    private bool _wroteHeader;
    //    private bool _wroteBytes;

    //    public DeflateStream(Stream stream, CompressionMode mode)
    //        : this(stream, mode, false) { }

    //    public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen)
    //    {

    //        if (stream == null)
    //            throw new ArgumentNullException("stream");

    //        if (CompressionMode.Compress != mode && CompressionMode.Decompress != mode)
    //            throw new ArgumentOutOfRangeException("mode");

    //        this.BaseStream = stream;
    //        _mode = mode;
    //        _leaveOpen = leaveOpen;

    //        switch (_mode)
    //        {
    //            case CompressionMode.Decompress:
    //                if (!BaseStream.CanRead)
    //                    throw new ArgumentException("Stream is not readable.", "stream");

    //                _inflater = new Inflater();

    //                _callBack = ReadCallback;
    //                break;

    //            case CompressionMode.Compress:
    //                if (!BaseStream.CanWrite)
    //                    throw new ArgumentException("Stream is not writeable", "stream");

    //                _deflater = new Deflater();

    //                _asyncWriterDelegate = this.InternalWrite;
    //                _callBack = WriteCallback;
    //                break;

    //        }

    //        _buffer = new byte[DefaultBufferSize];
    //    }

    //    // Implies mode = Compress
    //    public DeflateStream(Stream stream)
    //        : this(stream, false) { }

    //    // Implies mode = Compress
    //    public DeflateStream(Stream stream, bool leaveOpen)
    //    {

    //        if (stream == null)
    //            throw new ArgumentNullException("stream");

    //        if (!stream.CanWrite)
    //            throw new ArgumentException("Stream is not writeable.", "stream");

    //        this.BaseStream = stream;
    //        _mode = CompressionMode.Compress;
    //        _leaveOpen = leaveOpen;

    //        _deflater = new Deflater();

    //        _asyncWriterDelegate = this.InternalWrite;
    //        _callBack = WriteCallback;

    //        _buffer = new byte[DefaultBufferSize];
    //    }

    //    internal void SetFileFormatReader(IFileFormatReader reader)
    //    {
    //        if (reader != null)
    //            _inflater.SetFileFormatReader(reader);            
    //    }

    //    internal void SetFileFormatWriter(IFileFormatWriter writer)
    //    {
    //        if (writer != null)
    //            _formatWriter = writer;            
    //    }

    //    public Stream BaseStream { get; private set; }

    //    public override bool CanRead
    //    {
    //        get { return this.BaseStream != null && (_mode == CompressionMode.Decompress && this.BaseStream.CanRead); }
    //    }

    //    public override bool CanWrite
    //    {
    //        get { return this.BaseStream != null && (_mode == CompressionMode.Compress && this.BaseStream.CanWrite); }
    //    }

    //    public override bool CanSeek
    //    {
    //        get { return false; }
    //    }

    //    public override long Length
    //    {
    //        get { throw new NotSupportedException(); }
    //    }

    //    public override long Position
    //    {
    //        get { throw new NotSupportedException(); }
    //        set { throw new NotSupportedException(); }
    //    }

    //    public override void Flush()
    //    {
    //        this.EnsureNotDisposed();
    //    }

    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public override void SetLength(long value)
    //    {
    //        throw new NotSupportedException();
    //    }

    //    public override int Read(byte[] array, int offset, int count)
    //    {
    //        this.EnsureDecompressionMode();
    //        this.ValidateParameters(array, offset, count);
    //        this.EnsureNotDisposed();

    //        var currentOffset = offset;
    //        var remainingCount = count;

    //        while (true)
    //        {
    //            var bytesRead = _inflater.Inflate(array, currentOffset, remainingCount);
    //            currentOffset += bytesRead;
    //            remainingCount -= bytesRead;

    //            if (remainingCount == 0)
    //                break;

    //            if (_inflater.Finished())
    //            {
    //                // if we finished decompressing, we can't have anything left in the outputwindow.
    //                Debug.Assert(_inflater.AvailableOutput == 0, "We should have copied all stuff out!");
    //                break;
    //            }

    //            Debug.Assert(_inflater.NeedsInput(), "We can only run into this case if we are short of input");

    //            var bytes = this.BaseStream.Read(_buffer, 0, _buffer.Length);
    //            if (bytes == 0)
    //                break;      //Do we want to throw an exception here?

    //            _inflater.SetInput(_buffer, 0, bytes);
    //        }

    //        return count - remainingCount;
    //    }

    //    private void ValidateParameters(byte[] array, int offset, int count)
    //    {
    //        if (array == null)
    //            throw new ArgumentNullException("array");

    //        if (offset < 0)
    //            throw new ArgumentOutOfRangeException("offset");

    //        if (count < 0)
    //            throw new ArgumentOutOfRangeException("count");

    //        if (array.Length - offset < count)
    //            throw new ArgumentException("Invalid offset.");
    //    }

    //    private void EnsureNotDisposed()
    //    {
    //        if (this.BaseStream == null)
    //            throw new ObjectDisposedException(null);
    //    }

    //    private void EnsureDecompressionMode()
    //    {
    //        if (_mode != CompressionMode.Decompress)
    //            throw new InvalidOperationException();
    //    }

    //    private void EnsureCompressionMode()
    //    {

    //        if (_mode != CompressionMode.Compress)
    //            throw new InvalidOperationException();
    //    }

    //    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    //    {
    //        EnsureDecompressionMode();

    //        // We use this checking order for compat to earlier versions:
    //        if (_asyncOperations != 0)
    //            throw new InvalidOperationException();

    //        ValidateParameters(buffer, offset, count);
    //        EnsureNotDisposed();

    //        Interlocked.Increment(ref _asyncOperations);

    //        try
    //        {
    //            var userResult = new DeflateStreamAsyncResult(asyncState, asyncCallback, array, offset, count);

    //            // Try to read decompressed data in output buffer
    //            var bytesRead = _inflater.Inflate(buffer, offset, count);
    //            if (bytesRead != 0)
    //            {
    //                // If decompression output buffer is not empty, return immediately.
    //                // 'true' means we complete synchronously.
    //                userResult.InvokeCallback(true, bytesRead);
    //                return userResult;
    //            }

    //            if (_inflater.Finished())
    //            {
    //                // end of compression stream
    //                userResult.InvokeCallback(true, 0);
    //                return userResult;
    //            }

    //            // If there is no data on the output buffer and we are not at 
    //            // the end of the stream, we need to get more data from the base stream
    //            this.BaseStream.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
    //            userResult.CompletedSynchronously &= userResult.IsCompleted;

    //            return userResult;

    //        }
    //        catch
    //        {
    //            Interlocked.Decrement(ref _asyncOperations);
    //            throw;
    //        }
    //    }



    //    public override IAsyncResult BeginRead(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState)
    //    {
    //        this.EnsureDecompressionMode();

    //        // We use this checking order for compat to earlier versions:
    //        if (_asyncOperations != 0)
    //            throw new InvalidOperationException();

    //        this.ValidateParameters(array, offset, count);
    //        this.EnsureNotDisposed();

    //        Interlocked.Increment(ref _asyncOperations);

    //        try
    //        {
    //            var userResult = new DeflateStreamAsyncResult(asyncState, asyncCallback, array, offset, count);

    //            // Try to read decompressed data in output buffer
    //            var bytesRead = _inflater.Inflate(array, offset, count);
    //            if (bytesRead != 0)
    //            {
    //                // If decompression output buffer is not empty, return immediately.
    //                // 'true' means we complete synchronously.
    //                userResult.InvokeCallback(true, bytesRead);
    //                return userResult;
    //            }

    //            if (_inflater.Finished())
    //            {
    //                // end of compression stream
    //                userResult.InvokeCallback(true, 0);
    //                return userResult;
    //            }

    //            // If there is no data on the output buffer and we are not at 
    //            // the end of the stream, we need to get more data from the base stream
    //            this.BaseStream.BeginRead(_buffer, 0, _buffer.Length, _callBack, userResult);
    //            userResult.CompletedSynchronously &= userResult.IsCompleted;

    //            return userResult;

    //        }
    //        catch
    //        {
    //            Interlocked.Decrement(ref _asyncOperations);
    //            throw;
    //        }
    //    }

    //    // callback function for asynchrous reading on base stream
    //    private void ReadCallback(IAsyncResult baseStreamResult)
    //    {
    //        var outerResult = (DeflateStreamAsyncResult)baseStreamResult.AsyncState;
    //        outerResult.CompletedSynchronously &= baseStreamResult.CompletedSynchronously;

    //        try
    //        {
    //            this.EnsureNotDisposed();

    //            var bytesRead = this.BaseStream.EndRead(baseStreamResult);
    //            if (bytesRead <= 0)
    //            {
    //                // This indicates the base stream has received EOF
    //                outerResult.InvokeCallback(0);
    //                return;
    //            }

    //            // Feed the data from base stream into decompression engine
    //            _inflater.SetInput(_buffer, 0, bytesRead);
    //            bytesRead = _inflater.Inflate(outerResult.Buffer, outerResult.Offset, outerResult.Count);

    //            if (bytesRead == 0 && !_inflater.Finished())
    //            {

    //                // We could have read in head information and didn't get any data.
    //                // Read from the base stream again.   
    //                // Need to solve recusion.
    //                this.BaseStream.BeginRead(_buffer, 0, _buffer.Length, _callBack, outerResult);

    //            }
    //            else
    //            {
    //                outerResult.InvokeCallback(bytesRead);
    //            }
    //        }
    //        catch (Exception exc)
    //        {
    //            // Defer throwing this until EndRead where we will likely have user code on the stack.
    //            outerResult.InvokeCallback(exc);
    //        }
    //    }

    //    public override int EndRead(IAsyncResult asyncResult)
    //    {
    //        this.EnsureDecompressionMode();
    //        this.CheckEndXxxxLegalStateAndParams(asyncResult);

    //        // We checked that this will work in CheckEndXxxxLegalStateAndParams:
    //        var deflateStrmAsyncResult = (DeflateStreamAsyncResult)asyncResult;

    //        this.AwaitAsyncResultCompletion(deflateStrmAsyncResult);

    //        var previousException = deflateStrmAsyncResult.Result as Exception;
    //        if (previousException != null)
    //        {
    //            // Rethrowing will delete the stack trace. Let's help future debuggers:                
    //            //previousException.Data.Add(OrigStackTrace_ExceptionDataKey, previousException.StackTrace);
    //            throw previousException;
    //        }

    //        return (int)deflateStrmAsyncResult.Result;
    //    }

    //    public override void Write(byte[] array, int offset, int count)
    //    {
    //        this.EnsureCompressionMode();
    //        this.ValidateParameters(array, offset, count);
    //        this.EnsureNotDisposed();
    //        this.InternalWrite(array, offset, count, false);
    //    }

    //    // isAsync always seems to be false. why do we have it?
    //    internal void InternalWrite(byte[] array, int offset, int count, bool isAsync)
    //    {
    //        this.DoMaintenance(array, offset, count);

    //        // Write compressed the bytes we already passed to the deflater:

    //        this.WriteDeflaterOutput(isAsync);

    //        // Pass new bytes through deflater and write them too:

    //        _deflater.SetInput(array, offset, count);
    //        this.WriteDeflaterOutput(isAsync);
    //    }


    //    private void WriteDeflaterOutput(bool isAsync)
    //    {
    //        while (!_deflater.NeedsInput())
    //        {
    //            var compressedBytes = _deflater.GetDeflateOutput(_buffer);
    //            if (compressedBytes > 0)
    //                this.DoWrite(_buffer, 0, compressedBytes, isAsync);
    //        }
    //    }

    //    private void DoWrite(byte[] array, int offset, int count, bool isAsync)
    //    {
    //        Debug.Assert(array != null);
    //        Debug.Assert(count != 0);

    //        if (isAsync)
    //        {
    //            var result = this.BaseStream.BeginWrite(array, offset, count, null, null);
    //            this.BaseStream.EndWrite(result);
    //        }
    //        else
    //        {
    //            this.BaseStream.Write(array, offset, count);
    //        }
    //    }

    //    // Perform deflate-mode maintenance required due to custom header and footer writers
    //    // (e.g. set by GZipStream):
    //    private void DoMaintenance(byte[] array, int offset, int count)
    //    {

    //        // If no bytes written, do nothing:
    //        if (count <= 0)
    //            return;

    //        // Note that stream contains more than zero data bytes:
    //        _wroteBytes = true;

    //        // If no header/footer formatter present, nothing else to do:
    //        if (_formatWriter == null)
    //            return;

    //        // If formatter has not yet written a header, do it now:
    //        if (!_wroteHeader)
    //        {
    //            byte[] b = _formatWriter.GetHeader();
    //            this.BaseStream.Write(b, 0, b.Length);
    //            _wroteHeader = true;
    //        }

    //        // Inform formatter of the data bytes written:
    //        _formatWriter.UpdateWithBytesRead(array, offset, count);
    //    }

    //    // This is called by Dispose:
    //    private void PurgeBuffers(bool disposing)
    //    {

    //        if (!disposing)
    //            return;

    //        if (this.BaseStream == null)
    //            return;

    //        this.Flush();

    //        if (_mode != CompressionMode.Compress)
    //            return;

    //        // Some deflaters (e.g. ZLib write more than zero bytes for zero bytes inputs.
    //        // This round-trips and we should be ok with this, but our legacy managed deflater
    //        // always wrote zero output for zero input and upstack code (e.g. GZipStream)
    //        // took dependencies on it. Thus, make sure to only "flush" when we actually had
    //        // some input:
    //        if (_wroteBytes)
    //        {

    //            // Compress any bytes left:                        
    //            this.WriteDeflaterOutput(false);

    //            // Pull out any bytes left inside deflater:
    //            bool finished;
    //            do
    //            {
    //                int compressedBytes;
    //                finished = _deflater.Finish(_buffer, out compressedBytes);

    //                if (compressedBytes > 0)
    //                    this.DoWrite(_buffer, 0, compressedBytes, false);

    //            } 
    //            while (!finished);
    //        }

    //        // Write format footer:
    //        if (_formatWriter == null || !_wroteHeader) 
    //            return;

    //        var b = _formatWriter.GetFooter();
    //        this.BaseStream.Write(b, 0, b.Length);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        try
    //        {
    //            this.PurgeBuffers(disposing);
    //        }
    //        finally
    //        {

    //            // Close the underlying stream even if PurgeBuffers threw.
    //            // Stream.Close() may throw here (may or may not be due to the same error).
    //            // In this case, we still need to clean up internal resources, hence the inner finally blocks.
    //            try
    //            {

    //                if (disposing && !_leaveOpen && BaseStream != null)
    //                    this.BaseStream.Dispose();

    //            }
    //            finally
    //            {
    //                this.BaseStream = null;
    //                try
    //                {
    //                    if (_deflater != null)
    //                        _deflater.Dispose();
    //                }
    //                finally
    //                {
    //                    _deflater = null;
    //                    base.Dispose(disposing);
    //                }
    //            }
    //        } 
    //    }


    //    public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState)
    //    {
    //        this.EnsureCompressionMode();

    //        // We use this checking order for compat to earlier versions:
    //        if (_asyncOperations != 0)
    //            throw new InvalidOperationException();

    //        this.ValidateParameters(array, offset, count);
    //        this.EnsureNotDisposed();

    //        Interlocked.Increment(ref _asyncOperations);

    //        try
    //        {

    //            var userResult = new DeflateStreamAsyncResult(asyncState, asyncCallback, array, offset, count);

    //            _asyncWriterDelegate.BeginInvoke(array, offset, count, true, _callBack, userResult);
    //            userResult.CompletedSynchronously &= userResult.IsCompleted;

    //            return userResult;

    //        }
    //        catch
    //        {
    //            Interlocked.Decrement(ref _asyncOperations);
    //            throw;
    //        }
    //    }

    //    // Callback function for asynchrous reading on base stream
    //    private void WriteCallback(IAsyncResult asyncResult)
    //    {

    //        var outerResult = (DeflateStreamAsyncResult)asyncResult.AsyncState;
    //        outerResult.CompletedSynchronously &= asyncResult.CompletedSynchronously;

    //        try
    //        {
    //            _asyncWriterDelegate.EndInvoke(asyncResult);
    //        }
    //        catch (Exception exc)
    //        {
    //            // Defer throwing this until EndWrite where there is user code on the stack:
    //            outerResult.InvokeCallback(exc);
    //            return;
    //        }
    //        outerResult.InvokeCallback(null);
    //    }

    //    public override void EndWrite(IAsyncResult asyncResult)
    //    {
    //        this.EnsureCompressionMode();
    //        this.CheckEndXxxxLegalStateAndParams(asyncResult);

    //        // We checked that this will work in CheckEndXxxxLegalStateAndParams:
    //        var deflateStrmAsyncResult = (DeflateStreamAsyncResult)asyncResult;

    //        this.AwaitAsyncResultCompletion(deflateStrmAsyncResult);

    //        var previousException = deflateStrmAsyncResult.Result as Exception;
    //        if (previousException != null)
    //        {
    //            // Rethrowing will delete the stack trace. Let's help future debuggers:                
    //            //previousException.Data.Add(OrigStackTrace_ExceptionDataKey, previousException.StackTrace);
    //            throw previousException;
    //        }
    //    }

    //    private void CheckEndXxxxLegalStateAndParams(IAsyncResult asyncResult)
    //    {
    //        if (_asyncOperations != 1)
    //            throw new InvalidOperationException();

    //        if (asyncResult == null)
    //            throw new ArgumentNullException("asyncResult");

    //        this.EnsureNotDisposed();

    //        var myResult = asyncResult as DeflateStreamAsyncResult;

    //        // This should really be an ArgumentException, but we keep this for compat to previous versions:
    //        if (myResult == null)
    //            throw new ArgumentNullException("asyncResult");
    //    }

    //    private void AwaitAsyncResultCompletion(DeflateStreamAsyncResult asyncResult)
    //    {
    //        try
    //        {
    //            if (!asyncResult.IsCompleted)
    //                asyncResult.AsyncWaitHandle.WaitOne();
    //        }
    //        finally
    //        {
    //            Interlocked.Decrement(ref _asyncOperations);
    //            asyncResult.Close();  // this will just close the wait handle
    //        }
    //    }
    }
}