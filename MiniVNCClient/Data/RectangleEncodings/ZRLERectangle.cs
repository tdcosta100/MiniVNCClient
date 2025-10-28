namespace MiniVNCClient.Data.RectangleEncodings
{
    internal class ZRLERectangle
    {
        internal class RunLengthData
        {
            public int ColorIndex { get; set; }
            public int RunLength { get; set; }
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ZRLESubencodingType SubencodingType { get; set; }
        public byte[] PixelData { get; set; } = [];
        public byte[] PaletteData { get; set; } = [];
        public RunLengthData[] RunLengths { get; set; } = [];
    }
}
