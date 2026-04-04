using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using TikTokLive.Proto;

namespace TikTokLive.Events
{
    internal static class MessageRouter
    {
        private static readonly Dictionary<string, Func<byte[], List<TikTokLiveEvent>>> Handlers =
            new Dictionary<string, Func<byte[], List<TikTokLiveEvent>>>
            {
                // core (7) — social/member/control sub-routed
                ["WebcastChatMessage"] = p => One(TikTokLiveEvent.Chat(Decode<WebcastChatMessage>(p))),
                ["WebcastGiftMessage"] = p => One(TikTokLiveEvent.Gift(Decode<WebcastGiftMessage>(p))),
                ["WebcastLikeMessage"] = p => One(TikTokLiveEvent.Like(Decode<WebcastLikeMessage>(p))),
                ["WebcastMemberMessage"] = p => DecodeMember(p),
                ["WebcastSocialMessage"] = p => DecodeSocial(p),
                ["WebcastRoomUserSeqMessage"] = p => One(TikTokLiveEvent.RoomUserSeq(Decode<WebcastRoomUserSeqMessage>(p))),
                ["WebcastControlMessage"] = p => DecodeControl(p),
                // useful (5)
                ["WebcastLiveIntroMessage"] = p => One(TikTokLiveEvent.LiveIntro(Decode<WebcastLiveIntroMessage>(p))),
                ["WebcastRoomMessage"] = p => One(TikTokLiveEvent.RoomMessage(Decode<WebcastRoomMessage>(p))),
                ["WebcastCaptionMessage"] = p => One(TikTokLiveEvent.Caption(Decode<WebcastCaptionMessage>(p))),
                ["WebcastGoalUpdateMessage"] = p => One(TikTokLiveEvent.GoalUpdate(Decode<WebcastGoalUpdateMessage>(p))),
                ["WebcastImDeleteMessage"] = p => One(TikTokLiveEvent.ImDelete(Decode<WebcastImDeleteMessage>(p))),
                // niche (14)
                ["WebcastRankUpdateMessage"] = p => One(TikTokLiveEvent.RankUpdate(Decode<WebcastRankUpdateMessage>(p))),
                ["WebcastPollMessage"] = p => One(TikTokLiveEvent.Poll(Decode<WebcastPollMessage>(p))),
                ["WebcastEnvelopeMessage"] = p => One(TikTokLiveEvent.Envelope(Decode<WebcastEnvelopeMessage>(p))),
                ["WebcastRoomPinMessage"] = p => One(TikTokLiveEvent.RoomPin(Decode<WebcastRoomPinMessage>(p))),
                ["WebcastUnauthorizedMemberMessage"] = p => One(TikTokLiveEvent.UnauthorizedMember(Decode<WebcastUnauthorizedMemberMessage>(p))),
                ["WebcastLinkMicMethod"] = p => One(TikTokLiveEvent.LinkMicMethod(Decode<WebcastLinkMicMethod>(p))),
                ["WebcastLinkMicBattle"] = p => One(TikTokLiveEvent.LinkMicBattle(Decode<WebcastLinkMicBattle>(p))),
                ["WebcastLinkMicArmies"] = p => One(TikTokLiveEvent.LinkMicArmies(Decode<WebcastLinkMicArmies>(p))),
                ["WebcastLinkMessage"] = p => One(TikTokLiveEvent.LinkMessage(Decode<WebcastLinkMessage>(p))),
                ["WebcastLinkLayerMessage"] = p => One(TikTokLiveEvent.LinkLayer(Decode<WebcastLinkLayerMessage>(p))),
                ["WebcastLinkMicLayoutStateMessage"] = p => One(TikTokLiveEvent.LinkMicLayoutState(Decode<WebcastLinkMicLayoutStateMessage>(p))),
                ["WebcastGiftPanelUpdateMessage"] = p => One(TikTokLiveEvent.GiftPanelUpdate(Decode<WebcastGiftPanelUpdateMessage>(p))),
                ["WebcastInRoomBannerMessage"] = p => One(TikTokLiveEvent.InRoomBanner(Decode<WebcastInRoomBannerMessage>(p))),
                ["WebcastGuideMessage"] = p => One(TikTokLiveEvent.Guide(Decode<WebcastGuideMessage>(p))),
                // extended (13)
                ["WebcastEmoteChatMessage"] = p => One(TikTokLiveEvent.EmoteChat(Decode<WebcastEmoteChatMessage>(p))),
                ["WebcastQuestionNewMessage"] = p => One(TikTokLiveEvent.QuestionNew(Decode<WebcastQuestionNewMessage>(p))),
                ["WebcastSubNotifyMessage"] = p => One(TikTokLiveEvent.SubNotify(Decode<WebcastSubNotifyMessage>(p))),
                ["WebcastBarrageMessage"] = p => One(TikTokLiveEvent.Barrage(Decode<WebcastBarrageMessage>(p))),
                ["WebcastHourlyRankMessage"] = p => One(TikTokLiveEvent.HourlyRank(Decode<WebcastHourlyRankMessage>(p))),
                ["WebcastMsgDetectMessage"] = p => One(TikTokLiveEvent.MsgDetect(Decode<WebcastMsgDetectMessage>(p))),
                ["WebcastLinkMicFanTicketMethod"] = p => One(TikTokLiveEvent.LinkMicFanTicket(Decode<WebcastLinkMicFanTicketMethod>(p))),
                ["WebcastRoomVerifyMessage"] = p => One(TikTokLiveEvent.RoomVerify(Decode<WebcastRoomVerifyMessage>(p))),
                ["RoomVerifyMessage"] = p => One(TikTokLiveEvent.RoomVerify(Decode<WebcastRoomVerifyMessage>(p))),
                ["WebcastOecLiveShoppingMessage"] = p => One(TikTokLiveEvent.OecLiveShopping(Decode<WebcastOecLiveShoppingMessage>(p))),
                ["WebcastGiftBroadcastMessage"] = p => One(TikTokLiveEvent.GiftBroadcast(Decode<WebcastGiftBroadcastMessage>(p))),
                ["WebcastRankTextMessage"] = p => One(TikTokLiveEvent.RankText(Decode<WebcastRankTextMessage>(p))),
                ["WebcastGiftDynamicRestrictionMessage"] = p => One(TikTokLiveEvent.GiftDynamicRestriction(Decode<WebcastGiftDynamicRestrictionMessage>(p))),
                ["WebcastViewerPicksUpdateMessage"] = p => One(TikTokLiveEvent.ViewerPicksUpdate(Decode<WebcastViewerPicksUpdateMessage>(p))),
                // secondary (25)
                ["WebcastSystemMessage"] = p => One(TikTokLiveEvent.SystemMessage(Decode<WebcastSystemMessage>(p))),
                ["WebcastLiveGameIntroMessage"] = p => One(TikTokLiveEvent.LiveGameIntro(Decode<WebcastLiveGameIntroMessage>(p))),
                ["WebcastAccessControlMessage"] = p => One(TikTokLiveEvent.AccessControl(Decode<WebcastAccessControlMessage>(p))),
                ["WebcastAccessRecallMessage"] = p => One(TikTokLiveEvent.AccessRecall(Decode<WebcastAccessRecallMessage>(p))),
                ["WebcastAlertBoxAuditResultMessage"] = p => One(TikTokLiveEvent.AlertBoxAuditResult(Decode<WebcastAlertBoxAuditResultMessage>(p))),
                ["WebcastBindingGiftMessage"] = p => One(TikTokLiveEvent.BindingGift(Decode<WebcastBindingGiftMessage>(p))),
                ["WebcastBoostCardMessage"] = p => One(TikTokLiveEvent.BoostCard(Decode<WebcastBoostCardMessage>(p))),
                ["WebcastBottomMessage"] = p => One(TikTokLiveEvent.BottomMessage(Decode<WebcastBottomMessage>(p))),
                ["WebcastGameRankNotifyMessage"] = p => One(TikTokLiveEvent.GameRankNotify(Decode<WebcastGameRankNotifyMessage>(p))),
                ["WebcastGiftPromptMessage"] = p => One(TikTokLiveEvent.GiftPrompt(Decode<WebcastGiftPromptMessage>(p))),
                ["WebcastLinkStateMessage"] = p => One(TikTokLiveEvent.LinkState(Decode<WebcastLinkStateMessage>(p))),
                ["WebcastLinkMicBattlePunishFinish"] = p => One(TikTokLiveEvent.LinkMicBattlePunishFinish(Decode<WebcastLinkMicBattlePunishFinish>(p))),
                ["WebcastLinkmicBattleTaskMessage"] = p => One(TikTokLiveEvent.LinkmicBattleTask(Decode<WebcastLinkmicBattleTaskMessage>(p))),
                ["WebcastMarqueeAnnouncementMessage"] = p => One(TikTokLiveEvent.MarqueeAnnouncement(Decode<WebcastMarqueeAnnouncementMessage>(p))),
                ["WebcastNoticeMessage"] = p => One(TikTokLiveEvent.Notice(Decode<WebcastNoticeMessage>(p))),
                ["WebcastNotifyMessage"] = p => One(TikTokLiveEvent.Notify(Decode<WebcastNotifyMessage>(p))),
                ["WebcastPartnershipDropsUpdateMessage"] = p => One(TikTokLiveEvent.PartnershipDropsUpdate(Decode<WebcastPartnershipDropsUpdateMessage>(p))),
                ["WebcastPartnershipGameOfflineMessage"] = p => One(TikTokLiveEvent.PartnershipGameOffline(Decode<WebcastPartnershipGameOfflineMessage>(p))),
                ["WebcastPartnershipPunishMessage"] = p => One(TikTokLiveEvent.PartnershipPunish(Decode<WebcastPartnershipPunishMessage>(p))),
                ["WebcastPerceptionMessage"] = p => One(TikTokLiveEvent.Perception(Decode<WebcastPerceptionMessage>(p))),
                ["WebcastSpeakerMessage"] = p => One(TikTokLiveEvent.Speaker(Decode<WebcastSpeakerMessage>(p))),
                ["WebcastSubCapsuleMessage"] = p => One(TikTokLiveEvent.SubCapsule(Decode<WebcastSubCapsuleMessage>(p))),
                ["WebcastSubPinEventMessage"] = p => One(TikTokLiveEvent.SubPinEvent(Decode<WebcastSubPinEventMessage>(p))),
                ["WebcastSubscriptionNotifyMessage"] = p => One(TikTokLiveEvent.SubscriptionNotify(Decode<WebcastSubscriptionNotifyMessage>(p))),
                ["WebcastToastMessage"] = p => One(TikTokLiveEvent.Toast(Decode<WebcastToastMessage>(p))),
            };

