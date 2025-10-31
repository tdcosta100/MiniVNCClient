using Microsoft.Extensions.Logging;
using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using MiniVNCClient.Decoders;
using MiniVNCClient.Processors;
using MiniVNCClient.Util;
using System.Buffers.Binary;
using System.Text;

namespace MiniVNCClient
{
    internal class MessageHandler : IDisposable
    {
        #region Fields
        private readonly ILogger? _Logger;
        private readonly bool _LogTraceEnabled;
        private readonly PixelFormat _PixelFormat;
        private readonly Dictionary<VNCEncoding, IRectangleDecoder> _Decoders = [];
        private readonly Dictionary<VNCEncoding, IRectangleProcessor.ProcessRectangleDelegate> _Processors = [];
        #endregion

        #region Events
        public event Action<DateTime>? UpdateFramebufferBegin;
        public event Action<DateTime, RectangleInfo, IRectangleData, IRectangleProcessor.ProcessRectangleDelegate?>? ProcessRectangle;
        public event Action<DateTime, IEnumerable<RectangleInfo>>? UpdateFramebufferEnd;
        public event Action<int, IEnumerable<Color16>>? SetColourMapEntries;
        public event Action? Bell;
        public event Action<string>? ServerCutText;
        public event Action<FenceFlags, byte[]?>? ServerFence;
        public event Action<DateTime, RectangleInfo, CursorData>? Cursor;
        public event Action<DateTime, RectangleInfo>? FramebufferSizeChange;
        public event Action<string>? ServerNameChange;
        #endregion

        #region Public properties
        public bool ContinuousUpdatesSupported { get; private set; }
        #endregion

        #region Constructors
        public MessageHandler(PixelFormat pixelFormat, ILogger? logger)
        {
            _PixelFormat = pixelFormat;
            _Logger = logger;
            _LogTraceEnabled = _Logger?.IsEnabled(LogLevel.Trace) ?? false;

            _Processors.Add(VNCEncoding.Raw, pixelFormat.BytesPerPixel == 4 ? Processors.Processors32bpp.RawProcessor.ProcessRectangle : RawProcessor.ProcessRectangle);
            _Processors.Add(VNCEncoding.Hextile, pixelFormat.BytesPerPixel == 4 ? Processors.Processors32bpp.HextileProcessor.ProcessRectangle : HextileProcessor.ProcessRectangle);
            _Processors.Add(VNCEncoding.Zlib, _Processors[VNCEncoding.Raw]);
            _Processors.Add(VNCEncoding.ZlibHex, _Processors[VNCEncoding.Hextile]);
            _Processors.Add(VNCEncoding.ZRLE, pixelFormat.BytesPerPixel == 4 ? Processors.Processors32bpp.ZRLEProcessor.ProcessRectangle : ZRLEProcessor.ProcessRectangle);

            _Decoders.Add(VNCEncoding.Raw, new RawDecoder());
            _Decoders.Add(VNCEncoding.CopyRect, new CopyRectDecoder());
            _Decoders.Add(VNCEncoding.Hextile, new HextileDecoder());
            _Decoders.Add(VNCEncoding.Zlib, new ZlibDecoder((RawDecoder)_Decoders[VNCEncoding.Raw]));
            _Decoders.Add(VNCEncoding.ZlibHex, _Decoders[VNCEncoding.Hextile]);
            _Decoders.Add(VNCEncoding.ZRLE, new ZRLEDecoder());
            _Decoders.Add(VNCEncoding.Cursor, new CursorDecoder());
            _Decoders.Add(VNCEncoding.XCursor, new XCursorDecoder());
            _Decoders.Add(VNCEncoding.CursorWithAlpha, new CursorWithAlphaDecoder(_Decoders, _Processors));
        }
        #endregion

