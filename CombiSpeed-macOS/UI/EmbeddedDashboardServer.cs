using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CombiSpeed.macOS.Models;
using CombiSpeed.macOS.Services;

namespace CombiSpeed.macOS.UI;

public class EmbeddedDashboardServer
{
    private readonly ProxyConfigModel _config;
    private readonly List<NetworkAdapterModel> _adapters;
    private readonly MacProxyEngine _engine;
    private readonly MacLoadBalancer _loadBalancer;
    private readonly MacBandwidthThrottler _throttler;
    private readonly MacProxyManager _proxyManager;
    private readonly MacVirtualBridgeManager _bridgeManager;
    private readonly MacSettingsStorage _storage;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private bool _isEngineActive = true;

    public int Port => _config.DashboardPort;

    public EmbeddedDashboardServer(
        ProxyConfigModel config,
        List<NetworkAdapterModel> adapters,
        MacProxyEngine engine,
        MacLoadBalancer loadBalancer,
        MacBandwidthThrottler throttler,
        MacProxyManager proxyManager,
        MacVirtualBridgeManager bridgeManager,
        MacSettingsStorage storage)
    {
        _config = config;
        _adapters = adapters;
        _engine = engine;
        _loadBalancer = loadBalancer;
        _throttler = throttler;
        _proxyManager = proxyManager;
        _bridgeManager = bridgeManager;
        _storage = storage;
    }

