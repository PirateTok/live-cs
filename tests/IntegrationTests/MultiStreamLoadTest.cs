// Multi-stream concurrent load test.
// Skipped (no-op pass) when PIRATETOK_LIVE_TEST_USERS is not set.
// Gate: PIRATETOK_LIVE_TEST_USERS — comma-separated list of live TikTok usernames.
// All users must be live simultaneously.
//
// Flow:
//   1. Parse usernames from env var
//   2. Create one client per username (EU CDN, 15s timeout, 5 retries, 120s stale)
//   3. Register CONNECTED listener on each
//   4. RunAsync each on the thread pool concurrently
//   5. Wait up to 120s for all clients to reach CONNECTED
//   6. Listen for 60s (live window) counting chat events per channel
//   7. Cancel all clients
//   8. Wait up to 120s for all tasks to complete
//   9. Assert all tasks completed without unhandled exception

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    [Trait("Category", "Integration")]
    public class MultiStreamLoadTest
    {
        private readonly ITestOutputHelper _output;

        public MultiStreamLoadTest(ITestOutputHelper output) => _output = output;

        // M1
        [Fact]
        public async Task MultipleLiveClients_TrackChatForOneMinute()
        {
            string? raw = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_USERS");
            if (string.IsNullOrEmpty(raw))
            {
                _output.WriteLine(
                    "SKIP: set PIRATETOK_LIVE_TEST_USERS to a comma-separated list of live TikTok usernames to run this test");
                return;
            }

            string[] usernames = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .ToArray();

            Assert.True(usernames.Length > 0,
                "PIRATETOK_LIVE_TEST_USERS must contain at least one username");

            int n = usernames.Length;
            using var connectedLatch = new CountdownEvent(n);
            var chatCounts = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
            var sessionErrors = new ConcurrentBag<string>();
            using var cts = new CancellationTokenSource();

            foreach (string u in usernames)
                chatCounts[u] = 0;

            var clients = new List<(string username, TikTokLiveClient client)>(n);
            foreach (string username in usernames)
            {
                var client = new TikTokLiveClient(username)
                    .CdnEu()
                    .Timeout(TimeSpan.FromSeconds(15))
                    .MaxRetries(5)
                    .StaleTimeout(TimeSpan.FromSeconds(120));

                string captured = username;
                int signaled = 0;

                client.OnConnected += (_) =>
                {
                    if (Interlocked.CompareExchange(ref signaled, 1, 0) == 0)
                        connectedLatch.Signal();
                };

                client.OnChat += (_) =>
                    chatCounts.AddOrUpdate(captured, 1, (_, prev) => prev + 1);

                clients.Add((username, client));
            }

            // Start all RunAsync tasks concurrently on the thread pool
            var tasks = new List<Task>(n);
            foreach (var (username, client) in clients)
            {
                string captured = username;
                var t = Task.Run(async () =>
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
                        sessionErrors.Add($"@{captured}: {ex.Message}");
                    }
                });
                tasks.Add(t);
            }

            // Wait for all clients to reach CONNECTED (120s)
            bool allConnected = connectedLatch.Wait(TimeSpan.FromSeconds(120));
            Assert.True(allConnected,
                $"not all {n} clients reached CONNECTED within 120s — verify all users in PIRATETOK_LIVE_TEST_USERS are live");

            // Live window: collect events for 60s
            await Task.Delay(TimeSpan.FromSeconds(60), CancellationToken.None);

            // Disconnect all
            cts.Cancel();

            // Wait for all tasks to complete (120s)
            var allTask = Task.WhenAll(tasks);
            bool allDone = await Task.WhenAny(allTask, Task.Delay(TimeSpan.FromSeconds(120))) == allTask;
            Assert.True(allDone, "not all session tasks completed within 120s after cancel");

            // Log per-channel chat counts
            foreach (var (username, _) in clients)
            {
                int count = chatCounts.TryGetValue(username, out int c) ? c : 0;
                _output.WriteLine($"[integration test multi-stream] @{username}: {count} chat events in 60s");
            }

            // Assert no unhandled session errors
            if (!sessionErrors.IsEmpty)
            {
                Assert.Fail($"session errors: {string.Join("; ", sessionErrors)}");
            }
        }
    }
}
