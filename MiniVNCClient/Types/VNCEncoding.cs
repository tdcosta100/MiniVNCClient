using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Types
{
	public enum VNCEncoding : int
	{
		Raw = 0,
		CopyRect = 1,
		RRE = 2,
		Hextile = 5,
		Zlib = 6,
		Tight = 7,
		ZlibHex = 8,
		Ultra = 9,
		Ultra2 = 10,
		TRLE = 15,
		ZRLE = 16,
		HitachiZYWRLE = 17,
		H264 = 20,
		JPEG = 21,
		JRLE = 22,
		/* 1000 to 1002 */
		/* 1011 */
		/* 1100 to 1105 */
		Apple = 1000,
		/* 1024 to 1099 */
		RealVNC = 1024,
		VAH264 = 0x48323634,
		PluginStreaming = unchecked((int)0xc0a1e5cf),
		KeyboardLedState = unchecked((int)0xfffe0000),
		SupportedMessages = unchecked((int)0xfffe0001),
		SupportedEncodings = unchecked((int)0xfffe0002),
		ServerIdentity = unchecked((int)0xfffe0003),
		Cache = unchecked((int)0xffff0000),
		CacheEnable = unchecked((int)0xffff0001),
		XORZlib = unchecked((int)0xffff0002),
		XORMonoRectZlib = unchecked((int)0xffff0003),
		XORMultiColorZlib = unchecked((int)0xffff0004),
		SolidColor = unchecked((int)0xffff0005),
		XOREnable = unchecked((int)0xffff0006),
		CacheZip = unchecked((int)0xffff0007),
		SolMonoZip = unchecked((int)0xffff0008),
		UltraZip = unchecked((int)0xffff0009),
		ServerState = unchecked((int)0xffff8000),
		EnableKeepAlive = unchecked((int)0xffff8001),
		FTProtocolVersion = unchecked((int)0xffff8002),
		Session = unchecked((int)0xffff8003),

		/* -1 to -22 */
		/* -33 to -218 */
		TightOptions = -1,
		/* -23 to -32 */
		JPEGQualityLevel = -23,
		TightPNG = -260,
		/* -219 to -222 */
		/* 0xfffe0004 to 0xfffe00ff */
		libVNCServer = -219,
		DesktopSize = -223,
		LastRect = -224,
		PointerPos = -225,
		Cursor = -239,
		XCursor = -240,
		/* -247 to -256 */
		CompressionLevel = -247,
		QEMUPointerMotionChange = -257,
		QEMUExtendedKeyEvent = -258,
		QEMUAudio = -259,
		LEDState = -261,
		/* -262 to -272 */
		QEMU = -262,
		/* -273 to -304 */
		/* 0x574d5600 to 0x574d56ff */
		VMware = 0x574d5600,
		gii = -305,
		popa = -306,
		DesktopName = -307,
		ExtendedDesktopSize = -308,
		xvp = -309,
		OLIVECallControl = -310,
		ClientRedirect = -311,
		Fence = -312,
		ContinuousUpdates = -313,
		CursorWithAlpha = -314,
		/* -412 to -512 */
		JPEGFineGrainedQualityLevel = -412,
		/* -523 to -528 */
		CarConnectivity = -523,
		/* -763 to -768 */
		JPEGSubsamplingLevel = -763,
		VMwareCursor = unchecked((int)0x574d5664),
		VMwareCursorState = unchecked((int)0x574d5665),
		VMwareCursorPosition = unchecked((int)0x574d5666),
		VMwareKeyRepeat = unchecked((int)0x574d5667),
		VMwareLEDstate = unchecked((int)0x574d5668),
		VMwareDisplayModeChange = unchecked((int)0x574d5669),
		VMwareVirtualMachineState = unchecked((int)0x574d566a),
		ExtendedClipboard = unchecked((int)0xc0a1e5ce)
	}
}
