
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace matchmaking
{
	public class Room
	{
		public int id;
		public TcpClient creator;
		public TcpClient sub;
		public string Ip;
		public bool crReady;
		public bool subReady;
		public Room(int _id, TcpClient _creator, string _Ip)
		{
			id = _id;
			creator = _creator;
			Ip = _Ip;
			crReady = false;
			subReady = false;
		}
		public Room() { }
	}
}
