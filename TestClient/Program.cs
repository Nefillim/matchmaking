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
        public static void Main(string[] args) {
            var tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, int.Parse(args[0])));
            tcpClient.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8001));
            bool gameFound = false;
            string ip;
            using (var stream = tcpClient.GetStream())
            using (var reader = new BinaryReader(stream)) {
                while (!gameFound) {
                    byte[] buffer;
                    using (var m = new MemoryStream()) {
                        using (var writer = new BinaryWriter(m)) {
                            writer.Write(Console.ReadLine() ?? string.Empty);
                        }
                        buffer = m.ToArray();
                    }
                    tcpClient.Client.Send(new Packet(1, "AGDFFE23423dsdf", buffer)
                        .Serialize());
                    while (tcpClient.Available == 0) ;
                    reader.ReadInt32();
                    if (reader.ReadInt32() == 5)
                    {
                        reader.ReadString();
                        Console.WriteLine(reader.ReadString());
                        gameFound = true;
                    }
                }
            }
        }
    }
}