using System.Collections.Concurrent;
using System.Diagnostics;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// High-performance Token Bucket rate limiter and dynamic bandwidth traffic shaper.
/// Actively throttles network byte transfer rates and speeds across combined adapters
/// in real-time, supporting Maximum Bandwidth, Lowest Latency (Anti-Bufferbloat), and Balanced modes.
/// </summary>
public class BandwidthThrottler
{
    /// <summary>
    /// Thread-safe high-resolution Token Bucket implementation for rate limiting.
    /// </summary>
    public class TokenBucket
    {
        private readonly object _sync = new();
        private double _tokens;
        private long _lastTimestamp;
        private double _bytesPerSecond;
        private double _bucketCapacity;

        public double BytesPerSecond => _bytesPerSecond;
        public double AvailableTokens { get { lock (_sync) return _tokens; } }

        public TokenBucket(double bytesPerSecond, double burstSeconds = 0.05)
        {
            SetRate(bytesPerSecond, burstSeconds);
            _tokens = _bucketCapacity;
            _lastTimestamp = Stopwatch.GetTimestamp();
        }

        public void SetRate(double bytesPerSecond, double burstSeconds = 0.05)
        {
            lock (_sync)
            {
                _bytesPerSecond = Math.Max(0, bytesPerSecond);
                _bucketCapacity = _bytesPerSecond > 0 
                    ? Math.Max(16 * 1024, _bytesPerSecond * burstSeconds) 
                    : double.MaxValue;
                if (_tokens > _bucketCapacity) _tokens = _bucketCapacity;
            }
        }

        public bool TryConsume(int bytes)
        {
            if (_bytesPerSecond <= 0) return true;
            lock (_sync)
            {
                long now = Stopwatch.GetTimestamp();
                double elapsedSeconds = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
                _lastTimestamp = now;
                _tokens = Math.Min(_bucketCapacity, _tokens + (elapsedSeconds * _bytesPerSecond));

                if (_tokens >= bytes)
                {
                    _tokens -= bytes;
                    return true;
                }
                return false;
            }
        }

