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

namespace TestServer
{

	class Program
	{
		private static ConcurrentDictionary<string, TcpClient> _players;
		private static ConcurrentDictionary<TcpClient, string> _tokens;

		private enum MessageType : int
		{
			JOIN_ROOM,
			START_GAME,
			LEAVE_ROOM,
			CREATE_ROOM,
			JOIN_MM,
			LOGIN,
			LEAVE_GAME
		}

		public static void Main(string[] args)
		{
			_players = new ConcurrentDictionary<string, TcpClient>();
			_tokens = new ConcurrentDictionary<TcpClient, string>();
			List<Room> rooms = new List<Room>();
			int roomCount = 1;
			var server = new Server(8001);
			server.AddHandler((int)MessageType.CREATE_ROOM, (token, stream, client) => Task.Run(() =>
			{
				var url = "http://104.199.106.137:8000/start_game/";
				var message = "{ \"key\" : \"12345\", \"body\" : \"give me ip\" }";
				byte[] buf = Encoding.UTF8.GetBytes(message);
				byte[] resp;
				using (WebClient wc = new WebClient())
				{

					wc.Headers.Add(HttpRequestHeader.Accept, "text / html; q = 1.0, */*");
					wc.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
					wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU");
					wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
					wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.109 Safari/537.36");
					resp = wc.UploadData(url, "POST", buf);
				}
				string str = Encoding.UTF8.GetString(resp);
				Regex re = new Regex(@"[\d\.]+:\d+");
				var ip = re.Matches(str)[0].Value;
				Room newOne = new Room(roomCount, client, ip);
				rooms.Add(newOne);
				byte[] b = BitConverter.GetBytes(roomCount);
				Console.WriteLine("created room with id " + roomCount);
				roomCount++;
				server.Send(new Packet((int)MessageType.CREATE_ROOM, token, b), client);
			}));
			

			server.AddHandler((int)MessageType.JOIN_ROOM, (token, stream, client) => Task.Run(() =>
		   {
			   Console.WriteLine("player joined the room");
			   int i = stream.ReadInt32();
			   Console.WriteLine(i);
			   Room room = new Room();
			   bool disc = false;
			   foreach (Room _room in rooms)
			   {
				   if (_room.id == i && _room.sub == null)
				   {
					   string ttoken = _tokens[_room.creator];
					   _room.sub = client;
					   byte[] newIp = Encoding.UTF8.GetBytes(_room.Ip);
					   if (_players[token].Connected)
					   {
						   server.Send(new Packet((int)MessageType.START_GAME, token, newIp), _room.sub);
						   Console.WriteLine("ip sent to sub");
					   }
					   else {
						   server.Send(new Packet((int)MessageType.LEAVE_ROOM, ttoken, null), _room.creator);
						   room = _room;
						   _room.sub = null;
					   }					   
					   
					   if (_players[ttoken].Connected)
					   {
						   server.Send(new Packet((int)MessageType.START_GAME, ttoken, newIp), _room.creator);
						   Console.WriteLine("ip sent to creator");
					   }
					   else
					   {
						   server.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null), _room.sub);
						   room = _room;
						   disc = true;
					   }
				   }
				   if (_room.sub != null) server.Send(new Packet((int)MessageType.JOIN_MM, token, null), client);
			   }
			   if (disc) { rooms.Remove(room);}
		   }));

			server.AddHandler((int)MessageType.JOIN_MM, (token, stream, client) => Task.Run(() =>
			{
				Console.WriteLine("player joined MM");
				string str = "room list: ";
				foreach (Room _room in rooms)
				{
					str = str + _room.id + "; ";					
				}
				Console.WriteLine(str);
				byte[] buffer = Encoding.UTF8.GetBytes(str);
				server.Send(new Packet((int)MessageType.JOIN_MM, token, null), client);
			}));

			server.AddHandler((int)MessageType.START_GAME, (token, stream, client) => Task.Run(() =>
		   {
			   Room room = new Room();
			   foreach (Room _room in rooms)
			   {
				   if (_room.creator == client)
				   {
					   _room.crReady = true;
					   Console.WriteLine("creator is redy in room  " + _room.id);
				   }
				   if (_room.sub == client)
				   {
					   _room.subReady = true;
					   Console.WriteLine("sub is redy in room  " + _room.id);
				   }
				   if (_room.crReady == true && _room.subReady == true)
				   {
					   byte[] Ip = Encoding.UTF8.GetBytes(_room.Ip);
					   server.Send(new Packet(1, token, Ip), _room.creator);
					   server.Send(new Packet(1, token, Ip), _room.sub);
					   Console.WriteLine("Game begun.");
				   }
				   room = _room;
			   }
			   if (room.crReady && room.subReady) {
				   rooms.Remove(room);
			   }
		   }));	


			server.AddHandler((int)MessageType.LEAVE_ROOM, (token, stream, client) => Task.Run(() =>
		   {
			   Room room = new Room();
			   foreach (Room _room in rooms)
			   {
				   if (_room.creator == client)
				   {
					   server.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null), _room.sub);
					   room = _room;
				   }
				   if (_room.sub == client)
				   {
					   server.Send(new Packet((int)MessageType.LEAVE_ROOM, token, null), _room.creator);
				   }
			   }
			   rooms.Remove(room);
		   }));

			server.AddHandler((int)MessageType.LOGIN, (token, stream, client) => Task.Run(() =>
		   {
			   _players.AddOrUpdate(token, client, (t, old) => client);
			   _tokens.AddOrUpdate(client, token, (t, old) => token);
		   }));
			server.AddHandler((int)MessageType.LEAVE_GAME, (token, stream, client) => Task.Run(() =>
			{
				_players.TryRemove(token, out client);
				_tokens.TryRemove(client, out token);
			}));
			server.StartListener().Wait();
			
		}
	}
}
// token - string
// stream - binaryreader   contain id(mess type), token, anything  
// client - tcpClient
// when 2 players in list send to master json 
/*
{
"key": "12345",

}

*/
