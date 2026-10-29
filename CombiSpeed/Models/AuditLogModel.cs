namespace CombiSpeed.Models;

public class AuditLogModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Protocol { get; set; } = "HTTP";
    public string TargetHost { get; set; } = string.Empty;
    public int TargetPort { get; set; }
    public string AdapterId { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public string ClientIp { get; set; } = "127.0.0.1";
    public long BytesTransferred { get; set; }
    public long DurationMs { get; set; }
    public string Status { get; set; } = "Completed";
    public string? ErrorMessage { get; set; }

    public string TimeFormatted => Timestamp.ToString("HH:mm:ss");
    public string VolumeFormatted => NetworkAdapterModel.FormatBytes(BytesTransferred);
    public string DestinationFormatted => $"{TargetHost}:{TargetPort}";
}
