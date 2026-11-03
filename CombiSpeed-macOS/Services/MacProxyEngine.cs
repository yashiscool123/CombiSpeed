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

                    using var remoteSocket = CreateBoundSocket(adapter);
                    await remoteSocket.ConnectAsync(targetHost, targetPort, ct);
                    using var remoteStream = new NetworkStream(remoteSocket, true);

                    byte[] ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                    await clientStream.WriteAsync(ok, 0, ok.Length, ct);

                    AddAuditLog("HTTPS", targetHost, targetPort, adapter.Name, client.Client.RemoteEndPoint?.ToString() ?? "");
                    await PipeBidirectionalAsync(clientStream, remoteStream, adapter, ct);
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

                using var remoteSocket = CreateBoundSocket(adapter);
                await remoteSocket.ConnectAsync(targetHost, targetPort, ct);

                byte[] success = new byte[] { 0x05, 0x00, 0x00, 0x01, 127, 0, 0, 1, 0, 0 };
                await stream.WriteAsync(success, 0, success.Length, ct);

                using var remoteStream = new NetworkStream(remoteSocket, true);
                AddAuditLog("SOCKS5", targetHost, targetPort, adapter.Name, client.Client.RemoteEndPoint?.ToString() ?? "");
                await PipeBidirectionalAsync(stream, remoteStream, adapter, ct);
            }
        }
        catch (Exception) { }
        finally
        {
            Interlocked.Decrement(ref ActiveConnections);
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

    private void AddAuditLog(string protocol, string host, int port, string adapter, string clientEp)
    {
        lock (RecentLogs)
        {
            RecentLogs.Insert(0, new AuditLogModel
            {
                Protocol = protocol,
                TargetHost = host,
                TargetPort = port,
                OutboundAdapter = adapter,
                ClientEndpoint = clientEp,
                Timestamp = DateTime.Now
            });

            if (RecentLogs.Count > 100) RecentLogs.RemoveAt(RecentLogs.Count - 1);
        }
    }
}

