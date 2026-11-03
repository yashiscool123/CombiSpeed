namespace CombiSpeed.Linux.Models;

public class IpGeoLocationModel
{
    public string IntranetIp { get; set; } = "127.0.0.1";
    public string InternetIp { get; set; } = "Resolving...";
    public string Country { get; set; } = "Resolving...";
    public string CountryCode { get; set; } = "UN";
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌐";
    public DateTime LastResolved { get; set; } = DateTime.MinValue;

    public string LocationFormatted
    {
        get
        {
            if (string.IsNullOrWhiteSpace(City) && string.IsNullOrWhiteSpace(Country)) return "Resolving Location...";
            if (string.IsNullOrWhiteSpace(City)) return $"{Country} {FlagEmoji}";
            return $"{City}, {Country} {FlagEmoji}";
        }
    }

    public string FormattedLocation => LocationFormatted;

    public string FullSummaryFormatted
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(InternetIp) && InternetIp != "Resolving...") parts.Add(InternetIp);
            if (!string.IsNullOrWhiteSpace(LocationFormatted) && LocationFormatted != "Resolving Location...") parts.Add(LocationFormatted);
            if (!string.IsNullOrWhiteSpace(Isp)) parts.Add(Isp);
            return parts.Count > 0 ? string.Join(" | ", parts) : "Resolving Network Identity...";
        }
    }

    public static string ConvertCountryCodeToFlag(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2) return "🌐";
        string upper = code.ToUpperInvariant();
        if (upper == "UN") return "🌐";
        try
        {
            int first = 0x1F1E6 + (upper[0] - 'A');
            int second = 0x1F1E6 + (upper[1] - 'A');
            return char.ConvertFromUtf32(first) + char.ConvertFromUtf32(second);
        }
        catch
        {
            return "🌐";
        }
    }
}
