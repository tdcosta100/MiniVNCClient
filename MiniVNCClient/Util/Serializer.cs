using System;

using System.Runtime.InteropServices;


namespace MiniVNCClient.Util
{
	public static class Serializer
	{
		public static byte[] Serialize<T>(T structure) where T : struct
		{
			int structureSize = Marshal.SizeOf(structure);

			IntPtr structurePointer = Marshal.AllocHGlobal(structureSize);

			Marshal.StructureToPtr(structure, structurePointer, false);

			var serializedStructure = new byte[structureSize];

			Marshal.Copy(structurePointer, serializedStructure, 0, structureSize);
			Marshal.FreeHGlobal(structurePointer);

			return serializedStructure;
		}

		public static T Deserialize<T>(byte[] serializedStructure, int position) where T : struct
		{
			int structureSize = Marshal.SizeOf(typeof(T));

			if (position + structureSize > serializedStructure.Length)
			{
				throw new InvalidOperationException($"Not enough data to fill the structure. The structure size is {structureSize} bytes, serializedStructure have only {serializedStructure.Length - position} bytes");
			}

			IntPtr structurePointer = Marshal.AllocHGlobal(structureSize);

			var structure = (T)Marshal.PtrToStructure(structurePointer, typeof(T));

			Marshal.FreeHGlobal(structurePointer);

			return structure;
		}

		public static T Deserialize<T>(byte[] serializedStructure) where T : struct
		{
			return Deserialize<T>(serializedStructure, 0);
		}
	}
}
