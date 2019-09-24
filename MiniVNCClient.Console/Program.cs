using System;
using System.Collections.Generic;
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

			client.Connect("192.168.56.102", 5900);
		}
	}
}