        #region Private methods
        private void HandleFramebufferUpdate(BinaryStream stream)
        {
            try
            {
                _ = stream.ReadByte();

                var rectangleLength = stream.ReadUInt16();

                if (rectangleLength > 0)
                {
                    var updateTime = DateTime.UtcNow;
                    UpdateFramebufferBegin?.Invoke(updateTime);

                    var rectangles = new List<RectangleInfo>();
                    var processingTasks = new List<Task>();

                    while (rectangleLength-- > 0)
                    {
                        var rectangleInfo = Serializer.Deserialize<RectangleInfo>(stream);

                        if (rectangleInfo.Encoding == VNCEncoding.DesktopName)
                        {
                            ServerNameChange?.Invoke(
                                Encoding.UTF8.GetString(
                                    stream.ReadBytes((int)stream.ReadUInt32())
                                )
                            );

                            continue;
                        }

                        if (rectangleInfo.Encoding == VNCEncoding.LastRect)
                        {
                            break;
                        }

                        rectangles.Add(rectangleInfo);

                        if (rectangleInfo.Encoding == VNCEncoding.DesktopSize)
                        {
                            if (_LogTraceEnabled)
                            {
                                Task.Run(() => _Logger?.LogTrace("Received DesktopSize rectangle: {rectangle}", rectangleInfo));
                            }

                            Task.WhenAll(processingTasks).Wait();
                            FramebufferSizeChange?.Invoke(updateTime, rectangleInfo);
                            rectangles.Clear();
                            continue;
                        }

                        if (_Decoders.TryGetValue(rectangleInfo.Encoding, out IRectangleDecoder? decoder))
                        {
                            try
                            {
                                IRectangleData rectangleData = decoder.Decode(stream, rectangleInfo, _PixelFormat.BytesPerPixel, _PixelFormat.Depth);

                                switch (rectangleInfo.Encoding)
                                {
                                    case VNCEncoding.Cursor:
                                    case VNCEncoding.CursorWithAlpha:
                                    case VNCEncoding.XCursor:
                                        processingTasks.Add(Task.Run(() => Cursor?.Invoke(updateTime, rectangleInfo, (CursorData)rectangleData)));
                                        break;
                                    default:
                                        processingTasks.Add(Task.Run(() =>
                                            ProcessRectangle?.Invoke(
                                                updateTime,
                                                rectangleInfo,
                                                rectangleData,
                                                _Processors.TryGetValue(rectangleInfo.Encoding, out IRectangleProcessor.ProcessRectangleDelegate? processRectangle)
                                                    ? processRectangle
                                                    : null
                                            )
                                        ));
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _Logger?.LogError("Error decoding rectangle: {exception}", ex);
                            }
                        }
                        else
                        {
                            throw new Exception($"Unsupported rectangle encoding: {rectangleInfo.Encoding}");
                        }
                    }

                    if (_LogTraceEnabled)
                    {
                        Task.Run(() => _Logger?.LogTrace("Received {count} rectangles:{newLine}{rectangles}", rectangles.Count, Environment.NewLine, string.Join(Environment.NewLine, rectangles)));
                    }

                    Task.WhenAll(processingTasks).Wait();

                    UpdateFramebufferEnd?.Invoke(updateTime, [.. rectangles]);
                }
            }
            catch (Exception ex)
            {
                if (ex is OutOfMemoryException)
                {
                    throw;
                }

                if (ex is not ObjectDisposedException)
                {
                    _Logger?.LogError("Error while updating Framebuffer: {exception}", ex);
                }
            }
        }

        private void HandleSetColourMapEntries(BinaryStream stream)
        {
            _ = stream.ReadByte();

            var firstColor = stream.ReadUInt16();
            var colorLength = stream.ReadUInt16();
            var palette = Enumerable.Range(0, colorLength)
                .Select(_ => Serializer.Deserialize<Color16>(stream))
                .ToArray();

            if (_LogTraceEnabled)
            {
                Task.Run(() => _Logger?.LogTrace("Color palette update: First color = {firstColor}, number of color entries = {colorLength} Colors:{Environment.NewLine}{colors}", firstColor, colorLength, Environment.NewLine, string.Join(Environment.NewLine, palette)));
            }

            SetColourMapEntries?.Invoke(firstColor, palette);
        }

        private void HandleBell()
        {
            Bell?.Invoke();
        }

        private void HandleServerCutText(BinaryStream stream)
        {
            _ = stream.ReadBytes(3);
            var textLength = stream.ReadUInt32();
            var text = Encoding.Latin1.GetString(stream.ReadBytes((int)textLength));

            if (_LogTraceEnabled)
            {
                Task.Run(() => _Logger?.LogTrace("Server cut text: {text}", text));
            }

            ServerCutText?.Invoke(text);
        }

        private void HandleServerFence(BinaryStream stream)
        {
            _ = stream.ReadBytes(3);

            var flags = (FenceFlags)stream.ReadUInt32();
            var data = stream.ReadBytes(stream.ReadByte());

            if (_LogTraceEnabled)
            {
                Task.Run(() => _Logger?.LogTrace("ServerFence flags: {flags}, Data: {data}", flags, string.Join(" ", data.Select(v => v.ToString("x2")))));
            }

            ServerFence?.Invoke(flags, data);
        }
        #endregion

        #region Public methods
        public void HandleMessage(BinaryStream stream)
        {
            var messageType = (ServerToClientMessageType)stream.ReadByte();

            if (_LogTraceEnabled)
            {
                Task.Run(() => _Logger?.LogTrace("Received message: {messageType}", messageType));
            }

            switch (messageType)
            {
                case ServerToClientMessageType.FramebufferUpdate:
                    HandleFramebufferUpdate(stream);
                    break;
                case ServerToClientMessageType.SetColourMapEntries:
                    HandleSetColourMapEntries(stream);
                    break;
                case ServerToClientMessageType.Bell:
                    HandleBell();
                    break;
                case ServerToClientMessageType.ServerCutText:
                    HandleServerCutText(stream);
                    break;
                case ServerToClientMessageType.EndOfContinuousUpdates:
                    ContinuousUpdatesSupported = true;
                    break;
                case ServerToClientMessageType.ServerFence:
                    HandleServerFence(stream);
                    break;
                case ServerToClientMessageType.ResizeFrameBuffer:
                case ServerToClientMessageType.KeyFrameUpdate:
                case ServerToClientMessageType.UltraVNC:
                case ServerToClientMessageType.FileTransfer:
                case ServerToClientMessageType.TextChat:
                case ServerToClientMessageType.KeepAlive:
                case ServerToClientMessageType.VMware:
                case ServerToClientMessageType.CarConnectivity:
                case ServerToClientMessageType.ServerState:
                case ServerToClientMessageType.OLIVECallControl:
                case ServerToClientMessageType.xvpServerMessage:
                case ServerToClientMessageType.Tight:
                case ServerToClientMessageType.giiServerMessage:
                case ServerToClientMessageType.QEMUServerMessage:
                    Task.Run(() => _Logger?.LogWarning("Unsupported message type: {messageType}", messageType));
                    break;
                default:
                    throw new Exception($"Unknown message type: 0x{(byte)messageType:x2}");
            }
        }

        public void SendMessage(BinaryStream stream, ClientToServerMessageType messageType, byte[] content)
        {
            if (_LogTraceEnabled)
            {
                Task.Run(() => _Logger?.LogTrace("Sending message {messageType}: {content}", messageType, string.Join(" ", content.Select(b => b.ToString("x2")))));
            }

            stream.Write([
                (byte)messageType,
                ..content
            ]);
        }

        public void SetEncodings(BinaryStream stream, IEnumerable<VNCEncoding> encodings)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.SetEncodings,
                content: [
                    0x00,
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)encodings.Count())),
                    ..encodings.SelectMany(e => BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((int)e)))
                ]
            );
        }

        public void SetPixelFormat(BinaryStream stream, PixelFormat pixelFormat)
        {
            using var binaryStream = new BinaryStream(new MemoryStream());

            Serializer.Serialize(binaryStream, pixelFormat);

            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.SetPixelFormat,
                content: [
                    0x00,
                    0x00,
                    0x00,
                    ..((MemoryStream)binaryStream.Stream).ToArray()
                ]
            );
        }

        public void FramebufferUpdateRequest(BinaryStream stream, bool incremental, int x, int y, int width, int height)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.FramebufferUpdateRequest,
                content: [
                    (byte)(incremental ? 1 : 0),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)x)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)y)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)width)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)height))
                ]
            );
        }

        public void KeyEvent(BinaryStream stream, bool keyDown, KeyDefinitions keyCode)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.KeyEvent,
                content: [
                    (byte)(keyDown ? 1 : 0),
                    ..new byte[2],
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)keyCode))
                ]
            );
        }

        public void PointerEvent(BinaryStream stream, int x, int y, PointerButtons buttonMask)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.PointerEvent,
                content: [
                    (byte)buttonMask,
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)x)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)y))
                ]
            );
        }

        public void ClientCutText(BinaryStream stream, string text)
        {
            var textBytes = Encoding.ASCII.GetBytes(text);

            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.ClientCutText,
                content: [
                    ..new byte[3],
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)textBytes.Length)),
                    ..textBytes
                ]
            );
        }

        public void ClientFence(BinaryStream stream, FenceFlags flags, byte[]? data)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.ClientFence,
                content: [
                    ..new byte[3],
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)flags)),
                    (byte)(data?.Length ?? 0),
                    ..data ?? []
                ]
            );
        }

        public void EnableContinuousUpdates(BinaryStream stream, bool enable, int x, int y, int width, int height)
        {
            SendMessage(
                stream: stream,
                messageType: ClientToServerMessageType.EnableContinuousUpdates,
                content: [
                    (byte)(enable ? 1 : 0),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)x)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)y)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)width)),
                    ..BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)height))
                ]
            );
        }

        public void Dispose()
        {
            foreach (var decoder in _Decoders.Values.OfType<IDisposable>())
            {
                decoder.Dispose();
            }
        }
        #endregion
    }
}
