using Microsoft.Extensions.Logging;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Stub repository for star system data.
/// Will load SystemData.json and provide O(1) lookups by name/id when implemented.
/// </summary>
public sealed class SystemRepository
{
    private readonly ILogger<SystemRepository> _logger;

    public SystemRepository(ILogger<SystemRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>Loads system data from JSON. Deferred implementation.</summary>
    public void Load()
    {
        _logger.LogDebug("SystemRepository.Load called (stub)");
    }

    /// <summary>Finds a system by name. Deferred implementation.</summary>
    public object? FindByName(string name)
    {
        _logger.LogDebug("SystemRepository.FindByName called for {Name} (stub)", name);
        return null;
    }

    /// <summary>Finds a system by ID. Deferred implementation.</summary>
    public object? FindById(string id)
    {
        _logger.LogDebug("SystemRepository.FindById called for {Id} (stub)", id);
        return null;
    }
}
