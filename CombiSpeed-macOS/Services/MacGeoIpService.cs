using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public static class MacGeoIpService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(4) };
    private static IpGeoLocationModel _cachedLocation = new();
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _resolveLock = new(1, 1);

    public static async Task<IpGeoLocationModel> ResolveCurrentLocationAsync(
        IEnumerable<NetworkAdapterModel> adapters, 
        HarvestedProxyModel? proxy = null, 
        bool forceRefresh = false, 
        CancellationToken token = default)
    {
        if (!forceRefresh && DateTime.UtcNow < _cacheExpiry && _cachedLocation.InternetIp != "Resolving..." && proxy == null)
        {
            UpdateIntranetIp(_cachedLocation, adapters);
            return _cachedLocation;
        }

        if (!await _resolveLock.WaitAsync(200, token))
        {
            UpdateIntranetIp(_cachedLocation, adapters);
            return _cachedLocation;
        }

        HttpClient? proxyClient = null;
        try
        {
            var result = new IpGeoLocationModel();
            UpdateIntranetIp(result, adapters);

            HttpClient clientToUse = _httpClient;
            if (proxy != null)
            {
                try
                {
                    var handler = new SocketsHttpHandler
                    {
                        ConnectTimeout = TimeSpan.FromSeconds(3),
                        PooledConnectionLifetime = TimeSpan.FromMinutes(1)
                    };
                    if (proxy.Protocol == ProxyProtocol.Socks5 || proxy.Protocol == ProxyProtocol.Socks4)
                    {
                        handler.Proxy = new WebProxy($"socks5://{proxy.Host}:{proxy.Port}");
                    }
                    else
                    {
                        handler.Proxy = new WebProxy($"http://{proxy.Host}:{proxy.Port}");
                    }
                    if (!string.IsNullOrEmpty(proxy.Username))
                    {
                        handler.Proxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password ?? "");
                    }
                    proxyClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) };
                    clientToUse = proxyClient;
                }
                catch
                {
                    clientToUse = _httpClient;
                }
            }

            // Provider 1: ip-api.com
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(3000);

                string json = await clientToUse.GetStringAsync("http://ip-api.com/json/", cts.Token);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var st) && st.GetString() == "success")
                {
                    result.InternetIp = root.TryGetProperty("query", out var q) ? q.GetString() ?? "Unknown" : "Unknown";
                    result.Country = root.TryGetProperty("country", out var c) ? c.GetString() ?? "Unknown" : "Unknown";
                    result.CountryCode = root.TryGetProperty("countryCode", out var cc) ? cc.GetString() ?? "UN" : "UN";
                    result.Region = root.TryGetProperty("regionName", out var r) ? r.GetString() ?? "" : "";
                    result.City = root.TryGetProperty("city", out var cy) ? cy.GetString() ?? "" : "";
                    result.Isp = root.TryGetProperty("isp", out var isp) ? isp.GetString() ?? "" : "";
                    result.FlagEmoji = IpGeoLocationModel.ConvertCountryCodeToFlag(result.CountryCode);
                    result.LastResolved = DateTime.Now;

                    _cachedLocation = result;
                    _cacheExpiry = DateTime.UtcNow.AddSeconds(60);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MacGeoIp] Primary failed: {ex.Message}");
            }

            // Provider 2: ipwho.is
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(3000);

                string json = await clientToUse.GetStringAsync("https://ipwho.is/", cts.Token);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("success", out var suc) && suc.GetBoolean())
                {
                    result.InternetIp = root.TryGetProperty("ip", out var ip) ? ip.GetString() ?? "Unknown" : "Unknown";
                    result.Country = root.TryGetProperty("country", out var c) ? c.GetString() ?? "Unknown" : "Unknown";
                    result.CountryCode = root.TryGetProperty("country_code", out var cc) ? cc.GetString() ?? "UN" : "UN";
                    result.Region = root.TryGetProperty("region", out var r) ? r.GetString() ?? "" : "";
                    result.City = root.TryGetProperty("city", out var cy) ? cy.GetString() ?? "" : "";
                    if (root.TryGetProperty("connection", out var conn) && conn.TryGetProperty("isp", out var isp))
                    {
                        result.Isp = isp.GetString() ?? "";
                    }
                    result.FlagEmoji = IpGeoLocationModel.ConvertCountryCodeToFlag(result.CountryCode);
                    result.LastResolved = DateTime.Now;

                    _cachedLocation = result;
                    _cacheExpiry = DateTime.UtcNow.AddSeconds(45);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MacGeoIp] Fallback failed: {ex.Message}");
            }

            // Fallback for proxy
            if (proxy != null && (result.InternetIp == "Resolving..." || result.InternetIp == "Unknown"))
            {
                result.InternetIp = proxy.Host;
                result.Country = proxy.CountryCode;
                result.CountryCode = proxy.CountryCode;
                result.FlagEmoji = proxy.FlagEmoji;
                result.City = "Proxy Node";
                result.Isp = $"{proxy.Protocol} Upstream";
                result.LastResolved = DateTime.Now;
                _cachedLocation = result;
                return result;
            }

            return _cachedLocation;
        }
        finally
        {
            proxyClient?.Dispose();
            _resolveLock.Release();
        }
    }

    private static void UpdateIntranetIp(IpGeoLocationModel model, IEnumerable<NetworkAdapterModel> adapters)
    {
        var active = adapters.Where(a => a.IsEnabled && !string.IsNullOrWhiteSpace(a.IpAddress) && a.IpAddress != "0.0.0.0").ToList();
        if (active.Count == 0)
        {
            model.IntranetIp = "127.0.0.1 (Loopback)";
        }
        else if (active.Count == 1)
        {
            model.IntranetIp = $"{active[0].IpAddress} ({active[0].Name})";
        }
        else
        {
            model.IntranetIp = string.Join(" + ", active.Select(a => $"{a.IpAddress} ({a.Name})"));
        }
    }
}
