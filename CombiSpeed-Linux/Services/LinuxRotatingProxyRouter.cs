using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxRotatingProxyRouter
{
    private readonly LinuxProxyValidator _validator;
    private readonly RotatingProxyConfigModel _config;
    private readonly LinuxLoadBalancer _loadBalancer;

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
            if (!_config.AutoRotateEnabled) return -2; // -2: Paused while keeping connection active
            int intervalSec = Math.Clamp(_config.RotationIntervalSeconds, 5, 7200);
            var elapsed = (DateTime.UtcNow - _lastTimedRotation).TotalSeconds;
            return Math.Max(0, (int)(intervalSec - elapsed));
        }
    }

    /// <summary>
    /// Disconnects from the proxy server, switching all traffic back to direct multi-WAN adapter bonding.
    /// </summary>
    public void DisconnectProxy()
    {
        lock (_timeRotationLock)
        {
            _activeTimedProxy = null;
            _isLiveConnected = false;
        }
        OnTimedRotation?.Invoke(null);
    }

    public long TotalRotatedRequests;
    public long SuccessfulRotations;
    public long FailedRotations;

    public LinuxRotatingProxyRouter(LinuxProxyValidator validator, RotatingProxyConfigModel config, LinuxLoadBalancer loadBalancer)
    {
        _validator = validator;
        _config = config;
        _loadBalancer = loadBalancer;
    }

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
    }

    public HarvestedProxyModel? ForceRotate()
    {
        var pool = GetCandidatePool();
        if (pool.Count == 0) return null;
        var chosen = PickRoundRobin(pool);
        if (chosen != null)
        {
            ConfirmLiveConnected(chosen);
        }
        return _activeTimedProxy;
    }

    public async Task CheckAutoRotationAsync(CancellationToken ct = default)
    {
        if (_config.Strategy != RotationStrategy.TimeBasedInterval) return;
        if (!_config.AutoRotateEnabled) return; // Rotation paused
        if (!_isLiveConnected || _activeTimedProxy == null || _isAutoRotating) return;

        if (SecondsUntilNextRotation == 0)
        {
            _isAutoRotating = true;
            try
            {
                lock (_timeRotationLock)
                {
                    _isLiveConnected = false;
                }
                await ForceConnectToLiveProxyAsync(null, ct);
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

                byte[] req = new byte[] { 0x05, 0x01, 0x00, 0x01, 1, 1, 1, 1, 0, 80 };
                await stream.WriteAsync(req, token).ConfigureAwait(false);
                byte[] resp = new byte[512];
                r = await stream.ReadAsync(resp.AsMemory(0, resp.Length), token).ConfigureAwait(false);
                if (r < 4 || resp[0] != 0x05 || resp[1] != 0x00) return false;
            }
            else if (proxy.Protocol == ProxyProtocol.Socks4)
            {
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
        bool serverConnected = await ProbeLiveConnectionAsync(proxy, 3000, ct).ConfigureAwait(false);
        if (!serverConnected) return false;

        var sw = Stopwatch.StartNew();
        bool internetAccessible = await TestInternetAccessibilityAsync(proxy, internetThresholdMs, ct).ConfigureAwait(false);
        sw.Stop();

        if (internetAccessible)
        {
            proxy.LatencyMs = (int)sw.ElapsedMilliseconds;
            return true;
        }
        return false;
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

        const int internetThresholdMs = 10000;

        if (preferred != null)
        {
            bool ok = await ConnectAndTestInternetAsync(preferred, internetThresholdMs, null, ct).ConfigureAwait(false);
            if (ok)
            {
                ConfirmLiveConnected(preferred);
                return _activeTimedProxy;
            }
            else
            {
                MarkUnavailable(preferred.Endpoint);
            }
        }

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

            bool isVerified = await ConnectAndTestInternetAsync(candidate, internetThresholdMs, null, ct).ConfigureAwait(false);
            if (isVerified)
            {
                ConfirmLiveConnected(candidate);
                return _activeTimedProxy;
            }
            else
            {
                MarkUnavailable(candidate.Endpoint);
                refreshedPool = GetCandidatePool();
                if (refreshedPool.Count == 0) break;
            }
        }

        return null;
    }

    public List<HarvestedProxyModel> GetCandidatePool()
    {
        var healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, _config.CountryFilter);
        if (healthy.Count == 0 && !string.IsNullOrWhiteSpace(_config.CountryFilter) && !_config.CountryFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, "ALL");
        }

        var selected = healthy.Where(p => p.IsSelected).ToList();
        return selected.Count > 0 ? selected : healthy;
    }

    public HarvestedProxyModel? SelectUpstreamProxy(string? targetHost = null, string? clientSessionId = null)
    {
        var pool = GetCandidatePool();
        if (pool.Count == 0) return null;

        Interlocked.Increment(ref TotalRotatedRequests);

        if (_config.Strategy == RotationStrategy.TimeBasedInterval)
        {
            lock (_timeRotationLock)
            {
                if (_activeTimedProxy != null && _activeTimedProxy.IsHealthy && pool.Contains(_activeTimedProxy))
                {
                    return _activeTimedProxy;
                }
                _activeTimedProxy = PickRoundRobin(pool);
                _lastTimedRotation = DateTime.UtcNow;
                if (_activeTimedProxy != null)
                {
                    OnTimedRotation?.Invoke(_activeTimedProxy);
                }
                return _activeTimedProxy;
            }
        }

        if (_config.Strategy == RotationStrategy.StickyDomain && !string.IsNullOrWhiteSpace(targetHost))
        {
            string key = $"domain_{targetHost.ToLowerInvariant()}";
            if (_stickyCache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.Now && cached.Proxy.IsHealthy)
            {
                return cached.Proxy;
            }

            var chosen = PickRoundRobin(pool);
            if (chosen != null)
            {
                _stickyCache[key] = (chosen, DateTime.Now.AddMinutes(_config.StickySessionMinutes));
            }
            return chosen;
        }

        if (_config.Strategy == RotationStrategy.LowestLatency)
        {
            int poolSize = Math.Min(5, pool.Count);
            return pool[Random.Shared.Next(0, poolSize)];
        }

        return PickRoundRobin(pool);
    }

    private HarvestedProxyModel? PickRoundRobin(List<HarvestedProxyModel> pool)
    {
        if (pool.Count == 0) return null;
        int idx = (int)((uint)Interlocked.Increment(ref _roundRobinIndex) % (uint)pool.Count);
        return pool[idx];
    }

    public async Task<Socket> ConnectToProxyAsync(HarvestedProxyModel proxy, IEnumerable<NetworkAdapterModel> activeAdapters, CancellationToken ct = default)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            if (_config.ChainPhysicalAdapters)
            {
                var adapter = _loadBalancer.GetNextAdapter();
                if (adapter != null && IPAddress.TryParse(adapter.IpAddress, out var localIp))
                {
                    try { socket.Bind(new IPEndPoint(localIp, 0)); } catch { }
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

    public bool MarkUnavailable(string endpoint)
    {
        lock (_timeRotationLock)
        {
            if (_activeTimedProxy != null && _activeTimedProxy.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                _isLiveConnected = false;
            }
        }
        return _validator.MarkUnavailable(endpoint);
    }
}
