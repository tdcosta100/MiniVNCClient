﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

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
			return BitConverter.GetBytes(_Shared);
		}
	}
}
