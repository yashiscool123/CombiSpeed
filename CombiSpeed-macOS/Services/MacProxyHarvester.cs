using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacProxyHarvester
{
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(8),
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        UseProxy = false,
        Proxy = null
    })
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private static readonly Regex ProxyRegex = new(
        @"(?:(?<proto>https?|socks[45]):\/\/)?(?<ip>(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)){3})[ \t:;,]+(?<port>\d{2,5})(?:[ \t:;,]+(?<user>[a-zA-Z0-9_\-\.]+)[ \t:;,]+(?<pass>[^\s\r\n]+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex HtmlTableRegex = new(
        @"<td[^>]*>(?<ip>(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)){3})<\/td>\s*<td[^>]*>(?<port>\d{2,5})<\/td>(?:\s*<td[^>]*>(?<country>[a-zA-Z]{2})<\/td>)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public event Action<string>? OnLog;

    public MacProxyHarvester()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) CombiSpeed/2.1.1");
        }
    }

    public async Task<List<HarvestedProxyModel>> HarvestAllAsync(
        IEnumerable<string> sourceUrls,
        int maxPoolSize = 5000,
        CancellationToken ct = default)
    {
        var distinctSources = sourceUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        OnLog?.Invoke($"[Harvester] Harvesting from {distinctSources.Count} sources...");

        var candidates = new Dictionary<string, HarvestedProxyModel>(StringComparer.OrdinalIgnoreCase);
        var tasks = distinctSources.Select(url => ScrapeUrlAsync(url, ct)).ToList();

        var results = await Task.WhenAll(tasks);

        foreach (var list in results)
        {
            foreach (var proxy in list)
            {
                if (candidates.Count >= maxPoolSize) break;
                candidates.TryAdd(proxy.Endpoint, proxy);
            }
            if (candidates.Count >= maxPoolSize) break;
        }

        OnLog?.Invoke($"[Harvester] Completed harvest. Extracted {candidates.Count} unique candidate proxies.");
        return candidates.Values.ToList();
    }

    public async Task<List<HarvestedProxyModel>> ScrapeUrlAsync(string url, CancellationToken ct = default)
    {
        var list = new List<HarvestedProxyModel>();
        if (string.IsNullOrWhiteSpace(url)) return list;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Accept", "text/plain,text/html,application/json,*/*");

            using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                OnLog?.Invoke($"[Harvester] HTTP {(int)resp.StatusCode} for {url}");
                return list;
            }

            string content = await resp.Content.ReadAsStringAsync(ct);
            list = ExtractProxiesFromText(content, url);
            OnLog?.Invoke($"[Harvester] Scraped {list.Count} proxies from {url}");
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[Harvester] Error scraping {url}: {ex.Message}");
        }

        return list;
    }

    public List<HarvestedProxyModel> ExtractProxiesFromText(string rawText, string sourceIdentifier = "Manual/Local")
    {
        var results = new Dictionary<string, HarvestedProxyModel>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawText)) return results.Values.ToList();

        string defaultCountry = "UN";
        if (sourceIdentifier.Contains("country=", StringComparison.OrdinalIgnoreCase))
        {
            var cm = Regex.Match(sourceIdentifier, @"country=([a-zA-Z]{2})", RegexOptions.IgnoreCase);
            if (cm.Success) defaultCountry = cm.Groups[1].Value.ToUpperInvariant();
        }

        // 0. JSON array or object parser for modern proxy APIs
        string trimmed = rawText.TrimStart();
        if ((trimmed.StartsWith("[") || trimmed.StartsWith("{")) && trimmed.Contains("\"ip\"", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rawText);
                var root = doc.RootElement;
                var array = root.ValueKind == System.Text.Json.JsonValueKind.Array
                    ? root.EnumerateArray()
                    : root.TryGetProperty("data", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.Array
                        ? d.EnumerateArray()
                        : root.TryGetProperty("proxies", out var p) && p.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? p.EnumerateArray()
                            : System.Linq.Enumerable.Empty<System.Text.Json.JsonElement>();

                foreach (var item in array)
                {
                    string? ip = null;
                    if (item.TryGetProperty("ip", out var ipEl)) ip = ipEl.GetString();
                    else if (item.TryGetProperty("host", out var hEl)) ip = hEl.GetString();

                    int port = 0;
                    if (item.TryGetProperty("port", out var portEl))
                    {
                        if (portEl.ValueKind == System.Text.Json.JsonValueKind.Number) port = portEl.GetInt32();
                        else if (portEl.ValueKind == System.Text.Json.JsonValueKind.String) _ = int.TryParse(portEl.GetString(), out port);
                    }

                    if (string.IsNullOrWhiteSpace(ip) || port <= 0 || port > 65535 || IsPrivateOrLoopbackIp(ip))
                        continue;

                    string country = defaultCountry;
                    if (item.TryGetProperty("country", out var cEl)) country = cEl.GetString()?.ToUpperInvariant() ?? defaultCountry;
                    else if (item.TryGetProperty("countryCode", out var ccEl)) country = ccEl.GetString()?.ToUpperInvariant() ?? defaultCountry;

                    var proto = ProxyProtocol.Http;
                    if (item.TryGetProperty("protocol", out var prEl)) proto = DetectProtocol(prEl.GetString() ?? "", sourceIdentifier);
                    else if (item.TryGetProperty("protocols", out var prsEl) && prsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var first = prsEl.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == System.Text.Json.JsonValueKind.String) proto = DetectProtocol(first.GetString() ?? "", sourceIdentifier);
                    }

                    string key = $"{ip}:{port}";
                    results[key] = new HarvestedProxyModel
                    {
                        Host = ip,
                        Port = port,
                        Protocol = proto,
                        CountryCode = country,
                        SourceUrl = sourceIdentifier,
                        LastChecked = DateTime.MinValue,
                        IsHealthy = false
                    };
                }
            }
            catch { }
        }

        var matches = ProxyRegex.Matches(rawText);
        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            string ip = match.Groups["ip"].Value.Trim();
            if (!int.TryParse(match.Groups["port"].Value, out int port) || port <= 0 || port > 65535)
                continue;

            if (IsPrivateOrLoopbackIp(ip)) continue;

            string key = $"{ip}:{port}";
            if (results.ContainsKey(key)) continue;

            var protocol = DetectProtocol(match.Groups["proto"].Value, sourceIdentifier);

            results[key] = new HarvestedProxyModel
            {
                Host = ip,
                Port = port,
                Protocol = protocol,
                CountryCode = defaultCountry,
                SourceUrl = sourceIdentifier,
                Username = match.Groups["user"].Success ? match.Groups["user"].Value : null,
                Password = match.Groups["pass"].Success ? match.Groups["pass"].Value : null,
                LastChecked = DateTime.MinValue,
                IsHealthy = false
            };
        }

        if (rawText.Contains("<td", StringComparison.OrdinalIgnoreCase))
        {
            var tableMatches = HtmlTableRegex.Matches(rawText);
            foreach (Match match in tableMatches)
            {
                if (!match.Success) continue;

                string ip = match.Groups["ip"].Value.Trim();
                if (!int.TryParse(match.Groups["port"].Value, out int port) || port <= 0 || port > 65535)
                    continue;

                if (IsPrivateOrLoopbackIp(ip)) continue;

                string key = $"{ip}:{port}";
                if (results.ContainsKey(key)) continue;

                string country = match.Groups["country"].Success
                    ? match.Groups["country"].Value.ToUpperInvariant()
                    : defaultCountry;

                results[key] = new HarvestedProxyModel
                {
                    Host = ip,
                    Port = port,
                    Protocol = ProxyProtocol.Http,
                    CountryCode = country,
                    SourceUrl = sourceIdentifier,
                    LastChecked = DateTime.MinValue,
                    IsHealthy = false
                };
            }
        }

        return results.Values.ToList();
    }

    public async Task<List<HarvestedProxyModel>> ImportFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return new List<HarvestedProxyModel>();
        string content = await File.ReadAllTextAsync(filePath, ct);
        return ExtractProxiesFromText(content, Path.GetFileName(filePath));
    }

    public async Task ExportToFileAsync(string filePath, IEnumerable<HarvestedProxyModel> proxies, bool workingOnly = true)
    {
        var filtered = proxies.Where(p => !workingOnly || p.IsHealthy).ToList();
        var lines = filtered.Select(p => $"{p.FormattedProtocol.ToLower()}://{p.Endpoint}");
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllLinesAsync(filePath, lines);
    }

    private static ProxyProtocol DetectProtocol(string protoStr, string sourceIdentifier)
    {
        if (!string.IsNullOrEmpty(protoStr))
        {
            if (protoStr.Equals("socks5", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Socks5;
            if (protoStr.Equals("socks4", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Socks4;
            if (protoStr.Equals("https", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Https;
            return ProxyProtocol.Http;
        }

        if (sourceIdentifier.Contains("socks5", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Socks5;
        if (sourceIdentifier.Contains("socks4", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Socks4;
        if (sourceIdentifier.Contains("https", StringComparison.OrdinalIgnoreCase)) return ProxyProtocol.Https;

        return ProxyProtocol.Http;
    }

    private static bool IsPrivateOrLoopbackIp(string ip)
    {
        if (ip.StartsWith("127.") || ip == "0.0.0.0" || ip == "localhost") return true;
        if (ip.StartsWith("10.")) return true;
        if (ip.StartsWith("192.168.")) return true;
        if (ip.StartsWith("172."))
        {
            var parts = ip.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out int secondOctet))
            {
                if (secondOctet >= 16 && secondOctet <= 31) return true;
            }
        }
        if (ip.StartsWith("169.254.")) return true;
        return false;
    }
}

