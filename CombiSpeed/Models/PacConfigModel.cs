using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CombiSpeed.Models;

public class PacConfigModel : INotifyPropertyChanged
{
    private static PacConfigModel? _current;
    public static PacConfigModel Current => _current ??= new PacConfigModel();

    private bool _enablePacRouting;
    public bool EnablePacRouting
    {
        get => _enablePacRouting;
        set => SetField(ref _enablePacRouting, value);
    }

    public ObservableCollection<PacRuleModel> Rules { get; } = new();

    public PacConfigModel()
    {
        // Seed default best-practice PAC rules
        Rules.Add(new PacRuleModel
        {
            Pattern = "localhost; 127.*; ::1",
            Action = PacAction.Direct,
            Description = "Loopback and local machine addresses (Bypass)"
        });

        Rules.Add(new PacRuleModel
        {
            Pattern = "192.168.*; 10.*; 172.16.*; 172.17.*; 172.18.*; 172.19.*; 172.2*.*",
            Action = PacAction.Direct,
            Description = "RFC 1918 Private Local Area Network subnets (Bypass)"
        });

        Rules.Add(new PacRuleModel
        {
            Pattern = "*.local; *.lan; *.internal; *.corp",
            Action = PacAction.Direct,
            Description = "Internal corporate & mDNS local domains (Bypass)"
        });

        Rules.Add(new PacRuleModel
        {
            Pattern = "*",
            Action = PacAction.Bonded,
            Description = "All Internet Web & Application Traffic (Bonded Aggregation)"
        });
    }

    /// <summary>
    /// Compiles user-defined domain rules into a standard, RFC-compliant JavaScript PAC function.
    /// </summary>
    public string GeneratePacScript(int httpPort = 8080, int socksPort = 1080)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// CombiSpeed Dynamic Proxy Auto-Config (PAC) Script");
        sb.AppendLine("// Generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("function FindProxyForURL(url, host) {");
        sb.AppendLine("    host = host.toLowerCase();");
        sb.AppendLine();

        // Standard plain hostname bypass
        sb.AppendLine("    if (isPlainHostName(host)) return \"DIRECT\";");
        sb.AppendLine();

        foreach (var rule in Rules.Where(r => r.IsEnabled))
        {
            var patterns = rule.Pattern.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (patterns.Length == 0) continue;

            var condParts = new List<string>();
            foreach (var p in patterns)
            {
                if (p == "*")
                {
                    condParts.Add("true");
                }
                else if (p.Contains('*'))
                {
                    condParts.Add($"shExpMatch(host, \"{p.ToLowerInvariant()}\")");
                }
                else
                {
                    condParts.Add($"dnsDomainIs(host, \"{p.ToLowerInvariant()}\") || host === \"{p.ToLowerInvariant()}\"");
                }
            }

            string condition = string.Join(" || ", condParts);
            string returnDirective = rule.Action switch
            {
                PacAction.Direct => "\"DIRECT\"",
                PacAction.Socks5Only => $"\"SOCKS5 127.0.0.1:{socksPort}; SOCKS 127.0.0.1:{socksPort}; DIRECT\"",
                _ => $"\"PROXY 127.0.0.1:{httpPort}; SOCKS5 127.0.0.1:{socksPort}; DIRECT\""
            };

            sb.AppendLine($"    // {rule.Description}");
            sb.AppendLine($"    if ({condition}) {{");
            sb.AppendLine($"        return {returnDirective};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Fallback default
        sb.AppendLine($"    return \"PROXY 127.0.0.1:{httpPort}; SOCKS5 127.0.0.1:{socksPort}; DIRECT\";");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
