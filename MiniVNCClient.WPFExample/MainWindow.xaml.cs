using Microsoft.Extensions.Logging;
using MiniVNCClient.Data;
using MiniVNCClient.Data.RectangleEncodings;
using System.ComponentModel;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MiniVNCClient.WPFExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields
        private readonly ILogger _Logger;
        private readonly Client _Client;
        private WriteableBitmap? _RemoteFramebufferCanvas;
        private bool _RemoteFramebufferLocked = false;
        private string _OriginalWindowTitle;
        #endregion

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Properties
        public bool Connecting { get; private set; }
        public bool Connected => _Client.Connected;
        #endregion

        #region Constructors
        public MainWindow()
        {
            VisualBitmapScalingMode = BitmapScalingMode.LowQuality;
            using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information).AddDebug());
            _Logger = loggerFactory.CreateLogger(nameof(MainWindow));

            _Client = new(_Logger)
            {
                Shared = true
            };

            _Client.CreateFramebuffer += CreateFramebufferHandler;
            _Client.FramebufferUpdateStart += FramebufferUpdateStartHandler;
            _Client.FramebufferUpdateEnd += FramebufferUpdateEndHandler;
            _Client.PaletteUpdated += PaletteUpdatedHandler;
            _Client.CursorUpdated += CursorUpdatedHandler;
            _Client.Bell += BellHandler;
            _Client.ServerNameChanged += ServerNameChangedHandler;
            _Client.Disconnected += DisconnectedHandler;

            InitializeComponent();

            _OriginalWindowTitle = Title;
        }
        #endregion

        #region Private methods
        #region Window events
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Connecting = true;
            NotifyPropertyChanged(nameof(Connecting));

            var host = TextBoxHost.Text;

            if (string.IsNullOrWhiteSpace(host))
            {
                Connecting = false;
                NotifyPropertyChanged(nameof(Connecting));
                return;
            }

            if (!int.TryParse(TextBoxPort.Text, out int port))
            {
                Connecting = false;
                NotifyPropertyChanged(nameof(Connecting));
                return;
            }

            _Client.Password = Client.EncryptPassword(PasswordBoxPassword.Password);

            Task.Run(async () =>
            {
                try
                {
                    _Client.Connect(host, port, TimeSpan.FromSeconds(5));

                    Connecting = false;
                    NotifyPropertyChanged(nameof(Connecting));
                    NotifyPropertyChanged(nameof(Connected));

                    if (!_Client.Connected)
                    {
                        return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        Title = $"{Title} - {_Client.ServerInfo.Name}";
                        LabelHost.Content = $"{host}:{port}";
                    });

                    if (_Client.ServerInfo.PixelFormat.TrueColorFlag == 1)
                    {
                        _Client.SetPixelFormat(new Data.PixelFormat()
                        {
                            BitsPerPixel = 32,
                            Depth = 24,
                            BigEndianFlag = 0,
                            TrueColorFlag = 1,
                            RedMax = 255,
                            GreenMax = 255,
                            BlueMax = 255,
                            RedShift = 16,
                            GreenShift = 8,
                            BlueShift = 0
                        });
                    }

                    var timeout = 20;

                    while (timeout-- > 0 && !_Client.ContinuousUpdatesSupported)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                    }

                    while (_Client.Connected)
                    {
                        if (_Client.ContinuousUpdatesSupported)
                        {
                            _Client.EnableContinuousUpdates(true, 0, 0, _Client.ServerInfo.FramebufferWidth, _Client.ServerInfo.FramebufferHeight);
                        }
                        else
                        {
                            _Client.FramebufferUpdateRequest(false, 0, 0, _Client.ServerInfo.FramebufferWidth, _Client.ServerInfo.FramebufferHeight);

                            while (_Client.Connected)
                            {
                                _Client.FramebufferUpdateRequest(true, 0, 0, _Client.ServerInfo.FramebufferWidth, _Client.ServerInfo.FramebufferHeight);
                                await Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0));
                            }
                        }
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error connecting with {host}:{port}: {ex.Message}"));
                    NotifyPropertyChanged(nameof(Connected));
                }

                Connecting = false;
                NotifyPropertyChanged(nameof(Connecting));
            });

        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _Client.Disconnect();
            }
            catch
            {
            }
        }

        private void RemoteKeyEvent(object sender, KeyEventArgs e)
        {
            KeyDefinitions remoteKey = 0;

            var preventBubbling = false;

            var sourceKey = e.Key;

            if (e.SystemKey != Key.None)
            {
                sourceKey = e.SystemKey;
                preventBubbling = true;
            }

            if (e.DeadCharProcessedKey != Key.None)
            {
                sourceKey = e.DeadCharProcessedKey;
            }

            var previousKeyEvent = Task.CompletedTask;

            switch (sourceKey)
            {
                case Key.Back:
                    remoteKey = KeyDefinitions.BackSpace;
                    break;
                case Key.Tab:
                    remoteKey = KeyDefinitions.Tab;
                    preventBubbling = true;
                    break;
                case Key.Enter:
                    remoteKey = KeyDefinitions.Enter;
                    break;
                case Key.Pause:
                    remoteKey = KeyDefinitions.Pause;
                    break;
                case Key.Scroll:
                    remoteKey = KeyDefinitions.ScrollLock;
                    break;
                case Key.Escape:
                    remoteKey = KeyDefinitions.Escape;
                    break;
                case Key.Space:
                    remoteKey = KeyDefinitions.Space;
                    break;
                case Key.PageUp:
                    remoteKey = KeyDefinitions.PageUp;
                    break;
                case Key.PageDown:
                    remoteKey = KeyDefinitions.PageDown;
                    break;
                case Key.End:
                    remoteKey = KeyDefinitions.End;
                    break;
                case Key.Home:
                    remoteKey = KeyDefinitions.Home;
                    break;
                case Key.Left:
                    remoteKey = KeyDefinitions.Left;
                    break;
                case Key.Up:
                    remoteKey = KeyDefinitions.Up;
                    break;
                case Key.Right:
                    remoteKey = KeyDefinitions.Right;
                    break;
                case Key.Down:
                    remoteKey = KeyDefinitions.Down;
                    break;
                case Key.Insert:
                    remoteKey = KeyDefinitions.Insert;
                    break;
                case Key.Delete:
                    remoteKey = KeyDefinitions.Delete;
                    break;
                case Key.LWin:
                    remoteKey = KeyDefinitions.LeftMeta;
                    preventBubbling = true;
                    break;
                case Key.RWin:
                    remoteKey = KeyDefinitions.RightMeta;
                    preventBubbling = true;
                    break;
                case Key.LeftShift:
                    remoteKey = KeyDefinitions.LeftShift;
                    break;
                case Key.RightShift:
                    remoteKey = KeyDefinitions.RightShift;
                    break;
                case Key.LeftCtrl:
                    remoteKey = KeyDefinitions.LeftControl;
                    break;
                case Key.RightCtrl:
                    remoteKey = KeyDefinitions.RightControl;
                    break;
                case Key.LeftAlt:
                    remoteKey = KeyDefinitions.LeftAlt;
                    preventBubbling = true;
                    break;
                case Key.RightAlt:
                    remoteKey = KeyDefinitions.RightAlt;
                    preventBubbling = true;
                    break;
                case Key.Apps:
                    remoteKey = KeyDefinitions.Menu;
                    preventBubbling = true;
                    break;
                default:
                    if (Key.F1 <= sourceKey && sourceKey <= Key.F20)
                    {
                        remoteKey = (KeyDefinitions)((int)KeyDefinitions.F1 + (sourceKey - Key.F1));
                        break;
                    }

                    var codes = KeyboardHelper.GetCharFromKey(sourceKey);

                    if (codes.Length == 0)
                    {
                        return;
                    }

                    while (codes.Length > 1)
                    {
                        var code = codes[0];

                        previousKeyEvent = Task.Run(() =>
                        {
                            _Client.KeyEvent(true, (KeyDefinitions)code);
                            _Client.KeyEvent(false, (KeyDefinitions)code);
                        });

                        codes = codes[1..];
                    }

                    if (1 <= codes[0] && codes[0] <= 31)
                    {
                        remoteKey = codes[0] + KeyDefinitions.CapitalA - 1;
                        break;
                    }

                    remoteKey = (KeyDefinitions)codes[0];
                    break;
            }

            if (preventBubbling)
            {
                e.Handled = true;
            }

            previousKeyEvent.ContinueWith(_ => _Client.KeyEvent(e.IsDown || e.IsRepeat, remoteKey));
        }

        private void RemoteMouseEvent(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(RemoteFramebuffer);

            var buttons = PointerButtons.None;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                buttons |= PointerButtons.Left;
            }

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                buttons |= PointerButtons.Middle;
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                buttons |= PointerButtons.Right;
            }

            if (e is MouseWheelEventArgs ew)
            {
                if (ew.Delta > 0)
                {
                    _Client.PointerEvent((int)point.X, (int)point.Y, buttons | PointerButtons.ScrollUp);
                }
                else
                {
                    _Client.PointerEvent((int)point.X, (int)point.Y, buttons | PointerButtons.ScrollDown);
                }
            }

            Canvas.SetLeft(RemoteCursor, point.X);
            Canvas.SetTop(RemoteCursor, point.Y);

            if (!RemoteFramebuffer.IsFocused)
            {
                RemoteFramebuffer.Focus();
            }

            e.Handled = true;

            Task.Run(() => _Client.PointerEvent((int)point.X, (int)point.Y, buttons));
        }
        #endregion

        #region VNC Events
        private (nint FramebufferData, int FramebufferSize, int FramebufferStride) CreateFramebufferHandler(int size, int stride)
        {
            (nint FramebufferData, int FramebufferSize, int FramebufferStride)? result = null;

            Dispatcher.Invoke(() =>
            {
                if (_Client.ServerInfo.PixelFormat.TrueColorFlag == 1)
                {
                    _RemoteFramebufferCanvas = new WriteableBitmap(
                        pixelWidth: _Client.ServerInfo.FramebufferWidth,
                        pixelHeight: _Client.ServerInfo.FramebufferHeight,
                        dpiX: 96,
                        dpiY: 96,
                        pixelFormat: PixelFormats.Bgr32,
                        palette: null
                    );

                    RemoteFramebuffer.Source = _RemoteFramebufferCanvas;
                    _RemoteFramebufferLocked = false;
                }

                if (_RemoteFramebufferCanvas is not null)
                {
                    result = (_RemoteFramebufferCanvas.BackBuffer, _RemoteFramebufferCanvas.PixelHeight * _RemoteFramebufferCanvas.BackBufferStride, _RemoteFramebufferCanvas.BackBufferStride);
                }
            });

            try
            {
                return result ?? throw new NotSupportedException();
            }
            catch
            {
                Disconnect_Click(this, new RoutedEventArgs());
                throw;
            }
        }

        private void FramebufferUpdateStartHandler(DateTime updateTime)
        {
            Dispatcher.Invoke(() =>
            {
                _RemoteFramebufferCanvas?.Lock();
                _RemoteFramebufferLocked = true;
            });
        }

        private void FramebufferUpdateEndHandler(IEnumerable<RectangleInfo> rectangles, DateTime dateTime)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var rectangle in rectangles)
                {
                    if (rectangle.Width == 0
                        || rectangle.Height == 0
                        || rectangle.Encoding == VNCEncoding.Cursor
                        || rectangle.Encoding == VNCEncoding.CursorWithAlpha
                        || rectangle.Encoding == VNCEncoding.XCursor)
                    {
                        continue;
                    }

                    _RemoteFramebufferCanvas!.AddDirtyRect(new Int32Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                }

                if (rectangles.Any())
                {
                    LabelEncoding.Content = rectangles.Last().Encoding;
                }

                if (_RemoteFramebufferLocked)
                {
                    _RemoteFramebufferCanvas!.Unlock();
                }
            });
        }

        private void PaletteUpdatedHandler()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var clientPalette = _Client.ColorPalette;

                    var palette = new BitmapPalette([.. clientPalette!.Select(color => new Color() { R = (byte)(color.Red >> 8), G = (byte)(color.Green >> 8), B = (byte)(color.Blue >> 8), A = 0xff })]);

                    _RemoteFramebufferCanvas = new WriteableBitmap(
                        pixelWidth: _Client.ServerInfo.FramebufferWidth,
                        pixelHeight: _Client.ServerInfo.FramebufferHeight,
                        dpiX: 96,
                        dpiY: 96,
                        pixelFormat: PixelFormats.Indexed8,
                        palette: palette
                    );

                    _RemoteFramebufferCanvas.Lock();
                    _RemoteFramebufferLocked = true;

                    _Client.GetFramebuffer(_RemoteFramebufferCanvas.BackBuffer);

                    _RemoteFramebufferCanvas.AddDirtyRect(new Int32Rect(0, 0, _RemoteFramebufferCanvas.PixelWidth, _RemoteFramebufferCanvas.PixelHeight));

                    if (_RemoteFramebufferLocked)
                    {
                        _RemoteFramebufferLocked = false;
                        _RemoteFramebufferCanvas.Unlock();
                    }

                    RemoteFramebuffer.Source = _RemoteFramebufferCanvas;
                });
            }
            catch (Exception)
            {
                Disconnect_Click(this, new RoutedEventArgs());
            }
        }

        private void CursorUpdatedHandler()
        {
            Dispatcher.Invoke(() =>
            {
                if (_Client.Cursor!.PixelData.Length > 0)
                {
                    var cursorBitmap = new WriteableBitmap(_Client.Cursor.Width, _Client.Cursor.Height, 96, 96, PixelFormats.Bgra32, null);
                    cursorBitmap.Lock();
                    Marshal.Copy(_Client.Cursor.PixelData, 0, cursorBitmap.BackBuffer, _Client.Cursor.PixelData.Length);
                    cursorBitmap.AddDirtyRect(new Int32Rect(0, 0, cursorBitmap.PixelWidth, cursorBitmap.PixelHeight));
                    cursorBitmap.Unlock();

                    RemoteCursor.Source = cursorBitmap;
                    RemoteCursor.RenderTransform = new TranslateTransform(-_Client.Cursor.HotspotX, -_Client.Cursor.HotspotY);
                }
                else
                {
                    RemoteCursor.Source = null;
                }
            });
        }

        private void BellHandler()
        {
            SystemSounds.Beep.Play();
        }

        private void ServerNameChangedHandler()
        {
            Dispatcher.Invoke(() => Title = $"{_OriginalWindowTitle} - {_Client.ServerInfo.Name}");
        }

        private void DisconnectedHandler()
        {
            NotifyPropertyChanged(nameof(Connected));

            Dispatcher.Invoke(() =>
            {
                RemoteFramebuffer.Source = null;
                RemoteCursor.Source = null;

                _RemoteFramebufferCanvas = null;

                SizeToContent = SizeToContent.WidthAndHeight;

                Title = _OriginalWindowTitle;
            });
        }
        #endregion
        #endregion
    }
}
