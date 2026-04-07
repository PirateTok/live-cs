using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Errors;

namespace TikTokLive.Http
{
    public class RoomIdResult
    {
        public string RoomId { get; }
        public RoomIdResult(string roomId) => RoomId = roomId;
    }

    public class RoomInfo
    {
        public string Title { get; set; } = "";
        public long Viewers { get; set; }
        public long Likes { get; set; }
        public long TotalViewers { get; set; }
        public StreamUrl? StreamUrl { get; set; }
        public string RawJson { get; set; } = "";
    }

    public class StreamUrl
    {
        public string? FlvOrigin { get; set; }
        public string? FlvHd { get; set; }
        public string? FlvSd { get; set; }
        public string? FlvLd { get; set; }
        public string? FlvAo { get; set; }
    }

    public static class HttpApi
    {
        private const string TikTokUrlWeb = "https://www.tiktok.com/";
        private const string TikTokUrlWebcast = "https://webcast.tiktok.com/webcast/";

        public static async Task<RoomIdResult> CheckOnlineAsync(
            string username, TimeSpan timeout, CancellationToken ct = default)
        {
            string clean = username.Trim().TrimStart('@');
            string url = TikTokUrlWeb +
                "api-live/user/room?aid=1988&app_name=tiktok_web&device_platform=web_pc" +
                "&app_language=en&browser_language=en-US&user_is_login=false" +
                $"&uniqueId={Uri.EscapeDataString(clean)}&sourceType=54&staleTime=600000";

            using (var client = BuildClient(timeout, null))
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url, ct).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw new TikTokBlockedException(0, $"HTTP request failed: {ex.Message}");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == (HttpStatusCode)429)
                {
                    throw new TikTokBlockedException(
                        (int)response.StatusCode,
                        "rate-limited or geo-blocked");
                }

                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(body))
                    throw new TikTokBlockedException((int)response.StatusCode, "empty response");

                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    JsonElement root = doc.RootElement;

                    if (!root.TryGetProperty("statusCode", out JsonElement statusEl))
                        throw new ProtocolException("no statusCode in response");

                    long statusCode = statusEl.GetInt64();
                    if (statusCode == 19881007)
                        throw new UserNotFoundException(clean);
                    if (statusCode != 0)
                        throw new TikTokApiException(statusCode);

                    string roomId = "";
                    if (root.TryGetProperty("data", out JsonElement data) &&
                        data.TryGetProperty("user", out JsonElement user) &&
                        user.TryGetProperty("roomId", out JsonElement roomIdEl))
                    {
                        roomId = roomIdEl.GetString() ?? "";
                    }

                    if (string.IsNullOrEmpty(roomId) || roomId == "0")
                        throw new HostNotOnlineException("no active room");

                    long liveStatus = 0;
                    if (data.TryGetProperty("liveRoom", out JsonElement liveRoom) &&
                        liveRoom.TryGetProperty("status", out JsonElement statusProp))
                    {
                        liveStatus = statusProp.GetInt64();
                    }
                    else if (data.TryGetProperty("user", out JsonElement u2) &&
                             u2.TryGetProperty("status", out JsonElement st2))
                    {
                        liveStatus = st2.GetInt64();
                    }

                    if (liveStatus != 2)
                        throw new HostNotOnlineException($"status={liveStatus}");

                    return new RoomIdResult(roomId);
                }
            }
        }

        public static async Task<RoomInfo> FetchRoomInfoAsync(
            string roomId, TimeSpan timeout, string? cookies = null, CancellationToken ct = default)
        {
            string tz = Uri.EscapeDataString(UserAgent.SystemTimezone());
            string url = TikTokUrlWebcast +
                "room/info/?aid=1988&app_name=tiktok_web&device_platform=web_pc" +
                "&app_language=en&browser_language=en-US&browser_name=Mozilla" +
                "&browser_online=true&browser_platform=Win32" +
                "&browser_version=5.0+(Windows+NT+10.0%3B+Win64%3B+x64)" +
                "&cookie_enabled=true&focus_state=true&from_page=user" +
                "&screen_height=1080&screen_width=1920" +
                "&tz_name=" + tz + "&webcast_language=en" +
                $"&room_id={roomId}";

            using (var client = BuildClient(timeout, cookies))
            {
                var response = await client.GetAsync(url, ct).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(body))
                    throw new ProtocolException($"empty response from room/info (HTTP {(int)response.StatusCode})");

                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("status_code", out JsonElement sc))
                    {
                        long code = sc.GetInt64();
                        if (code == 4003110)
                            throw new AgeRestrictedException("18+ room — pass session cookies to FetchRoomInfoAsync()");
                        if (code != 0)
                            throw new TikTokApiException(code);
                    }

                    if (!root.TryGetProperty("data", out JsonElement dataEl))
                        throw new ProtocolException("missing 'data' in room info");

                    string title = GetString(dataEl, "title");
                    long viewers = GetLong(dataEl, "user_count");

                    long likes = 0, totalViewers = 0;
                    if (dataEl.TryGetProperty("stats", out JsonElement stats))
                    {
                        likes = GetLong(stats, "like_count");
                        totalViewers = GetLong(stats, "total_user");
                    }

                    StreamUrl? streamUrl = ParseStreamUrls(root);

                    return new RoomInfo
                    {
                        Title = title,
                        Viewers = viewers,
                        Likes = likes,
                        TotalViewers = totalViewers,
                        StreamUrl = streamUrl,
                        RawJson = body,
                    };
                }
            }
        }

        private static StreamUrl? ParseStreamUrls(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("data", out JsonElement data)) return null;
                if (!data.TryGetProperty("stream_url", out JsonElement streamUrlEl)) return null;
                if (!streamUrlEl.TryGetProperty("live_core_sdk_data", out JsonElement sdk)) return null;
                if (!sdk.TryGetProperty("pull_data", out JsonElement pull)) return null;
                if (!pull.TryGetProperty("stream_data", out JsonElement sdStr)) return null;

                string? nestedJson = sdStr.GetString();
                if (string.IsNullOrEmpty(nestedJson)) return null;

                using (JsonDocument nested = JsonDocument.Parse(nestedJson!))
                {
                    JsonElement nd = nested.RootElement;
                    return new StreamUrl
                    {
                        FlvOrigin = GetNestedFlv(nd, "origin"),
                        FlvHd = GetNestedFlv(nd, "hd") ?? GetNestedFlv(nd, "uhd"),
                        FlvSd = GetNestedFlv(nd, "sd"),
                        FlvLd = GetNestedFlv(nd, "ld"),
                        FlvAo = GetNestedFlv(nd, "ao"),
                    };
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? GetNestedFlv(JsonElement root, string quality)
        {
            if (!root.TryGetProperty("data", out JsonElement data)) return null;
            if (!data.TryGetProperty(quality, out JsonElement q)) return null;
            if (!q.TryGetProperty("main", out JsonElement main)) return null;
            if (!main.TryGetProperty("flv", out JsonElement flv)) return null;
            return flv.GetString();
        }

        private static HttpClient BuildClient(TimeSpan timeout, string? cookies)
        {
            var client = new HttpClient { Timeout = timeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent.RandomUa());
            client.DefaultRequestHeaders.Referrer = new Uri("https://www.tiktok.com/");
            if (!string.IsNullOrEmpty(cookies))
                client.DefaultRequestHeaders.Add("Cookie", cookies);
            return client;
        }

        private static string GetString(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out JsonElement v))
                return v.GetString() ?? "";
            return "";
        }

        private static long GetLong(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out JsonElement v) && v.TryGetInt64(out long val))
                return val;
            return 0;
        }
    }
}
