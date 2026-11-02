using System.Diagnostics;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacBandwidthThrottler
{
    private readonly object _sync = new();
    private double _rateBytesPerSec;
    private double _capacity;
    private double _tokens;
    private long _lastTicks;
    private double _burstSeconds = 0.10;
    private SpeedShaperMode _mode = SpeedShaperMode.BalancedDynamic;
    private bool _enableDynamicBorrowing = true;

    public double CurrentCapMbps { get; private set; }
    public double ActiveRateMbps => _rateBytesPerSec > 0 ? (_rateBytesPerSec * 8) / 1_000_000 : 0;
    public double BurstWindowMs => _burstSeconds * 1000.0;
    public SpeedShaperMode Mode => _mode;
    public bool EnableDynamicBorrowing => _enableDynamicBorrowing;

    public MacBandwidthThrottler(double speedLimitMbps = 0, SpeedShaperMode mode = SpeedShaperMode.BalancedDynamic)
    {
        Configure(speedLimitMbps, mode, CalculateOptimalBurstSeconds(mode), true);
    }

    public void Configure(double speedLimitMbps, SpeedShaperMode mode, double? customBurstSeconds = null, bool enableBorrowing = true)
    {
        lock (_sync)
        {
            CurrentCapMbps = speedLimitMbps;
            _mode = mode;
            _enableDynamicBorrowing = enableBorrowing;

            _burstSeconds = customBurstSeconds ?? CalculateOptimalBurstSeconds(mode);

            if (speedLimitMbps <= 0)
            {
                _rateBytesPerSec = 0;
                _capacity = 0;
                _tokens = 0;
            }
            else
            {
                _rateBytesPerSec = (speedLimitMbps * 1_000_000.0) / 8.0;
                _capacity = _rateBytesPerSec * _burstSeconds;
                _tokens = _capacity;
            }

            _lastTicks = Stopwatch.GetTimestamp();
        }
    }

    public static double CalculateOptimalBurstSeconds(SpeedShaperMode mode)
    {
        return mode switch
        {
            SpeedShaperMode.MaximumBandwidth => 0.20,
            SpeedShaperMode.LowestLatency => 0.02,
            _ => 0.10
        };
    }

    public async Task ThrottleAsync(int bytes, string? adapterId = null)
    {
        if (CurrentCapMbps <= 0 || bytes <= 0) return;

        double delayMs = 0;
        lock (_sync)
        {
            ReplenishTokens();

            if (_tokens >= bytes)
            {
                _tokens -= bytes;
                return;
            }

            double missing = bytes - _tokens;
            _tokens = 0;
            delayMs = (missing / _rateBytesPerSec) * 1000.0;
        }

        if (delayMs > 0)
        {
            int waitMs = (int)Math.Min(500, Math.Max(1, delayMs));
            await Task.Delay(waitMs);
        }
    }

    private void ReplenishTokens()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsedSeconds = (double)(now - _lastTicks) / Stopwatch.Frequency;
        _lastTicks = now;

        if (elapsedSeconds > 0 && _rateBytesPerSec > 0)
        {
            double newTokens = elapsedSeconds * _rateBytesPerSec;
            _tokens = Math.Min(_capacity, _tokens + newTokens);
        }
    }
}

