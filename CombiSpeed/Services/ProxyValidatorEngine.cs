using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// High-concurrency multi-protocol proxy validator that measures real-world RTT latency,
/// verifies HTTP/SOCKS protocol compliance, evaluates anonymity tiers, and prunes dead endpoints.
/// </summary>
public class ProxyValidatorEngine
{
    private readonly ConcurrentDictionary<string, HarvestedProxyModel> _pool = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? OnLog;
    public event Action<HarvestedProxyModel>? OnProxyStatusChanged;

    public int TotalCount => _pool.Count;
    public int HealthyCount => _pool.Values.Count(p => p.IsHealthy);

    public IEnumerable<HarvestedProxyModel> AllProxies => _pool.Values;

    public void AddOrUpdateCandidate(HarvestedProxyModel proxy)
    {
        _pool.AddOrUpdate(proxy.Endpoint, proxy, (_, existing) =>
        {
            existing.SourceUrl = proxy.SourceUrl;
            existing.Protocol = proxy.Protocol;
            return existing;
        });
    }

    public void AddCandidates(IEnumerable<HarvestedProxyModel> proxies)
    {
        foreach (var p in proxies)
        {
            AddOrUpdateCandidate(p);
        }
    }

    /// <summary>
    /// Validates all proxies currently in the pool or a provided candidate subset in parallel.
    /// </summary>
    public async Task<int> ValidateAllAsync(
        IEnumerable<HarvestedProxyModel>? targetSubset = null,
        int concurrencyLimit = 60,
        int timeoutMs = 2500,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var list = targetSubset?.ToList() ?? _pool.Values.ToList();
        if (list.Count == 0) return 0;

        OnLog?.Invoke($"[Validator] Validating {list.Count} proxies (concurrency: {concurrencyLimit}, timeout: {timeoutMs}ms)...");

        using var semaphore = new SemaphoreSlim(concurrencyLimit, concurrencyLimit);
        int completed = 0;
        int healthy = 0;

        var tasks = list.Select(async proxy =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                if (ct.IsCancellationRequested) return;

                bool ok = await ValidateSingleAsync(proxy, timeoutMs, ct);
                if (ok) Interlocked.Increment(ref healthy);

                int done = Interlocked.Increment(ref completed);
                progress?.Report(done * 100 / list.Count);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        OnLog?.Invoke($"[Validator] Validation finished. {healthy} of {list.Count} proxies verified healthy & responsive.");
        return healthy;
    }

    /// <summary>
    /// Validates an individual proxy against its protocol standard.
    /// </summary>
    public async Task<bool> ValidateSingleAsync(HarvestedProxyModel proxy, int timeoutMs = 2500, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        bool success = false;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            switch (proxy.Protocol)
            {
                case ProxyProtocol.Socks5:
                    success = await ProbeSocks5Async(proxy, cts.Token);
                    break;

                case ProxyProtocol.Socks4:
                    success = await ProbeSocks4Async(proxy, cts.Token);
                    break;

                case ProxyProtocol.Http:
                case ProxyProtocol.Https:
                default:
                    success = await ProbeHttpAsync(proxy, cts.Token);
                    break;
            }

            sw.Stop();

            if (success)
            {
                proxy.LatencyMs = (int)sw.ElapsedMilliseconds;
                proxy.IsHealthy = true;
                proxy.Successes++;
                proxy.Failures = 0;
                proxy.LastChecked = DateTime.Now;

                // Basic anonymity classification
                if (proxy.Anonymity == ProxyAnonymity.Transparent && proxy.LatencyMs < 400)
                {
                    proxy.Anonymity = ProxyAnonymity.Anonymous;
                }
            }
            else
            {
                proxy.IsHealthy = false;
                proxy.Failures++;
                proxy.LatencyMs = 999;
                proxy.LastChecked = DateTime.Now;
            }
        }
        catch
        {
            proxy.IsHealthy = false;
            proxy.Failures++;
            proxy.LatencyMs = 999;
            proxy.LastChecked = DateTime.Now;
        }

        OnProxyStatusChanged?.Invoke(proxy);
        return proxy.IsHealthy;
    }

