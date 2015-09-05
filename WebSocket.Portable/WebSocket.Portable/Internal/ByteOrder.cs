using System;

namespace WebSocket.Portable.Internal
{
    internal enum ByteOrder : byte { LittleEndian = 0, BigEndian = 1 }

    internal static class ByteOrderExtensions
    {
        public static bool IsHostOrder(this ByteOrder order)
        {
            // true : !(true ^ true)  or !(false ^ false)
            // false: !(true ^ false) or !(false ^ true)
            return !(BitConverter.IsLittleEndian ^ (order == ByteOrder.LittleEndian));
        }
    }
}