using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace CombiSpeed.Models;

public class VirtualBridgeModel : INotifyPropertyChanged
{
    private string _connectionName = "CombiSpeed Bonded Bridge";
    public string ConnectionName
    {
        get => _connectionName;
        set => SetField(ref _connectionName, value);
    }

    private string _adapterType = "Windows Virtual Adapter / VPN Bridge";
    public string AdapterType
    {
        get => _adapterType;
        set => SetField(ref _adapterType, value);
    }

    private string _virtualIp = "10.254.1.2";
    public string VirtualIp
    {
        get => _virtualIp;
        set => SetField(ref _virtualIp, value);
    }

    private string _gatewayIp = "10.254.1.1";
    public string GatewayIp
    {
        get => _gatewayIp;
        set => SetField(ref _gatewayIp, value);
    }

    private string _subnetMask = "255.255.255.0";
    public string SubnetMask
    {
        get => _subnetMask;
        set => SetField(ref _subnetMask, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetField(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }
    }

    public string StatusText => IsConnected ? "BONDED & ACTIVE" : "READY TO CONNECT";

    public Brush StatusBrush => IsConnected
        ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)) // Green
        : new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)); // Slate

    private string _mergedAdaptersSummary = "No Physical Adapters Merged";
    public string MergedAdaptersSummary
    {
        get => _mergedAdaptersSummary;
        set => SetField(ref _mergedAdaptersSummary, value);
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

    public string FormattedDownloadSpeed => NetworkAdapterModel.FormatSpeed(DownloadSpeedBps);
    public string FormattedUploadSpeed => NetworkAdapterModel.FormatSpeed(UploadSpeedBps);

    private long _totalRxBytes;
    public long TotalRxBytes
    {
        get => _totalRxBytes;
        set
        {
            if (SetField(ref _totalRxBytes, value))
            {
                OnPropertyChanged(nameof(FormattedTotalRx));
            }
        }
    }

    private long _totalTxBytes;
    public long TotalTxBytes
    {
        get => _totalTxBytes;
        set
        {
            if (SetField(ref _totalTxBytes, value))
            {
                OnPropertyChanged(nameof(FormattedTotalTx));
            }
        }
    }

    public string FormattedTotalRx => NetworkAdapterModel.FormatBytes(TotalRxBytes);
    public string FormattedTotalTx => NetworkAdapterModel.FormatBytes(TotalTxBytes);

    private int _activeStreams;
    public int ActiveStreams
    {
        get => _activeStreams;
        set => SetField(ref _activeStreams, value);
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
