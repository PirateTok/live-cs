using System;

namespace TikTokLive.Http
{
    /// <summary>
    /// User agent pool and system timezone detection.
    /// Rotated on each reconnect to reduce DEVICE_BLOCKED risk.
    /// </summary>
    internal static class UserAgent
    {
        private static readonly string[] Pool =
        {
            "Mozilla/5.0 (X11; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:138.0) Gecko/20100101 Firefox/138.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:139.0) Gecko/20100101 Firefox/139.0",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        };

        [ThreadStatic]
        private static Random? _rng;

        /// <summary>
        /// Pick a random user agent from the built-in pool.
        /// </summary>
        public static string RandomUa()
        {
            if (_rng == null) _rng = new Random();
            return Pool[_rng.Next(Pool.Length)];
        }

        /// <summary>
        /// Detect the system's IANA timezone name.
        /// On Linux/macOS, TimeZoneInfo.Local.Id returns IANA names (e.g. "Europe/Berlin").
        /// On Windows, it returns Windows timezone IDs (e.g. "W. Europe Standard Time"),
        /// which lack a "/" — we fall back to "UTC" in that case.
        /// </summary>
        public static string SystemTimezone()
        {
            string id = TimeZoneInfo.Local.Id;
            if (!string.IsNullOrEmpty(id) && id.Contains("/"))
                return id;
            return "UTC";
        }
    }
}
