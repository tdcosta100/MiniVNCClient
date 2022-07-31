// To turn on the debug messages, comment the line below
#undef DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Util
{
	public class BinaryReader : System.IO.BinaryReader
	{
#if DEBUG
        private MemoryStream _MemoryStream = new MemoryStream();
#endif

		#region Construtores
		public BinaryReader(Stream input) : base(input)
		{
		}

		public BinaryReader(Stream input, Encoding encoding) : base(input, encoding)
		{
		}
        #endregion

        public override short ReadInt16()
		{
			var bytes = ReadBytes(2);

			return (short)((bytes[0] << 8) | bytes[1]);
		}

		public override int ReadInt32()
		{
			var bytes = ReadBytes(4);

			return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
		}

		public override long ReadInt64()
		{
			var bytes = ReadBytes(8);

			uint high = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
			uint low = (uint)((bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7]);

			return (long)(((ulong)high << 32) | low);
		}

		public override ushort ReadUInt16()
		{
			var bytes = ReadBytes(2);

			return (ushort)((bytes[0] << 8) | bytes[1]);
		}

		public override uint ReadUInt32()
		{
			var bytes = ReadBytes(4);

			return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
		}

		public override ulong ReadUInt64()
		{
			var bytes = ReadBytes(8);

			uint high = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
			uint low = (uint)((bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7]);

			return ((ulong)high << 32) | low;
		}

#if DEBUG
        public override int Read(byte[] buffer, int index, int count)
        {
			int bytesRead = base.Read(buffer, index, count);

			_MemoryStream.Write(buffer, index, count);
			
			return bytesRead;
        }
#endif

		public string ReadString(int length)
		{
			return Encoding.UTF8.GetString(ReadBytes(length));
		}

#if DEBUG
        public override byte ReadByte()
		{
			return ReadBytes(1)[0];
		}

		public override byte[] ReadBytes(int count)
		{
			var bytes = base.ReadBytes(count);

			_MemoryStream.Write(bytes, 0, bytes.Length);

			return bytes;
		}

		public byte[] GetBuffer()
        {
			return _MemoryStream.GetBuffer();
        }

		public void ClearBuffer()
        {
			_MemoryStream.SetLength(0);
        }
#endif
	}
}
