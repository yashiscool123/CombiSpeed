using System.Text.Json.Serialization;

namespace CombiSpeed.macOS.Models;

public enum NetworkInterfaceType
{
    Ethernet,
    Wireless,
    Cellular,
    Thunderbolt,
    Virtual,
    Other
}

public class NetworkAdapterModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string InterfaceName { get; set; } = string.Empty; // e.g. en0, en1
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

    public string WanIp { get; set; } = "Resolving...";
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "UN";
    public string City { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public string FlagEmoji => IpGeoLocationModel.ConvertCountryCodeToFlag(CountryCode);
    public string FormattedLocation => !string.IsNullOrWhiteSpace(City)
        ? $"{City}, {Country} {FlagEmoji}"
        : (!string.IsNullOrWhiteSpace(Country) ? $"{Country} {FlagEmoji}" : "Global / UN");
}
