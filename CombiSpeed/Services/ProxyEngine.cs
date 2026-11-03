using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class ProxyEngine : IDisposable
{
    private readonly LoadBalancerEngine _loadBalancer;
    private readonly Func<List<NetworkAdapterModel>> _getAdapters;

    private TcpListener? _httpListener;
    private TcpListener? _socksListener;
    private CancellationTokenSource? _cts;

    private int _activeConnections;
    private int _totalConnections;
    private long _totalRxBytes;
    private long _totalTxBytes;

    private long _currentSecRx;
    private long _currentSecTx;
    private PeriodicTimer? _telemetryTimer;
    private readonly BandwidthThrottler _throttler = new();

    private readonly ConcurrentQueue<AuditLogModel> _auditLogs = new();
    private readonly int _maxLogs = 100;

    public bool IsRunning { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public int ActiveConnections => _activeConnections;
    public int TotalConnections => _totalConnections;
    public long TotalDownloadBytes => _totalRxBytes;
    public long TotalUploadBytes => _totalTxBytes;
    public long InstantDownloadSpeedBps { get; private set; }
    public long InstantUploadSpeedBps { get; private set; }
    public BandwidthThrottler Throttler => _throttler;

    public event Action<AuditLogModel>? OnAuditLog;
    public event Action? OnTelemetryTick;

    private int _httpPort = 8080;
    private int _socksPort = 1080;

    public ProxyEngine(LoadBalancerEngine loadBalancer, Func<List<NetworkAdapterModel>> getAdapters)
    {
        _loadBalancer = loadBalancer;
        _getAdapters = getAdapters;
    }

    public List<AuditLogModel> GetAuditLogs() => _auditLogs.ToList();

    public void UpdateThrottling(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config)
    {
        _throttler.UpdateConfiguration(adapters, config);
    }

    public async Task StartAsync(int httpPort = 8080, int socksPort = 1080)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        StartedAt = DateTime.Now;

        // 1. HTTP Listener with SO_REUSEADDR and conflict fallback
        int curHttpPort = httpPort;
        int httpAttempts = 0;
        TcpListener? activeHttp = null;
        while (httpAttempts < 20)
        {
            try
            {
                var l = new TcpListener(IPAddress.Any, curHttpPort);
                l.ExclusiveAddressUse = false;
                l.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                l.Start(100);
                activeHttp = l;
                break;
            }
            catch (SocketException)
            {
                curHttpPort++;
                httpAttempts++;
            }
        }

        // 2. SOCKS5 Listener with SO_REUSEADDR and conflict fallback
        int curSocksPort = socksPort;
        int socksAttempts = 0;
        TcpListener? activeSocks = null;
        while (socksAttempts < 20)
        {
            try
            {
                var l = new TcpListener(IPAddress.Any, curSocksPort);
                l.ExclusiveAddressUse = false;
                l.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                l.Start(100);
                activeSocks = l;
                break;
            }
            catch (SocketException)
            {
                curSocksPort++;
                socksAttempts++;
            }
        }

        if (activeHttp == null && activeSocks == null)
        {
            IsRunning = false;
            OnAuditLog?.Invoke(new AuditLogModel
            {
                Protocol = "ENGINE",
                TargetHost = "0.0.0.0",
                Status = $"Listener bind failed: ports {httpPort} and {socksPort} unavailable.",
                Timestamp = DateTime.Now
            });
            return;
        }

        _httpPort = curHttpPort;
        _socksPort = curSocksPort;
        _httpListener = activeHttp;
        _socksListener = activeSocks;
        IsRunning = true;

        if (curHttpPort != httpPort)
        {
            OnAuditLog?.Invoke(new AuditLogModel
            {
                Protocol = "HTTP",
                TargetHost = $"127.0.0.1:{curHttpPort}",
                Status = $"Port {httpPort} in use; shifted HTTP proxy to port {curHttpPort}",
                Timestamp = DateTime.Now
            });
        }
        if (curSocksPort != socksPort)
        {
            OnAuditLog?.Invoke(new AuditLogModel
            {
                Protocol = "SOCKS",
                TargetHost = $"127.0.0.1:{curSocksPort}",
                Status = $"Port {socksPort} in use; shifted SOCKS proxy to port {curSocksPort}",
                Timestamp = DateTime.Now
            });
        }

        // Start accepting clients
        if (_httpListener != null)
            _ = AcceptClientsAsync(_httpListener, HandleHttpClientAsync, _cts.Token);
        if (_socksListener != null)
            _ = AcceptClientsAsync(_socksListener, HandleSocksClientAsync, _cts.Token);

        // Start 1-second telemetry speed calculator
        _telemetryTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RunTelemetryLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _telemetryTimer?.Dispose();
        _telemetryTimer = null;

        try { _httpListener?.Stop(); } catch { }
        try { _socksListener?.Stop(); } catch { }

        _httpListener = null;
        _socksListener = null;
        IsRunning = false;
        _activeConnections = 0;
    }

    private async Task AcceptClientsAsync(TcpListener listener, Func<TcpClient, CancellationToken, Task> handler, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => handler(client, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Accept error or listener stop
            }
        }
    }

    #region HTTP / HTTPS CONNECT Proxy
    private async Task HandleHttpClientAsync(TcpClient client, CancellationToken token)
    {
        Interlocked.Increment(ref _activeConnections);
        Interlocked.Increment(ref _totalConnections);
        var sw = Stopwatch.StartNew();
        var clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        string targetHost = "";
        int targetPort = 80;
        string protocol = "HTTP";
        NetworkAdapterModel? chosenAdapter = null;
        long bytesSession = 0;
        string? error = null;

        try
        {
            using (client)
            using (var clientStream = client.GetStream())
            {
                // Read HTTP request line
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                int bytesRead = 0;
                try
                {
                    bytesRead = await clientStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                }
                finally
                {
                    // keep buffer for parsing
                }

                if (bytesRead <= 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                string headerText = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                var lines = headerText.Split("\r\n");
                var requestLine = lines[0].Split(' ');

                if (requestLine.Length < 2)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                string method = requestLine[0].ToUpperInvariant();
                string uriStr = requestLine[1];

                // Built-in PAC (Proxy Auto-Config) Script Server
                if (method == "GET" && (uriStr.Contains("proxy.pac", StringComparison.OrdinalIgnoreCase) || uriStr.EndsWith("/pac", StringComparison.OrdinalIgnoreCase)))
                {
                    string pacScript = PacConfigModel.Current.GeneratePacScript(_httpPort, _socksPort);
                    byte[] pacBytes = Encoding.UTF8.GetBytes(pacScript);
                    string pacResponse = $"HTTP/1.1 200 OK\r\nContent-Type: application/x-ns-proxy-autoconfig; charset=utf-8\r\nContent-Length: {pacBytes.Length}\r\nConnection: close\r\n\r\n";
                    await clientStream.WriteAsync(Encoding.ASCII.GetBytes(pacResponse), token);
                    await clientStream.WriteAsync(pacBytes, token);
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                // Select target adapter
                var adapters = _getAdapters();
                chosenAdapter = _loadBalancer.GetNextAdapter(adapters);
                if (chosenAdapter == null || !IPAddress.TryParse(chosenAdapter.Address, out var localIp))
                {
                    byte[] badGw = Encoding.ASCII.GetBytes("HTTP/1.1 502 Bad Gateway\r\n\r\nCombiSpeed: No Network Adapters");
                    await clientStream.WriteAsync(badGw, token);
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                if (method == "CONNECT")
                {
                    // HTTPS CONNECT tunnel
                    protocol = "HTTPS (CONNECT)";
                    var parts = uriStr.Split(':');
                    targetHost = parts[0];
                    targetPort = parts.Length > 1 ? int.Parse(parts[1]) : 443;

                    ArrayPool<byte>.Shared.Return(buffer);

                    // Establish bound outbound connection
                    using var targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Bind(new IPEndPoint(localIp, 0)); // BIND SOCKET TO ADAPTER IP!

                    using var connectCts = new CancellationTokenSource(5000);
                    await targetSocket.ConnectAsync(targetHost, targetPort, connectCts.Token);

                    byte[] okReply = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                    await clientStream.WriteAsync(okReply, token);

                    using var targetStream = new NetworkStream(targetSocket, ownsSocket: false);
                    bytesSession = await PipeBiDirectionalAsync(clientStream, targetStream, chosenAdapter, token);
                }
                else
                {
                    // Plain HTTP GET / POST
                    protocol = "HTTP";
                    Uri uri = uriStr.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? new Uri(uriStr)
                        : new Uri($"http://{lines.FirstOrDefault(l => l.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))?.Substring(5).Trim() ?? "localhost"}{uriStr}");

                    targetHost = uri.Host;
                    targetPort = uri.Port > 0 ? uri.Port : 80;

                    var activeAdapters = adapters.Where(a => a.IsEnabled && a.Status != AdapterStatus.Down && !string.IsNullOrWhiteSpace(a.Address)).ToList();

                    // Multi-NIC Parallel Range Chunk Speed Combiner: combine adapter speeds for single downloads
                    if (method == "GET" && activeAdapters.Count >= 2 && !headerText.Contains("Range:", StringComparison.OrdinalIgnoreCase))
                    {
                        var (combinedSuccess, transferred) = await TryCombinedRangeDownloadAsync(clientStream, uri, targetHost, targetPort, headerText, activeAdapters, token);
                        if (combinedSuccess)
                        {
                            bytesSession = transferred;
                            ArrayPool<byte>.Shared.Return(buffer);
                            return;
                        }
                    }

                    using var targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Bind(new IPEndPoint(localIp, 0)); // BIND SOCKET TO ADAPTER IP!

                    using var connectCts = new CancellationTokenSource(5000);
                    await targetSocket.ConnectAsync(targetHost, targetPort, connectCts.Token);

                    using var targetStream = new NetworkStream(targetSocket, ownsSocket: false);
                    await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    RecordBytes(chosenAdapter, 0, bytesRead);
                    ArrayPool<byte>.Shared.Return(buffer);

                    bytesSession = await PipeBiDirectionalAsync(clientStream, targetStream, chosenAdapter, token);
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            sw.Stop();
            Interlocked.Decrement(ref _activeConnections);

            if (chosenAdapter != null && !string.IsNullOrEmpty(targetHost))
            {
                var log = new AuditLogModel
                {
                    Protocol = protocol,
                    TargetHost = targetHost,
                    TargetPort = targetPort,
                    AdapterId = chosenAdapter.Id,
                    AdapterName = chosenAdapter.Name,
                    ClientIp = clientIp,
                    BytesTransferred = bytesSession,
                    DurationMs = sw.ElapsedMilliseconds,
                    Status = error == null ? "Completed" : "Failed",
                    ErrorMessage = error
                };

                AddAuditLog(log);
            }
        }
    }
    #endregion

    #region RFC 1928 SOCKS5 Proxy
    private async Task HandleSocksClientAsync(TcpClient client, CancellationToken token)
    {
        Interlocked.Increment(ref _activeConnections);
        Interlocked.Increment(ref _totalConnections);
        var sw = Stopwatch.StartNew();
        var clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        string targetHost = "";
        int targetPort = 0;
        string protocol = "SOCKS5";
        NetworkAdapterModel? chosenAdapter = null;
        long bytesSession = 0;
        string? error = null;

        try
        {
            using (client)
            using (var clientStream = client.GetStream())
            {
                byte[] handshake = new byte[256];
                int hsBytes = await clientStream.ReadAsync(handshake.AsMemory(0, 2), token);
                if (hsBytes < 2 || handshake[0] != 0x05) return;

                int numMethods = handshake[1];
                await clientStream.ReadAsync(handshake.AsMemory(0, numMethods), token);

                // No Auth Required response: [0x05, 0x00]
                await clientStream.WriteAsync(new byte[] { 0x05, 0x00 }, token);

                // Read connection request
                byte[] reqHeader = new byte[4];
                int reqBytes = await clientStream.ReadAsync(reqHeader.AsMemory(0, 4), token);
                if (reqBytes < 4 || reqHeader[0] != 0x05) return;

                byte cmd = reqHeader[1];
                if (cmd != 0x01 && cmd != 0x03) // 0x01: CONNECT, 0x03: UDP ASSOCIATE
                {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, token);
                    return;
                }

                if (cmd == 0x03)
                {
                    // RFC 1928 SOCKS5 UDP ASSOCIATE RELAY
                    protocol = "SOCKS5 (UDP ASSOCIATE)";
                    byte bndAddrType = reqHeader[3];
                    IPEndPoint? clientUdpEndpoint = null;

                    if (bndAddrType == 0x01)
                    {
                        byte[] ipBytes = new byte[4];
                        await clientStream.ReadAsync(ipBytes.AsMemory(0, 4), token);
                        byte[] clientPortBytes = new byte[2];
                        await clientStream.ReadAsync(clientPortBytes.AsMemory(0, 2), token);
                        int port = (clientPortBytes[0] << 8) | clientPortBytes[1];
                        clientUdpEndpoint = new IPEndPoint(new IPAddress(ipBytes), port);
                    }
                    else
                    {
                        int skip = (bndAddrType == 0x03) ? clientStream.ReadByte() : 16;
                        byte[] dummy = new byte[skip + 2];
                        await clientStream.ReadAsync(dummy.AsMemory(0, dummy.Length), token);
                    }

                    using var udpRelay = new Socks5UdpRelay(_loadBalancer, _getAdapters, RecordBytes, AddAuditLog, _throttler);
                    int assignedRelayPort = udpRelay.StartRelay(clientUdpEndpoint);

                    // Send Success Reply: [0x05, 0x00 (Rep), 0x00 (Rsv), 0x01 (IPv4), BND.ADDR (127.0.0.1), BND.PORT (2 bytes)]
                    byte[] udpReply = new byte[] {
                        0x05, 0x00, 0x00, 0x01,
                        127, 0, 0, 1,
                        (byte)((assignedRelayPort >> 8) & 0xFF),
                        (byte)(assignedRelayPort & 0xFF)
                    };
                    await clientStream.WriteAsync(udpReply, token);

                    // Hold TCP control channel until client disconnects
                    byte[] hold = new byte[1];
                    while (!token.IsCancellationRequested)
                    {
                        int r = await clientStream.ReadAsync(hold.AsMemory(0, 1), token);
                        if (r <= 0) break;
                    }
                    return;
                }

                byte addrType = reqHeader[3];
                if (addrType == 0x01) // IPv4
                {
                    byte[] ipBytes = new byte[4];
                    await clientStream.ReadAsync(ipBytes.AsMemory(0, 4), token);
                    targetHost = new IPAddress(ipBytes).ToString();
                }
                else if (addrType == 0x03) // Domain Name
                {
                    int domainLen = clientStream.ReadByte();
                    byte[] domainBytes = new byte[domainLen];
                    await clientStream.ReadAsync(domainBytes.AsMemory(0, domainLen), token);
                    targetHost = Encoding.ASCII.GetString(domainBytes);
                }
                else
                {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x08, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, token);
                    return;
                }

                byte[] portBytes = new byte[2];
                await clientStream.ReadAsync(portBytes.AsMemory(0, 2), token);
                targetPort = (portBytes[0] << 8) | portBytes[1];

                // Select target adapter
                var adapters = _getAdapters();
                chosenAdapter = _loadBalancer.GetNextAdapter(adapters);
                if (chosenAdapter == null || !IPAddress.TryParse(chosenAdapter.Address, out var localIp))
                {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x01, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, token);
                    return;
                }

                // Connect outbound socket explicitly bound to selected adapter local IP
                using var targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Bind(new IPEndPoint(localIp, 0));

                using var connectCts = new CancellationTokenSource(5000);
                await targetSocket.ConnectAsync(targetHost, targetPort, connectCts.Token);

                // Success reply: [0x05, 0x00, 0x00, 0x01, 0,0,0,0, 0,0]
                byte[] reply = new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
                await clientStream.WriteAsync(reply, token);

                using var targetStream = new NetworkStream(targetSocket, ownsSocket: false);
                bytesSession = await PipeBiDirectionalAsync(clientStream, targetStream, chosenAdapter, token);
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            sw.Stop();
            Interlocked.Decrement(ref _activeConnections);

            if (chosenAdapter != null && !string.IsNullOrEmpty(targetHost))
            {
                var log = new AuditLogModel
                {
                    Protocol = protocol,
                    TargetHost = targetHost,
                    TargetPort = targetPort,
                    AdapterId = chosenAdapter.Id,
                    AdapterName = chosenAdapter.Name,
                    ClientIp = clientIp,
                    BytesTransferred = bytesSession,
                    DurationMs = sw.ElapsedMilliseconds,
                    Status = error == null ? "Completed" : "Failed",
                    ErrorMessage = error
                };

                AddAuditLog(log);
            }
        }
    }
    #endregion

    private async Task<long> PipeBiDirectionalAsync(NetworkStream streamA, NetworkStream streamB, NetworkAdapterModel adapter, CancellationToken token)
    {
        long totalSessionBytes = 0;

        var taskAtoB = Task.Run(async () =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16384);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = await streamA.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (read <= 0) break;

                    await _throttler.ThrottleUploadAsync(adapter, read, token);
                    await streamB.WriteAsync(buffer.AsMemory(0, read), token);
                    RecordBytes(adapter, 0, read);
                    Interlocked.Add(ref totalSessionBytes, read);
                }
            }
            catch { }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }, token);

        var taskBtoA = Task.Run(async () =>
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16384);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = await streamB.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (read <= 0) break;

                    await _throttler.ThrottleDownloadAsync(adapter, read, token);
                    await streamA.WriteAsync(buffer.AsMemory(0, read), token);
                    RecordBytes(adapter, read, 0);
                    Interlocked.Add(ref totalSessionBytes, read);
                }
            }
            catch { }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }, token);

        await Task.WhenAny(taskAtoB, taskBtoA);
        return totalSessionBytes;
    }

    private void RecordBytes(NetworkAdapterModel adapter, long rx, long tx)
    {
        if (rx > 0)
        {
            Interlocked.Add(ref _totalRxBytes, rx);
            Interlocked.Add(ref _currentSecRx, rx);
            adapter.TotalRxBytes += rx;
        }
        if (tx > 0)
        {
            Interlocked.Add(ref _totalTxBytes, tx);
            Interlocked.Add(ref _currentSecTx, tx);
            adapter.TotalTxBytes += tx;
        }
    }

    private async Task RunTelemetryLoopAsync(CancellationToken token)
    {
        var lastAdapterBytes = new Dictionary<string, (long rx, long tx)>();

        while (_telemetryTimer != null && await _telemetryTimer.WaitForNextTickAsync(token))
        {
            long rx = Interlocked.Exchange(ref _currentSecRx, 0);
            long tx = Interlocked.Exchange(ref _currentSecTx, 0);

            InstantDownloadSpeedBps = rx;
            InstantUploadSpeedBps = tx;

            var adapters = _getAdapters();
            foreach (var adapter in adapters)
            {
                lastAdapterBytes.TryGetValue(adapter.Id, out var prev);
                long deltaRx = Math.Max(0, adapter.TotalRxBytes - prev.rx);
                long deltaTx = Math.Max(0, adapter.TotalTxBytes - prev.tx);

                adapter.DownloadSpeedBps = deltaRx;
                adapter.UploadSpeedBps = deltaTx;

                lastAdapterBytes[adapter.Id] = (adapter.TotalRxBytes, adapter.TotalTxBytes);
            }

            OnTelemetryTick?.Invoke();
        }
    }

    private void AddAuditLog(AuditLogModel log)
    {
        _auditLogs.Enqueue(log);
        while (_auditLogs.Count > _maxLogs && _auditLogs.TryDequeue(out _)) { }
        OnAuditLog?.Invoke(log);
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (int i = 0; i <= length - 4; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
            {
                return i + 4;
            }
        }
        return -1;
    }

    private async Task<(bool success, long bytesTransferred)> TryCombinedRangeDownloadAsync(
        NetworkStream clientStream,
        Uri uri,
        string targetHost,
        int targetPort,
        string originalHeader,
        List<NetworkAdapterModel> activeAdapters,
        CancellationToken token)
    {
        try
        {
            var primaryAdapter = activeAdapters[0];
            if (!IPAddress.TryParse(primaryAdapter.Address, out var primaryIp)) return (false, 0);

            using var probeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            probeSocket.Bind(new IPEndPoint(primaryIp, 0));
            using var connectCts = new CancellationTokenSource(3000);
            await probeSocket.ConnectAsync(targetHost, targetPort, connectCts.Token);

            using var probeStream = new NetworkStream(probeSocket, ownsSocket: false);
            byte[] reqBytes = Encoding.ASCII.GetBytes(originalHeader);
            await probeStream.WriteAsync(reqBytes, token);

            byte[] headerBuf = new byte[16384];
            int readTotal = 0;
            int headerEnd = -1;

            while (readTotal < headerBuf.Length)
            {
                int r = await probeStream.ReadAsync(headerBuf.AsMemory(readTotal, headerBuf.Length - readTotal), token);
                if (r <= 0) break;
                readTotal += r;
                headerEnd = FindHeaderEnd(headerBuf, readTotal);
                if (headerEnd >= 0) break;
            }

            if (headerEnd < 0) return (false, 0);

            string respHeader = Encoding.ASCII.GetString(headerBuf, 0, headerEnd);
            var headerLines = respHeader.Split("\r\n");
            if (headerLines.Length == 0 || !headerLines[0].Contains("200")) return (false, 0);

            long contentLength = -1;
            bool acceptRanges = false;
            foreach (var line in headerLines)
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    long.TryParse(line.Substring(15).Trim(), out contentLength);
                }
                else if (line.StartsWith("Accept-Ranges:", StringComparison.OrdinalIgnoreCase) && line.Contains("bytes", StringComparison.OrdinalIgnoreCase))
                {
                    acceptRanges = true;
                }
            }

            // Only engage chunk aggregation for sizable downloads that support range queries
            if (contentLength < 512 * 1024 || !acceptRanges)
            {
                await clientStream.WriteAsync(headerBuf.AsMemory(0, readTotal), token);
                RecordBytes(primaryAdapter, readTotal, 0);
                await _throttler.ThrottleDownloadAsync(primaryAdapter, readTotal, token);
                long remainingPiped = await PipeBiDirectionalAsync(clientStream, probeStream, primaryAdapter, token);
                return (true, readTotal + remainingPiped);
            }

            // Forward the original response headers to client
            await clientStream.WriteAsync(headerBuf.AsMemory(0, headerEnd), token);

            // Write any initial body bytes already read
            int initialBodyBytes = readTotal - headerEnd;
            long deliveredBytes = 0;
            if (initialBodyBytes > 0)
            {
                await clientStream.WriteAsync(headerBuf.AsMemory(headerEnd, initialBodyBytes), token);
                RecordBytes(primaryAdapter, initialBodyBytes, 0);
                await _throttler.ThrottleDownloadAsync(primaryAdapter, initialBodyBytes, token);
                deliveredBytes += initialBodyBytes;
            }

            // Parallel Multi-NIC Range Chunk Aggregator: fetch remaining bytes across ALL active adapters simultaneously!
            int chunkSize = 512 * 1024; // 512 KB per chunk
            string pathAndQuery = uri.PathAndQuery;

            while (deliveredBytes < contentLength && !token.IsCancellationRequested)
            {
                int chunksInBatch = Math.Min(activeAdapters.Count * 2, (int)Math.Ceiling((double)(contentLength - deliveredBytes) / chunkSize));
                var batchTasks = new Task<(int index, byte[] data, NetworkAdapterModel adapter)>[chunksInBatch];

                for (int b = 0; b < chunksInBatch; b++)
                {
                    int chunkIdx = b;
                    long chunkStart = deliveredBytes + (b * (long)chunkSize);
                    long chunkEnd = Math.Min(contentLength - 1, chunkStart + chunkSize - 1);
                    var adapter = activeAdapters[b % activeAdapters.Count];

                    batchTasks[b] = Task.Run(async () =>
                    {
                        if (!IPAddress.TryParse(adapter.Address, out var localIp)) return (chunkIdx, Array.Empty<byte>(), adapter);
                        using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        s.Bind(new IPEndPoint(localIp, 0));
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                        cts.CancelAfter(8000);
                        await s.ConnectAsync(targetHost, targetPort, cts.Token);

                        string rangeReq = $"GET {pathAndQuery} HTTP/1.1\r\nHost: {targetHost}\r\nRange: bytes={chunkStart}-{chunkEnd}\r\nConnection: close\r\n\r\n";
                        byte[] reqData = Encoding.ASCII.GetBytes(rangeReq);
                        using var ns = new NetworkStream(s, ownsSocket: true);
                        await ns.WriteAsync(reqData, cts.Token);

                        byte[] chunkHeaderBuf = new byte[4096];
                        int rTot = 0;
                        int cHeadEnd = -1;
                        while (rTot < chunkHeaderBuf.Length)
                        {
                            int r = await ns.ReadAsync(chunkHeaderBuf.AsMemory(rTot, chunkHeaderBuf.Length - rTot), cts.Token);
                            if (r <= 0) break;
                            rTot += r;
                            cHeadEnd = FindHeaderEnd(chunkHeaderBuf, rTot);
                            if (cHeadEnd >= 0) break;
                        }

                        if (cHeadEnd < 0) return (chunkIdx, Array.Empty<byte>(), adapter);

                        int expectedBytes = (int)(chunkEnd - chunkStart + 1);
                        byte[] resultData = new byte[expectedBytes];
                        int written = 0;

                        int initialFromHead = rTot - cHeadEnd;
                        if (initialFromHead > 0)
                        {
                            int copyLen = Math.Min(initialFromHead, expectedBytes);
                            Array.Copy(chunkHeaderBuf, cHeadEnd, resultData, 0, copyLen);
                            written += copyLen;
                        }

                        while (written < expectedBytes)
                        {
                            int r = await ns.ReadAsync(resultData.AsMemory(written, expectedBytes - written), cts.Token);
                            if (r <= 0) break;
                            written += r;
                        }

                        return (chunkIdx, resultData, adapter);
                    }, token);
                }

                var completedBatch = await Task.WhenAll(batchTasks);
                foreach (var item in completedBatch.OrderBy(x => x.index))
                {
                    if (item.data.Length == 0) return (false, deliveredBytes); // fallback if a chunk fails

                    await clientStream.WriteAsync(item.data, token);
                    RecordBytes(item.adapter, item.data.Length, 0);
                    await _throttler.ThrottleDownloadAsync(item.adapter, item.data.Length, token);
                    deliveredBytes += item.data.Length;
                }
            }

            return (true, deliveredBytes);
        }
        catch
        {
            return (false, 0);
        }
    }

    public void Dispose()
    {
        _ = StopAsync();
        _cts?.Dispose();
    }
}
