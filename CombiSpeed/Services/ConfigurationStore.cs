using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CombiSpeed.Models;

namespace CombiSpeed.Services;

public class ConfigurationStore
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CombiSpeed");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "adapter_settings.json");

    private PersistedAppDataModel _data = new();
    private Timer? _debounceTimer;
    private readonly object _lock = new();

    public PersistedAppDataModel Data => _data;
    public event Action? OnSaved;

    public ConfigurationStore()
    {
        Load();
    }

    /// <summary>
    /// Loads settings and adapter combinations from disk.
    /// </summary>
    public PersistedAppDataModel Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<PersistedAppDataModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loaded != null)
                    {
                        _data = loaded;

                        // Restore PAC rules if saved
                        if (_data.PacRules != null && _data.PacRules.Count > 0)
                        {
                            PacConfigModel.Current.Rules.Clear();
                            foreach (var r in _data.PacRules)
                            {
                                PacConfigModel.Current.Rules.Add(r);
                            }
                        }

                        return _data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load adapter settings: {ex.Message}");
            }

            // Defaults if file not found or corrupted
            _data = new PersistedAppDataModel();
            return _data;
        }
    }

    /// <summary>
    /// Debounced auto-save that commits to disk after 400ms of inactivity.
    /// </summary>
    public void RequestAutoSave(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config, string? activeProfileName = null)
    {
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            var adapterList = adapters.ToList();
            _debounceTimer = new Timer(_ =>
            {
                SaveImmediate(adapterList, config, activeProfileName);
            }, null, 400, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Immediately writes adapter combinations and application settings to disk.
    /// </summary>
    public void SaveImmediate(IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config, string? activeProfileName = null)
    {
        lock (_lock)
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                _data.Config = config;
                _data.LastSaved = DateTime.Now;
                if (!string.IsNullOrEmpty(activeProfileName))
                {
                    _data.ActiveProfileName = activeProfileName;
                }

                // Persist current adapters
                foreach (var adapter in adapters)
                {
                    string key = GetAdapterKey(adapter);
                    _data.AdapterSettings[key] = new AdapterSettingItem
                    {
                        AdapterKey = key,
                        Name = adapter.Name,
                        MacAddress = adapter.MacAddress,
                        Description = adapter.Description,
                        IsEnabled = adapter.IsEnabled,
                        Weight = adapter.Weight,
                        SpeedLimitBps = adapter.SpeedLimitBps
                    };
                }

                // Update active profile in SavedProfiles
                UpdateProfileInternal(_data.ActiveProfileName, adapters, config.RoutingMode);

                // Persist PAC rules
                _data.PacRules = PacConfigModel.Current.Rules.ToList();

                // Atomic write to prevent file corruption
                string tempPath = SettingsFilePath + ".tmp";
                string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(tempPath, json);
                File.Move(tempPath, SettingsFilePath, overwrite: true);

                OnSaved?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save adapter settings: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies saved adapter settings (enabled state and % weights) to detected network interfaces.
    /// </summary>
    public bool ApplyToAdapters(IEnumerable<NetworkAdapterModel> adapters)
    {
        lock (_lock)
        {
            if (_data.AdapterSettings.Count == 0) return false;

            bool anyApplied = false;
            foreach (var adapter in adapters)
            {
                string key = GetAdapterKey(adapter);
                AdapterSettingItem? setting = null;

                // Match by primary key (MAC or Name)
                if (_data.AdapterSettings.TryGetValue(key, out var s))
                {
                    setting = s;
                }
                else
                {
                    // Fallback match by Name or MAC
                    setting = _data.AdapterSettings.Values.FirstOrDefault(v =>
                        (!string.IsNullOrEmpty(v.MacAddress) && v.MacAddress == adapter.MacAddress) ||
                        (!string.IsNullOrEmpty(v.Name) && v.Name.Equals(adapter.Name, StringComparison.OrdinalIgnoreCase)));
                }

                if (setting != null)
                {
                    adapter.IsEnabled = setting.IsEnabled;
                    adapter.Weight = Math.Clamp(setting.Weight, 0, 100);
                    adapter.SpeedLimitBps = Math.Max(0, setting.SpeedLimitBps);
                    anyApplied = true;
                }
            }

            return anyApplied;
        }
    }

    /// <summary>
    /// Saves a named combination profile (e.g. "Dual-WAN: 70/30", "Office Mode").
    /// </summary>
    public void SaveCombinationProfile(string profileName, IEnumerable<NetworkAdapterModel> adapters, RoutingMode mode)
    {
        lock (_lock)
        {
            UpdateProfileInternal(profileName, adapters, mode);
            SaveImmediate(adapters, _data.Config, profileName);
        }
    }

    /// <summary>
    /// Applies a saved combination profile to the active network adapters.
    /// </summary>
    public bool ApplyProfile(string profileName, IEnumerable<NetworkAdapterModel> adapters, ProxyConfigModel config)
    {
        lock (_lock)
        {
            var profile = _data.SavedProfiles.FirstOrDefault(p => p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase));
            if (profile == null) return false;

            _data.ActiveProfileName = profile.ProfileName;
            config.RoutingMode = profile.RoutingMode;

            foreach (var adapter in adapters)
            {
                string key = GetAdapterKey(adapter);
                if (profile.Adapters.TryGetValue(key, out var setting) ||
                    profile.Adapters.Values.FirstOrDefault(v => v.Name.Equals(adapter.Name, StringComparison.OrdinalIgnoreCase)) is { } fallback && (setting = fallback) != null)
                {
                    adapter.IsEnabled = setting.IsEnabled;
                    adapter.Weight = setting.Weight;
                    adapter.SpeedLimitBps = setting.SpeedLimitBps;
                }
            }

            SaveImmediate(adapters, config, profileName);
            return true;
        }
    }

    public List<string> GetProfileNames()
    {
        lock (_lock)
        {
            var list = _data.SavedProfiles.Select(p => p.ProfileName).Distinct().ToList();
            if (!list.Contains("Default Combination")) list.Insert(0, "Default Combination");
            return list;
        }
    }

    private void UpdateProfileInternal(string profileName, IEnumerable<NetworkAdapterModel> adapters, RoutingMode mode)
    {
        var existing = _data.SavedProfiles.FirstOrDefault(p => p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            existing = new CombinationProfileModel
            {
                ProfileName = profileName
            };
            _data.SavedProfiles.Add(existing);
        }

        existing.RoutingMode = mode;
        existing.LastModified = DateTime.Now;
        existing.Adapters.Clear();

        foreach (var a in adapters)
        {
            string key = GetAdapterKey(a);
            existing.Adapters[key] = new AdapterSettingItem
            {
                AdapterKey = key,
                Name = a.Name,
                MacAddress = a.MacAddress,
                Description = a.Description,
                IsEnabled = a.IsEnabled,
                Weight = a.Weight,
                SpeedLimitBps = a.SpeedLimitBps
            };
        }
    }

    private static string GetAdapterKey(NetworkAdapterModel adapter)
    {
        if (!string.IsNullOrWhiteSpace(adapter.MacAddress) && adapter.MacAddress != "00:00:00:00:00:00")
        {
            return $"mac_{adapter.MacAddress.Replace(":", "").Replace("-", "").ToUpperInvariant()}";
        }
        return $"name_{adapter.Name.Replace(" ", "_").ToLowerInvariant()}";
    }
}

