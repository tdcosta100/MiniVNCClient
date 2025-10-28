namespace MiniVNCClient.Data.RectangleEncodings
{
    internal class HextileRectangle
    {
        internal class Subrectangle
        {
            public byte[]? Color { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public HextileSubencodingMask SubencodingMask { get; set; }
        public byte[]? PixelData { get; set; }
        public byte[]? BackgroundColor { get; set; }
        public byte[]? ForegroundColor { get; set; }
        public Subrectangle[]? Subrectangles { get; set; }
    }
}
