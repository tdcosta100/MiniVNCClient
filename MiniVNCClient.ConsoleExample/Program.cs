using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using System.Text;

namespace MiniVNCClient.ConsoleExample
{
    internal class Program
    {
        private class MockStream(string path) : FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
        {
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
                    client.Password = Client.EncryptPassword(args[2]);
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
                    client.Password = Client.EncryptPassword(password);
                }

                try
                {
                    client.Connect(host ?? string.Empty, port);
                }
                catch
                {
                    return;
                }
            }

            if (!Directory.Exists("Images"))
            {
                Directory.CreateDirectory("Images");
            }

            var frameBuffer = new byte[client.ServerInfo.FramebufferWidth * client.ServerInfo.FramebufferHeight * client.ServerInfo.PixelFormat.BytesPerPixel];

            client.FramebufferUpdateEnd += (rectangles, updateTime) =>
            {
                Task.Run(() =>
                {
                    var encodingStartTime = DateTime.Now;

                    client.GetFramebuffer(frameBuffer);

                    var file = File.Open($@"Images\{updateTime.ToLocalTime():HH_mm_ss.fff}.png", FileMode.Create, FileAccess.Write, FileShare.Read);
                    var image = Image.LoadPixelData<Bgra32>(frameBuffer, client.ServerInfo.FramebufferWidth, client.ServerInfo.FramebufferHeight);
                    image.SaveAsPng(file, new SixLabors.ImageSharp.Formats.Png.PngEncoder() { ColorType = SixLabors.ImageSharp.Formats.Png.PngColorType.Rgb });
                    
                    var encodingFinishTime = DateTime.Now;
                    Console.WriteLine($"Image encoding lasted {(encodingFinishTime - encodingStartTime).TotalSeconds} seconds, image size {file.Length:N0} bytes, total time {(encodingFinishTime - updateTime.ToLocalTime()).TotalSeconds} seconds at {encodingFinishTime:dd/MM/yyyy HH:mm:ss.fff}");
                });
            };

            client.ServerCutText += Console.WriteLine;

            client.FramebufferUpdateRequest(false, 0, 0, client.ServerInfo.FramebufferWidth, client.ServerInfo.FramebufferHeight);

            var updateInterval = TimeSpan.FromSeconds(1);

            Task.Run(() =>
            {
                Task.Delay(updateInterval).Wait();

                while (client.Connected)
                {
                    var updateTime = DateTime.Now;
                    client.FramebufferUpdateRequest(true, 0, 0, client.ServerInfo.FramebufferWidth, client.ServerInfo.FramebufferHeight);
                    Task.Delay(TimeSpan.FromSeconds(Math.Max(updateInterval.TotalSeconds - (DateTime.Now - updateTime).TotalSeconds, 0))).Wait();
                }
            });

            Task.Delay(TimeSpan.FromSeconds(waitTime)).Wait();

            client.Disconnect();
        }
    }
}
