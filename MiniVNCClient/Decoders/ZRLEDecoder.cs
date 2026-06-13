using MiniVNCClient.Data.RectangleEncodings;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace MiniVNCClient.Decoders
{
    internal class ZRLEDecoder : IRectangleDecoder, IDisposable
    {
        private sealed class MutableWindowStream(Stream compressedStream) : Stream
        {
            private long _RemainingBytes;

            public void Reset(long compressedLength) => _RemainingBytes = compressedLength;

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_RemainingBytes <= 0)
                {
                    return 0;
                }

                int bytesRead = compressedStream.Read(buffer, offset, (int)Math.Min(count, _RemainingBytes));

                _RemainingBytes -= bytesRead;

                return bytesRead;
            }

            public override int Read(Span<byte> buffer)
            {
                if (_RemainingBytes <= 0)
                {
                    return 0;
                }

                int bytesRead = compressedStream.Read(buffer[..(int)Math.Min(buffer.Length, _RemainingBytes)]);

                _RemainingBytes -= bytesRead;

                return bytesRead;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        #region Fields
        private MutableWindowStream? _CompressedStream;
        private ZLibStream? _ZlibStream;
        private int _DisposeCount = 0;
        #endregion

        #region Private decode methods
        private static void DecodeRaw(BinaryStream stream, ZRLERectangle rectangle, int bytesPerCPixel)
        {
            rectangle.PixelData = stream.ReadBytes(rectangle.Width * rectangle.Height * bytesPerCPixel);
        }

        private static void DecodeSolidColor(BinaryStream stream, ZRLERectangle rectangle, int bytesPerCPixel)
        {
            rectangle.PaletteData = stream.ReadBytes(bytesPerCPixel);
        }

        private static void DecodePackedPalette(BinaryStream stream, ZRLERectangle rectangle, int bytesPerCPixel, int paletteSize)
        {
            rectangle.PaletteData = stream.ReadBytes(paletteSize * bytesPerCPixel);

            var bitsPerPixel = paletteSize switch
            {
                <= 2 => 1,
                <= 4 => 2,
                _ => 4
            };

            rectangle.PixelData = stream.ReadBytes((rectangle.Width + 8 / bitsPerPixel - 1) / (8 / bitsPerPixel) * rectangle.Height);
        }

        private static void DecodeRLE(BinaryStream stream, ZRLERectangle rectangle, int bytesPerCPixel, int paletteSize)
        {
            var maxRuns = rectangle.Width * rectangle.Height;
            var runLengths = new List<ZRLERectangle.RunLengthData>();

            rectangle.PaletteData = stream.ReadBytes(Math.Max(paletteSize, 1) * bytesPerCPixel);

            while (maxRuns > 0)
            {
                var paletteIndex = paletteSize > 1 ? stream.ReadByte() : (1 << 7);
                var multipleRunLengths = (paletteIndex & (1 << 7)) != 0;
                paletteIndex &= (1 << 7) - 1;

                var runLength = new ZRLERectangle.RunLengthData()
                {
                    ColorIndex = paletteIndex,
                    RunLength = 1
                };

                if (multipleRunLengths)
                {
                    byte currentByte;

                    do
                    {
                        currentByte = stream.ReadByte();
                        runLength.RunLength += currentByte;
                    } while (currentByte == byte.MaxValue);
                }

                maxRuns -= runLength.RunLength;
                runLengths.Add(runLength);
            }

            if (maxRuns != 0)
            {

            }

            rectangle.RunLengths = [.. runLengths];
        }

        private static Task<ZRLERectangle[]> BeginDecode(byte[] buffer, int bufferLength, RectangleInfo rectangleInfo, int bytesPerCPixel)
        {
            return Task.Run(() =>
            {
                try
                {
                    using var memoryStream = new MemoryStream(buffer, 0, bufferLength, false);
                    using var dataStream = new BinaryStream(memoryStream);

                    var rectangles = new ZRLERectangle[(rectangleInfo.Width + 63) / 64 * ((rectangleInfo.Height + 63) / 64)];

                    var i = 0;

                    for (int y = 0; y < rectangleInfo.Height; y += 64)
                    {
                        for (int x = 0; x < rectangleInfo.Width; x += 64)
                        {
                            var subencoding = dataStream.ReadByte();

                            var useRLE = (subencoding & (1 << 7)) != 0;
                            var paletteSize = subencoding & ((1 << 7) - 1);

                            var rectangle = new ZRLERectangle()
                            {
                                X = rectangleInfo.X + x,
                                Y = rectangleInfo.Y + y,
                                Width = Math.Min(rectangleInfo.Width - x, 64),
                                Height = Math.Min(rectangleInfo.Height - y, 64),
                                SubencodingType = useRLE
                                ?
                                paletteSize switch
                                {
                                    0 => ZRLESubencodingType.PlainRLE,
                                    _ => ZRLESubencodingType.PaletteRLE
                                }
                                :
                                paletteSize switch
                                {
                                    0 => ZRLESubencodingType.Raw,
                                    1 => ZRLESubencodingType.SolidColor,
                                    _ => ZRLESubencodingType.PackedPalette
                                }
                            };

                            switch (rectangle.SubencodingType)
                            {
                                case ZRLESubencodingType.Raw:
                                    DecodeRaw(dataStream, rectangle, bytesPerCPixel);
                                    break;
                                case ZRLESubencodingType.SolidColor:
                                    DecodeSolidColor(dataStream, rectangle, bytesPerCPixel);
                                    break;
                                case ZRLESubencodingType.PackedPalette:
                                    DecodePackedPalette(dataStream, rectangle, bytesPerCPixel, paletteSize);
                                    break;
                                case ZRLESubencodingType.PlainRLE:
                                case ZRLESubencodingType.PaletteRLE:
                                    DecodeRLE(dataStream, rectangle, bytesPerCPixel, paletteSize);
                                    break;
                                default:
                                    break;
                            }

                            rectangles[i++] = rectangle;
                        }
                    }

                    return rectangles;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            });
        }
        #endregion

        #region Public methods
        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            try
            {
                if (Interlocked.CompareExchange(ref _DisposeCount, 0, 0) != 0)
                {
                    return new ZRLERectangleData();
                }

                var compressedLength = (int)stream.ReadUInt32();
                _CompressedStream ??= new MutableWindowStream(stream.Stream);
                _CompressedStream.Reset(compressedLength);

                _ZlibStream ??= new ZLibStream(_CompressedStream, CompressionMode.Decompress, true);

                var bytesPerCPixel = (bytesPerPixel > 1) ? (depth > 24 ? 4 : 3) : 1;
                var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(rectangleInfo.Width * rectangleInfo.Height * bytesPerCPixel, 65536));

                try
                {
                    int decompressedSize = 0;
                    int bytesRead;

                    while ((bytesRead = _ZlibStream.Read(buffer, decompressedSize, buffer.Length - decompressedSize)) > 0)
                    {
                        decompressedSize += bytesRead;

                        if (decompressedSize >= buffer.Length - 4096)
                        {
                            var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                            buffer.AsSpan(0, decompressedSize).CopyTo(newBuffer);
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = newBuffer;
                        }
                    }

                    var rectanglesTask = BeginDecode(buffer, decompressedSize, rectangleInfo, bytesPerCPixel);

                    return new ZRLERectangleData()
                    {
                        BytesPerCPixel = bytesPerCPixel,
                        RectanglesTask = rectanglesTask
                    };
                }
                catch
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
                return new ZRLERectangleData();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _DisposeCount) == 1)
            {
                _ZlibStream?.Dispose();
            }
        }
        #endregion
    }
}
