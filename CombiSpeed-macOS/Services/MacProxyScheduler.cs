using CombiSpeed.macOS.Models;

namespace CombiSpeed.macOS.Services;

public class MacProxyScheduler : IDisposable
{
    private readonly MacProxyHarvester _harvester;
    private readonly MacProxyValidator _validator;
    private readonly RotatingProxyConfigModel _config;

    private Timer? _harvestTimer;
    private Timer? _validationTimer;
    private bool _isHarvesting;
    private bool _isValidating;
    private bool _isDisposed;

    public event Action<string>? OnLog;

    public MacProxyScheduler(
        MacProxyHarvester harvester,
        MacProxyValidator validator,
        RotatingProxyConfigModel config)
    {
        _harvester = harvester;
        _validator = validator;
        _config = config;
    }

    public void Start()
    {
        if (_isDisposed) return;

        int harvestMinutes = Math.Clamp(_config.AutoHarvestIntervalMinutes, 2, 1440);
        int validateMinutes = Math.Clamp(_config.AutoValidateIntervalMinutes, 1, 120);

        _harvestTimer?.Dispose();
        _validationTimer?.Dispose();

        if (_config.AutoHarvestEnabled)
        {
            _harvestTimer = new Timer(async _ => await ScheduledHarvestAsync(), null,
                TimeSpan.FromMinutes(harvestMinutes), TimeSpan.FromMinutes(harvestMinutes));
            OnLog?.Invoke($"[Scheduler] Background Auto-Harvest active (every {harvestMinutes} min).");
        }

        if (_config.AutoValidateEnabled)
        {
            _validationTimer = new Timer(async _ => await ScheduledValidationAsync(), null,
                TimeSpan.FromMinutes(validateMinutes), TimeSpan.FromMinutes(validateMinutes));
            OnLog?.Invoke($"[Scheduler] Background Auto-Validation active (every {validateMinutes} min).");
        }
    }

    public async Task ScheduledHarvestAsync()
    {
        if (_isHarvesting || !_config.AutoHarvestEnabled) return;
        _isHarvesting = true;

        try
        {
            OnLog?.Invoke("[Scheduler] Initiating scheduled background proxy harvest...");
            var sources = RotatingProxyConfigModel.GetDefaultSources()
                .Concat(_config.CustomSources)
                .Distinct()
                .ToList();

            var candidates = await _harvester.HarvestAllAsync(sources, _config.MaxPoolSize);
            _validator.AddCandidates(candidates);
            OnLog?.Invoke($"[Scheduler] Harvest complete. Total candidate pool: {_validator.TotalCount} proxies.");

            await ScheduledValidationAsync();
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[Scheduler] Scheduled harvest error: {ex.Message}");
        }
        finally
        {
            _isHarvesting = false;
        }
    }

    public async Task ScheduledValidationAsync()
    {
        if (_isValidating || !_config.AutoValidateEnabled) return;
        _isValidating = true;

        try
        {
            OnLog?.Invoke("[Scheduler] Initiating scheduled background proxy health validation...");
            int healthy = await _validator.ValidateAllAsync(
                concurrencyLimit: _config.ConcurrencyLimit,
                timeoutMs: _config.MaxTimeoutMs);

            int pruned = _validator.PruneDead(maxFailures: 2);
            OnLog?.Invoke($"[Scheduler] Validation finished. Active healthy pool: {healthy} (Pruned dead: {pruned}).");
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[Scheduler] Scheduled validation error: {ex.Message}");
        }
        finally
        {
            _isValidating = false;
        }
    }

    public void Stop()
    {
        _harvestTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _validationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _harvestTimer?.Dispose();
        _validationTimer?.Dispose();
    }
}

