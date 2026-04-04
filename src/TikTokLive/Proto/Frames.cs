using System.Collections.Generic;
using ProtoBuf;

namespace TikTokLive.Proto
{
    [ProtoContract]
    public class WebcastPushFrame
    {
        [ProtoMember(1)] public long SeqId { get; set; }
        [ProtoMember(2)] public long LogId { get; set; }
        [ProtoMember(3)] public long Service { get; set; }
        [ProtoMember(4)] public long Method { get; set; }
        [ProtoMember(5)] public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        [ProtoMember(6)] public string PayloadEncoding { get; set; } = "";
        [ProtoMember(7)] public string PayloadType { get; set; } = "";
        [ProtoMember(8)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class HeartbeatMessage
    {
        [ProtoMember(1)] public ulong RoomId { get; set; }
    }

    [ProtoContract]
    public class WebcastImEnterRoomMessage
    {
        [ProtoMember(1)] public long RoomId { get; set; }
        [ProtoMember(2)] public string RoomTag { get; set; } = "";
        [ProtoMember(3)] public string LiveRegion { get; set; } = "";
        [ProtoMember(4)] public long LiveId { get; set; }
        [ProtoMember(5)] public string Identity { get; set; } = "";
        [ProtoMember(6)] public string Cursor { get; set; } = "";
        [ProtoMember(7)] public long AccountType { get; set; }
        [ProtoMember(8)] public long EnterUniqueId { get; set; }
        [ProtoMember(9)] public string FilterWelcomeMsg { get; set; } = "";
        [ProtoMember(10)] public bool IsAnchorContinueKeepMsg { get; set; }
    }
}
