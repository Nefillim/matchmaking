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

namespace TestServer
{

	class Program
	{
		private static ConcurrentDictionary<string, TcpClient> _players;

		private enum MessageType: int
		{
			TEST,
			START_GAME,
			LEAVE_GAME,
			CREATE_ROOM,
			GAME_FOUND,
			JOIN_MM,
			DO_WHATEVER_YOU_WANT,
		}

		public static void Main(string[] args)
		{
			_players = new ConcurrentDictionary<string, TcpClient>();
			var server = new Server(8001);
			server.AddHandler((int) MessageType.TEST, (token, stream, client) => Task.Run(() =>
			{
				var msgText = stream.ReadString();
				Console.WriteLine(msgText);
				byte[] buffer;
				using (var m = new MemoryStream())
				{
					using (var writer = new BinaryWriter(m))
					{
						writer.Write($"{msgText}\t OK");
					}
					buffer = m.ToArray();
				}
				server.Send(new Packet((int) MessageType.TEST, token, buffer), client);
			}));

			server.AddHandler((int) MessageType.START_GAME, (token, stream, client) => Task.Run(() =>
			{

				_players.AddOrUpdate(token, client, (t, old) => client);
				//server response  is not really needed, but why not!?
				//you may not add it in your program
				server.Send(new Packet(1, token, new byte[] {1}), client);
			}));

			server.AddHandler((int) MessageType.LEAVE_GAME, (token, stream, client) => Task.Run(() =>
			{
				TcpClient remove;
				_players.TryRemove(token, out remove);

				//it's not perfect and would cause (handled) exceptions.. in nerly future i ll fix it
				remove.Close();

				server.Send(new Packet(1, token, new byte[] {1}), client);
			}));

			server.AddHandler((int) MessageType.JOIN_MM, (token, stream, client) => Task.Run(() =>
			{
				_players.AddOrUpdate(token, client, (t, old) => client);
				/*
				esli v liste 2 clienta perenosit ih v novii list i chistit etot
				sozdaet http clienta ot nego otpravlyaet json key body poluchaet ip v json
				parse json (string)ip->byte[]
				posilae, ip clientam v novom spiske 
				*/
				if (_players.Count >= 2)
				{
					HttpClient mmClient = new HttpClient();
					var url = "http://104.199.106.137:8000/start_game/";
					var message = new { key = "12345", body = "give me ip" };
					var content = new HttpContent(message.ToString(), Encoding.UTF8, "application/json")
					var response = mmClient.PostAsync(url, content);
					var responseString = await response.Content.ReadAsStringAsync();
					List<string> Room = new List<string>();
					byte[] buffer;
					using (var m = new MemoryStream())
					{
						using (var writer = new BinaryWriter(m))
						{
							writer.Write(responseString);
						}
						buffer = m.ToArray();
					}
					foreach (string t in _players.Keys)
					{
						Room.Add(t);
						server.Send(new Packet(5, token, buffer), _players[t]);
					}
					foreach (var player in Room)
					{
						if (_players.ContainsKey(player))
						{
							TcpClient remove;
							_players.TryRemove(player, out remove);
							remove.Close();
						}
					}
				}
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