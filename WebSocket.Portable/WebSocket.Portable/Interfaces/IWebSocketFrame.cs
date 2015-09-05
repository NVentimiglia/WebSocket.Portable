using System.Threading;
using System.Threading.Tasks;

namespace WebSocket.Portable.Interfaces
{
    public interface IWebSocketFrame
    {
        bool IsFin { get; }

        bool IsMasked { get; }

        bool IsRsv1 { get; }

        bool IsRsv2 { get; }

        bool IsRsv3 { get; }        

        /// <summary>
        /// Gets a value indicating whether this frame is a control frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a control frame; otherwise, <c>false</c>.
        /// </value>
        bool IsControlFrame { get; }

        /// <summary>
        /// Gets a value indicating whether this frame is a data frame.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a data frame; otherwise, <c>false</c>.
        /// </value>
        bool IsDataFrame { get; }

        /// <summary>
        /// Gets a value indicating whether this frame contains binary data.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame contains binary data; otherwise, <c>false</c>.
        /// </value>
        bool IsBinaryData { get; }

        /// <summary>
        /// Gets a value indicating whether this frame contains text data.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame contains text data; otherwise, <c>false</c>.
        /// </value>
        bool IsTextData { get; }

        /// <summary>
        /// Gets a value indicating whether this ftame is a fragment.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is a fragment; otherwise, <c>false</c>.
        /// </value>
        bool IsFragment { get; }

        /// <summary>
        /// Gets a value indicating whether this frame is the first fragment in a series of fragments.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is the first fragment; otherwise, <c>false</c>.
        /// </value>
        bool IsFirstFragment { get; }

        /// <summary>
        /// Gets a value indicating whether this frame is the last fragment in a series of fragments.
        /// </summary>
        /// <value>
        /// <c>true</c> if this frame is the last fragment; otherwise, <c>false</c>.
        /// </value>
        bool IsLastFragment { get; }

        /// <summary>
        /// Gets the masking key.
        /// </summary>
        /// <value>
        /// The masking key.
        /// </value>
        byte[] MaskingKey { get; }

        /// <summary>
        /// Gets the opcode.
        /// </summary>
        /// <value>
        /// The opcode.
        /// </value>
        WebSocketOpcode Opcode { get; }

        /// <summary>
        /// Gets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        IWebSocketPayload Payload { get; }

        /// <summary>
        /// Writes the frame to the fiven layer asynchronous.
        /// </summary>
        /// <param name="layer">The layer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task WriteToAsync(IDataLayer layer, CancellationToken cancellationToken);
    }
}