using MiniVNCClient.Data.RectangleEncodings;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Processors.Processors32bpp
{
    internal class ZRLEProcessor : IRectangleProcessor
    {
        private delegate void ProcessSubencoding(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride);

        #region Fields
        private static readonly Dictionary<int, byte[]> _BitsPerPixelBitValue = new()
        {
            { 1, [
                0b0001 << 7,
                0b0001 << 6,
                0b0001 << 5,
                0b0001 << 4,
                0b0001 << 3,
                0b0001 << 2,
                0b0001 << 1,
                0b0001 << 0] },
            { 2, [
                0b0011 << 6,
                0b0011 << 4,
                0b0011 << 2,
                0b0011 << 0 ] },
            { 4, [
                0b1111 << 4,
                0b1111 << 0 ] }
        };

        private static readonly Dictionary<ZRLESubencodingType, ProcessSubencoding> _SubencodingProcessors = new()
        {
            { ZRLESubencodingType.Raw, ProcessRaw },
            { ZRLESubencodingType.SolidColor, ProcessSolidColor },
            { ZRLESubencodingType.PackedPalette, ProcessPackedPalette },
            { ZRLESubencodingType.PlainRLE, ProcessRLE },
            { ZRLESubencodingType.PaletteRLE, ProcessRLE }
        };
        #endregion

        #region Private methods
        private static void ProcessRaw(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), buffer), bufferSize);

            var width = rectangle.Width;
            var cPixelWidth = rectangle.Width * bytesPerCPixel;
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X;
            var columnEnd = columnStart + width;

            if (bytesPerCPixel == bytesPerPixel)
            {
                var pixelData = MemoryMarshal.Cast<byte, int>(rectangle.PixelData);

                for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += cPixelWidth, row += bufferStride)
                {
                    pixelData
                        .Slice(start: pixelDataRow, length: cPixelWidth)
                        .CopyTo(bufferSpan.Slice(start: row + columnStart, length: width));
                }
            }
            else
            {
                Span<byte> padding = stackalloc byte[bytesPerPixel - bytesPerCPixel];
                padding.Fill(0xff);

                var pixelData = rectangle.PixelData.AsSpan();

                for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += cPixelWidth, row += bufferStride)
                {
                    var column = columnStart;

                    for (var pixelDataColumn = pixelDataRow; column < columnEnd; pixelDataColumn += bytesPerCPixel, column++)
                    {
                        bufferSpan[row + column] = BinaryPrimitives.ReadInt32LittleEndian([..pixelData.Slice(start: pixelDataColumn, length: bytesPerCPixel), ..padding]);
                    }
                }
            }
        }

        private static void ProcessSolidColor(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), buffer), bufferSize);

            var width = rectangle.Width;
            Span<int> rowData = stackalloc int[width];
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X;
            Span<byte> padding = stackalloc byte[bytesPerPixel - bytesPerCPixel];
            padding.Fill(0xff);

            int color = (bytesPerCPixel == bytesPerPixel)
                ? BinaryPrimitives.ReadInt32LittleEndian(rectangle.PaletteData)
                : BinaryPrimitives.ReadInt32LittleEndian([..rectangle.PaletteData[..bytesPerCPixel], ..padding]);

            for (int x = 0; x < width; x++)
            {
                rowData[x] = color;
            }

            for (; row < rowEnd; row += bufferStride)
            {
                rowData.CopyTo(bufferSpan.Slice(start: row + columnStart, length: width));
            }
        }

        private static void ProcessPackedPalette(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var paletteSize = rectangle.PaletteData.Length / bytesPerCPixel;
            var bitsPerPixel = paletteSize switch
            {
                <= 2 => 1,
                <= 4 => 2,
                _ => 4
            };

            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), buffer), bufferSize);
            Span<int> palette;

            if (bytesPerCPixel == bytesPerPixel)
            {
                palette = MemoryMarshal.Cast<byte, int>(rectangle.PaletteData);
            }
            else
            {
                palette = new int[paletteSize];
                var paletteData = rectangle.PaletteData.AsSpan();
                Span<byte> padding = stackalloc byte[bytesPerPixel - bytesPerCPixel];
                padding.Fill(0xff);

                for (int i = 0; i < paletteSize; i++)
                {
                    palette[i] = BinaryPrimitives.ReadInt32LittleEndian([.. paletteData.Slice(start: i * bytesPerCPixel, length: bytesPerCPixel), .. padding]);
                }
            }

            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X;
            var packedRowStride = (rectangle.Width + 8 / bitsPerPixel - 1) / (8 / bitsPerPixel);

            var pixelData = rectangle.PixelData.AsSpan();
            var dummyColor = BinaryPrimitives.ReadInt32LittleEndian([0x0, 0x0, 0x0, 0xff]);

            for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += packedRowStride, row += bufferStride)
            {
                var pixelDataSpan = pixelData.Slice(start: pixelDataRow, length: packedRowStride);

                for (var column = 0; column < rectangle.Width; column++)
                {
                    var currentByte = column * bitsPerPixel / 8;
                    var currentShift = 8 - (column * bitsPerPixel % 8 + bitsPerPixel);
                    var paletteIndex = (pixelDataSpan[currentByte] & _BitsPerPixelBitValue[bitsPerPixel][column % (8 / bitsPerPixel)]) >> currentShift;

                    bufferSpan[row + columnStart + column] = (paletteIndex < paletteSize) ? palette[paletteIndex] : dummyColor;
                }
            }
        }

        private static void ProcessRLE(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var paletteSize = rectangle.PaletteData.Length / bytesPerCPixel;
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), buffer), bufferSize);

            Span<int> palette;
            Span<byte> padding = stackalloc byte[bytesPerPixel - bytesPerCPixel];
            padding.Fill(0xff);

            if (bytesPerCPixel == bytesPerPixel)
            {
                palette = MemoryMarshal.Cast<byte, int>(rectangle.PaletteData);
            }
            else
            {
                palette = new int[paletteSize];
                var paletteData = rectangle.PaletteData.AsSpan();

                for (var i = 0; i < paletteSize; i++)
                {
                    palette[i] = BinaryPrimitives.ReadInt32LittleEndian([.. paletteData.Slice(start: i * bytesPerCPixel, length: bytesPerCPixel), .. padding]);
                }
            }

            var dummyColor = BinaryPrimitives.ReadInt32LittleEndian([0x0, 0x0, 0x0, 0xff]);
            var color = 0;

            var width = rectangle.Width;
            Span<int> rowData = stackalloc int[width];
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X;
            var columnEnd = columnStart + width;

            var runLengthIndex = -1;
            var runLength = 0;
            var fullLines = 0;

            while (row < rowEnd)
            {
                var column = columnStart;

                while (column < columnEnd)
                {
                    if (runLength <= 0 && runLengthIndex < rectangle.RunLengths.Length)
                    {
                        runLengthIndex++;
                        runLength = rectangle.RunLengths[runLengthIndex].RunLength;
                        fullLines = (runLength - (column - columnStart) - ((column + runLength) % width)) / width;

                        var colorIndex = rectangle.RunLengths[runLengthIndex].ColorIndex;

                        color = (colorIndex < paletteSize) ? palette[colorIndex] : dummyColor;

                        if (fullLines > 0)
                        {
                            for (var x = 0; x < width; x++)
                            {
                                rowData[x] = color;
                            }
                        }
                    }

                    if (fullLines > 0)
                    {
                        var rowLength = Math.Min(runLength, width - (column - columnStart));
                        rowData[..rowLength].CopyTo(bufferSpan.Slice(start: row + column, length: rowLength));
                        column += rowLength;
                        runLength -= rowLength;
                        continue;
                    }

                    bufferSpan[row + column] = color;

                    column++;
                    runLength--;
                }

                row += bufferStride;
            }
        }
        #endregion

        #region Public methods
        public static void ProcessRectangle(nint buffer, int bufferSize, int bufferStride, RectangleInfo info, IRectangleData data, int bytesPerPixel, int depth)
        {
            var rectangleData = (ZRLERectangleData)data;

            if (rectangleData.RectanglesTask is null)
            {
                return;
            }

            var bytesPerCPixel = rectangleData.BytesPerCPixel;

            try
            {
                rectangleData.RectanglesTask.Wait();
                var rectangles = rectangleData.RectanglesTask.Result;

                Parallel.ForEach(rectangles, rectangle =>
                {
                    _SubencodingProcessors[rectangle.SubencodingType](rectangle, bytesPerCPixel, bytesPerPixel, buffer, bufferSize / 4, bufferStride / 4);
                });
            }
            catch
            {
                return;
            }
        }
        #endregion
    }
}
