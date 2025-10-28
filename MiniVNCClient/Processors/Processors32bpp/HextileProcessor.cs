using MiniVNCClient.Data.RectangleEncodings;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Processors.Processors32bpp
{
    internal class HextileProcessor : IRectangleProcessor
    {
        public static void ProcessRectangle(nint buffer, int bufferSize, int bufferStride, RectangleInfo info, IRectangleData data, int bytesPerPixel, int depth)
        {
            var rectangleData = (HextileRectangleData)data;

            if (rectangleData.Rectangles is null)
            {
                return;
            }

            bufferSize /= 4;
            bufferStride /= 4;

            Parallel.ForEach(rectangleData.Rectangles, rectangle =>
            {
                var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), buffer), bufferSize);
                var width = rectangle.Width;
                var row = rectangle.Y * bufferStride;
                var rowEnd = row + rectangle.Height * bufferStride;
                var column = rectangle.X;

                if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.Raw))
                {
                    var pixelData = MemoryMarshal.Cast<byte, int>(rectangle.PixelData);

                    for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += width, row += bufferStride)
                    {
                        pixelData
                            .Slice(start: pixelDataRow, length: width)
                            .CopyTo(bufferSpan.Slice(start: row + column, length: width));
                    }
                }
                else
                {
                    Span<int> rowData = stackalloc int[width];

                    if (rectangle.BackgroundColor is not null)
                    {
                        var backgroundColor = BinaryPrimitives.ReadInt32LittleEndian(rectangle.BackgroundColor);

                        for (int x = 0; x < width; x++)
                        {
                            rowData[x] = backgroundColor;
                        }

                        for (; row < rowEnd; row += bufferStride)
                        {
                            rowData.CopyTo(bufferSpan.Slice(start: row + column, length: width));
                        }
                    }

                    if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.AnySubrects) && rectangle.Subrectangles is not null)
                    {
                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.ForegroundSpecified) && rectangle.ForegroundColor is not null)
                        {
                            var foregroundColor = BinaryPrimitives.ReadInt32LittleEndian(rectangle.ForegroundColor);

                            for (int x = 0; x < width; x++)
                            {
                                rowData[x] = foregroundColor;
                            }
                        }

                        foreach (var subrectangle in rectangle.Subrectangles)
                        {
                            if (subrectangle.Color is not null)
                            {
                                width = subrectangle.Width;
                                row = subrectangle.Y * bufferStride;
                                rowEnd = row + subrectangle.Height * bufferStride;
                                column = subrectangle.X;

                                var color = BinaryPrimitives.ReadInt32LittleEndian(subrectangle.Color);

                                for (int x = 0; x < width; x++)
                                {
                                    rowData[x] = color;
                                }

                                for (; row < rowEnd; row += bufferStride)
                                {
                                    rowData[..width].CopyTo(bufferSpan.Slice(start: row + column, length: width));
                                }
                            }
                        }
                    }
                }
            });
        }
    }
}
