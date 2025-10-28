using MiniVNCClient.Data.RectangleEncodings;

namespace MiniVNCClient.Decoders
{
    internal interface IRectangleDecoder
    {
        IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth);
    }
}
