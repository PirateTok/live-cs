using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Auth;
using TikTokLive.Errors;
using Xunit;

namespace UnitTests
{
    /// <summary>Fake responder: no ttwid cookie for the first N requests, then a cookie.</summary>
    internal sealed class FlakyTtwidHandler : HttpMessageHandler
    {
        private readonly int _missesBeforeCookie;
        public int Requests { get; private set; }

        public FlakyTtwidHandler(int missesBeforeCookie) => _missesBeforeCookie = missesBeforeCookie;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("Set-Cookie", "tt_csrf_token=abc; path=/");
            if (Requests > _missesBeforeCookie)
                response.Headers.Add("Set-Cookie", "ttwid=1%7Cfresh; Path=/; Domain=tiktok.com; HttpOnly");
            return Task.FromResult(response);
        }
    }

    internal sealed class FailingHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            throw new HttpRequestException("connection refused");
        }
    }

    public class TtwidAuthTest
    {
        private const string Url = "http://fake.invalid/";
        private static readonly TimeSpan NoDelay = TimeSpan.Zero;

        private static Task<string> Fetch(HttpMessageHandler handler)
            => TtwidAuth.FetchTtwidAsync(handler, Url, TimeSpan.FromSeconds(5), "test-ua",
                TtwidAuth.FetchAttempts, NoDelay, CancellationToken.None);

        [Fact]
        public async Task MissingCookieThenCookieSucceeds()
        {
            var handler = new FlakyTtwidHandler(missesBeforeCookie: 5);
            string ttwid = await Fetch(handler);
            Assert.Equal("1%7Cfresh", ttwid);
            Assert.Equal(6, handler.Requests);
        }

        [Fact]
        public async Task FirstResponseWithCookieDoesNotRetry()
        {
            var handler = new FlakyTtwidHandler(missesBeforeCookie: 0);
            Assert.Equal("1%7Cfresh", await Fetch(handler));
            Assert.Equal(1, handler.Requests);
        }

        [Fact]
        public async Task NeverACookieFailsAfterEightAttempts()
        {
            var handler = new FlakyTtwidHandler(missesBeforeCookie: int.MaxValue);
            var ex = await Assert.ThrowsAsync<TikTokLiveException>(() => Fetch(handler));
            Assert.Equal(8, handler.Requests);
            Assert.Contains("after 8 attempts", ex.Message);
        }

        [Fact]
        public async Task TransportErrorPropagatesWithoutRetry()
        {
            var handler = new FailingHandler();
            await Assert.ThrowsAsync<HttpRequestException>(() => Fetch(handler));
            Assert.Equal(1, handler.Requests);
        }

        [Fact]
        public void DefaultsMatchReference()
        {
            Assert.Equal(8, TtwidAuth.FetchAttempts);
            Assert.Equal(TimeSpan.FromMilliseconds(750), TtwidAuth.RetryDelay);
        }
    }
}
