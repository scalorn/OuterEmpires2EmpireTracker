using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Persists DataGrid column widths and filter text per grid/view ID.
/// Stores alongside UIPreferences.json in the config directory.
/// Actual wiring to each DataGrid is deferred (requires code-behind per grid).
/// </summary>
public sealed class GridStateService
{
    private const string FileName = "GridState.json";
    private const string FilterFileName = "FilterState.json";

    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<GridStateService> _logger;

    private Dictionary<string, Dictionary<string, double>> _state;
    private Dictionary<string, Dictionary<string, string>> _filterState;

    public GridStateService(
        IFileSystemService fileSystem,
        ILogger<GridStateService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _state = new Dictionary<string, Dictionary<string, double>>();
        _filterState = new Dictionary<string, Dictionary<string, string>>();
        Load();
        LoadFilters();
    }

    /// <summary>
    /// Saves column widths for a given grid ID.
    /// </summary>
    /// <param name="gridId">Unique identifier for the grid (e.g. "ColonyStructures").</param>
    /// <param name="columnWidths">Dictionary of column header to width.</param>
    public void Save(string gridId, Dictionary<string, double> columnWidths)
    {
        _state[gridId] = new Dictionary<string, double>(columnWidths);
        Persist();
    }

    /// <summary>
    /// Loads saved column widths for a given grid ID.
    /// </summary>
    /// <param name="gridId">Unique identifier for the grid.</param>
    /// <returns>Dictionary of column header to width, or null if not saved.</returns>
    public Dictionary<string, double>? Load(string gridId)
    {
        return _state.TryGetValue(gridId, out var widths)
            ? new Dictionary<string, double>(widths)
            : null;
    }

    /// <summary>
    /// Saves filter text values for a given view ID.
    /// </summary>
    /// <param name="viewId">Unique identifier for the view (e.g. "ColonyList").</param>
    /// <param name="filters">Dictionary of filter name to filter text value.</param>
    public void SaveFilterState(string viewId, Dictionary<string, string> filters)
    {
        _filterState[viewId] = new Dictionary<string, string>(filters);
        PersistFilters();
    }

    /// <summary>
    /// Loads saved filter text values for a given view ID.
    /// </summary>
    /// <param name="viewId">Unique identifier for the view.</param>
    /// <returns>Dictionary of filter name to filter text value, or null if not saved.</returns>
    public Dictionary<string, string>? LoadFilterState(string viewId)
    {
        return _filterState.TryGetValue(viewId, out var filters)
            ? new Dictionary<string, string>(filters)
            : null;
    }

    private void Persist()
    {
        try
        {
            var path = GetFilePath();
            var json = JsonConvert.SerializeObject(_state, Formatting.Indented);
            OE2EmpireTracker.Persistence.SafeFileWriter.WriteAllText(path, json);
            _logger.LogDebug("Saved grid state to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save grid state");
        }
    }

    private string GetFilePath()
    {
        var configDir = _fileSystem.GetConfigDirectory();
        _fileSystem.EnsureDirectoryExists(configDir);
        return Path.Combine(configDir, FileName);
    }

    private void Load()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
        {
            _logger.LogDebug("Grid state file not found, using defaults");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(json);
            if (loaded is not null)
            {
                _state = loaded;
            }

            _logger.LogDebug("Loaded grid state from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load grid state from {Path}, using defaults", path);
            _state = new Dictionary<string, Dictionary<string, double>>();
        }
    }

    private void PersistFilters()
    {
        try
        {
            var path = GetFilterFilePath();
            var json = JsonConvert.SerializeObject(_filterState, Formatting.Indented);
            OE2EmpireTracker.Persistence.SafeFileWriter.WriteAllText(path, json);
            _logger.LogDebug("Saved filter state to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save filter state");
        }
    }

    private string GetFilterFilePath()
    {
        var configDir = _fileSystem.GetConfigDirectory();
        _fileSystem.EnsureDirectoryExists(configDir);
        return Path.Combine(configDir, FilterFileName);
    }

    private void LoadFilters()
    {
        var path = GetFilterFilePath();
        if (!File.Exists(path))
        {
            _logger.LogDebug("Filter state file not found, using defaults");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
            if (loaded is not null)
            {
                _filterState = loaded;
            }

            _logger.LogDebug("Loaded filter state from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load filter state from {Path}, using defaults", path);
            _filterState = new Dictionary<string, Dictionary<string, string>>();
        }
    }
}
