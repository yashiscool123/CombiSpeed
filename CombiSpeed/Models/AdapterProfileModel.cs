using System.Text.Json.Serialization;

namespace CombiSpeed.Models;

public class AdapterSettingItem
{
    public string AdapterKey { get; set; } = string.Empty; // MacAddress or Name
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Weight { get; set; } = 50;
    public string? CustomLabel { get; set; }
}

public class CombinationProfileModel
{
    public string ProfileId { get; set; } = Guid.NewGuid().ToString("N");
    public string ProfileName { get; set; } = "Default Combination";
    public RoutingMode RoutingMode { get; set; } = RoutingMode.Weighted;
    public Dictionary<string, AdapterSettingItem> Adapters { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.Now;
}

public class PersistedAppDataModel
{
    public string Version { get; set; } = "1.3.1";
    public string ActiveProfileName { get; set; } = "Default Combination";
    public ProxyConfigModel Config { get; set; } = new();
    public Dictionary<string, AdapterSettingItem> AdapterSettings { get; set; } = new();
    public List<CombinationProfileModel> SavedProfiles { get; set; } = new();
    public List<PacRuleModel> PacRules { get; set; } = new();
    public DateTime LastSaved { get; set; } = DateTime.Now;
}
