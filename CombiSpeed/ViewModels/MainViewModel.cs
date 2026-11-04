using CombiSpeed.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
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

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter is T t ? t : default);
    public void Execute(object? parameter) => _execute(parameter is T t ? t : default);
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
    private readonly ProxyHarvesterService _proxyHarvester = new();
    private readonly ProxyValidatorEngine _proxyValidator = new();
    private readonly RotatingProxyRouter _rotatingRouter;
    private readonly RotatingProxyServer _rotatingProxyServer;
    private readonly ProxySchedulerService _proxyScheduler;

    private readonly DispatcherTimer _uiTimer;
    private readonly List<double> _downloadHistory = new(new double[60]);
    private readonly List<double> _uploadHistory = new(new double[60]);
    private long _prevBridgeRx;
    private long _prevBridgeTx;

    public ObservableCollection<NetworkAdapterModel> Adapters { get; } = new();
    public ObservableCollection<AuditLogModel> AuditLogs { get; } = new();
    public ObservableCollection<string> SavedProfiles { get; } = new();

    public string CombinableAdaptersCountText => $"{Adapters.Count} Detected ({Adapters.Count(a => a.IsEnabled)} Combined)";
    public bool HasNoAdapters => Adapters.Count == 0;

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
            return ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.2.4";
        }
    }

    public string AppVersionFull
    {
        get
        {
            var ver = typeof(MainViewModel).Assembly.GetName().Version;
            var verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.2.4";
            return $"{verStr} (Build 31)";
        }
    }

    public string WindowTitle => $"CombiSpeed {AppVersion} - Multi-WAN Channel Bonding & Bandwidth Aggregator";

    // Accessibility Properties (WCAG 2.1 AA)
    private bool _isHighContrast;
    public bool IsHighContrast
    {
        get => _isHighContrast;
        set
        {
            if (SetField(ref _isHighContrast, value))
            {
                ApplyHighContrastTheme(value);
            }
        }
    }

    private double _uiScale = 1.0;
    public double UiScale
    {
        get => _uiScale;
        set
        {
            double clamped = Math.Clamp(value, 0.85, 1.5);
            SetField(ref _uiScale, clamped);
        }
    }

    private bool _isReduceMotion;
    public bool IsReduceMotion
    {
        get => _isReduceMotion;
        set => SetField(ref _isReduceMotion, value);
    }

    private bool _isScreenReaderOptimized;
    public bool IsScreenReaderOptimized
    {
        get => _isScreenReaderOptimized;
        set => SetField(ref _isScreenReaderOptimized, value);
    }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetField(ref _selectedTabIndex, value);
    }

    public ICommand OpenAccessibilityCommand => new RelayCommand(OpenAccessibilityDialog);
    public ICommand ToggleHighContrastCommand => new RelayCommand(() => IsHighContrast = !IsHighContrast);
    public ICommand SelectTabCommand => new RelayCommand<object>(param =>
    {
        if (param != null && int.TryParse(param.ToString(), out int idx))
        {
            SelectedTabIndex = idx;
        }
    });

    public ObservableCollection<string> ThrottlePresets { get; } = new()
    {
        "Unlimited (Combined Speed)",
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

    public ObservableCollection<string> ShaperModes { get; } = new()
    {
        "🚀 Maximum Bandwidth",
        "⚡ Lowest Latency / Ping",
        "⚖ Balanced Dynamic"
    };

    private string _selectedShaperMode = "🚀 Maximum Bandwidth";
    public string SelectedShaperMode
    {
        get => _selectedShaperMode;
        set
        {
            if (SetField(ref _selectedShaperMode, value))
            {
                Config.ShaperMode = value switch
                {
                    "⚡ Lowest Latency / Ping" or "Lowest Latency" => SpeedShaperMode.LowestLatency,
                    "⚖ Balanced Dynamic" or "Balanced Dynamic" => SpeedShaperMode.BalancedDynamic,
                    _ => SpeedShaperMode.MaximumBandwidth
                };
                _loadBalancer.ShaperMode = Config.ShaperMode;
                UpdateThrottlingState();
            }
        }
    }

    private string _selectedThrottlePreset = "Unlimited (Combined Speed)";
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

    private string _throttleStatusText = "Bandwidth: Combined Speed Aggregation";
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

    // Rotating Proxy & Harvester Commands
    public ICommand HarvestProxiesCommand { get; }
    public ICommand ValidatePoolCommand { get; }
    public ICommand PruneDeadProxiesCommand { get; }
    public ICommand ClearProxiesCommand { get; }
    public ICommand ExportWorkingProxiesCommand { get; }
    public ICommand ImportProxiesFileCommand { get; }
    public ICommand ToggleRotatingProxyCommand { get; }
    public ICommand SelectAllProxiesCommand { get; }
    public ICommand DeselectAllProxiesCommand { get; }
    public ICommand SelectFastProxiesCommand { get; }
    public ICommand ForceRotateCommand { get; }
    public ICommand ForceConnectSelectedOrNextCommand { get; }
    public ICommand RefreshGeoIpCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand DisconnectProxyCommand { get; }
    public ICommand ToggleAutoRotateCommand { get; }

    public SpeedTester SpeedTesterService => _speedTester;
    public LoadBalancerEngine LoadBalancerService => _loadBalancer;
    public WindowsProxyManager ProxyManager => _proxyManager;

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

        // Rotating Proxy Commands
        HarvestProxiesCommand = new RelayCommand(async () => await HarvestProxiesAsync());
        ValidatePoolCommand = new RelayCommand(async () => await ValidatePoolAsync());
        PruneDeadProxiesCommand = new RelayCommand(PruneDeadProxies);
        ClearProxiesCommand = new RelayCommand(ClearProxies);
        ExportWorkingProxiesCommand = new RelayCommand(async () => await ExportWorkingProxiesAsync());
        ImportProxiesFileCommand = new RelayCommand(async () => await ImportProxiesFileAsync());
        ToggleRotatingProxyCommand = new RelayCommand(ToggleRotatingProxy);
        SelectAllProxiesCommand = new RelayCommand(SelectAllProxies);
        DeselectAllProxiesCommand = new RelayCommand(DeselectAllProxies);
        SelectFastProxiesCommand = new RelayCommand(SelectFastProxies);
        ForceRotateCommand = new RelayCommand(async () => await ForceRotateProxyAsync());
        ForceConnectSelectedOrNextCommand = new RelayCommand<HarvestedProxyModel>(async (proxy) => await ForceConnectSelectedOrNextAsync(proxy));
        RefreshGeoIpCommand = new RelayCommand(async () => await ResolveGeoIpAsync(forceRefresh: true));
        ToggleThemeCommand = new RelayCommand(() => SelectedTheme = SelectedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light");
        DisconnectProxyCommand = new RelayCommand(DisconnectCurrentProxy);
        ToggleAutoRotateCommand = new RelayCommand(() => AutoRotateEnabled = !AutoRotateEnabled);

        _rotatingRouter = new RotatingProxyRouter(_proxyValidator, Config.RotatingProxy, _loadBalancer);
        _proxyEngine.RotatingRouter = _rotatingRouter;
        _proxyEngine.CurrentConfig = Config;
        _rotatingProxyServer = new RotatingProxyServer(Config.RotatingProxy, _rotatingRouter, () => _networkDetector.GetAllCached());
        _proxyScheduler = new ProxySchedulerService(_proxyHarvester, _proxyValidator, Config.RotatingProxy);

        _rotatingRouter.OnTimedRotation += (proxy) =>
        {
            Application.Current?.Dispatcher.InvokeAsync(async () =>
            {
                if (proxy != null)
                {
                    ActiveProxyDisplay = $"{proxy.Endpoint} ({proxy.CountryCode}) {proxy.FlagEmoji}";
                    await ResolveGeoIpAsync(forceRefresh: true, proxy: proxy);
                    RotationCountdownText = $"Next rotation in {FormatRemainingTime(_rotatingRouter.SecondsUntilNextRotation)}";
                }
                else
                {
                    ActiveProxyDisplay = "None Selected / Available";
                    RotationCountdownText = "⏳ Waiting for live proxy connection...";
                }
            });
        };

        _rotatingProxyServer.OnLog += log =>
        {
            if (!Config.EnableAuditLogging) return;
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                AuditLogs.Insert(0, new AuditLogModel { Protocol = "ROTATOR", TargetHost = log, Status = "OK", Timestamp = DateTime.Now });
                while (AuditLogs.Count > 100) AuditLogs.RemoveAt(AuditLogs.Count - 1);
            });
        };

        _proxyScheduler.OnLog += msg =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                HarvestStatusText = msg;
            });
        };

        _proxyScheduler.OnPoolUpdated += () =>
        {
            RefreshProxyCollection();
        };

        _proxyValidator.OnLog += msg =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                HarvestStatusText = msg;
            });
        };

        _proxyHarvester.OnLog += msg =>
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                HarvestStatusText = msg;
            });
        };

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
            Config.IsHighContrast = _configStore.Data.Config.IsHighContrast;
            Config.UiScale = _configStore.Data.Config.UiScale > 0 ? _configStore.Data.Config.UiScale : 1.0;
            Config.IsReduceMotion = _configStore.Data.Config.IsReduceMotion;
            Config.IsScreenReaderOptimized = _configStore.Data.Config.IsScreenReaderOptimized;

            if (_configStore.Data.Config.RotatingProxy != null)
            {
                var r = _configStore.Data.Config.RotatingProxy;
                Config.RotatingProxy.EnableRotatingProxy = r.EnableRotatingProxy;
                Config.RotatingProxy.RotatingProxyPort = r.RotatingProxyPort;
                Config.RotatingProxy.Strategy = r.Strategy;
                Config.RotatingProxy.ChainPhysicalAdapters = r.ChainPhysicalAdapters;
                Config.RotatingProxy.StrictAnonymity = r.StrictAnonymity;
                Config.RotatingProxy.AutoHarvestEnabled = r.AutoHarvestEnabled;
                Config.RotatingProxy.AutoHarvestIntervalMinutes = r.AutoHarvestIntervalMinutes;
                Config.RotatingProxy.AutoValidateEnabled = r.AutoValidateEnabled;
                Config.RotatingProxy.AutoValidateIntervalMinutes = r.AutoValidateIntervalMinutes;
                Config.RotatingProxy.CountryFilter = r.CountryFilter ?? "ALL";
                if (r.CustomSources != null && r.CustomSources.Count > 0)
                {
                    Config.RotatingProxy.CustomSources = r.CustomSources;
                }
            }

            if (Config.RotatingProxy.EnableRotatingProxy)
            {
                _rotatingProxyServer.Start();
                _proxyScheduler.Start();
                IsRotatingProxyRunning = _rotatingProxyServer.IsRunning;
            }

            _isHighContrast = Config.IsHighContrast;
            _uiScale = Config.UiScale;
            _isReduceMotion = Config.IsReduceMotion;
            _isScreenReaderOptimized = Config.IsScreenReaderOptimized;
            Config.EnableAuditLogging = _configStore.Data.Config.EnableAuditLogging;
            Config.SelectedTheme = !string.IsNullOrEmpty(_configStore.Data.Config.SelectedTheme) ? _configStore.Data.Config.SelectedTheme : "Dark";
            _selectedTheme = Config.SelectedTheme;
            ApplyTheme(_selectedTheme);
            if (_isHighContrast)
            {
                ApplyHighContrastTheme(true);
            }
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
            if (!Config.EnableAuditLogging) return;
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                AuditLogs.Insert(0, log);
                while (AuditLogs.Count > 100) AuditLogs.RemoveAt(AuditLogs.Count - 1);
            });
        };

        _virtualBridgeManager.OnAuditLog += log =>
        {
            if (!Config.EnableAuditLogging) return;
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
        _healthMonitor.HealthUpdated += _ =>
        {
            if (Config.EnableDynamicShaping)
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    _proxyEngine.Throttler.AdaptDynamically(Adapters, Config);
                    _virtualBridgeManager.Server?.Throttler.AdaptDynamically(Adapters, Config);
                    UpdateMergedSummary();
                }));
            }
        };

        // Configure initial bandwidth throttling state
        SyncSelectedShaperMode();
        SyncSelectedThrottlePreset();
        UpdateThrottlingState();

        // Restore persistent proxy repository sources
        string sourcesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CombiSpeed", "proxy_sources.json");
        if (File.Exists(sourcesPath))
        {
            try
            {
                var loadedSources = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(sourcesPath));
                if (loadedSources != null && loadedSources.Count > 0)
                {
                    Config.RotatingProxy.CustomSources = loadedSources.Distinct().ToList();
                }
            }
            catch { }
        }

        // Check if Windows Proxy is currently active in registry
        IsWindowsProxyActive = _proxyManager.GetSystemProxyStatus(out _);

        // Restore persistent proxy state (harvested proxies, selections, filters, and sorting) in Idempotence mode
        var savedProxyState = _proxyValidator.LoadPersistedState();
        if (savedProxyState != null)
        {
            if (!string.IsNullOrEmpty(savedProxyState.SelectedLatencyFilter)) _selectedLatencyFilter = savedProxyState.SelectedLatencyFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedSourceFilter)) _selectedSourceFilter = savedProxyState.SelectedSourceFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedProtocolFilter)) _selectedProtocolFilter = savedProxyState.SelectedProtocolFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedCountryFilter)) Config.RotatingProxy.CountryFilter = savedProxyState.SelectedCountryFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedSuccessFilter)) _selectedSuccessFilter = savedProxyState.SelectedSuccessFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedFailureFilter)) _selectedFailureFilter = savedProxyState.SelectedFailureFilter;
            if (!string.IsNullOrEmpty(savedProxyState.SelectedSortOption)) _selectedSortOption = savedProxyState.SelectedSortOption;
            if (!string.IsNullOrEmpty(savedProxyState.ProxySearchText)) _proxySearchText = savedProxyState.ProxySearchText;
            if (savedProxyState.RotationIntervalSeconds > 0) Config.RotatingProxy.RotationIntervalSeconds = savedProxyState.RotationIntervalSeconds;
            Config.RotatingProxy.RouteBondedTrafficThroughRotator = savedProxyState.RouteBondedTrafficThroughRotator;
            if ((Config.RotatingProxy.CustomSources == null || Config.RotatingProxy.CustomSources.Count == 0) && savedProxyState.RepositorySources != null && savedProxyState.RepositorySources.Count > 0)
            {
                Config.RotatingProxy.CustomSources = savedProxyState.RepositorySources.Distinct().ToList();
            }

            HarvestStatusText = $"Loaded {savedProxyState.Proxies?.Count ?? _proxyValidator.TotalCount} proxies and active filter/sort state from persistent disk storage.";
            RefreshProxyCollection();
        }
        else
        {
            int prewarmed = _proxyValidator.LoadHealthyProxies();
            if (prewarmed > 0)
            {
                HarvestStatusText = $"Loaded {prewarmed} verified healthy proxies from persistent disk storage.";
                RefreshProxyCollection();
            }
        }
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
        _ = ResolveGeoIpAsync();
        _healthMonitor.TriggerImmediateCheck();

        OnPropertyChanged(nameof(CombinableAdaptersCountText));
        OnPropertyChanged(nameof(HasNoAdapters));

        if (Config.EnableAuditLogging)
        {
            int enabledCount = Adapters.Count(a => a.IsEnabled);
            AuditLogs.Insert(0, new AuditLogModel
            {
                Protocol = "NIC-SCAN",
                TargetHost = "All Adapters",
                Status = $"{Adapters.Count} detected ({enabledCount} combined)",
                Timestamp = DateTime.Now
            });
            while (AuditLogs.Count > 100) AuditLogs.RemoveAt(AuditLogs.Count - 1);
        }
    }

    private void OnAdapterWeightChanged()
    {
        NetworkDetector.RecalculateEffectivePercentages(Adapters);
        UpdateMergedSummary();
        _proxyEngine.UpdateThrottling(Adapters, Config);
        _ = ResolveGeoIpAsync();
        _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
        AutoSaveStatusText = "Saving changes...";
        OnPropertyChanged(nameof(CombinableAdaptersCountText));
        OnPropertyChanged(nameof(HasNoAdapters));
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
            _selectedThrottlePreset = "Unlimited (Combined Speed)";
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

    public void SyncSelectedShaperMode()
    {
        _selectedShaperMode = Config.ShaperMode switch
        {
            SpeedShaperMode.LowestLatency => "⚡ Lowest Latency / Ping",
            SpeedShaperMode.BalancedDynamic => "⚖ Balanced Dynamic",
            _ => "🚀 Maximum Bandwidth"
        };
        OnPropertyChanged(nameof(SelectedShaperMode));
    }

    public void UpdateThrottlingState()
    {
        _loadBalancer.ShaperMode = Config.ShaperMode;
        _proxyEngine.UpdateThrottling(Adapters, Config);
        if (_virtualBridgeManager.Server != null)
        {
            _virtualBridgeManager.Server.Throttler.UpdateConfiguration(Adapters, Config);
        }

        string modeTag = Config.ShaperMode switch
        {
            SpeedShaperMode.LowestLatency => "⚡ Lowest Ping",
            SpeedShaperMode.MaximumBandwidth => "🚀 Max Bandwidth",
            _ => "⚖ Balanced"
        };

        if (Config.GlobalDownloadLimitBps > 0)
        {
            ThrottleStatusText = $"Combined Cap: {NetworkAdapterModel.FormatSpeedWithBits(Config.GlobalDownloadLimitBps)} ({modeTag})";
        }
        else if (Config.EnableBandwidthThrottling)
        {
            ThrottleStatusText = $"Bandwidth: Combined Speed Aggregation ({modeTag})";
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
            VirtualBridge.MergedAdaptersSummary = "No Physical Adapters Combined (Enable adapters to combine speeds)";
        }
        else
        {
            string parts = string.Join(" + ", enabled.Select(a => $"{a.Name} ({a.EffectivePercentage:F0}%)"));
            VirtualBridge.MergedAdaptersSummary = $"{enabled.Count} Adapters Combined: {parts}";
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

            _rotatingProxyServer.Stop();
            IsRotatingProxyRunning = false;

            await _proxyEngine.StopAsync();
            IsEngineRunning = false;
        }
        else
        {
            try
            {
                _loadBalancer.Mode = Config.RoutingMode;
                _loadBalancer.PrimaryAdapterId = Config.PrimaryAdapterId;
                _proxyEngine.UpdateThrottling(Adapters, Config);

                await _proxyEngine.StartAsync(Config.HttpPort, Config.SocksPort);
                IsEngineRunning = _proxyEngine.IsRunning;

                if (Config.RotatingProxy.EnableRotatingProxy)
                {
                    _rotatingProxyServer.Start();
                    IsRotatingProxyRunning = _rotatingProxyServer.IsRunning;
                }

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
            catch (Exception ex)
            {
                if (Config.EnableAuditLogging)
                {
                    AuditLogs.Insert(0, new AuditLogModel
                    {
                        Protocol = "ENGINE",
                        TargetHost = "PowerToggle",
                        Status = $"Start Error: {ex.Message}",
                        Timestamp = DateTime.Now
                    });
                }
                IsEngineRunning = _proxyEngine.IsRunning;
            }
        }
    }

    public async Task ToggleVirtualBridgeAsync()
    {
        try
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
        catch (Exception ex)
        {
            if (Config.EnableAuditLogging)
            {
                AuditLogs.Insert(0, new AuditLogModel
                {
                    Protocol = "BRIDGE",
                    TargetHost = $"Port {Config.VirtualBridgePort}",
                    Status = $"Toggle Error: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
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

        if (_rotatingRouter != null)
        {
            if (Config.RotatingProxy.Strategy == RotationStrategy.TimeBasedInterval)
            {
                if (!_rotatingRouter.IsLiveConnected || _rotatingRouter.ActiveTimedProxy == null)
                {
                    RotationCountdownText = "⏳ Waiting for live proxy connection (Direct Multi-WAN)...";
                }
                else if (!AutoRotateEnabled)
                {
                    RotationCountdownText = $"⏸ Auto-Rotation Paused (Connected to {_rotatingRouter.ActiveTimedProxy.Endpoint})";
                }
                else
                {
                    int remaining = _rotatingRouter.SecondsUntilNextRotation;
                    if (remaining > 0)
                    {
                        RotationCountdownText = $"Next rotation in {FormatRemainingTime(remaining)}";
                    }
                    else if (remaining == 0)
                    {
                        RotationCountdownText = "🔄 Rotating to next live proxy...";
                        _ = _rotatingRouter.CheckAutoRotationAsync();
                    }
                    else if (remaining == -2)
                    {
                        RotationCountdownText = $"⏸ Auto-Rotation Paused (Connected to {_rotatingRouter.ActiveTimedProxy.Endpoint})";
                    }
                    else
                    {
                        RotationCountdownText = "⏳ Waiting for live proxy connection...";
                    }
                }
            }
            else
            {
                RotationCountdownText = $"Strategy: {Config.RotatingProxy.Strategy}";
            }
        }
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

        if (IsReduceMotion)
        {
            // Reduce Motion: render smooth steady levels avoiding rapid flicker & vestibular strain
            double avgDown = height - (Math.Min(1.0, (_downloadHistory.Count > 0 ? _downloadHistory.Average() : 0) / maxSpeed) * height);
            double avgUp = height - (Math.Min(1.0, (_uploadHistory.Count > 0 ? _uploadHistory.Average() : 0) / maxSpeed) * height);
            ptsDown.Add(new Point(0, avgDown));
            ptsDown.Add(new Point(width, avgDown));
            ptsUp.Add(new Point(0, avgUp));
            ptsUp.Add(new Point(width, avgUp));
        }
        else
        {
            for (int i = 0; i < 60; i++)
            {
                double x = i * step;
                double yDown = height - (Math.Min(1.0, _downloadHistory[i] / maxSpeed) * height);
                double yUp = height - (Math.Min(1.0, _uploadHistory[i] / maxSpeed) * height);

                ptsDown.Add(new Point(x, yDown));
                ptsUp.Add(new Point(x, yUp));
            }
        }

        WaveformPointsDownload = ptsDown;
        WaveformPointsUpload = ptsUp;
    }

    public void ApplyHighContrastTheme(bool enabled)
    {
        if (Application.Current == null) return;

        var res = Application.Current.Resources;
        if (enabled)
        {
            res["CyberBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#030712"));
            res["CyberSurface"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0F1D"));
            res["CyberCard"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["CyberBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["TextSecondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            res["TextHeader"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));

            res["ButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
            res["ButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["ButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["ButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0F1D"));
            res["ButtonHoverBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["ButtonHoverFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["ButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));

            res["PrimaryButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PrimaryButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
            res["PrimaryButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["PrimaryButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["PrimaryButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));

            res["PowerButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
            res["PowerButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PowerButtonIconFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["PowerButtonActiveBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PowerButtonActiveBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["PowerButtonActiveFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));

            res["ToolTipBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
            res["ToolTipFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["ToolTipBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
        }
        else
        {
            ApplyTheme(_selectedTheme);
        }
    }

    public void ApplyTheme(string theme)
    {
        if (Application.Current == null) return;

        var res = Application.Current.Resources;
        bool isLight = theme.Equals("Light", StringComparison.OrdinalIgnoreCase);

        if (isLight)
        {
            // Soothing, eye-friendly light theme (WCAG 2.1 AAA/AA compliant)
            res["CyberBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["CyberSurface"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["CyberCard"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["CyberBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            res["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["TextSecondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            res["TextHeader"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            // Tooltip Palette: If font colour is dark (#0F172A) then background is strictly LIGHT (#FFFFFF)
            res["ToolTipBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["ToolTipFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["ToolTipBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            res["CyberCyan"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            res["CyberCyanHover"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
            res["TitleBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["StatusBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["DataGridBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["DataGridAltRow"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["BadgeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDF2F7"));
            res["BadgeFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["ProgressBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            res["InputBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["InputFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));

            // Button Colors under Light Theme: Font is dark (#0F172A / #0369A1 / #0284C7) -> Background is strictly LIGHT (#FFFFFF / #E0F2FE / #F1F5F9)!
            res["ButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["ButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["ButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            res["ButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["ButtonHoverBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            res["ButtonHoverFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            res["ButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));

            res["PrimaryButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
            res["PrimaryButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
            res["PrimaryButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            res["PrimaryButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
            res["PrimaryButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7DD3FC"));

            res["PowerButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["PowerButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            res["PowerButtonIconFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            res["PowerButtonActiveBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
            res["PowerButtonActiveBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            res["PowerButtonActiveFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
        }
        else
        {
            // Dark cyberpunk glassmorphic theme
            res["CyberBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080C14"));
            res["CyberSurface"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
            res["CyberCard"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#131D33"));
            res["CyberBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["TextSecondary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            res["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            res["TextHeader"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["ToolTipBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"));
            res["ToolTipFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            res["ToolTipBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["CyberCyan"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["CyberCyanHover"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["TitleBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060910"));
            res["StatusBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060910"));
            res["DataGridBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080C14"));
            res["DataGridAltRow"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"));
            res["BadgeBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["BadgeFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["ProgressBarBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["InputBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#080C14"));
            res["InputFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));

            res["ButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["ButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            res["ButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["ButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["ButtonHoverBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["ButtonHoverFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["ButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));

            res["PrimaryButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PrimaryButtonFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#040810"));
            res["PrimaryButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PrimaryButtonHoverBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["PrimaryButtonPressedBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));

            res["PowerButtonBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            res["PowerButtonBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            res["PowerButtonIconFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            res["PowerButtonActiveBg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00F0FF"));
            res["PowerButtonActiveBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
            res["PowerButtonActiveFg"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#040810"));
        }
    }

    private string _selectedTheme = "Dark";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetField(ref _selectedTheme, value))
            {
                Config.SelectedTheme = value;
                ApplyTheme(value);
                OnPropertyChanged(nameof(ThemeToggleIcon));
                OnPropertyChanged(nameof(ThemeToggleText));
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public string ThemeToggleIcon => SelectedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "🌙" : "☀️";
    public string ThemeToggleText => SelectedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Dark Mode" : "Light Mode";

    public bool EnableAuditLogging
    {
        get => Config.EnableAuditLogging;
        set
        {
            if (Config.EnableAuditLogging != value)
            {
                Config.EnableAuditLogging = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AuditLoggingDisabledVisibility));

                if (_proxyEngine.CurrentConfig != null)
                {
                    _proxyEngine.CurrentConfig.EnableAuditLogging = value;
                }
                if (_virtualBridgeManager != null)
                {
                    _virtualBridgeManager.EnableAuditLogging = value;
                }

                // Instantly save persistently with zero debounce
                SaveCurrentCombinationImmediate();
                AutoSaveStatusText = $"Audit Logging {(value ? "Enabled" : "Disabled")} (Saved)";

                // When disabled, immediately stop doing the audit and clear any recorded audit logs
                if (!value)
                {
                    _proxyEngine.ClearAuditLogs();
                    AuditLogs.Clear();
                }
            }
        }
    }

    public Visibility AuditLoggingDisabledVisibility => EnableAuditLogging ? Visibility.Collapsed : Visibility.Visible;

    public bool AutoRotateEnabled
    {
        get => Config.RotatingProxy.AutoRotateEnabled;
        set
        {
            if (Config.RotatingProxy.AutoRotateEnabled != value)
            {
                Config.RotatingProxy.AutoRotateEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoRotateToggleText));
                OnPropertyChanged(nameof(AutoRotateToggleIcon));
                SaveCurrentCombinationImmediate();
                if (!value && _rotatingRouter?.ActiveTimedProxy != null)
                {
                    RotationCountdownText = $"⏸ Auto-Rotation Paused (Connected to {_rotatingRouter.ActiveTimedProxy.Endpoint})";
                }
                else if (value && _rotatingRouter?.ActiveTimedProxy != null)
                {
                    RotationCountdownText = $"Next rotation in {FormatRemainingTime(_rotatingRouter.SecondsUntilNextRotation)}";
                }
            }
        }
    }

    public string AutoRotateToggleText => AutoRotateEnabled ? "Pause Rotation" : "Resume Rotation";
    public string AutoRotateToggleIcon => AutoRotateEnabled ? "⏸" : "▶";

    public bool CombineAdaptersWithProxy
    {
        get => Config.RotatingProxy.CombineAdaptersWithProxy;
        set
        {
            if (Config.RotatingProxy.CombineAdaptersWithProxy != value)
            {
                Config.RotatingProxy.CombineAdaptersWithProxy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProxyModeDescription));
                SaveCurrentCombinationImmediate();
            }
        }
    }

    public string ProxyModeDescription => CombineAdaptersWithProxy 
        ? "Bonded Multi-WAN + Proxy (Speed aggregation active)" 
        : "Proxy Only (Single Primary Adapter, Bonding bypassed)";

    public void DisconnectCurrentProxy()
    {
        _rotatingRouter?.DisconnectProxy();
        ActiveProxyDisplay = "None (Direct Multi-WAN Active)";
        RotationCountdownText = "Direct Multi-WAN (Adapters Combined, No Proxy)";
        HarvestStatusText = "Disconnected from proxy server. Routing directly through combined network adapters.";
        _ = ResolveGeoIpAsync(forceRefresh: true);
    }

    public void OpenAccessibilityDialog()
    {
        var dlg = new AccessibilityDialog(this);
        dlg.ShowDialog();
    }

    public void SaveAccessibilitySettings()
    {
        Config.IsHighContrast = IsHighContrast;
        Config.UiScale = UiScale;
        Config.IsReduceMotion = IsReduceMotion;
        Config.IsScreenReaderOptimized = IsScreenReaderOptimized;
        _configStore.SaveImmediate(Adapters, Config, ActiveProfileName);
        AutoSaveStatusText = "Accessibility Settings Saved";
    }

    // ==========================================
    // ROTATING PROXY & HARVESTER SUBSYSTEM
    // ==========================================

    public ObservableCollection<HarvestedProxyModel> HarvestedProxies { get; } = new();

    private int _totalHarvestedCount;
    public int TotalHarvestedCount
    {
        get => _totalHarvestedCount;
        set => SetField(ref _totalHarvestedCount, value);
    }

    private int _verifiedHealthyCount;
    public int VerifiedHealthyCount
    {
        get => _verifiedHealthyCount;
        set => SetField(ref _verifiedHealthyCount, value);
    }

    private int _averagePingMs;
    public int AveragePingMs
    {
        get => _averagePingMs;
        set => SetField(ref _averagePingMs, value);
    }

    private bool _isHarvestingOrValidating;
    public bool IsHarvestingOrValidating
    {
        get => _isHarvestingOrValidating;
        set => SetField(ref _isHarvestingOrValidating, value);
    }

    private string _harvestStatusText = "Ready to harvest open-source proxies";
    public string HarvestStatusText
    {
        get => _harvestStatusText;
        set => SetField(ref _harvestStatusText, value);
    }

    private int _harvestProgress;
    public int HarvestProgress
    {
        get => _harvestProgress;
        set => SetField(ref _harvestProgress, value);
    }

    private bool _isRotatingProxyRunning;
    public bool IsRotatingProxyRunning
    {
        get => _isRotatingProxyRunning;
        set => SetField(ref _isRotatingProxyRunning, value);
    }

    public ObservableCollection<string> RotationStrategies { get; } = new()
    {
        "Round Robin (Per-Request)",
        "Sticky Domain (Host Affinity)",
        "Sticky Session (Duration)",
        "Lowest Latency (Fastest)",
        "Elite Anonymity Only"
    };

    private string _selectedRotationStrategy = "Round Robin (Per-Request)";
    public string SelectedRotationStrategy
    {
        get => _selectedRotationStrategy;
        set
        {
            if (SetField(ref _selectedRotationStrategy, value))
            {
                Config.RotatingProxy.Strategy = value switch
                {
                    "Sticky Domain (Host Affinity)" => RotationStrategy.StickyDomain,
                    "Sticky Session (Duration)" => RotationStrategy.StickySession,
                    "Lowest Latency (Fastest)" => RotationStrategy.LowestLatency,
                    "Elite Anonymity Only" => RotationStrategy.EliteOnly,
                    _ => RotationStrategy.RoundRobin
                };
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public bool ChainPhysicalAdapters
    {
        get => Config.RotatingProxy.ChainPhysicalAdapters;
        set
        {
            if (Config.RotatingProxy.ChainPhysicalAdapters != value)
            {
                Config.RotatingProxy.ChainPhysicalAdapters = value;
                OnPropertyChanged();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public bool StrictAnonymity
    {
        get => Config.RotatingProxy.StrictAnonymity;
        set
        {
            if (Config.RotatingProxy.StrictAnonymity != value)
            {
                Config.RotatingProxy.StrictAnonymity = value;
                OnPropertyChanged();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public int RotatingProxyPort
    {
        get => Config.RotatingProxy.RotatingProxyPort;
        set
        {
            if (Config.RotatingProxy.RotatingProxyPort != value && value > 1024 && value < 65535)
            {
                Config.RotatingProxy.RotatingProxyPort = value;
                OnPropertyChanged();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public async Task HarvestProxiesAsync()
    {
        if (IsHarvestingOrValidating) return;
        IsHarvestingOrValidating = true;
        HarvestProgress = 10;
        HarvestStatusText = "Harvesting from open-source repositories & feeds (Idempotence Mode)...";

        try
        {
            var sources = Config.RotatingProxy.GetEffectiveSources();

            // Persist repository sources to disk immediately
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CombiSpeed");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string sourcesFile = Path.Combine(dir, "proxy_sources.json");
                File.WriteAllText(sourcesFile, JsonSerializer.Serialize(sources, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }

            var harvested = await _proxyHarvester.HarvestAllAsync(sources, Config.RotatingProxy.MaxPoolSize);

            // IDEMPOTENCE MODE: deduplicate candidates, strictly preserving existing user selections and benchmarks
            int newCandidates = _proxyValidator.IdempotentAddCandidates(harvested);
            HarvestProgress = 50;
            HarvestStatusText = $"Harvested in Idempotence mode: {newCandidates} new, {harvested.Count - newCandidates} existing preserved. Saving persistent state...";

            // Persist immediately upon harvesting so proxies and settings survive restarts
            SaveCurrentProxyState();

            HarvestProgress = 65;
            HarvestStatusText = $"Harvested {newCandidates} new ({harvested.Count - newCandidates} existing preserved). Validating top candidates...";

            var toValidate = harvested.Take(80).ToList();
            var prog = new Progress<int>(pct => HarvestProgress = 65 + (pct * 35 / 100));
            await _proxyValidator.ValidateAllAsync(toValidate, Config.RotatingProxy.ConcurrencyLimit, Config.RotatingProxy.MaxTimeoutMs, prog);

            // Persist state again after validation to record newly verified metrics
            SaveCurrentProxyState();
            RefreshProxyCollection();
            HarvestStatusText = $"Harvest complete (Idempotent)! {VerifiedHealthyCount} working proxies in pool.";
        }
        catch (Exception ex)
        {
            HarvestStatusText = $"Harvest error: {ex.Message}";
        }
        finally
        {
            HarvestProgress = 100;
            IsHarvestingOrValidating = false;
        }
    }

    public async Task ValidatePoolAsync()
    {
        if (IsHarvestingOrValidating || _proxyValidator.TotalCount == 0) return;
        IsHarvestingOrValidating = true;
        HarvestProgress = 0;
        HarvestStatusText = "Validating proxy pool...";

        try
        {
            var prog = new Progress<int>(pct => HarvestProgress = pct);
            await _proxyValidator.ValidateAllAsync(null, Config.RotatingProxy.ConcurrencyLimit, Config.RotatingProxy.MaxTimeoutMs, prog);
            RefreshProxyCollection();
            HarvestStatusText = $"Validation complete. {VerifiedHealthyCount} healthy proxies verified.";
        }
        catch (Exception ex)
        {
            HarvestStatusText = $"Validation error: {ex.Message}";
        }
        finally
        {
            HarvestProgress = 100;
            IsHarvestingOrValidating = false;
        }
    }

    public void PruneDeadProxies()
    {
        int pruned = _proxyValidator.PruneDead();
        RefreshProxyCollection();
        HarvestStatusText = $"Pruned {pruned} dead proxies from pool.";
    }

    public void ClearProxies()
    {
        _proxyValidator.Clear();
        RefreshProxyCollection();
        HarvestStatusText = "Proxy pool cleared.";
    }

    public async Task ExportWorkingProxiesAsync()
    {
        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            FileName = "working_proxies.txt"
        };

        if (sfd.ShowDialog() == true)
        {
            await _proxyHarvester.ExportToFileAsync(sfd.FileName, _proxyValidator.AllProxies, workingOnly: true);
            HarvestStatusText = $"Exported working proxies to {System.IO.Path.GetFileName(sfd.FileName)}";
        }
    }

    public async Task ImportProxiesFileAsync()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text & CSV Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            var imported = await _proxyHarvester.ImportFromFileAsync(ofd.FileName);
            _proxyValidator.AddCandidates(imported);
            RefreshProxyCollection();
            HarvestStatusText = $"Imported {imported.Count} proxies. Click Validate Pool to test them.";
        }
    }

    public ObservableCollection<string> CountryFilters { get; } = new()
    {
        "ALL",
        "US",
        "DE",
        "GB",
        "FR",
        "CA",
        "JP",
        "NL",
        "SG",
        "IN"
    };

    public string SelectedCountryFilter
    {
        get => Config.RotatingProxy.CountryFilter;
        set
        {
            if (Config.RotatingProxy.CountryFilter != value)
            {
                Config.RotatingProxy.CountryFilter = value;
                OnPropertyChanged();
                RefreshProxyCollection();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
                RequestSaveProxyState();
            }
        }
    }

    // --- Proxy Pool Sorting & Filtering Properties ---
    private string _proxySearchText = "";
    public string ProxySearchText
    {
        get => _proxySearchText;
        set
        {
            if (SetField(ref _proxySearchText, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public List<string> LatencyFilters { get; } = new()
    {
        "ALL Latencies",
        "< 150 ms (Ultra-Fast)",
        "< 300 ms (Fast)",
        "< 500 ms (Normal)",
        "< 1000 ms (Acceptable)"
    };

    private string _selectedLatencyFilter = "ALL Latencies";
    public string SelectedLatencyFilter
    {
        get => _selectedLatencyFilter;
        set
        {
            if (SetField(ref _selectedLatencyFilter, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public ObservableCollection<string> SourceFilters { get; } = new() { "ALL Sources" };

    private string _selectedSourceFilter = "ALL Sources";
    public string SelectedSourceFilter
    {
        get => _selectedSourceFilter;
        set
        {
            if (SetField(ref _selectedSourceFilter, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public List<string> ProtocolFilters { get; } = new()
    {
        "ALL Protocols",
        "HTTP",
        "HTTPS",
        "SOCKS4",
        "SOCKS5"
    };

    private string _selectedProtocolFilter = "ALL Protocols";
    public string SelectedProtocolFilter
    {
        get => _selectedProtocolFilter;
        set
        {
            if (SetField(ref _selectedProtocolFilter, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public List<string> SuccessFilters { get; } = new()
    {
        "ALL Success Counts",
        "≥ 1 Success",
        "≥ 3 Successes",
        "≥ 5 Successes",
        "≥ 10 Successes"
    };

    private string _selectedSuccessFilter = "ALL Success Counts";
    public string SelectedSuccessFilter
    {
        get => _selectedSuccessFilter;
        set
        {
            if (SetField(ref _selectedSuccessFilter, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public List<string> FailureFilters { get; } = new()
    {
        "ALL Failure Counts",
        "0 Failures (Perfect)",
        "≤ 1 Failure",
        "≤ 3 Failures",
        "≤ 5 Failures"
    };

    private string _selectedFailureFilter = "ALL Failure Counts";
    public string SelectedFailureFilter
    {
        get => _selectedFailureFilter;
        set
        {
            if (SetField(ref _selectedFailureFilter, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    public List<string> SortOptions { get; } = new()
    {
        "Lowest Latency (Fastest)",
        "Highest Latency",
        "Status / Health First",
        "Source (A-Z)",
        "Country Code (A-Z)",
        "Protocol (A-Z)",
        "Lowest Failures"
    };

    private string _selectedSortOption = "Lowest Latency (Fastest)";
    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetField(ref _selectedSortOption, value))
            {
                RefreshProxyCollection();
                RequestSaveProxyState();
            }
        }
    }

    private string _filteredCountText = "Showing 0 proxies";
    public string FilteredCountText
    {
        get => _filteredCountText;
        set => SetField(ref _filteredCountText, value);
    }

    public bool AutoHarvestEnabled
    {
        get => Config.RotatingProxy.AutoHarvestEnabled;
        set
        {
            if (Config.RotatingProxy.AutoHarvestEnabled != value)
            {
                Config.RotatingProxy.AutoHarvestEnabled = value;
                OnPropertyChanged();
                _proxyScheduler.RestartTimers();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public bool AutoValidateEnabled
    {
        get => Config.RotatingProxy.AutoValidateEnabled;
        set
        {
            if (Config.RotatingProxy.AutoValidateEnabled != value)
            {
                Config.RotatingProxy.AutoValidateEnabled = value;
                OnPropertyChanged();
                _proxyScheduler.RestartTimers();
                _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
            }
        }
    }

    public void ToggleRotatingProxy()
    {
        if (IsRotatingProxyRunning)
        {
            _proxyScheduler.Stop();
            _rotatingProxyServer.Stop();
            IsRotatingProxyRunning = false;
            Config.RotatingProxy.EnableRotatingProxy = false;
            HarvestStatusText = "Rotating proxy server stopped.";
        }
        else
        {
            _rotatingProxyServer.Start();
            _proxyScheduler.Start();
            IsRotatingProxyRunning = _rotatingProxyServer.IsRunning;
            Config.RotatingProxy.EnableRotatingProxy = true;
            HarvestStatusText = IsRotatingProxyRunning
                ? $"Rotating proxy server active on port {RotatingProxyPort}"
                : "Failed to start rotating proxy server.";
        }
        _configStore.RequestAutoSave(Adapters, Config, ActiveProfileName);
    }

    private void RefreshProxyCollection()
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            HarvestedProxies.Clear();
            var all = _proxyValidator.AllProxies.AsEnumerable();

            // 1. Dynamic Source Filters list discovery
            var discoveredSources = all.Select(p => ExtractSourceLabel(p.SourceUrl)).Distinct().OrderBy(s => s).ToList();
            foreach (var src in discoveredSources)
            {
                if (!string.IsNullOrWhiteSpace(src) && !SourceFilters.Contains(src))
                {
                    SourceFilters.Add(src);
                }
            }

            // 2. Country Filter
            if (!string.IsNullOrWhiteSpace(Config.RotatingProxy.CountryFilter) && !Config.RotatingProxy.CountryFilter.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                all = all.Where(p => p.CountryCode.Equals(Config.RotatingProxy.CountryFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Max Latency Threshold Filter
            if (!string.IsNullOrWhiteSpace(SelectedLatencyFilter) && !SelectedLatencyFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedLatencyFilter.Contains("150")) all = all.Where(p => p.LatencyMs <= 150);
                else if (SelectedLatencyFilter.Contains("300")) all = all.Where(p => p.LatencyMs <= 300);
                else if (SelectedLatencyFilter.Contains("500")) all = all.Where(p => p.LatencyMs <= 500);
                else if (SelectedLatencyFilter.Contains("1000")) all = all.Where(p => p.LatencyMs <= 1000);
            }

            // 4. Source Origin Filter
            if (!string.IsNullOrWhiteSpace(SelectedSourceFilter) && !SelectedSourceFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
            {
                all = all.Where(p => p.SourceUrl.Contains(SelectedSourceFilter, StringComparison.OrdinalIgnoreCase) ||
                                     ExtractSourceLabel(p.SourceUrl).Equals(SelectedSourceFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 5. Protocol Filter
            if (!string.IsNullOrWhiteSpace(SelectedProtocolFilter) && !SelectedProtocolFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<ProxyProtocol>(SelectedProtocolFilter, true, out var proto))
                {
                    all = all.Where(p => p.Protocol == proto);
                }
            }

            // 6. Success Count Filter
            if (!string.IsNullOrWhiteSpace(SelectedSuccessFilter) && !SelectedSuccessFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedSuccessFilter.Contains("10")) all = all.Where(p => p.Successes >= 10);
                else if (SelectedSuccessFilter.Contains("5")) all = all.Where(p => p.Successes >= 5);
                else if (SelectedSuccessFilter.Contains("3")) all = all.Where(p => p.Successes >= 3);
                else if (SelectedSuccessFilter.Contains("1")) all = all.Where(p => p.Successes >= 1);
            }

            // 7. Failure Count Filter
            if (!string.IsNullOrWhiteSpace(SelectedFailureFilter) && !SelectedFailureFilter.StartsWith("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedFailureFilter.Contains("0") || SelectedFailureFilter.Contains("Perfect")) all = all.Where(p => p.Failures == 0);
                else if (SelectedFailureFilter.Contains("1")) all = all.Where(p => p.Failures <= 1);
                else if (SelectedFailureFilter.Contains("3")) all = all.Where(p => p.Failures <= 3);
                else if (SelectedFailureFilter.Contains("5")) all = all.Where(p => p.Failures <= 5);
            }

            // 8. Text Search Filter (IP, Port, Country, Source, Protocol)
            if (!string.IsNullOrWhiteSpace(ProxySearchText))
            {
                string q = ProxySearchText.Trim();
                all = all.Where(p => p.Endpoint.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     p.CountryCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     p.SourceUrl.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     p.FormattedProtocol.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            // 9. Multi-Criteria Sorting
            all = SelectedSortOption switch
            {
                "Lowest Latency (Fastest)" => all.OrderBy(p => !p.IsHealthy).ThenBy(p => p.LatencyMs),
                "Highest Latency" => all.OrderBy(p => !p.IsHealthy).ThenByDescending(p => p.LatencyMs),
                "Status / Health First" => all.OrderByDescending(p => p.IsHealthy).ThenBy(p => p.Failures).ThenBy(p => p.LatencyMs),
                "Source (A-Z)" => all.OrderBy(p => p.SourceUrl).ThenBy(p => p.LatencyMs),
                "Country Code (A-Z)" => all.OrderBy(p => p.CountryCode).ThenBy(p => p.LatencyMs),
                "Protocol (A-Z)" => all.OrderBy(p => p.Protocol).ThenBy(p => p.LatencyMs),
                "Lowest Failures" => all.OrderBy(p => p.Failures).ThenBy(p => p.LatencyMs),
                _ => all.OrderBy(p => !p.IsHealthy).ThenBy(p => p.LatencyMs)
            };

            var displayList = all.Take(400).ToList();
            foreach (var p in displayList)
            {
                p.SelectionChanged -= OnProxySelectionChanged;
                p.SelectionChanged += OnProxySelectionChanged;
                HarvestedProxies.Add(p);
            }

            TotalHarvestedCount = _proxyValidator.TotalCount;
            VerifiedHealthyCount = _proxyValidator.HealthyCount;

            var healthy = _proxyValidator.GetHealthyProxies(Config.RotatingProxy.StrictAnonymity, Config.RotatingProxy.Strategy, Config.RotatingProxy.CountryFilter);
            AveragePingMs = healthy.Count > 0 ? (int)healthy.Average(p => p.LatencyMs) : 0;
            FilteredCountText = $"Showing {displayList.Count} of {TotalHarvestedCount} proxies ({VerifiedHealthyCount} working)";
            UpdateSelectedProxiesCount();
        });
    }

    private void OnProxySelectionChanged()
    {
        UpdateSelectedProxiesCount();
        RequestSaveProxyState();
    }

    private string _activeProxyDisplay = "Idle (Traffic combined & ready)";
    public string ActiveProxyDisplay
    {
        get => _activeProxyDisplay;
        set => SetField(ref _activeProxyDisplay, value);
    }

    private void UpdateSelectedProxiesCount()
    {
        int selCount = _proxyValidator.AllProxies.Count(p => p.IsSelected);
        SelectedProxiesCountText = $"Selected for Rotation: {selCount} of {_proxyValidator.HealthyCount} healthy";
    }

    public void SelectAllProxies()
    {
        foreach (var p in _proxyValidator.AllProxies)
        {
            p.IsSelected = true;
        }
        UpdateSelectedProxiesCount();
        SaveCurrentProxyState();
    }

    public void DeselectAllProxies()
    {
        foreach (var p in _proxyValidator.AllProxies)
        {
            p.IsSelected = false;
        }
        UpdateSelectedProxiesCount();
        SaveCurrentProxyState();
    }

    public void SelectFastProxies()
    {
        foreach (var p in _proxyValidator.AllProxies)
        {
            p.IsSelected = p.IsHealthy && p.LatencyMs <= 300;
        }
        UpdateSelectedProxiesCount();
        SaveCurrentProxyState();
    }

    /// <summary>
    /// Immediately saves all harvested proxies along with active UI filters, sorting choices,
    /// and user modifications to persistent disk storage in Idempotence mode.
    /// </summary>
    public void SaveCurrentProxyState()
    {
        var state = new PersistedProxyStateModel
        {
            SelectedLatencyFilter = SelectedLatencyFilter,
            SelectedSourceFilter = SelectedSourceFilter,
            SelectedProtocolFilter = SelectedProtocolFilter,
            SelectedCountryFilter = Config.RotatingProxy.CountryFilter,
            SelectedSuccessFilter = SelectedSuccessFilter,
            SelectedFailureFilter = SelectedFailureFilter,
            ProxySearchText = ProxySearchText,
            SelectedSortOption = SelectedSortOption,
            RouteBondedTrafficThroughRotator = RouteBondedTrafficThroughRotator,
            RotationIntervalSeconds = RotationIntervalSeconds,
            SelectedRotationStrategy = SelectedRotationStrategy,
            RepositorySources = Config.RotatingProxy.GetEffectiveSources().Distinct().ToList()
        };
        _proxyValidator.SavePersistedState(state);
    }

    /// <summary>
    /// Debounced asynchronous persistence of proxy state (filters, sorting, selections) after 400ms.
    /// </summary>
    public void RequestSaveProxyState()
    {
        _proxyValidator.RequestSavePersistedState(() => new PersistedProxyStateModel
        {
            SelectedLatencyFilter = SelectedLatencyFilter,
            SelectedSourceFilter = SelectedSourceFilter,
            SelectedProtocolFilter = SelectedProtocolFilter,
            SelectedCountryFilter = Config.RotatingProxy.CountryFilter,
            SelectedSuccessFilter = SelectedSuccessFilter,
            SelectedFailureFilter = SelectedFailureFilter,
            ProxySearchText = ProxySearchText,
            SelectedSortOption = SelectedSortOption,
            RouteBondedTrafficThroughRotator = RouteBondedTrafficThroughRotator,
            RotationIntervalSeconds = RotationIntervalSeconds,
            SelectedRotationStrategy = SelectedRotationStrategy,
            RepositorySources = Config.RotatingProxy.GetEffectiveSources().Distinct().ToList()
        });
    }

    private HarvestedProxyModel? _selectedProxyRow;
    public HarvestedProxyModel? SelectedProxyRow
    {
        get => _selectedProxyRow;
        set => SetField(ref _selectedProxyRow, value);
    }

    public async Task ForceRotateProxyAsync()
    {
        await ForceConnectSelectedOrNextAsync(null);
    }

    public void ForceRotateProxy()
    {
        _ = Task.Run(async () => await ForceRotateProxyAsync());
    }

    public async Task ForceConnectSelectedOrNextAsync(HarvestedProxyModel? target = null)
    {
        if (_rotatingRouter == null) return;
        var preferred = target ?? SelectedProxyRow;
        string targetDesc = preferred != null ? $"{preferred.Endpoint} ({preferred.CountryCode})" : "next live candidate";
        HarvestStatusText = $"Connecting to {targetDesc} and testing internet accessibility (10s threshold)...";
        RotationCountdownText = "⏳ Testing internet accessibility (10s threshold)...";

        var live = await _rotatingRouter.ForceConnectToLiveProxyAsync(preferred);
        if (live != null)
        {
            ActiveProxyDisplay = $"{live.Endpoint} ({live.CountryCode}) {live.FlagEmoji}";
            HarvestStatusText = $"Successfully connected & verified internet access via {live.Endpoint} ({live.CountryCode} {live.FlagEmoji})";
            RotationCountdownText = $"Next rotation in {FormatRemainingTime(_rotatingRouter.SecondsUntilNextRotation)}";
            RouteBondedTrafficThroughRotator = true;
            await ResolveGeoIpAsync(forceRefresh: true, proxy: live);
        }
        else
        {
            ActiveProxyDisplay = "Connection failed / No internet access";
            HarvestStatusText = $"Failed to establish internet-accessible proxy connection to {targetDesc} (10s threshold).";
            RotationCountdownText = "⏳ Waiting for live proxy connection...";
        }
    }

    // GeoIP & Intranet/Internet Location Resolution
    private IpGeoLocationModel _currentGeoLocation = new();
    public IpGeoLocationModel CurrentGeoLocation
    {
        get => _currentGeoLocation;
        set => SetField(ref _currentGeoLocation, value);
    }

    public string CurrentIntranetIp => _currentGeoLocation.IntranetIp;
    public string CurrentInternetIp => _currentGeoLocation.InternetIp;
    public string CurrentLocation => _currentGeoLocation.FormattedLocation;
    public string CurrentCountry => _currentGeoLocation.Country;
    public string CurrentIsp => _currentGeoLocation.Isp;
    public string CurrentFlagEmoji => _currentGeoLocation.FlagEmoji;

    public async Task ResolveGeoIpAsync(bool forceRefresh = false, HarvestedProxyModel? proxy = null)
    {
        try
        {
            var loc = await GeoIpService.ResolveCurrentLocationAsync(Adapters, proxy: proxy, forceRefresh: forceRefresh);
            CurrentGeoLocation = loc;
            OnPropertyChanged(nameof(CurrentIntranetIp));
            OnPropertyChanged(nameof(CurrentInternetIp));
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(CurrentCountry));
            OnPropertyChanged(nameof(CurrentIsp));
            OnPropertyChanged(nameof(CurrentFlagEmoji));
        }
        catch { }
    }

    public bool RouteBondedTrafficThroughRotator
    {
        get => Config.RotatingProxy.RouteBondedTrafficThroughRotator;
        set
        {
            if (Config.RotatingProxy.RouteBondedTrafficThroughRotator != value)
            {
                Config.RotatingProxy.RouteBondedTrafficThroughRotator = value;
                OnPropertyChanged();
                _proxyEngine.CurrentConfig = Config;
                SaveCurrentCombinationImmediate();
                RequestSaveProxyState();
            }
        }
    }

    public ObservableCollection<RotationIntervalOptionItem> RotationIntervalOptions { get; } = new()
    {
        new() { Seconds = 10, Label = "10s" },
        new() { Seconds = 15, Label = "15s" },
        new() { Seconds = 30, Label = "30s" },
        new() { Seconds = 45, Label = "45s" },
        new() { Seconds = 60, Label = "1 min" },
        new() { Seconds = 90, Label = "1.5 min" },
        new() { Seconds = 120, Label = "2 min" },
        new() { Seconds = 180, Label = "3 min" },
        new() { Seconds = 300, Label = "5 min" },
        new() { Seconds = 600, Label = "10 min" },
        new() { Seconds = 900, Label = "15 min" },
        new() { Seconds = 1800, Label = "30 min" },
        new() { Seconds = 2700, Label = "45 min" },
        new() { Seconds = 3600, Label = "1 hour" },
        new() { Seconds = 5400, Label = "1.5 hours" },
        new() { Seconds = 7200, Label = "2 hours" },
    };

    public int RotationIntervalSeconds
    {
        get => Config.RotatingProxy.RotationIntervalSeconds;
        set
        {
            int clamped = Math.Clamp(value, 5, 7200);
            if (Config.RotatingProxy.RotationIntervalSeconds != clamped)
            {
                Config.RotatingProxy.RotationIntervalSeconds = clamped;
                EnsureIntervalOptionExists(clamped);
                OnPropertyChanged();
                SaveCurrentCombinationImmediate();
                RequestSaveProxyState();
                if (_rotatingRouter != null && _rotatingRouter.IsLiveConnected)
                {
                    int rem = _rotatingRouter.SecondsUntilNextRotation;
                    RotationCountdownText = rem > 0 ? $"Next rotation in {FormatRemainingTime(rem)}" : "Rotating now...";
                }
            }
        }
    }

    private void EnsureIntervalOptionExists(int seconds)
    {
        if (!RotationIntervalOptions.Any(o => o.Seconds == seconds))
        {
            string label;
            if (seconds >= 3600)
                label = seconds % 3600 == 0 ? $"{seconds / 3600} hours" : $"{seconds / 3600.0:0.#} hours";
            else if (seconds >= 60)
                label = seconds % 60 == 0 ? $"{seconds / 60} min" : $"{seconds / 60.0:0.#} min";
            else
                label = $"{seconds}s";

            var item = new RotationIntervalOptionItem { Seconds = seconds, Label = label };
            int insertIdx = 0;
            while (insertIdx < RotationIntervalOptions.Count && RotationIntervalOptions[insertIdx].Seconds < seconds)
            {
                insertIdx++;
            }
            RotationIntervalOptions.Insert(insertIdx, item);
        }
    }

    public static string FormatRemainingTime(int totalSeconds)
    {
        if (totalSeconds < 0) return "Waiting for live proxy connection...";
        if (totalSeconds == 0) return "Rotating to next live proxy...";
        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{ts.Seconds}s";
    }

    private string _rotationCountdownText = "⏳ Waiting for live proxy connection...";
    public string RotationCountdownText
    {
        get => _rotationCountdownText;
        set => SetField(ref _rotationCountdownText, value);
    }

    private string _selectedProxiesCountText = "Selected for Rotation: 0 healthy";
    public string SelectedProxiesCountText
    {
        get => _selectedProxiesCountText;
        set => SetField(ref _selectedProxiesCountText, value);
    }

    private static string ExtractSourceLabel(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl)) return "Manual/Local";
        if (sourceUrl.Contains("TheSpeedX", StringComparison.OrdinalIgnoreCase)) return "TheSpeedX";
        if (sourceUrl.Contains("monosans", StringComparison.OrdinalIgnoreCase)) return "monosans";
        if (sourceUrl.Contains("clarketm", StringComparison.OrdinalIgnoreCase)) return "clarketm";
        if (sourceUrl.Contains("proxyscrape", StringComparison.OrdinalIgnoreCase)) return "proxyscrape";
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri)) return uri.Host;
        return sourceUrl;
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

public class RotationIntervalOptionItem
{
    public int Seconds { get; set; }
    public string Label { get; set; } = string.Empty;
    public override string ToString() => Label;
}
