using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// Dedicated high-concurrency rotating proxy server listening on a local port (default 8081).
/// Accepts HTTP and HTTPS CONNECT requests, dynamically dispatches them to verified upstream proxies,
/// chains outbound connections across local physical network adapters, and provides transparent circuit-breaking.
/// </summary>
public class RotatingProxyServer
{
    private readonly RotatingProxyConfigModel _config;
    private readonly RotatingProxyRouter _router;
    private readonly Func<IEnumerable<NetworkAdapterModel>> _getAdapters;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Port => _config.RotatingProxyPort;

    public long ActiveConnections;
    public long TotalConnectionsHandled;

    public event Action<string>? OnLog;

    public RotatingProxyServer(
        RotatingProxyConfigModel config,
        RotatingProxyRouter router,
        Func<IEnumerable<NetworkAdapterModel>> getAdapters)
    {
        _config = config;
        _router = router;
        _getAdapters = getAdapters;
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _cts = new CancellationTokenSource();
            int curPort = _config.RotatingProxyPort;
            int attempts = 0;
            TcpListener? activeListener = null;

            while (attempts < 20)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, curPort);
                    listener.ExclusiveAddressUse = false;
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    listener.Start(256);
                    activeListener = listener;
                    break;
                }
                catch (SocketException)
                {
                    curPort++;
                    attempts++;
                }
            }

            if (activeListener == null)
            {
                _isRunning = false;
                OnLog?.Invoke($"[RotatingProxy] Failed to start: all ports {_config.RotatingProxyPort}-{curPort} unavailable.");
                return;
            }

            _listener = activeListener;
            _config.RotatingProxyPort = curPort;
            _isRunning = true;

            OnLog?.Invoke($"[RotatingProxy] Server listening on 127.0.0.1:{_config.RotatingProxyPort} (Strategy: {_config.Strategy})");

            _ = AcceptLoopAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[RotatingProxy] Failed to start on port {_config.RotatingProxyPort}: {ex.Message}");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RotatingProxy] Stop error: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
            OnLog?.Invoke("[RotatingProxy] Server stopped.");
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                var clientSocket = await _listener!.AcceptSocketAsync(ct);
                Interlocked.Increment(ref ActiveConnections);
                Interlocked.Increment(ref TotalConnectionsHandled);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(clientSocket, ct);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[RotatingProxy] Client handling error: {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Decrement(ref ActiveConnections);
                        try { clientSocket.Dispose(); } catch { }
                    }
                }, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!_isRunning) break;
                OnLog?.Invoke($"[RotatingProxy] Accept loop error: {ex.Message}");
                await Task.Delay(100, ct);
            }
        }
    }

    private async Task HandleClientAsync(Socket clientSocket, CancellationToken ct)
    {
        clientSocket.NoDelay = true;
        using var clientStream = new NetworkStream(clientSocket, ownsSocket: false);

        byte[] headerBuffer = new byte[8192];
        int bytesRead = await clientStream.ReadAsync(headerBuffer, 0, headerBuffer.Length, ct);
        if (bytesRead <= 0) return;

        string requestString = Encoding.ASCII.GetString(headerBuffer, 0, bytesRead);
        string firstLine = requestString.Split("\r\n")[0];
        var parts = firstLine.Split(' ');
        if (parts.Length < 2) return;

        string method = parts[0].ToUpperInvariant();
        string target = parts[1];
        string targetHost = ParseHost(target);
        string clientIp = ((IPEndPoint?)clientSocket.RemoteEndPoint)?.Address.ToString() ?? "127.0.0.1";

        bool isConnect = method == "CONNECT";
        int maxRetries = 2;
        Socket? upstreamSocket = null;
        NetworkStream? upstreamStream = null;

        for (int retry = 0; retry <= maxRetries; retry++)
        {
            var proxy = _router.SelectUpstreamProxy(targetHost, clientIp);
            if (proxy == null)
            {
                // Fallback message if no proxies are currently harvested/verified
                string errorResponse = "HTTP/1.1 503 Service Unavailable\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\nCombiSpeed: No healthy rotating proxies available in pool. Please harvest or import proxies.\r\n";
                await clientStream.WriteAsync(Encoding.ASCII.GetBytes(errorResponse), ct);
                return;
            }

            try
            {
                var adapters = _getAdapters();
                upstreamSocket = await _router.ConnectToProxyAsync(proxy, adapters, ct);
                upstreamStream = new NetworkStream(upstreamSocket, ownsSocket: true);

                if (isConnect)
                {
                    // HTTPS CONNECT tunnel
                    if (proxy.Protocol == ProxyProtocol.Socks5)
                    {
                        bool socksOk = await PerformSocks5ConnectAsync(upstreamStream, target, proxy, ct);
                        if (!socksOk) throw new Exception("SOCKS5 CONNECT failed");
                    }
                    else
                    {
                        // HTTP/HTTPS upstream proxy CONNECT with optional Proxy-Authorization
                        string authHeader = "";
                        if (!string.IsNullOrEmpty(proxy.Username))
                        {
                            string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                            authHeader = $"Proxy-Authorization: Basic {creds}\r\n";
                        }

                        string connectReq = $"CONNECT {target} HTTP/1.1\r\nHost: {target}\r\n{authHeader}Proxy-Connection: Keep-Alive\r\n\r\n";
                        byte[] connectBytes = Encoding.ASCII.GetBytes(connectReq);
                        await upstreamStream.WriteAsync(connectBytes, ct);

                        byte[] respBuf = new byte[1024];
                        int respLen = await upstreamStream.ReadAsync(respBuf, ct);
                        string respStr = Encoding.ASCII.GetString(respBuf, 0, respLen);

                        if (!respStr.Contains("200"))
                        {
                            throw new Exception($"Upstream proxy returned non-200: {respStr.Split("\r\n")[0]}");
                        }
                    }

                    // Acknowledge connection established to client
                    string okResponse = "HTTP/1.1 200 Connection Established\r\nProxy-Agent: CombiSpeed-Rotator/2.1.1\r\n\r\n";
                    await clientStream.WriteAsync(Encoding.ASCII.GetBytes(okResponse), ct);
                }
                else
                {
                    // Standard HTTP request: inject credentials if present, else forward original header bytes
                    if (!string.IsNullOrEmpty(proxy.Username) && !requestString.Contains("Proxy-Authorization:", StringComparison.OrdinalIgnoreCase))
                    {
                        string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                        int firstCrLf = requestString.IndexOf("\r\n");
                        string injectedReq = firstCrLf >= 0
                            ? requestString.Insert(firstCrLf + 2, $"Proxy-Authorization: Basic {creds}\r\n")
                            : requestString;
                        byte[] injBytes = Encoding.ASCII.GetBytes(injectedReq);
                        await upstreamStream.WriteAsync(injBytes, ct);
                    }
                    else
                    {
                        await upstreamStream.WriteAsync(headerBuffer.AsMemory(0, bytesRead), ct);
                    }
                }

                // If handshake succeeded, break out of retry loop
                break;
            }
            catch (Exception ex)
            {
                proxy.Failures++;
                proxy.IsHealthy = false;
                _router.MarkUnavailable(proxy.Endpoint);
                upstreamStream?.Dispose();
                upstreamSocket?.Dispose();
                upstreamStream = null;
                upstreamSocket = null;

                if (retry == maxRetries)
                {
                    string failMsg = $"HTTP/1.1 502 Bad Gateway\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\nCombiSpeed Rotator: Upstream proxy connection failed after {maxRetries + 1} attempts ({ex.Message}).\r\n";
                    try { await clientStream.WriteAsync(Encoding.ASCII.GetBytes(failMsg), ct); } catch { }
                    return;
                }
            }
        }

        if (upstreamStream == null) return;

        using (upstreamStream)
        {
            // Duplex transfer between client and upstream proxy
            var t1 = clientStream.CopyToAsync(upstreamStream, ct);
            var t2 = upstreamStream.CopyToAsync(clientStream, ct);
            await Task.WhenAny(t1, t2);
        }
    }

    private static async Task<bool> PerformSocks5ConnectAsync(
        NetworkStream stream,
        string target,
        HarvestedProxyModel proxy,
        CancellationToken ct)
    {
        // 1. Auth greeting: offer No Auth (0x00) and RFC 1929 User/Password (0x02)
        byte[] greeting = string.IsNullOrEmpty(proxy.Username)
            ? new byte[] { 0x05, 0x01, 0x00 }
            : new byte[] { 0x05, 0x02, 0x00, 0x02 };

        await stream.WriteAsync(greeting, ct);

        byte[] resp = new byte[2];
        int read = await stream.ReadAsync(resp, ct);
        if (read != 2 || resp[0] != 0x05) return false;

        if (resp[1] == 0x02 && !string.IsNullOrEmpty(proxy.Username))
        {
            // RFC 1929 Username/Password Subnegotiation
            byte[] uBytes = Encoding.ASCII.GetBytes(proxy.Username);
            byte[] pBytes = Encoding.ASCII.GetBytes(proxy.Password ?? "");
            byte[] auth = new byte[1 + 1 + uBytes.Length + 1 + pBytes.Length];
            auth[0] = 0x01; // RFC 1929 Version
            auth[1] = (byte)uBytes.Length;
            Array.Copy(uBytes, 0, auth, 2, uBytes.Length);
            auth[2 + uBytes.Length] = (byte)pBytes.Length;
            Array.Copy(pBytes, 0, auth, 3 + uBytes.Length, pBytes.Length);

            await stream.WriteAsync(auth, ct);

            byte[] authResp = new byte[2];
            int aRead = await stream.ReadAsync(authResp, ct);
            if (aRead < 2 || authResp[1] != 0x00) return false;
        }
        else if (resp[1] != 0x00)
        {
            return false;
        }

        // 2. CONNECT request
        string host = target;
        int port = 443;
        if (target.Contains(':'))
        {
            var p = target.Split(':');
            host = p[0];
            _ = int.TryParse(p[1], out port);
        }

        byte[] hostBytes = Encoding.ASCII.GetBytes(host);
        byte[] req = new byte[4 + 1 + hostBytes.Length + 2];
        req[0] = 0x05; // SOCKS5
        req[1] = 0x01; // CONNECT
        req[2] = 0x00; // Reserved
        req[3] = 0x03; // Domain Name
        req[4] = (byte)hostBytes.Length;
        Array.Copy(hostBytes, 0, req, 5, hostBytes.Length);
        req[5 + hostBytes.Length] = (byte)(port >> 8);
        req[6 + hostBytes.Length] = (byte)(port & 0xFF);

        await stream.WriteAsync(req, ct);

        byte[] sResp = new byte[10];
        int sRead = await stream.ReadAsync(sResp, ct);
        return sRead >= 4 && sResp[0] == 0x05 && sResp[1] == 0x00;
    }

    private static string ParseHost(string target)
    {
        if (target.Contains(':'))
        {
            return target.Split(':')[0];
        }
        if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }
        return target;
    }
}

