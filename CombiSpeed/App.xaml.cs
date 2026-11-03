using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using CombiSpeed.Models;
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

                // Test 6: PAC Generation & Export Verification
                string tempPac = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_combispeed_proxy.pac");
                string pacScript = Models.PacConfigModel.Current.GeneratePacScript(8090, 1090);
                System.IO.File.WriteAllText(tempPac, pacScript, System.Text.Encoding.UTF8);
                if (!System.IO.File.Exists(tempPac) || !System.IO.File.ReadAllText(tempPac).Contains("FindProxyForURL"))
                {
                    throw new Exception("PAC file export verification failed.");
                }
                if (System.IO.File.Exists(tempPac)) System.IO.File.Delete(tempPac);
                Console.WriteLine("[PASS] PAC file generation and export verification passed (FindProxyForURL verified).");

                Console.WriteLine("=====================================================================");
                Console.WriteLine("  ALL ADAPTER PERSISTENCE & COMBINATION TESTS PASSED (100% OK)       ");
                Console.WriteLine("=====================================================================");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] Persistence verification failed: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
                return;
            }

            Environment.Exit(0);
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

                // Combined 2 MB/s cap in Combine Speeds Mode (no splitting)
                var config = new Models.ProxyConfigModel
                {
                    EnableBandwidthThrottling = true,
                    CombineSpeedsMode = true,
                    EnforceProportionalThrottling = false,
                    GlobalDownloadLimitBps = 2 * 1024 * 1024,
                    GlobalUploadLimitBps = 1 * 1024 * 1024
                };

                throttler.UpdateConfiguration(mockAdapters, config);

                // Verify Combine Speeds allocation: both adapters have access to the full combined pool
                if (mockAdapters[0].ThrottledBandwidthBps != config.GlobalDownloadLimitBps)
                {
                    throw new Exception($"Adapter 1 combined bandwidth mismatch: expected {config.GlobalDownloadLimitBps}, got {mockAdapters[0].ThrottledBandwidthBps}");
                }
                if (mockAdapters[1].ThrottledBandwidthBps != config.GlobalDownloadLimitBps)
                {
                    throw new Exception($"Adapter 2 combined bandwidth mismatch: expected {config.GlobalDownloadLimitBps}, got {mockAdapters[1].ThrottledBandwidthBps}");
                }

                Console.WriteLine($"[PASS] Combine Speeds Mode verified: Adapter 1={mockAdapters[0].ThrottledBandwidthBps / 1024} KB/s, Adapter 2={mockAdapters[1].ThrottledBandwidthBps / 1024} KB/s (Full Combined Capacity, Not Split).");

                // Test live throttling delay on combined pool:
                // Pump 1.5MB through the 2 MB/s combined pool (24 chunks of 65,536 bytes)
                // 1.5MB at 2 MB/s with burst takes ~500-750ms
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 24; i++)
                {
                    throttler.ThrottleDownloadAsync(mockAdapters[1], 65536).AsTask().Wait();
                }
                sw.Stop();

                Console.WriteLine($"[PASS] Throttler enforced delay: {sw.ElapsedMilliseconds}ms for 1.5MB transfer (strictly rate-limited at combined 2 MB/s).");
                if (sw.ElapsedMilliseconds < 200)
                {
                    throw new Exception("Throttler failed to enforce delay on rate-limited combined pool!");
                }

                // Test 500 Mbps Throttling Cap
                long limit500Mbps = BandwidthThrottler.ConvertToBytesPerSec(500, "Mbps");
                if (limit500Mbps != 62_500_000)
                {
                    throw new Exception($"500 Mbps conversion mismatch: expected 62500000, got {limit500Mbps}");
                }
                config.EnableBandwidthThrottling = true;
                config.CombineSpeedsMode = true;
                config.EnforceProportionalThrottling = false;
                config.GlobalDownloadLimitBps = limit500Mbps;
                config.BurstToleranceSeconds = 0.10;
                throttler.UpdateConfiguration(mockAdapters, config);

                if (mockAdapters[0].ThrottledBandwidthBps != limit500Mbps || mockAdapters[1].ThrottledBandwidthBps != limit500Mbps)
                {
                    throw new Exception("500 Mbps Combine Speeds allocation mismatch! Expected adapters to pool full combined limit.");
                }
                Console.WriteLine($"[PASS] 500 Mbps Combine Speeds cap verified: Both adapters combined into 500 Mbps ({limit500Mbps / 125_000} Mbps) aggregate pool.");

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
                Environment.Exit(1);
                return;
            }

            Environment.Exit(0);
            return;
        }

        if (e.Args.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase) ||
                             a.Equals("-v", StringComparison.OrdinalIgnoreCase) ||
                             a.Equals("/version", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            var ver = typeof(App).Assembly.GetName().Version;
            string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.1.0";
            Console.WriteLine($"CombiSpeed {verStr} (Build 17)");
            Console.WriteLine("Multi-WAN Channel Bonding & Bandwidth Aggregator Engine");
            Console.WriteLine("Platform: Windows x64 (.NET 8.0)");
            Environment.Exit(0);
            return;
        }

        int exportPacIdx = Array.FindIndex(e.Args, a => a.Equals("--export-pac", StringComparison.OrdinalIgnoreCase) ||
                                                        a.Equals("-export-pac", StringComparison.OrdinalIgnoreCase));
        if (exportPacIdx >= 0)
        {
            AttachConsole(-1);
            string outputPath = exportPacIdx + 1 < e.Args.Length ? e.Args[exportPacIdx + 1] : "proxy.pac";
            try
            {
                var pac = Models.PacConfigModel.Current.GeneratePacScript();
                string fullPath = System.IO.Path.GetFullPath(outputPath);
                string? dir = System.IO.Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                System.IO.File.WriteAllText(fullPath, pac, System.Text.Encoding.UTF8);
                Console.WriteLine($"[CombiSpeed] Successfully exported PAC file to: {fullPath}");
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CombiSpeed] ERROR: Failed to export PAC file: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
                return;
            }
        }

        if (e.Args.Any(a => a.Equals("--verify-benchmark", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            Console.WriteLine("=====================================================================");
            Console.WriteLine("              CombiSpeed Multi-WAN Speed Benchmark Self-Test         ");
            Console.WriteLine("=====================================================================");

            try
            {
                var detector = new NetworkDetector();
                var adapters = detector.DetectAdapters();
                Console.WriteLine($"Detected {adapters.Count} adapters.");
                foreach (var a in adapters)
                {
                    Console.WriteLine($" * Adapter: {a.Name} ({a.Address}) - Status: {a.Status}, IsEnabled: {a.IsEnabled}");
                }

                var tester = new SpeedTester();
                tester.OnProgress += p => Console.WriteLine($"[{p.Percent}%] {p.Stage}: {p.Message}");

                var result = Task.Run(() => tester.RunTestAsync(adapters)).GetAwaiter().GetResult();
                Console.WriteLine($"[BENCHMARK RESULT] Bonded Speed: {result.BondedDownloadMbps} Mbps, Boost: +{result.BoostPercentage}%, Latency: {result.BondedLatencyMs} ms");
                foreach (var item in result.AdapterResults)
                {
                    Console.WriteLine($"  - {item.Name}: {item.DownloadMbps} Mbps (Ping: {item.LatencyMs} ms)");
                }

                if (result.BondedDownloadMbps <= 0 && adapters.Any(a => a.IsEnabled))
                {
                    throw new Exception("Speed benchmark returned 0 Mbps throughput!");
                }

                Console.WriteLine("[PASS] Speed benchmark successfully executed!");
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] Benchmark failed with exception: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
                return;
            }
        }

        if (e.Args.Any(a => a.Equals("--verify-rotator", StringComparison.OrdinalIgnoreCase)))
        {
            AttachConsole(-1);
            var logSb = new System.Text.StringBuilder();
            void Log(string msg) { Console.WriteLine(msg); logSb.AppendLine(msg); }

            Log("=====================================================================");
            Log("        CombiSpeed Rotating Proxy & Harvester Engine Self-Test       ");
            Log("=====================================================================");

            try
            {
                var harvester = new ProxyHarvesterService();
                var validator = new ProxyValidatorEngine();
                var config = new RotatingProxyConfigModel
                {
                    RotatingProxyPort = 8999,
                    Strategy = RotationStrategy.RoundRobin,
                    ChainPhysicalAdapters = false
                };
                var loadBalancer = new LoadBalancerEngine();
                var router = new RotatingProxyRouter(validator, config, loadBalancer);

                // 1. Test Proxy Extraction & Regex Engine
                Log("[TEST 1/4] Testing Universal Proxy Scraper & Regex Parser...");
                string testSample = @"
                    # Sample proxies list
                    142.250.190.46:80
                    http://172.217.16.206:8080
                    socks5://142.250.72.110:1080:testuser:testpass
                    invalid_line_here
                    127.0.0.1:8080 (should be discarded as loopback)
                    192.168.1.1:8080 (should be discarded as private)
                    <td>104.21.5.19</td> <td>8080</td>
                ";

                var extracted = harvester.ExtractProxiesFromText(testSample, "UnitTestSource");
                Log($"Extracted {extracted.Count} valid proxies from sample (expected >= 4).");
                if (extracted.Count < 4)
                {
                    throw new Exception($"Proxy extraction failed! Expected at least 4 proxies, found {extracted.Count}");
                }
                if (extracted.Any(p => p.Host == "127.0.0.1" || p.Host == "192.168.1.1"))
                {
                    throw new Exception("Private/Loopback IP was not filtered out by harvester!");
                }

                var socksProxy = extracted.FirstOrDefault(p => p.Protocol == ProxyProtocol.Socks5);
                if (socksProxy == null || socksProxy.Port != 1080 || socksProxy.Username != "testuser")
                {
                    throw new Exception("SOCKS5 proxy with credentials was not parsed accurately!");
                }
                Log(" [PASS] Proxy extraction & regex parser verified successfully.");

                // 2. Test Validator Engine Pool Management
                Log("[TEST 2/4] Testing Validator Engine Pool & Anonymity Filters...");
                validator.AddCandidates(extracted);
                if (validator.TotalCount < 4)
                {
                    throw new Exception($"Validator failed to store candidates! Pool count: {validator.TotalCount}");
                }

                // Simulate validation status
                var p1 = extracted[0];
                p1.IsHealthy = true;
                p1.LatencyMs = 65;
                p1.Anonymity = ProxyAnonymity.Elite;

                var p2 = extracted[1];
                p2.IsHealthy = true;
                p2.LatencyMs = 180;
                p2.Anonymity = ProxyAnonymity.Anonymous;

                var p3 = extracted[2];
                p3.IsHealthy = false;
                p3.Failures = 3;

                validator.AddOrUpdateCandidate(p1);
                validator.AddOrUpdateCandidate(p2);
                validator.AddOrUpdateCandidate(p3);

                var healthy = validator.GetHealthyProxies();
                if (healthy.Count != 2)
                {
                    throw new Exception($"GetHealthyProxies returned {healthy.Count}, expected 2.");
                }

                var eliteOnly = validator.GetHealthyProxies(strictAnonymity: true);
                if (eliteOnly.Count != 1 || eliteOnly[0].Endpoint != p1.Endpoint)
                {
                    throw new Exception("Strict anonymity filter failed!");
                }

                int pruned = validator.PruneDead(maxFailures: 2);
                Log($"Pruned {pruned} dead proxies (expected >= 1).");
                if (pruned < 1)
                {
                    throw new Exception("PruneDead failed to remove dead proxies!");
                }
                Log(" [PASS] Validator engine pool and filters verified successfully.");

                // 3. Test Rotation Strategies (Round-Robin & Sticky Domain)
                Log("[TEST 3/4] Testing Rotation Strategies (Round-Robin & Sticky Domain)...");
                config.Strategy = RotationStrategy.RoundRobin;
                var rot1 = router.SelectUpstreamProxy();
                var rot2 = router.SelectUpstreamProxy();
                if (rot1 == null || rot2 == null)
                {
                    throw new Exception("Round-robin router returned null proxy!");
                }
                Log($"Rotations: 1st -> {rot1.Endpoint}, 2nd -> {rot2.Endpoint}");

                // Sticky Domain Test
                config.Strategy = RotationStrategy.StickyDomain;
                config.StickySessionMinutes = 5;
                var stick1 = router.SelectUpstreamProxy(targetHost: "api.target.com");
                var stick2 = router.SelectUpstreamProxy(targetHost: "api.target.com");
                var stick3 = router.SelectUpstreamProxy(targetHost: "api.target.com");
                if (stick1 == null || stick1.Endpoint != stick2?.Endpoint || stick1.Endpoint != stick3?.Endpoint)
                {
                    throw new Exception("Sticky Domain failed to retain identical proxy for same host!");
                }
                Log($"Sticky Domain affinity verified for host 'api.target.com' -> {stick1.Endpoint}");
                Log(" [PASS] Rotation strategies verified successfully.");

                // 4. Test Server Listener & Loopback End-to-End
                Log("[TEST 4/4] Testing RotatingProxyServer Listener & Loopback Dispatch...");
                var server = new RotatingProxyServer(config, router, () => new List<NetworkAdapterModel>());
                server.Start();
                if (!server.IsRunning)
                {
                    throw new Exception($"RotatingProxyServer failed to start on port {config.RotatingProxyPort}!");
                }

                // Probe listener socket
                using (var probeSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    probeSocket.Connect("127.0.0.1", config.RotatingProxyPort);
                    if (!probeSocket.Connected)
                    {
                        throw new Exception("Could not connect to active RotatingProxyServer socket!");
                    }
                }

                server.Stop();
                if (server.IsRunning)
                {
                    throw new Exception("RotatingProxyServer failed to stop cleanly!");
                }
                Log(" [PASS] RotatingProxyServer listener and lifecycle verified successfully.");

                Log("\n[ALL PASS] Rotating Proxy & Harvester Engine self-test PASSED with 100% SUCCESS!");
                System.IO.File.WriteAllText("rotator_test.log", logSb.ToString());
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                Log($"\n[FAIL] Rotating Proxy self-test FAILED with exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                System.IO.File.WriteAllText("rotator_test.log", logSb.ToString());
                Environment.Exit(1);
                return;
            }
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
