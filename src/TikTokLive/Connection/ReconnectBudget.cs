using System;

namespace TikTokLive.Connection
{
    internal enum SessionExit { Closed, DeviceBlocked, Errored, NoTtwid }

    internal enum AttemptEnd { Healthy, Failed, Blocked }

    internal readonly struct Judgement
    {
        public AttemptEnd End { get; }
        /// <summary>true = drop ttwid + UA and fetch fresh ones next attempt.</summary>
        public bool Rotate { get; }

        public Judgement(AttemptEnd end, bool rotate)
        {
            End = end;
            Rotate = rotate;
        }
    }

    internal readonly struct Verdict
    {
        public bool GiveUp { get; }
        public int Attempt { get; }
        public TimeSpan Delay { get; }

        public Verdict(bool giveUp, int attempt, TimeSpan delay)
        {
            GiveUp = giveUp;
            Attempt = attempt;
            Delay = delay;
        }
    }

    /// <summary>
    /// Counts consecutive failed attempts. A session that stayed up for
    /// <see cref="HealthySession"/> resets the count, so long-lived streams don't
    /// die after max_retries lifetime blips.
    /// </summary>
    internal sealed class ReconnectBudget
    {
        public static readonly TimeSpan HealthySession = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DeviceBlockedDelay = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

        private readonly int _maxRetries;

        public int Attempt { get; private set; }

        public ReconnectBudget(int maxRetries) => _maxRetries = maxRetries;

        public static Judgement Judge(SessionExit exit, TimeSpan lived)
        {
            bool healthy = lived >= HealthySession;
            AttemptEnd end = healthy ? AttemptEnd.Healthy : AttemptEnd.Failed;
            switch (exit)
            {
                case SessionExit.Closed: return new Judgement(end, false);
                case SessionExit.DeviceBlocked: return new Judgement(AttemptEnd.Blocked, true);
                case SessionExit.Errored: return new Judgement(end, !healthy);
                case SessionExit.NoTtwid: return new Judgement(AttemptEnd.Failed, true);
                default: throw new ArgumentOutOfRangeException(nameof(exit), exit, "unknown session exit");
            }
        }

        public Verdict Record(AttemptEnd end)
        {
            Attempt = end == AttemptEnd.Healthy ? 1 : Attempt + 1;
            if (Attempt > _maxRetries)
                return new Verdict(true, Attempt, TimeSpan.Zero);
            TimeSpan delay = end == AttemptEnd.Blocked ? DeviceBlockedDelay : Backoff(Attempt);
            return new Verdict(false, Attempt, delay);
        }

        public static TimeSpan Backoff(int attempt)
        {
            if (attempt >= 5)
                return MaxBackoff;
            return TimeSpan.FromSeconds(1 << attempt);
        }
    }
}
