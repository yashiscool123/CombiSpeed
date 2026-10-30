using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CombiSpeed.Models;
using CombiSpeed.Services;

namespace CombiSpeed.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly NetworkDetector _networkDetector = new();
    private readonly LoadBalancerEngine _loadBalancer = new();
    private readonly WindowsProxyManager _proxyManager = new();
    private readonly VirtualBridgeManager _virtualBridgeManager = new("CombiSpeed Bonded Bridge");
    private readonly SpeedTester _speedTester = new();
    private readonly HealthMonitor _healthMonitor;
    private readonly ProxyEngine _proxyEngine;

    private readonly DispatcherTimer _uiTimer;
    private readonly List<double> _downloadHistory = new(new double[60]);
    private readonly List<double> _uploadHistory = new(new double[60]);
    private long _prevBridgeRx;
    private long _prevBridgeTx;

    public ObservableCollection<NetworkAdapterModel> Adapters { get; } = new();
    public ObservableCollection<AuditLogModel> AuditLogs { get; } = new();

    public VirtualBridgeModel VirtualBridge => _virtualBridgeManager.Model;

    private bool _isEngineRunning;
    public bool IsEngineRunning
    {
        get => _isEngineRunning;
        set => SetField(ref _isEngineRunning, value);
    }

    private bool _isWindowsProxyActive;
    public bool IsWindowsProxyActive
    {
        get => _isWindowsProxyActive;
        set => SetField(ref _isWindowsProxyActive, value);
    }

    private string _bondedDownloadSpeed = "0.00 KB/s";
    public string BondedDownloadSpeed
    {
        get => _bondedDownloadSpeed;
        set => SetField(ref _bondedDownloadSpeed, value);
    }

    private string _bondedUploadSpeed = "0.00 KB/s";
    public string BondedUploadSpeed
    {
        get => _bondedUploadSpeed;
        set => SetField(ref _bondedUploadSpeed, value);
    }

    private int _activeStreamsCount;
    public int ActiveStreamsCount
    {
        get => _activeStreamsCount;
        set => SetField(ref _activeStreamsCount, value);
    }

    private int _totalStreamsCount;
    public int TotalStreamsCount
    {
        get => _totalStreamsCount;
        set => SetField(ref _totalStreamsCount, value);
    }

    private string _sessionVolume = "0 B";
    public string SessionVolume
    {
        get => _sessionVolume;
        set => SetField(ref _sessionVolume, value);
    }

    private string _uptime = "00:00:00";
    public string Uptime
    {
        get => _uptime;
        set => SetField(ref _uptime, value);
    }

    private PointCollection _waveformPointsDownload = new();
    public PointCollection WaveformPointsDownload
    {
        get => _waveformPointsDownload;
        set => SetField(ref _waveformPointsDownload, value);
    }

    private PointCollection _waveformPointsUpload = new();
    public PointCollection WaveformPointsUpload
    {
        get => _waveformPointsUpload;
        set => SetField(ref _waveformPointsUpload, value);
    }

    private string _peakSpeedFormatted = "Peak: 0.00 KB/s";
    public string PeakSpeedFormatted
    {
        get => _peakSpeedFormatted;
        set => SetField(ref _peakSpeedFormatted, value);
    }

    public ProxyConfigModel Config { get; } = new();

    public ICommand TogglePowerCommand { get; }
    public ICommand ToggleWindowsProxyCommand { get; }
    public ICommand ToggleVirtualBridgeCommand { get; }
    public ICommand OpenNetworkConnectionsCommand { get; }
    public ICommand ReprovisionBridgeCommand { get; }
    public ICommand RefreshAdaptersCommand { get; }
    public ICommand ClearLogsCommand { get; }

    public SpeedTester SpeedTesterService => _speedTester;
    public LoadBalancerEngine LoadBalancerService => _loadBalancer;

    public MainViewModel()
    {
        _healthMonitor = new HealthMonitor(() => _networkDetector.GetAllCached());
        _proxyEngine = new ProxyEngine(_loadBalancer, () => _networkDetector.GetAllCached());

        TogglePowerCommand = new RelayCommand(async () => await TogglePowerAsync());
        ToggleWindowsProxyCommand = new RelayCommand(ToggleWindowsProxy);
        ToggleVirtualBridgeCommand = new RelayCommand(async () => await ToggleVirtualBridgeAsync());
        OpenNetworkConnectionsCommand = new RelayCommand(VirtualBridgeManager.OpenWindowsNetworkConnections);
        ReprovisionBridgeCommand = new RelayCommand(async () => await ReprovisionBridgeAsync());
        RefreshAdaptersCommand = new RelayCommand(RefreshAdapters);
        ClearLogsCommand = new RelayCommand(() => AuditLogs.Clear());

        _proxyEngine.OnAuditLog += log =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                AuditLogs.Insert(0, log);
                while (AuditLogs.Count > 100) AuditLogs.RemoveAt(AuditLogs.Count - 1);
            });
        };

        _virtualBridgeManager.OnAuditLog += log =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                AuditLogs.Insert(0, log);
                while (AuditLogs.Count > 100) AuditLogs.RemoveAt(AuditLogs.Count - 1);
            });
        };

        _healthMonitor.HealthUpdated += updated =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                // Trigger UI refresh for pings and status
                foreach (var a in Adapters)
                {
                    var fresh = updated.FirstOrDefault(u => u.Id == a.Id);
                    if (fresh != null)
                    {
                        a.PingMs = fresh.PingMs;
                        a.Status = fresh.Status;
                        a.WanIp = fresh.WanIp;
                    }
                }
            });
        };

        // UI 1-second update timer
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uiTimer.Tick += (s, e) => UpdateTelemetryUI();
        _uiTimer.Start();

        // Initial scan
        RefreshAdapters();
        _healthMonitor.Start(3500);

        // Check if Windows Proxy is currently active in registry
        IsWindowsProxyActive = _proxyManager.GetSystemProxyStatus(out _);
    }

    public void RefreshAdapters()
    {
        var detected = _networkDetector.DetectAdapters();
        Adapters.Clear();
        foreach (var a in detected)
        {
            a.WeightChanged += OnAdapterWeightChanged;
            Adapters.Add(a);
        }

        NetworkDetector.RecalculateEffectivePercentages(Adapters);
        UpdateMergedSummary();
    }

    private void OnAdapterWeightChanged()
    {
        NetworkDetector.RecalculateEffectivePercentages(Adapters);
        UpdateMergedSummary();
    }

    private void UpdateMergedSummary()
    {
        var enabled = Adapters.Where(a => a.IsEnabled && a.Weight > 0).ToList();
        if (enabled.Count == 0)
        {
            VirtualBridge.MergedAdaptersSummary = "No Physical Adapters Merged (Enable adapters & set % weight > 0)";
        }
        else
        {
            string parts = string.Join(" + ", enabled.Select(a => $"{a.Name} ({a.EffectivePercentage:F0}%)"));
            VirtualBridge.MergedAdaptersSummary = $"{enabled.Count} Adapters Merged: {parts}";
        }
    }

    public async Task TogglePowerAsync()
    {
        if (IsEngineRunning)
        {
            if (IsWindowsProxyActive)
            {
                _proxyManager.Disable();
                IsWindowsProxyActive = false;
            }

            if (VirtualBridge.IsConnected)
            {
                await _virtualBridgeManager.StopBridgeAsync();
            }

            await _proxyEngine.StopAsync();
            IsEngineRunning = false;
        }
        else
        {
            _loadBalancer.Mode = Config.RoutingMode;
            _loadBalancer.PrimaryAdapterId = Config.PrimaryAdapterId;

            await _proxyEngine.StartAsync(Config.HttpPort, Config.SocksPort);
            IsEngineRunning = true;

            if (Config.EnableVirtualBridge)
            {
                await _virtualBridgeManager.StartBridgeAsync(Config.VirtualBridgePort, _loadBalancer, () => _networkDetector.GetAllCached());
            }

            if (Config.EnableWindowsProxy)
            {
                _proxyManager.Enable(Config.HttpPort, Config.SocksPort);
                IsWindowsProxyActive = true;
            }
        }
    }

    public async Task ToggleVirtualBridgeAsync()
    {
        if (VirtualBridge.IsConnected)
        {
            await _virtualBridgeManager.StopBridgeAsync();
        }
        else
        {
            await _virtualBridgeManager.StartBridgeAsync(Config.VirtualBridgePort, _loadBalancer, () => _networkDetector.GetAllCached());
        }
    }

    public async Task ReprovisionBridgeAsync()
    {
        await _virtualBridgeManager.EnsureProfileCreatedAsync();
    }

    public void ToggleWindowsProxy()
    {
        if (IsWindowsProxyActive)
        {
            _proxyManager.Disable();
            IsWindowsProxyActive = false;
        }
        else
        {
            bool ok = _proxyManager.Enable(Config.HttpPort, Config.SocksPort);
            IsWindowsProxyActive = ok;
        }
    }

    public void OnApplicationExit()
    {
        if (_proxyManager.IsApplied)
        {
            _proxyManager.Disable();
        }
        _virtualBridgeManager.Dispose();
        _healthMonitor.Stop();
        _ = _proxyEngine.StopAsync();
    }

    private void UpdateTelemetryUI()
    {
        long currentBridgeRx = VirtualBridge.TotalRxBytes;
        long currentBridgeTx = VirtualBridge.TotalTxBytes;
        VirtualBridge.DownloadSpeedBps = Math.Max(0, currentBridgeRx - _prevBridgeRx);
        VirtualBridge.UploadSpeedBps = Math.Max(0, currentBridgeTx - _prevBridgeTx);
        _prevBridgeRx = currentBridgeRx;
        _prevBridgeTx = currentBridgeTx;

        BondedDownloadSpeed = NetworkAdapterModel.FormatSpeed(_proxyEngine.InstantDownloadSpeedBps + VirtualBridge.DownloadSpeedBps);
        BondedUploadSpeed = NetworkAdapterModel.FormatSpeed(_proxyEngine.InstantUploadSpeedBps + VirtualBridge.UploadSpeedBps);
        ActiveStreamsCount = _proxyEngine.ActiveConnections + VirtualBridge.ActiveStreams;
        TotalStreamsCount = _proxyEngine.TotalConnections;
        SessionVolume = NetworkAdapterModel.FormatBytes(_proxyEngine.TotalDownloadBytes + _proxyEngine.TotalUploadBytes + VirtualBridge.TotalRxBytes + VirtualBridge.TotalTxBytes);

        if (_proxyEngine.StartedAt.HasValue && IsEngineRunning)
        {
            var elapsed = DateTime.Now - _proxyEngine.StartedAt.Value;
            Uptime = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
        else
        {
            Uptime = "00:00:00";
        }

        // Update Waveform Canvas
        _downloadHistory.RemoveAt(0);
        _downloadHistory.Add(_proxyEngine.InstantDownloadSpeedBps);

        _uploadHistory.RemoveAt(0);
        _uploadHistory.Add(_proxyEngine.InstantUploadSpeedBps);

        UpdateWaveformGeometry();
    }

    private void UpdateWaveformGeometry()
    {
        double maxSpeed = Math.Max(512 * 1024, Math.Max(_downloadHistory.Max(), _uploadHistory.Max()) * 1.25);
        PeakSpeedFormatted = $"Peak: {NetworkAdapterModel.FormatSpeed((long)(maxSpeed / 1.25))}";

        double width = 800;
        double height = 140;
        double step = width / 59.0;

        var ptsDown = new PointCollection();
        var ptsUp = new PointCollection();

        for (int i = 0; i < 60; i++)
        {
            double x = i * step;
            double yDown = height - (Math.Min(1.0, _downloadHistory[i] / maxSpeed) * height);
            double yUp = height - (Math.Min(1.0, _uploadHistory[i] / maxSpeed) * height);

            ptsDown.Add(new Point(x, yDown));
            ptsUp.Add(new Point(x, yUp));
        }

        WaveformPointsDownload = ptsDown;
        WaveformPointsUpload = ptsUp;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
