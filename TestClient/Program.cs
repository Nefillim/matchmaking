
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
			





			
			string roomList;
			using (var stream = tcpClient.GetStream())
			using (var reader = new BinaryReader(stream))
			{
				tcpClient.Client.Send(new Packet((int)MessageType.LOGIN, token, null).Serialize());
				tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());/*
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
						Console.WriteLine("Connected to " + ip);
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
				*/

				while (!gameFound)
				{
					while (tcpClient.Available == 0) ;
					var id = reader.ReadInt32();
					Console.WriteLine("last message id:" + id);
					//_handlers[id].Invoke();
					if (id == (int)MessageType.JOIN_MM)
					{
						reader.ReadString();						
						//roomList = Encoding.UTF8.GetString(reader.ReadBytes(11));
						//Console.WriteLine(roomList);
						command = Console.ReadLine();
						if (command == "create")
						{							
							tcpClient.Client.Send(new Packet((int)MessageType.CREATE_ROOM, token, null).Serialize());
							Console.WriteLine("Trying to create room.");
						}
						else
						{
							int Id = Convert.ToInt32(command);
							byte[] roomId = BitConverter.GetBytes(Id);
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
							Console.WriteLine("Connected to " + ip);
							string step = Console.ReadLine();
							if (step == "play")
							{
								tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());
							}
							else { tcpClient.Client.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null).Serialize()); }
							tcpClient.Connect(new IPEndPoint(IPAddress.Parse(ip), port));							
						}
						else {
							tcpClient.Client.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null).Serialize());
						}						
					}
					if (id == (int)MessageType.LEAVE_ROOM)
					{
						reader.ReadString();
						if (creator)
						{
							Console.WriteLine("Enother player left the room wait please.");
						}
						else {
							Console.WriteLine("Host left he romm? chose enother one please.");
							tcpClient.Client.Send(new Packet((int)MessageType.JOIN_MM, token, null).Serialize());
						}
					}
					if (id == (int)MessageType.CREATE_ROOM)
					{
						reader.ReadString();						
						int roomId = reader.ReadInt32();
						Console.WriteLine("You have created the room, wait for enother player join your room.");

					}
					if (id > 5) {
						while (tcpClient.Available > 0) { }
						Console.WriteLine("Error wrong type of message. Recconecting to the MM...");
						tcpClient.Client.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null).Serialize());
					}
				}
			}
			//Console.ReadKey();
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