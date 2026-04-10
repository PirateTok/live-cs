// WSS smoke tests — connect to a real live room and wait for specific event types.
// Skipped (no-op pass) when PIRATETOK_LIVE_TEST_USER is not set.
//
// Inherently flaky on quiet streams: quiet rooms may not produce all event types
// within the timeout window. That's acceptable — the test proves the pipeline works.
//
// WSS client config: EU CDN, 15s HTTP timeout, 5 max retries, 45s stale timeout.

using System;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Events;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    [Trait("Category", "Integration")]
    public class WssSmokeTest
    {
        private static readonly TimeSpan AwaitTraffic     = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan AwaitChat        = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan AwaitGift        = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan AwaitLike        = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan AwaitJoin        = TimeSpan.FromSeconds(150);
        private static readonly TimeSpan AwaitFollow      = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan AwaitSubscription = TimeSpan.FromSeconds(240);

        private readonly ITestOutputHelper _output;

        public WssSmokeTest(ITestOutputHelper output) => _output = output;

        // D1 — disconnect (cancel) unblocks RunAsync after connected
        [Fact]
        public async Task Disconnect_UnblocksRunAsyncAfterConnected()
        {
            string? user = GetLiveUser();
            if (user == null)
            {
                _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_USER to a live TikTok username to run this test");
                return;
            }

            var connected = new SemaphoreSlim(0, 1);
            var workerError = new ExceptionHolder();
            using var cts = new CancellationTokenSource();

            var client = BuildClient(user);
            bool signaled = false;
            client.OnConnected += (_) =>
            {
                if (!signaled) { signaled = true; connected.Release(); }
            };

            var worker = Task.Run(async () =>
            {
                try
                {
                    await client.RunAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // expected — cancel is the disconnect mechanism
                }
                catch (Exception ex)
                {
                    workerError.Set(ex);
                }
            });

            bool didConnect = await connected.WaitAsync(TimeSpan.FromSeconds(90));
            Assert.True(didConnect,
                "never reached CONNECTED within 90s (offline user or network issue)");
            Assert.Null(workerError.Get());

            long t0 = Environment.TickCount64;
            cts.Cancel();

            bool finished = await Task.WhenAny(worker, Task.Delay(TimeSpan.FromSeconds(20)))
                == worker;
            Assert.True(finished, "RunAsync task should complete soon after cancel");
            long elapsed = Environment.TickCount64 - t0;
            Assert.True(elapsed < 18_000,
                $"task should finish within 18s of cancel, took {elapsed}ms");
            Assert.Null(workerError.Get());
        }

        // W1 — any event within 90s
        [Fact]
        public async Task Connect_ReceivesTrafficBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitTraffic, (c, hit) =>
            {
                c.OnRoomUserSeq += (_) => hit();
                c.OnMember      += (_) => hit();
                c.OnChat        += (_) => hit();
                c.OnLike        += (_) => hit();
                c.OnControl     += (_) => hit();
            }, $"no room traffic within {AwaitTraffic.TotalSeconds}s (quiet stream or block)");
        }

        // W2 — chat event within 120s
        [Fact]
        public async Task Connect_ReceivesChatBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitChat, (c, hit) =>
            {
                c.OnChat += (msg) =>
                {
                    string nickname = msg.User?.Nickname ?? "?";
                    _output.WriteLine($"[integration test chat] {nickname}: {msg.Comment}");
                    hit();
                };
            }, $"no chat message within {AwaitChat.TotalSeconds}s (quiet stream or block)");
        }

        // W3 — gift event within 180s
        [Fact]
        public async Task Connect_ReceivesGiftBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitGift, (c, hit) =>
            {
                c.OnGift += (msg) =>
                {
                    string gifter   = msg.User?.Nickname ?? "?";
                    string giftName = msg.GiftDetails?.GiftName ?? "?";
                    int diamonds    = msg.GiftDetails?.DiamondCount ?? 0;
                    _output.WriteLine(
                        $"[integration test gift] {gifter} -> {giftName} x{msg.RepeatCount} ({diamonds} diamonds each)");
                    hit();
                };
            }, $"no gift within {AwaitGift.TotalSeconds}s (quiet stream or no gifts — try a busier stream)");
        }

        // W4 — like event within 120s
        [Fact]
        public async Task Connect_ReceivesLikeBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitLike, (c, hit) =>
            {
                c.OnLike += (msg) =>
                {
                    string liker = msg.User?.Nickname ?? "?";
                    _output.WriteLine(
                        $"[integration test like] {liker} count={msg.LikeCount} total={msg.TotalLikeCount}");
                    hit();
                };
            }, $"no like within {AwaitLike.TotalSeconds}s (quiet stream or block)");
        }

        // W5 — join sub-routed event within 150s
        [Fact]
        public async Task Connect_ReceivesJoinBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitJoin, (c, hit) =>
            {
                c.OnJoin += (_) =>
                {
                    _output.WriteLine("[integration test join] member joined");
                    hit();
                };
            }, $"no join within {AwaitJoin.TotalSeconds}s (try a busier stream)");
        }

        // W6 — follow sub-routed event within 180s
        [Fact]
        public async Task Connect_ReceivesFollowBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitFollow, (c, hit) =>
            {
                c.OnFollow += (_) =>
                {
                    _output.WriteLine("[integration test follow] follow received");
                    hit();
                };
            }, $"no follow within {AwaitFollow.TotalSeconds}s (follows are infrequent — try a growing stream)");
        }

        // W7 — subscription-related event within 240s (disabled — too rare on most streams)
        [Fact(Skip = "disabled by default — subscription events are too rare on most streams to be reliable")]
        public async Task Connect_ReceivesSubscriptionSignalBeforeTimeout()
        {
            string? user = GetLiveUser();
            if (user == null) { SkipNoUser(); return; }
            await AwaitWssEvent(user, AwaitSubscription, (c, hit) =>
            {
                c.OnEvent += (evt) =>
                {
                    if (evt.Type == TikTokLiveEventType.SubNotify ||
                        evt.Type == TikTokLiveEventType.SubscriptionNotify ||
                        evt.Type == TikTokLiveEventType.SubCapsule ||
                        evt.Type == TikTokLiveEventType.SubPinEvent)
                    {
                        _output.WriteLine($"[integration test subscription] {evt.Type}");
                        hit();
                    }
                };
            }, $"no subscription-related event within {AwaitSubscription.TotalSeconds}s");
        }

        // --- helpers ---

        private static string? GetLiveUser()
        {
            string? v = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_USER");
            return string.IsNullOrEmpty(v) ? null : v.Trim();
        }

        private void SkipNoUser()
            => _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_USER to a live TikTok username to run this test");

        private static TikTokLiveClient BuildClient(string username)
        {
            return new TikTokLiveClient(username)
                .CdnEu()
                .Timeout(TimeSpan.FromSeconds(15))
                .MaxRetries(5)
                .StaleTimeout(TimeSpan.FromSeconds(45));
        }

        private static async Task AwaitWssEvent(
            string user,
            TimeSpan awaitTimeout,
            Action<TikTokLiveClient, Action> registerListeners,
            string failureMessage)
        {
            var latch = new SemaphoreSlim(0, 1);
            var workerError = new ExceptionHolder();
            int hitOnce = 0;
            Action onHit = () =>
            {
                if (Interlocked.CompareExchange(ref hitOnce, 1, 0) == 0)
                    latch.Release();
            };

            using var cts = new CancellationTokenSource();
            var client = BuildClient(user);
            registerListeners(client, onHit);

            var worker = Task.Run(async () =>
            {
                try
                {
                    await client.RunAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // expected shutdown path
                }
                catch (Exception ex)
                {
                    workerError.Set(ex);
                    latch.Release(); // unblock the latch on error
                }
            });

            try
            {
                bool got = await latch.WaitAsync(awaitTimeout);
                Assert.Null(workerError.Get());
                Assert.True(got, failureMessage);
            }
            finally
            {
                cts.Cancel();
                await Task.WhenAny(worker, Task.Delay(TimeSpan.FromSeconds(30)));
                Assert.False(worker.IsFaulted, "worker task should not fault after cancel");
            }
        }
    }
}
