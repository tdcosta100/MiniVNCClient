﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MiniVNCClient.Events
{
	public class FrameBufferUpdatedEventArgs : ServerToClientMessageEventArgs
	{
		#region Properties
		public DateTime UpdateTime { get; }

		public Int32Rect[] UpdatedAreas { get; }

		public byte[] CurrentFrameBuffer { get; }
		#endregion

		#region Constructors
		public FrameBufferUpdatedEventArgs(DateTime updateTime, Int32Rect[] updatedAreas, byte[] currentFrameBuffer) : base(Types.ServerToClientMessageType.FramebufferUpdate)
		{
			UpdateTime = updateTime;
			UpdatedAreas = updatedAreas;
			CurrentFrameBuffer = currentFrameBuffer;
		}
		#endregion
	}
}
