using MiniVNCClient.Data.RectangleEncodings;
using MiniVNCClient.Util;

namespace MiniVNCClient.Decoders
{
    internal class CopyRectDecoder : IRectangleDecoder
    {
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            return Serializer.Deserialize<CopyRectRectangleData>(stream);
        }
    }
}
