using System;
using System.Collections.Generic;
using System.Linq;
using TikTokLive.Proto;

namespace TikTokLive.Helpers
{
    public class GiftStreakEvent
    {
        public ulong StreakId { get; set; }
        public bool IsActive { get; set; }
        public bool IsFinal { get; set; }
        public int EventGiftCount { get; set; }
        public int TotalGiftCount { get; set; }
        public long EventDiamondCount { get; set; }
        public long TotalDiamondCount { get; set; }
    }

    /// <summary>
    /// Tracks gift streak deltas from TikTok's running totals.
    /// Not thread-safe — use from a single event-handling thread.
    /// </summary>
    public class GiftStreakTracker
    {
        private const long StaleSecs = 60;

        private readonly Dictionary<ulong, (int lastRepeatCount, long lastSeenTicks)> _streaks = new();

        public GiftStreakEvent Process(WebcastGiftMessage msg)
        {
            int diamondPer = msg.GiftDetails?.DiamondCount ?? 0;
            bool isCombo = msg.IsComboGift();
            bool isFinal = msg.IsStreakOver();

            if (!isCombo)
            {
                return new GiftStreakEvent
                {
                    StreakId = msg.GroupId,
                    IsActive = false,
                    IsFinal = true,
                    EventGiftCount = 1,
                    TotalGiftCount = 1,
                    EventDiamondCount = diamondPer,
                    TotalDiamondCount = diamondPer,
                };
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            EvictStale(nowTicks);

            int prevCount = 0;
            if (_streaks.TryGetValue(msg.GroupId, out var prev))
                prevCount = prev.lastRepeatCount;

            int delta = Math.Max(msg.RepeatCount - prevCount, 0);

            if (isFinal)
                _streaks.Remove(msg.GroupId);
            else
                _streaks[msg.GroupId] = (msg.RepeatCount, nowTicks);

            long rc = Math.Max(msg.RepeatCount, 1);

            return new GiftStreakEvent
            {
                StreakId = msg.GroupId,
                IsActive = !isFinal,
                IsFinal = isFinal,
                EventGiftCount = delta,
                TotalGiftCount = msg.RepeatCount,
                EventDiamondCount = (long)diamondPer * delta,
                TotalDiamondCount = (long)diamondPer * rc,
            };
        }

        public int ActiveStreaks() => _streaks.Count;

        public void Reset() => _streaks.Clear();

        private void EvictStale(long nowTicks)
        {
            long cutoff = TimeSpan.FromSeconds(StaleSecs).Ticks;
            var stale = _streaks.Where(kv => (nowTicks - kv.Value.lastSeenTicks) >= cutoff)
                .Select(kv => kv.Key).ToList();
            foreach (ulong id in stale)
                _streaks.Remove(id);
        }
    }
}
