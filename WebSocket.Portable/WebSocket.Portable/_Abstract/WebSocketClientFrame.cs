namespace WebSocket.Portable
{
    internal class WebSocketClientFrame : WebSocketFrame
    {
        public WebSocketClientFrame()
        {
            this.MaskingKey = WebSocketHelper.CreateMaskingKey();
            this.IsMasked = true;
        }
    }
}
