using System.Text.Json.Serialization;

namespace CombiSpeed.Linux.Models;

public enum NetworkInterfaceType
{
    Ethernet,
    Wireless,
    Cellular,
    Virtual,
    Other
}

public class NetworkAdapterModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string InterfaceName { get; set; } = string.Empty; // e.g. eth0, wlan0
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public NetworkInterfaceType InterfaceType { get; set; } = NetworkInterfaceType.Ethernet;
    public long LinkSpeedMbps { get; set; } = 1000;
    public bool IsEnabled { get; set; } = true;
    public int Weight { get; set; } = 50;
    public double EffectivePercentage { get; set; } = 50.0;
    public int PingMs { get; set; } = 15;
    public int JitterMs { get; set; } = 2;
    public double PacketLossPercent { get; set; } = 0.0;
    public double DownloadSpeedKbps { get; set; } = 0.0;
    public double UploadSpeedKbps { get; set; } = 0.0;
    public double? CustomSpeedLimitMbps { get; set; }
    public string AccentColor { get; set; } = "#00F0FF";
}
