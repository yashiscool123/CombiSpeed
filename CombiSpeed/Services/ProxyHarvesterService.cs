using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

/// <summary>
/// High-throughput harvester and web scraper that collects proxy candidates from open-source repositories,
/// custom URLs, web pages, and local files.
/// </summary>
public class ProxyHarvesterService
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

    // Universal Regex covering IP:PORT, PROTOCOL://IP:PORT, IP:PORT:USER:PASS, and delimited plain text
    private static readonly Regex ProxyRegex = new(
        @"(?:(?<proto>https?|socks[45]):\/\/)?(?<ip>(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)){3})[ \t:;,]+(?<port>\d{2,5})(?:[ \t:;,]+(?<user>[a-zA-Z0-9_\-\.]+)[ \t:;,]+(?<pass>[^\s\r\n]+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // HTML table parser fallback for sites displaying proxies in <td>IP</td><td>PORT</td>
    private static readonly Regex HtmlTableRegex = new(
        @"<td[^>]*>(?<ip>(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)(?:\.(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]\d|\d)){3})<\/td>\s*<td[^>]*>(?<port>\d{2,5})<\/td>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public event Action<string>? OnLog;

    public ProxyHarvesterService()
    {
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) CombiSpeed/2.1.0");
        }
    }

    /// <summary>
    /// Harvests candidate proxies in parallel across all specified URLs (built-in open-source + custom).
    /// </summary>
    public async Task<List<HarvestedProxyModel>> HarvestAllAsync(
        IEnumerable<string> sourceUrls,
        int maxPoolSize = 5000,
        CancellationToken ct = default)
    {
        var distinctSources = sourceUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        OnLog?.Invoke($"[Harvester] Initiating harvest across {distinctSources.Count} proxy sources...");

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

    /// <summary>
    /// Scrapes and extracts proxy endpoints from an arbitrary URL (raw text, API, or HTML page).
    /// </summary>
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
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[Harvester] Error scraping {url}: {ex.Message}");
        }

        return list;
    }

    /// <summary>
    /// Universal proxy parser extracting proxies from raw text, lists, or HTML.
    /// </summary>
    public List<HarvestedProxyModel> ExtractProxiesFromText(string rawText, string sourceIdentifier = "Manual/Local")
    {
        var results = new Dictionary<string, HarvestedProxyModel>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawText)) return results.Values.ToList();

        // 1. Standard regex match
        var matches = ProxyRegex.Matches(rawText);
        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            string ip = match.Groups["ip"].Value.Trim();
            if (!int.TryParse(match.Groups["port"].Value, out int port) || port <= 0 || port > 65535)
                continue;

            // Discard private/loopback IPs
            if (IsPrivateOrLoopbackIp(ip)) continue;

            string key = $"{ip}:{port}";
            if (results.ContainsKey(key)) continue;

            var protocol = DetectProtocol(match.Groups["proto"].Value, sourceIdentifier);

            results[key] = new HarvestedProxyModel
            {
                Host = ip,
                Port = port,
                Protocol = protocol,
                SourceUrl = sourceIdentifier,
                Username = match.Groups["user"].Success ? match.Groups["user"].Value : null,
                Password = match.Groups["pass"].Success ? match.Groups["pass"].Value : null,
                LastChecked = DateTime.MinValue,
                IsHealthy = false
            };
        }

        // 2. HTML table regex parser for pages formatted in <td>IP</td><td>PORT</td>
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

                results[key] = new HarvestedProxyModel
                {
                    Host = ip,
                    Port = port,
                    Protocol = ProxyProtocol.Http,
                    SourceUrl = sourceIdentifier,
                    LastChecked = DateTime.MinValue,
                    IsHealthy = false
                };
            }
        }

        return results.Values.ToList();
    }

    /// <summary>
    /// Imports a list of proxies from a local text or CSV file.
    /// </summary>
    public async Task<List<HarvestedProxyModel>> ImportFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return new List<HarvestedProxyModel>();

        string content = await File.ReadAllTextAsync(filePath, ct);
        var proxies = ExtractProxiesFromText(content, Path.GetFileName(filePath));
        OnLog?.Invoke($"[Harvester] Imported {proxies.Count} proxies from {filePath}");
        return proxies;
    }

    /// <summary>
    /// Exports proxies to a text file in format: IP:PORT or PROTOCOL://IP:PORT.
    /// </summary>
    public async Task ExportToFileAsync(string filePath, IEnumerable<HarvestedProxyModel> proxies, bool workingOnly = true)
    {
        var filtered = proxies.Where(p => !workingOnly || p.IsHealthy).ToList();
        var lines = filtered.Select(p => $"{p.FormattedProtocol.ToLower()}://{p.Endpoint}");

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllLinesAsync(filePath, lines);
        OnLog?.Invoke($"[Harvester] Exported {filtered.Count} proxies to {filePath}");
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
        if (ip.StartsWith("169.254.")) return true; // Link-local
        return false;
    }
}
