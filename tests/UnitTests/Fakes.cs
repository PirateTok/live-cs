using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    /// <summary>One tunnel request seen by a fake proxy.</summary>
    internal sealed record ProxyHit(string Target, string Auth);

    /// <summary>
    /// Offline proxies on 127.0.0.1: they record what the client asked for and then refuse the
    /// tunnel, so nothing leaves the machine.
    /// </summary>
    internal sealed class FakeProxy : IDisposable
    {
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public ConcurrentQueue<ProxyHit> Hits { get; } = new ConcurrentQueue<ProxyHit>();
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        private FakeProxy(Func<FakeProxy, NetworkStream, Task> serve)
        {
            _listener.Start();
            _ = AcceptLoop(serve);
        }

        /// <summary>HTTP CONNECT proxy: 407 challenge without credentials, records + 502 with them.</summary>
        public static FakeProxy Connect() => new FakeProxy(ServeConnect);

        /// <summary>SOCKS5 proxy (RFC 1928/1929): records target + user:pass, replies "refused".</summary>
        public static FakeProxy Socks5() => new FakeProxy(ServeSocks5);

        private async Task AcceptLoop(Func<FakeProxy, NetworkStream, Task> serve)
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(_cts.Token); }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }
                _ = Task.Run(async () =>
                {
                    using (client)
                    {
                        try { await serve(this, client.GetStream()); }
                        catch (IOException) { }
                    }
                });
            }
        }

        private static async Task ServeConnect(FakeProxy p, NetworkStream s)
        {
            while (true)
            {
                Dictionary<string, string>? headers = await ReadHead(s);
                if (headers == null) return;
                headers.TryGetValue("proxy-authorization", out string? auth);
                if (auth == null)
                {
                    await Write(s, "HTTP/1.1 407 Proxy Authentication Required\r\n" +
                                   "Proxy-Authenticate: Basic realm=\"fake\"\r\nContent-Length: 0\r\n\r\n");
                    continue;
                }
                p.Hits.Enqueue(new ProxyHit(headers["request-target"], auth));
                await Write(s, "HTTP/1.1 502 Bad Gateway\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                return;
            }
        }

        private static async Task ServeSocks5(FakeProxy p, NetworkStream s)
        {
            byte[] head = await ReadExact(s, 2);
            byte[] methods = await ReadExact(s, head[1]);
            string auth = "";
            if (Array.IndexOf(methods, (byte)0x02) >= 0)
            {
                await s.WriteAsync(new byte[] { 0x05, 0x02 });
                byte[] ver = await ReadExact(s, 2);
                string user = Encoding.ASCII.GetString(await ReadExact(s, ver[1]));
                byte[] plen = await ReadExact(s, 1);
                string pass = Encoding.ASCII.GetString(await ReadExact(s, plen[0]));
                auth = user + ":" + pass;
                await s.WriteAsync(new byte[] { 0x01, 0x00 });
            }
            else
            {
                await s.WriteAsync(new byte[] { 0x05, 0x00 });
            }
            byte[] req = await ReadExact(s, 4);
            string host = req[3] switch
            {
                0x03 => Encoding.ASCII.GetString(await ReadExact(s, (await ReadExact(s, 1))[0])),
                0x01 => new IPAddress(await ReadExact(s, 4)).ToString(),
                _ => throw new IOException("atyp " + req[3]),
            };
            byte[] port = await ReadExact(s, 2);
            p.Hits.Enqueue(new ProxyHit($"{host}:{port[0] << 8 | port[1]}", auth));
            await s.WriteAsync(new byte[] { 0x05, 0x05, 0x00, 0x01, 0, 0, 0, 0, 0, 0 });
        }

        /// <summary>Reads an HTTP request head; keys lowercased, plus "request-target".</summary>
        internal static async Task<Dictionary<string, string>?> ReadHead(Stream s)
        {
            var sb = new StringBuilder();
            var one = new byte[1];
            while (!sb.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                if (await s.ReadAsync(one) == 0) return null;
                sb.Append((char)one[0]);
            }
            string[] lines = sb.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var headers = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["request-target"] = lines[0].Split(' ')[1],
            };
            for (int i = 1; i < lines.Length; i++)
            {
                int colon = lines[i].IndexOf(':');
                headers[lines[i].Substring(0, colon).Trim().ToLowerInvariant()] = lines[i].Substring(colon + 1).Trim();
            }
            return headers;
        }

        internal static async Task<byte[]> ReadExact(Stream s, int n)
        {
            var buf = new byte[n];
            int off = 0;
            while (off < n)
            {
                int r = await s.ReadAsync(buf.AsMemory(off, n - off));
                if (r == 0) throw new IOException("eof");
                off += r;
            }
            return buf;
        }

        internal static Task Write(Stream s, string text) => s.WriteAsync(Encoding.ASCII.GetBytes(text)).AsTask();

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }

    /// <summary>Plain ws:// server: manual RFC 6455 handshake, then the BCL server-side WebSocket.</summary>
    internal sealed class FakeWebcast : IDisposable
    {
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public FakeWebcast() => _listener.Start();

        /// <summary>Accepts one client; returns its request head and the server-side socket.</summary>
        public async Task<(Dictionary<string, string> Head, WebSocket Socket)> AcceptAsync()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            NetworkStream s = client.GetStream();
            Dictionary<string, string> head = await FakeProxy.ReadHead(s) ?? throw new IOException("no handshake");
            string accept = Convert.ToBase64String(SHA1.HashData(
                Encoding.ASCII.GetBytes(head["sec-websocket-key"] + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            await FakeProxy.Write(s, "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\n" +
                                     $"Connection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n");
            return (head, WebSocket.CreateFromStream(s, new WebSocketCreationOptions { IsServer = true }));
        }

        public static async Task<byte[]> ReceiveBinaryAsync(WebSocket ws, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            var buf = new byte[8192];
            while (true)
            {
                WebSocketReceiveResult r = await ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close) throw new IOException("client closed");
                ms.Write(buf, 0, r.Count);
                if (r.EndOfMessage) return ms.ToArray();
            }
        }

        public void Dispose() => _listener.Stop();
    }
}
