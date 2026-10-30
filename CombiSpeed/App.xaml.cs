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
