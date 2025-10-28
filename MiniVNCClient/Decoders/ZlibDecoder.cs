using MiniVNCClient.Data.RectangleEncodings;
using System.Buffers.Binary;
using System.IO.Compression;

namespace MiniVNCClient.Decoders
{
    internal class ZlibDecoder(RawDecoder rawDecoder) : IRectangleDecoder, IDisposable
    {
        private MemoryStream? _CompressedDataStream;
        private ZLibStream? _ZlibStream;
        private BinaryStream? _ZlibBinaryStream;
        private int _DisposeCount = 0;

        public IRectangleData Decode(BinaryStream stream, RectangleInfo rectangleInfo, int bytesPerPixel, int depth)
        {
            _CompressedDataStream ??= new MemoryStream();

            _CompressedDataStream.Position = 0;
            _CompressedDataStream.Write(stream.ReadBytes((int)stream.ReadUInt32()));
            _CompressedDataStream.SetLength(_CompressedDataStream.Position);
            _CompressedDataStream.Position = 0;

            if (_ZlibStream is null)
            {
                _ZlibStream = new ZLibStream(_CompressedDataStream, CompressionMode.Decompress);
                _ZlibBinaryStream = new BinaryStream(_ZlibStream);
            }

            return rawDecoder.Decode(_ZlibBinaryStream!, rectangleInfo, bytesPerPixel, depth);
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _DisposeCount) == 1)
            {
                _ZlibBinaryStream?.Close();
            }
        }
    }
}
