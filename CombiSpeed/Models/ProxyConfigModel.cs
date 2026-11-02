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
    public bool EnablePac { get; set; } = false;
    public string PacUrl { get; set; } = "http://127.0.0.1:8080/proxy.pac";
    public bool EnableVirtualBridge { get; set; } = true;
    public string VirtualAdapterName { get; set; } = "CombiSpeed Bonded Bridge";
    public int VirtualBridgePort { get; set; } = 8443;
    public string? PrimaryAdapterId { get; set; }

    // Bandwidth Throttling & Traffic Shaping
    public bool EnableBandwidthThrottling { get; set; } = true;
    public bool EnforceProportionalThrottling { get; set; } = true;
    public long GlobalDownloadLimitBps { get; set; } = 0; // 0 = Unlimited / Dynamic Proportional
    public long GlobalUploadLimitBps { get; set; } = 0;
    public double BurstToleranceSeconds { get; set; } = 0.05; // 50ms default Token Bucket burst window
    public string CustomThrottleUnit { get; set; } = "Mbps"; // "Mbps", "MB/s"
}
