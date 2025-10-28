namespace MiniVNCClient.Data.RectangleEncodings
{
    internal class ZRLERectangleData : IRectangleData
    {
        public int BytesPerCPixel { get; set; }
        public Task<ZRLERectangle[]>? RectanglesTask { get; set; }
    }
}
