using MiniVNCClient.Events;
using MiniVNCClient.Types;
using MiniVNCClient.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MiniVNCClient
{
	public class Client : IDisposable
	{
		#region Fields
		private static readonly Regex _RegexServerVersion = new Regex(@"RFB (?<major>\d{3})\.(?<minor>\d{3})\n");
		private static readonly string _VersionFormat = "RFB {0:000}.{1:000}\n";
		private static readonly Version _ClientVersion = new Version(3, 8);

		private static readonly RangeCollection<int, SecurityType?> _SecurityTypes;
		private static readonly SecurityType[] _SupportedSecurityTypes =
			new[]
			{
				SecurityType.Invalid,
				SecurityType.None,
				SecurityType.VNCAuthentication
			};

		private static readonly RangeCollection<int, VNCEncoding?> _Encodings;
		private static readonly VNCEncoding[] _SupportedEncodings =
			new[]
			{
				VNCEncoding.ZRLE,
				VNCEncoding.Zlib,
				VNCEncoding.ZlibHex,
				VNCEncoding.Hextile,
				VNCEncoding.Raw,
				VNCEncoding.Cursor,
				VNCEncoding.CopyRect,
				VNCEncoding.LastRect
			};

		private static readonly RangeCollection<int, ClientToServerMessageType?> _ClientToServerMessageTypes;
		private static readonly ClientToServerMessageType[] _SupportedClientToServerMessageTypes =
			new[]
			{
				ClientToServerMessageType.SetEncodings,
				ClientToServerMessageType.FramebufferUpdateRequest,
				ClientToServerMessageType.EnableContinuousUpdates
			};

		private static readonly RangeCollection<int, ServerToClientMessageType?> _ServerToClientMessageTypes;
		private static readonly ServerToClientMessageType[] _SupportedServerToClientMessageTypes =
			new[]
			{
				ServerToClientMessageType.FramebufferUpdate,
				ServerToClientMessageType.EndOfContinuousUpdates
			};

		private static readonly ZRLESubencodingType[] _ZRLESubencodingTypes;

		private VNCEncoding[] _EnabledEncodings;
		private TcpClient _TcpClient;
		private Stream _Stream;
		private Util.BinaryReader _Reader;
		private BinaryWriter _Writer;

		private MemoryStream _MemoryStreamCompressed = null;
		private BinaryWriter _MemoryStreamCompressedWriter = null;
		private DeflateStream _DeflateStream = null;
		private Util.BinaryReader _DeflateStreamReader = null;
		private MemoryStream _MemoryStreamCompressed2 = null;
		private BinaryWriter _MemoryStreamCompressedWriter2 = null;
		private DeflateStream _DeflateStream2 = null;
		private Util.BinaryReader _DeflateStreamReader2 = null;

		private TaskCompletionSource<object> _DelayTask;
		private Timer _DelayTimer;
		#endregion

		#region Events
		public event EventHandler<FrameBufferUpdatedEventArgs> FrameBufferUpdated;

		public event EventHandler<RemoteCursorUpdatedEventArgs> RemoteCursorUpdated;

		public event EventHandler<ServerCutTextEventArgs> ServerCutText;

		public event EventHandler<EventArgs> Bell;
		#endregion

		#region Properties
		public Version ClientVersion => _ClientVersion;

		public VNCEncoding[] SupportedEncodings => _SupportedEncodings;

		public VNCEncoding[] EnabledEncodings
		{
			get => _EnabledEncodings;
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				if (value.Any(v => !_EnabledEncodings.Contains(v)))
				{
					throw new InvalidOperationException($"{nameof(EnabledEncodings)} must be a subset of {nameof(SupportedEncodings)}");
				}

				_EnabledEncodings = value;
			}
		}

		public bool Connected => _TcpClient?.Connected ?? false;

		public Version ServerVersion { get; private set; }

		public ServerInit SessionInfo { get; private set; }

		public System.Windows.Media.Color[] ColorPalette { get; private set; }

		public string Password { get; set; }

		public Int32Rect[] UpdatedAreas { get; private set; }

		public byte[] FrameBufferState { get; private set; }

		public int FrameBufferStateStride { get; private set; }

		public Int32Rect RemoteCursorSizeAndTipPosition { get; private set; }

		public byte[] RemoteCursorData { get; private set; }

		public byte[] RemoteCursorBitMask { get; private set; }

		public Point RemoteCursorLocation { get; private set; }

		public TraceSource TraceSource { get; set; }
		#endregion

		#region Constructors
		static Client()
		{
			_SecurityTypes = new RangeCollection<int, SecurityType?>()
			{
				new Range<int, SecurityType?>(0, 0, SecurityType.Invalid),
				new Range<int, SecurityType?>(1, 1, SecurityType.None),
				new Range<int, SecurityType?>(2, 2, SecurityType.VNCAuthentication),
				new Range<int, SecurityType?>(3, 4, SecurityType.RealVNC),
				new Range<int, SecurityType?>(5, 5, SecurityType.RA2),
				new Range<int, SecurityType?>(6, 6, SecurityType.RA2ne),
				new Range<int, SecurityType?>(7, 15, SecurityType.RealVNC),
				new Range<int, SecurityType?>(16, 16, SecurityType.Tight),
				new Range<int, SecurityType?>(17, 17, SecurityType.Ultra),
				new Range<int, SecurityType?>(18, 18, SecurityType.TLS),
				new Range<int, SecurityType?>(19, 19, SecurityType.VeNCrypt),
				new Range<int, SecurityType?>(20, 20, SecurityType.SASL),
				new Range<int, SecurityType?>(21, 21, SecurityType.MD5),
				new Range<int, SecurityType?>(22, 22, SecurityType.xvp),
				new Range<int, SecurityType?>(23, 23, SecurityType.SecureTunnel),
				new Range<int, SecurityType?>(24, 24, SecurityType.IntegratedSSH),
				new Range<int, SecurityType?>(30, 35, SecurityType.Apple),
				new Range<int, SecurityType?>(128, 255, SecurityType.RealVNC)
			};

			_Encodings = new RangeCollection<int, VNCEncoding?>()
			{
				new Range<int, VNCEncoding?>(0, 0, VNCEncoding.Raw),
				new Range<int, VNCEncoding?>(1, 1, VNCEncoding.CopyRect),
				new Range<int, VNCEncoding?>(2, 2, VNCEncoding.RRE),
				new Range<int, VNCEncoding?>(5, 5, VNCEncoding.Hextile),
				new Range<int, VNCEncoding?>(6, 6, VNCEncoding.Zlib),
				new Range<int, VNCEncoding?>(7, 7, VNCEncoding.Tight),
				new Range<int, VNCEncoding?>(8, 8, VNCEncoding.ZlibHex),
				new Range<int, VNCEncoding?>(9, 9, VNCEncoding.Ultra),
				new Range<int, VNCEncoding?>(10, 10, VNCEncoding.Ultra2),
				new Range<int, VNCEncoding?>(15, 15, VNCEncoding.TRLE),
				new Range<int, VNCEncoding?>(16, 16, VNCEncoding.ZRLE),
				new Range<int, VNCEncoding?>(17, 17, VNCEncoding.HitachiZYWRLE),
				new Range<int, VNCEncoding?>(20, 20, VNCEncoding.H264),
				new Range<int, VNCEncoding?>(21, 21, VNCEncoding.JPEG),
				new Range<int, VNCEncoding?>(22, 22, VNCEncoding.JRLE),
				new Range<int, VNCEncoding?>(1000, 1002, VNCEncoding.Apple),
				new Range<int, VNCEncoding?>(1011, 1011, VNCEncoding.Apple),
				new Range<int, VNCEncoding?>(1100, 1105, VNCEncoding.Apple),
				new Range<int, VNCEncoding?>(1024, 1099, VNCEncoding.RealVNC),

				new Range<int, VNCEncoding?>(unchecked((int)0xc0a1e5cf), unchecked((int)0xc0a1e5cf), VNCEncoding.PluginStreaming),
				new Range<int, VNCEncoding?>(unchecked((int)0xfffe0000), unchecked((int)0xfffe0000), VNCEncoding.KeyboardLedState),
				new Range<int, VNCEncoding?>(unchecked((int)0xfffe0001), unchecked((int)0xfffe0001), VNCEncoding.SupportedMessages),
				new Range<int, VNCEncoding?>(unchecked((int)0xfffe0002), unchecked((int)0xfffe0002), VNCEncoding.SupportedEncodings),
				new Range<int, VNCEncoding?>(unchecked((int)0xfffe0003), unchecked((int)0xfffe0003), VNCEncoding.ServerIdentity),
				new Range<int, VNCEncoding?>(unchecked((int)0xfffe0004), unchecked((int)0xfffe00ff), VNCEncoding.libVNCServer),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0000), unchecked((int)0xffff0000), VNCEncoding.Cache),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0001), unchecked((int)0xffff0001), VNCEncoding.CacheEnable),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0002), unchecked((int)0xffff0002), VNCEncoding.XORZlib),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0003), unchecked((int)0xffff0003), VNCEncoding.XORMonoRectZlib),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0004), unchecked((int)0xffff0004), VNCEncoding.XORMultiColorZlib),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0005), unchecked((int)0xffff0005), VNCEncoding.SolidColor),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0006), unchecked((int)0xffff0006), VNCEncoding.XOREnable),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0007), unchecked((int)0xffff0007), VNCEncoding.CacheZip),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0008), unchecked((int)0xffff0008), VNCEncoding.SolMonoZip),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff0009), unchecked((int)0xffff0009), VNCEncoding.UltraZip),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff8000), unchecked((int)0xffff8000), VNCEncoding.ServerState),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff8001), unchecked((int)0xffff8001), VNCEncoding.EnableKeepAlive),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff8002), unchecked((int)0xffff8002), VNCEncoding.FTProtocolVersion),
				new Range<int, VNCEncoding?>(unchecked((int)0xffff8003), unchecked((int)0xffff8003), VNCEncoding.Session),

				new Range<int, VNCEncoding?>(-1, -22, VNCEncoding.TightOptions),
				new Range<int, VNCEncoding?>(-23, -32, VNCEncoding.JPEGQualityLevel),
				new Range<int, VNCEncoding?>(-260, -260, VNCEncoding.TightPNG),
				new Range<int, VNCEncoding?>(-219, -222, VNCEncoding.libVNCServer),
				new Range<int, VNCEncoding?>(-223, -223, VNCEncoding.DesktopSize),
				new Range<int, VNCEncoding?>(-224, -224, VNCEncoding.LastRect),
				new Range<int, VNCEncoding?>(-225, -225, VNCEncoding.PointerPos),
				new Range<int, VNCEncoding?>(-239, -239, VNCEncoding.Cursor),
				new Range<int, VNCEncoding?>(-240, -240, VNCEncoding.XCursor),
				new Range<int, VNCEncoding?>(-247, -256, VNCEncoding.CompressionLevel),
				new Range<int, VNCEncoding?>(-257, -257, VNCEncoding.QEMUPointerMotionChange),
				new Range<int, VNCEncoding?>(-258, -258, VNCEncoding.QEMUExtendedKeyEvent),
				new Range<int, VNCEncoding?>(-259, -259, VNCEncoding.QEMUAudio),
				new Range<int, VNCEncoding?>(-261, -261, VNCEncoding.LEDState),
				new Range<int, VNCEncoding?>(-262, -272, VNCEncoding.QEMU),
				new Range<int, VNCEncoding?>(-273, -304, VNCEncoding.VMware),
				new Range<int, VNCEncoding?>(-305, -305, VNCEncoding.gii),
				new Range<int, VNCEncoding?>(-306, -306, VNCEncoding.popa),
				new Range<int, VNCEncoding?>(-307, -307, VNCEncoding.DesktopName),
				new Range<int, VNCEncoding?>(-308, -308, VNCEncoding.ExtendedDesktopSize),
				new Range<int, VNCEncoding?>(-309, -309, VNCEncoding.xvp),
				new Range<int, VNCEncoding?>(-310, -310, VNCEncoding.OLIVECallControl),
				new Range<int, VNCEncoding?>(-311, -311, VNCEncoding.ClientRedirect),
				new Range<int, VNCEncoding?>(-312, -312, VNCEncoding.Fence),
				new Range<int, VNCEncoding?>(-313, -313, VNCEncoding.ContinuousUpdates),
				new Range<int, VNCEncoding?>(-314, -314, VNCEncoding.CursorWithAlpha),
				new Range<int, VNCEncoding?>(-412, -512, VNCEncoding.JPEGFineGrainedQualityLevel),
				new Range<int, VNCEncoding?>(-523, -528, VNCEncoding.CarConnectivity),
				new Range<int, VNCEncoding?>(-763, -768, VNCEncoding.JPEGSubsamplingLevel),

				new Range<int, VNCEncoding?>(unchecked((int)0x48323634), unchecked((int)0x48323634), VNCEncoding.VAH264),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5600), unchecked((int)0x574d5663), VNCEncoding.VMware),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5664), unchecked((int)0x574d5664), VNCEncoding.VMwareCursor),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5665), unchecked((int)0x574d5665), VNCEncoding.VMwareCursorState),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5666), unchecked((int)0x574d5666), VNCEncoding.VMwareCursorPosition),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5667), unchecked((int)0x574d5667), VNCEncoding.VMwareKeyRepeat),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5668), unchecked((int)0x574d5668), VNCEncoding.VMwareLEDstate),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d5669), unchecked((int)0x574d5669), VNCEncoding.VMwareDisplayModeChange),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d566a), unchecked((int)0x574d566a), VNCEncoding.VMwareVirtualMachineState),
				new Range<int, VNCEncoding?>(unchecked((int)0x574d566b), unchecked((int)0x574d56ff), VNCEncoding.VMware),
				new Range<int, VNCEncoding?>(unchecked((int)0xc0a1e5ce), unchecked((int)0xc0a1e5ce), VNCEncoding.ExtendedClipboard)
			};

			_ClientToServerMessageTypes = new RangeCollection<int, ClientToServerMessageType?>()
			{
				new Range<int, ClientToServerMessageType?>(0, 0, ClientToServerMessageType.SetPixelFormat),
				new Range<int, ClientToServerMessageType?>(2, 2, ClientToServerMessageType.SetEncodings),
				new Range<int, ClientToServerMessageType?>(3, 3, ClientToServerMessageType.FramebufferUpdateRequest),
				new Range<int, ClientToServerMessageType?>(4, 4, ClientToServerMessageType.KeyEvent),
				new Range<int, ClientToServerMessageType?>(5, 5, ClientToServerMessageType.PointerEvent),
				new Range<int, ClientToServerMessageType?>(6, 6, ClientToServerMessageType.ClientCutText),
				new Range<int, ClientToServerMessageType?>(7, 7, ClientToServerMessageType.FileTransfer),
				new Range<int, ClientToServerMessageType?>(8, 8, ClientToServerMessageType.SetScale),
				new Range<int, ClientToServerMessageType?>(9, 9, ClientToServerMessageType.SetServerInput),
				new Range<int, ClientToServerMessageType?>(10, 10, ClientToServerMessageType.SetSW),
				new Range<int, ClientToServerMessageType?>(11, 11, ClientToServerMessageType.TextChat),
				new Range<int, ClientToServerMessageType?>(12, 12, ClientToServerMessageType.KeyFrameRequest),
				new Range<int, ClientToServerMessageType?>(13, 13, ClientToServerMessageType.KeepAlive),
				new Range<int, ClientToServerMessageType?>(14, 14, ClientToServerMessageType.UltraVNC),
				new Range<int, ClientToServerMessageType?>(15, 15, ClientToServerMessageType.SetScaleFactor),
				new Range<int, ClientToServerMessageType?>(16, 19, ClientToServerMessageType.UltraVNC),
				new Range<int, ClientToServerMessageType?>(20, 20, ClientToServerMessageType.RequestSession),
				new Range<int, ClientToServerMessageType?>(21, 21, ClientToServerMessageType.SetSession),
				new Range<int, ClientToServerMessageType?>(80, 80, ClientToServerMessageType.NotifyPluginStreaming),
				new Range<int, ClientToServerMessageType?>(127, 127, ClientToServerMessageType.VMware),
				new Range<int, ClientToServerMessageType?>(128, 128, ClientToServerMessageType.CarConnectivity),
				new Range<int, ClientToServerMessageType?>(150, 150, ClientToServerMessageType.EnableContinuousUpdates),
				new Range<int, ClientToServerMessageType?>(248, 248, ClientToServerMessageType.ClientFence),
				new Range<int, ClientToServerMessageType?>(249, 249, ClientToServerMessageType.OLIVECallControl),
				new Range<int, ClientToServerMessageType?>(250, 250, ClientToServerMessageType.xvpClientMessage),
				new Range<int, ClientToServerMessageType?>(251, 251, ClientToServerMessageType.SetDesktopSize),
				new Range<int, ClientToServerMessageType?>(252, 252, ClientToServerMessageType.Tight),
				new Range<int, ClientToServerMessageType?>(253, 253, ClientToServerMessageType.giiClientMessage),
				new Range<int, ClientToServerMessageType?>(254, 254, ClientToServerMessageType.VMware),
				new Range<int, ClientToServerMessageType?>(255, 255, ClientToServerMessageType.QEMUClientMessage)
			};

			_ServerToClientMessageTypes = new RangeCollection<int, ServerToClientMessageType?>()
			{
				new Range<int, ServerToClientMessageType?>(0, 0, ServerToClientMessageType.FramebufferUpdate),
				new Range<int, ServerToClientMessageType?>(1, 1, ServerToClientMessageType.SetColourMapEntries),
				new Range<int, ServerToClientMessageType?>(2, 2, ServerToClientMessageType.Bell),
				new Range<int, ServerToClientMessageType?>(3, 3, ServerToClientMessageType.ServerCutText),
				new Range<int, ServerToClientMessageType?>(4, 4, ServerToClientMessageType.ResizeFrameBuffer),
				new Range<int, ServerToClientMessageType?>(5, 5, ServerToClientMessageType.KeyFrameUpdate),
				new Range<int, ServerToClientMessageType?>(6, 6, ServerToClientMessageType.UltraVNC),
				new Range<int, ServerToClientMessageType?>(7, 7, ServerToClientMessageType.FileTransfer),
				new Range<int, ServerToClientMessageType?>(8, 10, ServerToClientMessageType.UltraVNC),
				new Range<int, ServerToClientMessageType?>(11, 11, ServerToClientMessageType.TextChat),
				new Range<int, ServerToClientMessageType?>(12, 12, ServerToClientMessageType.UltraVNC),
				new Range<int, ServerToClientMessageType?>(13, 13, ServerToClientMessageType.KeepAlive),
				new Range<int, ServerToClientMessageType?>(14, 14, ServerToClientMessageType.UltraVNC),
				new Range<int, ServerToClientMessageType?>(15, 15, ServerToClientMessageType.ResizeFrameBuffer),
				new Range<int, ServerToClientMessageType?>(127, 127, ServerToClientMessageType.VMware),
				new Range<int, ServerToClientMessageType?>(128, 128, ServerToClientMessageType.CarConnectivity),
				new Range<int, ServerToClientMessageType?>(150, 150, ServerToClientMessageType.EndOfContinuousUpdates),
				new Range<int, ServerToClientMessageType?>(173, 173, ServerToClientMessageType.ServerState),
				new Range<int, ServerToClientMessageType?>(248, 248, ServerToClientMessageType.ServerFence),
				new Range<int, ServerToClientMessageType?>(249, 249, ServerToClientMessageType.OLIVECallControl),
				new Range<int, ServerToClientMessageType?>(250, 250, ServerToClientMessageType.xvpServerMessage),
				new Range<int, ServerToClientMessageType?>(252, 252, ServerToClientMessageType.Tight),
				new Range<int, ServerToClientMessageType?>(253, 253, ServerToClientMessageType.giiServerMessage),
				new Range<int, ServerToClientMessageType?>(254, 254, ServerToClientMessageType.VMware),
				new Range<int, ServerToClientMessageType?>(255, 255, ServerToClientMessageType.QEMUServerMessage)
			};

			var zRLESubencodingTypes = new RangeCollection<int, ZRLESubencodingType>()
			{
				new Range<int, ZRLESubencodingType>(0, 0, ZRLESubencodingType.Raw),
				new Range<int, ZRLESubencodingType>(1, 1, ZRLESubencodingType.SolidColor),
				new Range<int, ZRLESubencodingType>(2, 16, ZRLESubencodingType.PackedPalette),
				new Range<int, ZRLESubencodingType>(17, 127, ZRLESubencodingType.Unused),
				new Range<int, ZRLESubencodingType>(128, 128, ZRLESubencodingType.PlainRLE),
				new Range<int, ZRLESubencodingType>(129, 129, ZRLESubencodingType.Unused),
				new Range<int, ZRLESubencodingType>(130, 255, ZRLESubencodingType.PaletteRLE)
			};

			_ZRLESubencodingTypes =
				Enumerable.Range(0, 256)
				.Select(i => zRLESubencodingTypes.GetItemInRange(i))
				.ToArray();
		}

		public Client()
		{
			_DelayTimer = new Timer(_ => _DelayTask.SetResult(null));
		}
		#endregion

		#region Private methods
		private Task Delay(long delay)
		{
			_DelayTask = new TaskCompletionSource<object>();
			_DelayTimer.Change(delay, Timeout.Infinite);
			return _DelayTask.Task;
		}

		private Task Delay(int delay)
		{
			return Delay(delay);
		}

		private Task Delay(TimeSpan delay)
		{
			return Delay((long)delay.TotalMilliseconds);
		}

		private byte[] FillRectangle(int width, int height, byte[] data)
		{
			var rectangleData = new byte[width * height * data.Length];

			FillData(data, rectangleData, 0, rectangleData.Length / data.Length);

			return rectangleData;
		}

		private void FillData(byte[] source, byte[] destination, int destinationPosition, int count)
		{
			if (destinationPosition + count * source.Length > destination.Length)
			{
				throw new InvalidOperationException("Data offset and length must be contained inside destination bounds");
			}

			Buffer.BlockCopy(source, 0, destination, destinationPosition, source.Length);

			for (int iteration = 1; iteration < count; iteration *= 2)
			{
				Buffer.BlockCopy(
					src: destination,
					srcOffset: destinationPosition,
					dst: destination,
					dstOffset: destinationPosition + iteration * source.Length,
					count: Math.Min(iteration * source.Length, (count - iteration) * source.Length)
				);
			}
		}

		private byte[] ReadRectangle(Int32Rect rectangle, byte[] buffer, int bufferStride)
		{
			var rectangleStride = rectangle.Width * SessionInfo.PixelFormat.BytesPerPixel;
			var rectangleLineOffset = rectangle.X * SessionInfo.PixelFormat.BytesPerPixel;

			byte[] rectangleData = new byte[rectangle.Height * rectangleStride];

			for (int rectangleLine = 0; rectangleLine < rectangle.Height; rectangleLine++)
			{
				Buffer.BlockCopy(
					src: buffer,
					srcOffset: bufferStride * (rectangle.Y + rectangleLine) + rectangleLineOffset,
					dst: rectangleData,
					dstOffset: rectangleLine * rectangleStride,
					count: rectangleStride
				);
			}

			return rectangleData;
		}

		private void WriteRectangle(byte[] buffer, int bufferStride, Int32Rect rectangle, byte[] rectangleData)
		{
			var rectangleStride = rectangle.Width * SessionInfo.PixelFormat.BytesPerPixel;
			var rectangleLineOffset = rectangle.X * SessionInfo.PixelFormat.BytesPerPixel;

			for (int rectangleLine = 0; rectangleLine < rectangle.Height; rectangleLine++)
			{
				Buffer.BlockCopy(
					src: rectangleData,
					srcOffset: rectangleLine * rectangleStride,
					dst: buffer,
					dstOffset: bufferStride * (rectangle.Y + rectangleLine) + rectangleLineOffset,
					count: rectangleStride
				);
			}
		}

		private bool Initialize()
		{
			try
			{
				NegotiateVersion();
				NegotiateAuthentication();
				InitializeSession();
			}
			catch (Exception ex)
			{
				TraceSource.TraceEvent(TraceEventType.Error, 0, $"Error during intialization: {ex.Message},\r\n{ex.StackTrace}");
				return false;
			}

			return true;
		}

		private void NegotiateVersion()
		{
			TraceSource.TraceEvent(TraceEventType.Information, 0, "Negotiating version");

			var stringVersion = _Reader.ReadString(12);

			TraceSource.TraceEvent(TraceEventType.Information, 0, $"Received {stringVersion}");

			var matchVersion = _RegexServerVersion.Match(stringVersion);

			if (matchVersion.Success)
			{
				ServerVersion = new Version(int.Parse(matchVersion.Groups["major"].Value), int.Parse(matchVersion.Groups["minor"].Value));

				TraceSource.TraceEvent(TraceEventType.Information, 0, $"Detected server version: {ServerVersion}");

				var clientStringVersion = string.Format(_VersionFormat, _ClientVersion.Major, _ClientVersion.Minor);

				TraceSource.TraceEvent(TraceEventType.Information, 0, $"Sending client version {_ClientVersion}: {clientStringVersion}");

				_Writer.Write(Encoding.UTF8.GetBytes(clientStringVersion));
			}
			else
			{
				throw new Exception("Server not recognized");
			}
		}

		private void NegotiateAuthentication()
		{
			TraceSource.TraceEvent(TraceEventType.Information, 0, "Negotiating authentication method");

			SecurityType[] securityTypes = new SecurityType[0];

			if (ServerVersion >= new Version(3, 7))
			{
				var totalSecurityTypes = _Reader.ReadByte();

				if (totalSecurityTypes > 0)
				{
					securityTypes = _Reader.ReadBytes(totalSecurityTypes)
						.Select(s => _SecurityTypes.GetItemInRange(s))
						.Where(s => s.HasValue && _SupportedSecurityTypes.Contains(s.Value))
						.Select(s => s.Value)
						.Distinct()
						.ToArray();
				}
			}
			else
			{
				var securityType = _SecurityTypes.GetItemInRange((int)_Reader.ReadUInt32());

				if (securityType.HasValue)
				{
					if (_SupportedSecurityTypes.Contains(securityType.Value))
					{
						securityTypes = new[] { securityType.Value };
					}
				}
			}

			if (securityTypes.Contains(SecurityType.Invalid))
			{
				var reasonLength = _Reader.ReadInt32();

				var reason = Encoding.UTF8.GetString(_Reader.ReadBytes(reasonLength));

				throw new Exception($"Connection failed: {reason}");
			}

			if (!securityTypes.Any())
			{
				throw new Exception("No supported authentication methods");
			}

			TraceSource.TraceEvent(TraceEventType.Information, 0, $"Authentication methods {string.Join(", ", securityTypes.Select(s => s.ToString()))} are accepted by both server and client");

			if (securityTypes.Contains(SecurityType.None))
			{
				TraceSource.TraceEvent(TraceEventType.Information, 0, $"Sending selected security type: {SecurityType.None}");

				_Writer.Write((byte)SecurityType.None);

				if (ServerVersion >= new Version(3, 8))
				{
					var securityResult = (SecurityResult)_Reader.ReadUInt32();

					if (securityResult != SecurityResult.OK)
					{
						throw new Exception($"Authentication failed: unknown reason");
					}
				}
			}
			else if (securityTypes.Contains(SecurityType.VNCAuthentication))
			{
				TraceSource.TraceEvent(TraceEventType.Information, 0, $"Sending selected authentication method: {SecurityType.VNCAuthentication}");

				_Writer.Write((byte)SecurityType.VNCAuthentication);

				var challenge = _Reader.ReadBytes(16);

				TraceSource.TraceEvent(TraceEventType.Information, 0, "Challenge received");

				var key = (Password ?? string.Empty)
					.ToArray()
					.Concat(new char[8])
					.Take(8)
					.Select(
						item =>
						{
							return
								(byte)
								(
									((item >> 7) & 0x01)
									| (((item >> 6) & 0x01) << 1)
									| (((item >> 5) & 0x01) << 2)
									| (((item >> 4) & 0x01) << 3)
									| (((item >> 3) & 0x01) << 4)
									| (((item >> 2) & 0x01) << 5)
									| (((item >> 1) & 0x01) << 6)
									| ((item & 0x01) << 7)
								);
						}
					)
					.ToArray();

				using (var dESCryptoServiceProvider = new DESCryptoServiceProvider() { Key = key, Mode = CipherMode.ECB })
				using (var encryptor = dESCryptoServiceProvider.CreateEncryptor())
				{
					var response = new byte[16];

					encryptor.TransformBlock(challenge, 0, 16, response, 0);

					TraceSource.TraceEvent(TraceEventType.Information, 0, "Sending response");

					_Writer.Write(response);
				}

				var securityResult = (SecurityResult)_Reader.ReadUInt32();

				if (securityResult == SecurityResult.Failed)
				{
					throw new Exception($"Authentication failed: incorrect password");
				}
				else if (securityResult == SecurityResult.FailedTooManyAttempts)
				{
					throw new Exception($"Authentication failed: too many attempts");
				}
			}

			TraceSource.TraceEvent(TraceEventType.Information, 0, "Authentication succeeded");
		}

		private void InitializeSession()
		{
			var clientInit = new ClientInit() { Shared = true };

			TraceSource.TraceEvent(TraceEventType.Information, 0, "Sending client initialization");

			clientInit.Serialize(_Writer);

			SessionInfo = ServerInit.Deserialize(_Reader);

			TraceSource.TraceEvent(TraceEventType.Information, 0, "Received server initialization");

			FrameBufferStateStride = SessionInfo.FrameBufferWidth * SessionInfo.PixelFormat.BytesPerPixel;
			FrameBufferState = new byte[SessionInfo.FrameBufferHeight * FrameBufferStateStride];

			TraceSource.TraceEvent(TraceEventType.Information, 0, $"Sending supported encodings: {string.Join(", ", _SupportedEncodings.Select(e => e.ToString()))}");

			SetEncodings(_SupportedEncodings);

			Task.Factory.StartNew(
				() =>
				{
					TraceSource.TraceEvent(TraceEventType.Information, 0, "Starting to listen for server messages");

					var retryCount = 0;

					while (_TcpClient?.Connected ?? false)
					{
						try
						{
							MessageHandler((ServerToClientMessageType)_Reader.ReadByte());
						}
						catch (Exception ex)
						{
							TraceSource.TraceEvent(TraceEventType.Error, 0, $"Error reading messages from server: {ex.Message}\r\n{ex.StackTrace}");

							if (ex is OutOfMemoryException)
							{
								Close();
								break;
							}

							retryCount = ++retryCount % 5;

							if (retryCount == 0)
							{
								Delay(TimeSpan.FromMilliseconds(100)).Wait();
							}
						}
					}

					TraceSource.TraceEvent(TraceEventType.Information, 0, "Stopped to listen for server messages");
				},
				CancellationToken.None,
				TaskCreationOptions.None,
				TaskScheduler.Default
			);
		}

		private void MessageHandler(ServerToClientMessageType messageType)
		{
			TraceSource.TraceEvent(TraceEventType.Verbose, (int)messageType, $"Received message: {messageType}");

			switch (messageType)
			{
				case ServerToClientMessageType.FramebufferUpdate:
					FramebufferUpdateHandler();
					break;
				case ServerToClientMessageType.SetColourMapEntries:
					SetColourMapEntriesHandler();
					break;
				case ServerToClientMessageType.Bell:
					BellHandler();
					break;
				case ServerToClientMessageType.ServerCutText:
					ServerCutTextHandler();
					break;
				case ServerToClientMessageType.ResizeFrameBuffer:
					break;
				case ServerToClientMessageType.KeyFrameUpdate:
					break;
				case ServerToClientMessageType.UltraVNC:
					break;
				case ServerToClientMessageType.FileTransfer:
					break;
				case ServerToClientMessageType.TextChat:
					break;
				case ServerToClientMessageType.KeepAlive:
					break;
				case ServerToClientMessageType.VMware:
					break;
				case ServerToClientMessageType.CarConnectivity:
					break;
				case ServerToClientMessageType.EndOfContinuousUpdates:
					break;
				case ServerToClientMessageType.ServerState:
					break;
				case ServerToClientMessageType.ServerFence:
					break;
				case ServerToClientMessageType.OLIVECallControl:
					break;
				case ServerToClientMessageType.xvpServerMessage:
					break;
				case ServerToClientMessageType.Tight:
					break;
				case ServerToClientMessageType.giiServerMessage:
					break;
				case ServerToClientMessageType.QEMUServerMessage:
					break;
				default:
					break;
			}
		}

		private void FramebufferUpdateHandler()
		{
			try
			{
				var updateStartTime = DateTime.Now;

				var padding = _Reader.ReadByte();

				var numberOfRectangles = _Reader.ReadUInt16();

				TraceSource.TraceEvent(TraceEventType.Verbose, (int)ServerToClientMessageType.FramebufferUpdate, $"Total rectangles: {numberOfRectangles}");

				if (numberOfRectangles > 0)
				{
					byte[] newFrameBufferState = null;

					var updatedAreas = new List<Int32Rect>();

					for (int rectangleIndex = 0; rectangleIndex < numberOfRectangles; rectangleIndex++)
					{
						var rectangle = new Int32Rect()
						{
							X = _Reader.ReadUInt16(),
							Y = _Reader.ReadUInt16(),
							Width = _Reader.ReadUInt16(),
							Height = _Reader.ReadUInt16()
						};

						var encodingType = (VNCEncoding)_Reader.ReadInt32();

						if (
							encodingType == VNCEncoding.CopyRect
							||
							encodingType == VNCEncoding.ZRLE
							||
							encodingType == VNCEncoding.Hextile
							||
							encodingType == VNCEncoding.ZlibHex
							||
							encodingType == VNCEncoding.Zlib
							||
							encodingType == VNCEncoding.Raw
						)
						{
							if (newFrameBufferState == null)
							{
								newFrameBufferState = new byte[FrameBufferState.Length];
								Buffer.BlockCopy(FrameBufferState, 0, newFrameBufferState, 0, FrameBufferState.Length);
							}

							updatedAreas.Add(rectangle);
						}

						TraceSource.TraceEvent(TraceEventType.Verbose, (int)ServerToClientMessageType.FramebufferUpdate, $"Rectangle {rectangleIndex}: {{{rectangle}}}, EncodingType = {encodingType} (0x{encodingType:X})");

						if (encodingType == VNCEncoding.LastRect)
						{
							break;
						}

						if (encodingType == VNCEncoding.Cursor)
						{
							RemoteCursorSizeAndTipPosition = rectangle;
							RemoteCursorData = _Reader.ReadBytes(rectangle.Width * rectangle.Height * SessionInfo.PixelFormat.BytesPerPixel);
							RemoteCursorBitMask = _Reader.ReadBytes(((rectangle.Width + 7) / 8) * rectangle.Height);

							RemoteCursorUpdated?.Invoke(this, new RemoteCursorUpdatedEventArgs(RemoteCursorSizeAndTipPosition, RemoteCursorData, RemoteCursorBitMask));
						}

						if (encodingType == VNCEncoding.CopyRect)
						{
							var originalRectangle = rectangle;
							originalRectangle.X = _Reader.ReadUInt16();
							originalRectangle.Y = _Reader.ReadUInt16();

							var rectangleData = ReadRectangle(originalRectangle, newFrameBufferState, FrameBufferStateStride);
							WriteRectangle(newFrameBufferState, FrameBufferStateStride, rectangle, rectangleData);
						}

						if (encodingType == VNCEncoding.ZRLE)
						{
							if (_MemoryStreamCompressed == null)
							{
								_MemoryStreamCompressed = new MemoryStream();
								_MemoryStreamCompressedWriter = new BinaryWriter(_MemoryStreamCompressed);
							}

							var compressedDataLength = (int)_Reader.ReadUInt32();

							_MemoryStreamCompressed.Position = 0;
							_MemoryStreamCompressedWriter.Write(_Reader.ReadBytes(compressedDataLength));
							_MemoryStreamCompressed.SetLength(compressedDataLength);
							_MemoryStreamCompressed.Position = 0;

							if (_DeflateStream == null)
							{
								_MemoryStreamCompressed.Position = 2;

								_DeflateStream = new DeflateStream(_MemoryStreamCompressed, CompressionMode.Decompress);
								_DeflateStreamReader = new Util.BinaryReader(_DeflateStream);
							}

							var reader = _DeflateStreamReader;

							var bytesPerPixel = SessionInfo.PixelFormat.BytesPerPixel;
							var bytesPerCPixel = (bytesPerPixel > 1) ? 3 : 1;

							for (int rleTileY = 0; rleTileY < rectangle.Height; rleTileY += 64)
							{
								for (int rleTileX = 0; rleTileX < rectangle.Width; rleTileX += 64)
								{
									var rleTile = new Int32Rect(
										x: rleTileX + rectangle.X,
										y: rleTileY + rectangle.Y,
										width: Math.Min(rectangle.Width - rleTileX, 64),
										height: Math.Min(rectangle.Height - rleTileY, 64)
									);

									var rleTileData = new byte[rleTile.Width * rleTile.Height * bytesPerPixel];

									var subencoding = reader.ReadByte();
									var subencodingType = _ZRLESubencodingTypes[subencoding];

									if (subencodingType == ZRLESubencodingType.Raw)
									{
										for (int i = 0; i < rleTile.Width * rleTile.Height; i++)
										{
											reader.Read(rleTileData, i * bytesPerPixel, bytesPerCPixel);
										}
									}
									else if (subencodingType == ZRLESubencodingType.SolidColor)
									{
										var color = new byte[bytesPerPixel];
										reader.Read(color, 0, bytesPerCPixel);

										if (bytesPerPixel > bytesPerCPixel)
										{
											color[bytesPerPixel - 1] = 0xff;
										}

										FillData(color, rleTileData, 0, rleTileData.Length / color.Length);
									}
									else if (subencodingType == ZRLESubencodingType.PackedPalette)
									{
										var paletteSize = subencoding;

										var palette = new byte[128][];

										for (int i = 0; i < paletteSize; i++)
										{
											palette[i] = new byte[bytesPerPixel];
											reader.Read(palette[i], 0, bytesPerCPixel);

											if (bytesPerPixel > bytesPerCPixel)
											{
												palette[i][bytesPerPixel - 1] = 0xff;
											}
										}

										if (paletteSize < 128)
										{
											for (int i = paletteSize; i < 128; i++)
											{
												palette[i] = new byte[bytesPerPixel];
											}
										}

										var bitsPerPixel = 0;

										if (paletteSize > 16)
										{
											bitsPerPixel = 8;
										}
										else if (paletteSize > 4)
										{
											bitsPerPixel = 4;
										}
										else if (paletteSize > 2)
										{
											bitsPerPixel = 2;
										}
										else
										{
											bitsPerPixel = 1;
										}

										var mask = (byte)((1 << bitsPerPixel) - 1);

										var stride = (rleTile.Width + 8 / bitsPerPixel - 1) * bitsPerPixel / 8;

										for (int i = 0; i < rleTile.Height; i++)
										{
											var packedPixels = reader.ReadBytes(stride);

											for (int j = 0; j < rleTile.Width; j++)
											{
												// packedPixels[j * bitsPerPixel / 8] => current byte
												// >> (8 - ((j * bitsPerPixel) % 8) - bitsPerPixel)) => current pixel value inside byte
												// & mask => discard pixels that don't match the palette index

												var paletteIndex = (packedPixels[j * bitsPerPixel / 8] >> (8 - ((j * bitsPerPixel) % 8) - bitsPerPixel)) & mask;

												var color = palette[paletteIndex];

												Buffer.BlockCopy(color, 0, rleTileData, (i * rleTile.Width + j) * color.Length, color.Length);
											}
										}
									}
									else if (subencodingType == ZRLESubencodingType.PlainRLE)
									{
										var currentPosition = 0;

										while (currentPosition < rleTileData.Length)
										{
											var color = new byte[bytesPerPixel];
											reader.Read(color, 0, bytesPerCPixel);

											if (bytesPerPixel > bytesPerCPixel)
											{
												color[bytesPerPixel - 1] = 0xff;
											}

											var runLength = 1;

											var currentByte = reader.ReadByte();

											while (currentByte == 0xff)
											{
												runLength += currentByte;

												currentByte = reader.ReadByte();
											}

											runLength += currentByte;

											FillData(color, rleTileData, currentPosition, Math.Min(runLength, (rleTileData.Length - currentPosition) / bytesPerPixel));

											currentPosition += runLength * color.Length;
										}
									}
									else if (subencodingType == ZRLESubencodingType.PaletteRLE)
									{
										var paletteSize = subencoding & 0x7f;

										var palette = new byte[128][];

										for (int i = 0; i < paletteSize; i++)
										{
											palette[i] = new byte[bytesPerPixel];
											reader.Read(palette[i], 0, bytesPerCPixel);

											if (bytesPerPixel > bytesPerCPixel)
											{
												palette[i][bytesPerPixel - 1] = 0xff;
											}
										}

										if (paletteSize < 128)
										{
											for (int i = paletteSize; i < 128; i++)
											{
												palette[i] = new byte[bytesPerPixel];
											}
										}

										var currentPosition = 0;

										while (currentPosition < rleTileData.Length)
										{
											var currentByte = reader.ReadByte();

											var paletteIndex = currentByte & 0x7f;

											var color = palette[paletteIndex];

											var runLength = 1;

											if ((currentByte & 0x80) != 0)
											{
												currentByte = reader.ReadByte();

												while (currentByte == 0xff)
												{
													runLength += currentByte;

													currentByte = reader.ReadByte();
												}

												runLength += currentByte;
											}

											FillData(color, rleTileData, currentPosition, Math.Min(runLength, (rleTileData.Length - currentPosition) / bytesPerPixel));

											currentPosition += runLength * color.Length;
										}
									}

									WriteRectangle(newFrameBufferState, FrameBufferStateStride, rleTile, rleTileData);
								}
							}
						}

						if (encodingType == VNCEncoding.Hextile || encodingType == VNCEncoding.ZlibHex)
						{
							Util.BinaryReader reader = null;

							byte[] foregroundColor = null;
							byte[] backgroundColor = null;

							var bytesPerPixel = SessionInfo.PixelFormat.BytesPerPixel;

							for (int hextileY = 0; hextileY < rectangle.Height; hextileY += 16)
							{
								for (int hextileX = 0; hextileX < rectangle.Width; hextileX += 16)
								{
									var hextile = new Int32Rect(
										x: hextileX + rectangle.X,
										y: hextileY + rectangle.Y,
										width: Math.Min(rectangle.Width - hextileX, 16),
										height: Math.Min(rectangle.Height - hextileY, 16)
									);

									var subencodingMask = (HextileSubencodingMask)_Reader.ReadByte();

									if (encodingType == VNCEncoding.ZlibHex)
									{
										if (subencodingMask.HasFlag(HextileSubencodingMask.ZlibRaw))
										{
											if (_MemoryStreamCompressed == null)
											{
												_MemoryStreamCompressed = new MemoryStream();
												_MemoryStreamCompressedWriter = new BinaryWriter(_MemoryStreamCompressed);
											}

											var compressedDataLength = (int)_Reader.ReadUInt16();

											_MemoryStreamCompressed.Position = 0;
											_MemoryStreamCompressedWriter.Write(_Reader.ReadBytes(compressedDataLength));
											_MemoryStreamCompressed.SetLength(compressedDataLength);
											_MemoryStreamCompressed.Position = 0;

											if (_DeflateStream == null)
											{
												_MemoryStreamCompressed.Position = 2;

												_DeflateStream = new DeflateStream(_MemoryStreamCompressed, CompressionMode.Decompress);
												_DeflateStreamReader = new Util.BinaryReader(_DeflateStream);
											}
										}

										if (subencodingMask.HasFlag(HextileSubencodingMask.Zlib))
										{
											if (_MemoryStreamCompressed2 == null)
											{
												_MemoryStreamCompressed2 = new MemoryStream();
												_MemoryStreamCompressedWriter2 = new BinaryWriter(_MemoryStreamCompressed2);
											}

											var compressedDataLength = (int)_Reader.ReadUInt16();

											_MemoryStreamCompressed2.Position = 0;
											_MemoryStreamCompressedWriter2.Write(_Reader.ReadBytes(compressedDataLength));
											_MemoryStreamCompressed2.SetLength(compressedDataLength);
											_MemoryStreamCompressed2.Position = 0;

											if (_DeflateStream2 == null)
											{
												_MemoryStreamCompressed2.Position = 2;

												_DeflateStream2 = new DeflateStream(_MemoryStreamCompressed2, CompressionMode.Decompress);
												_DeflateStreamReader2 = new Util.BinaryReader(_DeflateStream2);
											}
										}
									}

									if (subencodingMask.HasFlag(HextileSubencodingMask.Raw) || subencodingMask.HasFlag(HextileSubencodingMask.ZlibRaw))
									{
										if (subencodingMask.HasFlag(HextileSubencodingMask.ZlibRaw))
										{
											reader = _DeflateStreamReader;
										}
										else
										{
											reader = _Reader;
										}

										var hextileData = reader.ReadBytes(hextile.Width * hextile.Height * bytesPerPixel);

										WriteRectangle(newFrameBufferState, FrameBufferStateStride, hextile, hextileData);
									}
									else
									{
										if (subencodingMask.HasFlag(HextileSubencodingMask.Zlib))
										{
											reader = _DeflateStreamReader2;
										}
										else
										{
											reader = _Reader;
										}

										byte[] hextileData = null;

										if (subencodingMask.HasFlag(HextileSubencodingMask.BackgroundSpecified))
										{
											backgroundColor = reader.ReadBytes(bytesPerPixel);
										}

										if (subencodingMask.HasFlag(HextileSubencodingMask.ForegroundSpecified))
										{
											foregroundColor = reader.ReadBytes(bytesPerPixel);
										}

										hextileData = FillRectangle(hextile.Width, hextile.Height, backgroundColor);

										WriteRectangle(newFrameBufferState, FrameBufferStateStride, hextile, hextileData);

										if (subencodingMask.HasFlag(HextileSubencodingMask.AnySubrects))
										{
											var numberOfSubrectangles = reader.ReadByte();

											for (int subrectangleIndex = 0; subrectangleIndex < numberOfSubrectangles; subrectangleIndex++)
											{
												byte[] subrectangleColor = null;

												if (subencodingMask.HasFlag(HextileSubencodingMask.SubrectsColoured))
												{
													subrectangleColor = reader.ReadBytes(bytesPerPixel);
												}
												else
												{
													subrectangleColor = foregroundColor;
												}

												var subrectangleXY = reader.ReadByte();
												var subrectangleWidthHeight = reader.ReadByte();

												var subrectangle = new Int32Rect(
													x: ((subrectangleXY & 0xf0) >> 4) + hextile.X,
													y: (subrectangleXY & 0x0f) + hextile.Y,
													width: ((subrectangleWidthHeight & 0xf0) >> 4) + 1,
													height: (subrectangleWidthHeight & 0x0f) + 1
												);

												var subrectangleData = FillRectangle(subrectangle.Width, subrectangle.Height, subrectangleColor);

												WriteRectangle(newFrameBufferState, FrameBufferStateStride, subrectangle, subrectangleData);
											}
										}
									}
								}
							}
						}

						if (encodingType == VNCEncoding.Raw || encodingType == VNCEncoding.Zlib)
						{
							byte[] rectangleData = null;

							if (encodingType == VNCEncoding.Zlib)
							{
								if (_MemoryStreamCompressed == null)
								{
									_MemoryStreamCompressed = new MemoryStream();
									_MemoryStreamCompressedWriter = new BinaryWriter(_MemoryStreamCompressed);
								}

								var compressedDataLength = (int)_Reader.ReadUInt32();
								_MemoryStreamCompressed.Position = 0;
								_MemoryStreamCompressedWriter.Write(_Reader.ReadBytes(compressedDataLength));
								_MemoryStreamCompressed.SetLength(compressedDataLength);
								_MemoryStreamCompressed.Position = 0;

								if (_DeflateStream == null)
								{
									_MemoryStreamCompressed.Position = 2;

									_DeflateStream = new DeflateStream(_MemoryStreamCompressed, CompressionMode.Decompress);
									_DeflateStreamReader = new Util.BinaryReader(_DeflateStream);
								}

								rectangleData = _DeflateStreamReader.ReadBytes(rectangle.Width * rectangle.Height * SessionInfo.PixelFormat.BytesPerPixel);
							}
							else
							{
								rectangleData = _Reader.ReadBytes(rectangle.Width * rectangle.Height * SessionInfo.PixelFormat.BytesPerPixel);
							}

							WriteRectangle(newFrameBufferState, FrameBufferStateStride, rectangle, rectangleData);
						}
					}

					if (newFrameBufferState != null)
					{
						FrameBufferState = newFrameBufferState;

						UpdatedAreas = updatedAreas.ToArray();

						TraceSource.TraceEvent(TraceEventType.Verbose, (int)ServerToClientMessageType.FramebufferUpdate, $"Finished updating framebuffer after {(DateTime.Now - updateStartTime).TotalSeconds} seconds");

						FrameBufferUpdated?.Invoke(this, new FrameBufferUpdatedEventArgs(updateStartTime, updatedAreas.ToArray(), newFrameBufferState));
					}
				}
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}

				TraceSource.TraceEvent(TraceEventType.Error, (int)ServerToClientMessageType.FramebufferUpdate, $"Error while updating Framebuffer: {ex.Message}\r\n{ex.StackTrace}");
			}
		}

		private void SetColourMapEntriesHandler()
		{
			var padding = _Reader.ReadByte();

			var firstColour = _Reader.ReadInt16();

			var numberOfColours = _Reader.ReadInt16();

			var colorPalette = new System.Windows.Media.Color[numberOfColours];

			for (int colorIndex = 0; colorIndex < numberOfColours; colorIndex++)
			{
				var red = _Reader.ReadBytes(2);
				var green = _Reader.ReadBytes(2);
				var blue = _Reader.ReadBytes(2);

				colorPalette[colorIndex] = System.Windows.Media.Color.FromRgb(red[1], green[1], blue[1]);
			}

			if (ColorPalette == null)
			{
				ColorPalette = colorPalette;
			}
		}

		private void ServerCutTextHandler()
		{
			var padding = _Reader.ReadBytes(3);

			var length = (int)_Reader.ReadUInt32();

			if (length > 0)
			{
				var text = _Reader.ReadString();
				TraceSource.TraceEvent(TraceEventType.Verbose, (int)ServerToClientMessageType.ServerCutText, $"Server cut text: \"{text}\"");

				ServerCutText?.Invoke(this, new ServerCutTextEventArgs(text));
			}
		}

		private void BellHandler()
		{
			Bell?.Invoke(this, EventArgs.Empty);
		}
		#endregion

		#region Public methods
		public bool Connect(string hostname, int port = 5900)
		{
			if (TraceSource == null)
			{
				TraceSource = new TraceSource("MiniVNCClient");
			}

			TraceSource.TraceEvent(TraceEventType.Information, 0, $"Connecting to {hostname}:{port}");

			try
			{
				_TcpClient = new TcpClient() { NoDelay = true };
				_TcpClient.Connect(hostname, port);
			}
			catch (Exception ex)
			{
				TraceSource.TraceEvent(TraceEventType.Error, 0, $"Error connecting to {hostname}:{port}: {ex.Message}");
				return false;
			}

			_Stream = _TcpClient.GetStream();
			_Reader = new Util.BinaryReader(_Stream);
			_Writer = new BinaryWriter(_Stream);

			TraceSource.TraceEvent(TraceEventType.Information, 0, "Connection successful, initializing session");

			return Initialize();
		}

		public bool SendMessage(ClientToServerMessageType messageType, byte[] content)
		{
			if (_TcpClient?.Connected ?? false)
			{
				TraceSource.TraceEvent(TraceEventType.Verbose, (int)messageType, $"Sending message {messageType}");

				_Writer.Write((byte)messageType);
				_Writer.Write(content);

				return true;
			}

			return false;
		}

		public bool SetEncodings(IEnumerable<VNCEncoding> encodings)
		{
			return SendMessage(
				messageType: ClientToServerMessageType.SetEncodings,
				content:
					new byte[1]
					.Concat(
						BinaryConverter.ToByteArray((ushort)encodings.Count())
					)
					.Concat(
						encodings.SelectMany(e => BinaryConverter.ToByteArray((int)e))
					)
					.ToArray()
			);
		}

		public bool SetPixelFormat(PixelFormat pixelFormat)
		{
			return SendMessage(
				messageType: ClientToServerMessageType.SetPixelFormat,
				content:
					new byte[3]
					.Concat(
						new[]
						{
							pixelFormat.BitsPerPixel,
							pixelFormat.Depth,
							pixelFormat.BigEndianFlag,
							pixelFormat.TrueColorFlag
						}
					)
					.Concat(
						new[]
						{
							BinaryConverter.ToByteArray(pixelFormat.RedMax),
							BinaryConverter.ToByteArray(pixelFormat.GreenMax),
							BinaryConverter.ToByteArray(pixelFormat.BlueMax)
						}
						.SelectMany(b => b)
					)
					.Concat(
						new byte[]
						{
							pixelFormat.RedShift,
							pixelFormat.GreenShift,
							pixelFormat.BlueShift
						}
					)
					.Concat(new byte[3])
					.ToArray()
			);
		}

		public bool FramebufferUpdateRequest(bool incremental, ushort x, ushort y, ushort width, ushort height)
		{
			return SendMessage(
				messageType: ClientToServerMessageType.FramebufferUpdateRequest,
				content:
					new[]
					{
						(byte)(incremental ? 1 : 0)
					}
					.Concat(
						new[] { x, y, width, height }
						.SelectMany(v => BinaryConverter.ToByteArray(v))
					)
					.ToArray()
			);
		}

		public bool EnableContinuousUpdates(bool enable, ushort x, ushort y, ushort width, ushort height)
		{
			return SendMessage(
				messageType: ClientToServerMessageType.EnableContinuousUpdates,
				content:
					new[]
					{
						(byte)(enable ? 1 : 0)
					}
					.Concat(
						new[] { x, y, width, height }
						.SelectMany(v => BinaryConverter.ToByteArray(v))
					)
					.ToArray()
			);
		}

		public byte[] GetFrameBufferArea(Int32Rect area)
		{
			if (!Connected || FrameBufferState == null)
			{
				return null;
			}

			return ReadRectangle(area, FrameBufferState, FrameBufferStateStride);
		}

		public void Close()
		{
			Dispose();
		}

		public void Dispose()
		{
			_TcpClient?.Close();

			_Stream?.Dispose();
			_DeflateStream?.Dispose();
			_DeflateStream2?.Dispose();
			_MemoryStreamCompressed?.Dispose();
			_MemoryStreamCompressed2?.Dispose();

			_Reader?.Dispose();
			_Writer?.Dispose();
			_DeflateStreamReader?.Dispose();
			_DeflateStreamReader2?.Dispose();

			_DelayTask?.TrySetResult(null);
			_DelayTimer?.Dispose();

			_TcpClient = null;

			_Stream = null;
			_DeflateStream = null;
			_DeflateStream2 = null;
			_MemoryStreamCompressed = null;
			_MemoryStreamCompressed2 = null;

			_Reader = null;
			_Writer = null;
			_DeflateStreamReader = null;
			_DeflateStreamReader2 = null;

			_DelayTask = null;
			_DelayTimer = null;

			ServerVersion = null;
			SessionInfo = default;
			Password = null;
			FrameBufferState = null;
			FrameBufferStateStride = default;
			RemoteCursorSizeAndTipPosition = default;
			RemoteCursorData = null;
			RemoteCursorBitMask = null;
			RemoteCursorLocation = default;
		}
		#endregion
	}
}
