using System;

namespace TikTokLive.Connection
{
    internal static class WssUrlBuilder
    {
        public static string Build(string cdnHost, string roomId, string timezone,
            string language = "en", string region = "US", bool compress = true)
        {
            double lastRtt = 100.0 + new Random().NextDouble() * 100.0;
            string rttStr = lastRtt.ToString("F3");
            string browserLanguage = $"{language}-{region}";

            string query = string.Join("&", new[]
            {
                "version_code=180800",
                "device_platform=web",
                "cookie_enabled=true",
                "screen_width=1920",
                "screen_height=1080",
                "browser_language=" + browserLanguage,
                "browser_platform=" + Uri.EscapeDataString("Linux x86_64"),
                "browser_name=Mozilla",
                "browser_version=" + Uri.EscapeDataString("5.0 (X11)"),
                "browser_online=true",
                "tz_name=" + Uri.EscapeDataString(timezone),
                "app_name=tiktok_web",
                "sup_ws_ds_opt=1",
                "update_version_code=2.0.0",
                "compress=" + (compress ? "gzip" : ""),
                "webcast_language=" + language,
                "ws_direct=1",
                "aid=1988",
                "live_id=12",
                "app_language=" + language,
                "client_enter=1",
                "room_id=" + roomId,
                "identity=audience",
                "history_comment_count=6",
                "last_rtt=" + rttStr,
                "heartbeat_duration=10000",
                "resp_content_type=protobuf",
                "did_rule=3",
            });

            return $"wss://{cdnHost}/webcast/im/ws_proxy/ws_reuse_supplement/?{query}";
        }
    }
}
