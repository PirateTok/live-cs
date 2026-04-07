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
    Console.WriteLine($"[chat] {msg.User?.Nickname}: {msg.Content}");

client.OnGift += msg =>
    Console.WriteLine($"[gift] {msg.User?.Nickname} sent {msg.Gift?.Name} x{msg.RepeatCount} ({msg.Gift?.DiamondCount} diamonds)");

client.OnLike += msg =>
    Console.WriteLine($"[like] {msg.User?.Nickname} ({msg.TotalLikes} total)");

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
    .CdnEU()
    .Timeout(TimeSpan.FromSeconds(15))
    .MaxRetries(10)
    .StaleTimeout(TimeSpan.FromSeconds(90));
```

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
dotnet run --project examples/BasicChat -- <username>       # connect + print chat events
dotnet run --project examples/OnlineCheck -- <username>     # check if user is live
dotnet run --project examples/StreamInfo -- <username>      # fetch room metadata + stream URLs
dotnet run --project examples/GiftTracker -- <username>     # track gifts with diamond totals
```

## Replay testing

Deterministic cross-lib validation against binary WSS captures. Requires testdata from a separate repo:

```bash
git clone https://github.com/PirateTok/live-testdata ../live-testdata
dotnet test
```

Tests skip gracefully if testdata is not found. You can also set `PIRATETOK_TESTDATA` to point to a custom location.

## Known gaps

- `Proxy(...)` exists on the client surface, but proxy transport plumbing is not wired into `HttpClient` or `ClientWebSocket` yet.
- Explicit `DEVICE_BLOCKED` handshake handling is not implemented yet.

## License

0BSD
