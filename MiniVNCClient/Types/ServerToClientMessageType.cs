using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	public enum ServerToClientMessageType : byte
	{
		FramebufferUpdate = 0,
		SetColourMapEntries = 1,
		Bell = 2,
		ServerCutText = 3,
		/* 4, 15 */
		ResizeFrameBuffer = 4,
		KeyFrameUpdate = 5,
		/* 6, 8 to 10, 12, 14 */
		UltraVNC = 6,
		FileTransfer = 7,
		TextChat = 11,
		KeepAlive = 13,
		/* 127, 254 */
		VMware = 127,
		CarConnectivity = 128,
		EndOfContinuousUpdates = 150,
		ServerState = 173,
		ServerFence = 248,
		OLIVECallControl = 249,
		xvpServerMessage = 250,
		Tight = 252,
		giiServerMessage = 253,
		QEMUServerMessage = 255
	}
}
