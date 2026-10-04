using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using TikTokLive.Connection;
using TikTokLive.Events;
using TikTokLive.Proto;
using Xunit;

namespace UnitTests
{
    /// <summary>Server-side view of WebcastResponse with internal_ext as raw bytes.</summary>
    [ProtoContract]
    public class RawWebcastResponse
    {
        [ProtoMember(1)] public List<WebcastMessage> Messages { get; set; } = new List<WebcastMessage>();
        [ProtoMember(5)] public byte[] InternalExt { get; set; } = Array.Empty<byte>();
        [ProtoMember(9)] public bool NeedsAck { get; set; }
    }

    public class WireTest
    {
        private static byte[] Ser<T>(T obj)
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            return ms.ToArray();
        }

        private static T De<T>(byte[] data)
        {
            using var ms = new MemoryStream(data);
            return Serializer.Deserialize<T>(ms);
        }

        // non-UTF-8 on purpose: internal_ext is opaque bytes and must be echoed verbatim
        private static readonly byte[] Ext = { 0xff, 0x00, 0x80, 0x7a, 0xc3 };

        // F2 + F7 + F8: heartbeat, enter_room, ack (log_id + internal_ext), and the UA / cookies /
        // language / region / compress / heartbeat_duration on the wire.
        [Fact]
        public async Task SocketLoopAgainstFakeWebcast()
        {
            using var server = new FakeWebcast();
            string url = WssUrlBuilder.Build("HOST", "7", "UTC", "ro", "RO", false, TimeSpan.FromSeconds(2))
                .Replace("wss://HOST", $"ws://127.0.0.1:{server.Port}");
            var events = new ConcurrentQueue<TikTokLiveEvent>();
            var loop = new SocketLoop(url, "abc", "7", "UA-test/1.0", "sessionid=s1", null,
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), events.Enqueue);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Task client = loop.RunAsync(cts.Token);

            var (head, ws) = await server.AcceptAsync();
            var frames = new List<WebcastPushFrame>();
            frames.Add(De<WebcastPushFrame>(await FakeWebcast.ReceiveBinaryAsync(ws, cts.Token)));
            frames.Add(De<WebcastPushFrame>(await FakeWebcast.ReceiveBinaryAsync(ws, cts.Token)));

            byte[] chat = Ser(new WebcastChatMessage { Comment = "hello from fake" });
            byte[] resp = Ser(new RawWebcastResponse
            {
                Messages = { new WebcastMessage { Type = "WebcastChatMessage", Payload = chat } },
                InternalExt = Ext,
                NeedsAck = true,
            });
            await ws.SendAsync(Ser(new WebcastPushFrame { LogId = 4242, PayloadType = "msg", Payload = resp }),
                WebSocketMessageType.Binary, true, cts.Token);

            WebcastPushFrame ack;
            do
            {
                ack = De<WebcastPushFrame>(await FakeWebcast.ReceiveBinaryAsync(ws, cts.Token));
                frames.Add(ack);
            } while (ack.PayloadType != "ack");
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
            await client;

            Assert.Equal(new[] { "hb", "im_enter_room" }, frames.Take(2).Select(f => f.PayloadType));
            Assert.Equal(4242, ack.LogId);
            Assert.Equal(Ext, ack.Payload);

            Assert.Equal("UA-test/1.0", head["user-agent"]);
            Assert.Equal("ttwid=abc; sessionid=s1", head["cookie"]);
            Assert.Equal("https://www.tiktok.com", head["origin"]);
            var query = System.Web.HttpUtility.ParseQueryString(new Uri("http://x" + head["request-target"]).Query);
            Assert.Equal("7", query["room_id"]);
            Assert.Equal("ro", query["webcast_language"]);
            Assert.Equal("ro", query["app_language"]);
            Assert.Equal("ro-RO", query["browser_language"]);
            Assert.Equal("", query["compress"]);
            Assert.Equal("2000", query["heartbeat_duration"]);

            TikTokLiveEvent chatEvent = Assert.Single(events, e => e.Type == TikTokLiveEventType.Chat);
            Assert.Equal("hello from fake", chatEvent.As<WebcastChatMessage>().Comment);
        }
    }
}
