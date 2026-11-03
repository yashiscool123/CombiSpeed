using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacProxyValidator
{
    private readonly ConcurrentDictionary<string, HarvestedProxyModel> _pool = new(StringComparer.OrdinalIgnoreCase);

    public event Action<string>? OnLog;

    public int TotalCount => _pool.Count;
    public int HealthyCount => _pool.Values.Count(p => p.IsHealthy);

    public IEnumerable<HarvestedProxyModel> AllProxies => _pool.Values;

    public void AddCandidates(IEnumerable<HarvestedProxyModel> proxies)
    {
        foreach (var p in proxies)
        {
            _pool.AddOrUpdate(p.Endpoint, p, (_, existing) =>
            {
                existing.SourceUrl = p.SourceUrl;
                existing.Protocol = p.Protocol;
                return existing;
            });
        }
    }

    public async Task<int> ValidateAllAsync(
        IEnumerable<HarvestedProxyModel>? targetSubset = null,
        int concurrencyLimit = 60,
        int timeoutMs = 2500,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var list = targetSubset?.ToList() ?? _pool.Values.ToList();
        if (list.Count == 0) return 0;

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
        OnLog?.Invoke($"[Validator] Verified {healthy} healthy proxies of {list.Count}.");
        return healthy;
    }

    public async Task<bool> ValidateSingleAsync(HarvestedProxyModel proxy, int timeoutMs = 2500, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        bool success = false;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            await socket.ConnectAsync(proxy.Host, proxy.Port, cts.Token);

            if (proxy.Protocol == ProxyProtocol.Socks5)
            {
                byte[] greeting = string.IsNullOrEmpty(proxy.Username)
                    ? new byte[] { 0x05, 0x01, 0x00 }
                    : new byte[] { 0x05, 0x02, 0x00, 0x02 };
                await socket.SendAsync(greeting, SocketFlags.None, cts.Token);
                byte[] response = new byte[2];
                int bytesRead = await socket.ReceiveAsync(response, SocketFlags.None, cts.Token);
                if (bytesRead == 2 && response[0] == 0x05)
                {
                    if (response[1] == 0x00)
                    {
                        success = true;
                        proxy.Anonymity = ProxyAnonymity.Elite;
                    }
                    else if (response[1] == 0x02 && !string.IsNullOrEmpty(proxy.Username))
                    {
                        byte[] uBytes = Encoding.ASCII.GetBytes(proxy.Username);
                        byte[] pBytes = Encoding.ASCII.GetBytes(proxy.Password ?? "");
                        byte[] auth = new byte[1 + 1 + uBytes.Length + 1 + pBytes.Length];
                        auth[0] = 0x01;
                        auth[1] = (byte)uBytes.Length;
                        Array.Copy(uBytes, 0, auth, 2, uBytes.Length);
                        auth[2 + uBytes.Length] = (byte)pBytes.Length;
                        Array.Copy(pBytes, 0, auth, 3 + uBytes.Length, pBytes.Length);
                        await socket.SendAsync(auth, SocketFlags.None, cts.Token);
                        byte[] authResp = new byte[2];
                        int aRead = await socket.ReceiveAsync(authResp, SocketFlags.None, cts.Token);
                        if (aRead == 2 && authResp[1] == 0x00)
                        {
                            success = true;
                            proxy.Anonymity = ProxyAnonymity.Elite;
                        }
                    }
                }
            }
            else
            {
                string authHeader = "";
                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                    authHeader = $"Proxy-Authorization: Basic {creds}\r\n";
                }
                string probe = $"GET http://1.1.1.1/ HTTP/1.1\r\nHost: 1.1.1.1\r\n{authHeader}Connection: close\r\n\r\n";
                await socket.SendAsync(Encoding.ASCII.GetBytes(probe), SocketFlags.None, cts.Token);
                byte[] buf = new byte[512];
                int bytesRead = await socket.ReceiveAsync(buf, SocketFlags.None, cts.Token);
                if (bytesRead > 0)
                {
                    string resp = Encoding.ASCII.GetString(buf, 0, bytesRead);
                    success = resp.StartsWith("HTTP/1.0 200") || resp.StartsWith("HTTP/1.1 200") ||
                              resp.StartsWith("HTTP/1.0 301") || resp.StartsWith("HTTP/1.1 301") ||
                              resp.StartsWith("HTTP/1.0 302") || resp.StartsWith("HTTP/1.1 302");
                    if (success)
                    {
                        proxy.Anonymity = resp.Contains("Via:", StringComparison.OrdinalIgnoreCase) ||
                                          resp.Contains("X-Forwarded-For:", StringComparison.OrdinalIgnoreCase)
                            ? ProxyAnonymity.Transparent
                            : ProxyAnonymity.Elite;
                    }
                }
            }

            sw.Stop();
            if (success)
            {
                proxy.LatencyMs = (int)sw.ElapsedMilliseconds;
                proxy.IsHealthy = true;
                proxy.Successes++;
                proxy.Failures = 0;
                proxy.LastChecked = DateTime.Now;
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

        return proxy.IsHealthy;
    }

    public int PruneDead(int maxFailures = 2)
    {
        var deadKeys = _pool.Where(kvp => kvp.Value.Failures >= maxFailures).Select(kvp => kvp.Key).ToList();
        int count = 0;
        foreach (var key in deadKeys)
        {
            if (_pool.TryRemove(key, out _)) count++;
        }
        return count;
    }

    public List<HarvestedProxyModel> GetHealthyProxies(
        bool strictAnonymity = false,
        RotationStrategy strategy = RotationStrategy.RoundRobin,
        string? countryFilter = null,
        int? maxLatencyMs = null,
        string? sourceFilter = null,
        ProxyProtocol? protocolFilter = null)
    {
        var query = _pool.Values.Where(p => p.IsHealthy);
        if (strictAnonymity || strategy == RotationStrategy.EliteOnly)
        {
            query = query.Where(p => p.Anonymity == ProxyAnonymity.Elite);
        }
        if (!string.IsNullOrWhiteSpace(countryFilter) && !countryFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.CountryCode.Equals(countryFilter, StringComparison.OrdinalIgnoreCase));
        }
        if (maxLatencyMs.HasValue && maxLatencyMs.Value > 0)
        {
            query = query.Where(p => p.LatencyMs <= maxLatencyMs.Value);
        }
        if (!string.IsNullOrWhiteSpace(sourceFilter) && !sourceFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.SourceUrl.Contains(sourceFilter, StringComparison.OrdinalIgnoreCase));
        }
        if (protocolFilter.HasValue)
        {
            query = query.Where(p => p.Protocol == protocolFilter.Value);
        }
        if (strategy == RotationStrategy.LowestLatency)
        {
            return query.OrderBy(p => p.LatencyMs).ToList();
        }
        return query.ToList();
    }

    public void Clear() => _pool.Clear();
}

