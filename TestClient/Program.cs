using matchmaking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace TestClient
{
	public static class Program
	{
		private enum MessageType : int
		{
			JOIN_ROOM,
			START_GAME,
			LEAVE_ROOM,
			CREATE_ROOM,
			JOIN_MM,
			LOGIN,

		}

		public delegate void Handler();
		

		public static void Main(string[] args)
		{
			Dictionary<int, Handler>  _handlers = new Dictionary<int, Handler>();
			int port;
			bool creator = true;
			string sPort = Console.ReadLine();			
			port = int.Parse(sPort);
			var tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, port));
			tcpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001));
			bool gameFound = false;
			string token = Console.ReadLine();
			string ip;
			string command;
			





			tcpClient.Client.Send(new Packet((int)MessageType.LOGIN, token, null).Serialize());
			tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());
			string roomList;
			using (var stream = tcpClient.GetStream())
			using (var reader = new BinaryReader(stream))
			{
				Handler join = delegate () {
					reader.ReadString();
					roomList = reader.ReadString();
					Console.WriteLine(roomList);
					command = Console.ReadLine();
					if (command == "create")
					{
						tcpClient.Client.Send(new Packet((int)MessageType.CREATE_ROOM, token, null).Serialize());
						Console.WriteLine("You have created the room, wait for enother player join your room.");

					}
					else
					{
						byte[] roomId = Encoding.UTF8.GetBytes(command);
						tcpClient.Client.Send(new Packet((int)MessageType.JOIN_ROOM, token, roomId).Serialize());
						Console.WriteLine("You joined the room.");
						creator = false;
					}
				};
				_handlers.Add((int)MessageType.JOIN_MM, join);


				join = delegate ()
				{
					reader.ReadString();
					Console.WriteLine("Now you can start the game.");
					command = Console.ReadLine();
					if (command == "start")
					{
						tcpClient.Client.Send(new Packet((int)MessageType.START_GAME, token, null).Serialize());
						ip = reader.ReadString();
						tcpClient.Connect(new IPEndPoint(IPAddress.Parse(ip), 8001));
						Console.WriteLine("Connected to %d", ip);
					}
					else
					{
						tcpClient.Client.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null).Serialize());
					}
				};
				_handlers.Add((int)MessageType.START_GAME, join);



				join = delegate ()
				{
					if (creator)
					{
						Console.WriteLine("Enother player left the room wait please.");
					}
					else
					{
						Console.WriteLine("Host left he romm? chose enother one please.");
						tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());
					}
				};
				_handlers.Add((int)MessageType.LEAVE_ROOM, join);


				while (!gameFound)
				{
					while (tcpClient.Available == 0) ;
					var id = reader.ReadInt32();
					_handlers[id].Invoke();
					/*if (id == (int)MessageType.JOIN_MM)
					{
						reader.ReadString();
						roomList = reader.ReadString();
						Console.WriteLine(roomList);
						command = Console.ReadLine();
						if (command == "create")
						{
							tcpClient.Client.Send(new Packet((int)MessageType.CREATE_ROOM, token, null).Serialize());
							Console.WriteLine("You have created the room, wait for enother player join your room.");

						}
						else
						{
							byte[] roomId = Encoding.UTF8.GetBytes(command);
							tcpClient.Client.Send(new Packet((int)MessageType.JOIN_ROOM, token, roomId).Serialize());
							Console.WriteLine("You joined the room.");
							creator = false;
						}
					}
					if (id == (int)MessageType.START_GAME)
					{
						reader.ReadString();
						Console.WriteLine("Now you can start the game.");
						command = Console.ReadLine();
						if (command == "start")
						{
							tcpClient.Client.Send(new Packet((int)MessageType.START_GAME, token, null).Serialize());
							ip = reader.ReadString();
							tcpClient.Connect(new IPEndPoint(IPAddress.Parse(ip), 8001));
							Console.WriteLine("Connected to %d", ip);
						}
						else {
							tcpClient.Client.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null).Serialize());
						}						
					}
					if (id == (int)MessageType.LEAVE_ROOM)
					{
						if (creator)
						{
							Console.WriteLine("Enother player left the room wait please.");
						}
						else {
							Console.WriteLine("Host left he romm? chose enother one please.");
							tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());
						}
					}*/
				}
			}
			Console.ReadKey();
		}
	}
}


/*
 * create room
 * enter room
 * leave room
 * start game
 * 
 */