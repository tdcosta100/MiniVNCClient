using MiniVNCClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Events
{
	public abstract class ServerToClientMessageEventArgs : EventArgs
	{
		#region Properties
		public ServerToClientMessageType Type { get; }
		#endregion

		#region Constructors
		public ServerToClientMessageEventArgs(ServerToClientMessageType type)
		{
			Type = type;
		}
		#endregion
	}
}
