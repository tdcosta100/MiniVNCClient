using System;
using System.IO;
using System.Runtime.InteropServices;


namespace MiniVNCClient.Types
{
	[StructLayout(LayoutKind.Sequential), Serializable]
	public struct ClientInit
	{
		private byte _Shared;

		public bool Shared { get => _Shared != 0; set => _Shared = (byte)(value ? 1 : 0); }

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(_Shared);
		}

		public byte[] ToByteArray()
		{
			return new byte[1] { _Shared };
		}
	}
}
