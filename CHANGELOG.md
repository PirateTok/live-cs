# Changelog

## 0.2.1

Fixes (found by new offline fake-server / fake-proxy tests):
- `internal_ext` was decoded as a UTF-8 string: non-UTF-8 bytes broke frame processing, so the ack never
  went out and the session went silent. `WebcastResponse.InternalExt` is now `byte[]`, echoed verbatim.
- One undecodable frame no longer ends the session; socket errors mid-session now surface as a failed attempt
  instead of being swallowed by `Task.WhenAny`; the socket is disposed after every session.
- The client now completes the WebSocket close handshake when the server closes.
- `.Proxy(string url)` dropped `user:pass@` credentials (`new WebProxy(url)` ignores userinfo).
  New `ProxyUrl.Parse` keeps them for HTTP CONNECT and SOCKS5.
- WSS errors other than `TikTokLiveException` (`WebSocketException`, `IOException`) no longer escape `RunAsync`.

Tests: fake CONNECT + SOCKS5 proxies (ttwid, API, WSS, credentials), fake webcast WS server
(heartbeat, enter_room, ack log_id + binary internal_ext, UA / cookies / locale / compress / heartbeat_duration
on the wire), client-level reconnect loop (Reconnecting×N → Disconnected once, rotation, cancel).

## 0.2.0

Breaking:
- `RoomIdResult(roomId)` → `RoomIdResult(roomId, anchorId)`; new `AnchorId` property.
- `Contributor.CoinCount` (int) → `Contributor.Score` (long); `Contributor.Rank` int → long.
- `ReplayTests` fail (instead of passing) when testdata is missing.

Changes:

- ttwid fetch retries up to 8× (750 ms apart) when TikTok omits the cookie; transport errors still propagate.
- Reconnect loop: a ttwid failure is a failed attempt (`Reconnecting`, backoff) instead of aborting `RunAsync`.
- ttwid + UA are reused across reconnects and rotated only on DEVICE_BLOCKED or a connection that died within 30 s.
- `MaxRetries` counts consecutive failures; a 30 s healthy session resets the count.
- `HeartbeatInterval` now also feeds the `heartbeat_duration` WSS URL param (ms).
- `RoomIdResult.AnchorId` (streamer user ID from `/api-live/user/room`).
- `WebcastRoomUserSeqMessage.TopViewers()`; `Contributor.CoinCount` renamed to `Score`, `Score`/`Rank` are now `long`.
- `FetchRoomAudienceAsync` (full viewer roster, login-gated) + `SessionRequiredException`; `Audience` example.
- Replay tests fail on missing testdata instead of passing silently; new offline `tests/UnitTests`.
- Package homepage: https://piratetok.rosint.org
