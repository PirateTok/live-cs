using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Auth;
using TikTokLive.Connection;
using TikTokLive.Http;
using Xunit;

namespace UnitTests
{
    /// <summary>F6: ttwid, API and WSS all go through the configured proxy, credentials included.</summary>
    public class ProxyTest
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
        private static readonly string Basic = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("user:pw"));

        private static IWebProxy Via(FakeProxy p, string scheme) => ProxyUrl.Parse($"{scheme}://user:pw@127.0.0.1:{p.Port}");

        private static async Task Refused(Func<Task> call)
        {
            Exception? ex = await Record.ExceptionAsync(call);
            Assert.NotNull(ex); // the fake refuses every tunnel
        }

        private static Task RunWss(IWebProxy proxy) =>
            new SocketLoop("wss://webcast-ws.tiktok.com/webcast/im/ws_proxy/ws_reuse_supplement/?room_id=7",
                "abc", "7", "ua", null, proxy, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), _ => { })
                .RunAsync(new CancellationTokenSource(Timeout).Token);

        [Fact]
        public void ParseKeepsCredentials()
        {
            WebProxy p = ProxyUrl.Parse("http://us%40er:p%3Aw@127.0.0.1:8080");
            Assert.Equal(new Uri("http://127.0.0.1:8080/"), p.Address);
            var cred = (NetworkCredential)p.Credentials!;
            Assert.Equal(("us@er", "p:w"), (cred.UserName, cred.Password));
        }

        [Fact]
        public async Task TtwidThroughConnectProxy()
        {
            using var p = FakeProxy.Connect();
            await Refused(() => TtwidAuth.FetchTtwidAsync(Timeout, "ua", Via(p, "http")));
            Assert.Equal(new ProxyHit("www.tiktok.com:443", Basic), Assert.Single(p.Hits));
        }

        [Fact]
        public async Task TtwidThroughSocks5Proxy()
        {
            using var p = FakeProxy.Socks5();
            await Refused(() => TtwidAuth.FetchTtwidAsync(Timeout, "ua", Via(p, "socks5")));
            Assert.Equal(new ProxyHit("www.tiktok.com:443", "user:pw"), Assert.Single(p.Hits));
        }

        [Fact]
        public async Task ApiThroughConnectProxy()
        {
            using var p = FakeProxy.Connect();
            await Refused(() => HttpApi.CheckOnlineAsync("someone", Timeout, Via(p, "http"), "en", "US"));
            Assert.Equal(new ProxyHit("www.tiktok.com:443", Basic), Assert.Single(p.Hits));
        }

        [Fact]
        public async Task ApiThroughSocks5Proxy()
        {
            using var p = FakeProxy.Socks5();
            await Refused(() => HttpApi.FetchRoomInfoAsync("7", Timeout, null, Via(p, "socks5"), "en", "US"));
            Assert.Equal(new ProxyHit("webcast.tiktok.com:443", "user:pw"), Assert.Single(p.Hits));
        }

        [Fact]
        public async Task WssThroughConnectProxy()
        {
            using var p = FakeProxy.Connect();
            await Refused(() => RunWss(Via(p, "http")));
            Assert.Equal(new ProxyHit("webcast-ws.tiktok.com:443", Basic), Assert.Single(p.Hits));
        }

        [Fact]
        public async Task WssThroughSocks5Proxy()
        {
            using var p = FakeProxy.Socks5();
            await Refused(() => RunWss(Via(p, "socks5")));
            Assert.Equal(new ProxyHit("webcast-ws.tiktok.com:443", "user:pw"), Assert.Single(p.Hits));
        }
    }
}
