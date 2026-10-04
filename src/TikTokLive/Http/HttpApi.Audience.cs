using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Errors;

namespace TikTokLive.Http
{
    public class RoomAudience
    {
        public long Total { get; set; }
        public long Anonymous { get; set; }
        public List<AudienceViewer> Viewers { get; set; } = new List<AudienceViewer>();
        public string RawJson { get; set; } = "";
    }

    public class AudienceViewer
    {
        public long Rank { get; set; }
        public long Score { get; set; }
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string SecUid { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public long FollowerCount { get; set; }
        public bool Verified { get; set; }
        /// <summary>Follows the streamer.</summary>
        public bool IsFollower { get; set; }
        /// <summary>The streamer follows them.</summary>
        public bool IsFollowing { get; set; }
        public bool IsSubscriber { get; set; }
    }

    public static partial class HttpApi
    {
        private const long StatusSessionRequired = 20003;

        /// <summary>
        /// Fetch the full audience roster: every named viewer currently in the room —
        /// the whole viewer panel, not just the top-3 box (for that, see
        /// <see cref="Proto.WebcastRoomUserSeqMessage.TopViewers"/>, which needs no cookies).
        ///
        /// TikTok gates this endpoint behind a login: pass session cookies
        /// ("sessionid=xxx; sid_tt=xxx") or you get <see cref="SessionRequiredException"/>.
        /// No ttwid, msToken, or signing needed.
        ///
        /// <paramref name="anchorId"/> is the streamer's user ID (<see cref="RoomIdResult.AnchorId"/>).
        /// Pass null to auto-resolve it from room info (one extra request).
        /// </summary>
        public static async Task<RoomAudience> FetchRoomAudienceAsync(
            string roomId, string? anchorId, TimeSpan timeout, string? cookies = null,
            IWebProxy? proxy = null, string? language = null, string? region = null,
            CancellationToken ct = default)
        {
            string anchor = anchorId ?? await ResolveAnchorIdAsync(
                roomId, timeout, cookies, proxy, language, region, ct).ConfigureAwait(false);

            string lang = language ?? UserAgent.SystemLanguage();
            string reg = region ?? UserAgent.SystemRegion();
            string url = TikTokUrlWebcast +
                "ranklist/online_audience/?aid=1988&app_name=tiktok_web&device_platform=web_pc" +
                $"&app_language={lang}&browser_language={lang}-{reg}&channel=tiktok_web" +
                $"&room_id={roomId}&anchor_id={anchor}";

            using (var client = BuildClient(timeout, cookies, proxy))
            using (var response = await client.GetAsync(url, ct).ConfigureAwait(false))
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseRoomAudience(body, (int)response.StatusCode);
            }
        }

        private static async Task<string> ResolveAnchorIdAsync(
            string roomId, TimeSpan timeout, string? cookies, IWebProxy? proxy,
            string? language, string? region, CancellationToken ct)
        {
            RoomInfo info = await FetchRoomInfoAsync(roomId, timeout, cookies, proxy, language, region, ct)
                .ConfigureAwait(false);
            using (JsonDocument doc = JsonDocument.Parse(info.RawJson))
            {
                if (doc.RootElement.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("owner", out JsonElement owner))
                {
                    string id = GetString(owner, "id_str");
                    if (id.Length > 0)
                        return id;
                }
            }
            throw new ProtocolException("no owner id in room info");
        }

        internal static RoomAudience ParseRoomAudience(string body, int httpStatus)
        {
            if (string.IsNullOrEmpty(body))
                throw new ProtocolException($"empty response from online_audience (HTTP {httpStatus})");

            using (JsonDocument doc = JsonDocument.Parse(body))
            {
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("status_code", out JsonElement sc) || !sc.TryGetInt64(out long code))
                    throw new ProtocolException("no status_code in online_audience response");

                if (code == StatusSessionRequired)
                    throw new SessionRequiredException(
                        "audience roster needs login — pass session cookies to FetchRoomAudienceAsync()");

                bool hasData = root.TryGetProperty("data", out JsonElement data)
                               && data.ValueKind == JsonValueKind.Object;
                if (code != 0)
                {
                    string message = hasData ? GetString(data, "message") : "";
                    throw new ProtocolException($"online_audience status_code={code} {message}");
                }
                if (!hasData)
                    throw new ProtocolException("missing 'data' in online_audience");

                var audience = new RoomAudience
                {
                    Total = GetLong(data, "total"),
                    Anonymous = GetLong(data, "anonymous"),
                    RawJson = body,
                };
                if (data.TryGetProperty("ranks", out JsonElement ranks) && ranks.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement rank in ranks.EnumerateArray())
                    {
                        if (rank.TryGetProperty("user", out JsonElement user) && user.ValueKind == JsonValueKind.Object)
                            audience.Viewers.Add(ParseAudienceViewer(rank, user));
                    }
                }
                return audience;
            }
        }

        private static AudienceViewer ParseAudienceViewer(JsonElement rank, JsonElement user)
        {
            string userId = GetString(user, "id_str");
            if (userId.Length == 0)
                userId = GetIdString(user, "id");

            long followers = 0;
            if (user.TryGetProperty("follow_info", out JsonElement follow))
                followers = GetLong(follow, "follower_count");

            return new AudienceViewer
            {
                Rank = GetLong(rank, "rank"),
                Score = GetLong(rank, "score"),
                UserId = userId,
                Username = GetString(user, "display_id"),
                Nickname = GetString(user, "nickname"),
                SecUid = GetString(user, "sec_uid"),
                AvatarUrl = FirstAvatarUrl(user),
                FollowerCount = followers,
                Verified = GetBool(user, "verified"),
                IsFollower = GetBool(user, "is_follower"),
                IsFollowing = GetBool(user, "is_following"),
                IsSubscriber = GetBool(user, "is_subscribe"),
            };
        }

        private static string? FirstAvatarUrl(JsonElement user)
        {
            if (!user.TryGetProperty("avatar_thumb", out JsonElement thumb)) return null;
            if (!thumb.TryGetProperty("url_list", out JsonElement urls)) return null;
            if (urls.ValueKind != JsonValueKind.Array || urls.GetArrayLength() == 0) return null;
            JsonElement first = urls[0];
            return first.ValueKind == JsonValueKind.String ? first.GetString() : null;
        }

        private static bool GetBool(JsonElement el, string prop)
            => el.TryGetProperty(prop, out JsonElement v) && v.ValueKind == JsonValueKind.True;
    }
}
