using System.Diagnostics;
using System.Runtime.InteropServices;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxProxyManager
{
    private readonly ProxyConfigModel _config;

    public LinuxProxyManager(ProxyConfigModel config)
    {
        _config = config;
    }

    public void Enable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        try
        {
            // GNOME System Proxy via gsettings
            RunCommand("gsettings", "set org.gnome.system.proxy mode 'manual'");
            RunCommand("gsettings", $"set org.gnome.system.proxy.http host '127.0.0.1'");
            RunCommand("gsettings", $"set org.gnome.system.proxy.http port {_config.HttpPort}");
            RunCommand("gsettings", $"set org.gnome.system.proxy.https host '127.0.0.1'");
            RunCommand("gsettings", $"set org.gnome.system.proxy.https port {_config.HttpPort}");
            RunCommand("gsettings", $"set org.gnome.system.proxy.socks host '127.0.0.1'");
            RunCommand("gsettings", $"set org.gnome.system.proxy.socks port {_config.SocksPort}");

            // Export environment file for bash/zsh
            WriteProxyEnv(true);
        }
        catch { }
    }

    public void Disable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        try
        {
            RunCommand("gsettings", "set org.gnome.system.proxy mode 'none'");
            WriteProxyEnv(false);
        }
        catch { }
    }

    public void EnablePac(string pacUrl)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        try
        {
            RunCommand("gsettings", "set org.gnome.system.proxy mode 'auto'");
            RunCommand("gsettings", $"set org.gnome.system.proxy autoconfig-url '{pacUrl}'");
        }
        catch { }
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

