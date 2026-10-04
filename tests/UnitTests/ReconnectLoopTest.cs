using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Connection;
using TikTokLive.Events;
using Xunit;

namespace UnitTests
{
    /// <summary>F4 + F5 at client level: the reconnect loop against fake ttwid / session / clock.</summary>
    public class ReconnectLoopTest
    {
        private sealed class Fakes
        {
            public int FreshCalls, RunCalls, FreshFails;
            public SessionExit[] Exits = { SessionExit.Errored };
            public readonly List<TimeSpan> Delays = new List<TimeSpan>();
            public readonly List<TikTokLiveEvent> Events = new List<TikTokLiveEvent>();

            public Task Run(int maxRetries, CancellationToken ct = default) => ReconnectLoop.RunAsync(
                maxRetries,
                _ => Task.FromResult(++FreshCalls <= FreshFails ? null : new LiveSession("t", "u")),
                (_, _) => Task.FromResult(Exits[RunCalls++ % Exits.Length]),
                (d, _) => { Delays.Add(d); return Task.CompletedTask; },
                Events.Add,
                ct);

            public IEnumerable<TikTokLiveEventType> Types => Events.Select(e => e.Type);
        }

        [Fact]
        public async Task ReconnectingPerRetryThenDisconnectedOnce()
        {
            var f = new Fakes { FreshFails = 2 };
            await f.Run(3);
            Assert.Equal(new[]
            {
                TikTokLiveEventType.Reconnecting, TikTokLiveEventType.Reconnecting,
                TikTokLiveEventType.Reconnecting, TikTokLiveEventType.Disconnected,
            }, f.Types);
            Assert.Equal(new[] { 1, 2, 3 }, f.Events.Take(3).Select(e => e.As<ReconnectInfo>().Attempt));
            // 2 ttwid failures + 2 young-error sessions, each rotating → fresh every attempt
            Assert.Equal((4, 2), (f.FreshCalls, f.RunCalls));
            Assert.Equal(new[] { 2.0, 4.0, 8.0 }, f.Delays.Select(d => d.TotalSeconds));
        }

        [Fact]
        public async Task CleanCloseReusesTtwidAndUa()
        {
            var f = new Fakes { Exits = new[] { SessionExit.Closed } };
            await f.Run(4);
            Assert.Equal((1, 5), (f.FreshCalls, f.RunCalls));
            Assert.Equal(4, f.Types.Count(t => t == TikTokLiveEventType.Reconnecting));
            Assert.Equal(1, f.Types.Count(t => t == TikTokLiveEventType.Disconnected));
        }

        [Fact]
        public async Task DeviceBlockedRotatesWithShortDelay()
        {
            var f = new Fakes { Exits = new[] { SessionExit.DeviceBlocked } };
            await f.Run(2);
            Assert.Equal(3, f.FreshCalls);
            Assert.Equal(ReconnectBudget.DeviceBlockedDelay, f.Delays[0]);
        }

        [Fact]
        public async Task UserCancelDisconnectsOnce()
        {
            using var cts = new CancellationTokenSource();
            var events = new List<TikTokLiveEvent>();
            await ReconnectLoop.RunAsync(5,
                _ => Task.FromResult<LiveSession?>(new LiveSession("t", "u")),
                async (_, ct) => { cts.Cancel(); await Task.Delay(Timeout.Infinite, ct); return SessionExit.Closed; },
                Task.Delay,
                events.Add,
                cts.Token);
            Assert.Equal(new[] { TikTokLiveEventType.Disconnected }, events.Select(e => e.Type));
        }
    }
}
