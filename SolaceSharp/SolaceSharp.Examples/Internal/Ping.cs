using ProtoBuf;

namespace SolaceSharp.Examples
{

    [ProtoContract]
    public class Ping
    {
        [ProtoMember(1)]
        public string Message { get; set; } = "";
    }
}
