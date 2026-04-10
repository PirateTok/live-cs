// Replay test -- reads a capture file, processes it through the full decode
// pipeline, and asserts every value matches the manifest JSON.
//
// Skips if testdata is not available. Set PIRATETOK_TESTDATA env var or
// place captures in ../live-testdata/.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProtoBuf;
using Xunit;
using Xunit.Abstractions;
using TikTokLive.Events;
using TikTokLive.Helpers;
using TikTokLive.Proto;

namespace ReplayTests
{
    public class ReplayTest
    {
        private readonly ITestOutputHelper _output;

        public ReplayTest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void ReplayCalvinterest6() => RunCaptureTest("calvinterest6");

        [Fact]
        public void ReplayHappyhappygaltv() => RunCaptureTest("happyhappygaltv");

        [Fact]
        public void ReplayFox4newsdallasfortworth() => RunCaptureTest("fox4newsdallasfortworth");

        // --- test runner ---

        private void RunCaptureTest(string name)
        {
            string? testdata = FindTestdata();
            if (testdata == null)
            {
                _output.WriteLine($"SKIP {name}: no testdata (set PIRATETOK_TESTDATA or clone live-testdata)");
                return;
            }

            string capPath = CapturePath(testdata, name);
            string manPath = ManifestPath(testdata, name);

            if (!File.Exists(capPath))
            {
                _output.WriteLine($"SKIP {name}: capture not found at {capPath}");
                return;
            }
            if (!File.Exists(manPath))
            {
                _output.WriteLine($"SKIP {name}: manifest not found at {manPath}");
                return;
            }

            string manifestJson = File.ReadAllText(manPath);
            Manifest manifest = JsonSerializer.Deserialize<Manifest>(manifestJson)
                ?? throw new InvalidOperationException("manifest deserialized to null");

            List<byte[]> frames = ReadCapture(capPath);
            ReplayResult result = Replay(frames);
            AssertReplay(name, result, manifest);
        }

        // --- test data location ---

