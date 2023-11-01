using System;

using System.Drawing;

namespace MiniVNCClient.Events
{
	public class FrameBufferUpdatedEventArgs : ServerToClientMessageEventArgs
	{
		#region Properties
		public DateTime UpdateTime { get; }

		public Rectangle[] UpdatedAreas { get; }
		#endregion

		#region Constructors
		public FrameBufferUpdatedEventArgs(DateTime updateTime, Rectangle[] updatedAreas) : base(Types.ServerToClientMessageType.FramebufferUpdate)
		{
			UpdateTime = updateTime;
			UpdatedAreas = updatedAreas;
		}
		#endregion
	}
}
