using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxProxyEngine
{
    private readonly ProxyConfigModel _config;
    private readonly LinuxLoadBalancer _loadBalancer;
    private readonly LinuxBandwidthThrottler _throttler;
    private TcpListener? _httpListener;
    private TcpListener? _socksListener;
    private CancellationTokenSource? _cts;

    public long TotalBytesDownloaded;
    public long TotalBytesUploaded;
    public int ActiveConnections;
    public List<AuditLogModel> RecentLogs { get; } = new();

    public LinuxProxyEngine(ProxyConfigModel config, LinuxLoadBalancer loadBalancer, LinuxBandwidthThrottler throttler)
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
            _httpListener = new TcpListener(IPAddress.Any, _config.HttpPort);
            _httpListener.Start(100);
            _ = AcceptHttpClientsAsync(_httpListener, _cts.Token);

            _socksListener = new TcpListener(IPAddress.Any, _config.SocksPort);
            _socksListener.Start(100);
            _ = AcceptSocksClientsAsync(_socksListener, _cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LinuxProxyEngine] Error starting listeners: {ex.Message}");
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
                // RFC 1928 Version Identifier & Method Selection
                int read = await stream.ReadAsync(buf, 0, buf.Length, ct);
                if (read < 3 || buf[0] != 0x05) return;

                // Send NO AUTHENTICATION REQUIRED (0x05, 0x00)
                await stream.WriteAsync(new byte[] { 0x05, 0x00 }, 0, 2, ct);

                // Read request
                read = await stream.ReadAsync(buf, 0, buf.Length, ct);
                if (read < 4 || buf[0] != 0x05) return;

                byte cmd = buf[1];
                if (cmd != 0x01) // We support CONNECT (0x01)
                {
                    await stream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, 0, 10, ct);
                    return;
                }

                byte atyp = buf[3];
                string targetHost = "";
                int targetPort = 0;
                int portOffset = 0;

                if (atyp == 0x01) // IPv4
                {
                    var ip = new IPAddress(new[] { buf[4], buf[5], buf[6], buf[7] });
                    targetHost = ip.ToString();
                    portOffset = 8;
                }
                else if (atyp == 0x03) // Domain name
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

                // Send SOCKS5 Success response
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

        // On Linux, bind socket to the physical interface name (SO_BINDTODEVICE = 25)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !string.IsNullOrEmpty(adapter.InterfaceName))
        {
            try
            {
                byte[] ifNameBytes = Encoding.ASCII.GetBytes(adapter.InterfaceName + "\0");
                socket.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)25, ifNameBytes);
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

