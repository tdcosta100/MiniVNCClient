using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
					Dispatcher.Invoke(new Action(() =>
					{
						stackPanelConnectionDetails.Visibility = Visibility.Collapsed;

						_WriteableBitmap = new WriteableBitmap(_Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight, 96, 96, PixelFormats.Bgr32, null);

						remoteFrameBuffer.Width = _WriteableBitmap.Width;
						remoteFrameBuffer.Height = _WriteableBitmap.Height;

						remoteFrameBuffer.Source = _WriteableBitmap;
					}));

					_Client.FrameBufferUpdated += Client_FrameBufferUpdated;

					_Client.FramebufferUpdateRequest(false, 0, 0, _Client.SessionInfo.FrameBufferWidth, _Client.SessionInfo.FrameBufferHeight);

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
				var stride = _Client.FrameBufferStateStride;

				_WriteableBitmap.Lock();

				foreach (var area in e.UpdatedAreas)
				{
					_WriteableBitmap.WritePixels(area, e.CurrentFrameBuffer, stride, area.X, area.Y);

					_WriteableBitmap.AddDirtyRect(area);
				}

				_WriteableBitmap.Unlock();
			}));
		}
	}
}
