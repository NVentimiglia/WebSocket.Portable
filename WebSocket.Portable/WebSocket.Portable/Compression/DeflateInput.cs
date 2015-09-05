using System.Diagnostics;

namespace WebSocket.Portable.Compression
{
    internal class DeflateInput
    {
        public byte[] Buffer { get; set; }

        public int Count { get; set; }

        public int StartIndex { get; set; }

        public void ConsumeBytes(int n)
        {
            Debug.Assert(n <= Count, "Should use more bytes than what we have in the buffer");
            StartIndex += n;
            Count -= n;
            Debug.Assert(StartIndex + Count <= Buffer.Length, "Input buffer is in invalid state!");
        }

        public InputState DumpState()
        {
            InputState savedState;
            savedState.Count = this.Count;
            savedState.StartIndex = this.StartIndex;
            return savedState;
        }

        public void RestoreState(InputState state)
        {
            Count = state.Count;
            StartIndex = state.StartIndex;
        }

        public struct InputState
        {
            public int Count;
            public int StartIndex;
        }
    }
}
