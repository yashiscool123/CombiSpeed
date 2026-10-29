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

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    private int _weight = 50;
    public int Weight
    {
        get => _weight;
        set => SetField(ref _weight, Math.Clamp(value, 0, 100));
    }

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