        public async ValueTask ThrottleAsync(int bytes, CancellationToken token = default)
        {
            if (_bytesPerSecond <= 0 || bytes <= 0) return;

            double delayMs = 0;

            lock (_sync)
            {
                long now = Stopwatch.GetTimestamp();
                double elapsedSeconds = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
                _lastTimestamp = now;

                _tokens = Math.Min(_bucketCapacity, _tokens + (elapsedSeconds * _bytesPerSecond));

                if (_tokens >= bytes)
                {
                    _tokens -= bytes;
                    return;
                }

                double deficit = bytes - _tokens;
                delayMs = (deficit / _bytesPerSecond) * 1000.0;
                _tokens = 0;
            }

            if (delayMs > 0.5)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delayMs, 2000)), token).ConfigureAwait(false);
            }
        }
    }

    private readonly ConcurrentDictionary<string, TokenBucket> _rxBuckets = new();
    private readonly ConcurrentDictionary<string, TokenBucket> _txBuckets = new();
    private readonly TokenBucket _globalRxBucket = new(0);
    private readonly TokenBucket _globalTxBucket = new(0);

    public bool IsThrottlingEnabled { get; set; } = true;
    public long GlobalDownloadLimitBps { get; set; } = 0;
    public long GlobalUploadLimitBps { get; set; } = 0;
    public bool CombineSpeedsMode { get; set; } = true;
    public bool EnforceProportionalThrottling { get; set; } = false;

    public SpeedShaperMode ShaperMode { get; set; } = SpeedShaperMode.MaximumBandwidth;
    public bool EnableDynamicShaping { get; set; } = true;
    public double ActiveBurstSeconds { get; private set; } = 0.05;

    public double CalculateOptimalBurstSeconds(ProxyConfigModel config)
    {
        return config.ShaperMode switch
        {
            SpeedShaperMode.LowestLatency => 0.02, // 20ms anti-bufferbloat window
            SpeedShaperMode.MaximumBandwidth => Math.Max(0.15, config.BurstToleranceSeconds > 0 ? config.BurstToleranceSeconds : 0.15), // 150-200ms TCP window expansion
            SpeedShaperMode.BalancedDynamic => config.BurstToleranceSeconds > 0 ? config.BurstToleranceSeconds : 0.05,
            _ => 0.05
        };
    }

    /// <summary>
    /// Recalculates and configures active rate limiters for all combined adapters.
    /// </summary>
    public void UpdateConfiguration(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config)
    {
        IsThrottlingEnabled = config.EnableBandwidthThrottling;
        GlobalDownloadLimitBps = config.GlobalDownloadLimitBps;
        GlobalUploadLimitBps = config.GlobalUploadLimitBps;
        CombineSpeedsMode = config.CombineSpeedsMode;
        EnforceProportionalThrottling = config.EnforceProportionalThrottling;
        ShaperMode = config.ShaperMode;
        EnableDynamicShaping = config.EnableDynamicShaping;

        ActiveBurstSeconds = CalculateOptimalBurstSeconds(config);

        if (!IsThrottlingEnabled)
        {
            _globalRxBucket.SetRate(0, ActiveBurstSeconds);
            _globalTxBucket.SetRate(0, ActiveBurstSeconds);
            foreach (var b in _rxBuckets.Values) b.SetRate(0, ActiveBurstSeconds);
            foreach (var b in _txBuckets.Values) b.SetRate(0, ActiveBurstSeconds);

            foreach (var adapter in adapters)
            {
                adapter.ThrottledBandwidthBps = 0;
            }
            return;
        }

        _globalRxBucket.SetRate(GlobalDownloadLimitBps, ActiveBurstSeconds);
        _globalTxBucket.SetRate(GlobalUploadLimitBps, ActiveBurstSeconds);

        var adapterList = adapters.Where(a => a.IsEnabled && a.Weight > 0).ToList();
        if (adapterList.Count == 0) return;

        // Calculate dynamic weights across connections based on ShaperMode
        var dynamicWeights = new Dictionary<string, double>();
        if (EnableDynamicShaping && ShaperMode == SpeedShaperMode.LowestLatency)
        {
            // Dynamic Latency Scoring: prioritize low pings, penalize loss & degradation
            foreach (var adapter in adapterList)
            {
                int ping = adapter.PingMs > 0 ? adapter.PingMs : 50;
                double latencyWeight = 1000.0 / Math.Max(5, ping);
                if (adapter.PacketLossPercent > 0)
                {
                    latencyWeight *= Math.Max(0.05, 1.0 - (adapter.PacketLossPercent / 100.0));
                }
                if (adapter.Status == AdapterStatus.Degraded)
                {
                    latencyWeight *= 0.5;
                }
                double combined = (adapter.Weight * 0.25) + (latencyWeight * 0.75);
                dynamicWeights[adapter.Id] = Math.Max(1.0, combined);
            }
        }
        else if (EnableDynamicShaping && ShaperMode == SpeedShaperMode.MaximumBandwidth)
        {
            // Maximum Throughput Mode: maximize bandwidth utilization
            foreach (var adapter in adapterList)
            {
                double lossMultiplier = adapter.PacketLossPercent > 20 ? 0.5 : 1.0;
                dynamicWeights[adapter.Id] = Math.Max(5.0, adapter.Weight * lossMultiplier);
            }
        }
        else
        {
            foreach (var adapter in adapterList)
            {
                dynamicWeights[adapter.Id] = Math.Max(1.0, adapter.Weight);
            }
        }

        double totalDynamicWeight = dynamicWeights.Values.Sum();

        foreach (var adapter in adapters)
        {
            var rxBucket = _rxBuckets.GetOrAdd(adapter.Id, _ => new TokenBucket(0, ActiveBurstSeconds));
            var txBucket = _txBuckets.GetOrAdd(adapter.Id, _ => new TokenBucket(0, ActiveBurstSeconds));

            if (!adapter.IsEnabled || adapter.Weight <= 0)
            {
                rxBucket.SetRate(0, ActiveBurstSeconds);
                txBucket.SetRate(0, ActiveBurstSeconds);
                adapter.ThrottledBandwidthBps = 0;
                continue;
            }

            double weight = dynamicWeights.TryGetValue(adapter.Id, out double dw) ? dw : adapter.Weight;
            double proportion = totalDynamicWeight > 0 ? weight / totalDynamicWeight : 1.0;

            if (EnableDynamicShaping && totalDynamicWeight > 0)
            {
                adapter.EffectivePercentage = Math.Round(proportion * 100.0, 1);
            }

            // 1. Adapter specific limit if set
            double adapterRxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : 0;
            double adapterTxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : 0;

            if (ShaperMode == SpeedShaperMode.LowestLatency && EnableDynamicShaping && GlobalDownloadLimitBps > 0)
            {
                // In Lowest Latency (Anti-Bufferbloat) mode: dynamic weighting prioritizes lowest ping connection
                double prioritizedRate = GlobalDownloadLimitBps * proportion;
                adapterRxRate = adapter.SpeedLimitBps > 0 ? Math.Min(adapter.SpeedLimitBps, prioritizedRate) : prioritizedRate;
                adapterTxRate = adapter.SpeedLimitBps > 0 ? Math.Min(adapter.SpeedLimitBps, prioritizedRate) : prioritizedRate;
            }
            else if (CombineSpeedsMode || !EnforceProportionalThrottling)
            {
                // COMBINE SPEEDS MODE (Maximum Bandwidth & Balanced):
                // Global limit applies to the combined aggregate throughput of all connections together.
                // Adapters are NOT partitioned or sliced into fractions.
                // Each adapter can burst up to the global limit (or full wire speed), pooling their bandwidth.
                adapterRxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : GlobalDownloadLimitBps;
                adapterTxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : GlobalUploadLimitBps;
            }
            else
            {
                // Legacy proportional slice of global limit (only if explicitly enabled)
                if (GlobalDownloadLimitBps > 0)
                {
                    double proportionalRate = GlobalDownloadLimitBps * proportion;
                    adapterRxRate = adapterRxRate > 0 ? Math.Min(adapterRxRate, proportionalRate) : proportionalRate;
                }

                if (GlobalUploadLimitBps > 0)
                {
                    double proportionalRate = GlobalUploadLimitBps * proportion;
                    adapterTxRate = adapterTxRate > 0 ? Math.Min(adapterTxRate, proportionalRate) : proportionalRate;
                }
            }

            rxBucket.SetRate(adapterRxRate, ActiveBurstSeconds);
            txBucket.SetRate(adapterTxRate, ActiveBurstSeconds);
            adapter.ThrottledBandwidthBps = (long)adapterRxRate;
        }
    }

    /// <summary>
    /// Dynamically adapts rates in real-time as adapter health/telemetry changes without resetting tokens.
    /// </summary>
    public void AdaptDynamically(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config)
    {
        if (!IsThrottlingEnabled || !EnableDynamicShaping) return;
        UpdateConfiguration(adapters, config);
    }

    /// <summary>
    /// Throttles download (Rx) traffic through the specified adapter and global rate limiters.
    /// In Combine Speeds Mode, all adapters and connections pool their bandwidth additively.
    /// </summary>
    public async ValueTask ThrottleDownloadAsync(NetworkAdapterModel? adapter, int bytes, CancellationToken token = default)
    {
        if (!IsThrottlingEnabled || bytes <= 0) return;

        // 1. Adapter-specific cap if user explicitly restricted this specific adapter
        if (adapter != null && adapter.SpeedLimitBps > 0 && _rxBuckets.TryGetValue(adapter.Id, out var adapterBucket))
        {
            await adapterBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
        else if (EnforceProportionalThrottling && !CombineSpeedsMode && adapter != null && _rxBuckets.TryGetValue(adapter.Id, out var propBucket))
        {
            await propBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }

        // 2. Global combined bucket - regulates aggregate bandwidth across all connections
        if (_globalRxBucket.BytesPerSecond > 0)
        {
            await _globalRxBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Throttles upload (Tx) traffic through the specified adapter and global rate limiters.
    /// In Combine Speeds Mode, all adapters and connections pool their bandwidth additively.
    /// </summary>
    public async ValueTask ThrottleUploadAsync(NetworkAdapterModel? adapter, int bytes, CancellationToken token = default)
    {
        if (!IsThrottlingEnabled || bytes <= 0) return;

        // 1. Adapter-specific cap if user explicitly restricted this specific adapter
        if (adapter != null && adapter.SpeedLimitBps > 0 && _txBuckets.TryGetValue(adapter.Id, out var adapterBucket))
        {
            await adapterBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
        else if (EnforceProportionalThrottling && !CombineSpeedsMode && adapter != null && _txBuckets.TryGetValue(adapter.Id, out var propBucket))
        {
            await propBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }

        // 2. Global combined bucket - regulates aggregate bandwidth across all connections
        if (_globalTxBucket.BytesPerSecond > 0)
        {
            await _globalTxBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Converts a numerical rate and unit string into raw bytes per second.
    /// </summary>
    public static long ConvertToBytesPerSec(double value, string unit)
    {
        if (value <= 0) return 0;
        return unit?.ToUpperInvariant() switch
        {
            "MBPS" => (long)(value * 125_000.0), // 1 Mbps = 1,000,000 bits / 8 = 125,000 bytes/sec
            "MB/S" => (long)(value * 1024.0 * 1024.0),
            "GBPS" => (long)(value * 125_000_000.0),
            "GB/S" => (long)(value * 1024.0 * 1024.0 * 1024.0),
            "KBPS" => (long)(value * 125.0),
            "KB/S" => (long)(value * 1024.0),
            _ => (long)(value * 125_000.0)
        };
    }

    /// <summary>
    /// Converts raw bytes per second into a numerical rate and unit string.
    /// </summary>
    public static (double value, string unit) ConvertFromBytesPerSec(long bytesPerSec, string preferredUnit = "Mbps")
    {
        if (bytesPerSec <= 0) return (0, preferredUnit);
        if (preferredUnit.Equals("MB/s", StringComparison.OrdinalIgnoreCase))
        {
            return (bytesPerSec / (1024.0 * 1024.0), "MB/s");
        }
        double mbps = (bytesPerSec * 8.0) / 1_000_000.0;
        return (mbps, "Mbps");
    }

    public static string GetModeDescription(SpeedShaperMode mode) => mode switch
    {
        SpeedShaperMode.MaximumBandwidth => "🚀 Max Bandwidth (High Throughput)",
        SpeedShaperMode.LowestLatency => "⚡ Lowest Latency (Anti-Bufferbloat)",
        SpeedShaperMode.BalancedDynamic => "⚖ Balanced Dynamic (Proportional)",
        _ => "Standard"
    };
}
