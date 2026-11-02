namespace CombiSpeed.macOS.Models;

public enum RoutingMode
{
    Weighted,
    EqualShare,
    LeastLatency,
    Failover
}

public enum SpeedShaperMode
{
    MaximumBandwidth,
    LowestLatency,
    BalancedDynamic
}

public class ProxyConfigModel
{
    public int HttpPort { get; set; } = 8080;
    public int SocksPort { get; set; } = 1080;
    public int VirtualBridgePort { get; set; } = 8443;
    public int DashboardPort { get; set; } = 8999;
    public RoutingMode RoutingMode { get; set; } = RoutingMode.Weighted;
    public SpeedShaperMode ShaperMode { get; set; } = SpeedShaperMode.BalancedDynamic;
    
    public double SpeedLimitMbps { get; set; } = 0;
    public double BurstToleranceSeconds { get; set; } = 0.10;
    public bool EnableDynamicAdaptation { get; set; } = true;
    public bool CombineSpeedsMode { get; set; } = true; // True: combine speeds, never split bandwidth
    
    public bool EnableSystemProxy { get; set; } = false;
    public bool EnableVirtualBridge { get; set; } = false;
    public bool EnablePac { get; set; } = false;

    // Accessibility preferences
    public bool IsHighContrast { get; set; } = false;
    public double UiScale { get; set; } = 1.0;
    public bool IsReduceMotion { get; set; } = false;
    public bool IsScreenReaderOptimized { get; set; } = false;
}

