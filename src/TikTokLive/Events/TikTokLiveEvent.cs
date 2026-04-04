using TikTokLive.Proto;

namespace TikTokLive.Events
{
    public class TikTokLiveEvent
    {
        public TikTokLiveEventType Type { get; }
        public object? Data { get; }

        private TikTokLiveEvent(TikTokLiveEventType type, object? data = null)
        {
            Type = type;
            Data = data;
        }

        // lifecycle
        public static TikTokLiveEvent Connected(string roomId) => new TikTokLiveEvent(TikTokLiveEventType.Connected, roomId);
        public static TikTokLiveEvent Reconnecting(int attempt, int maxRetries, int delaySecs) => new TikTokLiveEvent(TikTokLiveEventType.Reconnecting, new ReconnectInfo(attempt, maxRetries, delaySecs));
        public static TikTokLiveEvent Disconnected() => new TikTokLiveEvent(TikTokLiveEventType.Disconnected);

        // core (7)
        public static TikTokLiveEvent Chat(WebcastChatMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Chat, msg);
        public static TikTokLiveEvent Gift(WebcastGiftMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Gift, msg);
        public static TikTokLiveEvent Like(WebcastLikeMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Like, msg);
        public static TikTokLiveEvent Member(WebcastMemberMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Member, msg);
        public static TikTokLiveEvent Social(WebcastSocialMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Social, msg);
        public static TikTokLiveEvent RoomUserSeq(WebcastRoomUserSeqMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RoomUserSeq, msg);
        public static TikTokLiveEvent Control(WebcastControlMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Control, msg);

        // convenience sub-routed
        public static TikTokLiveEvent Follow(WebcastSocialMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Follow, msg);
        public static TikTokLiveEvent Share(WebcastSocialMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Share, msg);
        public static TikTokLiveEvent Join(WebcastMemberMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Join, msg);
        public static TikTokLiveEvent LiveEnded(WebcastControlMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LiveEnded, msg);

        // useful (5)
        public static TikTokLiveEvent LiveIntro(WebcastLiveIntroMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LiveIntro, msg);
        public static TikTokLiveEvent RoomMessage(WebcastRoomMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RoomMessage, msg);
        public static TikTokLiveEvent Caption(WebcastCaptionMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Caption, msg);
        public static TikTokLiveEvent GoalUpdate(WebcastGoalUpdateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GoalUpdate, msg);
        public static TikTokLiveEvent ImDelete(WebcastImDeleteMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.ImDelete, msg);

        // niche (14)
        public static TikTokLiveEvent RankUpdate(WebcastRankUpdateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RankUpdate, msg);
        public static TikTokLiveEvent Poll(WebcastPollMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Poll, msg);
        public static TikTokLiveEvent Envelope(WebcastEnvelopeMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Envelope, msg);
        public static TikTokLiveEvent RoomPin(WebcastRoomPinMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RoomPin, msg);
        public static TikTokLiveEvent UnauthorizedMember(WebcastUnauthorizedMemberMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.UnauthorizedMember, msg);
        public static TikTokLiveEvent LinkMicMethod(WebcastLinkMicMethod msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicMethod, msg);
        public static TikTokLiveEvent LinkMicBattle(WebcastLinkMicBattle msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicBattle, msg);
        public static TikTokLiveEvent LinkMicArmies(WebcastLinkMicArmies msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicArmies, msg);
        public static TikTokLiveEvent LinkMessage(WebcastLinkMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMessage, msg);
        public static TikTokLiveEvent LinkLayer(WebcastLinkLayerMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkLayer, msg);
        public static TikTokLiveEvent LinkMicLayoutState(WebcastLinkMicLayoutStateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicLayoutState, msg);
        public static TikTokLiveEvent GiftPanelUpdate(WebcastGiftPanelUpdateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GiftPanelUpdate, msg);
        public static TikTokLiveEvent InRoomBanner(WebcastInRoomBannerMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.InRoomBanner, msg);
        public static TikTokLiveEvent Guide(WebcastGuideMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Guide, msg);

        // extended (13)
        public static TikTokLiveEvent EmoteChat(WebcastEmoteChatMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.EmoteChat, msg);
        public static TikTokLiveEvent QuestionNew(WebcastQuestionNewMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.QuestionNew, msg);
        public static TikTokLiveEvent SubNotify(WebcastSubNotifyMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.SubNotify, msg);
        public static TikTokLiveEvent Barrage(WebcastBarrageMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Barrage, msg);
        public static TikTokLiveEvent HourlyRank(WebcastHourlyRankMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.HourlyRank, msg);
        public static TikTokLiveEvent MsgDetect(WebcastMsgDetectMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.MsgDetect, msg);
        public static TikTokLiveEvent LinkMicFanTicket(WebcastLinkMicFanTicketMethod msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicFanTicket, msg);
        public static TikTokLiveEvent RoomVerify(WebcastRoomVerifyMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RoomVerify, msg);
        public static TikTokLiveEvent OecLiveShopping(WebcastOecLiveShoppingMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.OecLiveShopping, msg);
        public static TikTokLiveEvent GiftBroadcast(WebcastGiftBroadcastMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GiftBroadcast, msg);
        public static TikTokLiveEvent RankText(WebcastRankTextMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.RankText, msg);
        public static TikTokLiveEvent GiftDynamicRestriction(WebcastGiftDynamicRestrictionMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GiftDynamicRestriction, msg);
        public static TikTokLiveEvent ViewerPicksUpdate(WebcastViewerPicksUpdateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.ViewerPicksUpdate, msg);

