using CombiSpeed.Views;
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
    private readonly ConfigurationStore _configStore = new();
    private readonly HealthMonitor _healthMonitor;
    private readonly ProxyEngine _proxyEngine;

    private readonly DispatcherTimer _uiTimer;
    private readonly List<double> _downloadHistory = new(new double[60]);
    private readonly List<double> _uploadHistory = new(new double[60]);
    private long _prevBridgeRx;
    private long _prevBridgeTx;

    public ObservableCollection<NetworkAdapterModel> Adapters { get; } = new();
    public ObservableCollection<AuditLogModel> AuditLogs { get; } = new();
    public ObservableCollection<string> SavedProfiles { get; } = new();

    public VirtualBridgeModel VirtualBridge => _virtualBridgeManager.Model;

    private string _activeProfileName = "Default Combination";
    public string ActiveProfileName
    {
        get => _activeProfileName;
        set
        {
            if (SetField(ref _activeProfileName, value))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _configStore.ApplyProfile(value, Adapters, Config);
                    NetworkDetector.RecalculateEffectivePercentages(Adapters);
                    UpdateMergedSummary();
                    AutoSaveStatusText = $"Loaded profile '{value}'";
                }
            }
        }
    }

    private string _autoSaveStatusText = "Settings Synced";
    public string AutoSaveStatusText
    {
        get => _autoSaveStatusText;
        set => SetField(ref _autoSaveStatusText, value);
    }

    public string AppVersion
    {
        get
        {
            var ver = typeof(MainViewModel).Assembly.GetName().Version;
            return ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.3.5";
        }
    }

    public string AppVersionFull
    {
        get
        {
            var ver = typeof(MainViewModel).Assembly.GetName().Version;
            var verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.3.5";
            return $"{verStr} (Build 10)";
        }
    }

    public string WindowTitle => $"CombiSpeed {AppVersion} - Multi-WAN Channel Bonding & Bandwidth Aggregator";

    public ObservableCollection<string> ThrottlePresets { get; } = new()
    {
        "Unlimited (Proportional)",
        "16 Mbps (2 MB/s)",
        "40 Mbps (5 MB/s)",
        "80 Mbps (10 MB/s)",
        "160 Mbps (20 MB/s)",
        "250 Mbps (31.25 MB/s)",
        "400 Mbps (50 MB/s)",
        "500 Mbps (62.5 MB/s)",
        "Custom..."
    };

    public ICommand OpenCustomThrottleCommand => new RelayCommand(OpenCustomThrottleDialog);

    private string _selectedThrottlePreset = "Unlimited (Proportional)";
    public string SelectedThrottlePreset
    {
        get => _selectedThrottlePreset;
        set
        {
            if (SetField(ref _selectedThrottlePreset, value))
            {
                ApplyThrottlePreset(value);
            }
        }
    }

    private string _throttleStatusText = "Throttling: Proportional % Shaping";
    public string ThrottleStatusText
    {
        get => _throttleStatusText;
        set => SetField(ref _throttleStatusText, value);
    }

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
    public ICommand SaveCurrentSettingsCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

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
        SaveCurrentSettingsCommand = new RelayCommand(SaveCurrentCombinationImmediate);
        SaveProfileCommand = new RelayCommand(SaveCurrentCombinationImmediate);
        DeleteProfileCommand = new RelayCommand(DeleteCurrentProfile);

        // Restore saved Config
        if (_configStore.Data.Config != null)
        {
            Config.HttpPort = _configStore.Data.Config.HttpPort;
            Config.SocksPort = _configStore.Data.Config.SocksPort;
            Config.VirtualBridgePort = _configStore.Data.Config.VirtualBridgePort;
            Config.RoutingMode = _configStore.Data.Config.RoutingMode;
            Config.EnableVirtualBridge = _configStore.Data.Config.EnableVirtualBridge;
            Config.EnableWindowsProxy = _configStore.Data.Config.EnableWindowsProxy;
            Config.EnablePac = _configStore.Data.Config.EnablePac;
            Config.PacUrl = _configStore.Data.Config.PacUrl;
            Config.PrimaryAdapterId = _configStore.Data.Config.PrimaryAdapterId;
        }

        // Restore profile list
        var profiles = _configStore.GetProfileNames();
        SavedProfiles.Clear();
        foreach (var p in profiles)
        {
            SavedProfiles.Add(p);
        }
        if (!string.IsNullOrWhiteSpace(_configStore.Data.ActiveProfileName) && SavedProfiles.Contains(_configStore.Data.ActiveProfileName))
        {
            _activeProfileName = _configStore.Data.ActiveProfileName;
        }
        else
        {
            _activeProfileName = SavedProfiles.FirstOrDefault() ?? "Default Combination";
        }

        _configStore.OnSaved += () =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                AutoSaveStatusText = $"Auto-Saved: {DateTime.Now:T}";
            });
        };

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

        // Configure initial bandwidth throttling state
        SyncSelectedThrottlePreset();
        UpdateThrottlingState();

        // Check if Windows Proxy is currently active in registry
        IsWindowsProxyActive = _proxyManager.GetSystemProxyStatus(out _);
    }

    public void SaveCurrentCombinationImmediate()
    {
        _configStore.SaveImmediate(Adapters, Config, ActiveProfileName);
        AutoSaveStatusText = $"Saved: {DateTime.Now:T}";
    }

    public void CreateNewProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return;
        _configStore.SaveCombinationProfile(profileName, Adapters, Config.RoutingMode);
        if (!SavedProfiles.Contains(profileName))
        {
            SavedProfiles.Add(profileName);
        }
        ActiveProfileName = profileName;
        AutoSaveStatusText = $"Profile '{profileName}' saved";
    }

    public void DeleteCurrentProfile()
    {
        if (ActiveProfileName == "Default Combination") return;
        var toRemove = ActiveProfileName;
        _configStore.Data.SavedProfiles.RemoveAll(p => p.ProfileName.Equals(toRemove, StringComparison.OrdinalIgnoreCase));
        SavedProfiles.Remove(toRemove);
        ActiveProfileName = "Default Combination";
        SaveCurrentCombinationImmediate();
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

        // Apply saved adapter settings & weights
        _configStore.ApplyToAdapters(Adapters);

        NetworkDetector.RecalculateEffectivePercentages(Adapters);
        UpdateMergedSummary();
        _proxyEngine.UpdateThrottling(Adapters, Config);
    }

    private void OnAdapterWeightChanged()
    {
        NetworkDetector.RecalculateEffectivePercentages(Adapters);
        UpdateMergedSummary();
        _proxyEngine.UpdateThrottling(Adapters, Config);
        _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
        AutoSaveStatusText = "Saving changes...";
    }

    private void ApplyThrottlePreset(string preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;

        if (preset == "Custom...")
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(OpenCustomThrottleDialog));
            return;
        }

        if (preset.StartsWith("Custom:"))
        {
            return;
        }

        long limitBps = preset switch
        {
            "16 Mbps (2 MB/s)" or "2 MB/s (16 Mbps)" => 2L * 1024 * 1024,
            "40 Mbps (5 MB/s)" or "5 MB/s (40 Mbps)" => 5L * 1024 * 1024,
            "80 Mbps (10 MB/s)" or "10 MB/s (80 Mbps)" => 10L * 1024 * 1024,
            "160 Mbps (20 MB/s)" or "20 MB/s (160 Mbps)" => 20L * 1024 * 1024,
            "250 Mbps (31.25 MB/s)" => (long)(250 * 125_000.0),
            "400 Mbps (50 MB/s)" or "50 MB/s (400 Mbps)" => 50L * 1024 * 1024,
            "500 Mbps (62.5 MB/s)" or "62.5 MB/s (500 Mbps)" => (long)(500 * 125_000.0),
            "100 MB/s (800 Mbps)" => 100L * 1024 * 1024,
            _ => 0L
        };

        Config.GlobalDownloadLimitBps = limitBps;
        Config.GlobalUploadLimitBps = limitBps > 0 ? (limitBps / 2) : 0L;
        Config.EnableBandwidthThrottling = limitBps > 0 || Config.EnforceProportionalThrottling;
        UpdateThrottlingState();
    }

    public void OpenCustomThrottleDialog()
    {
        var dialog = new CustomThrottleDialog(Config, Adapters)
        {
            Owner = Application.Current?.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            UpdateThrottlingState();
            SyncSelectedThrottlePreset();
        }
        else
        {
            SyncSelectedThrottlePreset();
        }
    }

    public void SyncSelectedThrottlePreset()
    {
        if (Config.GlobalDownloadLimitBps <= 0)
        {
            _selectedThrottlePreset = "Unlimited (Proportional)";
            OnPropertyChanged(nameof(SelectedThrottlePreset));
            return;
        }

        // Check if matches an existing preset
        string? match = null;
        double targetMbps = (Config.GlobalDownloadLimitBps * 8.0) / 1_000_000.0;
        foreach (var p in ThrottlePresets)
        {
            if (p.StartsWith("Unlimited") || p == "Custom..." || p.StartsWith("Custom:")) continue;
            var matchRegex = System.Text.RegularExpressions.Regex.Match(p, @"(\d+(\.\d+)?)\s*Mbps");
            if (matchRegex.Success && double.TryParse(matchRegex.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mbps))
            {
                if (Math.Abs(mbps - targetMbps) < 2.0)
                {
                    match = p;
                    break;
                }
            }
        }

        if (match != null)
        {
            _selectedThrottlePreset = match;
        }
        else
        {
            string customLabel = $"Custom: {NetworkAdapterModel.FormatSpeedWithBits(Config.GlobalDownloadLimitBps)}";
            var existingCustom = ThrottlePresets.FirstOrDefault(p => p.StartsWith("Custom:"));
            if (existingCustom != null)
            {
                ThrottlePresets.Remove(existingCustom);
            }
            int customIndex = ThrottlePresets.IndexOf("Custom...");
            if (customIndex >= 0)
            {
                ThrottlePresets.Insert(customIndex, customLabel);
            }
            else
            {
                ThrottlePresets.Add(customLabel);
            }
            _selectedThrottlePreset = customLabel;
        }
        OnPropertyChanged(nameof(SelectedThrottlePreset));
    }

    public void UpdateThrottlingState()
    {
        _proxyEngine.UpdateThrottling(Adapters, Config);
        if (_virtualBridgeManager.Server != null)
        {
            _virtualBridgeManager.Server.Throttler.UpdateConfiguration(Adapters, Config);
        }

        if (Config.GlobalDownloadLimitBps > 0)
        {
            ThrottleStatusText = $"Throttled: {NetworkAdapterModel.FormatSpeedWithBits(Config.GlobalDownloadLimitBps)} Cap";
        }
        else if (Config.EnableBandwidthThrottling)
        {
            ThrottleStatusText = "Throttling: Proportional % Shaping";
        }
        else
        {
            ThrottleStatusText = "Throttling: Disabled";
        }
        _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
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
                _proxyManager.DisablePac();
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
            _proxyEngine.UpdateThrottling(Adapters, Config);

            await _proxyEngine.StartAsync(Config.HttpPort, Config.SocksPort);
            IsEngineRunning = true;

            if (Config.EnableVirtualBridge)
            {
                await _virtualBridgeManager.StartBridgeAsync(Config.VirtualBridgePort, _loadBalancer, () => _networkDetector.GetAllCached(), _proxyEngine.Throttler);
            }

            if (Config.EnablePac)
            {
                _proxyManager.EnablePac(Config.PacUrl);
                IsWindowsProxyActive = true;
            }
            else if (Config.EnableWindowsProxy)
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
            _proxyEngine.UpdateThrottling(Adapters, Config);
            await _virtualBridgeManager.StartBridgeAsync(Config.VirtualBridgePort, _loadBalancer, () => _networkDetector.GetAllCached(), _proxyEngine.Throttler);
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
        try
        {
            _configStore.SaveImmediate(Adapters, Config, ActiveProfileName);
        }
        catch { }

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
