using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class LoadBalancerEngine
{
    private int _currentIndex;
    private readonly Dictionary<string, int> _currentWeights = new();
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

                    // Smooth Weighted Round Robin (SWRR):
                    // Guarantees mathematically optimal, smoothly interleaved dispatch strictly proportional to weights.
                    int totalWeight = weightedPool.Sum(a => a.Weight);
                    NetworkAdapterModel? selected = null;
                    int maxCurrentWeight = int.MinValue;

                    foreach (var adapter in weightedPool)
                    {
                        if (!_currentWeights.TryGetValue(adapter.Id, out int current))
                        {
                            current = 0;
                        }
                        current += adapter.Weight;
                        _currentWeights[adapter.Id] = current;

                        if (current > maxCurrentWeight)
                        {
                            maxCurrentWeight = current;
                            selected = adapter;
                        }
                    }

                    if (selected != null)
                    {
                        _currentWeights[selected.Id] -= totalWeight;
                        return selected;
                    }

                    _currentIndex = (_currentIndex + 1) % weightedPool.Count;
                    return weightedPool[_currentIndex];
            }
        }
    }

    public void ResetState()
    {
        lock (_lock)
        {
            _currentIndex = 0;
            _currentWeights.Clear();
        }
    }
}
