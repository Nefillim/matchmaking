using System.IO;

namespace matchmaking
{
    public class Packet
    {
        public int Type { get; }
        public string Token { get; }
        public byte[] Body { get; set; }

        public Packet(int type, string token, byte[] body) {
            Type = type;
            Token = token;
            Body = body;
        }

        public byte[] Serialize() {
            using (var m = new MemoryStream()) {
                using (var writer = new BinaryWriter(m)) {
                    writer.Write(Type);
                    writer.Write(Token);
					if(Body != null)
                    writer.Write(Body);
                }
                return m.ToArray();
            }
        }
    }
}