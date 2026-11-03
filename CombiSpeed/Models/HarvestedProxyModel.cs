namespace CombiSpeed.Models;

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
    RoundRobin,     // Rotates to a new proxy on every single request
    StickyDomain,   // Retains proxy affinity per target host/domain
    StickySession,  // Retains proxy affinity per client connection for a configurable duration
    LowestLatency,  // Selects from the fastest verified proxies
    EliteOnly       // Restricts rotation to high-anonymity Elite proxies
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

    public string StatusBadgeText => IsHealthy ? $"{LatencyMs} ms" : "FAILED";

    public string StatusColor => IsHealthy
        ? (LatencyMs < 250 ? "#10B981" : LatencyMs < 600 ? "#F59E0B" : "#38BDF8")
        : "#EF4444";
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
    public bool ChainPhysicalAdapters { get; set; } = true; // Binds outgoing proxy sockets across local physical NICs!

    public List<string> CustomSources { get; set; } = new();

    /// <summary>
    /// Complete list of active proxy repository and feed URLs used by the harvester.
    /// </summary>
    public List<string> RepositorySources { get; set; } = GetDefaultSources();

    public static List<string> GetDefaultSources() => new()
    {
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list-raw.txt",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=all&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=all&ssl=yes",
        "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/http.txt",
        "https://raw.githubusercontent.com/officialputuid/KangProxy/KangProxy/http/http.txt",
        "https://raw.githubusercontent.com/officialputuid/KangProxy/KangProxy/https/https.txt",
        "https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/protocols/http/data.txt",
        "https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/protocols/https/data.txt",
        "https://raw.githubusercontent.com/roosterkid/openproxylist/main/HTTPS_RAW.txt",
        "https://raw.githubusercontent.com/mmpx12/proxy-list/master/http.txt",
        "https://raw.githubusercontent.com/mmpx12/proxy-list/master/https.txt",
        "https://raw.githubusercontent.com/hookzof/socks5_list/master/proxy.txt",
        "https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies-https.txt",
        "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/http.txt",
        "https://raw.githubusercontent.com/sunny9577/proxy-scraper/master/proxies.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=5000&country=all&ssl=all&anonymity=elite",
        "https://raw.githubusercontent.com/mohammedcha/ProxRipper/main/full_proxies/https.txt",
        "https://raw.githubusercontent.com/VPSLabCloud/VPSLab-Free-Proxy-List/main/http_ssl.txt",
        "https://raw.githubusercontent.com/Thordata/awesome-free-proxy-list/main/proxies/https.txt",
        "https://raw.githubusercontent.com/iplocate/free-proxy-list/main/protocols/https.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/MostStable/https.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/Stable/https.txt",
        "https://api.openproxylist.xyz/http.txt",
        "https://proxyspace.pro/https.txt",
        "https://www.proxyscan.io/download?type=http",
        "https://www.proxy-list.download/api/v1/get?type=http&anon=elite",
        "https://vakhov.github.io/fresh-proxy-list/http.txt",
        "https://vakhov.github.io/fresh-proxy-list/https.txt",
        "https://raw.githubusercontent.com/Anonym0usWork1221/Free-Proxies/main/proxy_files/http_proxies.txt",
        "https://raw.githubusercontent.com/Anonym0usWork1221/Free-Proxies/main/proxy_files/https_proxies.txt",
        "https://raw.githubusercontent.com/arsscriptum/proxy-list-7/master/http.txt",
        "https://raw.githubusercontent.com/arsscriptum/proxy-list-7/master/https.txt",
        "https://raw.githubusercontent.com/prxchk/proxy-list/main/http.txt",
        "https://raw.githubusercontent.com/HyperBeats/proxy-list/main/http.txt",
        "https://raw.githubusercontent.com/HyperBeats/proxy-list/main/https.txt",
        "https://raw.githubusercontent.com/UptimerBot/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/aslisk/proxyhttps/main/https.txt",
        "https://raw.githubusercontent.com/fahimscirex/proxybd/master/proxylist/http.txt",
        "https://raw.githubusercontent.com/rdavydov/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/proxylist-to/proxy-list/main/http.txt",
        "https://raw.githubusercontent.com/opsxcq/proxy-list/master/list.txt",
        "https://raw.githubusercontent.com/mertguvencli/http-proxy-list/main/proxy-list/data.txt",
        "https://raw.githubusercontent.com/hendrikbgr/Free-Proxy-Repo/master/proxy_list.txt",
        "https://raw.githubusercontent.com/proxy4parsing/proxy-list/main/http.txt",
        "https://multiproxy.org/txt_all/proxy.txt",
        "http://www.proxylists.net/http_highanon.txt",
        "https://sunny9577.github.io/proxy-scraper/proxies.txt",
        "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/https.txt",
        "https://raw.githubusercontent.com/dinoz0rg/proxy-list/main/checked_proxies/http.txt",
        "https://raw.githubusercontent.com/saschazesiger/Free-Proxies/master/proxies/http.txt",
        "https://cdn.jsdelivr.net/gh/databay-labs/free-proxy-list/http.txt",
        "https://cdn.jsdelivr.net/gh/officialputuid/ProxyForEveryone@main/http/http.txt",
        "https://cdn.jsdelivr.net/gh/proxyscrape/free-proxy-list@main/proxies/protocols/http/data.txt",
        "https://cdn.jsdelivr.net/gh/proxyscrape/free-proxy-list@main/proxies/protocols/https/data.txt",
        "https://api.free-proxy-list.net/api/v1/proxy?protocol=http",
        "https://api.free-proxy-list.net/api/v1/proxy?protocol=https",
        "https://api.proxifly.dev/proxy/get?format=json&protocol=http",
        "https://api.proxifly.dev/proxy/get?format=json&protocol=https",
        "https://getproxylist.com/api/proxy?protocol=http",
        "https://getproxylist.com/api/proxy?protocol=https",
        "http://pubproxy.com/api/proxy?format=json&type=http",
        "http://pubproxy.com/api/proxy?format=json&type=https",
        "http://rootjazz.com/proxies/proxies.txt",
        "https://www.sslproxies.org/",
        "https://www.freeproxy.world/?type=http",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=10000&country=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=https&timeout=10000&country=all&ssl=all&anonymity=all",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000",
        "https://api.proxyscrape.com/v3/free-proxy-list/get?request=displayproxies&protocol=http&proxy_format=ipport&format=text",
        "https://api.proxyscrape.com/v3/free-proxy-list/get?request=displayproxies&proxy_format=ipport&format=text&timeout=20000",
        "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/https.txt",
        "https://proxyspace.pro/http.txt",
        "https://www.proxy-list.download/api/v1/get?type=http",
        "https://www.proxy-list.download/api/v2/get?l=en&t=http",
        "https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/https.txt",
        "https://raw.githubusercontent.com/almroot/proxylist/master/list.txt",
        "https://raw.githubusercontent.com/vakhov/fresh-proxy-list/master/http.txt",
        "https://raw.githubusercontent.com/JIH4DHoss4in/PROXY-List/main/http.txt",
        "https://raw.githubusercontent.com/MuRongPIG/Proxy-Master/main/http.txt",
        "https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies-http.txt",
        "https://raw.githubusercontent.com/Zaeem20/FREE_PROXIES_LIST/master/http.txt",
        "https://free-proxy-list.net/",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=US&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=GB&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=DE&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=FR&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=CA&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=NL&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=JP&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=SG&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=AU&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=BR&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=IN&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=RU&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=CN&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=TR&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=PK&ssl=all",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=IR&ssl=all",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=IT",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1000&country=PL",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1000&country=TH",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1000&country=UA",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=EG",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=HU",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=KH",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=KZ",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=MX",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=1500&country=VN",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=2250&country=CL",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=all&timeout=2250&country=HK",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=all&ssl=no",
        "https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/protocols/http/data.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=5000&country=all&ssl=no&anonymity=elite",
        "https://raw.githubusercontent.com/mohammedcha/ProxRipper/main/full_proxies/http.txt",
        "https://raw.githubusercontent.com/Riyoway/proxies/main/http.txt",
        "https://raw.githubusercontent.com/VPSLabCloud/VPSLab-Free-Proxy-List/main/http_all.txt",
        "https://raw.githubusercontent.com/gproxynet/free-proxy-list/main/http.txt",
        "https://raw.githubusercontent.com/Ian-Lusule/Proxies/main/proxies/http.txt",
        "https://raw.githubusercontent.com/Thordata/awesome-free-proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/iplocate/free-proxy-list/main/protocols/http.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/MostStable/http.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/Stable/http.txt",
        "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/http.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=US&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=GB&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=DE&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=FR&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=CA&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=NL&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=JP&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=SG&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=AU&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=BR&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=IN&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=RU&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=http&timeout=3000&country=CN&ssl=no",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=Socks4&timeout=10000&country=all",
        "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/socks4.txt",
        "https://raw.githubusercontent.com/roosterkid/openproxylist/main/SOCKS4_RAW.txt",
        "https://raw.githubusercontent.com/mmpx12/proxy-list/master/socks4.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=Socks4&timeout=10000&country=all&anonymity=elite",
        "https://raw.githubusercontent.com/mohammedcha/ProxRipper/main/full_proxies/socks4.txt",
        "https://raw.githubusercontent.com/Riyoway/proxies/main/socks4.txt",
        "https://raw.githubusercontent.com/VPSLabCloud/VPSLab-Free-Proxy-List/main/socks4_all.txt",
        "https://raw.githubusercontent.com/gproxynet/free-proxy-list/main/socks4.txt",
        "https://raw.githubusercontent.com/Ian-Lusule/Proxies/main/proxies/socks4.txt",
        "https://raw.githubusercontent.com/Thordata/awesome-free-proxy-list/main/proxies/socks4.txt",
        "https://raw.githubusercontent.com/iplocate/free-proxy-list/main/protocols/socks4.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/MostStable/socks4.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/Stable/socks4.txt",
        "https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/protocols/socks4/data.txt",
        "https://api.openproxylist.xyz/socks4.txt",
        "https://www.proxyscan.io/download?type=socks4",
        "https://www.proxy-list.download/api/v1/get?type=socks4&anon=elite",
        "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/socks4.txt",
        "https://raw.githubusercontent.com/Anonym0usWork1221/Free-Proxies/main/proxy_files/socks4_proxies.txt",
        "https://raw.githubusercontent.com/arsscriptum/proxy-list-7/master/socks4.txt",
        "http://www.proxylists.net/socks4.txt",
        "https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies-socks4.txt",
        "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/socks4.txt",
        "https://raw.githubusercontent.com/dinoz0rg/proxy-list/main/checked_proxies/socks4.txt",
        "https://raw.githubusercontent.com/saschazesiger/Free-Proxies/master/proxies/socks4.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks4.txt",
        "https://raw.githubusercontent.com/prxchk/proxy-list/main/socks4.txt",
        "https://cdn.jsdelivr.net/gh/databay-labs/free-proxy-list/socks4.txt",
        "https://cdn.jsdelivr.net/gh/officialputuid/ProxyForEveryone@main/socks4/socks4.txt",
        "https://cdn.jsdelivr.net/gh/proxyscrape/free-proxy-list@main/proxies/protocols/socks4/data.txt",
        "https://api.free-proxy-list.net/api/v1/proxy?protocol=socks4",
        "https://api.proxifly.dev/proxy/get?format=json&protocol=socks4",
        "https://getproxylist.com/api/proxy?protocol=socks4",
        "https://www.proxy-list.download/api/v1/get?type=socks4",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks4&timeout=10000&country=all&ssl=all&anonymity=elite",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=10000",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=5000",
        "https://vakhov.github.io/fresh-proxy-list/socks4.txt",
        "https://proxyspace.pro/socks4.txt",
        "https://raw.githubusercontent.com/vakhov/fresh-proxy-list/master/socks4.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=Socks4&timeout=10000&country=US",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=Socks4&timeout=10000&country=DE",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=Socks4&timeout=10000&country=RU",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=1000&country=CA",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=1000&country=FR",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=1000&country=IN",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=2100&country=NL",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=1500&country=BR",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=1500&country=JP",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=all",
        "https://raw.githubusercontent.com/TheSpeedX/SOCKS-List/master/socks5.txt",
        "https://raw.githubusercontent.com/roosterkid/openproxylist/main/SOCKS5_RAW.txt",
        "https://raw.githubusercontent.com/mmpx12/proxy-list/master/socks5.txt",
        "https://raw.githubusercontent.com/jetkai/proxy-list/main/online-proxies/txt/proxies-socks5.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=all&anonymity=elite",
        "https://raw.githubusercontent.com/mohammedcha/ProxRipper/main/full_proxies/socks5.txt",
        "https://raw.githubusercontent.com/Riyoway/proxies/main/socks5.txt",
        "https://raw.githubusercontent.com/VPSLabCloud/VPSLab-Free-Proxy-List/main/socks5_all.txt",
        "https://raw.githubusercontent.com/gproxynet/free-proxy-list/main/socks5.txt",
        "https://raw.githubusercontent.com/Ian-Lusule/Proxies/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/Thordata/awesome-free-proxy-list/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/iplocate/free-proxy-list/main/protocols/socks5.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/MostStable/socks5.txt",
        "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/Stable/socks5.txt",
        "https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/protocols/socks5/data.txt",
        "https://api.openproxylist.xyz/socks5.txt",
        "https://www.proxy-list.download/api/v1/get?type=socks5&anon=elite",
        "https://raw.githubusercontent.com/ShiftyTR/Proxy-List/master/socks5.txt",
        "https://raw.githubusercontent.com/Anonym0usWork1221/Free-Proxies/main/proxy_files/socks5_proxies.txt",
        "https://raw.githubusercontent.com/arsscriptum/proxy-list-7/master/socks5.txt",
        "http://www.proxylists.net/socks5.txt",
        "https://raw.githubusercontent.com/im-razvan/proxy_list/main/socks5.txt",
        "https://raw.githubusercontent.com/noarche/proxylist-socks5-sock4-exported-updates/main/socks5-online.txt",
        "https://raw.githubusercontent.com/iptotal/free-proxy-list/master/socks5.txt",
        "https://raw.githubusercontent.com/ErcinDedeoglu/proxies/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/B4RC0DE-TM/proxy-list/main/SOCKS5.txt",
        "https://raw.githubusercontent.com/proxylist-to/proxy-list/main/socks5.txt",
        "https://raw.githubusercontent.com/prxchk/proxy-list/main/socks5.txt",
        "https://vakhov.github.io/fresh-proxy-list/socks5.txt",
        "https://sunny9577.github.io/proxy-scraper/generated/socks5_proxies.txt",
        "https://raw.githubusercontent.com/dinoz0rg/proxy-list/main/checked_proxies/socks5.txt",
        "https://raw.githubusercontent.com/saschazesiger/Free-Proxies/master/proxies/socks5.txt",
        "https://raw.githubusercontent.com/BiHuDeH/Proxy-socks-list/main/Socks5.txt",
        "https://raw.githubusercontent.com/BreakingTechFr/Proxy_Free/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/officialputuid/KangProxy/KangProxy/socks5/socks5.txt",
        "https://raw.githubusercontent.com/proxifly/free-proxy-list/main/proxies/protocols/http/socks5.txt",
        "https://raw.githubusercontent.com/zloi-user/hideip.me/main/socks5.txt",
        "https://raw.githubusercontent.com/MuRongPIG/Proxy-Master/main/socks5.txt",
        "https://cdn.jsdelivr.net/gh/databay-labs/free-proxy-list/socks5.txt",
        "https://cdn.jsdelivr.net/gh/officialputuid/ProxyForEveryone@main/socks5/socks5.txt",
        "https://cdn.jsdelivr.net/gh/proxyscrape/free-proxy-list@main/proxies/protocols/socks5/data.txt",
        "https://api.free-proxy-list.net/api/v1/proxy?protocol=socks5",
        "https://api.proxifly.dev/proxy/get?format=json&protocol=socks5",
        "https://getproxylist.com/api/proxy?protocol=socks5",
        "https://www.proxy-list.download/api/v1/get?type=socks5",
        "https://proxyspace.pro/socks5.txt",
        "https://openproxylist.xyz/socks5.txt",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=all&ssl=all&anonymity=elite",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=5000",
        "https://raw.githubusercontent.com/Anonym0usWork1221/Free-Proxies/refs/heads/main/proxy_files/socks5_proxies.txt",
        "https://raw.githubusercontent.com/vakhov/fresh-proxy-list/master/socks5.txt",
        "http://pubproxy.com/api/proxy?format=json&type=socks5",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=US",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=DE",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=RU",
        "https://api.proxyscrape.com/v2/?request=getproxies&protocol=socks5&timeout=10000&country=NL",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=1000&country=CA",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=1000&country=FR",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=1000&country=IN",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=1500&country=BR",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=1500&country=JP",
    };

    /// <summary>
    /// Returns the active combined list of proxy repository sources.
    /// </summary>
    private static readonly HashSet<string> LegacySources = new(StringComparer.OrdinalIgnoreCase)
    {
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/http.txt",
        "https://raw.githubusercontent.com/TheSpeedX/PROXY-List/master/socks5.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/http.txt",
        "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
        "https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list-raw.txt",
        "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=all"
    };

    public List<string> GetEffectiveSources()
    {
        var list = new List<string>();
        if (RepositorySources == null || RepositorySources.Count == 0 ||
            (RepositorySources.Count == 6 && RepositorySources.All(u => LegacySources.Contains(u))))
        {
            list.AddRange(GetDefaultSources());
        }
        else
        {
            list.AddRange(RepositorySources);
        }

        if (CustomSources != null && CustomSources.Count > 0)
        {
            list.AddRange(CustomSources);
        }

        return list.Where(u => !string.IsNullOrWhiteSpace(u))
                   .Select(u => u.Trim())
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();
    }

    /// <summary>
    /// Resets repository sources back to official default open-source feeds.
    /// </summary>
    public void ResetSourcesToDefault()
    {
        RepositorySources = GetDefaultSources();
        CustomSources.Clear();
    }

    /// <summary>
    /// Parses and bulk-imports repository URLs from multi-line text (ignoring empty lines and # comments).
    /// </summary>
    public int SetSourcesFromText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            RepositorySources = new List<string>();
            return 0;
        }

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var validUrls = new List<string>();

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
            {
                continue;
            }

            if (Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                if (!validUrls.Contains(line, StringComparer.OrdinalIgnoreCase))
                {
                    validUrls.Add(line);
                }
            }
        }

        RepositorySources = validUrls;
        return validUrls.Count;
    }

    /// <summary>
    /// Serializes effective sources into multi-line text format.
    /// </summary>
    public string GetSourcesAsText()
    {
        var sources = GetEffectiveSources();
        return string.Join(Environment.NewLine, sources);
    }
}

