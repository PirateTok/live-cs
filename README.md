<p align="center">
  <img src="https://raw.githubusercontent.com/PirateTok/.github/main/profile/assets/og-banner-v2.png" alt="PirateTok" width="640" />
</p>

# PirateTok.Live

Connect to any TikTok Live stream and receive real-time events in C#. No signing server, no API keys, no authentication required.

```csharp
// Connect to a live stream — auth and room resolution handled internally
var client = new TikTokLiveClient("username_here");

// Subscribe to events — each carries fully decoded protobuf data
client.OnChat += msg =>
    Console.WriteLine($"[chat] {msg.User?.Nickname}: {msg.Comment}");

client.OnGift += msg =>
    Console.WriteLine($"[gift] {msg.User?.Nickname} sent {msg.GiftDetails?.GiftName} x{msg.RepeatCount} ({msg.GiftDetails?.DiamondCount} diamonds)");

client.OnLike += msg =>
    Console.WriteLine($"[like] {msg.User?.Nickname} ({msg.TotalLikeCount} total)");

// Blocks until disconnected — handles heartbeat and reconnection automatically
await client.RunAsync();
```

## Install

```
dotnet add package PirateTok.Live
```

Targets `netstandard2.0` — works with .NET 6+, .NET Framework 4.6.1+, Unity 2021+.

## Other languages

| Language | Install | Repo |
|:---------|:--------|:-----|
| **Rust** | `cargo add piratetok-live-rs` | [live-rs](https://github.com/PirateTok/live-rs) |
| **Go** | `go get github.com/PirateTok/live-go` | [live-go](https://github.com/PirateTok/live-go) |
| **Python** | `pip install piratetok-live-py` | [live-py](https://github.com/PirateTok/live-py) |
| **JavaScript** | `npm install piratetok-live-js` | [live-js](https://github.com/PirateTok/live-js) |
| **Java** | `com.piratetok:live` | [live-java](https://github.com/PirateTok/live-java) |
| **Lua** | `luarocks install piratetok-live-lua` | [live-lua](https://github.com/PirateTok/live-lua) |
| **Elixir** | `{:piratetok_live, "~> 0.1"}` | [live-ex](https://github.com/PirateTok/live-ex) |
| **Dart** | `dart pub add piratetok_live` | [live-dart](https://github.com/PirateTok/live-dart) |
| **C** | `#include "piratetok.h"` | [live-c](https://github.com/PirateTok/live-c) |
| **PowerShell** | `Install-Module PirateTok.Live` | [live-ps1](https://github.com/PirateTok/live-ps1) |
| **Shell** | `bpkg install PirateTok/live-sh` | [live-sh](https://github.com/PirateTok/live-sh) |

## Features

- **Zero signing dependency** — no API keys, no signing server, no external auth
- **64 decoded event types** — chat, gifts, likes, joins, follows, shares, battles, polls, and more
- **Auto-reconnection** — stale detection, exponential backoff, self-healing auth
- **Enriched User data** — badges, gifter level, moderator status, follow info, fan club
- **Sub-routed convenience events** — `OnFollow`, `OnShare`, `OnJoin`, `OnLiveEnded`

## Configuration

```csharp
var client = new TikTokLiveClient("username_here")
    .CdnEu()
    .Timeout(TimeSpan.FromSeconds(15))
    .MaxRetries(10)
    .StaleTimeout(TimeSpan.FromSeconds(90));
```

### Builder methods

| Method | Default | Description |
|:-------|:--------|:------------|
| `.Cdn(host)` | `webcast-ws.tiktok.com` | Set custom CDN hostname |
| `.CdnEu()` | — | Shorthand for EU CDN (`webcast-ws.eu.tiktok.com`) |
| `.CdnUs()` | — | Shorthand for US CDN (`webcast-ws.us.tiktok.com`) |
| `.Timeout(TimeSpan)` | 10s | HTTP request timeout for ttwid fetch, online check, room info |
| `.HeartbeatInterval(TimeSpan)` | 10s | Interval between WSS heartbeat frames |
| `.MaxRetries(int)` | 5 | Maximum reconnection attempts before giving up |
| `.StaleTimeout(TimeSpan)` | 60s | Close and reconnect if no data arrives within this window |
| `.Proxy(string)` | none | HTTP/HTTPS/SOCKS5 proxy URL — applies to all HTTP requests and WSS |
| `.Proxy(IWebProxy)` | none | Same, but accepts a `System.Net.IWebProxy` instance directly |
| `.UserAgent(string)` | random | Override the random UA pool with a fixed user agent |
| `.Cookies(string)` | none | Append session cookies alongside ttwid in WSS cookie header (`"sessionid=xxx; sid_tt=xxx"`) |
| `.Language(string)` | system | Override language sent in WSS URL params (auto-detected from system locale) |
| `.Region(string)` | system | Override region sent in WSS URL params (auto-detected from system locale) |
| `.Compress(bool)` | `true` | Disable gzip compression for WSS payloads (trades bandwidth for CPU) |

## Room info (optional, separate call)

```csharp
// Normal rooms
var info = await TikTokLiveClient.FetchRoomInfoAsync(
    "ROOM_ID",
    TimeSpan.FromSeconds(10));

// 18+ rooms — pass session cookies from browser DevTools
var info = await TikTokLiveClient.FetchRoomInfoAsync(
    "ROOM_ID",
    TimeSpan.FromSeconds(10),
    cookies: "sessionid=abc; sid_tt=abc");
```

## How it works

1. Resolves username to room ID via TikTok JSON API
2. Authenticates and opens a direct WSS connection
3. Protobuf heartbeats every 10s
4. Decodes protobuf stream into typed C# objects via protobuf-net
5. Auto-reconnects on stale/dropped connections with fresh credentials

## Examples

```bash
dotnet run --project examples/BasicChat -- <username>        # connect + print chat events
dotnet run --project examples/OnlineCheck -- <username>      # check if user is live
dotnet run --project examples/StreamInfo -- <username>       # fetch room metadata + stream URLs
dotnet run --project examples/GiftTracker -- <username>      # track gifts with diamond totals
dotnet run --project examples/GiftStreak -- <username>       # track gift streaks with per-event deltas
dotnet run --project examples/ProfileLookup -- <username>    # fetch user profile via SIGI scrape
```

## Replay testing

Deterministic cross-lib validation against binary WSS captures. Requires testdata from a separate repo:

```bash
git clone https://github.com/PirateTok/live-testdata testdata
dotnet test
```

Tests skip gracefully if testdata is not found. You can also set `PIRATETOK_TESTDATA` to point to a custom location.

## Known gaps

None currently tracked.

## License

0BSD
