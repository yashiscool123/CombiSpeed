using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CombiSpeed.Models;

public enum AdapterType
{
    Ethernet,
    WiFi,
    Cellular,
    Bridge,
    Virtual,
    Other
}

public enum AdapterStatus
{
    Up,
    Degraded,
    Down
}

public class NetworkAdapterModel : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Netmask { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public AdapterType Type { get; set; } = AdapterType.Ethernet;
    public string LinkSpeed { get; set; } = "Unknown";

    private AdapterStatus _status = AdapterStatus.Up;
    public AdapterStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public event Action? WeightChanged;

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                WeightChanged?.Invoke();
            }
        }
    }

    private int _weight = 50;
    public int Weight
    {
        get => _weight;
        set
        {
            if (SetField(ref _weight, Math.Clamp(value, 0, 100)))
            {
                WeightChanged?.Invoke();
            }
        }
    }

    private double _effectivePercentage = 50.0;
    public double EffectivePercentage
    {
        get => _effectivePercentage;
        set
        {
            if (SetField(ref _effectivePercentage, value))
            {
                OnPropertyChanged(nameof(FormattedEffectivePercentage));
                OnPropertyChanged(nameof(EffectivePercentageWidth));
            }
        }
    }

    public string FormattedEffectivePercentage => $"{EffectivePercentage:F1}% of Bonded Pool";
    public double EffectivePercentageWidth => Math.Max(2.0, EffectivePercentage * 3.0);

    private long _speedLimitBps = 0;
    public long SpeedLimitBps
    {
        get => _speedLimitBps;
        set
        {
            if (SetField(ref _speedLimitBps, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(FormattedSpeedLimit));
                WeightChanged?.Invoke();
            }
        }
    }

    public string FormattedSpeedLimit => _speedLimitBps <= 0 ? "Auto / Unlimited" : FormatSpeed(_speedLimitBps);

    public double CustomSpeedLimitMbps
    {
        get => _speedLimitBps <= 0 ? 0 : Math.Round((_speedLimitBps * 8.0) / 1_000_000.0, 1);
        set
        {
            SpeedLimitBps = value <= 0 ? 0 : (long)(value * 125_000.0);
            OnPropertyChanged();
        }
    }
    private long _throttledBandwidthBps = 0;
    public long ThrottledBandwidthBps
    {
        get => _throttledBandwidthBps;
        set
        {
            if (SetField(ref _throttledBandwidthBps, value))
            {
                OnPropertyChanged(nameof(FormattedThrottledBandwidth));
                OnPropertyChanged(nameof(IsThrottleActive));
            }
        }
    }

    public bool IsThrottleActive => _throttledBandwidthBps > 0;
    public string FormattedThrottledBandwidth => _throttledBandwidthBps <= 0 ? "Full Speed" : $"⚡ Capped: {FormatSpeed(_throttledBandwidthBps)}";

    public string AccentHexColor { get; set; } = "#00F0FF";

    private int _pingMs;
    public int PingMs
    {
        get => _pingMs;
        set => SetField(ref _pingMs, value);
    }

    private int _packetLossPercent;
    public int PacketLossPercent
    {
        get => _packetLossPercent;
        set => SetField(ref _packetLossPercent, value);
    }

    private long _downloadSpeedBps;
    public long DownloadSpeedBps
    {
        get => _downloadSpeedBps;
        set
        {
            if (SetField(ref _downloadSpeedBps, value))
            {
                OnPropertyChanged(nameof(FormattedDownloadSpeed));
            }
        }
    }

    private long _uploadSpeedBps;
    public long UploadSpeedBps
    {
        get => _uploadSpeedBps;
        set
        {
            if (SetField(ref _uploadSpeedBps, value))
            {
                OnPropertyChanged(nameof(FormattedUploadSpeed));
            }
        }
    }

    public long TotalRxBytes { get; set; }
    public long TotalTxBytes { get; set; }

    private string _wanIp = "Resolving...";
    public string WanIp
    {
        get => _wanIp;
        set => SetField(ref _wanIp, value);
    }

    public string FormattedDownloadSpeed => FormatSpeed(DownloadSpeedBps);
    public string FormattedUploadSpeed => FormatSpeed(UploadSpeedBps);

    public static string FormatSpeed(long bytesPerSec)
    {
        if (bytesPerSec <= 0) return "0.00 KB/s";
        double kb = bytesPerSec / 1024.0;
        if (kb < 1000) return $"{kb:F2} KB/s";
        double mb = kb / 1024.0;
        if (mb < 1000) return $"{mb:F2} MB/s";
        double gb = mb / 1024.0;
        return $"{gb:F2} GB/s";
    }


    public static string FormatSpeedWithBits(long bytesPerSec)
    {
        if (bytesPerSec <= 0) return "Unlimited";
        double mbps = (bytesPerSec * 8.0) / 1_000_000.0;
        double mb = bytesPerSec / (1024.0 * 1024.0);
        if (mbps >= 1000)
        {
            return $"{mbps / 1000.0:F2} Gbps ({mb / 1024.0:F2} GB/s)";
        }
        return $"{mbps:F1} Mbps ({mb:F1} MB/s)";
    }
    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
