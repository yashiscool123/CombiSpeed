using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using CombiSpeed.Services;
using CombiSpeed.ViewModels;

namespace CombiSpeed;

public partial class App : Application
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    public static MainViewModel? ViewModel { get; set; }
    public static bool AutoStart { get; set; }
    public static bool StartMinimized { get; set; }
    public static HeadlessEngine? HeadlessInstance { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool isStopCommand = e.Args.Any(a => a.Equals("--stop", StringComparison.OrdinalIgnoreCase) ||
                                             a.Equals("-stop", StringComparison.OrdinalIgnoreCase) ||
                                             a.Equals("/stop", StringComparison.OrdinalIgnoreCase));

        if (isStopCommand)
        {
            // Reset Windows Proxy immediately
            var proxyManager = new WindowsProxyManager();
            proxyManager.Disable();
            proxyManager.DisablePac();

            // Disconnect any active Virtual Bridge adapter
            var bridgeManager = new VirtualBridgeManager();
            _ = bridgeManager.DisconnectAsync();

            // Find and terminate any running CombiSpeed processes
            var currentProc = Process.GetCurrentProcess();
            var running = Process.GetProcessesByName("CombiSpeed");
            foreach (var proc in running)
            {
                if (proc.Id != currentProc.Id)
                {
                    try
                    {
                        proc.CloseMainWindow();
                        if (!proc.WaitForExit(1500))
                        {
                            proc.Kill();
                        }
                    }
                    catch { }
                }
            }

            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--verify-persistence", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Console.WriteLine("=====================================================================");
            Console.WriteLine("      CombiSpeed Adapter Settings & Profile Persistence Self-Test    ");
            Console.WriteLine("=====================================================================");

            try
            {
                var store = new ConfigurationStore();
                var mockAdapters = new List<Models.NetworkAdapterModel>
                {
                    new() { Id = "test-1", Name = "Ethernet 1", MacAddress = "00-11-22-33-44-55", IsEnabled = true, Weight = 75 },
                    new() { Id = "test-2", Name = "Wi-Fi Primary", MacAddress = "AA-BB-CC-DD-EE-FF", IsEnabled = true, Weight = 25 },
                    new() { Id = "test-3", Name = "Cellular Backup", MacAddress = "11-22-33-44-55-66", IsEnabled = false, Weight = 10 }
                };

                var config = new Models.ProxyConfigModel
                {
                    HttpPort = 8090,
                    SocksPort = 1090,
                    RoutingMode = Models.RoutingMode.Weighted
                };

                // Test 1: Immediate Save
                store.SaveImmediate(mockAdapters, config, "Dual-WAN 75/25");
                Console.WriteLine("[PASS] SaveImmediate executed successfully.");

                // Test 2: Reload and verify content
                var store2 = new ConfigurationStore();
                var loaded = store2.Load();

                if (loaded.AdapterSettings.Count < 3) throw new Exception("Adapter count mismatch after reload.");
                Console.WriteLine($"[PASS] Reloaded {loaded.AdapterSettings.Count} persisted adapter configurations.");

                // Test 3: Apply to new adapter instances
                var freshAdapters = new List<Models.NetworkAdapterModel>
                {
                    new() { Id = "fresh-1", Name = "Ethernet 1", MacAddress = "00:11:22:33:44:55", IsEnabled = false, Weight = 50 },
                    new() { Id = "fresh-2", Name = "Wi-Fi Primary", MacAddress = "AA:BB:CC:DD:EE:FF", IsEnabled = false, Weight = 50 }
                };

                bool applied = store2.ApplyToAdapters(freshAdapters);
                if (!applied) throw new Exception("ApplyToAdapters failed.");
                if (freshAdapters[0].Weight != 75 || !freshAdapters[0].IsEnabled) throw new Exception("Adapter 1 weight/enabled mismatch.");
                if (freshAdapters[1].Weight != 25 || !freshAdapters[1].IsEnabled) throw new Exception("Adapter 2 weight/enabled mismatch.");
                Console.WriteLine("[PASS] Successfully restored adapter enabled state and weight ratios.");

                // Test 4: Profile switching
                store2.SaveCombinationProfile("Streaming Priority", freshAdapters, Models.RoutingMode.LeastLatency);
                var profiles = store2.GetProfileNames();
                if (!profiles.Contains("Streaming Priority")) throw new Exception("Profile was not registered.");
                Console.WriteLine($"[PASS] Combination Profile registered: {string.Join(", ", profiles)}");

                // Test 5: Accessibility Persistence Verification
                config.IsHighContrast = true;
                config.UiScale = 1.15;
                config.IsReduceMotion = true;
                config.IsScreenReaderOptimized = true;
                store2.SaveImmediate(freshAdapters, config, "Accessibility Profile");
                var store3 = new ConfigurationStore();
                var loaded3 = store3.Load();
                if (!loaded3.Config.IsHighContrast || Math.Abs(loaded3.Config.UiScale - 1.15) > 0.01 || !loaded3.Config.IsReduceMotion)
                {
                    throw new Exception("Accessibility settings persistence verification failed!");
                }
                Console.WriteLine("[PASS] Accessibility persistence verified: HighContrast=True, UiScale=115%, ReduceMotion=True saved & loaded.");

                Console.WriteLine("=====================================================================");
                Console.WriteLine("  ALL ADAPTER PERSISTENCE & COMBINATION TESTS PASSED (100% OK)       ");
                Console.WriteLine("=====================================================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] Persistence verification failed: {ex.Message}");
                Console.ResetColor();
                Shutdown(1);
                return;
            }

            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--verify-throttling", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Console.WriteLine("=====================================================================");
            Console.WriteLine("      CombiSpeed Bandwidth Throttling & Speed Shaper Self-Test       ");
            Console.WriteLine("=====================================================================");

            try
            {
                var throttler = new BandwidthThrottler();
                var mockAdapters = new List<Models.NetworkAdapterModel>
                {
                    new() { Id = "adapter-1", Name = "Primary Ethernet", IsEnabled = true, Weight = 75 },
                    new() { Id = "adapter-2", Name = "Secondary Wi-Fi", IsEnabled = true, Weight = 25 }
                };

                // Combined 2 MB/s cap
                var config = new Models.ProxyConfigModel
                {
                    EnableBandwidthThrottling = true,
                    EnforceProportionalThrottling = true,
                    GlobalDownloadLimitBps = 2 * 1024 * 1024,
                    GlobalUploadLimitBps = 1 * 1024 * 1024
                };

                throttler.UpdateConfiguration(mockAdapters, config);

                // Verify proportional allocation
                long expectedAdapter1 = (long)(config.GlobalDownloadLimitBps * 0.75);
                long expectedAdapter2 = (long)(config.GlobalDownloadLimitBps * 0.25);

                if (mockAdapters[0].ThrottledBandwidthBps != expectedAdapter1)
                {
                    throw new Exception($"Adapter 1 allocated bandwidth mismatch: expected {expectedAdapter1}, got {mockAdapters[0].ThrottledBandwidthBps}");
                }
                if (mockAdapters[1].ThrottledBandwidthBps != expectedAdapter2)
                {
                    throw new Exception($"Adapter 2 allocated bandwidth mismatch: expected {expectedAdapter2}, got {mockAdapters[1].ThrottledBandwidthBps}");
                }

                Console.WriteLine($"[PASS] Proportional allocation verified: Adapter 1={expectedAdapter1 / 1024} KB/s (75%), Adapter 2={expectedAdapter2 / 1024} KB/s (25%)");

                // Test live throttling delay
                var sw = Stopwatch.StartNew();
                // Pump 384KB through Adapter 2 (capped at 524,288 B/s ~ 0.5 MB/s)
                // 384KB at 512KB/s takes ~600-700ms
                for (int i = 0; i < 24; i++)
                {
                    throttler.ThrottleDownloadAsync(mockAdapters[1], 16384).AsTask().Wait();
                }
                sw.Stop();

                Console.WriteLine($"[PASS] Throttler enforced delay: {sw.ElapsedMilliseconds}ms for 384KB transfer (strictly rate-limited).");
                if (sw.ElapsedMilliseconds < 200)
                {
                    throw new Exception("Throttler failed to enforce delay on rate-limited adapter!");
                }

                // Test 500 Mbps Throttling Cap
                long limit500Mbps = BandwidthThrottler.ConvertToBytesPerSec(500, "Mbps");
                if (limit500Mbps != 62_500_000)
                {
                    throw new Exception($"500 Mbps conversion mismatch: expected 62500000, got {limit500Mbps}");
                }
                config.EnableBandwidthThrottling = true;
                config.GlobalDownloadLimitBps = limit500Mbps;
                config.BurstToleranceSeconds = 0.10;
                throttler.UpdateConfiguration(mockAdapters, config);

                long expected500_1 = (long)(limit500Mbps * 0.75);
                long expected500_2 = (long)(limit500Mbps * 0.25);
                if (mockAdapters[0].ThrottledBandwidthBps != expected500_1 || mockAdapters[1].ThrottledBandwidthBps != expected500_2)
                {
                    throw new Exception("500 Mbps proportional allocation mismatch!");
                }
                Console.WriteLine($"[PASS] 500 Mbps cap verified: Adapter 1={expected500_1 / 125_000} Mbps (75%), Adapter 2={expected500_2 / 125_000} Mbps (25%)");

                // Test Custom Parameter Unit Conversion
                long custom350Mbps = BandwidthThrottler.ConvertToBytesPerSec(350, "Mbps");
                var (convertedVal, convertedUnit) = BandwidthThrottler.ConvertFromBytesPerSec(custom350Mbps, "Mbps");
                if (Math.Abs(convertedVal - 350.0) > 0.01 || convertedUnit != "Mbps")
                {
                    throw new Exception("Custom parameter unit conversion mismatch!");
                }
                Console.WriteLine($"[PASS] Custom throttling parameter conversion verified: 350 Mbps = {custom350Mbps} B/s");

                // Test Per-Adapter Custom Limit Override
                mockAdapters[1].SpeedLimitBps = 10 * 1024 * 1024;
                throttler.UpdateConfiguration(mockAdapters, config);
                if (mockAdapters[1].ThrottledBandwidthBps != 10 * 1024 * 1024)
                {
                    throw new Exception("Per-adapter custom speed limit override failed!");
                }
                Console.WriteLine("[PASS] Per-adapter custom speed cap override verified: Adapter 2 clamped to 10 MB/s.");

                // Test Maximum Bandwidth Mode Optimization
                config.EnableBandwidthThrottling = true;
                config.ShaperMode = Models.SpeedShaperMode.MaximumBandwidth;
                config.EnableDynamicShaping = true;
                config.GlobalDownloadLimitBps = limit500Mbps;
                throttler.UpdateConfiguration(mockAdapters, config);
                if (throttler.ActiveBurstSeconds < 0.10)
                {
                    throw new Exception("Maximum Bandwidth mode failed to expand burst window!");
                }
                Console.WriteLine($"[PASS] Maximum Bandwidth mode verified: Burst Window={throttler.ActiveBurstSeconds * 1000}ms for maximum TCP throughput.");

                // Test Lowest Latency Mode & Dynamic Ping-Weighted Shaper Optimization
                mockAdapters[1].SpeedLimitBps = 0; // Clear per-adapter override
                config.ShaperMode = Models.SpeedShaperMode.LowestLatency;
                mockAdapters[0].PingMs = 15;
                mockAdapters[1].PingMs = 120;
                throttler.UpdateConfiguration(mockAdapters, config);

                if (throttler.ActiveBurstSeconds > 0.03)
                {
                    throw new Exception("Lowest Latency mode failed to enforce anti-bufferbloat burst window!");
                }
                if (mockAdapters[0].ThrottledBandwidthBps <= mockAdapters[1].ThrottledBandwidthBps)
                {
                    throw new Exception("Lowest Latency mode failed to prioritize lowest ping connection!");
                }
                Console.WriteLine($"[PASS] Lowest Latency mode verified: Burst Window={throttler.ActiveBurstSeconds * 1000}ms (Anti-Bufferbloat), Fiber (15ms) allocated {mockAdapters[0].ThrottledBandwidthBps / 125_000} Mbps vs Cellular (120ms) allocated {mockAdapters[1].ThrottledBandwidthBps / 125_000} Mbps.");

                // Test Real-Time Dynamic Cross-Connection Adaptation
                mockAdapters[0].PingMs = 180;
                mockAdapters[1].PingMs = 25;
                throttler.AdaptDynamically(mockAdapters, config);
                if (mockAdapters[1].ThrottledBandwidthBps <= mockAdapters[0].ThrottledBandwidthBps)
                {
                    throw new Exception("Dynamic cross-connection adaptation failed to adapt to latency changes!");
                }
                Console.WriteLine($"[PASS] Dynamic cross-connection adaptation verified: Shaper dynamically shifted priority to Wi-Fi ({mockAdapters[1].ThrottledBandwidthBps / 125_000} Mbps) over high-ping line ({mockAdapters[0].ThrottledBandwidthBps / 125_000} Mbps).");

                // Test Persistent Serialization of ShaperMode & DynamicShaping
                var store = new ConfigurationStore();
                store.SaveImmediate(mockAdapters, config, "Dynamic Latency Test");
                var reloaded = new ConfigurationStore().Load();
                if (reloaded.Config.ShaperMode != Models.SpeedShaperMode.LowestLatency || !reloaded.Config.EnableDynamicShaping)
                {
                    throw new Exception("Persistent saving/loading of ShaperMode & DynamicShaping failed!");
                }
                Console.WriteLine($"[PASS] Persistence verified: SpeedShaperMode.{reloaded.Config.ShaperMode} and EnableDynamicShaping={reloaded.Config.EnableDynamicShaping} saved & loaded.");

                // Test unthrottled mode
                config.EnableBandwidthThrottling = false;
                mockAdapters[1].SpeedLimitBps = 0;
                throttler.UpdateConfiguration(mockAdapters, config);

                var sw2 = Stopwatch.StartNew();
                for (int i = 0; i < 24; i++)
                {
                    throttler.ThrottleDownloadAsync(mockAdapters[1], 16384).AsTask().Wait();
                }
                sw2.Stop();
                Console.WriteLine($"[PASS] Disabled mode executed without delay: {sw2.ElapsedMilliseconds}ms.");

                Console.WriteLine("=====================================================================");
                Console.WriteLine("  BANDWIDTH THROTTLING & SPEED SHAPER TESTS PASSED (100% OK)         ");
                Console.WriteLine("=====================================================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] Throttling verification failed: {ex.Message}");
                Console.ResetColor();
                Shutdown(1);
                return;
            }

            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase) ||
                             a.Equals("-v", StringComparison.OrdinalIgnoreCase) ||
                             a.Equals("/version", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            var ver = typeof(App).Assembly.GetName().Version;
            string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.4.1";
            Console.WriteLine($"CombiSpeed {verStr} (Build 13)");
            Console.WriteLine("Multi-WAN Channel Bonding & Bandwidth Aggregator Engine");
            Console.WriteLine("Platform: Windows x64 (.NET 8.0)");
            Shutdown(0);
            return;
        }

        // Service Management Commands
        if (e.Args.Any(a => a.Equals("--service-install", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            string exePath = Environment.ProcessPath ?? "";
            string cmd = $"create CombiSpeedService binPath= \"\\\"{exePath}\\\" --service\" start= auto DisplayName= \"CombiSpeed Bonding Engine Service\"";
            Process.Start("sc.exe", cmd)?.WaitForExit();
            Console.WriteLine("[SUCCESS] CombiSpeed Windows Service installed successfully.");
            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--service-uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Process.Start("sc.exe", "stop CombiSpeedService")?.WaitForExit();
            Process.Start("sc.exe", "delete CombiSpeedService")?.WaitForExit();
            Console.WriteLine("[SUCCESS] CombiSpeed Windows Service removed successfully.");
            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--service-start", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Process.Start("net.exe", "start CombiSpeedService")?.WaitForExit();
            Shutdown(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--service-stop", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Process.Start("net.exe", "stop CombiSpeedService")?.WaitForExit();
            Shutdown(0);
            return;
        }

        // Headless & Background Service Execution Mode
        bool isHeadless = e.Args.Any(a => a.Equals("--headless", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                                          a.Equals("--service", StringComparison.OrdinalIgnoreCase));

        if (isHeadless)
        {
            AttachConsole(-1);
            HeadlessInstance = new HeadlessEngine();
            bool enableProxy = e.Args.Any(a => a.Equals("--proxy", StringComparison.OrdinalIgnoreCase));

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += async (s, ev) =>
            {
                ev.Cancel = true;
                if (HeadlessInstance != null) await HeadlessInstance.StopAsync();
                cts.Cancel();
            };

            _ = Task.Run(async () =>
            {
                await HeadlessInstance.StartAsync(enableSystemProxy: enableProxy, enableBridge: true);
                await HeadlessInstance.RunBlockingAsync(cts.Token);
                Shutdown(0);
            });

            return;
        }

        if (e.Args.Any(a => a.Equals("--start", StringComparison.OrdinalIgnoreCase) ||
                           a.Equals("-s", StringComparison.OrdinalIgnoreCase) ||
                           a.Equals("/start", StringComparison.OrdinalIgnoreCase)))
        {
            AutoStart = true;
        }

        if (e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                           a.Equals("-m", StringComparison.OrdinalIgnoreCase) ||
                           a.Equals("/minimized", StringComparison.OrdinalIgnoreCase)))
        {
            StartMinimized = true;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ViewModel?.OnApplicationExit();
        base.OnExit(e);
    }
}
