using System;
using System.Threading.Tasks;
using TikTokLive.Errors;
using TikTokLive.Http;

class Program
{
    static async Task Main(string[] args)
    {
        string username = args.Length > 0 ? args[0] : "tiktok";
        var cache = new ProfileCache();

        Console.WriteLine($"Fetching profile for @{username}...");
        try
        {
            SigiProfile p = await cache.FetchAsync(username);
            Console.WriteLine($"  User ID:    {p.UserId}");
            Console.WriteLine($"  Nickname:   {p.Nickname}");
            Console.WriteLine($"  Verified:   {p.Verified}");
            Console.WriteLine($"  Followers:  {p.FollowerCount}");
            Console.WriteLine($"  Videos:     {p.VideoCount}");
            Console.WriteLine($"  Avatar (thumb):  {p.AvatarThumb}");
            Console.WriteLine($"  Avatar (720):    {p.AvatarMedium}");
            Console.WriteLine($"  Avatar (1080):   {p.AvatarLarge}");
            Console.WriteLine($"  Bio link:   {p.BioLink ?? "(none)"}");
            Console.WriteLine($"  Room ID:    {(string.IsNullOrEmpty(p.RoomId) ? "(offline)" : p.RoomId)}");

            Console.WriteLine($"\nFetching @{username} again (should be cached)...");
            SigiProfile p2 = await cache.FetchAsync(username);
            Console.WriteLine($"  [cached] {p2.Nickname} — {p2.FollowerCount} followers");
        }
        catch (ProfilePrivateException)
        {
            Console.WriteLine($"  @{username} is a private account");
        }
        catch (ProfileNotFoundException)
        {
            Console.WriteLine($"  @{username} does not exist");
        }
    }
}
