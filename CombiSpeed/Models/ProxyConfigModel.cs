namespace CombiSpeed.Models;

public enum RoutingMode
{
    Weighted,
    Equal,
    LeastLatency,
    Failover
}

public enum SpeedShaperMode
{
    MaximumBandwidth,   // Optimizes for peak aggregate throughput, window expansion & dynamic capacity sharing
    LowestLatency,      // Optimizes for minimum ping, gaming, VoIP & anti-bufferbloat jitter reduction
    BalancedDynamic     // Dynamic balanced shaping between high throughput and low latency
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
    public bool CombineSpeedsMode { get; set; } = true; // True: combine connection speeds, never split bandwidth
    public bool EnforceProportionalThrottling { get; set; } = false; // Legacy fallback; false when combining speeds
    public long GlobalDownloadLimitBps { get; set; } = 0; // 0 = Unlimited / Dynamic Combined Pool
    public long GlobalUploadLimitBps { get; set; } = 0;
    public double BurstToleranceSeconds { get; set; } = 0.05; // 50ms default Token Bucket burst window
    public string CustomThrottleUnit { get; set; } = "Mbps"; // "Mbps", "MB/s"
    public SpeedShaperMode ShaperMode { get; set; } = SpeedShaperMode.MaximumBandwidth;
    public bool EnableDynamicShaping { get; set; } = true;

    // Rotating Proxy & Harvester Configuration
    public RotatingProxyConfigModel RotatingProxy { get; set; } = new();

    // Accessibility & UI Display Preferences (WCAG 2.1 AA)
    public bool IsHighContrast { get; set; } = false;
    public double UiScale { get; set; } = 1.0; // 1.0 = 100%, 1.15 = 115%, 1.30 = 130%
    public bool IsReduceMotion { get; set; } = false;
    public bool IsScreenReaderOptimized { get; set; } = false;
}
