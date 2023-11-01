using System;

namespace MiniVNCClient.Types
{
	[Flags]
	public enum HextileSubencodingMask : byte
	{
		Raw = 1,
		BackgroundSpecified = 2,
		ForegroundSpecified = 4,
		AnySubrects = 8,
		SubrectsColoured = 16,
		ZlibRaw = 32,
		Zlib = 64
	}
}
