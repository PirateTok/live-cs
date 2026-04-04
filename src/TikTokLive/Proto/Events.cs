using System.Collections.Generic;
using ProtoBuf;

namespace TikTokLive.Proto
{
    [ProtoContract]
    public class WebcastChatMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public UserIdentity? User { get; set; }
        [ProtoMember(3)] public string Comment { get; set; } = "";
        [ProtoMember(14)] public string ContentLanguage { get; set; } = "";
        [ProtoMember(16)] public int QuickChatScene { get; set; }
        [ProtoMember(17)] public int CommunityflaggedStatus { get; set; }
        [ProtoMember(18)] public UserIdentityContext? UserIdentity { get; set; }
    }

    [ProtoContract]
    public class WebcastLikeMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int LikeCount { get; set; }
        [ProtoMember(3)] public int TotalLikeCount { get; set; }
        [ProtoMember(5)] public UserIdentity? User { get; set; }
        [ProtoMember(9)] public long EffectCnt { get; set; }
        [ProtoMember(12)] public long RoomMessageHeatLevel { get; set; }
    }

    [ProtoContract]
    public class GiftDetails
    {
        [ProtoMember(5)] public long Id { get; set; }
        [ProtoMember(10)] public bool Combo { get; set; }
        [ProtoMember(11)] public int GiftType { get; set; }
        [ProtoMember(12)] public int DiamondCount { get; set; }
        [ProtoMember(16)] public string GiftName { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastGiftMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int GiftId { get; set; }
        [ProtoMember(3)] public long FanTicketCount { get; set; }
        [ProtoMember(4)] public int GroupCount { get; set; }
        [ProtoMember(5)] public int RepeatCount { get; set; }
        [ProtoMember(6)] public int ComboCount { get; set; }
        [ProtoMember(7)] public UserIdentity? User { get; set; }
        [ProtoMember(8)] public UserIdentity? ToUser { get; set; }
        [ProtoMember(9)] public int RepeatEnd { get; set; }
        [ProtoMember(11)] public ulong GroupId { get; set; }
        [ProtoMember(15)] public GiftDetails? GiftDetails { get; set; }
        [ProtoMember(25)] public bool IsFirstSent { get; set; }
        [ProtoMember(32)] public UserIdentityContext? UserIdentity { get; set; }
        [ProtoMember(44)] public bool MultiGenerateMessage { get; set; }

        public bool IsComboGift() => GiftDetails != null && GiftDetails.GiftType == 1;
        public bool IsStreakOver() => !IsComboGift() || RepeatEnd == 1;
        public long DiamondTotal()
        {
            long perGift = GiftDetails != null ? (long)GiftDetails.DiamondCount : 0;
            long count = RepeatCount > 0 ? RepeatCount : 1;
            return perGift * count;
        }
    }

    [ProtoContract]
    public class WebcastMemberMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public byte[] UserBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(3)] public int MemberCount { get; set; }
        [ProtoMember(10)] public int Action { get; set; }
        [ProtoMember(20)] public string EffectDisplayType { get; set; } = "";
        [ProtoMember(21)] public string EffectAction { get; set; } = "";
        [ProtoMember(28)] public long ToastVisible { get; set; }
        [ProtoMember(33)] public long ShowWave { get; set; }
        [ProtoMember(35)] public long EnterEffectTarget { get; set; }
    }

    [ProtoContract]
    public class Contributor
    {
        [ProtoMember(1)] public int CoinCount { get; set; }
        [ProtoMember(2)] public UserIdentity? User { get; set; }
        [ProtoMember(3)] public int Rank { get; set; }
        [ProtoMember(4)] public long Delta { get; set; }
    }

    [ProtoContract]
    public class WebcastRoomUserSeqMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public List<Contributor> RanksList { get; set; } = new List<Contributor>();
        [ProtoMember(3)] public int ViewerCount { get; set; }
        [ProtoMember(4)] public string PopStr { get; set; } = "";
        [ProtoMember(5)] public List<Contributor> SeatsList { get; set; } = new List<Contributor>();
        [ProtoMember(6)] public long Popularity { get; set; }
        [ProtoMember(7)] public int TotalUser { get; set; }
        [ProtoMember(8)] public long Anonymous { get; set; }
    }

    [ProtoContract]
    public class WebcastSocialMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public UserIdentity? User { get; set; }
        [ProtoMember(3)] public long ShareType { get; set; }
        [ProtoMember(4)] public long Action { get; set; }
        [ProtoMember(5)] public string ShareTarget { get; set; } = "";
        [ProtoMember(6)] public int FollowCount { get; set; }
        [ProtoMember(7)] public long ShareDisplayStyle { get; set; }
        [ProtoMember(8)] public int ShareCount { get; set; }
    }

    [ProtoContract]
    public class WebcastControlExtra
    {
        [ProtoMember(2)] public long ReasonNo { get; set; }
        [ProtoMember(8)] public string Source { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastControlMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int Action { get; set; }
        [ProtoMember(3)] public string Tips { get; set; } = "";
        [ProtoMember(4)] public WebcastControlExtra? Extra { get; set; }
        [ProtoMember(9)] public int FloatStyle { get; set; }
    }

    [ProtoContract]
    public class WebcastLiveIntroMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public long RoomId { get; set; }
        [ProtoMember(3)] public int AuditStatus { get; set; }
        [ProtoMember(4)] public string Content { get; set; } = "";
        [ProtoMember(5)] public UserIdentity? Host { get; set; }
        [ProtoMember(6)] public int IntroMode { get; set; }
        [ProtoMember(8)] public string Language { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastRoomMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Content { get; set; } = "";
    }

    [ProtoContract]
    public class CaptionData
    {
        [ProtoMember(1)] public string Language { get; set; } = "";
        [ProtoMember(2)] public string Text { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastCaptionMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public ulong TimeStamp { get; set; }
        [ProtoMember(4)] public CaptionData? CaptionData { get; set; }
    }

    [ProtoContract]
    public class WebcastGoalUpdateMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(4)] public long ContributorId { get; set; }
        [ProtoMember(6)] public string ContributorDisplayId { get; set; } = "";
        [ProtoMember(9)] public long ContributeCount { get; set; }
        [ProtoMember(10)] public long ContributeScore { get; set; }
        [ProtoMember(11)] public long GiftRepeatCount { get; set; }
        [ProtoMember(12)] public string ContributorIdStr { get; set; } = "";
        [ProtoMember(13)] public bool Pin { get; set; }
        [ProtoMember(14)] public bool Unpin { get; set; }
    }

    [ProtoContract]
    public class WebcastImDeleteMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public List<long> DeleteMsgIdsList { get; set; } = new List<long>();
        [ProtoMember(3)] public List<long> DeleteUserIdsList { get; set; } = new List<long>();
    }

    [ProtoContract]
    public class WebcastRankUpdate
    {
        [ProtoMember(1)] public long RankType { get; set; }
        [ProtoMember(2)] public long OwnerRank { get; set; }
        [ProtoMember(5)] public bool ShowEntranceAnimation { get; set; }
        [ProtoMember(6)] public long Countdown { get; set; }
        [ProtoMember(8)] public long RelatedTabRankType { get; set; }
        [ProtoMember(9)] public long RequestFirstShowType { get; set; }
        [ProtoMember(10)] public long SupportedVersion { get; set; }
        [ProtoMember(11)] public bool OwnerOnRank { get; set; }
    }

    [ProtoContract]
    public class WebcastRankTabInfo
    {
        [ProtoMember(1)] public long RankType { get; set; }
        [ProtoMember(2)] public string Title { get; set; } = "";
        [ProtoMember(4)] public long ListLynxType { get; set; }
    }

    [ProtoContract]
    public class WebcastRankUpdateMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public List<WebcastRankUpdate> UpdatesList { get; set; } = new List<WebcastRankUpdate>();
        [ProtoMember(3)] public long GroupType { get; set; }
        [ProtoMember(5)] public long Priority { get; set; }
        [ProtoMember(6)] public List<WebcastRankTabInfo> TabsList { get; set; } = new List<WebcastRankTabInfo>();
        [ProtoMember(7)] public bool IsAnimationLoopPlay { get; set; }
        [ProtoMember(8)] public bool AnimationLoopForOff { get; set; }
    }

    [ProtoContract]
    public class WebcastPollMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int MessageType { get; set; }
        [ProtoMember(3)] public long PollId { get; set; }
        [ProtoMember(4)] public byte[] StartContentBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(5)] public byte[] EndContentBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(6)] public byte[] UpdateContentBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(7)] public int PollKind { get; set; }
    }

    [ProtoContract]
    public class EnvelopeInfo
    {
        [ProtoMember(1)] public string EnvelopeId { get; set; } = "";
        [ProtoMember(2)] public int BusinessType { get; set; }
        [ProtoMember(3)] public string EnvelopeIdc { get; set; } = "";
        [ProtoMember(4)] public string SendUserName { get; set; } = "";
        [ProtoMember(5)] public int DiamondCount { get; set; }
        [ProtoMember(6)] public int PeopleCount { get; set; }
        [ProtoMember(7)] public int UnpackAt { get; set; }
        [ProtoMember(8)] public string SendUserId { get; set; } = "";
        [ProtoMember(9)] public byte[] SendUserAvatarRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(10)] public string CreateAt { get; set; } = "";
        [ProtoMember(11)] public string RoomId { get; set; } = "";
        [ProtoMember(12)] public int FollowShowStatus { get; set; }
        [ProtoMember(13)] public int SkinId { get; set; }
    }

    [ProtoContract]
    public class WebcastEnvelopeMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public EnvelopeInfo? EnvelopeInfo { get; set; }
        [ProtoMember(3)] public int Display { get; set; }
    }

    [ProtoContract]
    public class WebcastRoomPinMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] PinnedMessage { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(30)] public string OriginalMsgType { get; set; } = "";
        [ProtoMember(31)] public ulong Timestamp { get; set; }
    }

    [ProtoContract]
    public class WebcastUnauthorizedMemberMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int Action { get; set; }
        [ProtoMember(3)] public byte[] NickNamePrefixBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(4)] public string NickName { get; set; } = "";
        [ProtoMember(5)] public byte[] EnterTextBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastLinkMicMethod
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public int MessageType { get; set; }
        [ProtoMember(5)] public long UserId { get; set; }
        [ProtoMember(8)] public long ChannelId { get; set; }
        [ProtoMember(21)] public long ToUserId { get; set; }
        [ProtoMember(26)] public long StartTimeMs { get; set; }
        [ProtoMember(37)] public string AnchorLinkMicIdStr { get; set; } = "";
        [ProtoMember(38)] public long RivalAnchorId { get; set; }
        [ProtoMember(40)] public string RivalLinkmicIdStr { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastLinkMicBattle
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public long BattleId { get; set; }
        [ProtoMember(4)] public int Action { get; set; }
    }

    [ProtoContract]
    public class WebcastLinkMicArmies
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public long BattleId { get; set; }
        [ProtoMember(4)] public long ChannelId { get; set; }
        [ProtoMember(7)] public int BattleStatus { get; set; }
        [ProtoMember(8)] public long FromUserId { get; set; }
        [ProtoMember(9)] public long GiftId { get; set; }
        [ProtoMember(10)] public int GiftCount { get; set; }
        [ProtoMember(12)] public int TotalDiamondCount { get; set; }
        [ProtoMember(13)] public int RepeatCount { get; set; }
        [ProtoMember(15)] public bool TriggerCriticalStrike { get; set; }
    }

    [ProtoContract]
    public class WebcastLinkMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public int MessageType { get; set; }
        [ProtoMember(3)] public long LinkerId { get; set; }
        [ProtoMember(4)] public int Scene { get; set; }
        [ProtoMember(20)] public byte[] ListChangeContentBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastLinkLayerMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public int MessageType { get; set; }
        [ProtoMember(3)] public long ChannelId { get; set; }
        [ProtoMember(4)] public int Scene { get; set; }
        [ProtoMember(5)] public string Source { get; set; } = "";
        [ProtoMember(6)] public string CenterizedIdc { get; set; } = "";
        [ProtoMember(7)] public long RtcRoomId { get; set; }
        [ProtoMember(118)] public byte[] GroupChangeBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(200)] public byte[] BusinessBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastLinkMicLayoutStateMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public long RoomId { get; set; }
        [ProtoMember(3)] public int LayoutState { get; set; }
        [ProtoMember(6)] public string LayoutKey { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastGiftPanelUpdateMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public long RoomId { get; set; }
        [ProtoMember(3)] public long PanelTsOrVersion { get; set; }
        [ProtoMember(10)] public byte[] PanelBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(11)] public byte[] GiftListBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(12)] public byte[] VaultBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastInRoomBannerMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(3)] public int Position { get; set; }
        [ProtoMember(4)] public int ActionType { get; set; }
    }

    [ProtoContract]
    public class WebcastGuideMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public int GuideType { get; set; }
        [ProtoMember(5)] public long DurationMs { get; set; }
        [ProtoMember(7)] public string Scene { get; set; } = "";
    }
}
