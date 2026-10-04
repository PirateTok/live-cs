# Changelog

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
