using MiniVNCClient.Data.RectangleEncodings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniVNCClient.Processors
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
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);

            var width = rectangle.Width * bytesPerPixel;
            var cPixelWidth = rectangle.Width * bytesPerCPixel;
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X * bytesPerPixel;
            var columnEnd = columnStart + width;
            var pixelData = rectangle.PixelData.AsSpan();

            if (bytesPerCPixel == bytesPerPixel)
            {
                for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += cPixelWidth, row += bufferStride)
                {
                    pixelData
                        .Slice(start: pixelDataRow, length: cPixelWidth)
                        .CopyTo(bufferSpan.Slice(start: row + columnStart, length: width));
                }
            }
            else
            {
                Span<byte> pixel = stackalloc byte[bytesPerPixel];

                for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += cPixelWidth, row += bufferStride)
                {
                    var column = columnStart;

                    for (var pixelDataColumn = pixelDataRow; column < columnEnd; pixelDataColumn += bytesPerCPixel, column += bytesPerPixel)
                    {
                        var bufferPixel = bufferSpan.Slice(start: row + column, length: bytesPerPixel);
                        bufferPixel.Fill(0xff);

                        pixelData
                            .Slice(start: pixelDataColumn, length: bytesPerCPixel)
                            .CopyTo(bufferPixel);
                    }
                }
            }
        }

        private static void ProcessSolidColor(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);

            var width = rectangle.Width * bytesPerPixel;
            Span<byte> rowData = stackalloc byte[width];
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X * bytesPerPixel;
            Span<byte> color = stackalloc byte[bytesPerPixel];
            color.Fill(0xff);

            rectangle.PaletteData.CopyTo(color);

            for (int x = 0; x < width; x += bytesPerPixel)
            {
                color.CopyTo(rowData.Slice(start: x, length: bytesPerPixel));
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

            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);
            var paletteData = rectangle.PaletteData.AsSpan();

            var palette = new byte[paletteSize][];

            for (int i = 0; i < paletteSize; i++)
            {
                palette[i] = new byte[bytesPerPixel];
                paletteData.Slice(start: i * bytesPerCPixel, length: bytesPerCPixel).CopyTo(palette[i]);

                if (bytesPerCPixel < bytesPerPixel)
                {
                    Array.Fill(palette[i][bytesPerCPixel..], (byte)0xff);
                }
            }

            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X * bytesPerPixel;
            var packedRowStride = (rectangle.Width + 8 / bitsPerPixel - 1) / (8 / bitsPerPixel);

            var pixelData = rectangle.PixelData.AsSpan();
            Span<byte> dummyColor = stackalloc byte[bytesPerPixel];
            dummyColor[bytesPerCPixel..].Fill(0xff);

            for (var pixelDataRow = 0; row < rowEnd; pixelDataRow += packedRowStride, row += bufferStride)
            {
                for (var column = 0; column < rectangle.Width; column++)
                {
                    var currentByte = column * bitsPerPixel / 8;
                    var currentShift = 8 - (column * bitsPerPixel % 8 + bitsPerPixel);
                    var paletteIndex = (pixelData[currentByte] & _BitsPerPixelBitValue[bitsPerPixel][column % (8 / bitsPerPixel)]) >> currentShift;

                    Span<byte> bufferPixel = bufferSpan.Slice(start: row + columnStart + column * bytesPerPixel, length: bytesPerPixel);

                    ((paletteIndex < paletteSize) ? palette[paletteIndex] : dummyColor)
                        .CopyTo(bufferPixel);
                }
            }
        }

        private static void ProcessRLE(ZRLERectangle rectangle, int bytesPerCPixel, int bytesPerPixel, nint buffer, int bufferSize, int bufferStride)
        {
            var paletteSize = rectangle.PaletteData.Length / bytesPerCPixel;
            var bufferSpan = MemoryMarshal.CreateSpan(ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), buffer), bufferSize);
            var paletteData = rectangle.PaletteData.AsSpan();
            var pixelData = rectangle.PixelData.AsSpan();

            var palette = new byte[paletteSize][];

            for (var i = 0; i < paletteSize; i++)
            {
                palette[i] = new byte[bytesPerPixel];
                paletteData.Slice(start: i * bytesPerCPixel, length: bytesPerCPixel).CopyTo(palette[i]);

                if (bytesPerCPixel < bytesPerPixel)
                {
                    Array.Fill(palette[i][bytesPerCPixel..], (byte)0xff);
                }
            }

            Span<byte> dummyColor = stackalloc byte[bytesPerPixel];
            dummyColor[bytesPerCPixel..].Fill(0xff);
            Span<byte> color = stackalloc byte[bytesPerPixel];
            color[bytesPerCPixel..].Fill(0xff);

            var width = rectangle.Width * bytesPerPixel;
            Span<byte> rowData = stackalloc byte[width];
            var row = rectangle.Y * bufferStride;
            var rowEnd = row + rectangle.Height * bufferStride;
            var columnStart = rectangle.X * bytesPerPixel;
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
                        runLength = rectangle.RunLengths[runLengthIndex].RunLength * bytesPerPixel;
                        fullLines = (runLength - (column - columnStart) - ((column + runLength) % width)) / width;

                        var colorIndex = rectangle.RunLengths[runLengthIndex].ColorIndex;

                        ((colorIndex < paletteSize) ? palette[colorIndex] : dummyColor).CopyTo(color);

                        if (fullLines > 0)
                        {
                            for (var x = 0; x < width; x += bytesPerPixel)
                            {
                                color.CopyTo(rowData.Slice(start: x, length: bytesPerPixel));
                            }
                        }
                    }

                    if (fullLines > 0)
                    {
                        var rowLength = Math.Min(runLength, width - (column - columnStart));
                        rowData[..rowLength].CopyTo(bufferSpan.Slice(start: row + column, rowLength));
                        column += rowLength;
                        runLength -= rowLength;
                        continue;
                    }

                    color.CopyTo(bufferSpan.Slice(start: row + column, bytesPerPixel));
                    column += bytesPerPixel;
                    runLength -= bytesPerPixel;
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
                    _SubencodingProcessors[rectangle.SubencodingType](rectangle, bytesPerCPixel, bytesPerPixel, buffer, bufferSize, bufferStride);
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
