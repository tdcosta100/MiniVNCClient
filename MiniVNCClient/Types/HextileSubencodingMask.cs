using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
