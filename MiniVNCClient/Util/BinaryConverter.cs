using System;
using System.Linq;


namespace MiniVNCClient.Util
{
	public static class BinaryConverter
	{
		public static short ToInt16(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToInt16(bytes, 0);
		}

		public static ushort ToUInt16(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToUInt16(bytes, 0);
		}

		public static int ToInt32(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToInt32(bytes, 0);
		}

		public static uint ToUInt32(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToUInt32(bytes, 0);
		}

		public static long ToInt64(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToInt64(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToInt64(bytes, 0);
		}

		public static ulong ToUInt64(byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.ToUInt64(bytes.Reverse().ToArray(), 0);
			}

			return BitConverter.ToUInt64(bytes, 0);
		}

		public static byte[] ToByteArray(short value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static byte[] ToByteArray(ushort value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static byte[] ToByteArray(int value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static byte[] ToByteArray(uint value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static byte[] ToByteArray(long value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static byte[] ToByteArray(ulong value)
		{
			if (BitConverter.IsLittleEndian)
			{
				return BitConverter.GetBytes(value).Reverse().ToArray();
			}

			return BitConverter.GetBytes(value);
		}

		public static short ReadInt16(BinaryReader reader)
		{
			return ToInt16(reader.ReadBytes(2));
		}

		public static ushort ReadUInt16(BinaryReader reader)
		{
			return ToUInt16(reader.ReadBytes(2));
		}

		public static int ReadInt32(BinaryReader reader)
		{
			return ToInt32(reader.ReadBytes(4));
		}

		public static uint ReadUInt32(BinaryReader reader)
		{
			return ToUInt32(reader.ReadBytes(4));
		}

		public static long ReadInt64(BinaryReader reader)
		{
			return ToInt64(reader.ReadBytes(8));
		}

		public static ulong ReadUInt64(BinaryReader reader)
		{
			return ToUInt64(reader.ReadBytes(8));
		}
	}
}
