using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	[StructLayout(LayoutKind.Sequential), Serializable]
	public struct ServerInit
	{
		public ushort FrameBufferWidth;
		public ushort FrameBufferHeight;
		public PixelFormat PixelFormat;

		[NonSerialized]
		public string Name;

		public static ServerInit Deserialize(Stream stream)
		{
			var reader = new BinaryReader(stream);

			var serverInit = new ServerInit()
			{
				FrameBufferWidth = Util.BinaryConverter.ToUInt16(reader.ReadBytes(2)),
				FrameBufferHeight = Util.BinaryConverter.ToUInt16(reader.ReadBytes(2)),
				PixelFormat = new PixelFormat()
				{
					BitsPerPixel = reader.ReadByte(),
					Depth = reader.ReadByte(),
					BigEndianFlag = reader.ReadByte(),
					TrueColorFlag = reader.ReadByte(),
					RedMax = Util.BinaryConverter.ToUInt16(reader.ReadBytes(2)),
					GreenMax = Util.BinaryConverter.ToUInt16(reader.ReadBytes(2)),
					BlueMax = Util.BinaryConverter.ToUInt16(reader.ReadBytes(2)),
					RedShift = reader.ReadByte(),
					GreenShift = reader.ReadByte(),
					BlueShift = reader.ReadByte()
				}
			};

			var padding = reader.ReadBytes(3);

			var nameSize = Util.BinaryConverter.ToUInt32(reader.ReadBytes(4));

			serverInit.Name = Encoding.UTF8.GetString(reader.ReadBytes((int)nameSize));

			return serverInit;
		}
	}
}
