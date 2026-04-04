// Gift tracker — shows all gifts with proper streak/combo handling.
//
// Usage:
//   dotnet run -- <tiktok_username>

using System;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive;

class Program
{
    static long _totalDiamonds;

    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: GiftTracker <tiktok_username>");
            return;
        }

        string username = args[0];
        var client = new TikTokLiveClient(username);

        client.OnConnected += (roomId) =>
            Console.WriteLine($"Connected to @{username} (room {roomId})! Tracking gifts...\n");

        client.OnGift += (gift) =>
        {
            string nickname = gift.User != null ? gift.User.Nickname : "unknown";
            string giftName = gift.GiftDetails != null ? gift.GiftDetails.GiftName : "unknown gift";
            long diamonds = gift.DiamondTotal();

            if (gift.IsComboGift())
            {
                if (gift.IsStreakOver())
                {
                    _totalDiamonds += diamonds;
                    Console.WriteLine($"[GIFT] {nickname} sent {giftName} x{gift.RepeatCount} — {diamonds} diamonds (streak ended)");
                    Console.WriteLine($"       Running total: {_totalDiamonds} diamonds\n");
                }
            }
            else
            {
                _totalDiamonds += diamonds;
                Console.WriteLine($"[GIFT] {nickname} sent {giftName} — {diamonds} diamonds");
                Console.WriteLine($"       Running total: {_totalDiamonds} diamonds\n");
            }
        };

        client.OnDisconnected += () =>
        {
            Console.WriteLine("Stream ended.");
            Console.WriteLine($"\nFinal total: {_totalDiamonds} diamonds");
        };

        using (var cts = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            try { await client.RunAsync(cts.Token); }
            catch (OperationCanceledException) { Console.WriteLine($"\nFinal total: {_totalDiamonds} diamonds"); }
        }
    }
}
