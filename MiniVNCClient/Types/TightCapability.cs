
using System.IO;

using System.Runtime.InteropServices;

namespace MiniVNCClient.Types
{
	[StructLayout(LayoutKind.Sequential)]
	public struct TightCapability
	{
		public int Code;
		public string Vendor;
		public string Signature;

		public static TightCapability Deserialize(Stream stream)
		{
			var reader = new Util.BinaryReader(stream);

			return new TightCapability()
			{
				Code = reader.ReadInt32(),
				Vendor = reader.ReadString(4),
				Signature = reader.ReadString(8)
			};
		}
	}
}
