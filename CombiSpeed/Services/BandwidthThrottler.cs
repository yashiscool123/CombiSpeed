using System.Collections.Concurrent;
using System.Diagnostics;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// High-performance Token Bucket rate limiter and bandwidth traffic shaper.
/// Actively throttles network byte transfer rates and speeds across combined adapters
/// in real-time, enforcing global speed limits and proportional bandwidth ratios.
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
                    ? Math.Max(32 * 1024, _bytesPerSecond * burstSeconds) 
                    : double.MaxValue;
                if (_tokens > _bucketCapacity) _tokens = _bucketCapacity;
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
    public bool EnforceProportionalThrottling { get; set; } = true;

    /// <summary>
    /// Recalculates and configures active rate limiters for all combined adapters.
    /// </summary>
    public void UpdateConfiguration(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config)
    {
        IsThrottlingEnabled = config.EnableBandwidthThrottling;
        GlobalDownloadLimitBps = config.GlobalDownloadLimitBps;
        GlobalUploadLimitBps = config.GlobalUploadLimitBps;
        EnforceProportionalThrottling = config.EnforceProportionalThrottling;

        if (!IsThrottlingEnabled)
        {
            _globalRxBucket.SetRate(0);
            _globalTxBucket.SetRate(0);
            foreach (var b in _rxBuckets.Values) b.SetRate(0);
            foreach (var b in _txBuckets.Values) b.SetRate(0);

            foreach (var adapter in adapters)
            {
                adapter.ThrottledBandwidthBps = 0;
            }
            return;
        }

        _globalRxBucket.SetRate(GlobalDownloadLimitBps);
        _globalTxBucket.SetRate(GlobalUploadLimitBps);

        var adapterList = adapters.Where(a => a.IsEnabled && a.Weight > 0).ToList();
        double totalWeight = adapterList.Sum(a => a.Weight);

        foreach (var adapter in adapters)
        {
            var rxBucket = _rxBuckets.GetOrAdd(adapter.Id, _ => new TokenBucket(0));
            var txBucket = _txBuckets.GetOrAdd(adapter.Id, _ => new TokenBucket(0));

            if (!adapter.IsEnabled || adapter.Weight <= 0)
            {
                rxBucket.SetRate(0);
                txBucket.SetRate(0);
                adapter.ThrottledBandwidthBps = 0;
                continue;
            }

            double proportion = totalWeight > 0 ? (double)adapter.Weight / totalWeight : 1.0;

            // 1. Adapter specific limit if set
            double adapterRxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : 0;
            double adapterTxRate = adapter.SpeedLimitBps > 0 ? adapter.SpeedLimitBps : 0;

            // 2. Proportional slice of global limit
            if (GlobalDownloadLimitBps > 0 && EnforceProportionalThrottling)
            {
                double proportionalRate = GlobalDownloadLimitBps * proportion;
                adapterRxRate = adapterRxRate > 0 ? Math.Min(adapterRxRate, proportionalRate) : proportionalRate;
            }

            if (GlobalUploadLimitBps > 0 && EnforceProportionalThrottling)
            {
                double proportionalRate = GlobalUploadLimitBps * proportion;
                adapterTxRate = adapterTxRate > 0 ? Math.Min(adapterTxRate, proportionalRate) : proportionalRate;
            }

            rxBucket.SetRate(adapterRxRate);
            txBucket.SetRate(adapterTxRate);
            adapter.ThrottledBandwidthBps = (long)adapterRxRate;
        }
    }

    /// <summary>
    /// Throttles download (Rx) traffic through the specified adapter and global rate limiters.
    /// </summary>
    public async ValueTask ThrottleDownloadAsync(NetworkAdapterModel? adapter, int bytes, CancellationToken token = default)
    {
        if (!IsThrottlingEnabled || bytes <= 0) return;

        // 1. Adapter-specific / proportional bucket
        if (adapter != null && _rxBuckets.TryGetValue(adapter.Id, out var adapterBucket))
        {
            await adapterBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }

        // 2. Global combined bucket
        if (_globalRxBucket.BytesPerSecond > 0)
        {
            await _globalRxBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Throttles upload (Tx) traffic through the specified adapter and global rate limiters.
    /// </summary>
    public async ValueTask ThrottleUploadAsync(NetworkAdapterModel? adapter, int bytes, CancellationToken token = default)
    {
        if (!IsThrottlingEnabled || bytes <= 0) return;

        // 1. Adapter-specific / proportional bucket
        if (adapter != null && _txBuckets.TryGetValue(adapter.Id, out var adapterBucket))
        {
            await adapterBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }

        // 2. Global combined bucket
        if (_globalTxBucket.BytesPerSecond > 0)
        {
            await _globalTxBucket.ThrottleAsync(bytes, token).ConfigureAwait(false);
        }
    }
}
