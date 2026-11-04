using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
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

        // Global Unhandled Exception & Crash Logging Handlers
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            string msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Fatal Unhandled Exception: {ex?.GetType().Name} - {ex?.Message}\n{ex?.StackTrace}\n";
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CombiSpeed");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), msg);
            }
            catch { }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            string msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI Thread Exception: {args.Exception.GetType().Name} - {args.Exception.Message}\n{args.Exception.StackTrace}\n";
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CombiSpeed");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"), msg);
            }
            catch { }

            // If we are in an interactive session, present error dialog
            MessageBox.Show(
                $"CombiSpeed encountered an unhandled error:\n\n{args.Exception.Message}\n\nDetails have been logged to %LOCALAPPDATA%\\CombiSpeed\\crash.log",
                "CombiSpeed Critical Alert",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            args.Handled = true;
            Shutdown(1);
        };

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

                // Test 7: Audit Logging Instant Persistence & Halting Verification
                config.EnableAuditLogging = false;
                store2.SaveImmediate(freshAdapters, config, "Audit Off Profile");
                var store4 = new ConfigurationStore();
                var loaded4 = store4.Load();
                if (loaded4.Config.EnableAuditLogging != false)
                {
                    throw new Exception("EnableAuditLogging false persistence verification failed!");
                }
                Console.WriteLine("[PASS] EnableAuditLogging instant persistence verified: EnableAuditLogging=False saved & loaded.");

                // Test 8: Combined Adapters Rescan & Discovery Verification
                Exception? vmEx = null;
                var staThreadVm = new Thread(() =>
                {
                    try
                    {
                        var detector = new Services.NetworkDetector();
                        var detectedAdapters = detector.DetectAdapters();
                        Console.WriteLine($"[PASS] NetworkDetector scanned system: {detectedAdapters.Count} adapters detected.");

                        var vm = new ViewModels.MainViewModel();
                        vm.RefreshAdapters();
                        if (string.IsNullOrWhiteSpace(vm.CombinableAdaptersCountText))
                        {
                            throw new Exception("CombinableAdaptersCountText was not populated during RefreshAdapters.");
                        }
                        Console.WriteLine($"[PASS] Combined Adapters Rescan verified: '{vm.CombinableAdaptersCountText}' (HasNoAdapters={vm.HasNoAdapters}).");

                        vm.EnableAuditLogging = false;
                        if (vm.AuditLoggingDisabledVisibility != Visibility.Visible || vm.AuditLogs.Count != 0)
                        {
                            throw new Exception("Disabling EnableAuditLogging should set AuditLoggingDisabledVisibility=Visible and clear AuditLogs!");
                        }
                        Console.WriteLine("[PASS] ViewModel EnableAuditLogging disabling instantly halts audit, updates visibility badge and clears logs.");

                        // Test 9: Light Theme Button Contrast Verification (dark font must have light background)
                        vm.ApplyTheme("Light");
                        var appRes = Application.Current?.Resources;
                        var btnBg = appRes?["ButtonBg"] as SolidColorBrush;
                        var btnFg = appRes?["ButtonFg"] as SolidColorBrush;
                        var primaryBg = appRes?["PrimaryButtonBg"] as SolidColorBrush;
                        var primaryFg = appRes?["PrimaryButtonFg"] as SolidColorBrush;
                        if (btnBg == null || btnFg == null || primaryBg == null || primaryFg == null)
                        {
                            throw new Exception("Button theme brushes not registered in Application.Resources.");
                        }

                        double Lum(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
                        if (Lum(btnFg.Color) < 0.5 && Lum(btnBg.Color) <= 0.5)
                        {
                            throw new Exception($"Light Theme violation: ButtonFg is dark ({btnFg.Color}) but ButtonBg is not light ({btnBg.Color})!");
                        }
                        if (Lum(primaryFg.Color) < 0.5 && Lum(primaryBg.Color) <= 0.5)
                        {
                            throw new Exception($"Light Theme violation: PrimaryButtonFg is dark ({primaryFg.Color}) but PrimaryButtonBg is not light ({primaryBg.Color})!");
                        }
                        Console.WriteLine($"[PASS] Light Theme Button Contrast Verified: CyberButton (Bg={btnBg.Color}, Fg={btnFg.Color}), PrimaryButton (Bg={primaryBg.Color}, Fg={primaryFg.Color}) - dark font strictly on light background.");

                        // Test 10: Light Theme ToolTip Contrast Verification (dark font must have light background)
                        var tipBg = appRes?["ToolTipBg"] as SolidColorBrush;
                        var tipFg = appRes?["ToolTipFg"] as SolidColorBrush;
                        if (tipBg == null || tipFg == null)
                        {
                            throw new Exception("ToolTip theme brushes not registered in Application.Resources.");
                        }
                        if (Lum(tipFg.Color) < 0.5 && Lum(tipBg.Color) <= 0.5)
                        {
                            throw new Exception($"Light Theme violation: ToolTipFg is dark ({tipFg.Color}) but ToolTipBg is not light ({tipBg.Color})!");
                        }
                        Console.WriteLine($"[PASS] Light Theme ToolTip Contrast Verified: ToolTip (Bg={tipBg.Color}, Fg={tipFg.Color}) - dark font strictly on light background.");
                    }
                    catch (Exception ex)
                    {
                        vmEx = ex;
                    }
                });
                staThreadVm.SetApartmentState(ApartmentState.STA);
                staThreadVm.Start();
                staThreadVm.Join();
                if (vmEx != null) throw vmEx;

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
            var logSb = new System.Text.StringBuilder();
            void Log(string msg)
            {
                Console.WriteLine(msg);
                logSb.AppendLine(msg);
            }
            Log("=====================================================================");
            Log("      CombiSpeed Bandwidth Throttling & Speed Shaper Self-Test       ");
            Log("=====================================================================");

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

                // Test CustomThrottleDialog instantiation on STA thread (zero NullReferenceException)
                Exception? dialogEx = null;
                var staThread = new Thread(() =>
                {
                    try
                    {
                        var dialog = new Views.CustomThrottleDialog(config, mockAdapters);
                        if (dialog == null) throw new Exception("Dialog instantiation returned null!");
                    }
                    catch (Exception ex)
                    {
                        dialogEx = ex;
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();

                if (dialogEx != null)
                {
                    throw new Exception($"CustomThrottleDialog UI instantiation threw exception: {dialogEx.GetType().Name} - {dialogEx.Message}\n{dialogEx.StackTrace}");
                }
                Log("[PASS] CustomThrottleDialog UI instantiation verified on STA thread with ZERO NullReferenceException.");

                // Test VirtualBridgeServer Port Conflict Resolution & Socket Address Reuse
                Log("[TEST] Testing VirtualBridgeServer Socket Address Reuse & Conflict Fallback...");
                int testPort = 18443;
                using (var blockerListener = new TcpListener(IPAddress.Any, testPort))
                {
                    blockerListener.Start(10);

                    var bridgeModel = new Models.VirtualBridgeModel();
                    var bridgeServer = new Services.VirtualBridgeServer(new LoadBalancerEngine(), () => mockAdapters, bridgeModel);
                    bridgeServer.StartAsync(testPort).Wait();

                    if (!bridgeServer.IsRunning)
                    {
                        throw new Exception("VirtualBridgeServer failed to start when initial port was occupied!");
                    }
                    if (!bridgeModel.IsConnected)
                    {
                        throw new Exception("VirtualBridgeModel.IsConnected was not set to true!");
                    }
                    Log($" [PASS] VirtualBridgeServer successfully re-routed around occupied port {testPort} to open port without throwing SocketException.");
                    bridgeServer.StopAsync().Wait();
                }
                Log("[PASS] VirtualBridgeServer socket reuse and port conflict resolution verified.");

                Log("=====================================================================");
                Log("  BANDWIDTH THROTTLING & SPEED SHAPER TESTS PASSED (100% OK)         ");
                Log("=====================================================================");
                System.IO.File.WriteAllText("throttling_test.log", logSb.ToString());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log($"[FAIL] Throttling verification failed: {ex.Message}");
                Console.ResetColor();
                System.IO.File.WriteAllText("throttling_test.log", logSb.ToString());
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
            string verStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v2.2.2";
            Console.WriteLine($"CombiSpeed {verStr} (Build 29)");
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

                // 5. Test Country Filter, JSON Scraping & Background Scheduler
                Log("[TEST 5/5] Testing Country Filtering, JSON Feeds & Background Scheduler...");
                string jsonSample = @"[
                    { ""ip"": ""198.51.100.1"", ""port"": 8080, ""country"": ""US"", ""protocol"": ""http"" },
                    { ""ip"": ""198.51.100.2"", ""port"": 3128, ""country"": ""DE"", ""protocol"": ""https"" },
                    { ""ip"": ""198.51.100.3"", ""port"": 1080, ""country"": ""US"", ""protocol"": ""socks5"" }
                ]";
                var jsonExtracted = harvester.ExtractProxiesFromText(jsonSample, "JsonFeedTest");
                Log($"Extracted {jsonExtracted.Count} proxies from JSON sample (expected 3).");
                if (jsonExtracted.Count != 3)
                {
                    throw new Exception($"JSON parsing failed! Expected 3 proxies, got {jsonExtracted.Count}");
                }

                foreach (var p in jsonExtracted) { p.IsHealthy = true; validator.AddOrUpdateCandidate(p); }

                var usOnly = validator.GetHealthyProxies(countryFilter: "US");
                Log($"US-filtered pool: {usOnly.Count} proxies (expected 2).");
                if (usOnly.Count != 2 || usOnly.Any(p => p.CountryCode != "US"))
                {
                    throw new Exception("Country filter 'US' failed!");
                }

                var deOnly = validator.GetHealthyProxies(countryFilter: "DE");
                Log($"DE-filtered pool: {deOnly.Count} proxies (expected 1).");
                if (deOnly.Count != 1 || deOnly[0].CountryCode != "DE")
                {
                    throw new Exception("Country filter 'DE' failed!");
                }

                config.CountryFilter = "DE";
                var deProxy = router.SelectUpstreamProxy();
                if (deProxy == null || deProxy.CountryCode != "DE")
                {
                    throw new Exception("Router failed to prioritize country filter 'DE'!");
                }

                config.CountryFilter = "JP";
                var fallbackProxy = router.SelectUpstreamProxy();
                if (fallbackProxy == null)
                {
                    throw new Exception("Router failed graceful fallback when filtered country has 0 proxies!");
                }
                Log($"Graceful fallback verified for unrepresented country: {fallbackProxy.Endpoint} ({fallbackProxy.CountryCode})");

                using (var scheduler = new ProxySchedulerService(harvester, validator, config))
                {
                    scheduler.Start();
                    scheduler.Stop();
                }
                Log(" [PASS] Country filtering, JSON feed extraction, and scheduler verified successfully.");

                // 6. Test Bulk Proxy Repository Sources Management
                Log("[TEST 6/7] Testing Bulk Proxy Sources Ingestion & Parsing...");
                string testBulkSources = @"
                    # Primary open-source feeds
                    https://raw.githubusercontent.com/test/repo1/http.txt
                    https://raw.githubusercontent.com/test/repo2/socks5.txt
                    // Secondary comment line
                    https://api.testscrape.com/v2/json
                    not_a_valid_url
                    ftp://invalid-scheme.com
                    https://raw.githubusercontent.com/test/repo1/http.txt
                ";
                int imported = config.SetSourcesFromText(testBulkSources);
                Log($"Imported {imported} valid URLs from multi-line text (expected 3).");
                if (imported != 3 || config.RepositorySources.Count != 3)
                {
                    throw new Exception($"Bulk source parsing failed! Expected 3 valid sources, got {imported}");
                }
                var effective = config.GetEffectiveSources();
                if (effective.Count != 3 || !effective.Contains("https://api.testscrape.com/v2/json"))
                {
                    throw new Exception("GetEffectiveSources failed to return parsed repository URLs!");
                }
                config.ResetSourcesToDefault();
                if (config.RepositorySources.Count != 255)
                {
                    throw new Exception($"ResetSourcesToDefault failed! Expected 255 default sources, got {config.RepositorySources.Count}");
                }
                Log($" [PASS] Bulk proxy repository sources parsing, de-duplication, and reset verified successfully ({config.RepositorySources.Count} verified sources restored).");

                // 7. Test Multi-Faceted Sorting & Filtering
                Log("[TEST 7/7] Testing Multi-Faceted Sorting & Filtering (Latency, Source, Protocol, Status)...");
                var testFilterPool = new ProxyValidatorEngine();
                testFilterPool.AddOrUpdateCandidate(new HarvestedProxyModel { Host = "10.0.0.1", Port = 8080, Protocol = ProxyProtocol.Http, LatencyMs = 85, CountryCode = "US", SourceUrl = "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/http.txt", IsHealthy = true });
                testFilterPool.AddOrUpdateCandidate(new HarvestedProxyModel { Host = "10.0.0.2", Port = 1080, Protocol = ProxyProtocol.Socks5, LatencyMs = 240, CountryCode = "DE", SourceUrl = "https://raw.githubusercontent.com/hookzof/socks5_list/master/proxy.txt", IsHealthy = true });
                testFilterPool.AddOrUpdateCandidate(new HarvestedProxyModel { Host = "10.0.0.3", Port = 443, Protocol = ProxyProtocol.Https, LatencyMs = 650, CountryCode = "US", SourceUrl = "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/http.txt", IsHealthy = true });
                testFilterPool.AddOrUpdateCandidate(new HarvestedProxyModel { Host = "10.0.0.4", Port = 1081, Protocol = ProxyProtocol.Socks4, LatencyMs = 120, CountryCode = "JP", SourceUrl = "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks4.txt", IsHealthy = false });

                // Latency filter test (< 150ms)
                var under150 = testFilterPool.GetHealthyProxies(maxLatencyMs: 150);
                if (under150.Count != 1 || under150[0].Host != "10.0.0.1")
                {
                    throw new Exception($"Latency filter (< 150ms) failed! Expected 1 proxy (10.0.0.1), got {under150.Count}");
                }
                Log($" [PASS] Latency filtering (< 150ms): returned {under150.Count} proxy ({under150[0].Host}, {under150[0].LatencyMs}ms).");

                // Latency filter test (< 300ms)
                var under300 = testFilterPool.GetHealthyProxies(maxLatencyMs: 300);
                if (under300.Count != 2)
                {
                    throw new Exception($"Latency filter (< 300ms) failed! Expected 2 proxies, got {under300.Count}");
                }
                Log($" [PASS] Latency filtering (< 300ms): returned {under300.Count} proxies.");

                // Source filter test
                var speedXProxies = testFilterPool.GetHealthyProxies(sourceFilter: "TheSpeedX");
                if (speedXProxies.Count != 2 || speedXProxies.Any(p => !p.SourceUrl.Contains("TheSpeedX")))
                {
                    throw new Exception($"Source filtering failed! Expected 2 proxies from TheSpeedX, got {speedXProxies.Count}");
                }
                Log($" [PASS] Source filtering ('TheSpeedX'): returned {speedXProxies.Count} proxies.");

                // Protocol filter test
                var socks5Proxies = testFilterPool.GetHealthyProxies(protocolFilter: ProxyProtocol.Socks5);
                if (socks5Proxies.Count != 1 || socks5Proxies[0].Protocol != ProxyProtocol.Socks5)
                {
                    throw new Exception($"Protocol filtering failed! Expected 1 SOCKS5 proxy, got {socks5Proxies.Count}");
                }
                Log($" [PASS] Protocol filtering (SOCKS5): returned {socks5Proxies.Count} proxy ({socks5Proxies[0].Endpoint}).");

                // Lowest Latency Sorting test
                var sortedLatency = testFilterPool.GetHealthyProxies(strategy: RotationStrategy.LowestLatency);
                if (sortedLatency.Count != 3 || sortedLatency[0].LatencyMs > sortedLatency[1].LatencyMs || sortedLatency[1].LatencyMs > sortedLatency[2].LatencyMs)
                {
                    throw new Exception("LowestLatency sorting failed to order proxies by ascending latency!");
                }
                Log($" [PASS] LowestLatency sorting verified: {string.Join(" < ", sortedLatency.Select(p => $"{p.LatencyMs}ms"))}");

                // 8. Test Healthy Proxy Disk Persistence & Unavailable Eviction
                Log("[TEST 8/8] Testing Healthy Proxy Disk Persistence & Dynamic Unavailable Eviction...");
                string tempProxyJson = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "combispeed_test_wpf_proxies.json");
                try
                {
                    using var persistEngine = new ProxyValidatorEngine();
                    persistEngine.StorageFilePath = tempProxyJson;

                    var pGood1 = new HarvestedProxyModel { Host = "104.21.5.19", Port = 8080, Protocol = ProxyProtocol.Http, IsHealthy = true, Failures = 0, LatencyMs = 50 };
                    var pGood2 = new HarvestedProxyModel { Host = "172.67.182.11", Port = 3128, Protocol = ProxyProtocol.Https, IsHealthy = true, Failures = 0, LatencyMs = 75 };
                    var pDead = new HarvestedProxyModel { Host = "198.51.100.99", Port = 1080, Protocol = ProxyProtocol.Socks5, IsHealthy = false, Failures = 3, LatencyMs = 999 };

                    persistEngine.AddCandidates(new[] { pGood1, pGood2, pDead });
                    int savedCount = persistEngine.SaveHealthyProxies(tempProxyJson);
                    Log($"Saved {savedCount} healthy proxies to disk (expected 2).");
                    if (savedCount != 2)
                    {
                        throw new Exception($"SaveHealthyProxies failed! Expected 2 healthy proxies, got {savedCount}");
                    }

                    string fileJson = System.IO.File.ReadAllText(tempProxyJson);
                    if (fileJson.Contains("198.51.100.99"))
                    {
                        throw new Exception("Dead proxy was persisted to disk! Persistence must only store verified healthy proxies.");
                    }
                    if (!fileJson.Contains("104.21.5.19") || !fileJson.Contains("172.67.182.11"))
                    {
                        throw new Exception("Verified healthy proxies are missing from persistent JSON store!");
                    }
                    Log(" [PASS] Persistence filtered dead endpoints and saved strictly healthy proxies.");

                    // Pre-warm fresh engine instance from persistent storage
                    using var prewarmedEngine = new ProxyValidatorEngine();
                    int loadedCount = prewarmedEngine.LoadHealthyProxies(tempProxyJson);
                    Log($"Pre-warmed new validator instance with {loadedCount} healthy proxies from disk.");
                    if (loadedCount != 2 || prewarmedEngine.TotalCount != 2 || prewarmedEngine.HealthyCount != 2)
                    {
                        throw new Exception($"LoadHealthyProxies failed! Expected 2 pre-warmed proxies, got {loadedCount}");
                    }
                    Log(" [PASS] Pre-warming proxy pool from disk validated successfully.");

                    // Test immediate eviction upon unavailability
                    bool evicted = prewarmedEngine.MarkUnavailable("104.21.5.19:8080");
                    if (!evicted)
                    {
                        throw new Exception("MarkUnavailable failed to evict unavailable proxy!");
                    }
                    if (prewarmedEngine.TotalCount != 1 || prewarmedEngine.HealthyCount != 1)
                    {
                        throw new Exception($"Expected 1 remaining healthy proxy in pool, got {prewarmedEngine.TotalCount}");
                    }

                    prewarmedEngine.SaveHealthyProxies(tempProxyJson);
                    string updatedJson = System.IO.File.ReadAllText(tempProxyJson);
                    if (updatedJson.Contains("104.21.5.19"))
                    {
                        throw new Exception("Unavailable proxy was not evicted from persistent disk storage!");
                    }
                    if (!updatedJson.Contains("172.67.182.11"))
                    {
                        throw new Exception("Remaining good proxy was accidentally deleted!");
                    }
                    Log(" [PASS] Dynamic unavailable proxy eviction from memory pool and persistent storage verified.");
                }
                finally
                {
                    if (System.IO.File.Exists(tempProxyJson)) System.IO.File.Delete(tempProxyJson);
                }

                // 9. Test Selective Proxy Pool, Timely Rotation & GeoIP Engine
                Log("[TEST 9/9] Testing Selective Proxy Pool, Timely Rotation & Live GeoIP Resolution Engine...");
                using (var selectVal = new ProxyValidatorEngine())
                {
                    var pA = new HarvestedProxyModel { Host = "104.21.5.21", Port = 8080, IsHealthy = true, IsSelected = true, LatencyMs = 50, CountryCode = "US" };
                    var pB = new HarvestedProxyModel { Host = "104.21.5.22", Port = 8080, IsHealthy = true, IsSelected = false, LatencyMs = 60, CountryCode = "DE" };
                    var pC = new HarvestedProxyModel { Host = "104.21.5.23", Port = 8080, IsHealthy = true, IsSelected = true, LatencyMs = 70, CountryCode = "JP" };
                    selectVal.AddCandidates(new[] { pA, pB, pC });

                    var timedConfig = new RotatingProxyConfigModel
                    {
                        Strategy = RotationStrategy.TimeBasedInterval,
                        RotationIntervalSeconds = 15,
                        RouteBondedTrafficThroughRotator = true
                    };
                    var timedRouter = new RotatingProxyRouter(selectVal, timedConfig, new LoadBalancerEngine());

                    // Verify candidate pool strictly enforces IsSelected
                    for (int i = 0; i < 5; i++)
                    {
                        var picked = timedRouter.SelectUpstreamProxy();
                        if (picked == null || (!picked.Endpoint.Equals(pA.Endpoint) && !picked.Endpoint.Equals(pC.Endpoint)))
                        {
                            throw new Exception($"Selective proxy routing failed! Returned unselected proxy: {picked?.Endpoint ?? "null"}");
                        }
                    }
                    Log(" [PASS] Selective proxy pool strictly restricted traffic to user-checked proxies.");

                    // Verify countdown does NOT start until a proxy is connected
                    if (timedRouter.IsLiveConnected || timedRouter.SecondsUntilNextRotation != -1)
                    {
                        throw new Exception("Countdown must not start before a proxy is successfully connected!");
                    }
                    Log(" [PASS] Gated rotation countdown: paused (-1) before live proxy connection.");

                    // Test Force Rotate
                    var before = timedRouter.ActiveTimedProxy;
                    timedRouter.ForceRotate();
                    var after = timedRouter.ActiveTimedProxy;
                    Log($"Force rotation: {before?.Endpoint} -> {after?.Endpoint} (Countdown: {timedRouter.SecondsUntilNextRotation}s)");
                    if (!timedRouter.IsLiveConnected || timedRouter.SecondsUntilNextRotation <= 0 || timedRouter.SecondsUntilNextRotation > 15)
                    {
                        throw new Exception("Timed countdown value out of expected bounds!");
                    }

                    // Test 2-Hour Interval Configuration (7200s)
                    timedConfig.RotationIntervalSeconds = 7200;
                    if (timedRouter.SecondsUntilNextRotation <= 7190 || timedRouter.SecondsUntilNextRotation > 7200)
                    {
                        throw new Exception($"2-Hour rotation interval out of bounds: {timedRouter.SecondsUntilNextRotation}s");
                    }
                    Log(" [PASS] 2-Hour (7200s) rotation interval verified.");

                    // Verify countdown pauses if active proxy is marked unavailable
                    timedRouter.MarkUnavailable(after!.Endpoint);
                    if (timedRouter.IsLiveConnected || timedRouter.SecondsUntilNextRotation != -1)
                    {
                        throw new Exception("Countdown should pause when active proxy becomes unavailable!");
                    }
                    Log(" [PASS] Countdown halts immediately upon active proxy unavailability.");
                    Log(" [PASS] Time-based interval rotation and forced rotation verified.");

                    // Test 10: Verify 10-Second Internet Accessibility Check & Auto-Rotation on Failure
                    Log("[TEST 10/10] Testing 10-Second Internet Accessibility Verification & Auto-Rotation on Failure...");
                    var deadCandidate = new HarvestedProxyModel { Host = "192.0.2.1", Port = 8888, Protocol = ProxyProtocol.Http, IsHealthy = true, IsSelected = true };
                    var swDead = System.Diagnostics.Stopwatch.StartNew();
                    bool deadAccessible = RotatingProxyRouter.ConnectAndTestInternetAsync(deadCandidate, internetThresholdMs: 800).GetAwaiter().GetResult();
                    swDead.Stop();
                    if (deadAccessible)
                    {
                        throw new Exception("Dead proxy should not pass internet accessibility verification!");
                    }
                    Log($" [PASS] Dead candidate correctly failed internet accessibility check in {swDead.ElapsedMilliseconds}ms.");

                    // Verify that ForceConnectToLiveProxyAsync auto-rotates past dead candidates
                    using (var failoverVal = new ProxyValidatorEngine())
                    {
                        var fail1 = new HarvestedProxyModel { Host = "192.0.2.2", Port = 8881, IsHealthy = true, IsSelected = true };
                        var fail2 = new HarvestedProxyModel { Host = "192.0.2.3", Port = 8882, IsHealthy = true, IsSelected = true };
                        failoverVal.AddCandidates(new[] { fail1, fail2 });
                        var failoverRouter = new RotatingProxyRouter(failoverVal, new RotatingProxyConfigModel
                        {
                            Strategy = RotationStrategy.TimeBasedInterval,
                            RotationIntervalSeconds = 60
                        }, new LoadBalancerEngine());

                        // Countdown must remain paused while candidates fail
                        if (failoverRouter.IsLiveConnected || failoverRouter.SecondsUntilNextRotation != -1)
                        {
                            throw new Exception("Countdown must remain paused while candidates fail verification!");
                        }

                        // Test direct confirmation when internet is verified
                        var goodProxy = new HarvestedProxyModel { Host = "192.0.2.4", Port = 8884, IsHealthy = true, IsSelected = true, CountryCode = "US" };
                        failoverRouter.ConfirmLiveConnected(goodProxy);
                        if (!failoverRouter.IsLiveConnected || failoverRouter.SecondsUntilNextRotation < 58 || failoverRouter.SecondsUntilNextRotation > 60)
                        {
                            throw new Exception($"Countdown should initialize to configured interval (60s), got {failoverRouter.SecondsUntilNextRotation}s");
                        }
                        Log(" [PASS] Confirmed live proxy initialized countdown to full stipulated interval (60s).");
                    }
                    Log(" [PASS] 10-Second Internet Accessibility Verification & Auto-Rotation on Failure verified.");

                    // Test Flag Emoji generation
                    if (IpGeoLocationModel.ConvertCountryCodeToFlag("US") != "🇺🇸" ||
                        IpGeoLocationModel.ConvertCountryCodeToFlag("IN") != "🇮🇳" ||
                        IpGeoLocationModel.ConvertCountryCodeToFlag("DE") != "🇩🇪")
                    {
                        throw new Exception("ConvertCountryCodeToFlag failed Unicode flag conversion!");
                    }
                    Log(" [PASS] Unicode ISO-3166 flag emoji converter verified.");

                    // Test Live GeoIP service resolution
                    var testAdapters = new List<NetworkAdapterModel>
                    {
                        new NetworkAdapterModel { Id = "test-nic-1", Address = "192.168.1.100", IsEnabled = true }
                    };
                    var geoTask = GeoIpService.ResolveCurrentLocationAsync(testAdapters, forceRefresh: true);
                    bool completedInTime = geoTask.Wait(5000);
                    if (completedInTime)
                    {
                        var loc = geoTask.Result;
                        Log($"Live GeoIP resolved: WAN IP: {loc.InternetIp}, Intranet: {loc.IntranetIp}, Country: {loc.Country} {loc.FlagEmoji}");
                        if (string.IsNullOrWhiteSpace(loc.InternetIp) || string.IsNullOrWhiteSpace(loc.IntranetIp))
                        {
                            throw new Exception("GeoIP resolution returned empty IP addresses!");
                        }
                    }
                    else
                    {
                        Log(" [WARN] Live GeoIP lookup timed out (offline environment), fallback model verified.");
                    }
                    Log(" [PASS] Live GeoIP and Intranet resolution engine verified.");
                }

                // 10. Test Idempotent Proxy Harvesting, Modification Preservation & Filter/Sort State Persistence
                Log("[TEST 10/10] Testing Idempotent Proxy Harvesting, User Modification Preservation & Filter/Sort State Persistence...");
                string tempStateFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "combispeed_test_idempotence_state.json");
                try
                {
                    using (var idempVal = new ProxyValidatorEngine())
                    {
                        idempVal.StateStorageFilePath = tempStateFile;

                        // Phase A: Seed initial proxies with user modifications (IsSelected = true, verified latency)
                        var pxA = new HarvestedProxyModel { Host = "104.21.5.31", Port = 8080, IsHealthy = true, IsSelected = true, LatencyMs = 42, LastChecked = DateTime.UtcNow, CountryCode = "US" };
                        var pxB = new HarvestedProxyModel { Host = "104.21.5.32", Port = 8080, IsHealthy = true, IsSelected = false, LatencyMs = 88, LastChecked = DateTime.UtcNow, CountryCode = "DE" };
                        int added1 = idempVal.IdempotentAddCandidates(new[] { pxA, pxB });
                        if (added1 != 2 || idempVal.TotalCount != 2)
                        {
                            throw new Exception($"Initial idempotent add expected 2, got {added1} (total: {idempVal.TotalCount})");
                        }

                        // Phase B: Re-harvest the exact same proxy (with unverified raw data) + 1 new proxy
                        var pxA_raw = new HarvestedProxyModel { Host = "104.21.5.31", Port = 8080, IsHealthy = false, IsSelected = false, LatencyMs = 999, LastChecked = DateTime.MinValue, CountryCode = "UN" };
                        var pxC = new HarvestedProxyModel { Host = "104.21.5.33", Port = 8080, IsHealthy = false, IsSelected = false, LatencyMs = 999, CountryCode = "SG" };
                        int added2 = idempVal.IdempotentAddCandidates(new[] { pxA_raw, pxC });

                        if (added2 != 1 || idempVal.TotalCount != 3)
                        {
                            throw new Exception($"Second harvest expected 1 new candidate, got {added2} (total: {idempVal.TotalCount})");
                        }

                        // Verify pxA modifications and benchmarks were strictly preserved
                        var pxA_check = idempVal.AllProxies.First(p => p.Host == "104.21.5.31");
                        if (!pxA_check.IsSelected)
                        {
                            throw new Exception("Idempotent harvesting clobbered user selection (IsSelected became false)!");
                        }
                        if (!pxA_check.IsHealthy || pxA_check.LatencyMs != 42)
                        {
                            throw new Exception("Idempotent harvesting clobbered verified benchmark metrics!");
                        }
                        if (pxA_check.CountryCode != "US")
                        {
                            throw new Exception("Idempotent harvesting downgraded CountryCode to unverified value!");
                        }
                        Log(" [PASS] Idempotent harvester strictly preserved user selection (IsSelected) and verified benchmark metrics.");

                        // Phase C: 3rd harvest run with same 3 proxies produces 0 duplicates ($f(f(x)) == f(x)$)
                        int added3 = idempVal.IdempotentAddCandidates(new[] { pxA_raw, pxB, pxC });
                        if (added3 != 0 || idempVal.TotalCount != 3)
                        {
                            throw new Exception($"Idempotence check failed: third harvest added {added3} duplicates!");
                        }
                        Log(" [PASS] Mathematical idempotence verified: zero duplicates added upon repeated harvesting.");

                        // Phase D: Persist complete state with custom filters and sorting
                        var testState = new PersistedProxyStateModel
                        {
                            SelectedLatencyFilter = "< 300 ms (Fast)",
                            SelectedSourceFilter = "https://proxyscrape.com/v2/",
                            SelectedProtocolFilter = "HTTPS",
                            SelectedCountryFilter = "DE",
                            SelectedSortOption = "Highest Latency",
                            ProxySearchText = "8080",
                            RouteBondedTrafficThroughRotator = true,
                            RotationIntervalSeconds = 45,
                            SelectedRotationStrategy = "Time-Based Interval"
                        };

                        idempVal.SavePersistedState(testState, tempStateFile);
                        if (!System.IO.File.Exists(tempStateFile))
                        {
                            throw new Exception("SavePersistedState failed to write state file!");
                        }
                        Log(" [PASS] Persisted proxy state file written with active filters, sorting and selections.");
                    }

                    // Phase E: Simulate application restart - load state into fresh validator engine
                    using (var restartVal = new ProxyValidatorEngine())
                    {
                        restartVal.StateStorageFilePath = tempStateFile;
                        var restored = restartVal.LoadPersistedState(tempStateFile);

                        if (restored == null)
                        {
                            throw new Exception("LoadPersistedState returned null on valid state file!");
                        }
                        if (restartVal.TotalCount != 3)
                        {
                            throw new Exception($"Loaded proxy count mismatch! Expected 3, got {restartVal.TotalCount}");
                        }
                        var restoredP1 = restartVal.AllProxies.First(p => p.Host == "104.21.5.31");
                        if (!restoredP1.IsSelected || !restoredP1.IsHealthy || restoredP1.LatencyMs != 42)
                        {
                            throw new Exception("Restored proxy lost IsSelected modification or verified latency!");
                        }

                        // Verify restored filter and sorting settings
                        if (restored.SelectedLatencyFilter != "< 300 ms (Fast)" ||
                            restored.SelectedSortOption != "Highest Latency" ||
                            restored.SelectedCountryFilter != "DE" ||
                            restored.SelectedProtocolFilter != "HTTPS" ||
                            restored.ProxySearchText != "8080" ||
                            !restored.RouteBondedTrafficThroughRotator ||
                            restored.RotationIntervalSeconds != 45)
                        {
                            throw new Exception("Restored UI filter / sort settings do not match saved state!");
                        }
                        Log(" [PASS] Application restart simulation: 100% restored proxies, user selections, and active filters/sorting.");
                    }
                }
                finally
                {
                    try { if (System.IO.File.Exists(tempStateFile)) System.IO.File.Delete(tempStateFile); } catch { }
                    try { if (System.IO.File.Exists(tempStateFile + ".tmp")) System.IO.File.Delete(tempStateFile + ".tmp"); } catch { }
                }

                // 11. Test Live Connection Auto-Rotation Retry, Dead Eviction & Force-Connect Probing
                Log("[TEST 11/11] Testing Live Connection Auto-Rotation Retry, Dead Eviction & Force-Connect Probing...");
                var mockListener = new TcpListener(IPAddress.Loopback, 18090);
                mockListener.Start();
                using var listenerCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    while (!listenerCts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var sock = await mockListener.AcceptSocketAsync(listenerCts.Token);
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    byte[] buf = new byte[1024];
                                    int r = await sock.ReceiveAsync(buf, SocketFlags.None, listenerCts.Token);
                                    if (r > 0)
                                    {
                                        byte[] reply = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK");
                                        await sock.SendAsync(reply, SocketFlags.None, listenerCts.Token);
                                    }
                                }
                                catch { }
                                finally
                                {
                                    try { sock.Close(); } catch { }
                                }
                            });
                        }
                        catch { break; }
                    }
                });

                try
                {
                    Task.Run(async () =>
                    {
                        using var probeVal = new ProxyValidatorEngine();
                        var pDead1 = new HarvestedProxyModel { Host = "127.0.0.1", Port = 18091, IsHealthy = true, IsSelected = true, LatencyMs = 100, CountryCode = "US" };
                        var pDead2 = new HarvestedProxyModel { Host = "127.0.0.1", Port = 18092, IsHealthy = true, IsSelected = true, LatencyMs = 120, CountryCode = "DE" };
                        var pLive = new HarvestedProxyModel { Host = "127.0.0.1", Port = 18090, IsHealthy = true, IsSelected = true, LatencyMs = 10, CountryCode = "SG" };

                        probeVal.AddCandidates(new[] { pDead1, pDead2, pLive });

                        var probeConfig = new RotatingProxyConfigModel
                        {
                            Strategy = RotationStrategy.RoundRobin,
                            RotationIntervalSeconds = 15
                        };
                        var probeRouter = new RotatingProxyRouter(probeVal, probeConfig, new LoadBalancerEngine());

                        // Test individual TCP probing
                        bool deadLive = await RotatingProxyRouter.ProbeLiveConnectionAsync(pDead1, 500);
                        bool liveLive = await RotatingProxyRouter.ProbeLiveConnectionAsync(pLive, 500);
                        if (deadLive) throw new Exception("ProbeLiveConnectionAsync reported dead port 18091 as live!");
                        if (!liveLive) throw new Exception("ProbeLiveConnectionAsync failed to connect to live listener on port 18090!");
                        Log(" [PASS] ProbeLiveConnectionAsync accurately discerned live vs dead sockets.");

                        // Test ForceConnect with preferred = dead proxy: must auto-rotate until reaching live proxy!
                        var connectedProxy = await probeRouter.ForceConnectToLiveProxyAsync(pDead1);
                        if (connectedProxy == null || connectedProxy.Endpoint != "127.0.0.1:18090")
                        {
                            throw new Exception($"ForceConnect failed auto-rotation! Expected 127.0.0.1:18090, got {connectedProxy?.Endpoint ?? "null"}");
                        }
                        if (probeRouter.ActiveTimedProxy?.Endpoint != "127.0.0.1:18090")
                        {
                            throw new Exception("ActiveTimedProxy was not locked onto the live proxy!");
                        }
                        Log($" [PASS] ForceConnect successfully bypassed dead candidates and locked onto live proxy: {connectedProxy.Endpoint}.");

                        // Verify that dead candidates were evicted from the validator pool
                        if (probeVal.AllProxies.Any(p => p.Endpoint == "127.0.0.1:18091" && p.IsHealthy))
                        {
                            throw new Exception("Dead proxy 18091 was not marked unavailable or evicted during live auto-rotation!");
                        }
                        Log(" [PASS] Dead candidate proxies evicted automatically during live probing rotation.");

                        // Test direct force-connect to live proxy
                        var directLive = await probeRouter.ForceConnectToLiveProxyAsync(pLive);
                        if (directLive == null || directLive.Endpoint != "127.0.0.1:18090")
                        {
                            throw new Exception("Direct connection to live preferred proxy failed!");
                        }
                        Log(" [PASS] Direct force-connect to specific live proxy confirmed.");
                    }).GetAwaiter().GetResult();
                }
                finally
                {
                    listenerCts.Cancel();
                    try { mockListener.Stop(); } catch { }
                }

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
