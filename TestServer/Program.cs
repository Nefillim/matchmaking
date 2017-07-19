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

		private enum MessageType : int
		{
			JOIN_ROOM,
			START_GAME,
			LEAVE_ROOM,
			CREATE_ROOM,
			JOIN_MM,
			LOGIN,
		}

		public static void Main(string[] args)
		{
			_players = new ConcurrentDictionary<string, TcpClient>();
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
				byte[] b = Encoding.UTF8.GetBytes(ip);
			}));
			

			server.AddHandler((int)MessageType.JOIN_ROOM, (token, stream, client) => Task.Run(() =>
		   {
			   string roomId = stream.ReadString();
			   foreach (Room _room in rooms)
			   {
				   if (_room.id == roomId)
				   {
					   _room.sub = client;
					   byte[] newIp = Encoding.UTF8.GetBytes(_room.Ip);
					   server.Send(new Packet((int)MessageType.START_GAME, token, newIp), _room.sub);
					   server.Send(new Packet((int)MessageType.START_GAME, token, newIp), _room.creator);
				   }
			   }
			   
		   }));

			server.AddHandler((int)MessageType.JOIN_MM, (token, stream, client) => Task.Run(() =>
			{
				string str = "";
				foreach (Room _room in rooms)
				{
					str = str + "," + _room.id;
				}
				byte[] buffer = Encoding.UTF8.GetBytes(str);
				server.Send(new Packet((int)MessageType.JOIN_MM, token, buffer), client);
			}));

			server.AddHandler((int)MessageType.START_GAME, (token, stream, client) => Task.Run(() =>
		   {
			   Room room = new Room();
			   foreach (Room _room in rooms)
			   {
				   if (_room.creator == client)
				   {
					   _room.crReady = true;
				   }
				   if (_room.sub == client)
				   {
					   _room.subReady = true;
				   }
				   if (_room.crReady == true && _room.subReady == true)
				   {
					   byte[] Ip = Encoding.UTF8.GetBytes(_room.Ip);
					   server.Send(new Packet(1, token, Ip), _room.creator);
					   server.Send(new Packet(1, token, Ip), _room.sub);
				   }
				   room = _room;
			   }
			   rooms.Remove(room);
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
