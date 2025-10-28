using MiniVNCClient.Data.RectangleEncodings;
using System.Buffers.Binary;
using System.IO.Compression;

namespace MiniVNCClient.Decoders
{
    internal class ZRLEDecoder : IRectangleDecoder, IDisposable
    {
        #region Fields
        private MemoryStream? _CompressedDataStream;
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

        private static Task<ZRLERectangle[]> BeginDecode(Stream stream, RectangleInfo rectangleInfo, int bytesPerCPixel)
        {
            return Task.Run(() =>
            {
                using var dataStream = new BinaryStream(stream);

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

                _CompressedDataStream ??= new MemoryStream();

                var buffer = new byte[16384];

                var bytesRead = _CompressedDataStream.Read(buffer, 0, (int)(_CompressedDataStream.Length - _CompressedDataStream.Position));

                _CompressedDataStream.Position = 0;

                if (bytesRead > 0)
                {
                    _CompressedDataStream.Write(buffer, 0, bytesRead);
                }

                _CompressedDataStream.Write(stream.ReadBytes((int)stream.ReadUInt32()));
                _CompressedDataStream.SetLength(_CompressedDataStream.Position);
                _CompressedDataStream.Position = 0;

                var decompressedDataStream = new MemoryStream();

                _ZlibStream ??= new ZLibStream(_CompressedDataStream, CompressionMode.Decompress);

                while ((bytesRead = _ZlibStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    decompressedDataStream.Write(buffer, 0, bytesRead);
                }

                decompressedDataStream.Position = 0;

                var bytesPerCPixel = (bytesPerPixel > 1) ? (depth > 24 ? 4 : 3) : 1;
                var rectanglesTask = BeginDecode(decompressedDataStream, rectangleInfo, bytesPerCPixel);

                return new ZRLERectangleData()
                {
                    BytesPerCPixel = bytesPerCPixel,
                    RectanglesTask = rectanglesTask
                };
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
                _ZlibStream?.Close();
            }
        }
        #endregion
    }
}
