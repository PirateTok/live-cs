// Minimal chat reader — connects to a TikTok Live stream and prints chat messages.
//
// Usage:
//   dotnet run -- <tiktok_username>
//
// Example:
//   dotnet run -- hacker_lautar

using System;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: BasicChat <tiktok_username>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  BasicChat hacker_lautar");
            return;
        }

        string username = args[0];
        Console.WriteLine($"Connecting to @{username}...");

        var client = new TikTokLiveClient(username);

        client.OnConnected += (roomId) =>
            Console.WriteLine($"Connected to room {roomId}! Waiting for chat messages...\n");

        client.OnChat += (msg) =>
        {
            string nickname = msg.User != null ? msg.User.Nickname : "?";
            Console.WriteLine($"{nickname}: {msg.Comment}");
        };

        client.OnDisconnected += () =>
            Console.WriteLine("Stream ended.");

        using (var cts = new CancellationTokenSource())
        {
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
            try
            {
                await client.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nDisconnected.");
            }
        }
    }
}