        private static string? FindTestdata()
        {
            string? envDir = Environment.GetEnvironmentVariable("PIRATETOK_TESTDATA");
            if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
                return envDir;

            // Walk up from the binary dir to find testdata/ in repo root
            string? dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string testdata = Path.Combine(dir, "testdata");
                if (Directory.Exists(Path.Combine(testdata, "captures")))
                    return testdata;
                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private static string CapturePath(string testdata, string name)
        {
            string inTestdata = Path.Combine(testdata, "captures", $"{name}.bin");
            if (File.Exists(inTestdata))
                return inTestdata;
            // dev fallback path
            return inTestdata;
        }

        private static string ManifestPath(string testdata, string name)
        {
            string inTestdata = Path.Combine(testdata, "manifests", $"{name}.json");
            if (File.Exists(inTestdata))
                return inTestdata;
            // dev fallback: captures/manifests/<name>.json
            string alt = Path.Combine(testdata, "captures", "manifests", $"{name}.json");
            if (File.Exists(alt))
                return alt;
            return inTestdata;
        }

        // --- frame reader ---

        private static List<byte[]> ReadCapture(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            var frames = new List<byte[]>();
            int pos = 0;
            while (pos + 4 <= data.Length)
            {
                uint len = BitConverter.ToUInt32(data, pos);
                pos += 4;
                if (pos + (int)len > data.Length)
                    throw new InvalidOperationException($"truncated frame at offset {pos - 4}");
                byte[] frame = new byte[len];
                Buffer.BlockCopy(data, pos, frame, 0, (int)len);
                frames.Add(frame);
                pos += (int)len;
            }
            return frames;
        }

        // --- replay engine ---

        private static ReplayResult Replay(List<byte[]> frames)
        {
            var r = new ReplayResult { FrameCount = frames.Count };
            var likeAcc = new LikeAccumulator();
            var giftTracker = new GiftStreakTracker();

            foreach (byte[] raw in frames)
            {
                WebcastPushFrame frame;
                try
                {
                    using var ms = new MemoryStream(raw);
                    frame = Serializer.Deserialize<WebcastPushFrame>(ms);
                }
                catch (Exception)
                {
                    r.DecodeFailures++;
                    continue;
                }

                Increment(r.PayloadTypes, frame.PayloadType);

                if (frame.PayloadType != "msg")
                    continue;

                byte[] payload;
                try
                {
                    payload = DecompressIfGzipped(frame.Payload);
                }
                catch (Exception)
                {
                    r.DecompressFailures++;
                    continue;
                }

                WebcastResponse response;
                try
                {
                    using var ms = new MemoryStream(payload);
                    response = Serializer.Deserialize<WebcastResponse>(ms);
                }
                catch (Exception)
                {
                    r.DecodeFailures++;
                    continue;
                }

                foreach (WebcastMessage msg in response.Messages)
                {
                    r.MessageCount++;
                    Increment(r.MessageTypes, msg.Type);

                    // Route through the same MessageRouter as the live connection
                    List<TikTokLiveEvent> events = MessageRouter.Decode(msg.Type, msg.Payload);
                    foreach (TikTokLiveEvent evt in events)
                    {
                        r.EventCount++;
                        string etype = EventTypeName(evt);
                        Increment(r.EventTypes, etype);

                        switch (evt.Type)
                        {
                            case TikTokLiveEventType.Follow:
                                r.FollowCount++;
                                break;
                            case TikTokLiveEventType.Share:
                                r.ShareCount++;
                                break;
                            case TikTokLiveEventType.Join:
                                r.JoinCount++;
                                break;
                            case TikTokLiveEventType.LiveEnded:
                                r.LiveEndedCount++;
                                break;
                            case TikTokLiveEventType.Unknown:
                                var unk = evt.As<UnknownEvent>();
                                Increment(r.UnknownTypes, unk.Method);
                                break;
                        }
                    }

                    // Like accumulator
                    if (msg.Type == "WebcastLikeMessage")
                    {
                        try
                        {
                            using var lms = new MemoryStream(msg.Payload);
                            var likeMsg = Serializer.Deserialize<WebcastLikeMessage>(lms);
                            LikeStats stats = likeAcc.Process(likeMsg);
                            r.LikeEvents.Add(new LikeEventRecord
                            {
                                WireCount = likeMsg.LikeCount,
                                WireTotal = likeMsg.TotalLikeCount,
                                AccTotal = stats.TotalLikeCount,
                                Accumulated = stats.AccumulatedCount,
                                WentBackwards = stats.WentBackwards,
                            });
                        }
                        catch (Exception)
                        {
                            // decode failure for like — should not happen
                        }
                    }

                    // Gift streak tracker
                    if (msg.Type == "WebcastGiftMessage")
                    {
                        try
                        {
                            using var gms = new MemoryStream(msg.Payload);
                            var giftMsg = Serializer.Deserialize<WebcastGiftMessage>(gms);
                            if (giftMsg.IsComboGift())
                                r.ComboCount++;
                            else
                                r.NonComboCount++;

                            GiftStreakEvent streak = giftTracker.Process(giftMsg);
                            if (streak.IsFinal)
                                r.StreakFinals++;
                            if (streak.EventGiftCount < 0)
                                r.NegativeDeltas++;

                            string key = giftMsg.GroupId.ToString();
                            if (!r.GiftGroups.TryGetValue(key, out List<GiftGroupRecord>? group))
                            {
                                group = new List<GiftGroupRecord>();
                                r.GiftGroups[key] = group;
                            }
                            group.Add(new GiftGroupRecord
                            {
                                GiftId = giftMsg.GiftId,
                                RepeatCount = giftMsg.RepeatCount,
                                Delta = streak.EventGiftCount,
                                IsFinal = streak.IsFinal,
                                DiamondTotal = streak.TotalDiamondCount,
                            });
                        }
                        catch (Exception)
                        {
                            // decode failure for gift — should not happen
                        }
                    }
                }
            }

            return r;
        }

        // --- event type name mapping ---

        private static string EventTypeName(TikTokLiveEvent evt)
        {
            return evt.Type switch
            {
                TikTokLiveEventType.Connected => "Connected",
                TikTokLiveEventType.Reconnecting => "Reconnecting",
                TikTokLiveEventType.Disconnected => "Disconnected",
                TikTokLiveEventType.Chat => "Chat",
                TikTokLiveEventType.Gift => "Gift",
                TikTokLiveEventType.Like => "Like",
                TikTokLiveEventType.Member => "Member",
                TikTokLiveEventType.Social => "Social",
                TikTokLiveEventType.Follow => "Follow",
                TikTokLiveEventType.Share => "Share",
                TikTokLiveEventType.Join => "Join",
                TikTokLiveEventType.RoomUserSeq => "RoomUserSeq",
                TikTokLiveEventType.Control => "Control",
                TikTokLiveEventType.LiveEnded => "LiveEnded",
                TikTokLiveEventType.LiveIntro => "LiveIntro",
                TikTokLiveEventType.RoomMessage => "RoomMessage",
                TikTokLiveEventType.Caption => "Caption",
                TikTokLiveEventType.GoalUpdate => "GoalUpdate",
                TikTokLiveEventType.ImDelete => "ImDelete",
                TikTokLiveEventType.RankUpdate => "RankUpdate",
                TikTokLiveEventType.Poll => "Poll",
                TikTokLiveEventType.Envelope => "Envelope",
                TikTokLiveEventType.RoomPin => "RoomPin",
                TikTokLiveEventType.UnauthorizedMember => "UnauthorizedMember",
                TikTokLiveEventType.LinkMicMethod => "LinkMicMethod",
                TikTokLiveEventType.LinkMicBattle => "LinkMicBattle",
                TikTokLiveEventType.LinkMicArmies => "LinkMicArmies",
                TikTokLiveEventType.LinkMessage => "LinkMessage",
                TikTokLiveEventType.LinkLayer => "LinkLayer",
                TikTokLiveEventType.LinkMicLayoutState => "LinkMicLayoutState",
                TikTokLiveEventType.GiftPanelUpdate => "GiftPanelUpdate",
                TikTokLiveEventType.InRoomBanner => "InRoomBanner",
                TikTokLiveEventType.Guide => "Guide",
                TikTokLiveEventType.EmoteChat => "EmoteChat",
                TikTokLiveEventType.QuestionNew => "QuestionNew",
                TikTokLiveEventType.SubNotify => "SubNotify",
                TikTokLiveEventType.Barrage => "Barrage",
                TikTokLiveEventType.HourlyRank => "HourlyRank",
                TikTokLiveEventType.MsgDetect => "MsgDetect",
                TikTokLiveEventType.LinkMicFanTicket => "LinkMicFanTicket",
                TikTokLiveEventType.RoomVerify => "RoomVerify",
                TikTokLiveEventType.OecLiveShopping => "OecLiveShopping",
                TikTokLiveEventType.GiftBroadcast => "GiftBroadcast",
                TikTokLiveEventType.RankText => "RankText",
                TikTokLiveEventType.GiftDynamicRestriction => "GiftDynamicRestriction",
                TikTokLiveEventType.ViewerPicksUpdate => "ViewerPicksUpdate",
                TikTokLiveEventType.SystemMessage => "SystemMessage",
                TikTokLiveEventType.LiveGameIntro => "LiveGameIntro",
                TikTokLiveEventType.AccessControl => "AccessControl",
                TikTokLiveEventType.AccessRecall => "AccessRecall",
                TikTokLiveEventType.AlertBoxAuditResult => "AlertBoxAuditResult",
                TikTokLiveEventType.BindingGift => "BindingGift",
                TikTokLiveEventType.BoostCard => "BoostCard",
                TikTokLiveEventType.BottomMessage => "BottomMessage",
                TikTokLiveEventType.GameRankNotify => "GameRankNotify",
                TikTokLiveEventType.GiftPrompt => "GiftPrompt",
                TikTokLiveEventType.LinkState => "LinkState",
                TikTokLiveEventType.LinkMicBattlePunishFinish => "LinkMicBattlePunishFinish",
                TikTokLiveEventType.LinkmicBattleTask => "LinkmicBattleTask",
                TikTokLiveEventType.MarqueeAnnouncement => "MarqueeAnnouncement",
                TikTokLiveEventType.Notice => "Notice",
                TikTokLiveEventType.Notify => "Notify",
                TikTokLiveEventType.PartnershipDropsUpdate => "PartnershipDropsUpdate",
                TikTokLiveEventType.PartnershipGameOffline => "PartnershipGameOffline",
                TikTokLiveEventType.PartnershipPunish => "PartnershipPunish",
                TikTokLiveEventType.Perception => "Perception",
                TikTokLiveEventType.Speaker => "Speaker",
                TikTokLiveEventType.SubCapsule => "SubCapsule",
                TikTokLiveEventType.SubPinEvent => "SubPinEvent",
                TikTokLiveEventType.SubscriptionNotify => "SubscriptionNotify",
                TikTokLiveEventType.Toast => "Toast",
                TikTokLiveEventType.Unknown => "Unknown",
                _ => "Unknown",
            };
        }

        // --- assertion helpers ---

        private static void AssertReplay(string name, ReplayResult r, Manifest m)
        {
            Assert.Equal(m.FrameCount, r.FrameCount);
            Assert.Equal(m.MessageCount, r.MessageCount);
            Assert.Equal(m.EventCount, r.EventCount);
            Assert.Equal(m.DecodeFailures, r.DecodeFailures);
            Assert.Equal(m.DecompressFailures, r.DecompressFailures);

            AssertMapsEqual($"{name}: payload_types", m.PayloadTypes, r.PayloadTypes);
            AssertMapsEqual($"{name}: message_types", m.MessageTypes, r.MessageTypes);
            AssertMapsEqual($"{name}: event_types", m.EventTypes, r.EventTypes);

            Assert.Equal(m.SubRouted.Follow, r.FollowCount);
            Assert.Equal(m.SubRouted.Share, r.ShareCount);
            Assert.Equal(m.SubRouted.Join, r.JoinCount);
            Assert.Equal(m.SubRouted.LiveEnded, r.LiveEndedCount);

            AssertMapsEqual($"{name}: unknown_types", m.UnknownTypes, r.UnknownTypes);

            // like accumulator
            ManifestLikeAcc ml = m.LikeAccumulator;
            Assert.Equal(ml.EventCount, (long)r.LikeEvents.Count);

            long backwards = r.LikeEvents.Count(e => e.WentBackwards);
            Assert.Equal(ml.BackwardsJumps, backwards);

            if (r.LikeEvents.Count > 0)
            {
                LikeEventRecord last = r.LikeEvents[r.LikeEvents.Count - 1];
                Assert.Equal(ml.FinalMaxTotal, last.AccTotal);
                Assert.Equal(ml.FinalAccumulated, last.Accumulated);
            }

            bool accMono = IsMonotonic(r.LikeEvents, e => e.AccTotal);
            bool accumMono = IsMonotonic(r.LikeEvents, e => e.Accumulated);
            Assert.Equal(ml.AccTotalMonotonic, accMono);
            Assert.Equal(ml.AccumulatedMonotonic, accumMono);

            // like event-by-event
            Assert.Equal(ml.Events.Count, r.LikeEvents.Count);
            for (int i = 0; i < r.LikeEvents.Count; i++)
            {
                LikeEventRecord got = r.LikeEvents[i];
                ManifestLikeEvent expected = ml.Events[i];
                Assert.Equal(expected.WireCount, got.WireCount);
                Assert.Equal(expected.WireTotal, got.WireTotal);
                Assert.Equal(expected.AccTotal, got.AccTotal);
                Assert.Equal(expected.Accumulated, got.Accumulated);
                Assert.Equal(expected.WentBackwards, got.WentBackwards);
            }

            // gift streaks
            ManifestGiftStreaks mg = m.GiftStreaks;
            Assert.Equal(mg.EventCount, r.ComboCount + r.NonComboCount);
            Assert.Equal(mg.ComboCount, r.ComboCount);
            Assert.Equal(mg.NonComboCount, r.NonComboCount);
            Assert.Equal(mg.StreakFinals, r.StreakFinals);
            Assert.Equal(mg.NegativeDeltas, r.NegativeDeltas);

            // gift group-by-group
            Assert.Equal(mg.Groups.Count, r.GiftGroups.Count);
            foreach (var kvp in r.GiftGroups)
            {
                Assert.True(mg.Groups.ContainsKey(kvp.Key), $"{name}: missing gift group {kvp.Key}");
                List<ManifestGiftGroupEvent> expectedEvts = mg.Groups[kvp.Key];
                List<GiftGroupRecord> gotEvts = kvp.Value;
                Assert.Equal(expectedEvts.Count, gotEvts.Count);
                for (int i = 0; i < gotEvts.Count; i++)
                {
                    GiftGroupRecord got = gotEvts[i];
                    ManifestGiftGroupEvent exp = expectedEvts[i];
                    Assert.Equal(exp.GiftId, got.GiftId);
                    Assert.Equal(exp.RepeatCount, got.RepeatCount);
                    Assert.Equal(exp.Delta, got.Delta);
                    Assert.Equal(exp.IsFinal, got.IsFinal);
                    Assert.Equal(exp.DiamondTotal, got.DiamondTotal);
                }
            }
        }

        // --- helpers ---

        private static void Increment(Dictionary<string, long> map, string key)
        {
            if (map.TryGetValue(key, out long val))
                map[key] = val + 1;
            else
                map[key] = 1;
        }

        private static void AssertMapsEqual(string label, Dictionary<string, long> expected, Dictionary<string, long> actual)
        {
            var allKeys = new SortedSet<string>(expected.Keys);
            foreach (string k in actual.Keys)
                allKeys.Add(k);

            foreach (string key in allKeys)
            {
                long exp = expected.TryGetValue(key, out long e) ? e : 0;
                long act = actual.TryGetValue(key, out long a) ? a : 0;
                Assert.True(exp == act, $"{label}[\"{key}\"]: expected {exp}, got {act}");
            }
        }

        private static bool IsMonotonic<T>(List<T> items, Func<T, long> selector)
        {
            for (int i = 1; i < items.Count; i++)
            {
                if (selector(items[i]) < selector(items[i - 1]))
                    return false;
            }
            return true;
        }

        private static byte[] DecompressIfGzipped(byte[] data)
        {
            if (data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b)
            {
                using var input = new MemoryStream(data);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
            return data;
        }
    }

    // --- replay result ---

    internal class ReplayResult
    {
        public long FrameCount { get; set; }
        public long MessageCount { get; set; }
        public long EventCount { get; set; }
        public long DecodeFailures { get; set; }
        public long DecompressFailures { get; set; }
        public Dictionary<string, long> PayloadTypes { get; } = new();
        public Dictionary<string, long> MessageTypes { get; } = new();
        public Dictionary<string, long> EventTypes { get; } = new();
        public long FollowCount { get; set; }
        public long ShareCount { get; set; }
        public long JoinCount { get; set; }
        public long LiveEndedCount { get; set; }
        public Dictionary<string, long> UnknownTypes { get; } = new();
        public List<LikeEventRecord> LikeEvents { get; } = new();
        public Dictionary<string, List<GiftGroupRecord>> GiftGroups { get; } = new();
        public long ComboCount { get; set; }
        public long NonComboCount { get; set; }
        public long StreakFinals { get; set; }
        public long NegativeDeltas { get; set; }
    }

    internal class LikeEventRecord
    {
        public int WireCount { get; set; }
        public int WireTotal { get; set; }
        public int AccTotal { get; set; }
        public long Accumulated { get; set; }
        public bool WentBackwards { get; set; }
    }

    internal class GiftGroupRecord
    {
        public int GiftId { get; set; }
        public int RepeatCount { get; set; }
        public int Delta { get; set; }
        public bool IsFinal { get; set; }
        public long DiamondTotal { get; set; }
    }

    // --- manifest deserialization ---

    internal class Manifest
    {
        [JsonPropertyName("frame_count")] public long FrameCount { get; set; }
        [JsonPropertyName("message_count")] public long MessageCount { get; set; }
        [JsonPropertyName("event_count")] public long EventCount { get; set; }
        [JsonPropertyName("decode_failures")] public long DecodeFailures { get; set; }
        [JsonPropertyName("decompress_failures")] public long DecompressFailures { get; set; }
        [JsonPropertyName("payload_types")] public Dictionary<string, long> PayloadTypes { get; set; } = new();
        [JsonPropertyName("message_types")] public Dictionary<string, long> MessageTypes { get; set; } = new();
        [JsonPropertyName("event_types")] public Dictionary<string, long> EventTypes { get; set; } = new();
        [JsonPropertyName("sub_routed")] public ManifestSubRouted SubRouted { get; set; } = new();
        [JsonPropertyName("unknown_types")] public Dictionary<string, long> UnknownTypes { get; set; } = new();
        [JsonPropertyName("like_accumulator")] public ManifestLikeAcc LikeAccumulator { get; set; } = new();
        [JsonPropertyName("gift_streaks")] public ManifestGiftStreaks GiftStreaks { get; set; } = new();
    }

    internal class ManifestSubRouted
    {
        [JsonPropertyName("follow")] public long Follow { get; set; }
        [JsonPropertyName("share")] public long Share { get; set; }
        [JsonPropertyName("join")] public long Join { get; set; }
        [JsonPropertyName("live_ended")] public long LiveEnded { get; set; }
    }

    internal class ManifestLikeAcc
    {
        [JsonPropertyName("event_count")] public long EventCount { get; set; }
        [JsonPropertyName("backwards_jumps")] public long BackwardsJumps { get; set; }
        [JsonPropertyName("final_max_total")] public int FinalMaxTotal { get; set; }
        [JsonPropertyName("final_accumulated")] public long FinalAccumulated { get; set; }
        [JsonPropertyName("acc_total_monotonic")] public bool AccTotalMonotonic { get; set; }
        [JsonPropertyName("accumulated_monotonic")] public bool AccumulatedMonotonic { get; set; }
        [JsonPropertyName("events")] public List<ManifestLikeEvent> Events { get; set; } = new();
    }

    internal class ManifestLikeEvent
    {
        [JsonPropertyName("wire_count")] public int WireCount { get; set; }
        [JsonPropertyName("wire_total")] public int WireTotal { get; set; }
        [JsonPropertyName("acc_total")] public int AccTotal { get; set; }
        [JsonPropertyName("accumulated")] public long Accumulated { get; set; }
        [JsonPropertyName("went_backwards")] public bool WentBackwards { get; set; }
    }

    internal class ManifestGiftStreaks
    {
        [JsonPropertyName("event_count")] public long EventCount { get; set; }
        [JsonPropertyName("combo_count")] public long ComboCount { get; set; }
        [JsonPropertyName("non_combo_count")] public long NonComboCount { get; set; }
        [JsonPropertyName("streak_finals")] public long StreakFinals { get; set; }
        [JsonPropertyName("negative_deltas")] public long NegativeDeltas { get; set; }
        [JsonPropertyName("groups")] public Dictionary<string, List<ManifestGiftGroupEvent>> Groups { get; set; } = new();
    }

    internal class ManifestGiftGroupEvent
    {
        [JsonPropertyName("gift_id")] public int GiftId { get; set; }
        [JsonPropertyName("repeat_count")] public int RepeatCount { get; set; }
        [JsonPropertyName("delta")] public int Delta { get; set; }
        [JsonPropertyName("is_final")] public bool IsFinal { get; set; }
        [JsonPropertyName("diamond_total")] public long DiamondTotal { get; set; }
    }
}