    /// <summary>
    /// Probes an HTTP/HTTPS proxy using HTTP CONNECT or GET request.
    /// </summary>
    private static async Task<bool> ProbeHttpAsync(HarvestedProxyModel proxy, CancellationToken ct)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            SendTimeout = 2000,
            ReceiveTimeout = 2000
        };

        await socket.ConnectAsync(proxy.Host, proxy.Port, ct);

        // Send a lightweight HTTP GET or CONNECT request to probe liveness
        string probe = $"GET http://1.1.1.1/ HTTP/1.1\r\nHost: 1.1.1.1\r\nUser-Agent: CombiSpeed/2.1.0\r\nConnection: close\r\n\r\n";
        byte[] reqBytes = Encoding.ASCII.GetBytes(probe);
        await socket.SendAsync(reqBytes, SocketFlags.None, ct);

        byte[] buffer = new byte[512];
        int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None, ct);
        if (bytesRead <= 0) return false;

        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        if (response.StartsWith("HTTP/1.0 200") || response.StartsWith("HTTP/1.1 200") ||
            response.StartsWith("HTTP/1.1 301") || response.StartsWith("HTTP/1.1 302") ||
            response.StartsWith("HTTP/1.0 301") || response.StartsWith("HTTP/1.0 302"))
        {
            // Evaluate Anonymity headers
            if (response.Contains("Via:", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("X-Forwarded-For:", StringComparison.OrdinalIgnoreCase))
            {
                proxy.Anonymity = ProxyAnonymity.Transparent;
            }
            else
            {
                proxy.Anonymity = ProxyAnonymity.Elite;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Probes a SOCKS5 proxy via RFC 1928 handshake (Version 5, 1 Auth Method: No Auth 0x00).
    /// </summary>
    private static async Task<bool> ProbeSocks5Async(HarvestedProxyModel proxy, CancellationToken ct)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        await socket.ConnectAsync(proxy.Host, proxy.Port, ct);

        // Handshake: [Version: 5, Methods: 1, Method: 0 (No Auth)]
        byte[] greeting = { 0x05, 0x01, 0x00 };
        await socket.SendAsync(greeting, SocketFlags.None, ct);

        byte[] response = new byte[2];
        int bytesRead = await socket.ReceiveAsync(response, SocketFlags.None, ct);
        if (bytesRead == 2 && response[0] == 0x05 && response[1] == 0x00)
        {
            proxy.Anonymity = ProxyAnonymity.Elite;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Probes a SOCKS4 proxy.
    /// </summary>
    private static async Task<bool> ProbeSocks4Async(HarvestedProxyModel proxy, CancellationToken ct)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        await socket.ConnectAsync(proxy.Host, proxy.Port, ct);

        // SOCKS4 connect request to 1.1.1.1:80
        byte[] req = { 0x04, 0x01, 0x00, 0x50, 0x01, 0x01, 0x01, 0x01, 0x00 };
        await socket.SendAsync(req, SocketFlags.None, ct);

        byte[] response = new byte[8];
        int bytesRead = await socket.ReceiveAsync(response, SocketFlags.None, ct);
        if (bytesRead >= 2 && response[0] == 0x00 && response[1] == 0x5A) // 0x5A = request granted
        {
            proxy.Anonymity = ProxyAnonymity.Anonymous;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Prunes proxies that have accumulated failures beyond the threshold.
    /// </summary>
    public int PruneDead(int maxFailures = 2)
    {
        var deadKeys = _pool.Where(kvp => kvp.Value.Failures >= maxFailures).Select(kvp => kvp.Key).ToList();
        int count = 0;
        foreach (var key in deadKeys)
        {
            if (_pool.TryRemove(key, out _))
            {
                count++;
            }
        }
        OnLog?.Invoke($"[Validator] Pruned {count} dead proxies from pool.");
        return count;
    }

    /// <summary>
    /// Retrieves a list of active healthy proxies filtered by user preferences.
    /// </summary>
    public List<HarvestedProxyModel> GetHealthyProxies(bool strictAnonymity = false, RotationStrategy strategy = RotationStrategy.RoundRobin)
    {
        var query = _pool.Values.Where(p => p.IsHealthy);

        if (strictAnonymity || strategy == RotationStrategy.EliteOnly)
        {
            query = query.Where(p => p.Anonymity == ProxyAnonymity.Elite);
        }

        if (strategy == RotationStrategy.LowestLatency)
        {
            return query.OrderBy(p => p.LatencyMs).ToList();
        }

        return query.ToList();
    }

    public void Clear()
    {
        _pool.Clear();
        OnLog?.Invoke("[Validator] Cleared all proxies from pool.");
    }
}
