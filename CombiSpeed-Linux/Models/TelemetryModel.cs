namespace CombiSpeed.Linux.Models;

public class TelemetryModel
{
    public double TotalDownloadKbps { get; set; }
    public double TotalUploadKbps { get; set; }
    public double PeakDownloadKbps { get; set; }
    public double PeakUploadKbps { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytesUploaded { get; set; }
    public int ActiveConnections { get; set; }
    public TimeSpan Uptime { get; set; }
    public bool IsEngineActive { get; set; }
    public double CurrentSpeedCapMbps { get; set; }
    public SpeedShaperMode ShaperMode { get; set; }
    public List<double> WaveformRx { get; set; } = new();
    public List<double> WaveformTx { get; set; } = new();
}

public class AuditLogModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Protocol { get; set; } = "HTTPS";
    public string TargetHost { get; set; } = string.Empty;
    public int TargetPort { get; set; } = 443;
    public string OutboundAdapter { get; set; } = string.Empty;
    public string ClientEndpoint { get; set; } = string.Empty;
    public long BytesTransferred { get; set; }
    public string Status { get; set; } = "Established";
    public string? ProxyServerUrl { get; set; }
    public bool IsSuccess { get; set; } = true;
    public long LatencyMs { get; set; }
    public string? ResolvedIp { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VirtualBridgeModel
{
    public string BridgeName { get; set; } = "combispeed0";
    public bool IsActive { get; set; }
    public int ListenPort { get; set; } = 8443;
    public string Subnet { get; set; } = "10.254.1.0/24";
    public int MergedAdapterCount { get; set; }
    public string SummaryText { get; set; } = "Ready to merge physical Linux adapters.";
}

