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

    public event Action<AuditLogModel>? OnAuditLog;
    public event Action? OnTelemetryTick;

    public ProxyEngine(LoadBalancerEngine loadBalancer, Func<List<NetworkAdapterModel>> getAdapters)
    {
        _loadBalancer = loadBalancer;
        _getAdapters = getAdapters;
    }

    public List<AuditLogModel> GetAuditLogs() => _auditLogs.ToList();

    public async Task StartAsync(int httpPort = 8080, int socksPort = 1080)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        StartedAt = DateTime.Now;

        _httpListener = new TcpListener(IPAddress.Any, httpPort);
        _httpListener.Start(100);

        _socksListener = new TcpListener(IPAddress.Any, socksPort);
        _socksListener.Start(100);

        IsRunning = true;

        // Start accepting clients
        _ = AcceptClientsAsync(_httpListener, HandleHttpClientAsync, _cts.Token);
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
                if (reqBytes < 4 || reqHeader[0] != 0x05 || reqHeader[1] != 0x01) // Only CONNECT supported
                {
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0, 0, 0, 0, 0, 0 }, token);
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
                    Protocol = "SOCKS5",
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

    private void RecordBytes(NetworkAdapterModel adapter, int rx, int tx)
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

    public void Dispose()
    {
        _ = StopAsync();
        _cts?.Dispose();
    }
}
