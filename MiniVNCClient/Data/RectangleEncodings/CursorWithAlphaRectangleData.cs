using MiniVNCClient.Decoders;
using MiniVNCClient.Processors;

namespace MiniVNCClient.Data.RectangleEncodings
{
    internal class CursorWithAlphaRectangleData : CursorData
    {
        public IRectangleData? CursorData { get; set; }
        internal IRectangleProcessor.ProcessRectangleDelegate? ProcessRectangle { get; set; }
    }
}
