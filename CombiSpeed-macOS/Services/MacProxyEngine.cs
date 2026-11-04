using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacProxyEngine
{
    private readonly ProxyConfigModel _config;
    private readonly MacLoadBalancer _loadBalancer;
    private readonly MacBandwidthThrottler _throttler;
    private TcpListener? _httpListener;
    private TcpListener? _socksListener;
    private CancellationTokenSource? _cts;

    public long TotalBytesDownloaded;
    public long TotalBytesUploaded;
    public int ActiveConnections;
    public List<AuditLogModel> RecentLogs { get; } = new();
    public MacRotatingProxyRouter? RotatingRouter { get; set; }

    public MacProxyEngine(ProxyConfigModel config, MacLoadBalancer loadBalancer, MacBandwidthThrottler throttler)
    {
        _config = config;
        _loadBalancer = loadBalancer;
        _throttler = throttler;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();

        try
        {
            int curHttpPort = _config.HttpPort;
            int httpAttempts = 0;
            while (httpAttempts < 20)
            {
                try
                {
                    var l = new TcpListener(IPAddress.Any, curHttpPort);
                    l.ExclusiveAddressUse = false;
                    l.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    l.Start(100);
                    _httpListener = l;
                    _config.HttpPort = curHttpPort;
                    break;
                }
                catch (SocketException)
                {
                    curHttpPort++;
                    httpAttempts++;
                }
            }

            if (_httpListener != null)
                _ = AcceptHttpClientsAsync(_httpListener, _cts.Token);

            int curSocksPort = _config.SocksPort;
            int socksAttempts = 0;
            while (socksAttempts < 20)
            {
                try
                {
                    var l = new TcpListener(IPAddress.Any, curSocksPort);
                    l.ExclusiveAddressUse = false;
                    l.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    l.Start(100);
                    _socksListener = l;
                    _config.SocksPort = curSocksPort;
                    break;
                }
                catch (SocketException)
                {
                    curSocksPort++;
                    socksAttempts++;
                }
            }

            if (_socksListener != null)
                _ = AcceptSocksClientsAsync(_socksListener, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MacProxyEngine] Error starting listeners: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _httpListener?.Stop(); } catch { }
        try { _socksListener?.Stop(); } catch { }
        _httpListener = null;
        _socksListener = null;
    }

    private async Task AcceptHttpClientsAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = HandleHttpClientAsync(client, ct);
            }
            catch when (ct.IsCancellationRequested) { break; }
            catch (Exception) { await Task.Delay(50, ct); }
        }
    }

    private async Task HandleHttpClientAsync(TcpClient client, CancellationToken ct)
    {
        Interlocked.Increment(ref ActiveConnections);
        try
        {
            using (client)
            using (var clientStream = client.GetStream())
            {
                var buffer = new byte[8192];
                int read = await clientStream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read <= 0) return;

                string request = Encoding.ASCII.GetString(buffer, 0, read);
                string[] lines = request.Split("\r\n");
                if (lines.Length == 0) return;

                string[] reqLine = lines[0].Split(' ');
                if (reqLine.Length < 2) return;

                string method = reqLine[0].ToUpperInvariant();
                string target = reqLine[1];

                // PAC Script serving
                if (method == "GET" && target.Contains("proxy.pac"))
                {
                    string pac = PacConfigModel.Current.GeneratePacScript(_config.HttpPort, _config.SocksPort);
                    byte[] pacBytes = Encoding.UTF8.GetBytes(pac);
                    string resp = $"HTTP/1.1 200 OK\r\nContent-Type: application/x-ns-proxy-autoconfig\r\nContent-Length: {pacBytes.Length}\r\nConnection: close\r\n\r\n";
                    await clientStream.WriteAsync(Encoding.ASCII.GetBytes(resp), ct);
                    await clientStream.WriteAsync(pacBytes, ct);
                    return;
                }

                var adapter = _loadBalancer.GetNextAdapter();
                if (adapter == null) return;

                string targetHost = string.Empty;
                int targetPort = 80;

                if (method == "CONNECT")
                {
                    var parts = target.Split(':');
                    targetHost = parts[0];
                    targetPort = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 443;

                    string clientEp = client.Client.RemoteEndPoint?.ToString() ?? "";
                    bool viaRotator = _config?.RotatingProxy?.RouteBondedTrafficThroughRotator == true && RotatingRouter != null;

                    if (viaRotator)
                    {
                        int maxAttempts = Math.Min(12, Math.Max(3, RotatingRouter?.GetCandidatePool().Count ?? 1));
                        bool connected = false;

                        for (int attempt = 0; attempt < maxAttempts; attempt++)
                        {
                            var proxy = RotatingRouter?.SelectUpstreamProxy(targetHost, clientEp);
                            if (proxy == null) break;

                            using var attemptSocket = CreateBoundSocket(adapter);
                            try
                            {
                                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                                connectCts.CancelAfter(2500);
                                await attemptSocket.ConnectAsync(proxy.Host, proxy.Port, connectCts.Token);
                                using var remoteStream = new NetworkStream(attemptSocket, true);
                                bool tunnelOk = await EstablishUpstreamProxyTunnelAsync(remoteStream, proxy, targetHost, targetPort, connectCts.Token);
                                if (tunnelOk)
                                {
                                    byte[] ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                                    await clientStream.WriteAsync(ok, 0, ok.Length, ct);
                                    AddAuditLog($"HTTPS -> {proxy.Endpoint} ({proxy.CountryCode})", targetHost, targetPort, adapter.Name, clientEp, $"{proxy.Protocol.ToString().ToLower()}://{proxy.Endpoint}", true, proxy.LatencyMs, proxy.Host);
                                    await PipeBidirectionalAsync(clientStream, remoteStream, adapter, ct);
                                    connected = true;
                                    break;
                                }
                                else
                                {
                                    RotatingRouter?.MarkUnavailable(proxy.Endpoint);
                                }
                            }
                            catch
                            {
                                RotatingRouter?.MarkUnavailable(proxy.Endpoint);
                            }
                        }

                        if (!connected)
                        {
                            throw new Exception("Unable to establish live connection across rotating proxy servers (exhausted live candidates)");
                        }
                    }
                    else
                    {
                        using var remoteSocket = CreateBoundSocket(adapter);
                        await remoteSocket.ConnectAsync(targetHost, targetPort, ct);
                        using var remoteStream = new NetworkStream(remoteSocket, true);

                        byte[] ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                        await clientStream.WriteAsync(ok, 0, ok.Length, ct);

                        AddAuditLog("HTTPS", targetHost, targetPort, adapter.Name, clientEp);
                        await PipeBidirectionalAsync(clientStream, remoteStream, adapter, ct);
                    }
                }
                else
                {
                    if (Uri.TryCreate(target, UriKind.Absolute, out var uri))
                    {
                        targetHost = uri.Host;
                        targetPort = uri.Port;
                    }
                    else
                    {
                        var parts = target.Split(':');
                        targetHost = parts[0];
                        targetPort = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 80;
                    }

                    using var remoteSocket = CreateBoundSocket(adapter);
                    await remoteSocket.ConnectAsync(targetHost, targetPort, ct);
                    using var remoteStream = new NetworkStream(remoteSocket, true);

                    await remoteStream.WriteAsync(buffer, 0, read, ct);
                    AddAuditLog("HTTP", targetHost, targetPort, adapter.Name, client.Client.RemoteEndPoint?.ToString() ?? "");
                    await PipeBidirectionalAsync(clientStream, remoteStream, adapter, ct);
                }
            }
        }
        catch (Exception) { }
        finally
        {
            Interlocked.Decrement(ref ActiveConnections);
        }
    }

    private async Task AcceptSocksClientsAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = HandleSocksClientAsync(client, ct);
            }
            catch when (ct.IsCancellationRequested) { break; }
            catch (Exception) { await Task.Delay(50, ct); }
        }
    }

    private async Task HandleSocksClientAsync(TcpClient client, CancellationToken ct)
    {
        Interlocked.Increment(ref ActiveConnections);
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buf = new byte[512];
                int read = await stream.ReadAsync(buf, 0, buf.Length, ct);
                if (read < 3 || buf[0] != 0x05) return;

                await stream.WriteAsync(new byte[] { 0x05, 0x00 }, 0, 2, ct);

                read = await stream.ReadAsync(buf, 0, buf.Length, ct);
                if (read < 4 || buf[0] != 0x05) return;

                byte cmd = buf[1];
                if (cmd != 0x01)
                {
                    await stream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, 0, 10, ct);
                    return;
                }

                byte atyp = buf[3];
                string targetHost = "";
                int targetPort = 0;
                int portOffset = 0;

                if (atyp == 0x01)
                {
                    var ip = new IPAddress(new[] { buf[4], buf[5], buf[6], buf[7] });
                    targetHost = ip.ToString();
                    portOffset = 8;
                }
                else if (atyp == 0x03)
                {
                    int len = buf[4];
                    targetHost = Encoding.ASCII.GetString(buf, 5, len);
                    portOffset = 5 + len;
                }
                else
                {
                    await stream.WriteAsync(new byte[] { 0x05, 0x08, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, 0, 10, ct);
                    return;
                }

                targetPort = (buf[portOffset] << 8) | buf[portOffset + 1];

                var adapter = _loadBalancer.GetNextAdapter();
                if (adapter == null) return;

                string clientEp = client.Client.RemoteEndPoint?.ToString() ?? "";
                bool viaRotator = _config?.RotatingProxy?.RouteBondedTrafficThroughRotator == true && RotatingRouter != null;

                if (viaRotator)
                {
                    int maxAttempts = Math.Min(12, Math.Max(3, RotatingRouter?.GetCandidatePool().Count ?? 1));
                    bool connected = false;

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        var proxy = RotatingRouter?.SelectUpstreamProxy(targetHost, clientEp);
                        if (proxy == null) break;

                        using var attemptSocket = CreateBoundSocket(adapter);
                        try
                        {
                            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                            connectCts.CancelAfter(2500);
                            await attemptSocket.ConnectAsync(proxy.Host, proxy.Port, connectCts.Token);
                            using var remoteStream = new NetworkStream(attemptSocket, true);
                            bool tunnelOk = await EstablishUpstreamProxyTunnelAsync(remoteStream, proxy, targetHost, targetPort, connectCts.Token);
                            if (tunnelOk)
                            {
                                byte[] success = new byte[] { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 };
                                await stream.WriteAsync(success, 0, success.Length, ct);
                                AddAuditLog($"SOCKS5 -> {proxy.Endpoint} ({proxy.CountryCode})", targetHost, targetPort, adapter.Name, clientEp, $"{proxy.Protocol.ToString().ToLower()}://{proxy.Endpoint}", true, proxy.LatencyMs, proxy.Host);
                                await PipeBidirectionalAsync(stream, remoteStream, adapter, ct);
                                connected = true;
                                break;
                            }
                            else
                            {
                                RotatingRouter?.MarkUnavailable(proxy.Endpoint);
                            }
                        }
                        catch
                        {
                            RotatingRouter?.MarkUnavailable(proxy.Endpoint);
                        }
                    }

                    if (!connected)
                    {
                        throw new Exception("Unable to establish live connection across rotating proxy servers (exhausted live candidates)");
                    }
                }
                else
                {
                    using var remoteSocket = CreateBoundSocket(adapter);
                    await remoteSocket.ConnectAsync(targetHost, targetPort, ct);
                    byte[] success = new byte[] { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 };
                    await stream.WriteAsync(success, 0, success.Length, ct);

                    using var remoteStream = new NetworkStream(remoteSocket, true);
                    AddAuditLog("SOCKS5", targetHost, targetPort, adapter.Name, clientEp);
                    await PipeBidirectionalAsync(stream, remoteStream, adapter, ct);
                }
            }
        }
        catch (Exception) { }
        finally
        {
            Interlocked.Decrement(ref ActiveConnections);
        }
    }

    private static async Task<bool> EstablishUpstreamProxyTunnelAsync(NetworkStream stream, HarvestedProxyModel proxy, string targetHost, int targetPort, CancellationToken token)
    {
        try
        {
            if (proxy.Protocol == ProxyProtocol.Socks5)
            {
                // RFC 1928 SOCKS5 Handshake
                bool hasAuth = !string.IsNullOrEmpty(proxy.Username);
                byte[] greeting = hasAuth ? new byte[] { 0x05, 0x02, 0x00, 0x02 } : new byte[] { 0x05, 0x01, 0x00 };
                await stream.WriteAsync(greeting, token);

                byte[] reply = new byte[2];
                int r = await stream.ReadAsync(reply.AsMemory(0, 2), token);
                if (r < 2 || reply[0] != 0x05) return false;

                if (reply[1] == 0x02 && hasAuth)
                {
                    var uBytes = Encoding.UTF8.GetBytes(proxy.Username ?? "");
                    var pBytes = Encoding.UTF8.GetBytes(proxy.Password ?? "");
                    var authMsg = new byte[3 + uBytes.Length + pBytes.Length];
                    authMsg[0] = 0x01;
                    authMsg[1] = (byte)uBytes.Length;
                    Array.Copy(uBytes, 0, authMsg, 2, uBytes.Length);
                    authMsg[2 + uBytes.Length] = (byte)pBytes.Length;
                    Array.Copy(pBytes, 0, authMsg, 3 + uBytes.Length, pBytes.Length);
                    await stream.WriteAsync(authMsg, token);

                    byte[] authReply = new byte[2];
                    r = await stream.ReadAsync(authReply.AsMemory(0, 2), token);
                    if (r < 2 || authReply[1] != 0x00) return false;
                }
                else if (reply[1] != 0x00)
                {
                    return false;
                }

                // SOCKS5 Connect
                byte[] req;
                if (IPAddress.TryParse(targetHost, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ipBytes = ip.GetAddressBytes();
                    req = new byte[10];
                    req[0] = 0x05;
                    req[1] = 0x01;
                    req[2] = 0x00;
                    req[3] = 0x01;
                    Array.Copy(ipBytes, 0, req, 4, 4);
                    req[8] = (byte)((targetPort >> 8) & 0xFF);
                    req[9] = (byte)(targetPort & 0xFF);
                }
                else
                {
                    var hostBytes = Encoding.ASCII.GetBytes(targetHost);
                    req = new byte[7 + hostBytes.Length];
                    req[0] = 0x05;
                    req[1] = 0x01;
                    req[2] = 0x00;
                    req[3] = 0x03;
                    req[4] = (byte)hostBytes.Length;
                    Array.Copy(hostBytes, 0, req, 5, hostBytes.Length);
                    req[5 + hostBytes.Length] = (byte)((targetPort >> 8) & 0xFF);
                    req[6 + hostBytes.Length] = (byte)(targetPort & 0xFF);
                }

                await stream.WriteAsync(req, token);
                byte[] resp = new byte[512];
                r = await stream.ReadAsync(resp.AsMemory(0, resp.Length), token);
                return r >= 4 && resp[0] == 0x05 && resp[1] == 0x00;
            }
            else if (proxy.Protocol == ProxyProtocol.Socks4)
            {
                // RFC SOCKS4 / SOCKS4a Handshake
                bool isIp = IPAddress.TryParse(targetHost, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork;
                byte[] userBytes = Encoding.ASCII.GetBytes(proxy.Username ?? "combispeed");
                byte[] req;

                if (isIp && ip != null)
                {
                    var ipBytes = ip.GetAddressBytes();
                    req = new byte[8 + userBytes.Length + 1];
                    req[0] = 0x04;
                    req[1] = 0x01;
                    req[2] = (byte)((targetPort >> 8) & 0xFF);
                    req[3] = (byte)(targetPort & 0xFF);
                    Array.Copy(ipBytes, 0, req, 4, 4);
                    Array.Copy(userBytes, 0, req, 8, userBytes.Length);
                    req[req.Length - 1] = 0x00;
                }
                else
                {
                    var hostBytes = Encoding.ASCII.GetBytes(targetHost);
                    req = new byte[8 + userBytes.Length + 1 + hostBytes.Length + 1];
                    req[0] = 0x04;
                    req[1] = 0x01;
                    req[2] = (byte)((targetPort >> 8) & 0xFF);
                    req[3] = (byte)(targetPort & 0xFF);
                    req[4] = 0x00;
                    req[5] = 0x00;
                    req[6] = 0x00;
                    req[7] = 0x01;
                    Array.Copy(userBytes, 0, req, 8, userBytes.Length);
                    req[8 + userBytes.Length] = 0x00;
                    Array.Copy(hostBytes, 0, req, 9 + userBytes.Length, hostBytes.Length);
                    req[req.Length - 1] = 0x00;
                }

                await stream.WriteAsync(req, token);
                byte[] resp = new byte[8];
                int r = await stream.ReadAsync(resp.AsMemory(0, 8), token);
                return r >= 8 && resp[0] == 0x00 && resp[1] == 0x5A;
            }
            else
            {
                // HTTP / HTTPS CONNECT tunnel
                var connectBuilder = new StringBuilder();
                connectBuilder.Append($"CONNECT {targetHost}:{targetPort} HTTP/1.1\r\n");
                connectBuilder.Append($"Host: {targetHost}:{targetPort}\r\n");
                connectBuilder.Append("Proxy-Connection: Keep-Alive\r\n");
                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{proxy.Username}:{proxy.Password}"));
                    connectBuilder.Append($"Proxy-Authorization: Basic {credentials}\r\n");
                }
                connectBuilder.Append("\r\n");

                byte[] connectReq = Encoding.ASCII.GetBytes(connectBuilder.ToString());
                await stream.WriteAsync(connectReq, token);

                byte[] buf = new byte[2048];
                int total = 0;
                while (total < buf.Length)
                {
                    int read = await stream.ReadAsync(buf.AsMemory(total, buf.Length - total), token);
                    if (read <= 0) return false;
                    total += read;
                    string res = Encoding.ASCII.GetString(buf, 0, total);
                    if (res.Contains("\r\n\r\n"))
                    {
                        var firstLine = res.Split("\r\n")[0];
                        return firstLine.Contains(" 200");
                    }
                }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private Socket CreateBoundSocket(NetworkAdapterModel adapter)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.NoDelay = true;

        if (IPAddress.TryParse(adapter.IpAddress, out var ip))
        {
            try
            {
                socket.Bind(new IPEndPoint(ip, 0));
            }
            catch { }
        }

        // On macOS (Darwin), IP_BOUND_IF (option 68) or SO_DONTROUTE binds outbound interface
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);
            }
            catch { }
        }

        return socket;
    }

    private async Task PipeBidirectionalAsync(Stream clientStream, Stream remoteStream, NetworkAdapterModel adapter, CancellationToken ct)
    {
        var clientToRemote = Task.Run(async () =>
        {
            var buf = new byte[16384];
            while (!ct.IsCancellationRequested)
            {
                int r = await clientStream.ReadAsync(buf, 0, buf.Length, ct);
                if (r <= 0) break;
                await _throttler.ThrottleAsync(r, adapter.Id);
                await remoteStream.WriteAsync(buf, 0, r, ct);
                Interlocked.Add(ref TotalBytesUploaded, r);
                adapter.UploadSpeedKbps += (r * 8.0) / 1024.0;
            }
        }, ct);

        var remoteToClient = Task.Run(async () =>
        {
            var buf = new byte[16384];
            while (!ct.IsCancellationRequested)
            {
                int r = await remoteStream.ReadAsync(buf, 0, buf.Length, ct);
                if (r <= 0) break;
                await _throttler.ThrottleAsync(r, adapter.Id);
                await clientStream.WriteAsync(buf, 0, r, ct);
                Interlocked.Add(ref TotalBytesDownloaded, r);
                adapter.DownloadSpeedKbps += (r * 8.0) / 1024.0;
            }
        }, ct);

        await Task.WhenAny(clientToRemote, remoteToClient);
    }

    private void AddAuditLog(string protocol, string host, int port, string adapter, string clientEp, string? proxyUrl = null, bool isSuccess = true, long latencyMs = 0, string? resolvedIp = null, string? error = null)
    {
        if (_config?.EnableAuditLogging == false) return;
        lock (RecentLogs)
        {
            RecentLogs.Insert(0, new AuditLogModel
            {
                Protocol = protocol,
                TargetHost = host,
                TargetPort = port,
                OutboundAdapter = adapter,
                ClientEndpoint = clientEp,
                Timestamp = DateTime.Now,
                ProxyServerUrl = proxyUrl ?? "Direct Multi-WAN",
                IsSuccess = isSuccess,
                LatencyMs = latencyMs,
                ResolvedIp = resolvedIp ?? host,
                ErrorMessage = error
            });

            if (RecentLogs.Count > 100) RecentLogs.RemoveAt(RecentLogs.Count - 1);
        }
    }
}

