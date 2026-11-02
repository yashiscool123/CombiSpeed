using System.Diagnostics;
using System.Runtime.InteropServices;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacProxyManager
{
    private readonly ProxyConfigModel _config;

    public MacProxyManager(ProxyConfigModel config)
    {
        _config = config;
    }

    public void Enable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        try
        {
            var services = GetNetworkServices();
            foreach (var svc in services)
            {
                RunCommand("networksetup", $"-setwebproxy \"{svc}\" 127.0.0.1 {_config.HttpPort}");
                RunCommand("networksetup", $"-setsecurewebproxy \"{svc}\" 127.0.0.1 {_config.HttpPort}");
                RunCommand("networksetup", $"-setsocksfirewallproxy \"{svc}\" 127.0.0.1 {_config.SocksPort}");
                RunCommand("networksetup", $"-setwebproxystate \"{svc}\" on");
                RunCommand("networksetup", $"-setsecurewebproxystate \"{svc}\" on");
                RunCommand("networksetup", $"-setsocksfirewallproxystate \"{svc}\" on");
            }

            WriteProxyEnv(true);
        }
        catch { }
    }

    public void Disable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        try
        {
            var services = GetNetworkServices();
            foreach (var svc in services)
            {
                RunCommand("networksetup", $"-setwebproxystate \"{svc}\" off");
                RunCommand("networksetup", $"-setsecurewebproxystate \"{svc}\" off");
                RunCommand("networksetup", $"-setsocksfirewallproxystate \"{svc}\" off");
                RunCommand("networksetup", $"-setautoproxystate \"{svc}\" off");
            }

            WriteProxyEnv(false);
        }
        catch { }
    }

    public void EnablePac(string pacUrl)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        try
        {
            var services = GetNetworkServices();
            foreach (var svc in services)
            {
                RunCommand("networksetup", $"-setautoproxyurl \"{svc}\" \"{pacUrl}\"");
                RunCommand("networksetup", $"-setautoproxystate \"{svc}\" on");
            }
        }
        catch { }
    }

    private List<string> GetNetworkServices()
    {
        var list = new List<string> { "Wi-Fi", "Ethernet", "Thunderbolt Bridge" };
        try
        {
            var psi = new ProcessStartInfo("networksetup", "-listallnetworkservices")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string outStr = p.StandardOutput.ReadToEnd();
                p.WaitForExit(1000);
                var lines = outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.Contains("An asterisk") && !string.IsNullOrWhiteSpace(l))
                    .ToList();
                if (lines.Count > 0) return lines;
            }
        }
        catch { }
        return list;
    }

    private void WriteProxyEnv(bool enable)
    {
        try
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string configDir = Path.Combine(home, ".config", "combispeed");
            Directory.CreateDirectory(configDir);
            string envFile = Path.Combine(configDir, "proxy.env");

            if (enable)
            {
                string envContent = $"export HTTP_PROXY=\"http://127.0.0.1:{_config.HttpPort}\"\n" +
                                   $"export HTTPS_PROXY=\"http://127.0.0.1:{_config.HttpPort}\"\n" +
                                   $"export ALL_PROXY=\"socks5://127.0.0.1:{_config.SocksPort}\"\n" +
                                   $"export http_proxy=\"http://127.0.0.1:{_config.HttpPort}\"\n" +
                                   $"export https_proxy=\"http://127.0.0.1:{_config.HttpPort}\"\n" +
                                   $"export all_proxy=\"socks5://127.0.0.1:{_config.SocksPort}\"\n";
                File.WriteAllText(envFile, envContent);
            }
            else
            {
                if (File.Exists(envFile)) File.Delete(envFile);
            }
        }
        catch { }
    }

    private void RunCommand(string cmd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(1000);
        }
        catch { }
    }
}

