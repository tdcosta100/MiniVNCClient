using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	[StructLayout(LayoutKind.Sequential), Serializable]
	public struct PixelFormat
	{
		public byte BitsPerPixel;
		public byte Depth;
		public byte BigEndianFlag;
		public byte TrueColorFlag;
		public ushort RedMax;
		public ushort GreenMax;
		public ushort BlueMax;
		public byte RedShift;
		public byte GreenShift;
		public byte BlueShift;
		public byte[] Padding;
	}
}