    public void Start(bool openBrowser = true)
    {
        _cts = new CancellationTokenSource();
        int attempts = 0;
        int port = _config.DashboardPort;

        while (attempts < 10)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();
                _config.DashboardPort = port;
                break;
            }
            catch
            {
                port++;
                attempts++;
            }
        }

        if (_listener == null || !_listener.IsListening)
        {
            Console.WriteLine("[CombiSpeed] Could not bind HTTP Dashboard Listener.");
            return;
        }

        Console.WriteLine($"[CombiSpeed] Dashboard active on http://127.0.0.1:{port}/");
        _ = ProcessRequestsAsync(_cts.Token);

        if (openBrowser)
        {
            OpenBrowser($"http://127.0.0.1:{port}/");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener = null;
    }

    private void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }
        catch { }
    }

    private async Task ProcessRequestsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
            catch when (ct.IsCancellationRequested) { break; }
            catch { }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 204;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

        try
        {
            if (path == "/" || path == "/index.html")
            {
                string html = CyberpunkSpa.GetHtml("MACOS EDITION");
                byte[] bytes = Encoding.UTF8.GetBytes(html);
                res.ContentType = "text/html; charset=utf-8";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
            }
            else if (path == "/api/pac/export")
            {
                string pac = PacConfigModel.Current.GeneratePacScript(_config.HttpPort, _config.SocksPort);
                byte[] bytes = Encoding.UTF8.GetBytes(pac);
                res.ContentType = "application/x-ns-proxy-autoconfig";
                res.Headers.Add("Content-Disposition", "attachment; filename=\"proxy.pac\"");
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
            }
            else if (path == "/api/telemetry")
            {
                var telem = new
                {
                    isEngineActive = _isEngineActive,
                    totalDownloadKbps = _adapters.Sum(a => a.DownloadSpeedKbps),
                    totalUploadKbps = _adapters.Sum(a => a.UploadSpeedKbps),
                    totalBytesDownloaded = _engine.TotalBytesDownloaded,
                    totalBytesUploaded = _engine.TotalBytesUploaded,
                    activeConnections = _engine.ActiveConnections,
                    shaperMode = _throttler.Mode.ToString(),
                    currentCapMbps = _throttler.CurrentCapMbps,
                    adapters = _adapters,
                    auditLogs = _engine.RecentLogs.Take(25).ToList()
                };

                // Clear momentary speeds for next sample
                foreach (var a in _adapters)
                {
                    a.DownloadSpeedKbps = 0;
                    a.UploadSpeedKbps = 0;
                }

                string json = JsonSerializer.Serialize(telem);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
            }
            else if (path == "/api/power/toggle" && req.HttpMethod == "POST")
            {
                _isEngineActive = !_isEngineActive;
                if (_isEngineActive) _engine.Start();
                else _engine.Stop();

                string json = JsonSerializer.Serialize(new { isActive = _isEngineActive });
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                await res.OutputStream.WriteAsync(bytes);
            }
            else if (path == "/api/adapters/update" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(body);
                string id = doc.RootElement.GetProperty("id").GetString() ?? "";
                bool isEnabled = doc.RootElement.GetProperty("isEnabled").GetBoolean();
                int weight = doc.RootElement.GetProperty("weight").GetInt32();

                var target = _adapters.FirstOrDefault(a => a.Id == id);
                if (target != null)
                {
                    target.IsEnabled = isEnabled;
                    target.Weight = weight;
                    _loadBalancer.UpdateAdapters(_adapters, _config.RoutingMode, _config.ShaperMode);
                    _storage.Save(_adapters, _config);
                }

                res.StatusCode = 200;
            }
            else if (path == "/api/shaper/mode" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(body);
                string modeStr = doc.RootElement.GetProperty("mode").GetString() ?? "";
                if (Enum.TryParse<SpeedShaperMode>(modeStr, out var mode))
                {
                    _config.ShaperMode = mode;
                    _throttler.Configure(_config.SpeedLimitMbps, mode, null, _config.EnableDynamicAdaptation);
                    _loadBalancer.UpdateAdapters(_adapters, _config.RoutingMode, mode);
                    _storage.Save(_adapters, _config);
                }
                res.StatusCode = 200;
            }
            else if (path == "/api/shaper/throttle" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(body);
                double limit = doc.RootElement.GetProperty("limitMbps").GetDouble();
                double burst = doc.RootElement.TryGetProperty("burstTolerance", out var b) ? b.GetDouble() : _config.BurstToleranceSeconds;

                _config.SpeedLimitMbps = limit;
                _config.BurstToleranceSeconds = burst;
                _throttler.Configure(limit, _config.ShaperMode, burst, _config.EnableDynamicAdaptation);
                _storage.Save(_adapters, _config);
                res.StatusCode = 200;
            }
            else if (path == "/api/shaper/borrowing" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(body);
                bool borrowing = doc.RootElement.GetProperty("borrowing").GetBoolean();
                _config.EnableDynamicAdaptation = borrowing;
                _throttler.Configure(_config.SpeedLimitMbps, _config.ShaperMode, _config.BurstToleranceSeconds, borrowing);
                _storage.Save(_adapters, _config);
                res.StatusCode = 200;
            }
            else if (path == "/api/bridge/toggle" && req.HttpMethod == "POST")
            {
                if (_bridgeManager.Model.IsActive) _bridgeManager.Disconnect();
                else _bridgeManager.Connect(_adapters.Count(a => a.IsEnabled));
                res.StatusCode = 200;
            }
            else if (path == "/api/proxy/toggle" && req.HttpMethod == "POST")
            {
                _config.EnableSystemProxy = !_config.EnableSystemProxy;
                if (_config.EnableSystemProxy) _proxyManager.Enable();
                else _proxyManager.Disable();
                res.StatusCode = 200;
            }
            else if (path == "/api/audit/clear" && req.HttpMethod == "POST")
            {
                lock (_engine.RecentLogs) _engine.RecentLogs.Clear();
                res.StatusCode = 200;
            }
            else if (path == "/api/speedtest/start" && req.HttpMethod == "POST")
            {
                var tester = new MacSpeedTester();
                var result = await tester.RunTestAsync(_adapters);
                var responseObj = new
                {
                    bondedMbps = result.BondedDownloadMbps,
                    boostPercent = result.BoostPercentage,
                    latencyMs = result.BondedLatencyMs,
                    adapters = result.AdapterResults
                };
                string json = JsonSerializer.Serialize(responseObj);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
            }
            else if (path == "/api/accessibility" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("isHighContrast", out var hc)) _config.IsHighContrast = hc.GetBoolean();
                if (doc.RootElement.TryGetProperty("isReduceMotion", out var rm)) _config.IsReduceMotion = rm.GetBoolean();
                if (doc.RootElement.TryGetProperty("uiScale", out var us)) _config.UiScale = us.GetDouble();
                _storage.Save(_adapters, _config);
                res.StatusCode = 200;
            }
            else
            {
                res.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            res.StatusCode = 500;
            byte[] err = Encoding.UTF8.GetBytes(ex.Message);
            await res.OutputStream.WriteAsync(err);
        }
        finally
        {
            try { res.Close(); } catch { }
        }
    }
}

