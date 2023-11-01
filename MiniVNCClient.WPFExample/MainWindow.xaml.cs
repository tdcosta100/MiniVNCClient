using System;

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace MiniVNCClient.WPFExample
{
	/// <summary>
	/// Interação lógica para MainWindow.xam
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		string _Host;
		int _Port = 5900;
		string _Password;

		Client _Client = new Client();

		WriteableBitmap _WriteableBitmap;

		public event PropertyChangedEventHandler PropertyChanged;

		public string Host
		{
			get => _Host;
			set
			{
				_Host = value;
				OnPropertyChanged();
			}
		}

		public int Port
		{
			get => _Port;
			set
			{
				_Port = value;
				OnPropertyChanged();
			}
		}

		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnPropertyChanged([CallerMemberName] string property = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		private void TextBoxPassword_PasswordChanged(object sender, RoutedEventArgs e)
		{
			var passwordBox = (PasswordBox)sender;

			_Password = passwordBox.Password;
		}

		private void ButtonConnect_Click(object sender, RoutedEventArgs e)
		{
			labelStatus.Content = "Connecting...";

			Task.Run(() =>
			{
				_Client.Password = _Password;

				if (_Client.Connect(_Host, _Port))
				{
					_Client.FrameBufferUpdated += Client_FrameBufferUpdated;

					_Client.FramebufferUpdateRequest(false, 0, 0, _Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight);

					Dispatcher.Invoke(new Action(() =>
					{
						stackPanelConnectionDetails.Visibility = Visibility.Collapsed;

						if (_Client.SessionInfo.PixelFormat.TrueColorFlag != 0x00)
						{
							_WriteableBitmap = new WriteableBitmap(_Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight, 96, 96, PixelFormats.Bgr32, null);
						}
						else
						{
							do
							{
								Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
							} while (_Client.ColorPalette == null);

							_WriteableBitmap = new WriteableBitmap(_Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_Client.ColorPalette.Select( x => Color.FromRgb(x.R, x.G, x.B)).ToArray()));
						}

						remoteFrameBuffer.Width = _WriteableBitmap.Width;
						remoteFrameBuffer.Height = _WriteableBitmap.Height;

						remoteFrameBuffer.Source = _WriteableBitmap;
					}));

					Task.Run(() =>
					{
						while (true)
						{
							_Client.FramebufferUpdateRequest(true, 0, 0, _Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight);

							Task.Delay(TimeSpan.FromSeconds(1 / 30.0)).Wait();
						}
					});
				}
				else
				{
					Dispatcher.Invoke(new Action(() => labelStatus.Content = "Connection failed"));
				}
			});
		}

		private void Client_FrameBufferUpdated(object sender, Events.FrameBufferUpdatedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				_WriteableBitmap.Lock();

				_Client.GetFrameBuffer(_WriteableBitmap.BackBuffer);

				foreach (var area in e.UpdatedAreas)
				{
					_WriteableBitmap.AddDirtyRect(new Int32Rect()
					{
						X = area.X,
						Y = area.Y,
						Width = area.Width,
						Height = area.Height
					});
				}

				_WriteableBitmap.Unlock();
			}));
		}
	}
}
