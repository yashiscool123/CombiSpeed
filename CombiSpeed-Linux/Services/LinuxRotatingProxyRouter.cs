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

    public LinuxRotatingProxyRouter(LinuxProxyValidator validator, RotatingProxyConfigModel config, LinuxLoadBalancer loadBalancer)
    {
        _validator = validator;
        _config = config;
        _loadBalancer = loadBalancer;
    }

    public HarvestedProxyModel? SelectUpstreamProxy(string? targetHost = null, string? clientSessionId = null)
    {
        var healthy = _validator.GetHealthyProxies(_config.StrictAnonymity, _config.Strategy);
        if (healthy.Count == 0) return null;

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

        if (_config.Strategy == RotationStrategy.LowestLatency)
        {
            int poolSize = Math.Min(5, healthy.Count);
            return healthy[Random.Shared.Next(0, poolSize)];
        }

        return PickRoundRobin(healthy);
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
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
