using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniVNCClient.Types
{
	public enum ZRLESubencodingType : byte
	{
		Raw = 0,
		SolidColor = 1,
		PackedPalette = 2,
		Unused = 17,
		PlainRLE = 128,
		PaletteRLE = 130
	}
}
