using System.Diagnostics;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// Headless execution engine that operates without a GUI.
/// Powers the Windows Service mode, background daemon, and CLI headless runner.
/// </summary>
public class HeadlessEngine : IDisposable
{
    private readonly NetworkDetector _networkDetector = new();
    private readonly LoadBalancerEngine _loadBalancer = new();
    private readonly WindowsProxyManager _proxyManager = new();
    private readonly VirtualBridgeManager _virtualBridgeManager = new("CombiSpeed Bonded Bridge");
    private readonly ProxyEngine _proxyEngine;

    private readonly ProxyConfigModel _config = new();
    private CancellationTokenSource? _cts;

    public bool IsRunning => _proxyEngine.IsRunning;

    public HeadlessEngine(ProxyConfigModel? config = null)
    {
        if (config != null) _config = config;
        _proxyEngine = new ProxyEngine(_loadBalancer, () => _networkDetector.GetAllCached());

        _proxyEngine.OnAuditLog += log =>
        {
            Console.WriteLine($"[{log.TimeFormatted}] [{log.Protocol}] {log.TargetHost}:{log.TargetPort} -> {log.AdapterName} ({log.VolumeFormatted})");
        };

        _virtualBridgeManager.OnAuditLog += log =>
        {
            Console.WriteLine($"[{log.TimeFormatted}] [{log.Protocol}] {log.TargetHost}:{log.TargetPort} -> {log.AdapterName} ({log.VolumeFormatted})");
        };
    }

    public async Task StartAsync(bool enableSystemProxy = false, bool enableBridge = true)
    {
        Console.WriteLine("=====================================================================");
        Console.WriteLine("      CombiSpeed Headless Engine & Background Service Active         ");
        Console.WriteLine("=====================================================================");

        _cts = new CancellationTokenSource();

        // 1. Detect physical adapters
        var adapters = _networkDetector.DetectAdapters();

        // Load saved adapter combinations and weight settings
        var configStore = new ConfigurationStore();
        if (configStore.ApplyToAdapters(adapters))
        {
            NetworkDetector.RecalculateEffectivePercentages(adapters);
            Console.WriteLine("[INFO] Successfully restored saved adapter settings & combination weights from disk.");
        }

        Console.WriteLine($"[INFO] Detected {adapters.Count} active network adapters:");
        foreach (var a in adapters)
        {
            Console.WriteLine($"  - {a.Name} ({a.Address}) | Speed: {a.LinkSpeed} | Weight: {a.Weight}% ({a.EffectivePercentage:F1}% of pool)");
        }

        // 2. Start Proxy Engine (HTTP 8080, SOCKS5 1080)
        Console.WriteLine($"[INFO] Starting Proxy Engine on HTTP:{_config.HttpPort}, SOCKS5:{_config.SocksPort}...");
        await _proxyEngine.StartAsync(_config.HttpPort, _config.SocksPort);

        // 3. Start Virtual Bridge Adapter
        if (enableBridge)
        {
            Console.WriteLine($"[INFO] Starting Virtual Bonded Bridge Gateway on port {_config.VirtualBridgePort}...");
            await _virtualBridgeManager.StartBridgeAsync(_config.VirtualBridgePort, _loadBalancer, () => _networkDetector.GetAllCached());
        }

        // 4. Optionally enable Windows Proxy / PAC
        if (_config.EnablePac)
        {
            Console.WriteLine($"[INFO] Enabling Windows PAC domain routing ({_config.PacUrl})...");
            _proxyManager.EnablePac(_config.PacUrl);
        }
        else if (enableSystemProxy)
        {
            Console.WriteLine("[INFO] Enabling Windows System Proxy for all applications...");
            _proxyManager.Enable(_config.HttpPort, _config.SocksPort);
        }

        Console.WriteLine("[OK] CombiSpeed Bonding Engine is now actively aggregating bandwidth.");
        Console.WriteLine("Press Ctrl+C or send --stop command to terminate gracefully.");
    }

    public async Task StopAsync()
    {
        Console.WriteLine("[INFO] Shutting down CombiSpeed Headless Engine...");
        _cts?.Cancel();

        if (_proxyManager.IsApplied || _proxyManager.IsPacApplied)
        {
            _proxyManager.Disable();
            _proxyManager.DisablePac();
            Console.WriteLine("[OK] Direct Internet restored.");
        }

        await _virtualBridgeManager.StopBridgeAsync();
        await _proxyEngine.StopAsync();
        Console.WriteLine("[OK] Headless engine stopped successfully.");
    }

    public async Task RunBlockingAsync(CancellationToken externalToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts?.Token ?? CancellationToken.None, externalToken);

        // Telemetry logger loop
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        try
        {
            while (await timer.WaitForNextTickAsync(linkedCts.Token))
            {
                string down = NetworkAdapterModel.FormatSpeed(_proxyEngine.InstantDownloadSpeedBps + _virtualBridgeManager.Model.DownloadSpeedBps);
                string up = NetworkAdapterModel.FormatSpeed(_proxyEngine.InstantUploadSpeedBps + _virtualBridgeManager.Model.UploadSpeedBps);
                int streams = _proxyEngine.ActiveConnections + _virtualBridgeManager.Model.ActiveStreams;
                Console.WriteLine($"[TELEMETRY] Bonded Speed: ↓ {down} | ↑ {up} | Active Streams: {streams}");
            }
        }
        catch (OperationCanceledException)
        {
            // Exit loop
        }
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}

