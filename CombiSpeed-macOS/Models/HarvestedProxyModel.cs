namespace CombiSpeed.macOS.Models;

public enum ProxyProtocol
{
    Http,
    Https,
    Socks4,
    Socks5
}

public enum ProxyAnonymity
{
    Transparent,
    Anonymous,
    Elite
}

public enum RotationStrategy
{
    RoundRobin,
    StickyDomain,
    StickySession,
    LowestLatency,
    EliteOnly
}

public class HarvestedProxyModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public ProxyProtocol Protocol { get; set; } = ProxyProtocol.Http;
    public ProxyAnonymity Anonymity { get; set; } = ProxyAnonymity.Anonymous;
    public int LatencyMs { get; set; } = 999;
    public double SpeedBps { get; set; }
    public string CountryCode { get; set; } = "UN";
    public bool IsHealthy { get; set; }
    public int Failures { get; set; }
    public int Successes { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.MinValue;
    public string SourceUrl { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }

    public string Endpoint => $"{Host}:{Port}";

    public string FormattedProtocol => Protocol switch
    {
        ProxyProtocol.Http => "HTTP",
        ProxyProtocol.Https => "HTTPS",
        ProxyProtocol.Socks4 => "SOCKS4",
        ProxyProtocol.Socks5 => "SOCKS5",
        _ => "HTTP"
    };

    public string FormattedAnonymity => Anonymity switch
    {
        ProxyAnonymity.Elite => "Elite",
        ProxyAnonymity.Anonymous => "Anonymous",
        ProxyAnonymity.Transparent => "Transparent",
        _ => "Unknown"
    };
}

public class RotatingProxyConfigModel
{
    public bool EnableRotatingProxy { get; set; } = true;
    public int RotatingProxyPort { get; set; } = 8081;
    public RotationStrategy Strategy { get; set; } = RotationStrategy.RoundRobin;
    public int StickySessionMinutes { get; set; } = 5;
    public bool StrictAnonymity { get; set; } = false;
    public int MaxTimeoutMs { get; set; } = 2500;
    public int ConcurrencyLimit { get; set; } = 60;
    public bool AutoHarvestEnabled { get; set; } = false;
    public int AutoHarvestIntervalMinutes { get; set; } = 30;
    public bool AutoValidateEnabled { get; set; } = true;
    public int AutoValidateIntervalMinutes { get; set; } = 5;
    public string CountryFilter { get; set; } = "ALL";
    public int MaxPoolSize { get; set; } = 5000;
    public bool ChainPhysicalAdapters { get; set; } = true;

    public List<string> CustomSources { get; set; } = new();

    public static List<string> GetDefaultSources() => new()
    {
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list-raw.txt",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all"
    };
}

