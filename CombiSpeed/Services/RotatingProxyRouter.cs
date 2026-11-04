using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// Dispatcher and routing engine that selects upstream proxies according to configurable policies
/// (TimeBasedInterval, Round Robin, Sticky Domain, Lowest Latency, Elite Only) and binds outbound sockets across
/// local physical network adapters for double-layer multi-WAN acceleration.
/// </summary>
public class RotatingProxyRouter
{
    private readonly ProxyValidatorEngine _validator;
    private readonly RotatingProxyConfigModel _config;
    private readonly LoadBalancerEngine _loadBalancer;

    private int _roundRobinIndex;
    private readonly ConcurrentDictionary<string, (HarvestedProxyModel Proxy, DateTime ExpiresAt)> _stickyCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _timeRotationLock = new();
    private HarvestedProxyModel? _activeTimedProxy;
    private DateTime _lastTimedRotation = DateTime.MinValue;
    private bool _isLiveConnected = false;
    private bool _isAutoRotating = false;

    public HarvestedProxyModel? ActiveTimedProxy => _activeTimedProxy;
    public bool IsLiveConnected => _isLiveConnected;
    public event Action<HarvestedProxyModel?>? OnTimedRotation;

    public int SecondsUntilNextRotation
    {
        get
        {
            if (_config.Strategy != RotationStrategy.TimeBasedInterval) return 0;
            if (!_isLiveConnected || _activeTimedProxy == null) return -1;
            int intervalSec = Math.Clamp(_config.RotationIntervalSeconds, 5, 7200);
            var elapsed = (DateTime.UtcNow - _lastTimedRotation).TotalSeconds;
            return Math.Max(0, (int)(intervalSec - elapsed));
        }
    }

    public long TotalRotatedRequests;
    public long SuccessfulRotations;
    public long FailedRotations;

    public event Action<string>? OnLog;

    public RotatingProxyRouter(ProxyValidatorEngine validator, RotatingProxyConfigModel config, LoadBalancerEngine loadBalancer)
    {
        _validator = validator;
        _config = config;
        _loadBalancer = loadBalancer;
    }

    /// <summary>
    /// Confirms that a proxy is actively connected and tested with live internet, starting the rotation countdown timer.
    /// </summary>
    public void ConfirmLiveConnected(HarvestedProxyModel proxy)
    {
        lock (_timeRotationLock)
        {
            _activeTimedProxy = proxy;
            _lastTimedRotation = DateTime.UtcNow;
            _isLiveConnected = true;
            proxy.IsHealthy = true;
            proxy.Failures = 0;
            proxy.Successes++;
        }
        OnTimedRotation?.Invoke(proxy);
        OnLog?.Invoke($"[Router] Live proxy connection confirmed with verified internet: {proxy.Endpoint} ({proxy.CountryCode} {proxy.FlagEmoji}). Rotation countdown started ({Math.Clamp(_config.RotationIntervalSeconds, 5, 7200)}s).");
    }

    /// <summary>
    /// Forces an immediate rotation to the next available selected proxy.
    /// </summary>
    public HarvestedProxyModel? ForceRotate()
    {
        var healthy = GetCandidatePool();
        if (healthy.Count == 0) return null;

        var chosen = PickRoundRobin(healthy);
        if (chosen != null)
        {
            ConfirmLiveConnected(chosen);
        }
        return _activeTimedProxy;
    }

    /// <summary>
    /// Evaluates if the rotation interval has expired for the active live proxy,
    /// and auto-advances to the next live proxy if expired.
    /// Waits for the configured interval once connected and tested successfully for internet.
    /// </summary>
    public async Task CheckAutoRotationAsync(CancellationToken ct = default)
    {
        if (_config.Strategy != RotationStrategy.TimeBasedInterval) return;
        if (!_isLiveConnected || _activeTimedProxy == null || _isAutoRotating) return;

        if (SecondsUntilNextRotation == 0)
        {
            _isAutoRotating = true;
            try
            {
                OnLog?.Invoke($"[Router] Timed rotation interval expired ({_config.RotationIntervalSeconds}s). Auto-rotating to next proxy server and testing internet (10s threshold)...");
                lock (_timeRotationLock)
                {
                    _isLiveConnected = false;
                }
                var nextLive = await ForceConnectToLiveProxyAsync(null, ct);
                if (nextLive == null)
                {
                    OnLog?.Invoke("[Router] No proxy candidates with accessible internet found. Countdown paused.");
                }
            }
            finally
            {
                _isAutoRotating = false;
            }
        }
    }

