namespace MiniVNCClient.Data.RectangleEncodings
{
    internal class XCursorRectangleData : CursorData
    {
        public Color8 PrimaryColor { get; set; }
        public Color8 SecondaryColor { get; set; }
        public byte[]? BitMap { get; set; }
        public byte[]? BitMask { get; set; }
    }
}
