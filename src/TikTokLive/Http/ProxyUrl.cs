using System;
using System.Net;

namespace TikTokLive.Http
{
    public static class ProxyUrl
    {
        /// <summary>
        /// Parses <c>scheme://[user:pass@]host:port</c> (http, https, socks4, socks4a, socks5) into a
        /// <see cref="WebProxy"/>. <c>new WebProxy(url)</c> silently drops the userinfo, so credentials
        /// are carried over explicitly.
        /// </summary>
        public static WebProxy Parse(string url)
        {
            var uri = new Uri(url);
            var proxy = new WebProxy(new UriBuilder(uri) { UserName = "", Password = "" }.Uri);
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] parts = uri.UserInfo.Split(new[] { ':' }, 2);
                proxy.Credentials = new NetworkCredential(
                    Uri.UnescapeDataString(parts[0]),
                    parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "");
            }
            return proxy;
        }
    }
}
