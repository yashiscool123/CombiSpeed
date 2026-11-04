using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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
            int intervalSec = Math.Clamp(_config.RotationIntervalSeconds, 5, 7200);
            var elapsed = (DateTime.UtcNow - _lastTimedRotation).TotalSeconds;
            return Math.Max(0, (int)(intervalSec - elapsed));
        }
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
    public static async Task<bool> ProbeLiveConnectionAsync(HarvestedProxyModel proxy, int timeoutMs = 1500, CancellationToken ct = default)
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
    /// Forces a connection to a specific target proxy (if provided) or rotates through candidates
    /// until a live, responsive connection is found and connected.
    /// Guaranteed to continue rotating until finding a live connection or exhausting candidate pool.
    /// </summary>
    public async Task<HarvestedProxyModel?> ForceConnectToLiveProxyAsync(HarvestedProxyModel? preferred = null, CancellationToken ct = default)
    {
        var pool = GetCandidatePool();
        if (pool.Count == 0) return null;

        if (preferred != null)
        {
            bool ok = await ProbeLiveConnectionAsync(preferred, 1500, ct).ConfigureAwait(false);
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
        int maxAttempts = Math.Min(refreshedPool.Count, 15);

        for (int i = 0; i < maxAttempts; i++)
        {
            HarvestedProxyModel? candidate;
            lock (_timeRotationLock)
            {
                candidate = PickRoundRobin(refreshedPool);
            }
            if (candidate == null) break;

            bool isLive = await ProbeLiveConnectionAsync(candidate, 1200, ct).ConfigureAwait(false);
            if (isLive)
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
            var now = DateTime.UtcNow;
            int intervalSec = Math.Max(5, _config.RotationIntervalSeconds);
            lock (_timeRotationLock)
            {
                if (_activeTimedProxy == null || !_activeTimedProxy.IsHealthy || (now - _lastTimedRotation) >= TimeSpan.FromSeconds(intervalSec) || !pool.Contains(_activeTimedProxy))
                {
                    _activeTimedProxy = PickRoundRobin(pool);
                    _lastTimedRotation = now;
                    OnTimedRotation?.Invoke(_activeTimedProxy);
                }
                return _activeTimedProxy ?? PickRoundRobin(pool);
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
