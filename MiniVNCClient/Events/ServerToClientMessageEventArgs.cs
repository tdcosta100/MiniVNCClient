using MiniVNCClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Events
{
	public abstract class ServerToClientMessageEventArgs
	{
		#region Properties
		public ServerToClientMessageType Type { get; set; }

		public byte[] Content { get; set; }
		#endregion
	}
}
