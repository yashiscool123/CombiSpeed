using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxProxyValidator : IDisposable
{
    private readonly ConcurrentDictionary<string, HarvestedProxyModel> _pool = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _saveLock = new();
    private Timer? _saveDebounceTimer;

    public string StorageFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "combispeed",
        "healthy_proxies.json");

    public event Action<string>? OnLog;

    public int TotalCount => _pool.Count;
    public int HealthyCount => _pool.Values.Count(p => p.IsHealthy);

    public IEnumerable<HarvestedProxyModel> AllProxies => _pool.Values;

    /// <summary>
    /// Loads persisted healthy proxies from disk storage into the active pool.
    /// </summary>
    public int LoadHealthyProxies(string? customPath = null)
    {
        lock (_saveLock)
        {
            try
            {
                string targetPath = customPath ?? StorageFilePath;
                if (!File.Exists(targetPath)) return 0;

                string json = File.ReadAllText(targetPath);
                var loaded = JsonSerializer.Deserialize<List<HarvestedProxyModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loaded == null || loaded.Count == 0) return 0;

                int loadedCount = 0;
                foreach (var proxy in loaded)
                {
                    if (proxy.IsHealthy && proxy.Failures == 0)
                    {
                        _pool[proxy.Endpoint] = proxy;
                        loadedCount++;
                    }
                }

                OnLog?.Invoke($"[Validator] Loaded {loadedCount} healthy proxies from persistent storage ({Path.GetFileName(targetPath)}).");
                return loadedCount;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Validator] Error loading healthy proxies: {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// Debounced save that commits verified healthy proxies to disk after 400ms of inactivity.
    /// </summary>
    public void RequestSaveHealthy(string? customPath = null)
    {
        lock (_saveLock)
        {
            _saveDebounceTimer?.Dispose();
            _saveDebounceTimer = new Timer(_ =>
            {
                SaveHealthyProxies(customPath);
            }, null, 400, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Saves only verified healthy proxies (IsHealthy == true and Failures == 0) to persistent disk storage.
    /// </summary>
    public int SaveHealthyProxies(string? customPath = null)
    {
        lock (_saveLock)
        {
            try
            {
                string targetPath = customPath ?? StorageFilePath;
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var goodProxies = _pool.Values
                    .Where(p => p.IsHealthy && p.Failures == 0)
                    .OrderBy(p => p.LatencyMs)
                    .ToList();

                string json = JsonSerializer.Serialize(goodProxies, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string tempPath = targetPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, targetPath, overwrite: true);

                OnLog?.Invoke($"[Validator] Persisted {goodProxies.Count} healthy proxies to disk ({Path.GetFileName(targetPath)}).");
                return goodProxies.Count;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Validator] Error persisting healthy proxies: {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// Immediately marks a proxy as unavailable, removes it from memory pool, and evicts it from persistent storage.
    /// </summary>
    public bool MarkUnavailable(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return false;

        if (_pool.TryGetValue(endpoint, out var proxy))
        {
            proxy.IsHealthy = false;
            proxy.Failures++;
            _pool.TryRemove(endpoint, out _);
            RequestSaveHealthy();
            OnLog?.Invoke($"[Validator] Evicted unavailable proxy {endpoint} from active pool and persistent storage.");
            return true;
        }

        return false;
    }

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
        SaveHealthyProxies();
        OnLog?.Invoke($"[Validator] Verified {healthy} healthy proxies of {list.Count}.");
        return healthy;
    }

    public async Task<bool> ValidateSingleAsync(HarvestedProxyModel proxy, int timeoutMs = 2500, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        bool success = false;
        bool wasHealthy = proxy.IsHealthy;

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
                string probe = $"GET http://1.1.1.1/ HTTP/1.1\r\nHost: 1.1.1.1\r\n{authHeader}User-Agent: CombiSpeed/2.1.1\r\nConnection: close\r\n\r\n";
                await socket.SendAsync(Encoding.ASCII.GetBytes(probe), SocketFlags.None, cts.Token);

                byte[] buffer = new byte[512];
                int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
                if (bytesRead > 0)
                {
                    string resp = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (resp.StartsWith("HTTP/1.0 200") || resp.StartsWith("HTTP/1.1 200") || resp.Contains(" 200 OK") ||
                        resp.StartsWith("HTTP/1.1 301") || resp.StartsWith("HTTP/1.1 302"))
                    {
                        success = true;
                        proxy.Anonymity = resp.Contains("Via:") || resp.Contains("X-Forwarded-For:")
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
                RequestSaveHealthy();
            }
            else
            {
                proxy.IsHealthy = false;
                proxy.Failures++;
                proxy.LatencyMs = 999;
                proxy.LastChecked = DateTime.Now;

                if (wasHealthy)
                {
                    _pool.TryRemove(proxy.Endpoint, out _);
                    RequestSaveHealthy();
                }
            }
        }
        catch
        {
            proxy.IsHealthy = false;
            proxy.Failures++;
            proxy.LatencyMs = 999;
            proxy.LastChecked = DateTime.Now;

            if (wasHealthy)
            {
                _pool.TryRemove(proxy.Endpoint, out _);
                RequestSaveHealthy();
            }
        }

        return proxy.IsHealthy;
    }

    public int PruneDead(int maxFailures = 2)
    {
        var deadKeys = _pool.Where(kvp => kvp.Value.Failures >= maxFailures || !kvp.Value.IsHealthy).Select(kvp => kvp.Key).ToList();
        int count = 0;
        foreach (var key in deadKeys)
        {
            if (_pool.TryRemove(key, out _)) count++;
        }
        SaveHealthyProxies();
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

    public void Clear()
    {
        _pool.Clear();
        SaveHealthyProxies();
    }

    public void Dispose()
    {
        lock (_saveLock)
        {
            _saveDebounceTimer?.Dispose();
            _saveDebounceTimer = null;
        }
    }
}
