using System.Diagnostics;
using System.Windows;
using CombiSpeed.Services;
using CombiSpeed.ViewModels;

namespace CombiSpeed;

public partial class App : Application
{
    public static MainViewModel? ViewModel { get; set; }
    public static bool AutoStart { get; set; }
    public static bool StartMinimized { get; set; }

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
