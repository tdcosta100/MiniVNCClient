using MiniVNCClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Events
{
	public class PseudoEncodingEventArgs
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
