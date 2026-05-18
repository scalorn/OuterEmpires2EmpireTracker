using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OE2EmpireTracker.Desktop.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Loads and saves <see cref="ThresholdPreferences"/> and <see cref="SavedWindowState"/>
/// from UIPreferences.json.
/// Uses <see cref="OE2EmpireTracker.Persistence.SafeFileWriter"/> for atomic writes.
/// If the file is missing or malformed, defaults are used.
/// </summary>
public sealed class PreferencesStore
{
    private const string PreferencesFileName = "UIPreferences.json";

    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<PreferencesStore> _logger;

    private ThresholdPreferences _thresholds;
    private SavedWindowState _windowState;

    public PreferencesStore(
        IFileSystemService fileSystem,
        ILogger<PreferencesStore> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _thresholds = new ThresholdPreferences();
        _windowState = new SavedWindowState();
        Load();
    }

    /// <summary>Gets the current threshold preferences (read-only snapshot).</summary>
    public ThresholdPreferences Thresholds => _thresholds;

    /// <summary>Gets the current window state.</summary>
    public SavedWindowState WindowState => _windowState;

    /// <summary>
    /// Saves the given thresholds to UIPreferences.json and updates the in-memory copy.
    /// </summary>
    /// <param name="thresholds">The thresholds to persist.</param>
    public void Save(ThresholdPreferences thresholds)
    {
        _thresholds = thresholds;
        PersistAll();
    }

    /// <summary>
    /// Saves the given window state to UIPreferences.json and updates the in-memory copy.
    /// </summary>
    /// <param name="windowState">The window state to persist.</param>
    public void SaveWindowState(SavedWindowState windowState)
    {
        _windowState = windowState;
        PersistAll();
    }

    private void PersistAll()
    {
        try
        {
            var path = GetFilePath();
            var wrapper = new UIPreferencesFile
            {
                Thresholds = _thresholds,
                WindowState = _windowState,
            };
            var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            OE2EmpireTracker.Persistence.SafeFileWriter.WriteAllText(path, json);
            _logger.LogDebug("Saved preferences to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save preferences");
        }
    }

    private string GetFilePath()
    {
        var configDir = _fileSystem.GetConfigDirectory();
        _fileSystem.EnsureDirectoryExists(configDir);
        return Path.Combine(configDir, PreferencesFileName);
    }

    private void Load()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
        {
            _logger.LogDebug("Preferences file not found, using defaults");
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var wrapper = JsonConvert.DeserializeObject<UIPreferencesFile>(json);
            if (wrapper?.Thresholds is not null)
            {
                _thresholds = wrapper.Thresholds;
            }
            else
            {
                _logger.LogDebug("Thresholds property null in preferences file, using defaults");
            }

            if (wrapper?.WindowState is not null)
            {
                _windowState = wrapper.WindowState;
            }
            else
            {
                _logger.LogDebug("WindowState property null in preferences file, using defaults");
            }

            _logger.LogDebug("Loaded preferences from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load preferences from {Path}, using defaults", path);
            _thresholds = new ThresholdPreferences();
            _windowState = new SavedWindowState();
        }
    }

    /// <summary>
    /// Wrapper class matching the UIPreferences.json structure.
    /// </summary>
    private sealed class UIPreferencesFile
    {
        public ThresholdPreferences? Thresholds { get; set; }

        public SavedWindowState? WindowState { get; set; }
    }
}
