using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Auth;
using TikTokLive.Connection;
using TikTokLive.Errors;
using TikTokLive.Events;
using TikTokLive.Http;
using TikTokLive.Proto;

namespace TikTokLive
{
    public class TikTokLiveClient
    {
        private readonly string _username;
        private string _cdnHost = "webcast-ws.tiktok.com";
        private TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(10);
        private TimeSpan _staleTimeout = TimeSpan.FromSeconds(60);
        private int _maxRetries = 5;
        private IWebProxy? _proxy;
        private string? _userAgent;
        private string? _cookies;
        private string? _language;
        private string? _region;
        private bool _compress = true;

        // lifecycle
        public event Action<string>? OnConnected;
        public event Action<ReconnectInfo>? OnReconnecting;
        public event Action? OnDisconnected;

        // raw proto events
        public event Action<WebcastChatMessage>? OnChat;
        public event Action<WebcastGiftMessage>? OnGift;
        public event Action<WebcastLikeMessage>? OnLike;
        public event Action<WebcastMemberMessage>? OnMember;
        public event Action<WebcastSocialMessage>? OnSocial;
        public event Action<WebcastRoomUserSeqMessage>? OnRoomUserSeq;
        public event Action<WebcastControlMessage>? OnControl;
        public event Action<WebcastLiveIntroMessage>? OnLiveIntro;

        // sub-routed convenience events
        public event Action<WebcastSocialMessage>? OnFollow;
        public event Action<WebcastSocialMessage>? OnShare;
        public event Action<WebcastMemberMessage>? OnJoin;
        public event Action<WebcastControlMessage>? OnLiveEnded;

        // catch-all
        public event Action<TikTokLiveEvent>? OnEvent;

        public TikTokLiveClient(string username) => _username = username;

        public TikTokLiveClient Cdn(string host)
        {
            _cdnHost = host;
            return this;
        }

        public TikTokLiveClient CdnEu() => Cdn("webcast-ws.eu.tiktok.com");
        public TikTokLiveClient CdnUs() => Cdn("webcast-ws.us.tiktok.com");

        public TikTokLiveClient Timeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public TikTokLiveClient HeartbeatInterval(TimeSpan interval)
        {
            _heartbeatInterval = interval;
            return this;
        }

        public TikTokLiveClient MaxRetries(int n)
        {
            _maxRetries = n;
            return this;
        }

        public TikTokLiveClient StaleTimeout(TimeSpan timeout)
        {
            _staleTimeout = timeout;
            return this;
        }

        public TikTokLiveClient Proxy(IWebProxy proxy)
        {
            _proxy = proxy;
            return this;
        }

        public TikTokLiveClient Proxy(string url)
        {
            _proxy = new WebProxy(url);
            return this;
        }

        /// <summary>
        /// Override the user agent for all requests (HTTP + WSS).
        /// When not set, a random UA from the built-in pool is picked on each
        /// reconnect attempt. This is recommended for reducing DEVICE_BLOCKED risk.
        /// Only set this if you have a specific UA you want to use.
        /// </summary>
        public TikTokLiveClient UserAgent(string ua)
        {
            _userAgent = ua;
            return this;
        }

        /// <summary>
        /// Set session cookies for the WSS connection.
        /// These are appended alongside the ttwid cookie. Only needed if you want to
        /// pass authenticated cookies to the WebSocket handshake.
        /// For fetching room info on 18+ rooms, pass cookies directly to
        /// <see cref="FetchRoomInfoAsync"/> instead.
        /// Format: "sessionid=xxx; sid_tt=xxx" (TikTok session cookies from browser DevTools)
        /// </summary>
        public TikTokLiveClient Cookies(string cookies)
        {
            _cookies = cookies;
            return this;
        }

        public TikTokLiveClient Language(string lang)
        {
            _language = lang;
            return this;
        }

        public TikTokLiveClient Region(string reg)
        {
            _region = reg;
            return this;
        }

        public TikTokLiveClient Compress(bool enabled)
        {
            _compress = enabled;
            return this;
        }

        public static Task<RoomIdResult> CheckOnlineAsync(
            string username, TimeSpan timeout, IWebProxy? proxy = null,
            string? language = null, string? region = null,
            CancellationToken ct = default)
            => HttpApi.CheckOnlineAsync(username, timeout, proxy, language, region, ct);

        public static Task<RoomInfo> FetchRoomInfoAsync(
            string roomId, TimeSpan timeout, string? cookies = null,
            IWebProxy? proxy = null, string? language = null,
            string? region = null, CancellationToken ct = default)
            => HttpApi.FetchRoomInfoAsync(roomId, timeout, cookies, proxy, language, region, ct);

        /// <inheritdoc cref="HttpApi.FetchRoomAudienceAsync"/>
        public static Task<RoomAudience> FetchRoomAudienceAsync(
            string roomId, string? anchorId, TimeSpan timeout, string? cookies = null,
            IWebProxy? proxy = null, string? language = null, string? region = null,
            CancellationToken ct = default)
            => HttpApi.FetchRoomAudienceAsync(roomId, anchorId, timeout, cookies, proxy, language, region, ct);