        // secondary (25)
        public static TikTokLiveEvent SystemMessage(WebcastSystemMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.SystemMessage, msg);
        public static TikTokLiveEvent LiveGameIntro(WebcastLiveGameIntroMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LiveGameIntro, msg);
        public static TikTokLiveEvent AccessControl(WebcastAccessControlMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.AccessControl, msg);
        public static TikTokLiveEvent AccessRecall(WebcastAccessRecallMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.AccessRecall, msg);
        public static TikTokLiveEvent AlertBoxAuditResult(WebcastAlertBoxAuditResultMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.AlertBoxAuditResult, msg);
        public static TikTokLiveEvent BindingGift(WebcastBindingGiftMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.BindingGift, msg);
        public static TikTokLiveEvent BoostCard(WebcastBoostCardMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.BoostCard, msg);
        public static TikTokLiveEvent BottomMessage(WebcastBottomMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.BottomMessage, msg);
        public static TikTokLiveEvent GameRankNotify(WebcastGameRankNotifyMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GameRankNotify, msg);
        public static TikTokLiveEvent GiftPrompt(WebcastGiftPromptMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.GiftPrompt, msg);
        public static TikTokLiveEvent LinkState(WebcastLinkStateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkState, msg);
        public static TikTokLiveEvent LinkMicBattlePunishFinish(WebcastLinkMicBattlePunishFinish msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkMicBattlePunishFinish, msg);
        public static TikTokLiveEvent LinkmicBattleTask(WebcastLinkmicBattleTaskMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.LinkmicBattleTask, msg);
        public static TikTokLiveEvent MarqueeAnnouncement(WebcastMarqueeAnnouncementMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.MarqueeAnnouncement, msg);
        public static TikTokLiveEvent Notice(WebcastNoticeMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Notice, msg);
        public static TikTokLiveEvent Notify(WebcastNotifyMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Notify, msg);
        public static TikTokLiveEvent PartnershipDropsUpdate(WebcastPartnershipDropsUpdateMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.PartnershipDropsUpdate, msg);
        public static TikTokLiveEvent PartnershipGameOffline(WebcastPartnershipGameOfflineMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.PartnershipGameOffline, msg);
        public static TikTokLiveEvent PartnershipPunish(WebcastPartnershipPunishMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.PartnershipPunish, msg);
        public static TikTokLiveEvent Perception(WebcastPerceptionMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Perception, msg);
        public static TikTokLiveEvent Speaker(WebcastSpeakerMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Speaker, msg);
        public static TikTokLiveEvent SubCapsule(WebcastSubCapsuleMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.SubCapsule, msg);
        public static TikTokLiveEvent SubPinEvent(WebcastSubPinEventMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.SubPinEvent, msg);
        public static TikTokLiveEvent SubscriptionNotify(WebcastSubscriptionNotifyMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.SubscriptionNotify, msg);
        public static TikTokLiveEvent Toast(WebcastToastMessage msg) => new TikTokLiveEvent(TikTokLiveEventType.Toast, msg);

        // catch-all
        public static TikTokLiveEvent Unknown(string method, byte[] payload) => new TikTokLiveEvent(TikTokLiveEventType.Unknown, new UnknownEvent(method, payload));

        public T As<T>() where T : class => (T)Data!;
        public string AsRoomId() => (string)Data!;
    }

    public enum TikTokLiveEventType
    {
        // lifecycle
        Connected, Reconnecting, Disconnected,
        // core
        Chat, Gift, Like, Member, Social, RoomUserSeq, Control,
        // convenience sub-routed
        Follow, Share, Join, LiveEnded,
        // useful
        LiveIntro, RoomMessage, Caption, GoalUpdate, ImDelete,
        // niche
        RankUpdate, Poll, Envelope, RoomPin, UnauthorizedMember,
        LinkMicMethod, LinkMicBattle, LinkMicArmies, LinkMessage,
        LinkLayer, LinkMicLayoutState, GiftPanelUpdate, InRoomBanner, Guide,
        // extended
        EmoteChat, QuestionNew, SubNotify, Barrage, HourlyRank,
        MsgDetect, LinkMicFanTicket, RoomVerify, OecLiveShopping,
        GiftBroadcast, RankText, GiftDynamicRestriction, ViewerPicksUpdate,
        // secondary
        SystemMessage, LiveGameIntro, AccessControl, AccessRecall,
        AlertBoxAuditResult, BindingGift, BoostCard, BottomMessage,
        GameRankNotify, GiftPrompt, LinkState, LinkMicBattlePunishFinish,
        LinkmicBattleTask, MarqueeAnnouncement, Notice, Notify,
        PartnershipDropsUpdate, PartnershipGameOffline, PartnershipPunish,
        Perception, Speaker, SubCapsule, SubPinEvent, SubscriptionNotify, Toast,
        // catch-all
        Unknown,
    }

    public class UnknownEvent
    {
        public string Method { get; }
        public byte[] Payload { get; }
        public UnknownEvent(string method, byte[] payload) { Method = method; Payload = payload; }
    }

    public class ReconnectInfo
    {
        public int Attempt { get; }
        public int MaxRetries { get; }
        public int DelaySecs { get; }
        public ReconnectInfo(int attempt, int maxRetries, int delaySecs)
        {
            Attempt = attempt; MaxRetries = maxRetries; DelaySecs = delaySecs;
        }
    }
}
