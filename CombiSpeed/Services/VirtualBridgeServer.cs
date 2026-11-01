using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class VirtualBridgeServer : IDisposable
{
    private readonly LoadBalancerEngine _loadBalancer;
    private readonly Func<List<NetworkAdapterModel>> _getAdapters;
    private readonly VirtualBridgeModel _model;
    private readonly BandwidthThrottler _throttler;

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _activeStreams;
    private int _totalStreams;

    public bool IsRunning { get; private set; }
    public VirtualBridgeModel Model => _model;
    public BandwidthThrottler Throttler => _throttler;

    public event Action<AuditLogModel>? OnAuditLog;

    public VirtualBridgeServer(LoadBalancerEngine loadBalancer, Func<List<NetworkAdapterModel>> getAdapters, VirtualBridgeModel model, BandwidthThrottler? throttler = null)
    {
        _loadBalancer = loadBalancer;
        _getAdapters = getAdapters;
        _model = model;
        _throttler = throttler ?? new BandwidthThrottler();
    }

    public async Task StartAsync(int port = 8443)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start(100);
        IsRunning = true;
        _model.IsConnected = true;

        _ = AcceptLoopAsync(_listener, _cts.Token);
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener = null;
        IsRunning = false;
        _model.IsConnected = false;
        _activeStreams = 0;
        _model.ActiveStreams = 0;
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleBridgeClientAsync(client, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Listener stopped or accept error
            }
        }
    }

    private async Task HandleBridgeClientAsync(TcpClient client, CancellationToken token)
    {
        Interlocked.Increment(ref _activeStreams);
        Interlocked.Increment(ref _totalStreams);
        _model.ActiveStreams = _activeStreams;

        var sw = Stopwatch.StartNew();
        var clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        string targetHost = "";
        int targetPort = 80;
        string protocol = "VIRTUAL-BRIDGE (TCP)";
        NetworkAdapterModel? chosenAdapter = null;
        long bytesSession = 0;
        string? error = null;

        try
        {
            using (client)
            using (var clientStream = client.GetStream())
            {
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                int bytesRead = 0;
                try
                {
                    bytesRead = await clientStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                }
                finally
                {
                    // keep buffer
                }

                if (bytesRead <= 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                // Check if HTTP / HTTPS CONNECT request
                string headerText = Encoding.ASCII.GetString(buffer, 0, Math.Min(bytesRead, 1024));

                if (headerText.StartsWith("CONNECT ", StringComparison.OrdinalIgnoreCase))
                {
                    protocol = "VIRTUAL-BRIDGE (HTTPS)";
                    var parts = headerText.Split(' ');
                    if (parts.Length > 1)
                    {
                        var hostPort = parts[1].Split(':');
                        targetHost = hostPort[0];
                        targetPort = hostPort.Length > 1 ? int.Parse(hostPort[1]) : 443;
                    }
                }
                else if (headerText.StartsWith("GET ", StringComparison.OrdinalIgnoreCase) ||
                         headerText.StartsWith("POST ", StringComparison.OrdinalIgnoreCase) ||
                         headerText.StartsWith("HEAD ", StringComparison.OrdinalIgnoreCase))
                {
                    protocol = "VIRTUAL-BRIDGE (HTTP)";
                    var lines = headerText.Split("\r\n");
                    var hostLine = lines.FirstOrDefault(l => l.StartsWith("Host:", StringComparison.OrdinalIgnoreCase));
                    if (hostLine != null)
                    {
                        var hostVal = hostLine.Substring(5).Trim();
                        var hostParts = hostVal.Split(':');
                        targetHost = hostParts[0];
                        targetPort = hostParts.Length > 1 ? int.Parse(hostParts[1]) : 80;
                    }
                }
                else if (bytesRead >= 3 && buffer[0] == 0x05)
                {
                    // SOCKS5 Handshake inside bridge
                    protocol = "VIRTUAL-BRIDGE (SOCKS5)";
                    // Reply No Auth
                    await clientStream.WriteAsync(new byte[] { 0x05, 0x00 }, token);
                    byte[] req = new byte[256];
                    int rLen = await clientStream.ReadAsync(req.AsMemory(0, req.Length), token);
                    if (rLen > 4 && req[1] == 0x01)
                    {
                        if (req[3] == 0x01) // IPv4
                        {
                            targetHost = new IPAddress(new byte[] { req[4], req[5], req[6], req[7] }).ToString();
                            targetPort = (req[8] << 8) | req[9];
                        }
                        else if (req[3] == 0x03) // Domain
                        {
                            int dLen = req[4];
                            targetHost = Encoding.ASCII.GetString(req, 5, dLen);
                            targetPort = (req[5 + dLen] << 8) | req[6 + dLen];
                        }
                    }
                }

                if (string.IsNullOrEmpty(targetHost))
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                // Select target physical adapter using Smooth Weighted Round Robin with % weights
                var adapters = _getAdapters();
                chosenAdapter = _loadBalancer.GetNextAdapter(adapters);
                if (chosenAdapter == null || !IPAddress.TryParse(chosenAdapter.Address, out var localIp))
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    return;
                }

                // Connect outbound socket strictly bound to selected physical adapter's local IP
                using var targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Bind(new IPEndPoint(localIp, 0));

                using var connectCts = new CancellationTokenSource(5000);
                await targetSocket.ConnectAsync(targetHost, targetPort, connectCts.Token);

                if (protocol.Contains("HTTPS"))
                {
                    byte[] okReply = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                    await clientStream.WriteAsync(okReply, token);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else if (protocol.Contains("SOCKS5"))
                {
                    byte[] okReply = new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
                    await clientStream.WriteAsync(okReply, token);
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else
                {
                    // Plain HTTP forward
                    using var firstStream = new NetworkStream(targetSocket, ownsSocket: false);
                    await firstStream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                    RecordBytes(chosenAdapter, 0, bytesRead);
                    ArrayPool<byte>.Shared.Return(buffer);
                }

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
            Interlocked.Decrement(ref _activeStreams);
            _model.ActiveStreams = _activeStreams;

            if (chosenAdapter != null && !string.IsNullOrEmpty(targetHost))
            {
                var log = new AuditLogModel
                {
                    Protocol = protocol,
                    TargetHost = targetHost,
                    TargetPort = targetPort,
                    AdapterId = chosenAdapter.Id,
                    AdapterName = $"{chosenAdapter.Name} ({chosenAdapter.EffectivePercentage:F0}%)",
                    ClientIp = clientIp,
                    BytesTransferred = bytesSession,
                    DurationMs = sw.ElapsedMilliseconds,
                    Status = error == null ? "Completed" : "Failed",
                    ErrorMessage = error
                };

                OnAuditLog?.Invoke(log);
            }
        }
    }

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
        return Interlocked.Read(ref totalSessionBytes);
    }

    private void RecordBytes(NetworkAdapterModel adapter, long rx, long tx)
    {
        adapter.TotalRxBytes += rx;
        adapter.TotalTxBytes += tx;

        _model.TotalRxBytes += rx;
        _model.TotalTxBytes += tx;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
    }
}
