using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace TestClient
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			int port;
			string sPort = Console.ReadLine();
			port = int.Parse(sPort);
			var tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, port));
			tcpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001));
			bool gameFound = false;
			string token = Console.ReadLine();
			tcpClient.Client.Send(new Packet(5, token, null).Serialize());

			using (var stream = tcpClient.GetStream())
			using (var reader = new BinaryReader(stream))
			{
				while (!gameFound)
				{
					while (tcpClient.Available == 0) ;
					var id = reader.ReadInt32();
					reader.ReadString();
					Console.WriteLine(reader.ReadString());
					gameFound = true;
				}
			}
			Console.ReadKey();
		}
	}
}