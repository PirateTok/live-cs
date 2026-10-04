using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Errors;
using TikTokLive.Http;

namespace TikTokLive.Auth
{
    internal static class TtwidAuth
    {
        private const string TikTokUrl = "https://www.tiktok.com/";

        // TikTok only sets ttwid on ~1 in 5-8 anonymous GETs — retry when it's absent.
        public const int FetchAttempts = 8;
        public static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(750);

        public static Task<string> FetchTtwidAsync(
            TimeSpan timeout, string? userAgent = null, IWebProxy? proxy = null,
            CancellationToken ct = default)
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            if (proxy != null)
            {
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }
            return FetchTtwidAsync(handler, TikTokUrl, timeout, userAgent ?? UserAgent.RandomUa(),
                FetchAttempts, RetryDelay, ct);
        }

        /// <summary>
        /// Retries only when the response carries no ttwid cookie; transport errors propagate.
        /// </summary>
        internal static async Task<string> FetchTtwidAsync(
            HttpMessageHandler handler, string url, TimeSpan timeout, string userAgent,
            int attempts, TimeSpan retryDelay, CancellationToken ct)
        {
            using (handler)
            using (var client = new HttpClient(handler) { Timeout = timeout })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                for (int attempt = 1; ; attempt++)
                {
                    using (var response = await client.GetAsync(url, ct).ConfigureAwait(false))
                    {
                        string? ttwid = FindTtwid(response);
                        if (ttwid != null)
                            return ttwid;
                        if (attempt >= attempts)
                            throw new TikTokLiveException(
                                $"no ttwid cookie after {attempt} attempts (last HTTP {(int)response.StatusCode})");
                    }
                    await Task.Delay(retryDelay, ct).ConfigureAwait(false);
                }
            }
        }

        private static string? FindTtwid(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
                return null;
            foreach (string cookie in cookies)
            {
                string? ttwid = ExtractTtwid(cookie);
                if (!string.IsNullOrEmpty(ttwid))
                    return ttwid;
            }
            return null;
        }

        private static string? ExtractTtwid(string setCookieHeader)
        {
            if (!setCookieHeader.StartsWith("ttwid=", StringComparison.Ordinal))
                return null;

            int end = setCookieHeader.IndexOf(';');
            if (end < 0)
                return setCookieHeader.Substring(6);

            return setCookieHeader.Substring(6, end - 6);
        }
    }
}
