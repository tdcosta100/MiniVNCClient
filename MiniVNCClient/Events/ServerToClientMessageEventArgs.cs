using MiniVNCClient.Types;
using System;


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
