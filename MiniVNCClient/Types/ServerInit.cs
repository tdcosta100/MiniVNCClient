using System;

using System.Runtime.InteropServices;


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

		public static ServerInit Deserialize(Util.BinaryReader reader)
		{
			var serverInit = new ServerInit()
			{
				FrameBufferWidth = reader.ReadUInt16(),
				FrameBufferHeight = reader.ReadUInt16(),
				PixelFormat = PixelFormat.Deserialize(reader)
			};

			var nameSize = reader.ReadUInt32();

			serverInit.Name = reader.ReadString((int)nameSize);

			return serverInit;
		}
	}
}
