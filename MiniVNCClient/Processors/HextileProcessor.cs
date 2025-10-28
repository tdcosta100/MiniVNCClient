using MiniVNCClient.Data.RectangleEncodings;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Processors
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

            Parallel.ForEach(rectangleData.Rectangles, rectangle =>
            {
                var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);
                var width = rectangle.Width * bytesPerPixel;
                var row = rectangle.Y * bufferStride;
                var rowEnd = row + rectangle.Height * bufferStride;
                var column = rectangle.X * bytesPerPixel;

                if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.Raw))
                {
                    var pixelData = rectangle.PixelData.AsSpan();

                    for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += width, row += bufferStride)
                    {
                        pixelData
                            .Slice(start: pixelDataRow, length: width)
                            .CopyTo(bufferSpan.Slice(start: row + column, length: width));
                    }
                }
                else
                {
                    Span<byte> rowData = stackalloc byte[width];

                    if (rectangle.BackgroundColor is not null)
                    {
                        for (int x = 0; x < width; x += bytesPerPixel)
                        {
                            rectangle.BackgroundColor.CopyTo(rowData.Slice(start: x, length: bytesPerPixel));
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
                            for (int x = 0; x < width; x += bytesPerPixel)
                            {
                                rectangle.ForegroundColor.CopyTo(rowData.Slice(start: x, length: bytesPerPixel));
                            }
                        }

                        foreach (var subrectangle in rectangle.Subrectangles)
                        {
                            if (subrectangle.Color is not null)
                            {
                                width = subrectangle.Width * bytesPerPixel;
                                row = subrectangle.Y * bufferStride;
                                rowEnd = row + subrectangle.Height * bufferStride;
                                column = subrectangle.X * bytesPerPixel;

                                for (int x = 0; x < width; x += bytesPerPixel)
                                {
                                    subrectangle.Color.CopyTo(rowData.Slice(start: x, length: bytesPerPixel));
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
