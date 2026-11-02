using System.Text;

namespace CombiSpeed.macOS.Models;

public enum PacAction
{
    Bonded,
    Direct,
    Socks5Only
}

public class PacRuleModel
{
    public string Pattern { get; set; } = string.Empty;
    public PacAction Action { get; set; } = PacAction.Direct;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class PacConfigModel
{
    private static PacConfigModel? _current;
    public static PacConfigModel Current => _current ??= new PacConfigModel();

    public bool EnablePacRouting { get; set; }
    public List<PacRuleModel> Rules { get; } = new();

    public PacConfigModel()
    {
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
            Pattern = "*.local; *.lan; *.internal; *.corp; *.apple.com",
            Action = PacAction.Direct,
            Description = "Apple local domains & services (Bypass)"
        });

        Rules.Add(new PacRuleModel
        {
            Pattern = "*",
            Action = PacAction.Bonded,
            Description = "All Internet Web & Application Traffic (Bonded Aggregation)"
        });
    }

    public string GeneratePacScript(int httpPort = 8080, int socksPort = 1080)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// CombiSpeed macOS Dynamic Proxy Auto-Config (PAC) Script");
        sb.AppendLine("// Generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("function FindProxyForURL(url, host) {");
        sb.AppendLine("    host = host.toLowerCase();");
        sb.AppendLine();
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

        sb.AppendLine($"    return \"PROXY 127.0.0.1:{httpPort}; SOCKS5 127.0.0.1:{socksPort}; DIRECT\";");
        sb.AppendLine("}");

        return sb.ToString();
    }
}

