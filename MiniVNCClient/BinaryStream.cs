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
            var bytes = ReadBytes(2);
            return unchecked((ushort)(bytes[0] << 8 | bytes[1]));
        }

        public ushort ReadUInt16LE()
        {
            var bytes = ReadBytes(2);
            return unchecked((ushort)(bytes[1] << 8 | bytes[0]));
        }

        public uint ReadUInt32()
        {
            var bytes = ReadBytes(4);
            return unchecked((uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]));
        }

        public uint ReadUInt32LE()
        {
            var bytes = ReadBytes(4);
            return unchecked((uint)(bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0]));
        }

        public ulong ReadUInt64()
        {
            var bytes = ReadBytes(8);
            return unchecked(
                (ulong)bytes[0] << 56
                | (ulong)bytes[1] << 48
                | (ulong)bytes[2] << 40
                | (ulong)bytes[3] << 32
                | (ulong)bytes[4] << 24
                | (ulong)bytes[5] << 16
                | (ulong)bytes[6] << 8
                | bytes[7]
            );
        }

        public ulong ReadUInt64LE()
        {
            var bytes = ReadBytes(8);
            return unchecked(
                (ulong)bytes[7] << 56
                | (ulong)bytes[6] << 48
                | (ulong)bytes[5] << 40
                | (ulong)bytes[4] << 32
                | (ulong)bytes[3] << 24
                | (ulong)bytes[2] << 16
                | (ulong)bytes[1] << 8
                | bytes[0]
            );
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
            return unchecked((short)ReadUInt16());
        }

        public short ReadInt16LE()
        {
            return unchecked((short)ReadUInt16LE());
        }

        public int ReadInt32()
        {
            return unchecked((int)ReadUInt32());
        }

        public int ReadInt32LE()
        {
            return unchecked((int)ReadUInt32LE());
        }

        public long ReadInt64()
        {
            return unchecked((long)ReadUInt64());
        }

        public long ReadInt64LE()
        {
            return unchecked((long)ReadUInt64());
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
            Write([
                (byte)((value >> 8) & 0xff),
                (byte)(value & 0xff)
            ]);
        }

        public void Write(uint value)
        {
            Write([
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)(value & 0xff)
            ]);
        }

        public void Write(ulong value)
        {
            Write([
                (byte)((value >> 56) & 0xff),
                (byte)((value >> 48) & 0xff),
                (byte)((value >> 40) & 0xff),
                (byte)((value >> 32) & 0xff),
                (byte)((value >> 24) & 0xff),
                (byte)((value >> 16) & 0xff),
                (byte)((value >> 8) & 0xff),
                (byte)(value & 0xff)
            ]);
        }

        public void Write(sbyte value)
        {
            Write(unchecked((byte)value));
        }

        public void Write(short value)
        {
            Write(unchecked((ushort)value));
        }

        public void Write(int value)
        {
            Write(unchecked((uint)value));
        }

        public void Write(long value)
        {
            Write(unchecked((ulong)value));
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
