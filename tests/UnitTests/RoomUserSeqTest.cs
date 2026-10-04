using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using TikTokLive.Proto;
using Xunit;

namespace UnitTests
{
    public class RoomUserSeqTest
    {
        private static Contributor Ranked(long rank, long score, long userId, string nick)
            => new Contributor { Rank = rank, Score = score, User = new UserIdentity { UserId = userId, Nickname = nick } };

        private static WebcastRoomUserSeqMessage RoundTrip(WebcastRoomUserSeqMessage msg)
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, msg);
            ms.Position = 0;
            return Serializer.Deserialize<WebcastRoomUserSeqMessage>(ms);
        }

        [Fact]
        public void DecodesRanksListAndSortsTopViewersByRank()
        {
            var wire = new WebcastRoomUserSeqMessage
            {
                RanksList = new List<Contributor>
                {
                    Ranked(3, 10, 300, "third"),
                    new Contributor { Rank = 0, Score = 999 }, // no user — skipped
                    Ranked(1, 5000, 100, "first"),
                    Ranked(2, 1200, 200, "second"),
                },
                ViewerCount = 321,
                TotalUser = 4567,
                Anonymous = 12,
            };

            WebcastRoomUserSeqMessage decoded = RoundTrip(wire);

            Assert.Equal(4, decoded.RanksList.Count);
            Assert.Equal(321, decoded.ViewerCount);
            Assert.Equal(4567, decoded.TotalUser);
            Assert.Equal(12, decoded.Anonymous);

            List<Contributor> top = decoded.TopViewers();
            Assert.Equal(new long[] { 1, 2, 3 }, top.Select(c => c.Rank).ToArray());
            Assert.Equal(new[] { "first", "second", "third" }, top.Select(c => c.User!.Nickname).ToArray());
            Assert.Equal(5000, top[0].Score);
            Assert.Equal(100, top[0].User!.UserId);
        }

        [Fact]
        public void TopViewersEmptyWhenNoRanks()
        {
            Assert.Empty(new WebcastRoomUserSeqMessage().TopViewers());
        }
    }
}
