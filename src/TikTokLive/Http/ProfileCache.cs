using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TikTokLive.Auth;
using TikTokLive.Errors;

namespace TikTokLive.Http
{
    public class ProfileCache
    {
        private readonly TimeSpan _ttl;
        private readonly string? _userAgent;
        private readonly string? _cookies;
        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
        private string? _ttwid;
        private readonly object _ttwidLock = new();

        private sealed class CacheEntry
        {
            public object Value { get; }
            public long InsertedAtTicks { get; }
            public CacheEntry(object value) { Value = value; InsertedAtTicks = DateTime.UtcNow.Ticks; }
        }

        public ProfileCache(
            TimeSpan? ttl = null,
            string? userAgent = null,
            string? cookies = null)
        {
            _ttl = ttl ?? TimeSpan.FromSeconds(300);
            _userAgent = userAgent;
            _cookies = cookies;
        }

        public async Task<SigiProfile> FetchAsync(string username, CancellationToken ct = default)
        {
            string key = NormalizeKey(username);

            if (_entries.TryGetValue(key, out CacheEntry? entry) && !IsExpired(entry))
            {
                if (entry.Value is Exception ex) throw ex;
                return (SigiProfile)entry.Value;
            }

            string ttwid = await EnsureTtwidAsync(ct).ConfigureAwait(false);

            try
            {
                var profile = await Sigi.ScrapeProfileAsync(
                    key, ttwid, TimeSpan.FromSeconds(15),
                    _userAgent, _cookies, ct).ConfigureAwait(false);

                _entries[key] = new CacheEntry(profile);
                return profile;
            }
            catch (Exception ex) when (
                ex is ProfilePrivateException ||
                ex is ProfileNotFoundException ||
                ex is ProfileErrorException)
            {
                _entries[key] = new CacheEntry(ex);
                throw;
            }
        }

        public SigiProfile? Cached(string username)
        {
            string key = NormalizeKey(username);
            if (!_entries.TryGetValue(key, out CacheEntry? entry)) return null;
            if (IsExpired(entry)) return null;
            if (entry.Value is Exception) return null;
            return (SigiProfile)entry.Value;
        }

        public void Invalidate(string username)
        {
            _entries.TryRemove(NormalizeKey(username), out _);
        }

        public void InvalidateAll()
        {
            _entries.Clear();
        }

        private async Task<string> EnsureTtwidAsync(CancellationToken ct)
        {
            lock (_ttwidLock)
            {
                if (_ttwid != null) return _ttwid;
            }

            string ttwid = await TtwidAuth.FetchTtwidAsync(
                TimeSpan.FromSeconds(10), _userAgent, ct).ConfigureAwait(false);

            lock (_ttwidLock) { _ttwid = ttwid; }
            return ttwid;
        }

        private bool IsExpired(CacheEntry entry)
        {
            long elapsed = DateTime.UtcNow.Ticks - entry.InsertedAtTicks;
            return elapsed >= _ttl.Ticks;
        }

        private static string NormalizeKey(string username)
        {
            return username.Trim().TrimStart('@').ToLowerInvariant();
        }
    }
}
