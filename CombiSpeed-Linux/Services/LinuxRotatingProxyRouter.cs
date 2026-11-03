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

    public HarvestedProxyModel? ActiveTimedProxy => _activeTimedProxy;
    public event Action<HarvestedProxyModel?>? OnTimedRotation;

    public int SecondsUntilNextRotation
    {
        get
        {
            if (_config.Strategy != RotationStrategy.TimeBasedInterval) return 0;
            int intervalSec = Math.Max(5, _config.RotationIntervalSeconds);
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

    public HarvestedProxyModel? ForceRotate()
    {
        var pool = GetCandidatePool();
        if (pool.Count == 0) return null;
        lock (_timeRotationLock)
        {
            _activeTimedProxy = PickRoundRobin(pool);
            _lastTimedRotation = DateTime.UtcNow;
            OnTimedRotation?.Invoke(_activeTimedProxy);
            return _activeTimedProxy;
        }
    }

    private List<HarvestedProxyModel> GetCandidatePool()
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

    public bool MarkUnavailable(string endpoint) => _validator.MarkUnavailable(endpoint);
}
