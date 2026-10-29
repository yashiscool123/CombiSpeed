using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CombiSpeed.Services;

public class WindowsProxyManager
{
    private const string InternetSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    private int? _originalProxyEnable;
    private string? _originalProxyServer;

    public bool IsApplied { get; private set; }

    public bool GetSystemProxyStatus(out string server)
    {
        server = "";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, false);
            if (key == null) return false;

            int enabled = (int)(key.GetValue("ProxyEnable", 0) ?? 0);
            server = key.GetValue("ProxyServer", "")?.ToString() ?? "";
            return enabled == 1;
        }
        catch
        {
            return false;
        }
    }

    public bool Enable(int httpPort = 8080, int socksPort = 1080)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true);
            if (key == null) return false;

            // 1. Backup if first time
            if (!_originalProxyEnable.HasValue)
            {
                _originalProxyEnable = (int)(key.GetValue("ProxyEnable", 0) ?? 0);
                _originalProxyServer = key.GetValue("ProxyServer", "")?.ToString();
            }

            // 2. Set proxy for HTTP, HTTPS, SOCKS
            string proxyServer = $"http=127.0.0.1:{httpPort};https=127.0.0.1:{httpPort};socks=127.0.0.1:{socksPort}";

            key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
            key.SetValue("ProxyServer", proxyServer, RegistryValueKind.String);

            // 3. Flush WinINet cache
            RefreshWinINet();
            IsApplied = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable Windows proxy: {ex.Message}");
            return false;
        }
    }

    public bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, true);
            if (key == null) return false;

            if (_originalProxyEnable.HasValue && _originalProxyEnable.Value == 1 && !string.IsNullOrEmpty(_originalProxyServer))
            {
                key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                key.SetValue("ProxyServer", _originalProxyServer, RegistryValueKind.String);
            }
            else
            {
                key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
            }

            RefreshWinINet();
            IsApplied = false;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable Windows proxy: {ex.Message}");
            return false;
        }
    }

    private static void RefreshWinINet()
    {
        try
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        catch { }
    }
}
