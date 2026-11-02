using System.Text.Json;
using CombiSpeed.Linux.Models;

namespace CombiSpeed.Linux.Services;

public class PersistedSettingsDto
{
    public ProxyConfigModel Config { get; set; } = new();
    public List<AdapterPersistenceDto> Adapters { get; set; } = new();
    public string ActiveProfile { get; set; } = "Default Profile";
    public DateTime LastSaved { get; set; } = DateTime.Now;
}

public class AdapterPersistenceDto
{
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public int Weight { get; set; } = 50;
    public double? CustomSpeedLimitMbps { get; set; }
}

public class LinuxSettingsStorage
{
    private readonly string _filePath;

    public LinuxSettingsStorage(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath))
        {
            _filePath = customPath;
        }
        else
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string configDir = Path.Combine(home, ".config", "combispeed");
            Directory.CreateDirectory(configDir);
            _filePath = Path.Combine(configDir, "adapter_settings.json");
        }
    }

    public void Save(List<NetworkAdapterModel> adapters, ProxyConfigModel config, string profileName = "Default Profile")
    {
        var dto = new PersistedSettingsDto
        {
            Config = config,
            ActiveProfile = profileName,
            LastSaved = DateTime.Now,
            Adapters = adapters.Select(a => new AdapterPersistenceDto
            {
                Name = a.Name,
                MacAddress = a.MacAddress,
                IsEnabled = a.IsEnabled,
                Weight = a.Weight,
                CustomSpeedLimitMbps = a.CustomSpeedLimitMbps
            }).ToList()
        };

        string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        string dir = Path.GetDirectoryName(_filePath) ?? "";
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, json);
    }

    public PersistedSettingsDto Load()
    {
        if (!File.Exists(_filePath)) return new PersistedSettingsDto();

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PersistedSettingsDto>(json) ?? new PersistedSettingsDto();
        }
        catch
        {
            return new PersistedSettingsDto();
        }
    }

    public void ApplyToAdapters(List<NetworkAdapterModel> adapters)
    {
        var settings = Load();
        if (settings.Adapters.Count == 0) return;

        foreach (var adapter in adapters)
        {
            var match = settings.Adapters.FirstOrDefault(s =>
                (!string.IsNullOrEmpty(s.MacAddress) && s.MacAddress.Equals(adapter.MacAddress, StringComparison.OrdinalIgnoreCase)) ||
                s.Name.Equals(adapter.Name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                adapter.IsEnabled = match.IsEnabled;
                adapter.Weight = match.Weight;
                adapter.CustomSpeedLimitMbps = match.CustomSpeedLimitMbps;
            }
        }
    }
}
