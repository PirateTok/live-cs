using System.Collections.Generic;
using ProtoBuf;

namespace TikTokLive.Proto
{
    [ProtoContract]
    public class WebcastResponse
    {
        [ProtoMember(1)] public List<WebcastMessage> Messages { get; set; } = new List<WebcastMessage>();
        [ProtoMember(2)] public string Cursor { get; set; } = "";
        [ProtoMember(3)] public long FetchInterval { get; set; }
        [ProtoMember(4)] public long Now { get; set; }
        [ProtoMember(5)] public string InternalExt { get; set; } = "";
        [ProtoMember(6)] public int FetchType { get; set; }
        [ProtoMember(7)] public Dictionary<string, string> RouteParamsMap { get; set; } = new Dictionary<string, string>();
        [ProtoMember(8)] public int HeartBeatDuration { get; set; }
        [ProtoMember(9)] public bool NeedsAck { get; set; }
        [ProtoMember(10)] public string PushServer { get; set; } = "";
        [ProtoMember(11)] public bool IsFirst { get; set; }
        [ProtoMember(12)] public string HistoryCommentCursor { get; set; } = "";
        [ProtoMember(13)] public bool HistoryNoMore { get; set; }
    }

    [ProtoContract]
    public class WebcastMessage
    {
        [ProtoMember(1)] public string Type { get; set; } = "";
        [ProtoMember(2)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(3)] public long MsgId { get; set; }
        [ProtoMember(4)] public int MsgType { get; set; }
        [ProtoMember(5)] public long Offset { get; set; }
        [ProtoMember(6)] public bool IsHistory { get; set; }
    }

    [ProtoContract]
    public class CommonMessageData
    {
        [ProtoMember(1)] public string Method { get; set; } = "";
        [ProtoMember(2)] public long MsgId { get; set; }
        [ProtoMember(3)] public long RoomId { get; set; }
        [ProtoMember(4)] public long CreateTime { get; set; }
        [ProtoMember(12)] public string LogId { get; set; } = "";
    }

    [ProtoContract]
    public class Image
    {
        [ProtoMember(1)] public List<string> UrlList { get; set; } = new List<string>();
    }

    [ProtoContract]
    public class FollowInfo
    {
        [ProtoMember(1)] public long FollowingCount { get; set; }
        [ProtoMember(2)] public long FollowerCount { get; set; }
        [ProtoMember(3)] public long FollowStatus { get; set; }
    }

    [ProtoContract]
    public class PayGrade
    {
        [ProtoMember(3)] public string Name { get; set; } = "";
        [ProtoMember(6)] public long Level { get; set; }
        [ProtoMember(25)] public long Score { get; set; }
    }

    [ProtoContract]
    public class PrivilegeLogExtra
    {
        [ProtoMember(1)] public string DataVersion { get; set; } = "";
        [ProtoMember(2)] public string PrivilegeId { get; set; } = "";
        [ProtoMember(5)] public string Level { get; set; } = "";
    }

    [ProtoContract]
    public class BadgeImage
    {
        [ProtoMember(2)] public Image? Image { get; set; }
    }

    [ProtoContract]
    public class BadgeText
    {
        [ProtoMember(2)] public string Key { get; set; } = "";
        [ProtoMember(3)] public string DefaultPattern { get; set; } = "";
    }

    [ProtoContract]
    public class BadgeString
    {
        [ProtoMember(2)] public string ContentStr { get; set; } = "";
    }

    /// badge_scene: ADMIN=1, SUBSCRIBER=4, RANK_LIST=6, USER_GRADE=8, FANS=10
    [ProtoContract]
    public class BadgeStruct
    {
        [ProtoMember(1)] public int DisplayType { get; set; }
        [ProtoMember(3)] public int BadgeScene { get; set; }
        [ProtoMember(11)] public bool Display { get; set; }
        [ProtoMember(12)] public PrivilegeLogExtra? LogExtra { get; set; }
        [ProtoMember(20)] public BadgeImage? ImageBadge { get; set; }
        [ProtoMember(21)] public BadgeText? TextBadge { get; set; }
        [ProtoMember(22)] public BadgeString? StringBadge { get; set; }
    }

    [ProtoContract]
    public class FansClubData
    {
        [ProtoMember(1)] public string ClubName { get; set; } = "";
        [ProtoMember(2)] public int Level { get; set; }
    }

