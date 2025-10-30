using Microsoft.Extensions.Logging;
using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using MiniVNCClient.Processors;
using MiniVNCClient.Security;
using MiniVNCClient.Util;
using System.Collections.Concurrent;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniVNCClient
{
    /// <summary>
    /// The exception that is thrown when a client is not connected, either by not being connected yet or by having its connection lost
    /// </summary>
    public class ClientNotConnectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ClientNotConnectedException"/> exception
        /// </summary>
        public ClientNotConnectedException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ClientNotConnectedException"/> exception
        /// with a specified error message
        /// </summary>
        /// <param name="message"></param>
        public ClientNotConnectedException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ClientNotConnectedException"/> exception
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ClientNotConnectedException(string? message, Exception innerException) : base(message, innerException)
        {
        }
    }

    internal readonly record struct Update(Task Start, ConcurrentBag<Task<RectangleInfo>> Pending, TaskCompletionSource End);

    /// <summary>
    /// Client for VNC connections
    /// </summary>
    public partial class Client
    {
        #region Fields
        private static readonly RangeCollection<SecurityType> _SecurityTypes = new([
            new KeyValuePair<Range, SecurityType>(0..0,     SecurityType.Invalid),
            new KeyValuePair<Range, SecurityType>(1..1,     SecurityType.None),
            new KeyValuePair<Range, SecurityType>(2..2,     SecurityType.VNCAuthentication),
            new KeyValuePair<Range, SecurityType>(3..4,     SecurityType.RealVNC),
            new KeyValuePair<Range, SecurityType>(5..5,     SecurityType.RA2),
            new KeyValuePair<Range, SecurityType>(6..6,     SecurityType.RA2ne),
            new KeyValuePair<Range, SecurityType>(7..15,    SecurityType.RealVNC),
            new KeyValuePair<Range, SecurityType>(16..16,   SecurityType.Tight),
            new KeyValuePair<Range, SecurityType>(17..17,   SecurityType.Ultra),
            new KeyValuePair<Range, SecurityType>(18..18,   SecurityType.TLS),
            new KeyValuePair<Range, SecurityType>(19..19,   SecurityType.VeNCrypt),
            new KeyValuePair<Range, SecurityType>(20..20,   SecurityType.SASL),
            new KeyValuePair<Range, SecurityType>(21..21,   SecurityType.MD5),
            new KeyValuePair<Range, SecurityType>(22..22,   SecurityType.xvp),
            new KeyValuePair<Range, SecurityType>(23..23,   SecurityType.SecureTunnel),
            new KeyValuePair<Range, SecurityType>(24..24,   SecurityType.IntegratedSSH),
            new KeyValuePair<Range, SecurityType>(30..35,   SecurityType.Apple),
            new KeyValuePair<Range, SecurityType>(128..255, SecurityType.RealVNC)
        ]);

        private static readonly Dictionary<SecurityType, IAuthHandler> _SupportedSecurityTypes = new()
        {
            { SecurityType.None, new NoneAuthHandler() },
            { SecurityType.VNCAuthentication, new VNCAuthHandler() }
        };

        private static readonly VNCEncoding[] _SupportedEncodings = [
            /* Real encodings */
            VNCEncoding.Raw,
            VNCEncoding.CopyRect,
            VNCEncoding.Hextile,
            VNCEncoding.Zlib,
            VNCEncoding.ZlibHex,
            VNCEncoding.ZRLE,
            
            /* Pseudo-encodings */
            VNCEncoding.DesktopSize,
            VNCEncoding.LastRect,
            VNCEncoding.Cursor,
            VNCEncoding.XCursor,
            VNCEncoding.Fence,
            VNCEncoding.ContinuousUpdates,
            VNCEncoding.CursorWithAlpha,
        ];

        [GeneratedRegex(@"RFB 0*(?<major>\d+)\.0*(?<minor>\d+)\n")]
        private static partial Regex ServerVersionRegex();

        private readonly ILogger? _Logger;
        private readonly bool _LogTraceEnabled = false;

        private VNCEncoding[] _EnabledEncodings = _SupportedEncodings;

        private bool _DisconnectCalled = false;

        private Stream? _Stream;
        private TcpClient? _TcpClient;
        private BinaryStream? _RFBStream;

        private MessageHandler? _MessageHandler;

        private byte[]? _Framebuffer;
        private nint? _FramebufferAddress;
        private int? _FramebufferSize;
        private int? _FramebufferStride;
        private GCHandle? _FramebufferHandle;
        private ReaderWriterLockSlim? _FramebufferLock;

        private Color16[]? _ColorPalette;
        private CursorData? _Cursor;

        private CancellationTokenSource? _CancellationTokenSource;
        private ConcurrentBag<Task>? _PendingUpdates;
        #endregion

        #region Private properties
        private Stream Stream => _Stream ?? throw new ClientNotConnectedException();
        private TcpClient TcpClient => _TcpClient ?? throw new ClientNotConnectedException();
        private BinaryStream RFBStream => _RFBStream ?? throw new ClientNotConnectedException();
        private MessageHandler MessageHandler => _MessageHandler ?? throw new ClientNotConnectedException();
        private nint FramebufferAddress => _FramebufferAddress ?? throw new ClientNotConnectedException();
        private ReaderWriterLockSlim FramebufferLock => _FramebufferLock ?? throw new ClientNotConnectedException();
        private CancellationTokenSource CancellationTokenSource => _CancellationTokenSource ?? throw new ClientNotConnectedException();
        private ConcurrentBag<Task> PendingUpdates => _PendingUpdates ?? throw new ClientNotConnectedException();
        private Span<byte> FramebufferSpan =>
            MemoryMarshal.CreateSpan(
                reference: ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), FramebufferAddress),
                length: FramebufferSize
            );
        #endregion

        #region Delegates
        /// <summary>
        /// Represents the method that will return a <see cref="nint"/> pointing to the framebuffer data,
        /// and the framebuffer stride (size of the each framebuffer line, in bytes)
        /// </summary>
        /// <param name="size">The minimum size, in bytes, of the framebuffer</param>
        /// <param name="stride">The minimum stride, in bytes, of the framebuffer</param>
        /// <returns>A tuple with the pointer to the framebuffer data, the framebuffer data size and the framebuffer stride</returns>
        /// <exception cref="InvalidOperationException">Thrown if the stride is less than the requested value</exception>
        public delegate (nint FramebufferData, int FramebufferSize, int FramebufferStride) CreateFramebufferHandler(int size, int stride);

        /// <summary>
        /// Represents the method that will handle a framebuffer update start
        /// </summary>
        /// <param name="updateTime">The time at which the update was sent by the server</param>
        public delegate void FramebufferUpdateStartHandler(DateTime updateTime);

        /// <summary>
        /// Represents the method that will handle a framebuffer update end
        /// </summary>
        /// <param name="rectangles">The areas of the framebuffer that changed</param>
        /// <param name="updateTime">The time at which the update was sent by the server</param>
        public delegate void FramebufferUpdateEndHandler(IEnumerable<RectangleInfo> rectangles, DateTime updateTime);

        /// <summary>
        /// Represents the method that will handle a
        /// <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#servercuttext">ServerCutText</see> message
        /// </summary>
        /// <param name="text">The text of the server clipboard</param>
        public delegate void ServerCutTextHandler(string text);
        #endregion

        #region Events
        /// <summary>
        /// Raised when the framebuffer is created, when the connection is made or the screen size changes.
        /// It's optional to subscribe this event. If not subscribed, the framebuffer will be created internally,
        /// and can be accessed using one of the methods <see cref="GetFramebuffer()"/>, <see cref="GetFramebuffer(nint)"/>, <see cref="GetFramebuffer(Span{byte})"/>
        /// or <see cref="GetFramebufferArea(Rectangle, Span{byte}, int, Rectangle?)"/>
        /// </summary>
        public event CreateFramebufferHandler? CreateFramebuffer;

        /// <summary>
        /// Raised when the server sends a <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#framebufferupdate">FramebufferUpdate</see> message.
        /// </summary>
        public event FramebufferUpdateStartHandler? FramebufferUpdateStart;

        /// <summary>
        /// Raised when the server finishes sending a <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#framebufferupdate">FramebufferUpdate</see> message.
        /// </summary>
        public event FramebufferUpdateEndHandler? FramebufferUpdateEnd;

        /// <summary>
        /// Raised when the server sends a <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#setcolourmapentries">SetColourMapEntries</see> message.
        /// </summary>
        public event Action? PaletteUpdated;

        /// <summary>
        /// Raised when the server sends a <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#bell">Bell</see> message.
        /// </summary>
        public event Action? Bell;

        /// <summary>
        /// Raised when the server sends a <see href="https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#servercuttext">ServerCutText</see> message.
        /// </summary>
        public event ServerCutTextHandler? ServerCutText;

        /// <summary>
        /// Raised when the server updates the remote cursor.
        /// </summary>
        public event Action? CursorUpdated;
        
        /// <summary>
        /// Raised when the client is disconnected.
        /// </summary>
        public event Action? Disconnected;
        #endregion

        #region Properties
        internal static Version Version33 => new(3, 3);
        internal static Version Version37 => new(3, 7);
        internal static Version Version38 => new(3, 8);

        /// <summary>
        /// Gets or sets if the client should try to connect in shared (<see langword="true"/>) or exclusive (<see langword="false"/>) mode.
        /// </summary>
        public bool Shared { get; set; }

        /// <summary>
        /// Sets the encrypted password (generated by tools like <i>vncpasswd</i>) to be used in the connection.
        /// </summary>
        public byte[]? Password { get; set; }

        /// <summary>
        /// Client RFB protocol version.
        /// </summary>
        public static Version ClientVersion => Version38;

        /// <summary>
        /// Client supported encodings.
        /// </summary>
        public static VNCEncoding[] SupportedEncodings => _SupportedEncodings;

        /// <summary>
        /// Gets or sets the client enabled encodings. Must be a subset of <see cref="SupportedEncodings"/>.
        /// The order of the encodings defines the preference if the server needs to choose between them.
        /// </summary>
        public VNCEncoding[] EnabledEncodings
        {
            get => _EnabledEncodings;
            set
            {
                var notSupportedEncodings = value.Where(v => !SupportedEncodings.Contains(v));

                if (notSupportedEncodings.Any())
                {
                    throw new InvalidOperationException($"The following encodings are not supported: {string.Join(", ", notSupportedEncodings)}");
                }

                _EnabledEncodings = value;
            }
        }

        /// <summary>
        /// <see langword="true"/> if the client is connected to the server.
        /// </summary>
        public bool Connected
        {
            get
            {
                try
                {
                    return !_DisconnectCalled && _Stream is not null && (_Stream is not NetworkStream || TcpClient.Connected);
                }
                catch (ClientNotConnectedException)
                {
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Server RFB protocol version.
        /// </summary>
        public Version? ServerVersion { get; private set; }

        /// <summary>
        /// Server parameters for the connection.
        /// </summary>
        public ServerInfo ServerInfo { get; private set; }

        /// <summary>
        /// <see langword="true"/> if server supports continuous framebuffer updates.
        /// </summary>
        public bool ContinuousUpdatesSupported => _MessageHandler?.ContinuousUpdatesSupported ?? false;

        /// <summary>
        /// Framebuffer total size (in <see cref="byte"/>s).
        /// </summary>
        public int FramebufferSize => _FramebufferSize ?? 0;

        /// <summary>
        /// Framebuffer line length (in <see cref="byte"/>s).
        /// </summary>
        public int FramebufferStride => _FramebufferStride ?? default;

        /// <summary>
        /// Color palette, when the <see cref="PixelFormat.TrueColorFlag"/> is set to zero, <see langword="null"/> otherwise.
        /// </summary>
        public Color16[]? ColorPalette => _ColorPalette;

        /// <summary>
        /// Remote cursor data, if set by server.
        /// </summary>
        public CursorData? Cursor => _Cursor;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a <see cref="Client"/> instance
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger"/> to register log messages.</param>
        public Client(ILogger? logger = default)
        {
            _Logger = logger;
            _LogTraceEnabled = _Logger?.IsEnabled(LogLevel.Trace) ?? false;
        }
        #endregion

        #region Private methods
        private void Initialize()
        {
            try
            {
                NegotiateVersion();
                NegotiateAuthentication();
                InitializeSession();
            }
            catch (ClientNotConnectedException)
            {
                throw new Exception("Connection lost during initialization");
            }
            catch (Exception ex)
            {
                _Logger?.LogError("Error during intialization: {exception}", ex);
                throw;
            }
        }

        private void NegotiateVersion()
        {
            _Logger?.LogInformation("Negotiating version");

            var stringVersion = Encoding.ASCII.GetString(RFBStream.ReadBytes(12));

            if (_LogTraceEnabled)
            {
                _Logger?.LogTrace("Received {stringVersion}", stringVersion);
            }

            var versionMatch = ServerVersionRegex().Match(stringVersion);

            if (!versionMatch.Success)
            {
                throw new Exception("Server not recognized");
            }

            ServerVersion = new Version($"{versionMatch.Groups["major"].Value}.{versionMatch.Groups["minor"].Value}");

            _Logger?.LogInformation("Server version: {serverVersion}", ServerVersion);

            var clientVersionString = $"RFB {ClientVersion.Major:000}.{ClientVersion.Minor:000}\n";

            if (_LogTraceEnabled)
            {
                _Logger?.LogTrace("Sending client version {clientVersion}: {clientVersionString}", ClientVersion, clientVersionString);
            }

            RFBStream.Write(Encoding.ASCII.GetBytes(clientVersionString));
        }

        private void NegotiateAuthentication()
        {
            _Logger?.LogInformation("Negotiating authentication method");

            IAuthHandler[] authHandlers = [];

            if (ServerVersion >= Version37)
            {
                var securityTypesCount = RFBStream.ReadByte();

                if (securityTypesCount > 0)
                {
                    authHandlers = [
                        .. RFBStream.ReadBytes(securityTypesCount)
                        .Where(s => _SecurityTypes.ContainsKey(s) && _SupportedSecurityTypes.ContainsKey(_SecurityTypes[s]))
                        .Select(s => _SupportedSecurityTypes[_SecurityTypes[s]])
                    ];
                }
                else
                {
                    var reasonLength = RFBStream.ReadUInt32();
                    throw new Exception($"Connection failed: {Encoding.ASCII.GetString(RFBStream.ReadBytes((int)reasonLength))}");
                }
            }
            else
            {
                var securityType = (SecurityType)RFBStream.ReadUInt32();

                if (securityType == SecurityType.Invalid)
                {
                    var reasonLength = RFBStream.ReadUInt32();
                    throw new Exception($"Connection failed: {Encoding.ASCII.GetString(RFBStream.ReadBytes((int)reasonLength))}");
                }

                if (_SupportedSecurityTypes.TryGetValue(securityType, out IAuthHandler? value))
                {
                    authHandlers = [value];
                }
            }

            if (authHandlers.Length > 0)
            {
                switch (authHandlers[0].Handle(this, RFBStream))
                {
                    case SecurityResult.OK:
                        _Logger?.LogInformation($"Authentication successful");
                        break;
                    case SecurityResult.Failed:
                        throw new Exception("Authentication failed");
                    case SecurityResult.FailedTooManyAttempts:
                        throw new Exception("Authentication failed: too many attempts");
                    default:
                        break;
                }
            }
            else
            {
                throw new Exception("No supported authentication method");
            }
        }

        private void InitializeSession()
        {
            _Logger?.LogInformation("Sending client initialization");

            RFBStream.Write((byte)(Shared ? 1 : 0));

            ServerInfo = Serializer.Deserialize<ServerInfo>(RFBStream);

            _Logger?.LogInformation("Received server initialization: {serverInfo}", ServerInfo);

            _FramebufferLock = new(LockRecursionPolicy.SupportsRecursion);

            _FramebufferStride = ServerInfo.FramebufferWidth * ServerInfo.PixelFormat.BytesPerPixel;

            FramebufferLock.EnterWriteLock();

            if (CreateFramebuffer is not null)
            {
                (nint framebufferAddress, _FramebufferSize, _FramebufferStride) = CreateFramebuffer(ServerInfo.FramebufferHeight * FramebufferStride, FramebufferStride);
                _FramebufferAddress = framebufferAddress;
            }
            else
            {
                _Framebuffer = new byte[ServerInfo.FramebufferHeight * FramebufferStride];
                _FramebufferSize = _Framebuffer.Length;
                _FramebufferHandle = GCHandle.Alloc(_Framebuffer, GCHandleType.Pinned);
                _FramebufferAddress = _FramebufferHandle.Value.AddrOfPinnedObject();
            }

            FramebufferLock.ExitWriteLock();

            _PendingUpdates = [];

            _MessageHandler = new MessageHandler(ServerInfo.PixelFormat, _Logger);

            MessageHandler.UpdateFramebufferBegin += UpdateFramebufferBeginHandler;
            MessageHandler.UpdateFramebufferEnd += UpdateFramebufferEndHandler;
            MessageHandler.ProcessRectangle += ProcessRectangleHandler;
            MessageHandler.SetColourMapEntries += SetColourMapEntriesHandler;
            MessageHandler.Bell += BellHandler;
            MessageHandler.ServerCutText += ServerCutTextMessageHandler;
            MessageHandler.ServerFence += ServerFenceHandler;
            MessageHandler.Cursor += CursorHandler;
            MessageHandler.FramebufferSizeChange += FramebufferSizeChangeHandler;

            _Logger?.LogInformation("Sending supported encodings: {supportedEncodings}", string.Join(", ", _SupportedEncodings));

            SetEncodings(_EnabledEncodings);

            _CancellationTokenSource = new();

            Task.Run(() =>
            {
                Thread.CurrentThread.Name = "MiniVNCClient - Message Listener";

                _Logger?.LogInformation("Starting to listen for server messages");

                while (Connected)
                {
                    try
                    {
                        MessageHandler.HandleMessage(RFBStream);
                    }
                    catch (EndOfStreamException)
                    {
                        try
                        {
                            Disconnect();
                        }
                        finally
                        {
                        }
                    }
                    catch (ClientNotConnectedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        if (Connected)
                        {
                            _Logger?.LogError("Error reading messages from server: {exception}", ex);
                        }
                    }
                }
            }, CancellationTokenSource.Token);
        }

        private void UpdateFramebufferBeginHandler(DateTime updateTime)
        {
            var successful = false;

            try
            {
                FramebufferLock.EnterWriteLock();
                FramebufferUpdateStart?.Invoke(updateTime);
                successful = true;
            }
            catch (ClientNotConnectedException)
            {
                successful = false;
            }
            catch (OperationCanceledException)
            {
                successful = false;
            }
            finally
            {
                if (!successful)
                {
                    _FramebufferLock?.ExitWriteLock();
                }
            }
        }
        private void UpdateFramebufferEndHandler(DateTime updateTime, IEnumerable<RectangleInfo> rectangles)
        {
            try
            {
                Task.WhenAll(PendingUpdates).Wait();
                PendingUpdates.Clear();
                FramebufferLock.ExitWriteLock();
                FramebufferUpdateEnd?.Invoke(rectangles, updateTime);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ClientNotConnectedException)
            {
            }
            finally
            {
                if (_FramebufferLock is not null && _FramebufferLock.IsWriteLockHeld)
                {
                    _FramebufferLock?.ExitWriteLock();
                }
            }
        }

        private void ProcessRectangle(RectangleInfo info, IRectangleData data, IRectangleProcessor.ProcessRectangleDelegate? processRectangle)
        {
            try
            {
                if (data is CopyRectRectangleData rectangleData)
                {
                    CopyRect(info, rectangleData);
                }
                else
                {
                    processRectangle?.Invoke(FramebufferAddress, FramebufferSize, FramebufferStride, info, data, ServerInfo.PixelFormat.BytesPerPixel, ServerInfo.PixelFormat.Depth);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ClientNotConnectedException)
            {
            }
            catch (Exception ex)
            {
                _Logger?.LogError("Error processing rectangle: {exception}", ex);
            }
        }

        private void ProcessRectangleHandler(DateTime updateTime, RectangleInfo info, IRectangleData data, IRectangleProcessor.ProcessRectangleDelegate? processRectangle)
        {
            try
            {
                PendingUpdates.Add(Task.Run(() =>
                {
                    ProcessRectangle(info, data, processRectangle);
                }, CancellationTokenSource.Token));
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void CopyRect(RectangleInfo info, CopyRectRectangleData rectangleData)
        {
            try
            {
                var width = info.Width * ServerInfo.PixelFormat.BytesPerPixel;

                var destRow = info.Y * FramebufferStride;
                var destRowEnd = destRow + info.Height * FramebufferStride;
                var destColumn = info.X * ServerInfo.PixelFormat.BytesPerPixel;

                var sourceRow = rectangleData.SourceY * FramebufferStride;
                var sourceColumn = rectangleData.SourceX * ServerInfo.PixelFormat.BytesPerPixel;

                var framebufferSpan = FramebufferSpan;

                for (; destRow < destRowEnd; sourceRow += FramebufferStride, destRow += FramebufferStride)
                {
                    framebufferSpan
                        .Slice(start: sourceRow + sourceColumn, length: width)
                        .CopyTo(framebufferSpan.Slice(start: destRow + destColumn, length: width));
                }
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void SetColourMapEntriesHandler(int firstColor, IEnumerable<Color16> colors)
        {
            try
            {
                if (!Connected)
                {
                    return;
                }

                _ColorPalette ??= new Color16[256];
                colors.ToArray().CopyTo(_ColorPalette, firstColor);
                Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "MiniVNCClient - PaletteUpdated event";
                    PaletteUpdated?.Invoke();
                }, CancellationTokenSource.Token);
            }
            catch (ClientNotConnectedException)
            {
            }
            catch (Exception ex)
            {
                _Logger?.LogError("Error setting the color palette: {exception}", ex);
            }
        }

        private void BellHandler()
        {
            try
            {
                Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "MiniVNCClient - Bell event";
                    Bell?.Invoke();
                }, CancellationTokenSource.Token);
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void ServerCutTextMessageHandler(string text)
        {
            try
            {
                Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "MiniVNCClient - ServerCutText event";
                    ServerCutText?.Invoke(text);
                }, CancellationTokenSource.Token);
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void ServerFenceHandler(FenceFlags flags, byte[]? data)
        {
            try
            {
                MessageHandler?.ClientFence(RFBStream, flags & (FenceFlags.BlockBefore | FenceFlags.BlockAfter | FenceFlags.SyncNext), data);
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void CursorHandler(DateTime updateTime, RectangleInfo rectangleInfo, CursorData cursorData)
        {
            try
            {
                Task.Run(() =>
                {
                    Thread.CurrentThread.Name = "MiniVNCClient - CursorUpdated event";

                    var cursorBytesPerPixel = 4;
                    var clientBytesPerPixel = ServerInfo.PixelFormat.BytesPerPixel;

                    switch (cursorData)
                    {
                        case CursorRectangleData cursor:
                            {
                                var bitStride = (cursor.Width + 7) / 8 * 8;
                                var bitMask = cursor.BitMask
                                    !.SelectMany(b =>
                                        Enumerable.Range(0, 8)
                                        .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                                    )
                                    .ToArray();

                                var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];

                                var width = cursor.Width * cursorBytesPerPixel;
                                var height = cursor.Height * cursorBytesPerPixel;

                                for (int y = 0; y < cursor.Height; y++)
                                {
                                    for (int x = 0; x < cursor.Width; x++)
                                    {
                                        cursor.PixelData
                                            .AsSpan()
                                            .Slice(start: (y * cursor.Width + x) * clientBytesPerPixel, length: clientBytesPerPixel)
                                            .CopyTo(
                                                pixelData.AsSpan()
                                                .Slice(start: (y * cursor.Width + x) * cursorBytesPerPixel, length: cursorBytesPerPixel)
                                            );

                                        pixelData[(y * cursor.Width + x) * cursorBytesPerPixel + 3] = (byte)(bitMask[y * bitStride + x] ? 0xff : 0x00);
                                    }
                                }

                                cursor.PixelData = pixelData;
                            }
                            break;
                        case CursorWithAlphaRectangleData cursor:
                            {
                                var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];
                                var pixelDataHandler = GCHandle.Alloc(pixelData, GCHandleType.Pinned);

                                rectangleInfo.X = 0;
                                rectangleInfo.Y = 0;
                                cursor.ProcessRectangle!(pixelDataHandler.AddrOfPinnedObject(), pixelData.Length, cursor.Width * cursorBytesPerPixel, rectangleInfo, cursor.CursorData!, cursorBytesPerPixel, 32);
                                cursor.PixelData = pixelData;
                                pixelDataHandler.Free();
                            }
                            break;
                        case XCursorRectangleData cursor:
                            {
                                var pixelData = new byte[cursor.Width * cursor.Height * cursorBytesPerPixel];

                                var bitStride = (cursor.Width + 7) / 8 * 8;

                                var bitMask = cursor.BitMask!
                                    .SelectMany(b =>
                                        Enumerable.Range(0, 8)
                                        .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                                    )
                                    .ToArray();

                                var bitMap = cursor.BitMap!
                                    .SelectMany(b =>
                                        Enumerable.Range(0, 8)
                                        .Select(i => ((b >> (8 - (i + 1))) & 0b00000001) == 1)
                                    )
                                    .ToArray();

                                byte[][] colors = [
                                    [ cursor.PrimaryColor.Blue, cursor.PrimaryColor.Green, cursor.PrimaryColor.Red ],
                                [ cursor.SecondaryColor.Blue, cursor.SecondaryColor.Green, cursor.SecondaryColor.Red ]
                                ];

                                for (int y = 0; y < cursor.Height; y++)
                                {
                                    for (int x = 0; x < cursor.Width; x++)
                                    {
                                        colors[bitMap[y * bitStride + x] ? 0 : 1]
                                            .AsSpan()
                                            .CopyTo(
                                                pixelData.AsSpan()
                                                .Slice(start: (y * cursor.Width + x) * cursorBytesPerPixel, length: cursorBytesPerPixel)
                                            );

                                        pixelData[(y * cursor.Width + x) * cursorBytesPerPixel + 3] = (byte)(bitMask[y * bitStride + x] ? 0xff : 0x00);
                                    }
                                }

                                cursor.PixelData = pixelData;
                            }
                            break;
                        default:
                            break;
                    }

                    _Cursor = cursorData;
                    CursorUpdated?.Invoke();
                }, CancellationTokenSource.Token);
            }
            catch (ClientNotConnectedException)
            {
            }
        }

        private void FramebufferSizeChangeHandler(DateTime updateTime, RectangleInfo rectangleInfo)
        {
            _Framebuffer = null;
            _FramebufferHandle?.Free();
            _FramebufferSize = null;
            _FramebufferHandle = null;
            _FramebufferAddress = null;

            ServerInfo = new ServerInfo()
            {
                FramebufferWidth = rectangleInfo.Width,
                FramebufferHeight = rectangleInfo.Height,
                PixelFormat = ServerInfo.PixelFormat,
                NameLength = ServerInfo.NameLength,
                Name = ServerInfo.Name
            };

            _FramebufferStride = ServerInfo.PixelFormat.BytesPerPixel;

            if (CreateFramebuffer is not null)
            {
                (nint framebufferAddress, _FramebufferSize, _FramebufferStride) = CreateFramebuffer(ServerInfo.FramebufferHeight * FramebufferStride, FramebufferStride);
                _FramebufferAddress = framebufferAddress;
            }
            else
            {
                _Framebuffer = new byte[ServerInfo.FramebufferHeight * FramebufferStride];
                _FramebufferSize = _Framebuffer.Length;
                _FramebufferHandle = GCHandle.Alloc(_Framebuffer, GCHandleType.Pinned);
                _FramebufferAddress = _FramebufferHandle.Value.AddrOfPinnedObject();
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Encrypts a plain-text password, just like <i>vncpasswd</i> does.
        /// Use this method to encrypt a password for the <see cref="Password"/> property.
        /// </summary>
        /// <param name="password">Plain-text password</param>
        /// <returns>Encrypted password</returns>
        public static byte[] EncryptPassword(string password)
        {
            using var des = DES.Create();
            des.Key = [0xe8, 0x4a, 0xd6, 0x60, 0xc4, 0x72, 0x1a, 0xe0];

            var result = des.EncryptCbc(
                plaintext: (byte[])[.. password.PadRight(8, '\0').Take(8).Select(c => (byte)c)],
                iv: new byte[8],
                paddingMode: PaddingMode.None
            );

            return result;
        }

        /// <summary>
        /// Connects to a VNC server using the host and port specified.
        /// </summary>
        /// <param name="host">Host name or IP address to connect.</param>
        /// <param name="port">Port of the host to connect.</param>
        /// <param name="timeout">Connection timeout.</param>
        /// <exception cref="Exception">Thrown if the connection is not successful</exception>
        public void Connect(string host, int port = 5900, TimeSpan? timeout = default)
        {
            if (Connected)
            {
                _Logger?.LogWarning("The client is already connected");
                return;
            }

            _Logger?.LogInformation("Connecting to {hostname}:{port}", host, port);

            try
            {
                _TcpClient = new TcpClient() { NoDelay = true };
                var connectResult = TcpClient.BeginConnect(host, port, null, null);

                var successful = connectResult.AsyncWaitHandle.WaitOne(timeout ?? Timeout.InfiniteTimeSpan);

                if (!successful)
                {
                    throw new Exception("Connection timeout");
                }

                TcpClient.EndConnect(connectResult);

                _Logger?.LogInformation("Connection successful, initializing session");

                Connect(TcpClient.GetStream());
            }
            catch (Exception ex)
            {
                _Logger?.LogError("Error connecting to {hostname}:{port}: {exception}", host, port, ex);

                try
                {
                    Disconnect();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Connects to a VNC server using the specified stream.
        /// </summary>
        /// <param name="stream">Connection stream.</param>
        public void Connect(Stream stream)
        {
            _DisconnectCalled = false;

            _Stream = stream;
            _RFBStream = new BinaryStream(Stream);

            Initialize();
        }

        /// <summary>
        /// Disconnects the client from the established connection and frees all client resources.
        /// </summary>
        public void Disconnect()
        {
            if (_DisconnectCalled)
            {
                return;
            }

            _DisconnectCalled = true;

            try
            {
                _CancellationTokenSource?.Cancel();
                _CancellationTokenSource = null;

                _TcpClient?.Close();
                _TcpClient = null;

                _RFBStream?.Close();
                _RFBStream = null;

                _Stream = null;

                _PendingUpdates?.Clear();
                _PendingUpdates = null;

                _MessageHandler?.Dispose();
                _MessageHandler = null;

                _Framebuffer = null;

                _FramebufferHandle?.Free();
                _FramebufferHandle = null;

                _FramebufferSize = null;
                _FramebufferStride = null;

                _ColorPalette = null;
                _Cursor = null;

                ServerInfo = default;
                ServerVersion = null;

                var framebufferLock = _FramebufferLock;
                _FramebufferLock = null;
                framebufferLock?.Dispose();
            }
            catch
            {
            }

            Disconnected?.Invoke();
        }

        /// <summary>
        /// Sends a message to the VNC server.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="content">Message content.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool SendMessage(ClientToServerMessageType messageType, byte[] content)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.SendMessage(RFBStream, messageType, content);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending message {messageType}: {exception}", messageType, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.SetEncodings"/> message to the VNC server.
        /// </summary>
        /// <param name="encodings">Collection of <see cref="VNCEncoding"/> supported by client.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool SetEncodings(IEnumerable<VNCEncoding> encodings)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.SetEncodings(RFBStream, encodings);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.SetEncodings, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.SetPixelFormat"/> message to the VNC server.
        /// </summary>
        /// <param name="pixelFormat">The <see cref="PixelFormat"/> to be used in the connection.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool SetPixelFormat(PixelFormat pixelFormat)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.SetPixelFormat(RFBStream, pixelFormat);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.SetPixelFormat, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.FramebufferUpdateRequest"/> message to the VNC server.
        /// </summary>
        /// <param name="incremental"><see langword="true"/> if the framebuffer update should be incremental.</param>
        /// <param name="x">X coordinate of the framebuffer area to request.</param>
        /// <param name="y">Y coordinate of the framebuffer area to request.</param>
        /// <param name="width">Width of the framebuffer area to request.</param>
        /// <param name="height">Height coordinate of the framebuffer area to request.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool FramebufferUpdateRequest(bool incremental, int x, int y, int width, int height)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.FramebufferUpdateRequest(RFBStream, incremental, x, y, width, height);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.FramebufferUpdateRequest, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.KeyEvent"/> message to the VNC server.
        /// </summary>
        /// <param name="keyDown"><see langword="true"/> if the key is pressed, <see langword="false"/> otherwise.</param>
        /// <param name="keyCode">The <see cref="KeyDefinitions"/> code corresponding to the key pressed.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool KeyEvent(bool keyDown, KeyDefinitions keyCode)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.KeyEvent(RFBStream, keyDown, keyCode);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.KeyEvent, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.PointerEvent"/> message to the VNC server.
        /// </summary>
        /// <param name="x">X coordinate of the remote pointer.</param>
        /// <param name="y">Y coordinate of the remote pointer.</param>
        /// <param name="buttonMask">Bitwise combination of the <see cref="PointerButtons"/> values specifying which buttons are pressed.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool PointerEvent(int x, int y, PointerButtons buttonMask)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.PointerEvent(RFBStream, x, y, buttonMask);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.PointerEvent, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.ClientCutText"/> message to the VNC server.
        /// </summary>
        /// <param name="text">Text to be sent to the server.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool ClientCutText(string text)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.ClientCutText(RFBStream, text);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.ClientCutText, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.ClientFence"/> message to the VNC server.
        /// </summary>
        /// <param name="flags">Bitwise combination of the <see cref="FenceFlags"/> values indicating the synchronization desired.</param>
        /// <param name="data"><see langword="byte"/> array containing the message data.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool ClientFence(FenceFlags flags, byte[]? data)
        {
            if (Connected)
            {
                try
                {
                    MessageHandler?.ClientFence(RFBStream, flags, data);
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.ClientFence, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a <see cref="ClientToServerMessageType.EnableContinuousUpdates"/> message to the VNC server.
        /// </summary>
        /// <param name="enable"><see langword="true"/> if server should start sending framebuffer updates continuously,
        /// <see langword="false"/> if server should stop sending framebuffer updates continuously.</param>
        /// <param name="x">X coordinate of the framebuffer area to request framebuffer continuous updates.</param>
        /// <param name="y">Y coordinate of the framebuffer area to request framebuffer continuous updates.</param>
        /// <param name="width">Width of the framebuffer area to request framebuffer continuous updates.</param>
        /// <param name="height">Height coordinate of the framebuffer area to request framebuffer continuous updates.</param>
        /// <returns><see langword="true"/> if the message was sent with success.</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool EnableContinuousUpdates(bool enable, int x, int y, int width, int height)
        {
            if (Connected && ContinuousUpdatesSupported)
            {
                try
                {
                    MessageHandler?.EnableContinuousUpdates(RFBStream, enable, x, y, width, height);

                    return true;
                }
                catch (Exception ex)
                {
                    _Logger?.LogError("Error sending {messageType}: {exception}", ClientToServerMessageType.EnableContinuousUpdates, ex);
                }
            }

            return false;
        }

        /// <summary>
        /// <para>Returns a <see cref="Span{T}"/> of <see cref="byte"/> containing the framebuffer data.</para>
        /// <para><b>Notice:</b> before calling this function, the framebuffer needs to be locked for writing using <see cref="LockFramebuffer"/>.</para>
        /// </summary>
        /// <returns><see cref="Span{T}"/> of <see cref="byte"/> containing the framebuffer data</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="LockFramebuffer"/> was not called before <see cref="GetFramebuffer()"/></exception>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public ReadOnlySpan<byte> GetFramebuffer()
        {
            if (!FramebufferLock.IsReadLockHeld)
            {
                throw new InvalidOperationException("LockFramebuffer() must be called before calling this method");
            }

            return FramebufferSpan;
        }

        /// <summary>
        /// Locks the framebuffer for writing. To release the lock, use <see cref="UnlockFramebuffer"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was successful, <see langword="false"/> otherwise</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool LockFramebuffer()
        {
            try
            {
                FramebufferLock.EnterReadLock();
                return true;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (ClientNotConnectedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _Logger?.LogWarning("Exception thrown: {exception}", ex);
            }

            return false;
        }

        /// <summary>
        /// Unlocks the framebuffer that was locked using <see cref="LockFramebuffer"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the unlock was successful, <see langword="false"/> otherwise</returns>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public bool UnlockFramebuffer()
        {
            if (!FramebufferLock.IsReadLockHeld)
            {
                return true;
            }

            try
            {
                FramebufferLock.ExitReadLock();
                return true;
            }
            catch (ClientNotConnectedException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _Logger?.LogWarning("Exception thrown: {exception}", ex);
            }

            return false;
        }

        /// <summary>
        /// Copies the framebuffer to the <see cref="Span{T}"/> of <see cref="byte"/> specified.
        /// </summary>
        /// <param name="buffer">The <see cref="Span{T}"/> of <see cref="byte"/> which will receive the framebuffer data.</param>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public void GetFramebuffer(Span<byte> buffer)
        {
            try
            {
                FramebufferLock.EnterReadLock();

                if (ServerInfo.PixelFormat.BytesPerPixel == 4)
                {
                    MemoryMarshal.Cast<byte, int>(FramebufferSpan).CopyTo(MemoryMarshal.Cast<byte, int>(buffer));
                }
                else
                {
                    FramebufferSpan.CopyTo(buffer);
                }
            }
            finally
            {
                if (FramebufferLock.IsReadLockHeld)
                {
                    FramebufferLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Copies the framebuffer to the memory address pointed by the <see cref="nint"/> specified.
        /// </summary>
        /// <param name="ptr">The <see cref="nint"/> pointer to the memory address which will receive the framebuffer data.</param>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public void GetFramebuffer(nint ptr)
        {
            try
            {
                FramebufferLock.EnterReadLock();

                var framebufferSpan = FramebufferSpan;

                if (ServerInfo.PixelFormat.BytesPerPixel == 4)
                {
                    var bufferSpan = MemoryMarshal.CreateSpan(
                        reference: ref Unsafe.AddByteOffset(ref Unsafe.NullRef<int>(), ptr),
                        length: framebufferSpan.Length / 4
                    );

                    MemoryMarshal.Cast<byte, int>(framebufferSpan).CopyTo(bufferSpan);
                }
                else
                {
                    var bufferSpan = MemoryMarshal.CreateSpan(
                        reference: ref Unsafe.AddByteOffset(ref Unsafe.NullRef<byte>(), ptr),
                        length: framebufferSpan.Length
                    );

                    framebufferSpan.CopyTo(bufferSpan);
                }
            }
            finally
            {
                FramebufferLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the framebuffer data area specified to the <see cref="Span{T}"/> of <see cref="byte"/> specified.
        /// </summary>
        /// <param name="sourceArea">The area in the framebuffer to be copied to the <paramref name="buffer"/> specified.</param>
        /// <param name="buffer">The <see cref="Span{T}"/> of <see cref="byte"/> to receive the framebuffer area data.</param>
        /// <param name="bufferStride">The line length of the <paramref name="buffer"/>.</param>
        /// <param name="destinationArea">The area in the <paramref name="buffer"/> which will receive the framebuffer area data.
        /// If <see langword="null"/>, will be the same as <paramref name="sourceArea"/>.</param>
        /// <exception cref="InvalidOperationException"><paramref name="sourceArea"/> is outside the framebuffer or <paramref name="destinationArea"/> is outside the <paramref name="buffer"/></exception>
        /// <exception cref="ClientNotConnectedException">Thrown if the client is not connected</exception>
        public void GetFramebufferArea(Rectangle sourceArea, Span<byte> buffer, int bufferStride, Rectangle? destinationArea = default)
        {
            try
            {
                FramebufferLock.EnterReadLock();

                if (ServerInfo.PixelFormat.BytesPerPixel == 4)
                {
                    bufferStride /= 4;
                    var framebufferStride = FramebufferStride / 4;
                    var destArea = destinationArea ?? sourceArea;
                    var sourceColumn = sourceArea.X;
                    var sourceRow = sourceArea.Y * framebufferStride;
                    var sourceRowEnd = sourceRow + sourceArea.Height * framebufferStride;
                    var sourceWidth = sourceArea.Width;
                    var destinationColumn = destArea.X;
                    var destinationRow = destArea.Y * bufferStride;

                    var framebufferSpan = MemoryMarshal.Cast<byte, int>(FramebufferSpan);
                    var bufferSpan = MemoryMarshal.Cast<byte, int>(buffer);

                    if (
                        (sourceColumn + sourceArea.Width) > framebufferStride
                        || sourceRowEnd > framebufferSpan.Length
                    )
                    {
                        throw new InvalidOperationException("Source area outside framebuffer");
                    }

                    if (
                        (destinationColumn + destArea.Width) > bufferStride
                        || (destinationRow + destArea.Height) > bufferSpan.Length
                    )
                    {
                        throw new InvalidOperationException("Destination area outside buffer");
                    }

                    for (; sourceRow < sourceRowEnd; sourceRow += framebufferStride, destinationRow += bufferStride)
                    {
                        framebufferSpan.Slice(start: sourceRow + sourceColumn, length: sourceWidth)
                            .CopyTo(bufferSpan.Slice(start: destinationRow + destinationColumn, length: sourceWidth));
                    }
                }
                else
                {
                    var destArea = destinationArea ?? sourceArea;
                    var bytesPerPixel = ServerInfo.PixelFormat.BytesPerPixel;
                    var sourceColumn = sourceArea.X * bytesPerPixel;
                    var sourceRow = sourceArea.Y * FramebufferStride;
                    var sourceRowEnd = sourceRow + sourceArea.Height * FramebufferStride;
                    var sourceWidth = sourceArea.Width * bytesPerPixel;
                    var destinationColumn = destArea.X * bytesPerPixel;
                    var destinationRow = destArea.Y * bufferStride;

                    var framebufferSpan = FramebufferSpan;

                    if (
                        (sourceColumn + sourceArea.Width * bytesPerPixel) > FramebufferStride
                        || sourceRowEnd > framebufferSpan.Length
                    )
                    {
                        throw new InvalidOperationException("Source area outside framebuffer");
                    }

                    if (
                        (destinationColumn + destArea.Width * bytesPerPixel) > bufferStride
                        || (destinationRow + destArea.Height * bytesPerPixel) > buffer.Length
                    )
                    {
                        throw new InvalidOperationException("Destination area outside buffer");
                    }

                    for (; sourceRow < sourceRowEnd; sourceRow += FramebufferStride, destinationRow += bufferStride)
                    {
                        framebufferSpan.Slice(start: sourceRow + sourceColumn, length: sourceWidth)
                            .CopyTo(buffer.Slice(start: destinationRow + destinationColumn, length: sourceWidth));
                    }
                }
            }
            finally
            {
                FramebufferLock.ExitReadLock();
            }
        }
        #endregion
    }
}