    /// <summary>
    /// Proactively probes a proxy endpoint to verify live TCP/protocol connectivity within a short timeout.
    /// </summary>
    public static async Task<bool> ProbeLiveConnectionAsync(HarvestedProxyModel proxy, int timeoutMs = 2500, CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(proxy.Host, proxy.Port);
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMs, ct)).ConfigureAwait(false);
            if (completedTask == connectTask && client.Connected)
            {
                return true;
            }
            try { client.Close(); } catch { }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies whether the proxy server URL provides active, functioning internet access
    /// within the specified threshold (default 10,000 ms / 10 seconds).
    /// Tests reachability to high-reliability internet test endpoints through the proxy.
    /// Supports HTTP, HTTPS, SOCKS4, and SOCKS5.
    /// </summary>
    public static async Task<bool> TestInternetAccessibilityAsync(
        HarvestedProxyModel proxy, 
        int thresholdMs = 10000, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(proxy.Host) || proxy.Port <= 0) return false;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(thresholdMs);
        var token = cts.Token;

        try
        {
            // Primary Method: Native SocketsHttpHandler via WebProxy (HTTP, HTTPS, SOCKS5)
            if (proxy.Protocol != ProxyProtocol.Socks4)
            {
                try
                {
                    var handler = new SocketsHttpHandler
                    {
                        ConnectTimeout = TimeSpan.FromMilliseconds(Math.Min(4000, thresholdMs)),
                        PooledConnectionLifetime = TimeSpan.FromSeconds(15)
                    };

                    string scheme = (proxy.Protocol == ProxyProtocol.Socks5) ? "socks5" : "http";
                    handler.Proxy = new WebProxy($"{scheme}://{proxy.Host}:{proxy.Port}");
                    if (!string.IsNullOrEmpty(proxy.Username))
                    {
                        handler.Proxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password ?? "");
                    }

                    using var client = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromMilliseconds(thresholdMs)
                    };

                    using var response = await client.GetAsync("http://1.1.1.1/", HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                    if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 400)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Fall through to socket-level tunnel verification
                }
            }

            // Secondary / Socket-level Method: Protocol tunnel to 1.1.1.1:80
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                SendTimeout = 4000,
                ReceiveTimeout = 4000
            };

            await socket.ConnectAsync(proxy.Host, proxy.Port, token).ConfigureAwait(false);
            using var stream = new NetworkStream(socket, ownsSocket: false);

            if (proxy.Protocol == ProxyProtocol.Socks5)
            {
                // RFC 1928 SOCKS5 Handshake
                bool hasAuth = !string.IsNullOrEmpty(proxy.Username);
                byte[] greeting = hasAuth ? new byte[] { 0x05, 0x02, 0x00, 0x02 } : new byte[] { 0x05, 0x01, 0x00 };
                await stream.WriteAsync(greeting, token).ConfigureAwait(false);

                byte[] reply = new byte[2];
                int r = await stream.ReadAsync(reply.AsMemory(0, 2), token).ConfigureAwait(false);
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
                    await stream.WriteAsync(authMsg, token).ConfigureAwait(false);

                    byte[] authReply = new byte[2];
                    r = await stream.ReadAsync(authReply.AsMemory(0, 2), token).ConfigureAwait(false);
                    if (r < 2 || authReply[1] != 0x00) return false;
                }
                else if (reply[1] != 0x00)
                {
                    return false;
                }

                // SOCKS5 Connect to 1.1.1.1:80
                byte[] req = new byte[] { 0x05, 0x01, 0x00, 0x01, 1, 1, 1, 1, 0, 80 };
                await stream.WriteAsync(req, token).ConfigureAwait(false);
                byte[] resp = new byte[512];
                r = await stream.ReadAsync(resp.AsMemory(0, resp.Length), token).ConfigureAwait(false);
                if (r < 4 || resp[0] != 0x05 || resp[1] != 0x00) return false;
            }
            else if (proxy.Protocol == ProxyProtocol.Socks4)
            {
                // RFC SOCKS4 Handshake to 1.1.1.1:80
                byte[] userBytes = Encoding.ASCII.GetBytes(proxy.Username ?? "combispeed");
                byte[] req = new byte[8 + userBytes.Length + 1];
                req[0] = 0x04;
                req[1] = 0x01;
                req[2] = 0;
                req[3] = 80;
                req[4] = 1;
                req[5] = 1;
                req[6] = 1;
                req[7] = 1;
                Array.Copy(userBytes, 0, req, 8, userBytes.Length);
                req[req.Length - 1] = 0x00;

                await stream.WriteAsync(req, token).ConfigureAwait(false);
                byte[] resp = new byte[8];
                int r = await stream.ReadAsync(resp.AsMemory(0, 8), token).ConfigureAwait(false);
                if (r < 8 || resp[0] != 0x00 || resp[1] != 0x5A) return false;
            }
            else
            {
                // HTTP Forward GET to 1.1.1.1
                string authHeader = "";
                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{proxy.Username}:{proxy.Password ?? ""}"));
                    authHeader = $"Proxy-Authorization: Basic {creds}\r\n";
                }
                string getReq = $"GET http://1.1.1.1/ HTTP/1.1\r\nHost: 1.1.1.1\r\n{authHeader}Connection: close\r\n\r\n";
                byte[] reqBytes = Encoding.ASCII.GetBytes(getReq);
                await stream.WriteAsync(reqBytes, token).ConfigureAwait(false);

                byte[] buf = new byte[512];
                int r = await stream.ReadAsync(buf.AsMemory(0, buf.Length), token).ConfigureAwait(false);
                if (r > 0)
                {
                    string respStr = Encoding.ASCII.GetString(buf, 0, r);
                    if (respStr.StartsWith("HTTP/1.0 200") || respStr.StartsWith("HTTP/1.1 200") ||
                        respStr.StartsWith("HTTP/1.0 301") || respStr.StartsWith("HTTP/1.1 301") ||
                        respStr.StartsWith("HTTP/1.0 302") || respStr.StartsWith("HTTP/1.1 302"))
                    {
                        return true;
                    }
                }
                return false;
            }

            // For SOCKS tunnels, send a lightweight HTTP HEAD / to verify actual internet data transit
            byte[] testHttp = Encoding.ASCII.GetBytes("HEAD / HTTP/1.1\r\nHost: 1.1.1.1\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(testHttp, token).ConfigureAwait(false);
            byte[] httpResp = new byte[256];
            int httpRead = await stream.ReadAsync(httpResp.AsMemory(0, httpResp.Length), token).ConfigureAwait(false);
            if (httpRead > 0)
            {
                string httpHeader = Encoding.ASCII.GetString(httpResp, 0, httpRead);
                return httpHeader.StartsWith("HTTP/1.");
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Step 1: Connects to the proxy server URL.
    /// Step 2: Once connected successfully, tests the proxy server URL to check if internet is accessible with a threshold of 10 seconds.
    /// Returns true only if the proxy server URL connects AND has verified internet access.
    /// </summary>
    public static async Task<bool> ConnectAndTestInternetAsync(
        HarvestedProxyModel proxy, 
        int internetThresholdMs = 10000, 
        Action<string>? log = null, 
        CancellationToken ct = default)
    {
        // 1. Initial proxy server connection probe
        log?.Invoke($"[Router] Connecting to proxy server URL {proxy.Endpoint} ({proxy.CountryCode})...");
        bool serverConnected = await ProbeLiveConnectionAsync(proxy, 3000, ct).ConfigureAwait(false);
        if (!serverConnected)
        {
            log?.Invoke($"[Router] Cannot connect to proxy server URL {proxy.Endpoint}: host unreachable or connection refused.");
            return false;
        }

        // 2. Once connected successfully, test internet accessibility with 10s threshold
        log?.Invoke($"[Router] Proxy server URL {proxy.Endpoint} connected successfully. Testing internet accessibility (threshold: 10s)...");
        var sw = Stopwatch.StartNew();
        bool internetAccessible = await TestInternetAccessibilityAsync(proxy, internetThresholdMs, ct).ConfigureAwait(false);
        sw.Stop();

        if (internetAccessible)
        {
            proxy.LatencyMs = (int)sw.ElapsedMilliseconds;
            log?.Invoke($"[Router] Internet access confirmed via {proxy.Endpoint} in {sw.ElapsedMilliseconds}ms (threshold: {internetThresholdMs}ms).");
            return true;
        }
        else
        {
            log?.Invoke($"[Router] Proxy server {proxy.Endpoint} connected, but internet is NOT accessible within 10s threshold ({sw.ElapsedMilliseconds}ms).");
            return false;
        }
    }

    /// <summary>
    /// Forces a connection to a specific target proxy (if provided) or rotates through candidates
    /// until a proxy server is connected AND tested successfully for internet access (10s threshold).
    /// If internet is not accessible, only then does it rotate to the next available proxy server.
    /// Once connected and tested successfully, the rotation waits for the configured interval time.
    /// </summary>
    public async Task<HarvestedProxyModel?> ForceConnectToLiveProxyAsync(HarvestedProxyModel? preferred = null, CancellationToken ct = default)
    {
        var pool = GetCandidatePool();
        if (pool.Count == 0) return null;

        const int internetThresholdMs = 10000; // 10 seconds threshold

        // 1. If user specifically selected a preferred proxy, attempt it first
        if (preferred != null)
        {
            OnLog?.Invoke($"[Router] Attempting connection to user-selected proxy {preferred.Endpoint} ({preferred.CountryCode} {preferred.FlagEmoji})...");
            bool ok = await ConnectAndTestInternetAsync(preferred, internetThresholdMs, msg => OnLog?.Invoke(msg), ct).ConfigureAwait(false);
            if (ok)
            {
                ConfirmLiveConnected(preferred);
                OnLog?.Invoke($"[Router] Successfully connected & verified internet access via {preferred.Endpoint}! Waiting for configured interval ({_config.RotationIntervalSeconds}s) before next rotation.");
                return _activeTimedProxy;
            }
            else
            {
                OnLog?.Invoke($"[Router] Selected proxy {preferred.Endpoint} failed internet access test (10s threshold). Auto-rotating to next available proxy server...");
                MarkUnavailable(preferred.Endpoint);
            }
        }

        // 2. Rotate through candidates until finding one with verified internet access
        var refreshedPool = GetCandidatePool();
        int maxAttempts = Math.Min(refreshedPool.Count, 25);

        for (int i = 0; i < maxAttempts; i++)
        {
            HarvestedProxyModel? candidate;
            lock (_timeRotationLock)
            {
                candidate = PickRoundRobin(refreshedPool);
            }
            if (candidate == null) break;

            bool isVerified = await ConnectAndTestInternetAsync(candidate, internetThresholdMs, msg => OnLog?.Invoke(msg), ct).ConfigureAwait(false);
            if (isVerified)
            {
                ConfirmLiveConnected(candidate);
                OnLog?.Invoke($"[Router] Locked onto verified live proxy: {candidate.Endpoint} ({candidate.CountryCode} {candidate.FlagEmoji}) with internet access! Waiting for configured interval ({_config.RotationIntervalSeconds}s) before next rotation.");
                return _activeTimedProxy;
            }
            else
            {
                OnLog?.Invoke($"[Router] Proxy {candidate.Endpoint} internet test failed (10s threshold). Auto-rotating to next available proxy server...");
                MarkUnavailable(candidate.Endpoint);
                refreshedPool = GetCandidatePool();
                if (refreshedPool.Count == 0) break;
            }
        }

        OnLog?.Invoke("[Router] Exhausted candidate pool without finding a proxy server with accessible internet.");
        return null;
    }

    public List<HarvestedProxyModel> GetCandidatePool()
    {
        var healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, _config.CountryFilter);
        if (healthy.Count == 0 && !string.IsNullOrWhiteSpace(_config.CountryFilter) && !_config.CountryFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, "ALL");
        }

        // Apply selective proxy filter: use selected proxies if any are marked selected
        var selected = healthy.Where(p => p.IsSelected).ToList();
        return selected.Count > 0 ? selected : healthy;
    }

    /// <summary>
    /// Selects an upstream proxy according to the configured rotation strategy and country filter.
    /// </summary>
    public HarvestedProxyModel? SelectUpstreamProxy(string? targetHost = null, string? clientSessionId = null)
    {
        var candidates = GetCandidatePool();
        if (candidates.Count == 0) return null;

        Interlocked.Increment(ref TotalRotatedRequests);

        // 0. Time-Based Interval Strategy (Timely rotation every X seconds)
        if (_config.Strategy == RotationStrategy.TimeBasedInterval)
        {
            var now = DateTime.UtcNow;
            int intervalSec = Math.Max(5, _config.RotationIntervalSeconds);
            lock (_timeRotationLock)
            {
                if (_activeTimedProxy == null || !_activeTimedProxy.IsHealthy || (now - _lastTimedRotation) >= TimeSpan.FromSeconds(intervalSec) || !candidates.Contains(_activeTimedProxy))
                {
                    _activeTimedProxy = PickRoundRobin(candidates);
                    _lastTimedRotation = now;
                    OnTimedRotation?.Invoke(_activeTimedProxy);
                    OnLog?.Invoke($"[Router] Timely rotation: Switched active proxy to {_activeTimedProxy?.Endpoint} ({_activeTimedProxy?.CountryCode} {_activeTimedProxy?.FlagEmoji}) - interval {intervalSec}s.");
                }
                return _activeTimedProxy ?? PickRoundRobin(candidates);
            }
        }

        // 1. Sticky Domain Strategy
        if (_config.Strategy == RotationStrategy.StickyDomain && !string.IsNullOrWhiteSpace(targetHost))
        {
            string key = $"domain_{targetHost.ToLowerInvariant()}";
            if (_stickyCache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.Now && cached.Proxy.IsHealthy)
            {
                return cached.Proxy;
            }

            var chosen = PickRoundRobin(candidates);
            if (chosen != null)
            {
                _stickyCache[key] = (chosen, DateTime.Now.AddMinutes(_config.StickySessionMinutes));
            }
            return chosen;
        }

        // 2. Sticky Session Strategy
        if (_config.Strategy == RotationStrategy.StickySession && !string.IsNullOrWhiteSpace(clientSessionId))
        {
            string key = $"sess_{clientSessionId}";
            if (_stickyCache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.Now && cached.Proxy.IsHealthy)
            {
                return cached.Proxy;
            }

            var chosen = PickRoundRobin(candidates);
            if (chosen != null)
            {
                _stickyCache[key] = (chosen, DateTime.Now.AddMinutes(_config.StickySessionMinutes));
            }
            return chosen;
        }

        // 3. Lowest Latency Strategy (picks uniformly from the top 5 fastest to prevent overload)
        if (_config.Strategy == RotationStrategy.LowestLatency)
        {
            int poolSize = Math.Min(5, candidates.Count);
            int index = Random.Shared.Next(0, poolSize);
            return candidates[index];
        }

        // 4. Default: Pure Round-Robin (Per-Request Rotation)
        return PickRoundRobin(candidates);
    }

    private HarvestedProxyModel? PickRoundRobin(List<HarvestedProxyModel> pool)
    {
        if (pool.Count == 0) return null;
        int idx = (int)((uint)Interlocked.Increment(ref _roundRobinIndex) % (uint)pool.Count);
        return pool[idx];
    }

    /// <summary>
    /// Establishes an outbound TCP socket to the chosen upstream proxy, binding to a local physical
    /// network adapter when physical adapter chaining is enabled.
    /// </summary>
    public async Task<Socket> ConnectToProxyAsync(
        HarvestedProxyModel proxy,
        IEnumerable<NetworkAdapterModel> activeAdapters,
        CancellationToken ct = default)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            ReceiveTimeout = 15000,
            SendTimeout = 15000
        };

        try
        {
            // Bind to a physical network interface for Double-Layer Multi-NIC Egress
            if (_config.ChainPhysicalAdapters)
            {
                var adapter = _loadBalancer.GetNextAdapter(activeAdapters.ToList());
                if (adapter != null && IPAddress.TryParse(adapter.Address, out var localIp) &&
                    localIp.AddressFamily == AddressFamily.InterNetwork && !localIp.Equals(IPAddress.Any))
                {
                    try
                    {
                        socket.Bind(new IPEndPoint(localIp, 0));
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"[Router] Physical bind to {adapter.Name} ({localIp}) failed: {ex.Message}. Falling back to default routing.");
                    }
                }
            }

            await socket.ConnectAsync(proxy.Host, proxy.Port, ct);
            Interlocked.Increment(ref SuccessfulRotations);
            return socket;
        }
        catch
        {
            Interlocked.Increment(ref FailedRotations);
            socket.Dispose();
            _validator.MarkUnavailable(proxy.Endpoint);
            throw;
        }
    }

    /// <summary>
    /// Evicts an unavailable upstream proxy from the router pool and persistent storage immediately.
    /// Halts rotation countdown if the active proxy becomes unavailable.
    /// </summary>
    public bool MarkUnavailable(string endpoint)
    {
        lock (_timeRotationLock)
        {
            if (_activeTimedProxy != null && _activeTimedProxy.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                _isLiveConnected = false;
                OnLog?.Invoke($"[Router] Active proxy {endpoint} marked unavailable. Countdown paused until next live connection.");
            }
        }
        return _validator.MarkUnavailable(endpoint);
    }
}
