using MiniVNCClient.Data.RectangleEncodings;

namespace MiniVNCClient.Decoders
{
    internal class CursorDecoder : IRectangleDecoder
    {
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            return new CursorRectangleData()
            {
                HotspotX = rectangleInfo.X,
                HotspotY = rectangleInfo.Y,
                Width = rectangleInfo.Width,
                Height = rectangleInfo.Height,
                PixelData = stream.ReadBytes(rectangleInfo.Width * rectangleInfo.Height * bytesPerPixel),
                BitMask = stream.ReadBytes((rectangleInfo.Width + 7) / 8 * rectangleInfo.Height)
            };
        }
    }
}
