using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using TikTokLive.Errors;
using TikTokLive.Events;
using TikTokLive.Proto;

namespace TikTokLive.Connection
{
    internal class SocketLoop
    {
        private readonly string _wssUrl;
        private readonly string _ttwid;
        private readonly string _roomId;
        private readonly string _userAgent;
        private readonly string? _extraCookies;
        private readonly IWebProxy? _proxy;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _staleTimeout;
        private readonly Action<TikTokLiveEvent> _emit;
        private ClientWebSocket? _ws;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public SocketLoop(
            string wssUrl, string ttwid, string roomId,
            string userAgent, string? extraCookies, IWebProxy? proxy,
            TimeSpan heartbeatInterval, TimeSpan staleTimeout,
            Action<TikTokLiveEvent> emit)
        {
            _wssUrl = wssUrl;
            _ttwid = ttwid;
            _roomId = roomId;
            _userAgent = userAgent;
            _extraCookies = extraCookies;
            _proxy = proxy;
            _heartbeatInterval = heartbeatInterval;
            _staleTimeout = staleTimeout;
            _emit = emit;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _ws = new ClientWebSocket();
            if (_proxy != null)
                _ws.Options.Proxy = _proxy;
            _ws.Options.SetRequestHeader("User-Agent", _userAgent);

            string cookie = string.IsNullOrEmpty(_extraCookies)
                ? $"ttwid={_ttwid}"
                : $"ttwid={_ttwid}; {_extraCookies}";
            _ws.Options.SetRequestHeader("Cookie", cookie);
            _ws.Options.SetRequestHeader("Origin", "https://www.tiktok.com");

            try
            {
                await _ws.ConnectAsync(new Uri(_wssUrl), ct).ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                // ClientWebSocket on netstandard2.0 does not expose HTTP response
                // headers from the failed handshake, so we cannot inspect
                // Handshake-Msg directly. Treat handshake rejection as a potential
                // DEVICE_BLOCKED — the client will rotate ttwid + UA on retry.
                throw new DeviceBlockedException(
                    $"websocket handshake failed (possible DEVICE_BLOCKED): {ex.Message}", ex);
            }

            byte[] hb = BuildHeartbeat();
            await SendAsync(hb, ct).ConfigureAwait(false);

            byte[] enter = BuildEnterRoom();
            await SendAsync(enter, ct).ConfigureAwait(false);

            using (var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                Task heartbeatTask = RunHeartbeatAsync(heartbeatCts.Token);
                Task receiveTask = RunReceiveAsync(ct);

                await Task.WhenAny(heartbeatTask, receiveTask).ConfigureAwait(false);
                heartbeatCts.Cancel();
            }
            // No Disconnected emit — client owns lifecycle events
        }

        private async Task RunHeartbeatAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_heartbeatInterval, ct).ConfigureAwait(false);

                if (_ws == null || _ws.State != WebSocketState.Open)
                    break;

                byte[] hbBytes = BuildHeartbeat();
                await SendAsync(hbBytes, ct).ConfigureAwait(false);
            }
        }

        private async Task RunReceiveAsync(CancellationToken ct)
        {
            var recvBuf = new byte[65536];

            while (_ws != null && _ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                try
                {
                    using var staleCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    staleCts.CancelAfter(_staleTimeout);
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(recvBuf), staleCts.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (WebSocketException) { break; }

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Binary)
                    continue;

                byte[] frameData = await CollectMessageAsync(recvBuf, result, ct).ConfigureAwait(false);
                await ProcessFrameAsync(frameData, ct).ConfigureAwait(false);
            }
        }

        private async Task<byte[]> CollectMessageAsync(
            byte[] buf, WebSocketReceiveResult first, CancellationToken ct)
        {
            if (first.EndOfMessage)
            {
                var data = new byte[first.Count];
                Buffer.BlockCopy(buf, 0, data, 0, first.Count);
                return data;
            }

            using (var ms = new MemoryStream())
            {
                ms.Write(buf, 0, first.Count);
                var seg = new ArraySegment<byte>(buf);
                while (true)
                {
                    WebSocketReceiveResult r = await _ws!.ReceiveAsync(seg, ct)
                        .ConfigureAwait(false);
                    ms.Write(buf, 0, r.Count);
                    if (r.EndOfMessage) break;
                }
                return ms.ToArray();
            }
        }

        private async Task ProcessFrameAsync(byte[] data, CancellationToken ct)
        {
            WebcastPushFrame frame;
            using (var ms = new MemoryStream(data))
            {
                frame = Serializer.Deserialize<WebcastPushFrame>(ms);
            }

            switch (frame.PayloadType)
            {
                case "msg":
                    byte[] payload = DecompressIfGzipped(frame.Payload);
                    WebcastResponse response;
                    using (var ms = new MemoryStream(payload))
                    {
                        response = Serializer.Deserialize<WebcastResponse>(ms);
                    }

                    if (response.NeedsAck && !string.IsNullOrEmpty(response.InternalExt))
                    {
                        byte[] ack = BuildAck(frame.LogId, Encoding.UTF8.GetBytes(response.InternalExt));
                        await SendAsync(ack, ct).ConfigureAwait(false);
                    }

                    foreach (WebcastMessage msg in response.Messages)
                    {
                        List<TikTokLiveEvent> events = MessageRouter.Decode(msg.Type, msg.Payload);
                        foreach (TikTokLiveEvent evt in events)
                            _emit(evt);
                    }
                    break;

                case "im_enter_room_resp":
                case "hb":
                    break;
            }
        }

        private async Task SendAsync(byte[] data, CancellationToken ct)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await _ws.SendAsync(
                    new ArraySegment<byte>(data), WebSocketMessageType.Binary,
                    true, ct).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private byte[] BuildHeartbeat()
        {
            if (!ulong.TryParse(_roomId, out ulong rid))
                throw new ProtocolException($"room_id is not a valid ulong: {_roomId}");

            var hb = new HeartbeatMessage { RoomId = rid };
            var frame = new WebcastPushFrame
            {
                PayloadEncoding = "pb",
                PayloadType = "hb",
                Payload = SerializeToBytes(hb),
            };
            return SerializeToBytes(frame);
        }

        private byte[] BuildEnterRoom()
        {
            if (!long.TryParse(_roomId, out long rid))
                throw new ProtocolException($"room_id is not a valid long: {_roomId}");

            var msg = new WebcastImEnterRoomMessage
            {
                RoomId = rid,
                LiveId = 12,
                Identity = "audience",
                FilterWelcomeMsg = "0",
            };
            var frame = new WebcastPushFrame
            {
                PayloadEncoding = "pb",
                PayloadType = "im_enter_room",
                Payload = SerializeToBytes(msg),
            };
            return SerializeToBytes(frame);
        }

        private static byte[] BuildAck(long logId, byte[] internalExt)
        {
            var frame = new WebcastPushFrame
            {
                LogId = logId,
                PayloadEncoding = "pb",
                PayloadType = "ack",
                Payload = internalExt,
            };
            return SerializeToBytes(frame);
        }

        private static byte[] SerializeToBytes<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static byte[] DecompressIfGzipped(byte[] data)
        {
            if (data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b)
            {
                using (var input = new MemoryStream(data))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
            return data;
        }
    }
}
