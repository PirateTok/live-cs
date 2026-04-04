// Extended + secondary message types.

using System.Collections.Generic;
using ProtoBuf;

namespace TikTokLive.Proto
{
    // -- Extended events (full proto fields) --

    [ProtoContract]
    public class EmoteData
    {
        [ProtoMember(1)] public int PlaceInComment { get; set; }
        [ProtoMember(2)] public EmoteDetails? Emote { get; set; }
    }

    [ProtoContract]
    public class EmoteDetails
    {
        [ProtoMember(1)] public string EmoteId { get; set; } = "";
        [ProtoMember(2)] public Image? Image { get; set; }
    }

    [ProtoContract]
    public class WebcastEmoteChatMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public UserIdentity? User { get; set; }
        [ProtoMember(3)] public List<EmoteData> EmoteList { get; set; } = new List<EmoteData>();
        [ProtoMember(5)] public UserIdentityContext? UserIdentity { get; set; }
    }

    [ProtoContract]
    public class QuestionDetails
    {
        [ProtoMember(1)] public long QuestionId { get; set; }
        [ProtoMember(2)] public string QuestionText { get; set; } = "";
        [ProtoMember(3)] public int AnswerStatus { get; set; }
        [ProtoMember(4)] public long CreateTime { get; set; }
        [ProtoMember(5)] public UserIdentity? User { get; set; }
    }

    [ProtoContract]
    public class WebcastQuestionNewMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public QuestionDetails? Details { get; set; }
    }

    [ProtoContract]
    public class WebcastSubNotifyMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public UserIdentity? Sender { get; set; }
        [ProtoMember(3)] public int ExhibitionType { get; set; }
        [ProtoMember(4)] public int SubMonth { get; set; }
        [ProtoMember(5)] public int SubscribeType { get; set; }
        [ProtoMember(6)] public int OldSubscribeStatus { get; set; }
        [ProtoMember(8)] public int SubscribingStatus { get; set; }
    }

    [ProtoContract]
    public class WebcastBarrageMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] EventBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(3)] public int MsgType { get; set; }
        [ProtoMember(6)] public long Duration { get; set; }
        [ProtoMember(9)] public int DisplayConfig { get; set; }
        [ProtoMember(10)] public long GalleryGiftId { get; set; }
        [ProtoMember(22)] public string Schema { get; set; } = "";
        [ProtoMember(23)] public string SubType { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastHourlyRankMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] RankContainerBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(3)] public uint Data2 { get; set; }
    }

    [ProtoContract]
    public class MsgDetectTriggerCondition
    {
        [ProtoMember(1)] public bool UplinkDetectHttp { get; set; }
        [ProtoMember(2)] public bool UplinkDetectWebSocket { get; set; }
        [ProtoMember(3)] public bool DetectP2PMsg { get; set; }
        [ProtoMember(4)] public bool DetectRoomMsg { get; set; }
        [ProtoMember(5)] public bool HttpOptimize { get; set; }
    }

    [ProtoContract]
    public class MsgDetectTimeInfo
    {
        [ProtoMember(1)] public long ClientStartMs { get; set; }
        [ProtoMember(2)] public long ApiRecvTimeMs { get; set; }
        [ProtoMember(3)] public long ApiSendToGoimMs { get; set; }
    }

    [ProtoContract]
    public class WebcastMsgDetectMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int DetectType { get; set; }
        [ProtoMember(3)] public MsgDetectTriggerCondition? TriggerCondition { get; set; }
        [ProtoMember(4)] public MsgDetectTimeInfo? TimeInfo { get; set; }
        [ProtoMember(5)] public int TriggerBy { get; set; }
        [ProtoMember(6)] public string FromRegion { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastLinkMicFanTicketMethod
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] FanTicketRoomNoticeBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastRoomVerifyMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int Action { get; set; }
        [ProtoMember(3)] public string Content { get; set; } = "";
        [ProtoMember(4)] public int NoticeType { get; set; }
        [ProtoMember(5)] public bool CloseRoom { get; set; }
    }

    [ProtoContract]
    public class WebcastOecLiveShoppingMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] ShoppingDataBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastGiftBroadcastMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] BroadcastDataBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastRankTextMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int Scene { get; set; }
        [ProtoMember(3)] public long OwnerIdxBeforeUpdate { get; set; }
        [ProtoMember(4)] public long OwnerIdxAfterUpdate { get; set; }
        [ProtoMember(5)] public string SelfGetBadgeMsg { get; set; } = "";
        [ProtoMember(6)] public string OtherGetBadgeMsg { get; set; } = "";
        [ProtoMember(7)] public long CurUserId { get; set; }
    }

    [ProtoContract]
    public class WebcastGiftDynamicRestrictionMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public byte[] RestrictionBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastViewerPicksUpdateMessage
    {
        [ProtoMember(1)] public byte[] CommonRaw { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public int UpdateType { get; set; }
        [ProtoMember(3)] public byte[] PicksBlob { get; set; } = System.Array.Empty<byte>();
    }

    // -- Secondary events (common + key fields) --

    [ProtoContract]
    public class WebcastSystemMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Message { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastLiveGameIntroMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] GameDataBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastAccessControlMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] CaptchaBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastAccessRecallMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int Status { get; set; }
        [ProtoMember(3)] public long Duration { get; set; }
        [ProtoMember(4)] public long EndTime { get; set; }
    }

    [ProtoContract]
    public class WebcastAlertBoxAuditResultMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public long UserId { get; set; }
        [ProtoMember(5)] public int Scene { get; set; }
    }

    [ProtoContract]
    public class WebcastBindingGiftMessage
    {
        [ProtoMember(1)] public byte[] GiftMessageBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(2)] public CommonMessageData? Common { get; set; }
    }

    [ProtoContract]
    public class WebcastBoostCardMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] CardsBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastBottomMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Content { get; set; } = "";
        [ProtoMember(3)] public int ShowType { get; set; }
        [ProtoMember(5)] public long Duration { get; set; }
    }

    [ProtoContract]
    public class WebcastGameRankNotifyMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int MsgType { get; set; }
        [ProtoMember(3)] public string NotifyText { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastGiftPromptMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Title { get; set; } = "";
        [ProtoMember(3)] public string Body { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastLinkStateMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public long ChannelId { get; set; }
        [ProtoMember(3)] public int Scene { get; set; }
        [ProtoMember(4)] public int Version { get; set; }
    }

    [ProtoContract]
    public class WebcastLinkMicBattlePunishFinish
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public long Id1 { get; set; }
        [ProtoMember(3)] public long Timestamp { get; set; }
    }

    [ProtoContract]
    public class WebcastLinkmicBattleTaskMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] TaskDataBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastMarqueeAnnouncementMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int MessageScene { get; set; }
        [ProtoMember(3)] public byte[] EntityListBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastNoticeMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Content { get; set; } = "";
        [ProtoMember(3)] public int NoticeType { get; set; }
    }

    [ProtoContract]
    public class WebcastNotifyMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Schema { get; set; } = "";
        [ProtoMember(3)] public int NotifyType { get; set; }
        [ProtoMember(4)] public string ContentStr { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastPartnershipDropsUpdateMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int ChangeMode { get; set; }
    }

    [ProtoContract]
    public class WebcastPartnershipGameOfflineMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] OfflineGameListBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastPartnershipPunishMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] PunishInfoBlob { get; set; } = System.Array.Empty<byte>();
    }

    [ProtoContract]
    public class WebcastPerceptionMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public byte[] DialogBlob { get; set; } = System.Array.Empty<byte>();
        [ProtoMember(4)] public long EndTime { get; set; }
    }

    [ProtoContract]
    public class WebcastSpeakerMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
    }

    [ProtoContract]
    public class WebcastSubCapsuleMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public string Description { get; set; } = "";
        [ProtoMember(3)] public string BtnName { get; set; } = "";
        [ProtoMember(4)] public string BtnUrl { get; set; } = "";
    }

    [ProtoContract]
    public class WebcastSubPinEventMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public int ActionType { get; set; }
        [ProtoMember(4)] public long OperatorUserId { get; set; }
    }

    [ProtoContract]
    public class WebcastSubscriptionNotifyMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public UserIdentity? User { get; set; }
        [ProtoMember(3)] public int ExhibitionType { get; set; }
    }

    [ProtoContract]
    public class WebcastToastMessage
    {
        [ProtoMember(1)] public CommonMessageData? Common { get; set; }
        [ProtoMember(2)] public long DisplayDurationMs { get; set; }
        [ProtoMember(3)] public long DelayDisplayDurationMs { get; set; }
    }
}
