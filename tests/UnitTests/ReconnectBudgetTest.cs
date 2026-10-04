using System;
using TikTokLive.Connection;
using Xunit;

namespace UnitTests
{
    public class ReconnectBudgetTest
    {
        private static readonly TimeSpan Short = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan Long = TimeSpan.FromSeconds(45);

        [Fact]
        public void ConsecutiveFailuresAccumulateUntilGiveUp()
        {
            var budget = new ReconnectBudget(3);
            Verdict v1 = budget.Record(AttemptEnd.Failed);
            Verdict v2 = budget.Record(AttemptEnd.Failed);
            Verdict v3 = budget.Record(AttemptEnd.Failed);
            Verdict v4 = budget.Record(AttemptEnd.Failed);
            Assert.Equal((false, 1, 2.0), (v1.GiveUp, v1.Attempt, v1.Delay.TotalSeconds));
            Assert.Equal((false, 2, 4.0), (v2.GiveUp, v2.Attempt, v2.Delay.TotalSeconds));
            Assert.Equal((false, 3, 8.0), (v3.GiveUp, v3.Attempt, v3.Delay.TotalSeconds));
            Assert.True(v4.GiveUp);
            Assert.Equal(4, v4.Attempt);
        }

        [Fact]
        public void HealthySessionResetsTheCount()
        {
            var budget = new ReconnectBudget(3);
            budget.Record(AttemptEnd.Failed);
            budget.Record(AttemptEnd.Failed);
            budget.Record(AttemptEnd.Failed);
            Verdict afterHealthy = budget.Record(AttemptEnd.Healthy);
            Assert.False(afterHealthy.GiveUp);
            Assert.Equal(1, afterHealthy.Attempt);
            // a lifetime of blips separated by healthy sessions never exhausts the budget
            for (int i = 0; i < 20; i++)
            {
                Assert.False(budget.Record(AttemptEnd.Failed).GiveUp);
                Assert.False(budget.Record(AttemptEnd.Healthy).GiveUp);
            }
        }

        [Fact]
        public void BlockedUsesShortDelayAndStillCounts()
        {
            var budget = new ReconnectBudget(2);
            Verdict v = budget.Record(AttemptEnd.Blocked);
            Assert.Equal(ReconnectBudget.DeviceBlockedDelay, v.Delay);
            Assert.Equal(1, v.Attempt);
            budget.Record(AttemptEnd.Blocked);
            Assert.True(budget.Record(AttemptEnd.Blocked).GiveUp);
        }

        [Fact]
        public void BackoffCapsAtThirtySeconds()
        {
            Assert.Equal(TimeSpan.FromSeconds(16), ReconnectBudget.Backoff(4));
            Assert.Equal(TimeSpan.FromSeconds(30), ReconnectBudget.Backoff(5));
            Assert.Equal(TimeSpan.FromSeconds(30), ReconnectBudget.Backoff(int.MaxValue));
        }

        [Fact]
        public void DeviceBlockedRotates()
        {
            Judgement j = ReconnectBudget.Judge(SessionExit.DeviceBlocked, Long);
            Assert.Equal(AttemptEnd.Blocked, j.End);
            Assert.True(j.Rotate);
        }

        [Fact]
        public void TtwidFailureIsAFailedAttemptAndRefetches()
        {
            Judgement j = ReconnectBudget.Judge(SessionExit.NoTtwid, TimeSpan.Zero);
            Assert.Equal(AttemptEnd.Failed, j.End);
            Assert.True(j.Rotate);
        }

        [Fact]
        public void ErrorThatDiedYoungRotatesButHealthyErrorKeeps()
        {
            Judgement young = ReconnectBudget.Judge(SessionExit.Errored, Short);
            Assert.Equal(AttemptEnd.Failed, young.End);
            Assert.True(young.Rotate);

            Judgement healthy = ReconnectBudget.Judge(SessionExit.Errored, Long);
            Assert.Equal(AttemptEnd.Healthy, healthy.End);
            Assert.False(healthy.Rotate);
        }

        [Fact]
        public void CleanCloseKeepsCredentials()
        {
            Assert.False(ReconnectBudget.Judge(SessionExit.Closed, Short).Rotate);
            Assert.False(ReconnectBudget.Judge(SessionExit.Closed, Long).Rotate);
            Assert.Equal(AttemptEnd.Healthy, ReconnectBudget.Judge(SessionExit.Closed, Long).End);
            Assert.Equal(AttemptEnd.Failed, ReconnectBudget.Judge(SessionExit.Closed, Short).End);
        }
    }
}
