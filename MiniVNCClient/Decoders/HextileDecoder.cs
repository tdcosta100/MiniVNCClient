using MiniVNCClient.Data.RectangleEncodings;
using System.Buffers.Binary;
using System.IO.Compression;

namespace MiniVNCClient.Decoders
{
    internal class HextileDecoder : IRectangleDecoder, IDisposable
    {
        private MemoryStream? _CompressedDataStream;
        private ZLibStream? _ZlibRawStream;
        private ZLibStream? _ZlibStream;
        private BinaryStream? _ZlibRawBinaryStream;
        private BinaryStream? _ZlibBinaryStream;
        private int _DisposeCount = 0;

        public IRectangleData Decode(BinaryStream rfbStream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            byte[]? backgroundColor = null;
            byte[]? foregroundColor = null;

            var result = new HextileRectangleData()
            {
                Rectangles = new HextileRectangle[(rectangleInfo.Width + 15) / 16 * ((rectangleInfo.Height + 15) / 16)]
            };

            var i = 0;

            for (int y = 0; y < rectangleInfo.Height; y += 16)
            {
                for (int x = 0; x < rectangleInfo.Width; x += 16)
                {
                    var rectangle = new HextileRectangle()
                    {
                        X = rectangleInfo.X + x,
                        Y = rectangleInfo.Y + y,
                        Width = Math.Min(rectangleInfo.Width - x, 16),
                        Height = Math.Min(rectangleInfo.Height - y, 16),
                        SubencodingMask = (HextileSubencodingMask)rfbStream.ReadByte()
                    };

                    BinaryStream dataStream = rfbStream;

                    if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.ZlibRaw) || rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.Zlib))
                    {
                        _CompressedDataStream ??= new MemoryStream();

                        _CompressedDataStream.Position = 0;
                        _CompressedDataStream.Write(dataStream.ReadBytes(rfbStream.ReadUInt16()));
                        _CompressedDataStream.SetLength(_CompressedDataStream.Position);
                        _CompressedDataStream.Position = 0;

                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.ZlibRaw))
                        {
                            if (_ZlibRawStream is null)
                            {
                                _ZlibRawStream = new ZLibStream(_CompressedDataStream, CompressionMode.Decompress);
                                _ZlibRawBinaryStream = new BinaryStream(_ZlibRawStream);
                            }

                            dataStream = _ZlibRawBinaryStream!;
                        }

                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.Zlib))
                        {
                            if (_ZlibStream is null)
                            {
                                _ZlibStream = new ZLibStream(_CompressedDataStream, CompressionMode.Decompress);
                                _ZlibBinaryStream = new BinaryStream(_ZlibStream);
                            }

                            dataStream = _ZlibBinaryStream!;
                        }
                    }

                    if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.Raw))
                    {
                        rectangle.PixelData = dataStream.ReadBytes(rectangle.Width * rectangle.Height * bytesPerPixel);
                    }
                    else
                    {
                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.BackgroundSpecified))
                        {
                            backgroundColor = dataStream.ReadBytes(bytesPerPixel);
                        }

                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.ForegroundSpecified))
                        {
                            foregroundColor = dataStream.ReadBytes(bytesPerPixel);
                        }

                        rectangle.BackgroundColor = backgroundColor;
                        rectangle.ForegroundColor = foregroundColor;

                        if (rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.AnySubrects))
                        {
                            rectangle.Subrectangles = new HextileRectangle.Subrectangle[dataStream.ReadByte()];

                            for (int j = 0; j < rectangle.Subrectangles.Length; j++)
                            {
                                var subrectangle = new HextileRectangle.Subrectangle()
                                {
                                    Color = rectangle.SubencodingMask.HasFlag(HextileSubencodingMask.SubrectsColoured) ? dataStream.ReadBytes(bytesPerPixel) : foregroundColor
                                };

                                var xAndY = dataStream.ReadByte();
                                var widthAndHeight = dataStream.ReadByte();

                                subrectangle.X = rectangle.X + ((xAndY & 0b11110000) >> 4);
                                subrectangle.Y = rectangle.Y + (xAndY & 0b00001111);
                                subrectangle.Width = ((widthAndHeight & 0b11110000) >> 4) + 1;
                                subrectangle.Height = (widthAndHeight & 0b00001111) + 1;

                                rectangle.Subrectangles[j] = subrectangle;
                            }
                        }
                    }

                    result.Rectangles[i++] = rectangle;
                }
            }

            return result;
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _DisposeCount) == 1)
            {
                _ZlibRawBinaryStream?.Close();
                _ZlibBinaryStream?.Close();
            }
        }

    }
}