        public async Task RunAsync(CancellationToken ct = default)
        {
            string lang = _language ?? Http.UserAgent.SystemLanguage();
            string reg = _region ?? Http.UserAgent.SystemRegion();

            RoomIdResult room = await HttpApi.CheckOnlineAsync(
                _username, _timeout, _proxy, lang, reg, ct)
                .ConfigureAwait(false);

            EmitEvent(TikTokLiveEvent.Connected(room.RoomId));

            string tz = Http.UserAgent.SystemTimezone();
            var budget = new ReconnectBudget(_maxRetries);
            // ttwid + UA are fetched once and reused across reconnects; rotated only
            // on DEVICE_BLOCKED, a ttwid failure, or a session that died young.
            string? ttwid = null;
            string ua = "";
            while (!ct.IsCancellationRequested)
            {
                SessionExit exit;
                TimeSpan lived = TimeSpan.Zero;
                if (ttwid == null)
                {
                    ua = _userAgent ?? Http.UserAgent.RandomUa();
                    try
                    {
                        ttwid = await TtwidAuth.FetchTtwidAsync(_timeout, ua, _proxy, ct)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                    catch (Exception ex) when (ex is TikTokLiveException || ex is HttpRequestException
                                               || ex is OperationCanceledException)
                    {
                        Trace.TraceWarning($"ttwid acquisition failed: {ex.Message}");
                    }
                }

                if (ttwid == null)
                {
                    exit = SessionExit.NoTtwid;
                }
                else
                {
                    string wssUrl = WssUrlBuilder.Build(_cdnHost, room.RoomId, tz, lang, reg,
                        _compress, _heartbeatInterval);
                    var loop = new SocketLoop(wssUrl, ttwid, room.RoomId,
                        ua, _cookies, _proxy,
                        _heartbeatInterval, _staleTimeout, EmitEvent);

                    var started = Stopwatch.StartNew();
                    try
                    {
                        await loop.RunAsync(ct).ConfigureAwait(false);
                        exit = SessionExit.Closed;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                    catch (DeviceBlockedException) { exit = SessionExit.DeviceBlocked; }
                    catch (TikTokLiveException ex)
                    {
                        Trace.TraceWarning($"websocket error: {ex.Message}");
                        exit = SessionExit.Errored;
                    }
                    lived = started.Elapsed;
                }

                if (ct.IsCancellationRequested) break;

                Judgement judgement = ReconnectBudget.Judge(exit, lived);
                if (judgement.Rotate)
                    ttwid = null;

                Verdict verdict = budget.Record(judgement.End);
                if (verdict.GiveUp) break;

                int delay = (int)verdict.Delay.TotalSeconds;
                EmitEvent(TikTokLiveEvent.Reconnecting(verdict.Attempt, _maxRetries, delay));
                try { await Task.Delay(verdict.Delay, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            }

            EmitEvent(TikTokLiveEvent.Disconnected());
        }

        private void EmitEvent(TikTokLiveEvent evt)
        {
            OnEvent?.Invoke(evt);

            switch (evt.Type)
            {
                case TikTokLiveEventType.Connected:
                    OnConnected?.Invoke(evt.AsRoomId());
                    break;
                case TikTokLiveEventType.Reconnecting:
                    OnReconnecting?.Invoke(evt.As<ReconnectInfo>());
                    break;
                case TikTokLiveEventType.Disconnected:
                    OnDisconnected?.Invoke();
                    break;
                case TikTokLiveEventType.Chat:
                    OnChat?.Invoke(evt.As<WebcastChatMessage>());
                    break;
                case TikTokLiveEventType.Gift:
                    OnGift?.Invoke(evt.As<WebcastGiftMessage>());
                    break;
                case TikTokLiveEventType.Like:
                    OnLike?.Invoke(evt.As<WebcastLikeMessage>());
                    break;
                case TikTokLiveEventType.Member:
                    OnMember?.Invoke(evt.As<WebcastMemberMessage>());
                    break;
                case TikTokLiveEventType.Social:
                    OnSocial?.Invoke(evt.As<WebcastSocialMessage>());
                    break;
                case TikTokLiveEventType.RoomUserSeq:
                    OnRoomUserSeq?.Invoke(evt.As<WebcastRoomUserSeqMessage>());
                    break;
                case TikTokLiveEventType.Control:
                    OnControl?.Invoke(evt.As<WebcastControlMessage>());
                    break;
                case TikTokLiveEventType.Follow:
                    OnFollow?.Invoke(evt.As<WebcastSocialMessage>());
                    break;
                case TikTokLiveEventType.Share:
                    OnShare?.Invoke(evt.As<WebcastSocialMessage>());
                    break;
                case TikTokLiveEventType.Join:
                    OnJoin?.Invoke(evt.As<WebcastMemberMessage>());
                    break;
                case TikTokLiveEventType.LiveEnded:
                    OnLiveEnded?.Invoke(evt.As<WebcastControlMessage>());
                    break;
                case TikTokLiveEventType.LiveIntro:
                    OnLiveIntro?.Invoke(evt.As<WebcastLiveIntroMessage>());
                    break;
            }
        }
    }
}
