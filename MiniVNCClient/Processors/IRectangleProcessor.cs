using MiniVNCClient.Data.RectangleEncodings;

namespace MiniVNCClient.Processors
{
    internal interface IRectangleProcessor
    {
        internal delegate void ProcessRectangleDelegate(nint buffer, int bufferSize, int bufferStride, RectangleInfo info, IRectangleData data, int bytesPerPixel, int depth);
        static void ProcessRectangle(nint buffer, int bufferSize, int bufferStride, RectangleInfo info, IRectangleData data, int bytesPerPixel, int depth)
        {
        }
    }
}
