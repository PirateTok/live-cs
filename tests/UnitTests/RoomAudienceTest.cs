using System;
using TikTokLive.Connection;
using TikTokLive.Errors;
using TikTokLive.Http;
using Xunit;

namespace UnitTests
{
    public class RoomAudienceTest
    {
        private const string Ok = @"{
          ""status_code"": 0,
          ""data"": {
            ""total"": 1234,
            ""anonymous"": 56,
            ""ranks"": [
              { ""rank"": 1, ""score"": 900, ""user"": {
                  ""id"": 111, ""id_str"": ""7000000000000000111"", ""display_id"": ""viewer_one"",
                  ""nickname"": ""Viewer One"", ""sec_uid"": ""MS4w-one"",
                  ""avatar_thumb"": { ""url_list"": [""https://p16.example/a.webp"", ""https://p19.example/a.webp""] },
                  ""follow_info"": { ""follower_count"": 42 },
                  ""verified"": true, ""is_follower"": true, ""is_following"": false, ""is_subscribe"": true } },
              { ""rank"": 2, ""score"": 10 },
              { ""rank"": 3, ""score"": 5, ""user"": { ""id"": 333, ""display_id"": ""viewer_three"", ""nickname"": ""V3"" } }
            ]
          }
        }";

        [Fact]
        public void ParsesRosterSkippingEntriesWithoutUser()
        {
            RoomAudience a = HttpApi.ParseRoomAudience(Ok, 200);
            Assert.Equal(1234, a.Total);
            Assert.Equal(56, a.Anonymous);
            Assert.Equal(2, a.Viewers.Count);
            Assert.Equal(Ok, a.RawJson);

            AudienceViewer one = a.Viewers[0];
            Assert.Equal(1, one.Rank);
            Assert.Equal(900, one.Score);
            Assert.Equal("7000000000000000111", one.UserId);
            Assert.Equal("viewer_one", one.Username);
            Assert.Equal("Viewer One", one.Nickname);
            Assert.Equal("MS4w-one", one.SecUid);
            Assert.Equal("https://p16.example/a.webp", one.AvatarUrl);
            Assert.Equal(42, one.FollowerCount);
            Assert.True(one.Verified);
            Assert.True(one.IsFollower);
            Assert.False(one.IsFollowing);
            Assert.True(one.IsSubscriber);

            AudienceViewer three = a.Viewers[1];
            Assert.Equal("333", three.UserId); // falls back to numeric id
            Assert.Null(three.AvatarUrl);
            Assert.Equal(0, three.FollowerCount);
        }

        [Fact]
        public void Status20003IsSessionRequired()
        {
            var ex = Assert.Throws<SessionRequiredException>(
                () => HttpApi.ParseRoomAudience(@"{""status_code"":20003,""data"":{""message"":""login""}}", 200));
            Assert.Contains("session cookies", ex.Message);
        }

        [Fact]
        public void OtherStatusIsInvalidResponseWithCodeAndMessage()
        {
            var ex = Assert.Throws<ProtocolException>(
                () => HttpApi.ParseRoomAudience(@"{""status_code"":10011,""data"":{""message"":""room gone""}}", 200));
            Assert.Contains("status_code=10011 room gone", ex.Message);
        }

        [Fact]
        public void MissingStatusAndEmptyBodyAreInvalid()
        {
            Assert.Throws<ProtocolException>(() => HttpApi.ParseRoomAudience(@"{""data"":{}}", 200));
            var empty = Assert.Throws<ProtocolException>(() => HttpApi.ParseRoomAudience("", 403));
            Assert.Contains("HTTP 403", empty.Message);
        }

        [Fact]
        public void WssUrlCarriesHeartbeatIntervalInMillis()
        {
            string url = WssUrlBuilder.Build("h", "1", "UTC", "en", "US", true, TimeSpan.FromSeconds(7));
            Assert.Contains("heartbeat_duration=7000&", url);
            Assert.Contains("heartbeat_duration=10000&", WssUrlBuilder.Build("h", "1", "UTC"));
        }
    }
}
