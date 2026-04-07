using TikTokLive.Proto;

namespace TikTokLive.Helpers
{
    public class LikeStats
    {
        public int EventLikeCount { get; set; }
        public int TotalLikeCount { get; set; }
        public long AccumulatedCount { get; set; }
        public bool WentBackwards { get; set; }
    }

    /// <summary>
    /// Monotonizes TikTok's inconsistent total_like_count.
    /// Not thread-safe — use from a single event-handling thread.
    /// </summary>
    public class LikeAccumulator
    {
        private int _maxTotal;
        private long _accumulated;

        public LikeStats Process(WebcastLikeMessage msg)
        {
            _accumulated += msg.LikeCount;
            bool wentBackwards = msg.TotalLikeCount < _maxTotal;
            if (msg.TotalLikeCount > _maxTotal)
                _maxTotal = msg.TotalLikeCount;

            return new LikeStats
            {
                EventLikeCount = msg.LikeCount,
                TotalLikeCount = _maxTotal,
                AccumulatedCount = _accumulated,
                WentBackwards = wentBackwards,
            };
        }

        public void Reset()
        {
            _maxTotal = 0;
            _accumulated = 0;
        }
    }
}
