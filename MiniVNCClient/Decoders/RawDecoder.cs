using MiniVNCClient.Data.RectangleEncodings;

namespace MiniVNCClient.Decoders
{
    internal class RawDecoder : IRectangleDecoder
    {
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            return new RawRectangleData()
            {
                PixelData = stream.ReadBytes(rectangleInfo.Width * rectangleInfo.Height * bytesPerPixel)
            };
        }
    }
}
