using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OE2EmpireTracker.Desktop.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Loads and saves <see cref="ThresholdPreferences"/> from UIPreferences.json.
/// Uses <see cref="SafeFileWriter"/> for atomic writes.
/// If the file is missing or malformed, defaults are used.
/// </summary>
public sealed class PreferencesStore
{
    private const string PreferencesFileName = "UIPreferences.json";

    private readonly IFileSystemService _fileSystem;
    private readonly SafeFileWriter _safeFileWriter;
    private readonly ILogger<PreferencesStore> _logger;

    private ThresholdPreferences _thresholds;

    public PreferencesStore(
        IFileSystemService fileSystem,
        SafeFileWriter safeFileWriter,
        ILogger<PreferencesStore> logger)
    {
        _fileSystem = fileSystem;
        _safeFileWriter = safeFileWriter;
        _logger = logger;
        _thresholds = new ThresholdPreferences();
        Load();
    }

    /// <summary>Gets the current threshold preferences (read-only snapshot).</summary>
    public ThresholdPreferences Thresholds => _thresholds;

    /// <summary>
    /// Saves the given thresholds to UIPreferences.json and updates the in-memory copy.
    /// </summary>
    /// <param name="thresholds">The thresholds to persist.</param>
    public void Save(ThresholdPreferences thresholds)
    {
        _thresholds = thresholds;

        try
        {
            var path = GetFilePath();
            var wrapper = new UIPreferencesFile { Thresholds = thresholds };
            var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            _safeFileWriter.WriteAllText(path, json);
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

            _logger.LogDebug("Loaded preferences from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load preferences from {Path}, using defaults", path);
            _thresholds = new ThresholdPreferences();
        }
    }

    /// <summary>
    /// Wrapper class matching the UIPreferences.json structure.
    /// </summary>
    private sealed class UIPreferencesFile
    {
        public ThresholdPreferences? Thresholds { get; set; }
    }
}
