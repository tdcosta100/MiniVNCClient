using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Util.NativeWrappers
{
	public static class Kernel32
	{
		[DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
		public static extern void MoveMemory(IntPtr dest, IntPtr src, int size);
	}
}
