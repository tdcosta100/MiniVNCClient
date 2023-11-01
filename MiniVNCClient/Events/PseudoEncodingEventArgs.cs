using MiniVNCClient.Types;
using System;


namespace MiniVNCClient.Events
{
	public class PseudoEncodingEventArgs : EventArgs
	{
		#region Properties
		public VNCEncoding PseudoEncoding { get; }
		#endregion

		#region Constructors
		public PseudoEncodingEventArgs(VNCEncoding pseudoEncoding)
		{
			PseudoEncoding = pseudoEncoding;
		}
		#endregion
	}
}
