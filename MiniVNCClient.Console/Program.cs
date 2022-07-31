using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.ConsoleExample
{
	class Program
	{
        private class MockStream : FileStream
        {
            public MockStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
            {

            }
			
			public override void Write(byte[] buffer, int offset, int count)
            {
                
            }

            public override void WriteByte(byte value)
            {
                
            }
        }

		static void Main(string[] args)
		{
			double waitTime = 60;
			var client = new Client();

			if (args.Length >= 2 && args[0] == "simulationMode")
			{
				var filename = args[1];

				var stream = new MockStream(filename);

				if (args.Length >= 3 && !string.IsNullOrEmpty(args[2]))
				{
					client.Password = args[2];
				}

				if (args.Length >= 4 && double.TryParse(args[3], out waitTime))
				{

				}

				client.Connect(stream);
			}
			else
			{
				Console.Write("Host IP/Name: ");

				var host = Console.ReadLine();

				Console.Write("Host Port (or press enter for default 5900): ");

				int port;
				var userPort = Console.ReadLine();

				if (!int.TryParse(userPort, out port))
				{
					port = 5900;
				}

				Console.Write("VNC Password (or press enter to connect without a password): ");

				var stringBuilder = new StringBuilder();

				while (true)
				{
					var character = Console.ReadKey(true);
					if (character.Key == ConsoleKey.Enter)
					{
						Console.WriteLine();
						break;
					}

					if (character.Key == ConsoleKey.Backspace)
					{
						if (stringBuilder.Length > 0)
						{
							Console.Write("\b\0\b");
							stringBuilder.Length--;
						}

						continue;
					}

					Console.Write('*');
					stringBuilder.Append(character.KeyChar);
				}

				var password = stringBuilder.ToString();


				if (!string.IsNullOrEmpty(password))
				{
					client.Password = password;
				}

				if (!client.Connect(host, port))
				{
					return;
				}
			}

			if (!Directory.Exists("Images"))
			{
				Directory.CreateDirectory("Images");
			}

			var frameBuffer = new byte[client.SessionInfo.FrameBufferWidth * client.SessionInfo.FrameBufferHeight * client.SessionInfo.PixelFormat.BytesPerPixel];

			client.FrameBufferUpdated += (sender, e) =>
			{
				Task.Run(() =>
				{
					var encodingStartTime = DateTime.Now;

					client.GetFrameBuffer(frameBuffer, 0);

					using (var file = File.Open($@"Images\{e.UpdateTime:HH_mm_ss.fff}.png", FileMode.Create, FileAccess.Write, FileShare.Read))
					using (var image = new ImageMagick.MagickImage(frameBuffer, new ImageMagick.PixelReadSettings(client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight, ImageMagick.StorageType.Char, "BGRA")))
					{
						image.Format = ImageMagick.MagickFormat.Png32;

						image.Alpha(ImageMagick.AlphaOption.Off);
						image.Alpha(ImageMagick.AlphaOption.Remove);

						image.Settings.SetDefine(ImageMagick.MagickFormat.Png, "compression-level", "2");
						image.Settings.SetDefine(ImageMagick.MagickFormat.Png, "compression-filter", "2");

						image.Write(file);

						var encodingFinishTime = DateTime.Now;
						Trace.TraceInformation($"Image encoding lasted {(encodingFinishTime - encodingStartTime).TotalSeconds} seconds, image size {file.Length:N} bytes, total time {(encodingFinishTime - e.UpdateTime).TotalSeconds} seconds at {encodingFinishTime:dd/MM/yyyy HH:mm:ss.fff}");
					}
				});
			};

			client.ServerCutText += (sender, e) => { Console.WriteLine(e.Text); };

			client.Bell += (sender, e) => { System.Media.SystemSounds.Beep.Play(); };

			client.FramebufferUpdateRequest(false, 0, 0, client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight);

			var updateInterval = TimeSpan.FromSeconds(1);

			Task.Run(() =>
			{
				Task.Delay(updateInterval).Wait();

				while (client.Connected)
				{
					var updateTime = DateTime.Now;
					client.FramebufferUpdateRequest(true, 0, 0, client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight);
					Task.Delay(TimeSpan.FromSeconds(Math.Max(updateInterval.TotalSeconds - (DateTime.Now - updateTime).TotalSeconds, 0))).Wait();
				}
			});

			Task.Delay(TimeSpan.FromSeconds(waitTime)).Wait();

			client.Close();
		}
	}
}
