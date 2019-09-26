using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniVNCClient.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			var client = new Client();
			client.Password = "testvnc";

			if (client.Connect("192.168.56.104", 5900))
			{
				client.FrameBufferUpdated += (sender, e) =>
				{
					Task.Run(() =>
					{
						var encodingStartTime = DateTime.Now;

						using (var file = File.Open($@"Images\{e.UpdateTime:HH_mm_ss.fff}.png", FileMode.Create, FileAccess.Write, FileShare.Read))
						using (var image = new ImageMagick.MagickImage(e.CurrentFrameBuffer, new ImageMagick.PixelReadSettings(client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight, ImageMagick.StorageType.Char, "BGRA")))
						{
							image.Format = ImageMagick.MagickFormat.Png32;

							image.Settings.SetDefine(ImageMagick.MagickFormat.Png, "compression-level", "2");
							image.Settings.SetDefine(ImageMagick.MagickFormat.Png, "compression-filter", "2");

							image.Write(file);

							var encodingFinishTime = DateTime.Now;
							Trace.TraceInformation($"Image encoding lasted {(encodingFinishTime - encodingStartTime).TotalSeconds} seconds, image size {file.Length:N} bytes, total time {(encodingFinishTime - e.UpdateTime).TotalSeconds} seconds at {encodingFinishTime:dd/MM/yyyy HH:mm:ss.fff}");
						}
					});
				};

				client.ServerCutText += (sender, e) => { System.Console.WriteLine(e.Text); };

				client.Bell += (sender, e) => { System.Media.SystemSounds.Beep.Play(); };

				client.FramebufferUpdateRequest(false, 0, 0, client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight);

				var updateInterval = TimeSpan.FromSeconds(1);

				Task.Run(() =>
				{
					Task.Delay(updateInterval).Wait();

					while (true)
					{
						var updateTime = DateTime.Now;
						client.FramebufferUpdateRequest(true, 0, 0, client.SessionInfo.FrameBufferWidth, client.SessionInfo.FrameBufferHeight);
						Task.Delay(TimeSpan.FromSeconds(Math.Max(updateInterval.TotalSeconds - (DateTime.Now - updateTime).TotalSeconds, 0))).Wait();
					}
				});


				Task.Delay(TimeSpan.FromMinutes(10)).Wait();
			}
		}
	}
}
