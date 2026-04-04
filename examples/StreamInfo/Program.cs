// Stream info — fetches room info and stream URLs, then exits.
//
// Usage:
//   dotnet run -- <tiktok_username> [cookies]
//
// For 18+ rooms (needs session cookies):
//   dotnet run -- username "sessionid=abc; sid_tt=abc"

using System;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Http;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: StreamInfo <tiktok_username> [cookies]");
            return;
        }

        string username = args[0];
        string? cookies = args.Length > 1 ? args[1] : null;
        TimeSpan timeout = TimeSpan.FromSeconds(10);

        RoomIdResult room;
        try
        {
            room = await TikTokLiveClient.CheckOnlineAsync(username, timeout);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return;
        }

        try
        {
            RoomInfo info = await TikTokLiveClient.FetchRoomInfoAsync(room.RoomId, timeout, cookies);

            Console.WriteLine("=== Room Info ===");
            Console.WriteLine($"Username: @{username}");
            Console.WriteLine($"Room ID:  {room.RoomId}");
            Console.WriteLine($"Title:    {info.Title}");
            Console.WriteLine($"Viewers:  {info.Viewers}");
            Console.WriteLine($"Likes:    {info.Likes}");
            Console.WriteLine($"Total:    {info.TotalViewers} unique viewers");

            if (info.StreamUrl != null)
            {
                Console.WriteLine();
                Console.WriteLine("=== Stream URLs (FLV) ===");
                if (info.StreamUrl.FlvOrigin != null) Console.WriteLine($"Origin: {info.StreamUrl.FlvOrigin}");
                if (info.StreamUrl.FlvHd != null) Console.WriteLine($"HD:     {info.StreamUrl.FlvHd}");
                if (info.StreamUrl.FlvSd != null) Console.WriteLine($"SD:     {info.StreamUrl.FlvSd}");
                if (info.StreamUrl.FlvLd != null) Console.WriteLine($"LD:     {info.StreamUrl.FlvLd}");
                if (info.StreamUrl.FlvAo != null) Console.WriteLine($"Audio:  {info.StreamUrl.FlvAo}");
            }
            else
            {
                Console.WriteLine("\nNo stream URLs available.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Room info failed: {ex.Message}");
            if (cookies == null)
                Console.Error.WriteLine("Hint: if this is an 18+ room, pass session cookies as the second argument");
        }
    }
}
