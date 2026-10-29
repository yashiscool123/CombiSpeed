using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class LoadBalancerEngine
{
    private int _currentIndex;
    private int _weightCounter;
    private readonly object _lock = new();

    public RoutingMode Mode { get; set; } = RoutingMode.Weighted;
    public string? PrimaryAdapterId { get; set; }

    public NetworkAdapterModel? GetNextAdapter(List<NetworkAdapterModel> adapters)
    {
        lock (_lock)
        {
            var available = adapters.Where(a => a.IsEnabled && !string.IsNullOrWhiteSpace(a.Address)).ToList();
            if (available.Count == 0) return null;

            // Prefer healthy adapters, fallback to all available
            var healthy = available.Where(a => a.Status != AdapterStatus.Down).ToList();
            var pool = healthy.Count > 0 ? healthy : available;

            if (pool.Count == 1) return pool[0];

            switch (Mode)
            {
                case RoutingMode.LeastLatency:
                    return pool.OrderBy(a => a.PingMs > 0 ? a.PingMs : 999).First();

                case RoutingMode.Failover:
                    if (!string.IsNullOrEmpty(PrimaryAdapterId))
                    {
                        var primary = pool.FirstOrDefault(a => a.Id == PrimaryAdapterId);
                        if (primary != null && primary.Status == AdapterStatus.Up)
                        {
                            return primary;
                        }
                    }
                    return pool[0];

                case RoutingMode.Equal:
                    _currentIndex = (_currentIndex + 1) % pool.Count;
                    return pool[_currentIndex];

                case RoutingMode.Weighted:
                default:
                    var weightedPool = pool.Where(a => a.Weight > 0).ToList();
                    if (weightedPool.Count == 0)
                    {
                        _currentIndex = (_currentIndex + 1) % pool.Count;
                        return pool[_currentIndex];
                    }

                    int minWeight = Math.Max(1, weightedPool.Min(a => a.Weight));
                    var current = weightedPool[_currentIndex % weightedPool.Count];

                    _weightCounter++;
                    int quota = Math.Max(1, (int)Math.Round((double)current.Weight / minWeight));

                    if (_weightCounter >= quota)
                    {
                        _currentIndex = (_currentIndex + 1) % weightedPool.Count;
                        _weightCounter = 0;
                    }

                    return current;
            }
        }
    }
}
