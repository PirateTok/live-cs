// Audience — fetches the full viewer roster of a live room, then exits.
//
// TikTok gates this endpoint behind a login, so session cookies are required:
//   dotnet run -- <tiktok_username> "sessionid=abc; sid_tt=abc"

using System;
using System.Threading.Tasks;
using TikTokLive;
using TikTokLive.Errors;
using TikTokLive.Http;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Audience <tiktok_username> \"sessionid=xxx; sid_tt=xxx\"");
            return;
        }

        string username = args[0];
        string cookies = args[1];
        TimeSpan timeout = TimeSpan.FromSeconds(10);

        try
        {
            RoomIdResult room = await TikTokLiveClient.CheckOnlineAsync(username, timeout);
            RoomAudience audience = await TikTokLiveClient.FetchRoomAudienceAsync(
                room.RoomId, room.AnchorId, timeout, cookies);

            Console.WriteLine($"@{username} — {audience.Total} in room ({audience.Anonymous} anonymous), {audience.Viewers.Count} listed");
            foreach (AudienceViewer v in audience.Viewers)
            {
                Console.WriteLine($"#{v.Rank,-3} @{v.Username} ({v.Nickname}) score={v.Score} followers={v.FollowerCount}"
                    + (v.IsSubscriber ? " [sub]" : "") + (v.IsFollower ? " [follower]" : ""));
            }
        }
        catch (SessionRequiredException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine("Hint: copy sessionid + sid_tt from browser DevTools while logged in");
        }
        catch (TikTokLiveException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}
