using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniVNCClient.Types;

namespace MiniVNCClient.Events
{
	public class ServerCutTextEventArgs : ServerToClientMessageEventArgs
	{
		#region Properties
		public string Text { get; private set; }
		#endregion

		#region Constructors
		public ServerCutTextEventArgs(string text) : base(ServerToClientMessageType.ServerCutText)
		{
			Text = text;
		}
		#endregion
	}
}
