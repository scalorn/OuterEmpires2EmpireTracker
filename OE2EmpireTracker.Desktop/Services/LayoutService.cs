using System;
using System.IO;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Saves and restores the Dock layout state across sessions.
/// Uses Newtonsoft.Json for serialization.
/// </summary>
public sealed class LayoutService
{
    private const string LayoutFileName = "dock-layout.json";

    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<LayoutService> _logger;

    public LayoutService(IFileSystemService fileSystem, ILogger<LayoutService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Saves the current dock layout to a JSON file.
    /// </summary>
    public void SaveLayout(IRootDock? layout)
    {
        if (layout is null)
        {
            return;
        }

        try
        {
            var configDir = _fileSystem.GetConfigDirectory();
            _fileSystem.EnsureDirectoryExists(configDir);
            var path = Path.Combine(configDir, LayoutFileName);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            var json = JsonConvert.SerializeObject(layout, settings);
            File.WriteAllText(path, json);

            _logger.LogDebug("Dock layout saved to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save dock layout");
        }
    }

    /// <summary>
    /// Attempts to load a previously saved dock layout.
    /// Returns null if no saved layout exists or loading fails.
    /// </summary>
    public IRootDock? LoadLayout()
    {
        try
        {
            var configDir = _fileSystem.GetConfigDirectory();
            var path = Path.Combine(configDir, LayoutFileName);

            if (!File.Exists(path))
            {
                _logger.LogDebug("No saved dock layout found at {Path}", path);
                return null;
            }

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            };

            var json = File.ReadAllText(path);
            var layout = JsonConvert.DeserializeObject<IRootDock>(json, settings);

            _logger.LogInformation("Dock layout restored from {Path}", path);
            return layout;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load dock layout, using default");
            return null;
        }
    }
}
