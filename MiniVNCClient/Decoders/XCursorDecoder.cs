using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using MiniVNCClient.Util;

namespace MiniVNCClient.Decoders
{
    internal class XCursorDecoder : IRectangleDecoder
    {
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            return new XCursorRectangleData()
            {
                HotspotX = rectangleInfo.X,
                HotspotY = rectangleInfo.Y,
                Width = rectangleInfo.Width,
                Height = rectangleInfo.Height,
                PrimaryColor = Serializer.Deserialize<Color8>(stream),
                SecondaryColor = Serializer.Deserialize<Color8>(stream),
                BitMap = stream.ReadBytes((rectangleInfo.Width + 7) / 8 * rectangleInfo.Height),
                BitMask = stream.ReadBytes((rectangleInfo.Width + 7) / 8 * rectangleInfo.Height)
            };
        }
    }
}
