namespace CombiSpeed.Models;

public enum RoutingMode
{
    Weighted,
    Equal,
    LeastLatency,
    Failover
}

public class ProxyConfigModel
{
    public int HttpPort { get; set; } = 8080;
    public int SocksPort { get; set; } = 1080;
    public RoutingMode RoutingMode { get; set; } = RoutingMode.Weighted;
    public bool EnableWindowsProxy { get; set; } = false;
    public string? PrimaryAdapterId { get; set; }
}
