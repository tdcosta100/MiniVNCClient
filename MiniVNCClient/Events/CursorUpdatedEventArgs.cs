using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MiniVNCClient.Types;

namespace MiniVNCClient.Events
{
	public class RemoteCursorUpdatedEventArgs : PseudoEncodingEventArgs
	{
		#region Properties
		public Int32Rect SizeAndTipPosition { get; }
		public byte[] Data { get; }
		public byte[] BitMask { get; }
		#endregion

		#region Constructors
		public RemoteCursorUpdatedEventArgs(Int32Rect sizeAndTipPosition, byte[] data, byte[] bitMask) : base(VNCEncoding.Cursor)
		{
			SizeAndTipPosition = sizeAndTipPosition;
			Data = data;
			BitMask = bitMask;
		}
		#endregion
	}
}
