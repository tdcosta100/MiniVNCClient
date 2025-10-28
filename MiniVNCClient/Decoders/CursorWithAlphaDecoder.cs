using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using MiniVNCClient.Processors;
using System.Buffers.Binary;

namespace MiniVNCClient.Decoders
{
    internal class CursorWithAlphaDecoder(IDictionary<VNCEncoding, IRectangleDecoder> decoders, IDictionary<VNCEncoding, IRectangleProcessor.ProcessRectangleDelegate> processors) : IRectangleDecoder
    {
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            rectangleInfo.Encoding = (VNCEncoding)stream.ReadInt32();

            return new CursorWithAlphaRectangleData()
            {
                HotspotX = rectangleInfo.X,
                HotspotY = rectangleInfo.Y,
                Width = rectangleInfo.Width,
                Height = rectangleInfo.Height,
                CursorData = decoders[rectangleInfo.Encoding].Decode(stream, rectangleInfo, bytesPerPixel, depth),
                ProcessRectangle = processors.TryGetValue(rectangleInfo.Encoding, out IRectangleProcessor.ProcessRectangleDelegate? processRectangle)
                    ? processRectangle
                    : null
            };
        }
    }
}
