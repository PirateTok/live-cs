using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Events;

namespace TikTokLive.Connection
{
    /// <summary>ttwid + UA pair, reused across reconnects until rotated.</summary>
    internal sealed class LiveSession
    {
        public string Ttwid { get; }
        public string UserAgent { get; }

        public LiveSession(string ttwid, string userAgent)
        {
            Ttwid = ttwid;
            UserAgent = userAgent;
        }
    }

    /// <summary>
    /// The reconnect loop with its side effects injected: <c>fresh</c> fetches ttwid + UA (null = failed),
    /// <c>run</c> runs one WSS session, <c>delay</c> waits between attempts. ttwid + UA are reused and
    /// rotate only on DEVICE_BLOCKED, a ttwid failure, or a connection that died young.
    /// Emits Reconnecting per retry and Disconnected exactly once, last.
    /// </summary>
    internal static class ReconnectLoop
    {
        public static async Task RunAsync(
            int maxRetries,
            Func<CancellationToken, Task<LiveSession?>> fresh,
            Func<LiveSession, CancellationToken, Task<SessionExit>> run,
            Func<TimeSpan, CancellationToken, Task> delay,
            Action<TikTokLiveEvent> emit,
            CancellationToken ct)
        {
            var budget = new ReconnectBudget(maxRetries);
            LiveSession? held = null;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    SessionExit exit = SessionExit.NoTtwid;
                    TimeSpan lived = TimeSpan.Zero;
                    held ??= await fresh(ct).ConfigureAwait(false);
                    if (held != null)
                    {
                        var started = Stopwatch.StartNew();
                        exit = await run(held, ct).ConfigureAwait(false);
                        lived = started.Elapsed;
                    }
                    if (ct.IsCancellationRequested) break;

                    Judgement judgement = ReconnectBudget.Judge(exit, lived);
                    if (judgement.Rotate)
                        held = null;

                    Verdict verdict = budget.Record(judgement.End);
                    if (verdict.GiveUp) break;

                    emit(TikTokLiveEvent.Reconnecting(verdict.Attempt, maxRetries, (int)verdict.Delay.TotalSeconds));
                    await delay(verdict.Delay, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // user cancelled mid-fetch or mid-delay — fall through to the single Disconnected
            }
            emit(TikTokLiveEvent.Disconnected());
        }
    }
}
