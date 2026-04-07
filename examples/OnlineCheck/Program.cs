// Online check — checks if a TikTok user is currently live.
//
// Usage:
//   dotnet run -- <tiktok_username> [username2] [username3] ...
//
// Example:
//   dotnet run -- tiktok

using System;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Errors;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: OnlineCheck <tiktok_username> [username2] ...");
            return;
        }

        TimeSpan timeout = TimeSpan.FromSeconds(10);

        foreach (string username in args)
        {
            try
            {
                var room = await TikTokLiveClient.CheckOnlineAsync(username, timeout);
                Console.WriteLine($"  LIVE  @{username} — room {room.RoomId}");
            }
            catch (UserNotFoundException)
            {
                Console.WriteLine($"  404   @{username} — user does not exist");
            }
            catch (HostNotOnlineException)
            {
                Console.WriteLine($"  OFF   @{username} — not currently live");
            }
            catch (TikTokBlockedException ex)
            {
                Console.WriteLine($"  BLOCK @{username} — {ex.Message}");
            }
            catch (TikTokApiException ex)
            {
                Console.WriteLine($"  ERR   @{username} — API error: statusCode={ex.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL  @{username} — {ex.Message}");
            }
        }
    }
}
