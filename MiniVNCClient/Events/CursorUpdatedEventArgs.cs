
using MiniVNCClient.Types;

using System.Drawing;

namespace MiniVNCClient.Events
{
	public class RemoteCursorUpdatedEventArgs : PseudoEncodingEventArgs
	{
		#region Properties
		public Rectangle SizeAndTipPosition { get; }
		public byte[] Data { get; }
		public byte[] BitMask { get; }
		#endregion

		#region Constructors
		public RemoteCursorUpdatedEventArgs(Rectangle sizeAndTipPosition, byte[] data, byte[] bitMask) : base(VNCEncoding.Cursor)
		{
			SizeAndTipPosition = sizeAndTipPosition;
			Data = data;
			BitMask = bitMask;
		}
		#endregion
	}
}
