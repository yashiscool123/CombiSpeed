using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// High-concurrency multi-protocol proxy validator that measures real-world RTT latency,
/// verifies HTTP/SOCKS protocol compliance, evaluates anonymity tiers, prunes dead endpoints,
/// and persists verified healthy proxies to disk with automatic eviction on unavailability.
/// </summary>
public class ProxyValidatorEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, HarvestedProxyModel> _pool = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _saveLock = new();
    private Timer? _saveDebounceTimer;

    public string StorageFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CombiSpeed",
        "healthy_proxies.json");

    public string StateStorageFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CombiSpeed",
        "proxy_state.json");

    public event Action<string>? OnLog;
    public event Action<HarvestedProxyModel>? OnProxyStatusChanged;

    public int TotalCount => _pool.Count;
    public int HealthyCount => _pool.Values.Count(p => p.IsHealthy);

    public IEnumerable<HarvestedProxyModel> AllProxies => _pool.Values;

    /// <summary>
    /// Loads complete persisted proxy state (proxies, selections, filters, and sorting) from disk.
    /// Falls back to healthy_proxies.json if proxy_state.json does not exist.
    /// </summary>
    public PersistedProxyStateModel? LoadPersistedState(string? customStatePath = null)
    {
        lock (_saveLock)
        {
            try
            {
                string targetPath = customStatePath ?? StateStorageFilePath;
                if (File.Exists(targetPath))
                {
                    string json = File.ReadAllText(targetPath);
                    var loaded = JsonSerializer.Deserialize<PersistedProxyStateModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loaded != null)
                    {
                        if (loaded.Proxies != null && loaded.Proxies.Count > 0)
                        {
                            foreach (var proxy in loaded.Proxies)
                            {
                                IdempotentAddOrUpdate(proxy);
                            }
                        }
                        OnLog?.Invoke($"[Validator] Loaded {loaded.Proxies?.Count ?? 0} proxies and saved filters/sorting from persistent state ({Path.GetFileName(targetPath)}).");
                        return loaded;
                    }
                }

                // Fallback: If proxy_state.json doesn't exist yet, load healthy_proxies.json
                int loadedHealthy = LoadHealthyProxies();
                if (loadedHealthy > 0)
                {
                    return new PersistedProxyStateModel
                    {
                        Proxies = _pool.Values.ToList()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Validator] Error loading persisted proxy state: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Debounced save that commits complete proxy state (proxies, selections, filters, and sorting) to disk.
    /// </summary>
    public void RequestSavePersistedState(Func<PersistedProxyStateModel> stateFactory, string? customPath = null)
    {
        lock (_saveLock)
        {
            _saveDebounceTimer?.Dispose();
            _saveDebounceTimer = new Timer(_ =>
            {
                try
                {
                    var state = stateFactory();
                    SavePersistedState(state, customPath);
                }
                catch { }
            }, null, 400, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Saves all harvested proxies with their user modifications (IsSelected), along with
    /// active filters and sorting configurations, in Idempotence mode to disk.
    /// </summary>
    public int SavePersistedState(PersistedProxyStateModel state, string? customStatePath = null)
    {
        lock (_saveLock)
        {
            try
            {
                string targetPath = customStatePath ?? StateStorageFilePath;
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                state.Proxies = _pool.Values
                    .OrderByDescending(p => p.IsSelected)
                    .ThenByDescending(p => p.IsHealthy)
                    .ThenBy(p => p.LatencyMs)
                    .ToList();
                state.LastSaved = DateTime.UtcNow;

                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string tempPath = targetPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, targetPath, overwrite: true);

                // Synchronize healthy_proxies.json for backward compatibility
                SaveHealthyProxies();

                OnLog?.Invoke($"[Validator] Idempotently saved persistent proxy state ({state.Proxies.Count} proxies, filters, and sorting) to {Path.GetFileName(targetPath)}.");
                return state.Proxies.Count;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"[Validator] Error persisting proxy state: {ex.Message}");
                return 0;
            }
        }
    }

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

                // Filter ONLY verified good proxies: IsHealthy == true and Failures == 0
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
            OnProxyStatusChanged?.Invoke(proxy);
            RequestSaveHealthy();
            OnLog?.Invoke($"[Validator] Evicted unavailable proxy {endpoint} from active pool and persistent storage.");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Idempotently adds or updates a candidate proxy into the pool.
    /// In Idempotence Mode:
    /// - Preserves user modifications: IsSelected (never unchecks user selections)
    /// - Preserves verified benchmark metrics: LatencyMs, IsHealthy, Failures, Successes, LastChecked
    /// - Non-destructively enriches metadata: CountryCode/FlagEmoji if previously unknown, upgraded Protocol
    /// - Deduplicates by Endpoint (Host:Port) with zero duplicate insertions
    /// Guaranteed strictly idempotent: f(f(x)) == f(x).
    /// </summary>
    public bool IdempotentAddOrUpdate(HarvestedProxyModel candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.Host) || candidate.Port <= 0) return false;
        string key = candidate.Endpoint;

        bool isNew = false;
        _pool.AddOrUpdate(key,
            _ =>
            {
                isNew = true;
                return candidate;
            },
            (_, existing) =>
            {
                // IDEMPOTENCE MODE: strictly preserve user modifications and verified metrics!
                // 1. Keep existing.IsSelected (do not clobber user checkbox selection)
                // 2. Keep existing verification benchmarks if already tested
                if (existing.LastChecked != DateTime.MinValue && candidate.LastChecked == DateTime.MinValue)
                {
                    // Existing is verified; keep existing metrics
                }
                else if (candidate.LastChecked != DateTime.MinValue && existing.LastChecked == DateTime.MinValue)
                {
                    existing.IsHealthy = candidate.IsHealthy;
                    existing.LatencyMs = candidate.LatencyMs;
                    existing.Failures = candidate.Failures;
                    existing.Successes = candidate.Successes;
                    existing.LastChecked = candidate.LastChecked;
                    existing.Anonymity = candidate.Anonymity;
                }

                // 3. Non-destructively enrich CountryCode & FlagEmoji if previously unknown
                if ((string.IsNullOrEmpty(existing.CountryCode) || existing.CountryCode.Equals("UN", StringComparison.OrdinalIgnoreCase)) &&
                    !string.IsNullOrEmpty(candidate.CountryCode) && !candidate.CountryCode.Equals("UN", StringComparison.OrdinalIgnoreCase))
                {
                    existing.CountryCode = candidate.CountryCode;
                }

                // 4. Upgrade protocol if existing was generic HTTP and candidate has specific HTTPS/SOCKS5
                if (existing.Protocol == ProxyProtocol.Http && candidate.Protocol != ProxyProtocol.Http)
                {
                    existing.Protocol = candidate.Protocol;
                }

                // 5. Update source if missing
                if (string.IsNullOrEmpty(existing.SourceUrl) && !string.IsNullOrEmpty(candidate.SourceUrl))
                {
                    existing.SourceUrl = candidate.SourceUrl;
                }

                return existing;
            });

        return isNew;
    }

    /// <summary>
    /// Idempotently populates a batch of harvested proxies. Returns the count of newly discovered proxies.
    /// </summary>
    public int IdempotentAddCandidates(IEnumerable<HarvestedProxyModel> candidates)
    {
        int newCount = 0;
        foreach (var p in candidates)
        {
            if (IdempotentAddOrUpdate(p))
            {
                newCount++;
            }
        }
        return newCount;
    }

    public void AddOrUpdateCandidate(HarvestedProxyModel proxy)
    {
        IdempotentAddOrUpdate(proxy);
    }

    public void AddCandidates(IEnumerable<HarvestedProxyModel> proxies)
    {
        IdempotentAddCandidates(proxies);
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

        SaveHealthyProxies();
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
        bool wasHealthy = proxy.IsHealthy;

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
        string authHeader = "";
        if (!string.IsNullOrEmpty(proxy.Username))
        {
            string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
            authHeader = $"Proxy-Authorization: Basic {creds}\r\n";
        }
        string probe = $"GET http://1.1.1.1/ HTTP/1.1\r\nHost: 1.1.1.1\r\n{authHeader}User-Agent: CombiSpeed/2.1.1\r\nConnection: close\r\n\r\n";
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
    /// Probes a SOCKS5 proxy via RFC 1928 handshake (Version 5, No Auth 0x00 and RFC 1929 Auth 0x02).
    /// </summary>
    private static async Task<bool> ProbeSocks5Async(HarvestedProxyModel proxy, CancellationToken ct)
    {
        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        await socket.ConnectAsync(proxy.Host, proxy.Port, ct);

        // Handshake: [Version: 5, Methods: 1 or 2]
        byte[] greeting = string.IsNullOrEmpty(proxy.Username)
            ? new byte[] { 0x05, 0x01, 0x00 }
            : new byte[] { 0x05, 0x02, 0x00, 0x02 };

        await socket.SendAsync(greeting, SocketFlags.None, ct);

        byte[] response = new byte[2];
        int bytesRead = await socket.ReceiveAsync(response, SocketFlags.None, ct);
        if (bytesRead == 2 && response[0] == 0x05)
        {
            if (response[1] == 0x00) // No Auth Granted
            {
                proxy.Anonymity = ProxyAnonymity.Elite;
                return true;
            }
            if (response[1] == 0x02 && !string.IsNullOrEmpty(proxy.Username)) // RFC 1929 Auth requested
            {
                byte[] uBytes = Encoding.ASCII.GetBytes(proxy.Username);
                byte[] pBytes = Encoding.ASCII.GetBytes(proxy.Password ?? "");
                byte[] auth = new byte[1 + 1 + uBytes.Length + 1 + pBytes.Length];
                auth[0] = 0x01; // RFC 1929 Version
                auth[1] = (byte)uBytes.Length;
                Array.Copy(uBytes, 0, auth, 2, uBytes.Length);
                auth[2 + uBytes.Length] = (byte)pBytes.Length;
                Array.Copy(pBytes, 0, auth, 3 + uBytes.Length, pBytes.Length);

                await socket.SendAsync(auth, SocketFlags.None, ct);
                byte[] authResp = new byte[2];
                int aRead = await socket.ReceiveAsync(authResp, SocketFlags.None, ct);
                if (aRead == 2 && authResp[1] == 0x00)
                {
                    proxy.Anonymity = ProxyAnonymity.Elite;
                    return true;
                }
            }
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
    /// Prunes proxies that have accumulated failures beyond the threshold or are unhealthy.
    /// </summary>
    public int PruneDead(int maxFailures = 2)
    {
        var deadKeys = _pool.Where(kvp => kvp.Value.Failures >= maxFailures || !kvp.Value.IsHealthy).Select(kvp => kvp.Key).ToList();
        int count = 0;
        foreach (var key in deadKeys)
        {
            if (_pool.TryRemove(key, out _))
            {
                count++;
            }
        }
        SaveHealthyProxies();
        OnLog?.Invoke($"[Validator] Pruned {count} dead proxies from pool and updated persistent storage.");
        return count;
    }

    /// <summary>
    /// Retrieves a list of active healthy proxies filtered by user preferences and country filter.
    /// </summary>
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
        OnLog?.Invoke("[Validator] Cleared all proxies from pool and persistent storage.");
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

