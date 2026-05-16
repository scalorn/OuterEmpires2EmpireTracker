using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Persists DataGrid column widths per grid ID.
/// Stores alongside UIPreferences.json in the config directory.
/// Actual wiring to each DataGrid is deferred (requires code-behind per grid).
/// </summary>
public sealed class GridStateService
{
    private const string FileName = "GridState.json";

    private readonly IFileSystemService _fileSystem;
    private readonly SafeFileWriter _safeFileWriter;
    private readonly ILogger<GridStateService> _logger;

    private Dictionary<string, Dictionary<string, double>> _state;

    public GridStateService(
        IFileSystemService fileSystem,
        SafeFileWriter safeFileWriter,
        ILogger<GridStateService> logger)
    {
        _fileSystem = fileSystem;
        _safeFileWriter = safeFileWriter;
        _logger = logger;
        _state = new Dictionary<string, Dictionary<string, double>>();
        Load();
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

    private void Persist()
    {
        try
        {
            var path = GetFilePath();
            var json = JsonConvert.SerializeObject(_state, Formatting.Indented);
            _safeFileWriter.WriteAllText(path, json);
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
}