    [ProtoContract]
    public class FansClubMember
    {
        [ProtoMember(1)] public FansClubData? Data { get; set; }
    }

    [ProtoContract]
    public class UserAttr
    {
        [ProtoMember(1)] public bool IsMuted { get; set; }
        [ProtoMember(2)] public bool IsAdmin { get; set; }
        [ProtoMember(3)] public bool IsSuperAdmin { get; set; }
        [ProtoMember(4)] public long MuteDuration { get; set; }
    }

    [ProtoContract]
    public class AuthenticationInfo
    {
        [ProtoMember(1)] public string CustomVerify { get; set; } = "";
        [ProtoMember(2)] public string EnterpriseVerifyReason { get; set; } = "";
    }

    [ProtoContract]
    public class SubscribeInfo
    {
        [ProtoMember(2)] public bool IsSubscribe { get; set; }
        [ProtoMember(5)] public long SubscriberCount { get; set; }
    }

    [ProtoContract]
    public class FansClubInfo
    {
        [ProtoMember(2)] public long FansLevel { get; set; }
        [ProtoMember(3)] public long FansScore { get; set; }
        [ProtoMember(5)] public long FansCount { get; set; }
        [ProtoMember(6)] public string FansClubName { get; set; } = "";
    }

    [ProtoContract]
    public class UserIdentity
    {
        [ProtoMember(1)] public long UserId { get; set; }
        [ProtoMember(3)] public string Nickname { get; set; } = "";
        [ProtoMember(5)] public string BioDescription { get; set; } = "";
        [ProtoMember(9)] public Image? AvatarThumb { get; set; }
        [ProtoMember(10)] public Image? AvatarMedium { get; set; }
        [ProtoMember(11)] public Image? AvatarLarge { get; set; }
        [ProtoMember(12)] public bool Verified { get; set; }
        [ProtoMember(16)] public long CreateTime { get; set; }
        [ProtoMember(17)] public long ModifyTime { get; set; }
        [ProtoMember(22)] public FollowInfo? FollowInfo { get; set; }
        [ProtoMember(23)] public PayGrade? PayGrade { get; set; }
        [ProtoMember(24)] public FansClubMember? FansClub { get; set; }
        [ProtoMember(31)] public int TopVipNo { get; set; }
        [ProtoMember(32)] public UserAttr? UserAttr { get; set; }
        [ProtoMember(34)] public long PayScore { get; set; }
        [ProtoMember(35)] public long FanTicketCount { get; set; }
        [ProtoMember(38)] public string UniqueId { get; set; } = "";
        [ProtoMember(39)] public bool WithCommerce { get; set; }
        [ProtoMember(46)] public string DisplayId { get; set; } = "";
        [ProtoMember(53)] public AuthenticationInfo? AuthenticationInfo { get; set; }
        [ProtoMember(63)] public SubscribeInfo? SubscribeInfo { get; set; }
        [ProtoMember(64)] public List<BadgeStruct> BadgeList { get; set; } = new();
        [ProtoMember(66)] public FansClubInfo? FansClubInfo { get; set; }
        [ProtoMember(1002)] public bool AllowFindByContacts { get; set; }
        [ProtoMember(1018)] public string Constellation { get; set; } = "";
        [ProtoMember(1024)] public long FollowStatus { get; set; }
        [ProtoMember(1029)] public bool IsFollower { get; set; }
        [ProtoMember(1030)] public bool IsFollowing { get; set; }
        [ProtoMember(1043)] public string VerifiedReason { get; set; } = "";
        [ProtoMember(1090)] public bool IsSubscribe { get; set; }
    }

    [ProtoContract]
    public class UserIdentityContext
    {
        [ProtoMember(1)] public bool IsGiftGiverOfAnchor { get; set; }
        [ProtoMember(2)] public bool IsSubscriberOfAnchor { get; set; }
        [ProtoMember(3)] public bool IsMutualFollowingWithAnchor { get; set; }
        [ProtoMember(4)] public bool IsFollowerOfAnchor { get; set; }
        [ProtoMember(5)] public bool IsModeratorOfAnchor { get; set; }
        [ProtoMember(6)] public bool IsAnchor { get; set; }
    }
}
