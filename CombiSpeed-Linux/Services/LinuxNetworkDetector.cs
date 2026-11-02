using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class LinuxNetworkDetector
{
    private static readonly string[] AccentPalette = new[]
    {
        "#00F0FF", // Cyan
        "#7928CA", // Purple
        "#10B981", // Emerald Green
        "#F59E0B", // Amber
        "#EC4899", // Pink
        "#3B82F6", // Blue
        "#14B8A6"  // Teal
    };

    public List<NetworkAdapterModel> DetectAdapters()
    {
        var result = new List<NetworkAdapterModel>();
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        int colorIdx = 0;

        foreach (var ni in interfaces)
        {
            // Filter loopback and down interfaces
            if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                continue;

            if (ni.OperationalStatus != OperationalStatus.Up && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                continue;

            var ipProps = ni.GetIPProperties();
            var ipv4 = ipProps.UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4 == null) continue;

            string ip = ipv4.Address.ToString();
            // Skip link-local 169.254.x.x
            if (ip.StartsWith("169.254.")) continue;

            string gateway = ipProps.GatewayAddresses
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "0.0.0.0";

            string mac = string.Join(":", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));

            var ifaceType = ClassifyInterface(ni.Name, ni.Description, ni.NetworkInterfaceType);

            long speedMbps = ni.Speed > 0 ? ni.Speed / 1_000_000 : 1000;
            if (speedMbps <= 0) speedMbps = 1000;

            result.Add(new NetworkAdapterModel
            {
                Id = ni.Id,
                Name = ni.Name,
                InterfaceName = ni.Name,
                Description = ni.Description,
                IpAddress = ip,
                Gateway = gateway,
                MacAddress = mac,
                InterfaceType = ifaceType,
                LinkSpeedMbps = speedMbps,
                IsEnabled = true,
                Weight = 50,
                PingMs = 12 + (colorIdx * 4),
                JitterMs = 2,
                PacketLossPercent = 0.0,
                AccentColor = AccentPalette[colorIdx % AccentPalette.Length]
            });

            colorIdx++;
        }

        // If running in container or test without physical adapters, provide fallback mock adapters
        if (result.Count == 0)
        {
            result.Add(new NetworkAdapterModel
            {
                Id = "eth0",
                Name = "Ethernet 1 (eth0)",
                InterfaceName = "eth0",
                Description = "Primary Gigabit Ethernet",
                IpAddress = "192.168.1.100",
                Gateway = "192.168.1.1",
                MacAddress = "02:42:AC:11:00:02",
                InterfaceType = Models.NetworkInterfaceType.Ethernet,
                LinkSpeedMbps = 1000,
                IsEnabled = true,
                Weight = 60,
                PingMs = 12,
                AccentColor = AccentPalette[0]
            });

            result.Add(new NetworkAdapterModel
            {
                Id = "wlan0",
                Name = "Wi-Fi 6 (wlan0)",
                InterfaceName = "wlan0",
                Description = "Wireless Network Adapter",
                IpAddress = "192.168.2.105",
                Gateway = "192.168.2.1",
                MacAddress = "02:42:AC:11:00:03",
                InterfaceType = Models.NetworkInterfaceType.Wireless,
                LinkSpeedMbps = 650,
                IsEnabled = true,
                Weight = 40,
                PingMs = 22,
                AccentColor = AccentPalette[1]
            });
        }

        return result;
    }

    private Models.NetworkInterfaceType ClassifyInterface(string name, string desc, System.Net.NetworkInformation.NetworkInterfaceType type)
    {
        string lower = (name + " " + desc).ToLowerInvariant();
        if (lower.Contains("wlan") || lower.Contains("wifi") || lower.Contains("wireless") || lower.Contains("802.11") ||
            type == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
            return Models.NetworkInterfaceType.Wireless;

        if (lower.Contains("wwan") || lower.Contains("cellular") || lower.Contains("lte") || lower.Contains("5g") || lower.Contains("modem"))
            return Models.NetworkInterfaceType.Cellular;

        if (lower.Contains("tun") || lower.Contains("tap") || lower.Contains("docker") || lower.Contains("br-") || lower.Contains("veth"))
            return Models.NetworkInterfaceType.Virtual;

        return Models.NetworkInterfaceType.Ethernet;
    }
}
