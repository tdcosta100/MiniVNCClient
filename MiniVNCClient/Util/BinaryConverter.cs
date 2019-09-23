using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
