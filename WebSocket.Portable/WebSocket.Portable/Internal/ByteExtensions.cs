using System;

namespace WebSocket.Portable.Internal
{
    internal static class ByteExtensions
    {
        public static int ToBit(this bool value)
        {
            return value ? 1 : 0;
        }

        public static byte[] ToByteArray(this bool value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this char value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this double value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this float value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this int value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this long value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this short value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this uint value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this ulong value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static byte[] ToByteArray(this ushort value, ByteOrder byteOrder)
        {
            var bytes = BitConverter.GetBytes(value);
            return AdjustByteOrder(bytes, byteOrder);
        }

        public static ushort ToUInt16(this byte[] src, ByteOrder srcOrder)
        {
            return BitConverter.ToUInt16(src.ToHostOrder(srcOrder), 0);
        }

        public static ulong ToUInt64(this byte[] src, ByteOrder srcOrder)
        {
            return BitConverter.ToUInt64(src.ToHostOrder(srcOrder), 0);
        }

        public static byte[] ToHostOrder(this byte[] src, ByteOrder srcOrder)
        {
            if (src == null)
                return null;

            return src.Length > 1 && !srcOrder.IsHostOrder()
                   ? src.CopyReverse()
                   : src;
        }

        public static byte[] CopyReverse(this byte[] src)
        {
            if (src == null)
                return null;

            var len = src.Length;

            var reverse = new byte[len];
            for (var i = 0; i <= len / 2; ++i)
            {
                var t = src[i];
                var o = len - i - 1;
                reverse[i] = src[o];
                reverse[o] = t;
            }

            return reverse;
        }

        private static byte[] AdjustByteOrder(byte[] bytes, ByteOrder byteOrder)
        {
            if (!byteOrder.IsHostOrder())
                Array.Reverse(bytes);
            return bytes;
        }
    }
}
