// Gift streak tracker — shows per-event deltas for combo gifts
// using the GiftStreakTracker helper.
//
// Usage:
//   dotnet run -- <tiktok_username>

using System;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Helpers;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: GiftStreak <tiktok_username>");
            return;
        }

        string username = args[0];
        var client = new TikTokLiveClient(username);
        var tracker = new GiftStreakTracker();
        long totalDiamonds = 0;

        client.OnConnected += (roomId) =>
            Console.WriteLine($"Connected to @{username} (room {roomId})\n");

        client.OnGift += (gift) =>
        {
            var e = tracker.Process(gift);

            string nick = gift.User != null ? gift.User.Nickname : "?";
            string name = gift.GiftDetails != null ? gift.GiftDetails.GiftName : "?";

            if (e.IsFinal)
            {
                totalDiamonds += e.TotalDiamondCount;
                Console.WriteLine($"[FINAL] streak={e.StreakId} {nick} -> {name} x{e.TotalGiftCount} — {e.TotalDiamondCount} diamonds");
                Console.WriteLine($"        running total: {totalDiamonds} diamonds\n");
            }
            else if (e.EventGiftCount > 0)
            {
                Console.WriteLine($"[ongoing] streak={e.StreakId} {nick} -> {name} +{e.EventGiftCount} (+{e.EventDiamondCount} dmnd)");
            }
        };

        client.OnDisconnected += () =>
        {
            Console.WriteLine($"\nFinal total: {totalDiamonds} diamonds");
            Console.WriteLine($"Active streaks at disconnect: {tracker.ActiveStreaks()}");
        };

        using (var cts = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (_, ev) => { ev.Cancel = true; cts.Cancel(); };
            try
            {
                await client.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"\nFinal total: {totalDiamonds} diamonds");
                Console.WriteLine($"Active streaks at disconnect: {tracker.ActiveStreaks()}");
            }
        }
    }
}