        public static List<TikTokLiveEvent> Decode(string msgType, byte[] payload)
        {
            if (Handlers.TryGetValue(msgType, out Func<byte[], List<TikTokLiveEvent>>? handler))
            {
                try
                {
                    return handler(payload);
                }
                catch (Exception)
                {
                    return One(TikTokLiveEvent.Unknown(msgType, payload));
                }
            }
            return One(TikTokLiveEvent.Unknown(msgType, payload));
        }

        private static List<TikTokLiveEvent> DecodeSocial(byte[] payload)
        {
            var msg = Decode<WebcastSocialMessage>(payload);
            var events = new List<TikTokLiveEvent> { TikTokLiveEvent.Social(msg) };
            if (msg.Action == 1)
                events.Add(TikTokLiveEvent.Follow(msg));
            else if (msg.Action >= 2 && msg.Action <= 5)
                events.Add(TikTokLiveEvent.Share(msg));
            return events;
        }

        private static List<TikTokLiveEvent> DecodeMember(byte[] payload)
        {
            var msg = Decode<WebcastMemberMessage>(payload);
            var events = new List<TikTokLiveEvent> { TikTokLiveEvent.Member(msg) };
            if (msg.Action == 1)
                events.Add(TikTokLiveEvent.Join(msg));
            return events;
        }

        private static List<TikTokLiveEvent> DecodeControl(byte[] payload)
        {
            var msg = Decode<WebcastControlMessage>(payload);
            var events = new List<TikTokLiveEvent> { TikTokLiveEvent.Control(msg) };
            if (msg.Action == 3)
                events.Add(TikTokLiveEvent.LiveEnded(msg));
            return events;
        }

        private static List<TikTokLiveEvent> One(TikTokLiveEvent evt)
        {
            return new List<TikTokLiveEvent> { evt };
        }

        private static T Decode<T>(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
