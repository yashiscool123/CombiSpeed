using System.Net.NetworkInformation;
using System.Net.Sockets;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class NetworkDetector
{
    private readonly Dictionary<string, NetworkAdapterModel> _cachedAdapters = new();

    private static readonly string[] AccentPalette = { "#00F0FF", "#8B5CF6", "#10B981", "#F59E0B", "#EC4899", "#3B82F6" };

    public List<NetworkAdapterModel> DetectAdapters()
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        var result = new List<NetworkAdapterModel>();
        int colorIdx = 0;

        foreach (var iface in interfaces)
        {
            // Filter inactive, loopback, or tunnel interfaces
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

            var ipProps = iface.GetIPProperties();
            var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(u =>
                u.Address.AddressFamily == AddressFamily.InterNetwork &&
                !u.Address.ToString().StartsWith("127.") &&
                !u.Address.ToString().StartsWith("169.254."));

            if (ipv4 == null) continue;

            string ipAddress = ipv4.Address.ToString();
            string netmask = ipv4.IPv4Mask?.ToString() ?? "255.255.255.0";
            string mac = iface.GetPhysicalAddress().ToString();
            string id = $"adapter_{mac}_{ipAddress.Replace('.', '_')}";

            var type = DetermineAdapterType(iface.NetworkInterfaceType, iface.Name, iface.Description);
            string linkSpeed = FormatLinkSpeed(iface.Speed);

            _cachedAdapters.TryGetValue(id, out var existing);

            var model = new NetworkAdapterModel
            {
                Id = id,
                Name = iface.Name,
                Description = iface.Description,
                Address = ipAddress,
                Netmask = netmask,
                MacAddress = FormatMac(mac),
                Type = type,
                LinkSpeed = linkSpeed,
                Status = AdapterStatus.Up,
                IsEnabled = existing?.IsEnabled ?? true,
                Weight = existing?.Weight ?? 50,
                PingMs = existing?.PingMs ?? 0,
                PacketLossPercent = existing?.PacketLossPercent ?? 0,
                DownloadSpeedBps = existing?.DownloadSpeedBps ?? 0,
                UploadSpeedBps = existing?.UploadSpeedBps ?? 0,
                TotalRxBytes = existing?.TotalRxBytes ?? 0,
                TotalTxBytes = existing?.TotalTxBytes ?? 0,
                WanIp = existing?.WanIp ?? "Resolving...",
                AccentHexColor = AccentPalette[colorIdx % AccentPalette.Length]
            };

            colorIdx++;
            _cachedAdapters[id] = model;
            result.Add(model);
        }

        // Auto-balance weights if all unconfigured
        var enabled = result.Where(a => a.IsEnabled).ToList();
        if (enabled.Count > 0 && enabled.All(a => a.Weight == 0))
        {
            int share = 100 / enabled.Count;
            for (int i = 0; i < enabled.Count; i++)
            {
                enabled[i].Weight = (i == 0) ? (100 - share * (enabled.Count - 1)) : share;
            }
        }

        RecalculateEffectivePercentages(result);
        return result;
    }

    public static void RecalculateEffectivePercentages(IEnumerable<NetworkAdapterModel> adapters)
    {
        var list = adapters.ToList();
        var enabled = list.Where(a => a.IsEnabled).ToList();
        int totalWeight = enabled.Sum(a => a.Weight);

        foreach (var adapter in list)
        {
            if (!adapter.IsEnabled || totalWeight == 0)
            {
                adapter.EffectivePercentage = 0;
            }
            else
            {
                adapter.EffectivePercentage = Math.Round((double)adapter.Weight / totalWeight * 100.0, 1);
            }
        }
    }

    public List<NetworkAdapterModel> GetAllCached() => _cachedAdapters.Values.ToList();

    public void UpdateWeights(Dictionary<string, int> weights)
    {
        foreach (var kvp in weights)
        {
            if (_cachedAdapters.TryGetValue(kvp.Key, out var adapter))
            {
                adapter.Weight = Math.Clamp(kvp.Value, 0, 100);
            }
        }
    }

    private static AdapterType DetermineAdapterType(NetworkInterfaceType ifaceType, string name, string description)
    {
        string combined = $"{name} {description}".ToLowerInvariant();

        if (combined.Contains("wi-fi") || combined.Contains("wifi") || combined.Contains("wireless") ||
            combined.Contains("802.11") || ifaceType == NetworkInterfaceType.Wireless80211)
        {
            return AdapterType.WiFi;
        }

        if (combined.Contains("bridge") || combined.Contains("multiplexor"))
        {
            return AdapterType.Bridge;
        }

        if (combined.Contains("cellular") || combined.Contains("mobile") || combined.Contains("lte") ||
            combined.Contains("wwan") || ifaceType == NetworkInterfaceType.Wwanpp || ifaceType == NetworkInterfaceType.Wwanpp2)
        {
            return AdapterType.Cellular;
        }

        if (combined.Contains("hyper-v") || combined.Contains("vethernet") || combined.Contains("tap") ||
            combined.Contains("vpn") || combined.Contains("virtual"))
        {
            return AdapterType.Virtual;
        }

        if (combined.Contains("ethernet") || combined.Contains("gigabit") || combined.Contains("intel") ||
            combined.Contains("realtek") || ifaceType == NetworkInterfaceType.Ethernet)
        {
            return AdapterType.Ethernet;
        }

        return AdapterType.Other;
    }

    private static string FormatLinkSpeed(long speedBps)
    {
        if (speedBps <= 0) return "Unknown";
        if (speedBps >= 1_000_000_000)
        {
            return $"{speedBps / 1_000_000_000.0:F1} Gbps";
        }
        if (speedBps >= 1_000_000)
        {
            return $"{speedBps / 1_000_000.0:F0} Mbps";
        }
        return $"{speedBps / 1_000.0:F0} Kbps";
    }

    private static string FormatMac(string rawMac)
    {
        if (string.IsNullOrWhiteSpace(rawMac) || rawMac.Length != 12) return rawMac;
        return string.Join("-", Enumerable.Range(0, 6).Select(i => rawMac.Substring(i * 2, 2)));
    }
}
