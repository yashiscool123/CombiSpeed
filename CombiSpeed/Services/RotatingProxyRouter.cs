using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// Dispatcher and routing engine that selects upstream proxies according to configurable policies
/// (Round Robin, Sticky Domain, Lowest Latency, Elite Only) and binds outbound sockets across
/// local physical network adapters for double-layer multi-WAN acceleration.
/// </summary>
public class RotatingProxyRouter
{
    private readonly ProxyValidatorEngine _validator;
    private readonly RotatingProxyConfigModel _config;
    private readonly LoadBalancerEngine _loadBalancer;

    private int _roundRobinIndex;
    private readonly ConcurrentDictionary<string, (HarvestedProxyModel Proxy, DateTime ExpiresAt)> _stickyCache = new(StringComparer.OrdinalIgnoreCase);

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
    /// Selects an upstream proxy according to the configured rotation strategy and country filter.
    /// </summary>
    public HarvestedProxyModel? SelectUpstreamProxy(string? targetHost = null, string? clientSessionId = null)
    {
        var healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, _config.CountryFilter);
        if (healthy.Count == 0 && !string.IsNullOrWhiteSpace(_config.CountryFilter) && !_config.CountryFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            // Graceful fallback to all available healthy proxies if specific country has none
            healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy, "ALL");
        }
        if (healthy.Count == 0) return null;

        Interlocked.Increment(ref TotalRotatedRequests);

        // 1. Sticky Domain Strategy
        if (_config.Strategy == RotationStrategy.StickyDomain && !string.IsNullOrWhiteSpace(targetHost))
        {
            string key = $"domain_{targetHost.ToLowerInvariant()}";
            if (_stickyCache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTime.Now && cached.Proxy.IsHealthy)
            {
                return cached.Proxy;
            }

            var chosen = PickRoundRobin(healthy);
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

            var chosen = PickRoundRobin(healthy);
            if (chosen != null)
            {
                _stickyCache[key] = (chosen, DateTime.Now.AddMinutes(_config.StickySessionMinutes));
            }
            return chosen;
        }

        // 3. Lowest Latency Strategy (picks uniformly from the top 5 fastest to prevent overload)
        if (_config.Strategy == RotationStrategy.LowestLatency)
        {
            int poolSize = Math.Min(5, healthy.Count);
            int index = Random.Shared.Next(0, poolSize);
            return healthy[index];
        }

        // 4. Default: Pure Round-Robin (Per-Request Rotation)
        return PickRoundRobin(healthy);
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
