using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Errors;

namespace TikTokLive.Http
{
    public class SigiProfile
    {
        public string UserId { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string Bio { get; set; } = "";
        public string AvatarThumb { get; set; } = "";
        public string AvatarMedium { get; set; } = "";
        public string AvatarLarge { get; set; } = "";
        public bool Verified { get; set; }
        public bool PrivateAccount { get; set; }
        public bool IsOrganization { get; set; }
        public string RoomId { get; set; } = "";
        public string? BioLink { get; set; }
        public long FollowerCount { get; set; }
        public long FollowingCount { get; set; }
        public long HeartCount { get; set; }
        public long VideoCount { get; set; }
        public long FriendCount { get; set; }
    }

    public static class Sigi
    {
        private const string SigiMarker = "id=\"__UNIVERSAL_DATA_FOR_REHYDRATION__\"";

        public static async Task<SigiProfile> ScrapeProfileAsync(
            string username,
            string ttwid,
            TimeSpan timeout,
            string? userAgent = null,
            string? cookies = null,
            IWebProxy? proxy = null,
            string? language = null,
            string? region = null,
            CancellationToken ct = default)
        {
            string clean = username.Trim().TrimStart('@').ToLowerInvariant();
            string ua = userAgent ?? UserAgent.RandomUa();
            string cookieHeader = BuildCookieHeader(ttwid, cookies);
            string lang = language ?? UserAgent.SystemLanguage();
            string reg = region ?? UserAgent.SystemRegion();
            string acceptLang = $"{lang}-{reg},{lang};q=0.9";

            var handler = new HttpClientHandler();
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            using (handler)
            using (var client = new HttpClient(handler) { Timeout = timeout })
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://www.tiktok.com/@{clean}");
                request.Headers.Add("User-Agent", ua);
                request.Headers.Add("Cookie", cookieHeader);
                request.Headers.Add("Accept-Language", acceptLang);

                using (var resp = await client.SendAsync(request, ct).ConfigureAwait(false))
                {
                    string html = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string jsonStr = ExtractSigiJson(html);

                    using (JsonDocument doc = JsonDocument.Parse(jsonStr))
                    {
                        return ParseProfile(doc.RootElement, clean);
                    }
                }
            }
        }

        private static SigiProfile ParseProfile(JsonElement root, string clean)
        {
            if (!root.TryGetProperty("__DEFAULT_SCOPE__", out JsonElement scope) ||
                !scope.TryGetProperty("webapp.user-detail", out JsonElement userDetail))
            {
                throw new ProfileScrapeException("missing __DEFAULT_SCOPE__/webapp.user-detail");
            }

            long statusCode = 0;
            if (userDetail.TryGetProperty("statusCode", out JsonElement scEl))
                statusCode = scEl.GetInt64();

            if (statusCode != 0)
            {
                if (statusCode == 10222) throw new ProfilePrivateException(clean);
                if (statusCode == 10221 || statusCode == 10223) throw new ProfileNotFoundException(clean);
                throw new ProfileErrorException(statusCode);
            }

            if (!userDetail.TryGetProperty("userInfo", out JsonElement userInfo))
                throw new ProfileScrapeException("missing userInfo");

            JsonElement user = userInfo.TryGetProperty("user", out JsonElement u) ? u : default;
            JsonElement stats = userInfo.TryGetProperty("stats", out JsonElement s) ? s : default;

            string? bioLink = null;
            if (user.TryGetProperty("bioLink", out JsonElement bl) &&
                bl.TryGetProperty("link", out JsonElement linkEl))
            {
                string? linkVal = linkEl.GetString();
                if (!string.IsNullOrEmpty(linkVal))
                    bioLink = linkVal;
            }

            return new SigiProfile
            {
                UserId = GetStr(user, "id"),
                UniqueId = GetStr(user, "uniqueId"),
                Nickname = GetStr(user, "nickname"),
                Bio = GetStr(user, "signature"),
                AvatarThumb = GetStr(user, "avatarThumb"),
                AvatarMedium = GetStr(user, "avatarMedium"),
                AvatarLarge = GetStr(user, "avatarLarger"),
                Verified = GetBool(user, "verified"),
                PrivateAccount = GetBool(user, "privateAccount"),
                IsOrganization = GetLong(user, "isOrganization") != 0,
                RoomId = GetStr(user, "roomId"),
                BioLink = bioLink,
                FollowerCount = GetLong(stats, "followerCount"),
                FollowingCount = GetLong(stats, "followingCount"),
                HeartCount = GetLong(stats, "heartCount"),
                VideoCount = GetLong(stats, "videoCount"),
                FriendCount = GetLong(stats, "friendCount"),
            };
        }

        private static string ExtractSigiJson(string html)
        {
            int markerPos = html.IndexOf(SigiMarker, StringComparison.Ordinal);
            if (markerPos < 0)
                throw new ProfileScrapeException("SIGI script tag not found in HTML");

            int gtPos = html.IndexOf('>', markerPos);
            if (gtPos < 0)
                throw new ProfileScrapeException("no > after SIGI marker");

            int jsonStart = gtPos + 1;
            int scriptEnd = html.IndexOf("</script>", jsonStart, StringComparison.Ordinal);
            if (scriptEnd < 0)
                throw new ProfileScrapeException("no </script> after SIGI JSON");

            string jsonStr = html.Substring(jsonStart, scriptEnd - jsonStart);
            if (string.IsNullOrEmpty(jsonStr))
                throw new ProfileScrapeException("empty SIGI JSON blob");

            return jsonStr;
        }

        private static string BuildCookieHeader(string ttwid, string? cookies)
        {
            if (string.IsNullOrEmpty(cookies))
                return $"ttwid={ttwid}";

            var parts = cookies!.Split(new[] { "; " }, StringSplitOptions.None);
            var filtered = new System.Collections.Generic.List<string>();
            foreach (string part in parts)
            {
                if (!part.StartsWith("ttwid=", StringComparison.Ordinal))
                    filtered.Add(part);
            }
            if (filtered.Count == 0)
                return $"ttwid={ttwid}";
            return $"ttwid={ttwid}; {string.Join("; ", filtered)}";
        }

        private static string GetStr(JsonElement el, string prop)
        {
            if (el.ValueKind != JsonValueKind.Undefined &&
                el.TryGetProperty(prop, out JsonElement v))
                return v.GetString() ?? "";
            return "";
        }

        private static bool GetBool(JsonElement el, string prop)
        {
            if (el.ValueKind != JsonValueKind.Undefined &&
                el.TryGetProperty(prop, out JsonElement v) &&
                v.ValueKind == JsonValueKind.True)
                return true;
            return false;
        }

        private static long GetLong(JsonElement el, string prop)
        {
            if (el.ValueKind != JsonValueKind.Undefined &&
                el.TryGetProperty(prop, out JsonElement v) &&
                v.TryGetInt64(out long val))
                return val;
            return 0;
        }
    }
}
