using ImageMagick;
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiniVNCClient
{
	public class Client
	{
		#region Fields
		private static readonly Regex _RegexServerVersion = new Regex(@"RFB (?<major>\d{3})\.(?<minor>\d{3})\n");
		private static readonly string _VersionFormat = "RFB {0:000}.{1:000}\n";
		private static readonly Version _ClientVersion = new Version(3, 8);

		private static readonly RangeCollection<int, SecurityType?> _SecurityTypes;
		private static readonly SecurityType[] _SupportedSecurityTypes = new[] { SecurityType.Invalid, SecurityType.None };

		private static readonly RangeCollection<int, VNCEncoding?> _Encodings;
		private static readonly VNCEncoding[] _SupportedEncodings = new[] { /* VNCEncoding.Raw, */ VNCEncoding.Cursor, VNCEncoding.CopyRect, VNCEncoding.LastRect, /* VNCEncoding.Zlib, */ VNCEncoding.Hextile };

		private static readonly RangeCollection<int, ClientToServerMessageType?> _ClientToServerMessageTypes;
		private static readonly ClientToServerMessageType[] _SupportedClientToServerMessageTypes = new[] { ClientToServerMessageType.SetEncodings, ClientToServerMessageType.FramebufferUpdateRequest, ClientToServerMessageType.EnableContinuousUpdates };

		private static readonly RangeCollection<int, ServerToClientMessageType?> _ServerToClientMessageTypes;
		private static readonly ServerToClientMessageType[] _SupportedServerToClientMessageTypes = new[] { ServerToClientMessageType.FramebufferUpdate, ServerToClientMessageType.EndOfContinuousUpdates };

		private Stream _Stream;
		private BinaryReader _Reader;
		private BinaryWriter _Writer;

		private MagickImage _FrameBufferState;

		private MemoryStream _MemoryStreamCompressed = null;
		private BinaryWriter _MemoryStreamCompressedWriter = null;
		private DeflateStream _DeflateStream = null;
		private BinaryReader _DeflateStreamReader = null;
		#endregion

		#region Events
		public EventHandler<FrameBufferUpdatedEventArgs> FrameBufferUpdated;
		#endregion

		#region Properties
		public Version ServerVersion { get; set; }

		public ServerInit SessionInfo { get; set; }

		public Version ClientVersion => _ClientVersion;
		#endregion

		#region Constructors
		static Client()
		{
			_SecurityTypes = new RangeCollection<int, SecurityType?>()
			{
				(0, 0, SecurityType.Invalid),
				(1, 1, SecurityType.None),
				(2, 2, SecurityType.VNCAuthentication),
				(3, 4, SecurityType.RealVNC),
				(5, 5, SecurityType.RA2),
				(6, 6, SecurityType.RA2ne),
				(7, 15, SecurityType.RealVNC),
				(16, 16, SecurityType.Tight),
				(17, 17, SecurityType.Ultra),
				(18, 18, SecurityType.TLS),
				(19, 19, SecurityType.VeNCrypt),
				(20, 20, SecurityType.SASL),
				(21, 21, SecurityType.MD5),
				(22, 22, SecurityType.xvp),
				(23, 23, SecurityType.SecureTunnel),
				(24, 24, SecurityType.IntegratedSSH),
				(30, 35, SecurityType.Apple),
				(128, 255, SecurityType.RealVNC)
			};

			_Encodings = new RangeCollection<int, VNCEncoding?>()
			{
				(0, 0, VNCEncoding.Raw),
				(1, 1, VNCEncoding.CopyRect),
				(2, 2, VNCEncoding.RRE),
				(5, 5, VNCEncoding.Hextile),
				(6, 6, VNCEncoding.Zlib),
				(7, 7, VNCEncoding.Tight),
				(8, 8, VNCEncoding.ZlibHex),
				(9, 9, VNCEncoding.Ultra),
				(10, 10, VNCEncoding.Ultra2),
				(15, 15, VNCEncoding.TRLE),
				(16, 16, VNCEncoding.ZRLE),
				(17, 17, VNCEncoding.HitachiZYWRLE),
				(20, 20, VNCEncoding.H264),
				(21, 21, VNCEncoding.JPEG),
				(22, 22, VNCEncoding.JRLE),
				(1000, 1002, VNCEncoding.Apple),
				(1011, 1011, VNCEncoding.Apple),
				(1100, 1105, VNCEncoding.Apple),
				(1024, 1099, VNCEncoding.RealVNC),

				(unchecked((int)0xc0a1e5cf), unchecked((int)0xc0a1e5cf), VNCEncoding.PluginStreaming),
				(unchecked((int)0xfffe0000), unchecked((int)0xfffe0000), VNCEncoding.KeyboardLedState),
				(unchecked((int)0xfffe0001), unchecked((int)0xfffe0001), VNCEncoding.SupportedMessages),
				(unchecked((int)0xfffe0002), unchecked((int)0xfffe0002), VNCEncoding.SupportedEncodings),
				(unchecked((int)0xfffe0003), unchecked((int)0xfffe0003), VNCEncoding.ServerIdentity),
				(unchecked((int)0xfffe0004), unchecked((int)0xfffe00ff), VNCEncoding.libVNCServer),
				(unchecked((int)0xffff0000), unchecked((int)0xffff0000), VNCEncoding.Cache),
				(unchecked((int)0xffff0001), unchecked((int)0xffff0001), VNCEncoding.CacheEnable),
				(unchecked((int)0xffff0002), unchecked((int)0xffff0002), VNCEncoding.XORZlib),
				(unchecked((int)0xffff0003), unchecked((int)0xffff0003), VNCEncoding.XORMonoRectZlib),
				(unchecked((int)0xffff0004), unchecked((int)0xffff0004), VNCEncoding.XORMultiColorZlib),
				(unchecked((int)0xffff0005), unchecked((int)0xffff0005), VNCEncoding.SolidColor),
				(unchecked((int)0xffff0006), unchecked((int)0xffff0006), VNCEncoding.XOREnable),
				(unchecked((int)0xffff0007), unchecked((int)0xffff0007), VNCEncoding.CacheZip),
				(unchecked((int)0xffff0008), unchecked((int)0xffff0008), VNCEncoding.SolMonoZip),
				(unchecked((int)0xffff0009), unchecked((int)0xffff0009), VNCEncoding.UltraZip),
				(unchecked((int)0xffff8000), unchecked((int)0xffff8000), VNCEncoding.ServerState),
				(unchecked((int)0xffff8001), unchecked((int)0xffff8001), VNCEncoding.EnableKeepAlive),
				(unchecked((int)0xffff8002), unchecked((int)0xffff8002), VNCEncoding.FTProtocolVersion),
				(unchecked((int)0xffff8003), unchecked((int)0xffff8003), VNCEncoding.Session),

				(-1, -22, VNCEncoding.TightOptions),
				(-23, -32, VNCEncoding.JPEGQualityLevel),
				(-260, -260, VNCEncoding.TightPNG),
				(-219, -222, VNCEncoding.libVNCServer),
				(-223, -223, VNCEncoding.DesktopSize),
				(-224, -224, VNCEncoding.LastRect),
				(-225, -225, VNCEncoding.PointerPos),
				(-239, -239, VNCEncoding.Cursor),
				(-240, -240, VNCEncoding.XCursor),
				(-247, -256, VNCEncoding.CompressionLevel),
				(-257, -257, VNCEncoding.QEMUPointerMotionChange),
				(-258, -258, VNCEncoding.QEMUExtendedKeyEvent),
				(-259, -259, VNCEncoding.QEMUAudio),
				(-261, -261, VNCEncoding.LEDState),
				(-262, -272, VNCEncoding.QEMU),
				(-273, -304, VNCEncoding.VMware),
				(-305, -305, VNCEncoding.gii),
				(-306, -306, VNCEncoding.popa),
				(-307, -307, VNCEncoding.DesktopName),
				(-308, -308, VNCEncoding.ExtendedDesktopSize),
				(-309, -309, VNCEncoding.xvp),
				(-310, -310, VNCEncoding.OLIVECallControl),
				(-311, -311, VNCEncoding.ClientRedirect),
				(-312, -312, VNCEncoding.Fence),
				(-313, -313, VNCEncoding.ContinuousUpdates),
				(-314, -314, VNCEncoding.CursorWithAlpha),
				(-412, -512, VNCEncoding.JPEGFineGrainedQualityLevel),
				(-523, -528, VNCEncoding.CarConnectivity),
				(-763, -768, VNCEncoding.JPEGSubsamplingLevel),

				(unchecked((int)0x48323634), unchecked((int)0x48323634), VNCEncoding.VAH264),
				(unchecked((int)0x574d5600), unchecked((int)0x574d5663), VNCEncoding.VMware),
				(unchecked((int)0x574d5664), unchecked((int)0x574d5664), VNCEncoding.VMwareCursor),
				(unchecked((int)0x574d5665), unchecked((int)0x574d5665), VNCEncoding.VMwareCursorState),
				(unchecked((int)0x574d5666), unchecked((int)0x574d5666), VNCEncoding.VMwareCursorPosition),
				(unchecked((int)0x574d5667), unchecked((int)0x574d5667), VNCEncoding.VMwareKeyRepeat),
				(unchecked((int)0x574d5668), unchecked((int)0x574d5668), VNCEncoding.VMwareLEDstate),
				(unchecked((int)0x574d5669), unchecked((int)0x574d5669), VNCEncoding.VMwareDisplayModeChange),
				(unchecked((int)0x574d566a), unchecked((int)0x574d566a), VNCEncoding.VMwareVirtualMachineState),
				(unchecked((int)0x574d566b), unchecked((int)0x574d56ff), VNCEncoding.VMware),
				(unchecked((int)0xc0a1e5ce), unchecked((int)0xc0a1e5ce), VNCEncoding.ExtendedClipboard)
			};

			_ClientToServerMessageTypes = new RangeCollection<int, ClientToServerMessageType?>()
			{
				(0, 0, ClientToServerMessageType.SetPixelFormat),
				(2, 2, ClientToServerMessageType.SetEncodings),
				(3, 3, ClientToServerMessageType.FramebufferUpdateRequest),
				(4, 4, ClientToServerMessageType.KeyEvent),
				(5, 5, ClientToServerMessageType.PointerEvent),
				(6, 6, ClientToServerMessageType.ClientCutText),
				(7, 7, ClientToServerMessageType.FileTransfer),
				(8, 8, ClientToServerMessageType.SetScale),
				(9, 9, ClientToServerMessageType.SetServerInput),
				(10, 10, ClientToServerMessageType.SetSW),
				(11, 11, ClientToServerMessageType.TextChat),
				(12, 12, ClientToServerMessageType.KeyFrameRequest),
				(13, 13, ClientToServerMessageType.KeepAlive),
				(14, 14, ClientToServerMessageType.UltraVNC),
				(15, 15, ClientToServerMessageType.SetScaleFactor),
				(16, 19, ClientToServerMessageType.UltraVNC),
				(20, 20, ClientToServerMessageType.RequestSession),
				(21, 21, ClientToServerMessageType.SetSession),
				(80, 80, ClientToServerMessageType.NotifyPluginStreaming),
				(127, 127, ClientToServerMessageType.VMware),
				(128, 128, ClientToServerMessageType.CarConnectivity),
				(150, 150, ClientToServerMessageType.EnableContinuousUpdates),
				(248, 248, ClientToServerMessageType.ClientFence),
				(249, 249, ClientToServerMessageType.OLIVECallControl),
				(250, 250, ClientToServerMessageType.xvpClientMessage),
				(251, 251, ClientToServerMessageType.SetDesktopSize),
				(252, 252, ClientToServerMessageType.Tight),
				(253, 253, ClientToServerMessageType.giiClientMessage),
				(254, 254, ClientToServerMessageType.VMware),
				(255, 255, ClientToServerMessageType.QEMUClientMessage)
			};

			_ServerToClientMessageTypes = new RangeCollection<int, ServerToClientMessageType?>()
			{
				(0, 0, ServerToClientMessageType.FramebufferUpdate),
				(1, 1, ServerToClientMessageType.SetColourMapEntries),
				(2, 2, ServerToClientMessageType.Bell),
				(3, 3, ServerToClientMessageType.ServerCutText),
				(4, 4, ServerToClientMessageType.ResizeFrameBuffer),
				(5, 5, ServerToClientMessageType.KeyFrameUpdate),
				(6, 6, ServerToClientMessageType.UltraVNC),
				(7, 7, ServerToClientMessageType.FileTransfer),
				(8, 10, ServerToClientMessageType.UltraVNC),
				(11, 11, ServerToClientMessageType.TextChat),
				(12, 12, ServerToClientMessageType.UltraVNC),
				(13, 13, ServerToClientMessageType.KeepAlive),
				(14, 14, ServerToClientMessageType.UltraVNC),
				(15, 15, ServerToClientMessageType.ResizeFrameBuffer),
				(127, 127, ServerToClientMessageType.VMware),
				(128, 128, ServerToClientMessageType.CarConnectivity),
				(150, 150, ServerToClientMessageType.EndOfContinuousUpdates),
				(173, 173, ServerToClientMessageType.ServerState),
				(248, 248, ServerToClientMessageType.ServerFence),
				(249, 249, ServerToClientMessageType.OLIVECallControl),
				(250, 250, ServerToClientMessageType.xvpServerMessage),
				(252, 252, ServerToClientMessageType.Tight),
				(253, 253, ServerToClientMessageType.giiServerMessage),
				(254, 254, ServerToClientMessageType.VMware),
				(255, 255, ServerToClientMessageType.QEMUServerMessage)
			};
		}

		public Client()
		{
		}
		#endregion

		#region Private methods
		private void Initialize(Stream stream)
		{
			_Stream = stream;
			_Reader = new BinaryReader(_Stream);
			_Writer = new BinaryWriter(_Stream);

			NegotiateVersion();
			NegotiateAuthentication();
			InitializeSession();
		}

		private void NegotiateVersion()
		{
			var matchVersion = _RegexServerVersion.Match(Encoding.UTF8.GetString(_Reader.ReadBytes(12)));

			ServerVersion = new Version(int.Parse(matchVersion.Groups["major"].Value), int.Parse(matchVersion.Groups["minor"].Value));

			_Writer.Write(Encoding.UTF8.GetBytes(string.Format(_VersionFormat, _ClientVersion.Major, _ClientVersion.Minor)));
		}

		private void NegotiateAuthentication()
		{
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
				throw new Exception("No supported security types");
			}

			if (securityTypes.Contains(SecurityType.None))
			{
				_Writer.Write((byte)SecurityType.None);

				if (ServerVersion >= new Version(3, 8))
				{
					var securityResult = (SecurityResult)_Reader.ReadUInt32();

					if (securityResult != SecurityResult.OK)
					{
						throw new Exception($"Connection failed: unknown reason");
					}
				}
			}
			else if (securityTypes.Contains(SecurityType.VNCAuthentication))
			{
				var challenge = _Reader.ReadBytes(16);

				throw new NotImplementedException("TODO: VNC Authentication");
			}
		}

		private void InitializeSession()
		{
			var clientInit = new ClientInit() { Shared = true };

			clientInit.Serialize(_Stream);

			SessionInfo = ServerInit.Deserialize(_Stream);

			_FrameBufferState = new MagickImage(MagickColors.Black, SessionInfo.FrameBufferWidth, SessionInfo.FrameBufferHeight);
			_FrameBufferState.Format = MagickFormat.Mpc;

			SetEncodings(_SupportedEncodings);

			SetPixelFormat(SessionInfo.PixelFormat);

			FramebufferUpdateRequest(false, 0, 0, SessionInfo.FrameBufferWidth, SessionInfo.FrameBufferHeight);

			//EnableContinuousUpdates(true, 0, 0, SessionInfo.FrameBufferWidth, SessionInfo.FrameBufferHeight);

			Task.Run(() =>
			{
				while (true)
				{
					var ultimaAtualizacao = DateTime.Now;
					FramebufferUpdateRequest(true, 0, 0, SessionInfo.FrameBufferWidth, SessionInfo.FrameBufferHeight);
					Task.Delay(TimeSpan.FromSeconds(Math.Max(0.1 - (DateTime.Now - ultimaAtualizacao).TotalSeconds, 0))).Wait();

					break;
				}
			});


			Task.Run(
				() =>
				{
					while (true)
					{
						MessageHandler((ServerToClientMessageType)_Reader.ReadByte());
					}
				}
			).Wait();
		}

		private void MessageHandler(ServerToClientMessageType messageType)
		{
			Trace.TraceInformation($"Received message: {messageType}");

			switch (messageType)
			{
				case ServerToClientMessageType.FramebufferUpdate:
					FramebufferUpdateHandler();
					break;
				case ServerToClientMessageType.SetColourMapEntries:
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
				var stopwatch = Stopwatch.StartNew();

				var padding = _Reader.ReadByte();

				var numberOfRectangles = BinaryConverter.ReadUInt16(_Reader);

				Trace.TraceInformation($"Total rectangles: {numberOfRectangles}");

				if (numberOfRectangles > 0)
				{
					//var newFrameBufferState = new MagickImage(_FrameBufferState);
					var newFrameBufferState = _FrameBufferState;

					MagickImage cursor = null;
					System.Windows.Point cursorPoint = new System.Windows.Point();

					for (int i = 0; i < numberOfRectangles; i++)
					{
						var x = BinaryConverter.ReadUInt16(_Reader);
						var y = BinaryConverter.ReadUInt16(_Reader);
						var width = BinaryConverter.ReadUInt16(_Reader);
						var height = BinaryConverter.ReadUInt16(_Reader);

						var encodingType = (VNCEncoding)BinaryConverter.ReadInt32(_Reader);

						Trace.TraceInformation($"Rectangle {i}: {{{x}, {y}, {width}, {height}}}, EncodingType = {encodingType} ({encodingType:X})");

						if (encodingType == VNCEncoding.Cursor)
						{
							var data = _Reader.ReadBytes(width * height * ((SessionInfo.PixelFormat.BitsPerPixel + 7) / 8));
							var bitmask = _Reader.ReadBytes(((width + 7) / 8) * height);

							cursor = new MagickImage(data, new PixelReadSettings(width, height, StorageType.Char, "BGRA"));
							var rectangleBitMask = new MagickImage(bitmask, new MagickReadSettings() { Format = MagickFormat.Mono, Width = width, Height = height });
							rectangleBitMask.Depth = 1;

							cursorPoint = new System.Windows.Point(x, y);

							//rectangleData.Composite(rectangleBitMask, CompositeOperator.ChangeMask);

							/*
							using (var arquivo = File.Open($@"Imagens\{DateTime.Now:HH_mm_ss}_cursor.png", FileMode.Create, FileAccess.Write, FileShare.Read))
							{
								cursor.Format = MagickFormat.Png32;

								cursor.Write(arquivo);
							}
							*/

							//newFrameBufferState.Composite(rectangleData, x, y, CompositeOperator.Blend);
						}

						if (encodingType == VNCEncoding.CopyRect)
						{
							
						}

						if (encodingType == VNCEncoding.Zlib)
						{
							if (_MemoryStreamCompressed == null)
							{
								_MemoryStreamCompressed = new MemoryStream();
								_MemoryStreamCompressedWriter = new BinaryWriter(_MemoryStreamCompressed);
							}

							var compressedDataLength = (int)BinaryConverter.ReadUInt32(_Reader);
							_MemoryStreamCompressed.Position = 0;
							_MemoryStreamCompressedWriter.Write(_Reader.ReadBytes(compressedDataLength));
							_MemoryStreamCompressed.SetLength(compressedDataLength);
							_MemoryStreamCompressed.Position = 0;

							if (_DeflateStream == null)
							{
								_MemoryStreamCompressed.Position = 2;
								_DeflateStream = new DeflateStream(_MemoryStreamCompressed, CompressionMode.Decompress, false);
								_DeflateStreamReader = new BinaryReader(_DeflateStream);
							}

							var data = _DeflateStreamReader.ReadBytes(width * height * ((SessionInfo.PixelFormat.BitsPerPixel + 7) / 8));

							var rectangleData = new MagickImage(data, new PixelReadSettings(width, height, StorageType.Char, "BGRA"));

							newFrameBufferState.Composite(rectangleData, x, y);
						}

						if (encodingType == VNCEncoding.Hextile)
						{
							MagickColor background = null;
							MagickColor foreground = null;

							for (int textileY = 0; textileY < height; textileY += 16)
							{
								var textileHeight = Math.Min(height - textileY, 16);

								for (int textileX = 0; textileX < width; textileX += 16)
								{
									var textileWidth = Math.Min(width - textileX, 16);

									var subencodingMask = (HextileSubencodingMask)_Reader.ReadByte();

									Trace.TraceInformation($"Drawing textile {{{textileX}, {textileY}, {textileWidth}, {textileHeight}}}, subencoding mask: {subencodingMask}");

									if (subencodingMask.HasFlag(HextileSubencodingMask.Raw))
									{
										var data = _Reader.ReadBytes(textileWidth * textileHeight * ((SessionInfo.PixelFormat.BitsPerPixel + 7) / 8));

										var rectangleData = new MagickImage(data, new PixelReadSettings(textileWidth, textileHeight, StorageType.Char, "BGRA"));

										newFrameBufferState.Composite(rectangleData, x + textileX, y + textileY);
									}
									else
									{
										var rectangleData = new MagickImage(MagickColors.Transparent, textileWidth, textileHeight);

										if (subencodingMask.HasFlag(HextileSubencodingMask.BackgroundSpecified))
										{
											background =
												new MagickColor(
													red: _Reader.ReadByte(),
													green: _Reader.ReadByte(),
													blue: _Reader.ReadByte(),
													alpha: _Reader.ReadByte()
												);

											rectangleData.Draw(new DrawableFillColor(background), new DrawableRectangle(new System.Drawing.Rectangle(textileX, textileY, textileWidth, textileHeight)));
										}

										if (subencodingMask.HasFlag(HextileSubencodingMask.ForegroundSpecified))
										{
											foreground =
												new MagickColor(
													red: _Reader.ReadByte(),
													green: _Reader.ReadByte(),
													blue: _Reader.ReadByte(),
													alpha: _Reader.ReadByte()
												);
										}

										if (subencodingMask.HasFlag(HextileSubencodingMask.AnySubrects))
										{
											var numberOfSubrectangles = _Reader.ReadByte();

											Trace.TraceInformation($"Number of subrectangles: {numberOfRectangles}");

											for (int j = 0; j < numberOfSubrectangles; j++)
											{
												var subrectangleColor = foreground;

												if (subencodingMask.HasFlag(HextileSubencodingMask.SubrectsColoured))
												{
													subrectangleColor =
														new MagickColor(
															red: _Reader.ReadByte(),
															green: _Reader.ReadByte(),
															blue: _Reader.ReadByte(),
															alpha: _Reader.ReadByte()
														);
												}

												var subrectangleXY = _Reader.ReadByte();
												var subrectangleWidthHeight = _Reader.ReadByte();

												var subrectangleX = (subrectangleXY & 0xf0) >> 4;
												var subrectangleY = subrectangleXY & 0x0f;

												var subrectangleWidth = ((subrectangleWidthHeight & 0xf0) >> 4) + 1;
												var subrectangleHeight = (subrectangleWidthHeight & 0x0f) + 1;

												rectangleData.Draw(new DrawableFillColor(subrectangleColor), new DrawableRectangle(new System.Drawing.Rectangle(subrectangleX, subrectangleY, subrectangleWidth, subrectangleHeight)));
											}
										}

										newFrameBufferState.Composite(rectangleData, x + textileX, y + textileY);
									}
								}
							}
						}

						if (encodingType == VNCEncoding.Raw)
						{
							var data = _Reader.ReadBytes(width * height * ((SessionInfo.PixelFormat.BitsPerPixel + 7) / 8));

							var rectangleData = new MagickImage(data, new PixelReadSettings(width, height, StorageType.Char, "BGRA"));

							newFrameBufferState.Composite(rectangleData, x, y);
							//rectangleData.Composite(newFrameBufferState, x, y);
						}

						if (encodingType == VNCEncoding.LastRect)
						{
							break;
						}
					}

					if (cursor != null)
					{
						newFrameBufferState.Composite(cursor, (int)cursorPoint.X, (int)cursorPoint.Y, CompositeOperator.Atop);
					}

					//newFrameBufferState.Alpha(AlphaOption.Off);
					//newFrameBufferState.Alpha(AlphaOption.Remove);
					//newFrameBufferState.Format = MagickFormat.Bmp;

					using (var arquivo = File.Open($@"Imagens\{DateTime.Now:HH_mm_ss.fff}.bmp", FileMode.Create, FileAccess.Write, FileShare.Read))
					{
						newFrameBufferState.Write(arquivo);
					}

					//_FrameBufferState.Dispose();
					//_FrameBufferState = newFrameBufferState;

					Trace.TraceInformation($"Finished updating framebuffer after {stopwatch.Elapsed.TotalSeconds} seconds");
				}

				stopwatch.Stop();
			}
			catch (Exception ex)
			{
				Trace.TraceError($"Error while updating Framebuffer: {ex.Message}\r\n{ex.StackTrace}");
			}
		}

		public void ServerCutTextHandler()
		{
			var padding = _Reader.ReadBytes(3);

			var textLength = BinaryConverter.ReadInt32(_Reader);
			var text = Encoding.UTF8.GetString(_Reader.ReadBytes(textLength));
			Trace.TraceInformation($"Server cut text: \"{text}\"");
		}

		public void BellHandler()
		{
			System.Media.SystemSounds.Beep.Play();
		}
		#endregion

		#region Public methods
		public void Connect(string hostname, int port)
		{
			var tcpClient = new TcpClient();
			tcpClient.Connect(hostname, port);

			Initialize(tcpClient.GetStream());
		}

		public void SendMessage(ClientToServerMessageType messageType, byte[] content)
		{
			_Writer.Write((byte)messageType);
			_Writer.Write(content);

			//Trace.TraceInformation($"Message sent: {messageType}");
		}

		public void SetEncodings(IEnumerable<VNCEncoding> encodings)
		{
			SendMessage(
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

		public void SetPixelFormat(PixelFormat pixelFormat)
		{
			SendMessage(
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

		public void FramebufferUpdateRequest(bool incremental, ushort x, ushort y, ushort width, ushort height)
		{
			SendMessage(
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

		public void EnableContinuousUpdates(bool enable, ushort x, ushort y, ushort width, ushort height)
		{
			SendMessage(
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
		#endregion
	}
}
