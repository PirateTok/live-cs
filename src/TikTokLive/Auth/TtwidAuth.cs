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

        public static async Task<string> FetchTtwidAsync(
            TimeSpan timeout, string? userAgent = null, CancellationToken ct = default)
        {
            string ua = userAgent ?? UserAgent.RandomUa();

            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler) { Timeout = timeout })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
                using (var response = await client.GetAsync(TikTokUrl, ct).ConfigureAwait(false))
                {
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                    {
                        foreach (string cookie in cookies)
                        {
                            string? ttwid = ExtractTtwid(cookie);
                            if (ttwid != null)
                                return ttwid;
                        }
                    }

                    throw new TikTokLiveException("no ttwid cookie in tiktok.com response");
                }
            }
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
