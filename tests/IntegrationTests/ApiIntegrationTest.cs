// Hits real TikTok HTTP endpoints. Skipped (no-op pass) unless env vars are set.
// Default dotnet test stays green with zero network calls.
//
// PIRATETOK_LIVE_TEST_USER        — TikTok username that is live for the entire test run
// PIRATETOK_LIVE_TEST_OFFLINE_USER — must not be live (for offline error mapping)
// PIRATETOK_LIVE_TEST_COOKIES     — "sessionid=xxx; sid_tt=xxx" for 18+ room info fetch
// PIRATETOK_LIVE_TEST_HTTP=1      — enables nonexistent-user probe (synthetic username)

using System;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Errors;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    [Trait("Category", "Integration")]
    public class ApiIntegrationTest
    {
        // Hardcoded synthetic username — unlikely to be registered.
        // TikTok must return user-not-found for this probe.
        private const string SyntheticNonexistentUser =
            "piratetok_cs_nf_7a3c9e2f1b8d4a6c0e5f3a2b1d9c8e7";

        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(25);

        private readonly ITestOutputHelper _output;

        public ApiIntegrationTest(ITestOutputHelper output) => _output = output;

        // H1
        [Fact]
        public async Task CheckOnline_LiveUser_ReturnsRoomId()
        {
            string? user = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_USER");
            if (string.IsNullOrEmpty(user))
            {
                _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_USER to a live TikTok username to run this test");
                return;
            }
            user = user.Trim();

            var result = await TikTokLiveClient.CheckOnlineAsync(user, HttpTimeout);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.RoomId),
                "expected non-empty room ID for live user");
            Assert.NotEqual("0", result.RoomId);
        }

        // H2
        [Fact]
        public async Task CheckOnline_OfflineUser_ThrowsHostNotOnline()
        {
            string? user = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_OFFLINE_USER");
            if (string.IsNullOrEmpty(user))
            {
                _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_OFFLINE_USER to an offline TikTok username to run this test");
                return;
            }
            user = user.Trim();

            var ex = await Assert.ThrowsAsync<HostNotOnlineException>(
                () => TikTokLiveClient.CheckOnlineAsync(user, HttpTimeout));

            // Error quality: message must indicate offline — NOT "blocked" or "not found"
            string msg = ex.Message.ToLowerInvariant();
            Assert.True(
                msg.Contains("not online") || msg.Contains("offline") || msg.Contains("status"),
                $"expected offline-style message, got: {ex.Message}");
        }

        // H3
        [Fact]
        public async Task CheckOnline_NonexistentUser_ThrowsUserNotFound()
        {
            string? flag = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_HTTP");
            if (flag != "1" && flag != "true" && flag != "yes")
            {
                _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_HTTP=1 to enable the not-found probe (calls TikTok API)");
                return;
            }

            var ex = await Assert.ThrowsAsync<UserNotFoundException>(
                () => TikTokLiveClient.CheckOnlineAsync(SyntheticNonexistentUser, HttpTimeout));

            Assert.Equal(SyntheticNonexistentUser, ex.Username);
        }

        // H4
        [Fact]
        public async Task FetchRoomInfo_LiveRoom_ReturnsRoomInfo()
        {
            string? user = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_USER");
            if (string.IsNullOrEmpty(user))
            {
                _output.WriteLine("SKIP: set PIRATETOK_LIVE_TEST_USER to a live TikTok username to run this test");
                return;
            }
            user = user.Trim();

            string? cookies = Environment.GetEnvironmentVariable("PIRATETOK_LIVE_TEST_COOKIES");

            var room = await TikTokLiveClient.CheckOnlineAsync(user, HttpTimeout);
            var info = await TikTokLiveClient.FetchRoomInfoAsync(
                room.RoomId, HttpTimeout, cookies);

            Assert.NotNull(info);
            Assert.True(info.Viewers >= 0, "viewer count must be non-negative");
        }
    }
}
