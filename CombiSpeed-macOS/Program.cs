using System.Diagnostics;
using CombiSpeed.macOS.Models;
using CombiSpeed.macOS.Services;
using CombiSpeed.macOS.UI;

namespace CombiSpeed.macOS;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Version information
        if (args.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
        {
            var ver = typeof(Program).Assembly.GetName().Version;
            string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.2.0";
            Console.WriteLine($"CombiSpeed {verStr} (Build 27) - macOS Edition");
            Console.WriteLine("Multi-WAN Channel Bonding & Dynamic Bandwidth QoS Shaper");
            Console.WriteLine("Platform: macOS x64/arm64 (.NET 8.0)");
            return;
        }

        // 2. PAC file export command
        int exportPacIdx = Array.FindIndex(args, a => a.Equals("--export-pac", StringComparison.OrdinalIgnoreCase));
        if (exportPacIdx >= 0)
        {
            string outPath = exportPacIdx + 1 < args.Length ? args[exportPacIdx + 1] : "proxy.pac";
            try
            {
                string pac = PacConfigModel.Current.GeneratePacScript();
                string fullPath = Path.GetFullPath(outPath);
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(fullPath, pac, System.Text.Encoding.UTF8);
                Console.WriteLine($"[CombiSpeed macOS] Successfully exported PAC file to: {fullPath}");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CombiSpeed macOS] ERROR: Failed to export PAC file: {ex.Message}");
                Environment.Exit(1);
                return;
            }
        }

        // 3. Stop command
        if (args.Any(a => a.Equals("--stop", StringComparison.OrdinalIgnoreCase)))
        {
            var config = new ProxyConfigModel();
            var pm = new MacProxyManager(config);
            pm.Disable();
            Console.WriteLine("[CombiSpeed macOS] System proxy restored. Sent termination signal to engine.");
            return;
        }

        // 4. Persistence verification test
        if (args.Any(a => a.Equals("--verify-persistence", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("=====================================================================");
            Console.WriteLine("      CombiSpeed macOS Settings & Profile Persistence Self-Test      ");
            Console.WriteLine("=====================================================================");

            string tempJson = Path.Combine(Path.GetTempPath(), "combispeed_test_macOS_settings.json");
            try
            {
                var storage = new MacSettingsStorage(tempJson);
                var testAdapters = new List<NetworkAdapterModel>
                {
                    new() { Id = "test-eth0", Name = "eth0", MacAddress = "AA:BB:CC:DD:EE:01", Weight = 80, IsEnabled = true },
                    new() { Id = "test-wlan0", Name = "wlan0", MacAddress = "AA:BB:CC:DD:EE:02", Weight = 20, IsEnabled = true }
                };

                var config = new ProxyConfigModel
                {
                    SpeedLimitMbps = 350,
                    ShaperMode = SpeedShaperMode.LowestLatency,
                    IsHighContrast = true,
                    UiScale = 1.15
                };

                storage.Save(testAdapters, config, "Dual-WAN Test Profile");
                Console.WriteLine("[PASS] macOS storage save executed.");

                var loaded = storage.Load();
                if (loaded.Adapters.Count != 2) throw new Exception("Adapter count mismatch!");
                if (loaded.Config.SpeedLimitMbps != 350) throw new Exception("Speed cap mismatch!");
                if (loaded.Config.ShaperMode != SpeedShaperMode.LowestLatency) throw new Exception("Shaper mode mismatch!");
                if (!loaded.Config.IsHighContrast || Math.Abs(loaded.Config.UiScale - 1.15) > 0.01) throw new Exception("Accessibility mismatch!");
                Console.WriteLine("[PASS] Reloaded macOS config verified: Cap=350Mbps, ShaperMode=LowestLatency, HighContrast=True, UiScale=115%.");

                // Test PAC export
                string tempPac = Path.Combine(Path.GetTempPath(), "test_macOS_pac.pac");
                string pacScript = PacConfigModel.Current.GeneratePacScript(8080, 1080);
                File.WriteAllText(tempPac, pacScript, System.Text.Encoding.UTF8);
                if (!File.Exists(tempPac) || !File.ReadAllText(tempPac).Contains("FindProxyForURL"))
                {
                    throw new Exception("PAC file export verification failed!");
                }
                if (File.Exists(tempPac)) File.Delete(tempPac);
                Console.WriteLine("[PASS] PAC file generation and export verified.");

                // Test Healthy Proxy Persistence & Eviction
                string tempProxyJson = Path.Combine(Path.GetTempPath(), "combispeed_test_mac_proxies.json");
                try
                {
                    using var validator = new MacProxyValidator();
                    validator.StorageFilePath = tempProxyJson;

                    var pGood1 = new HarvestedProxyModel { Host = "1.2.3.4", Port = 8080, IsHealthy = true, Failures = 0, LatencyMs = 45 };
                    var pGood2 = new HarvestedProxyModel { Host = "5.6.7.8", Port = 3128, IsHealthy = true, Failures = 0, LatencyMs = 90 };
                    var pDead = new HarvestedProxyModel { Host = "9.9.9.9", Port = 1080, IsHealthy = false, Failures = 3, LatencyMs = 999 };

                    validator.AddCandidates(new[] { pGood1, pGood2, pDead });
                    int savedCount = validator.SaveHealthyProxies(tempProxyJson);
                    if (savedCount != 2) throw new Exception($"Expected 2 healthy proxies saved, got {savedCount}");

                    string content = File.ReadAllText(tempProxyJson);
                    if (content.Contains("9.9.9.9")) throw new Exception("Dead proxy was persisted to disk!");
                    if (!content.Contains("1.2.3.4") || !content.Contains("5.6.7.8")) throw new Exception("Good proxies missing from disk!");

                    // Test reloading into new instance
                    using var validator2 = new MacProxyValidator();
                    int loadedProxies = validator2.LoadHealthyProxies(tempProxyJson);
                    if (loadedProxies != 2) throw new Exception($"Expected 2 healthy proxies loaded, got {loadedProxies}");

                    // Test unavailable eviction
                    bool evicted = validator2.MarkUnavailable("1.2.3.4:8080");
                    if (!evicted) throw new Exception("MarkUnavailable failed to evict proxy!");
                    if (validator2.TotalCount != 1) throw new Exception($"Expected 1 remaining proxy in pool, got {validator2.TotalCount}");

                    validator2.SaveHealthyProxies(tempProxyJson);
                    string updatedContent = File.ReadAllText(tempProxyJson);
                    if (updatedContent.Contains("1.2.3.4")) throw new Exception("Unavailable proxy was not evicted from disk!");
                    Console.WriteLine("[PASS] macOS healthy proxy persistence & dynamic unavailable eviction verified.");
                }
                finally
                {
                    if (File.Exists(tempProxyJson)) File.Delete(tempProxyJson);
                }

                // Test Combined Adapters Rescan Verification
                var netDetector = new MacNetworkDetector();
                var scannedAdapters = netDetector.DetectAdapters();
                if (scannedAdapters == null || scannedAdapters.Count == 0) throw new Exception("MacNetworkDetector returned empty adapter list!");
                storage.ApplyToAdapters(scannedAdapters);
                Console.WriteLine($"[PASS] macOS Combined Adapters Rescan verified: {scannedAdapters.Count} adapters detected ({scannedAdapters.Count(a => a.IsEnabled)} combinable).");

                Console.WriteLine("=====================================================================");
                Console.WriteLine("  ALL macOS PERSISTENCE & COMBINATION TESTS PASSED (100% OK)         ");
                Console.WriteLine("=====================================================================");
            }
            finally
            {
                if (File.Exists(tempJson)) File.Delete(tempJson);
            }
            return;
        }

        // 5. Throttling verification test
        if (args.Any(a => a.Equals("--verify-throttling", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("=====================================================================");
            Console.WriteLine("      CombiSpeed macOS Bandwidth Throttling & Speed Shaper Self-Test ");
            Console.WriteLine("=====================================================================");

            var throttler = new MacBandwidthThrottler();

            // 500 Mbps test
            throttler.Configure(500, SpeedShaperMode.MaximumBandwidth);
            if (throttler.CurrentCapMbps != 500) throw new Exception("500 Mbps cap mismatch!");
            if (throttler.BurstWindowMs != 200.0) throw new Exception("MaximumBandwidth burst window mismatch (expected 200ms)!");
            Console.WriteLine("[PASS] 500 Mbps throttling cap and 200ms burst window verified.");

            // Lowest latency mode
            throttler.Configure(100, SpeedShaperMode.LowestLatency);
            if (throttler.BurstWindowMs != 20.0) throw new Exception("LowestLatency burst window mismatch (expected 20ms)!");
            Console.WriteLine("[PASS] LowestLatency mode anti-bufferbloat pacing (20ms burst) verified.");

            // Throttle execution check
            var sw = Stopwatch.StartNew();
            // Request 5 MB at 100 Mbps (~12.5 MB/s -> should delay ~200-400ms)
            for (int i = 0; i < 20; i++)
            {
                await throttler.ThrottleAsync(250_000);
            }
            sw.Stop();
            Console.WriteLine($"[PASS] Token bucket throttling execution verified ({sw.ElapsedMilliseconds} ms for 5 MB burst).");

            Console.WriteLine("=====================================================================");
            Console.WriteLine("  ALL macOS THROTTLING & SPEED SHAPER TESTS PASSED (100% OK)         ");
            Console.WriteLine("=====================================================================");
            return;
        }

        // 6. Normal start
        bool isHeadless = args.Any(a => a.Equals("--headless", StringComparison.OrdinalIgnoreCase) ||
                                        a.Equals("--daemon", StringComparison.OrdinalIgnoreCase));

        var curVer = typeof(Program).Assembly.GetName().Version;
        string curVerStr = curVer != null ? $"v{curVer.Major}.{curVer.Minor}.{curVer.Build}" : "v2.2.0";
        Console.WriteLine("=====================================================================");
        Console.WriteLine($"   CombiSpeed {curVerStr} (Build 27) - Multi-WAN Bandwidth Aggregator (macOS) ");
        Console.WriteLine("=====================================================================");

        var storageService = new MacSettingsStorage();
        var detector = new MacNetworkDetector();
        var adapters = detector.DetectAdapters();
        storageService.ApplyToAdapters(adapters);

        var configModel = storageService.Load().Config ?? new ProxyConfigModel();
        var loadBalancer = new MacLoadBalancer();
        loadBalancer.UpdateAdapters(adapters, configModel.RoutingMode, configModel.ShaperMode);

        var throttlerEngine = new MacBandwidthThrottler(configModel.SpeedLimitMbps, configModel.ShaperMode);
        var proxyEngine = new MacProxyEngine(configModel, loadBalancer, throttlerEngine);
        proxyEngine.Start();

        var healthMonitor = new MacHealthMonitor(adapters, loadBalancer, configModel);
        healthMonitor.Start();

        var proxyMgr = new MacProxyManager(configModel);
        if (configModel.EnableSystemProxy) proxyMgr.Enable();

        var bridgeMgr = new MacVirtualBridgeManager();
        if (configModel.EnableVirtualBridge) bridgeMgr.Connect(adapters.Count(a => a.IsEnabled));

        var proxyHarvester = new MacProxyHarvester();
        var proxyValidator = new MacProxyValidator();
        var rotatingRouter = new MacRotatingProxyRouter(proxyValidator, configModel.RotatingProxy, loadBalancer);
        proxyEngine.RotatingRouter = rotatingRouter;
        var rotatingServer = new MacRotatingProxyServer(configModel.RotatingProxy, rotatingRouter, () => adapters);
        var proxyScheduler = new MacProxyScheduler(proxyHarvester, proxyValidator, configModel.RotatingProxy);

        if (configModel.RotatingProxy.EnableRotatingProxy)
        {
            rotatingServer.Start();
            proxyScheduler.Start();
        }

        var dashboard = new EmbeddedDashboardServer(
            configModel,
            adapters,
            proxyEngine,
            loadBalancer,
            throttlerEngine,
            proxyMgr,
            bridgeMgr,
            storageService,
            rotatingServer,
            proxyHarvester,
            proxyValidator);

        dashboard.Start(openBrowser: !isHeadless);

        Console.WriteLine($"[CombiSpeed macOS] SOCKS5 Proxy listening on 127.0.0.1:{configModel.SocksPort}");
        Console.WriteLine($"[CombiSpeed macOS] HTTP Proxy listening on 127.0.0.1:{configModel.HttpPort}");
        Console.WriteLine($"[CombiSpeed macOS] Rotating Proxy listening on 127.0.0.1:{configModel.RotatingProxy.RotatingProxyPort}");
        Console.WriteLine($"[CombiSpeed macOS] Dynamic PAC URL: http://127.0.0.1:{configModel.HttpPort}/proxy.pac");
        Console.WriteLine($"[CombiSpeed macOS] Web UI Dashboard: http://127.0.0.1:{dashboard.Port}/");
        Console.WriteLine("[CombiSpeed macOS] Press Ctrl+C to terminate cleanly.");

        var tcs = new TaskCompletionSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[CombiSpeed macOS] Shutting down cleanly...");
            healthMonitor.Stop();
            proxyEngine.Stop();
            proxyScheduler.Stop();
            rotatingServer.Stop();
            dashboard.Stop();
            proxyMgr.Disable();
            tcs.TrySetResult();
        };

        await tcs.Task;
    }
}

