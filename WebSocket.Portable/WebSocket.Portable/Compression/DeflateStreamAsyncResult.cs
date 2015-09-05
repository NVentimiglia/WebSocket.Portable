using System;
using System.Threading;

namespace WebSocket.Portable.Compression
{
    internal class DeflateStreamAsyncResult : IAsyncResult
    {
        public byte[] Buffer;
        public int Offset;
        public int Count;
        
        private readonly AsyncCallback _asyncCallback;      // Caller's callback method.

        private int _invokedCallback;               // 0 is callback is not called
        private int _completed;                     // 0 if not completed >0 otherwise.
        private ManualResetEvent _event;            // lazy allocated event to be returned in the IAsyncResult for the client to wait on

        public DeflateStreamAsyncResult(object asyncState, AsyncCallback asyncCallback, byte[] buffer, int offset, int count)
        {
            this.Buffer = buffer;
            this.Offset = offset;
            this.Count = count;
            this.AsyncState = asyncState;

            CompletedSynchronously = true;
            _asyncCallback = asyncCallback;
        }

        // Interface method to return the caller's state object.
        public object AsyncState { get; private set; }

        // Interface property to return a WaitHandle that can be waited on for I/O completion.
        // This property implements lazy event creation.
        // the event object is only created when this property is accessed,
        // since we're internally only using callbacks, as long as the user is using
        // callbacks as well we will not create an event at all.
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                // save a copy of the completion status
                var savedCompleted = _completed;
                if (_event == null)
                {
                    // lazy allocation of the event:
                    // if this property is never accessed this object is never created
                    Interlocked.CompareExchange(ref _event, new ManualResetEvent(savedCompleted != 0), null);
                }

                if (savedCompleted == 0 && _completed != 0)
                {
                    // if, while the event was created in the reset state,
                    // the IO operation completed, set the event here.
                    _event.Set();
                }
                return _event;
            }
        }

        // Interface property, returning synchronous completion status.
        public bool CompletedSynchronously { get; internal set; }

        // Interface property, returning completion status.
        public bool IsCompleted
        {
            get { return _completed != 0; }
        }

        // Internal property for setting the IO result.
        public object Result { get; private set; }

        public void Close()
        {
            if (_event != null)
                _event.Dispose();            
        }

        public void InvokeCallback(bool completedSynchronously, object result)
        {
            this.Complete(completedSynchronously, result);
        }

        public void InvokeCallback(object result)
        {
            this.Complete(result);
        }

        // Internal method for setting completion.
        // As a side effect, we'll signal the WaitHandle event and clean up.
        private void Complete(bool completedSynchronously, object result)
        {
            CompletedSynchronously = completedSynchronously;
            this.Complete(result);
        }

        private void Complete(object result)
        {
            this.Result = result;

            // Set IsCompleted and the event only after the usercallback method. 
            Interlocked.Increment(ref _completed);

            if (_event != null)
                _event.Set();            

            if (Interlocked.Increment(ref _invokedCallback) != 1) 
                return;

            if (_asyncCallback != null)
                _asyncCallback(this);            
        }

    }
}
