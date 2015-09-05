namespace WebSocket.Portable
{
    public static class WebSocketState
    {
        /// <summary>
        /// Equivalent to numeric value 0.
        /// Indicates that the connection has not yet been established.
        /// </summary>
        public const int Connecting = 0;

        /// <summary>
        /// Equivalent to numeric value 1.
        /// Indicates that the connection is established and the communication is possible.
        /// </summary>
        public const int Connected = 1;

        /// <summary>
        /// Equivalent to numeric value 2.
        /// Indicates that the connection is going through the opening handshake.
        /// </summary>
        public const int Opening = 2;

        /// <summary>
        /// Equivalent to numeric value 3.
        /// Indicates that the connection went through the opening handshake and is now open
        /// </summary>        
        public const int Open = 3;

        /// <summary>
        /// Equivalent to numeric value 4.
        /// Indicates that the connection is going through the closing handshake or
        /// the <c>Close</c> method has been invoked.
        /// </summary>
        public const int Closing = 4;

        /// <summary>
        /// Equivalent to numeric value 5.
        /// Indicates that the connection has been closed or couldn't be opened.
        /// </summary>        
        public const int Closed = 5;
    }
}
