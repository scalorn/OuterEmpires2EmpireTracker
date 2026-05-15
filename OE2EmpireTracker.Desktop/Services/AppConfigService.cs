using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Manages application configuration stored in a simple JSON file.
/// Stores settings like Last_Opened_Path.
/// </summary>
public sealed class AppConfigService
{
    private const string ConfigFileName = "AppConfig.json";

    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<AppConfigService> _logger;

    private AppConfig _config;

    public AppConfigService(IFileSystemService fileSystem, ILogger<AppConfigService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _config = new AppConfig();
        Load();
    }

    /// <summary>Gets or sets the last opened file path.</summary>
    public string? LastOpenedPath
    {
        get => _config.LastOpenedPath;
        set
        {
            _config.LastOpenedPath = value;
            Save();
        }
    }

    private string GetConfigFilePath()
    {
        var configDir = _fileSystem.GetConfigDirectory();
        _fileSystem.EnsureDirectoryExists(configDir);
        return Path.Combine(configDir, ConfigFileName);
    }

    private void Load()
    {
        var path = GetConfigFilePath();
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonConvert.DeserializeObject<AppConfig>(json);
            if (loaded is not null)
            {
                _config = loaded;
            }

            _logger.LogDebug("Loaded app config from {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load app config from {Path}", path);
        }
    }

    private void Save()
    {
        try
        {
            var path = GetConfigFilePath();
            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(path, json);
            _logger.LogDebug("Saved app config to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save app config");
        }
    }

    private sealed class AppConfig
    {
        public string? LastOpenedPath { get; set; }
    }
}
