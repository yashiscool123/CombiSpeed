using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxLoadBalancer
{
    private readonly object _lock = new();
    private readonly List<AdapterWeightState> _states = new();
    private RoutingMode _routingMode = RoutingMode.Weighted;
    private SpeedShaperMode _shaperMode = SpeedShaperMode.BalancedDynamic;

    private class AdapterWeightState
    {
        public NetworkAdapterModel Adapter { get; set; } = null!;
        public int EffectiveWeight { get; set; }
        public int CurrentWeight { get; set; }
    }

    public void UpdateAdapters(IEnumerable<NetworkAdapterModel> adapters, RoutingMode routingMode, SpeedShaperMode shaperMode)
    {
        lock (_lock)
        {
            _routingMode = routingMode;
            _shaperMode = shaperMode;
            _states.Clear();

            var enabled = adapters.Where(a => a.IsEnabled).ToList();
            if (enabled.Count == 0) return;

            int totalWeight = enabled.Sum(a => a.Weight);
            if (totalWeight == 0) totalWeight = 1;

            foreach (var adapter in enabled)
            {
                int weight = adapter.Weight;

                // Dynamic shaper adjustments
                if (_shaperMode == SpeedShaperMode.LowestLatency)
                {
                    // Bias towards lower ping (inverse ping score)
                    int ping = Math.Max(1, adapter.PingMs);
                    double latencyFactor = 50.0 / ping;
                    weight = Math.Max(5, (int)(weight * latencyFactor));
                }
                else if (_shaperMode == SpeedShaperMode.MaximumBandwidth)
                {
                    // Bias towards higher link speed
                    double speedFactor = Math.Max(0.5, adapter.LinkSpeedMbps / 1000.0);
                    weight = Math.Max(10, (int)(weight * speedFactor));
                }

                _states.Add(new AdapterWeightState
                {
                    Adapter = adapter,
                    EffectiveWeight = Math.Max(1, weight),
                    CurrentWeight = 0
                });

                adapter.EffectivePercentage = Math.Round((double)adapter.Weight / totalWeight * 100.0, 1);
            }
        }
    }

    public NetworkAdapterModel? GetNextAdapter()
    {
        lock (_lock)
        {
            if (_states.Count == 0) return null;
            if (_states.Count == 1) return _states[0].Adapter;

            if (_routingMode == RoutingMode.EqualShare)
            {
                // Simple Round-Robin
                var first = _states[0];
                _states.RemoveAt(0);
                _states.Add(first);
                return first.Adapter;
            }

            if (_routingMode == RoutingMode.LeastLatency)
            {
                return _states.OrderBy(s => s.Adapter.PingMs).First().Adapter;
            }

            // Smooth Weighted Round-Robin (SWRR)
            int totalWeight = 0;
            AdapterWeightState? best = null;

            foreach (var state in _states)
            {
                state.CurrentWeight += state.EffectiveWeight;
                totalWeight += state.EffectiveWeight;

                if (best == null || state.CurrentWeight > best.CurrentWeight)
                {
                    best = state;
                }
            }

            if (best != null)
            {
                best.CurrentWeight -= totalWeight;
                return best.Adapter;
            }

            return _states[0].Adapter;
        }
    }
}
