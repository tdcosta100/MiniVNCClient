using System.Buffers.Binary;

namespace MiniVNCClient
{
    internal class BinaryStream(Stream stream) : IDisposable
    {
        private readonly object _Lock = new();
        private bool _Disposed = false;

        public Stream Stream => stream;

        public byte ReadByte()
        {
            var byteValue = stream.ReadByte();

            if (byteValue < 0)
            {
                throw new Exception("Error reading data from the stream");
            }

            return (byte)byteValue;
        }

        public byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            stream.ReadExactly(buffer);
            return buffer;
        }

        public ushort ReadUInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public ushort ReadUInt16LE()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        public uint ReadUInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public uint ReadUInt32LE()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public ulong ReadUInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        public ulong ReadUInt64LE()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        public sbyte ReadSByte()
        {
            var byteValue = stream.ReadByte();

            if (byteValue < 0)
            {
                throw new Exception("Error reading data from the stream");
            }

            return unchecked((sbyte)byteValue);
        }

        public short ReadInt16()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public short ReadInt16LE()
        {
            Span<byte> buffer = stackalloc byte[2];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public int ReadInt32()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public int ReadInt32LE()
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public long ReadInt64()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64BigEndian(ReadBytes(8));
        }

        public long ReadInt64LE()
        {
            Span<byte> buffer = stackalloc byte[8];
            stream.ReadExactly(buffer);
            return BinaryPrimitives.ReadInt64LittleEndian(ReadBytes(8));
        }

        public void Write(byte value)
        {
            stream.WriteByte(value);
        }

        public void Write(Span<byte> bytes)
        {
            stream.Write(bytes);
        }

        public void Write(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            Write(buffer);
        }

        public void Write(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            Write(buffer);
        }

        public void Write(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
            Write(buffer);
        }

        public void Write(sbyte value)
        {
            Write(unchecked((byte)value));
        }

        public void Write(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            BinaryPrimitives.WriteInt16BigEndian(buffer, value);
            Write(buffer);
        }

        public void Write(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            Write(buffer);
        }

        public void Write(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(buffer, value);
            Write(buffer);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (_Lock)
            {
                if (_Disposed)
                {
                    return;
                }

                _Disposed = true;

                stream.Close();
            }
        }
    }
}
