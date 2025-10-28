using MiniVNCClient.Data.RectangleEncodings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Processors
{
    internal class RawProcessor : IRectangleProcessor
    {
        public static void ProcessRectangle(nint buffer, int bufferSize, int bufferStride, RectangleInfo info, IRectangleData data, int bytesPerPixel, int depth)
        {
            var rectangleData = (RawRectangleData)data;

            if (rectangleData.PixelData is not null)
            {
                var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);
                var width = info.Width * bytesPerPixel;
                var row = info.Y * bufferStride;
                var rowEnd = row + info.Height * bufferStride;
                var column = info.X * bytesPerPixel;

                var pixelData = rectangleData.PixelData.AsSpan();

                for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += width, row += bufferStride)
                {
                    pixelData
                        .Slice(start: pixelDataRow, length: width)
                        .CopyTo(bufferSpan.Slice(start: row + column, length: width));
                }
            }
        }
    }
}
