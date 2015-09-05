namespace WebSocket.Portable.Interfaces
{
    public interface ITcpConnection : IDataLayer
    {
        /// <summary>
        /// Gets a value indicating whether this tcp connection is secure.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this tcp connection is secure; otherwise, <c>false</c>.
        /// </value>
        bool IsSecure { get; }
    }
}
