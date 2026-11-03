using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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

    public event Action<string>? OnLog;

    public RotatingProxyRouter(ProxyValidatorEngine validator, RotatingProxyConfigModel config, LoadBalancerEngine loadBalancer)
    {
        _validator = validator;
        _config = config;
        _loadBalancer = loadBalancer;
    }

    /// <summary>
    /// Forces an immediate rotation to the next available selected proxy.
    /// </summary>
    public HarvestedProxyModel? ForceRotate()
    {
        var healthy = GetCandidatePool();
        if (healthy.Count == 0) return null;

        lock (_timeRotationLock)
        {
            _activeTimedProxy = PickRoundRobin(healthy);
            _lastTimedRotation = DateTime.UtcNow;
            OnTimedRotation?.Invoke(_activeTimedProxy);
            OnLog?.Invoke($"[Router] Manual rotation: Switched active proxy to {_activeTimedProxy?.Endpoint} ({_activeTimedProxy?.CountryCode} {_activeTimedProxy?.FlagEmoji}).");
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
    /// </summary>
    public bool MarkUnavailable(string endpoint) => _validator.MarkUnavailable(endpoint);
}
