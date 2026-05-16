using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Repository for star system data. Loads SystemData.json and provides
/// O(1) lookups by Id and case-insensitive partial name search.
/// </summary>
public sealed class SystemRepository
{
    private readonly ILogger<SystemRepository> _logger;
    private readonly IFileSystemService _fileSystem;
    private readonly SafeFileWriter _safeFileWriter;
    private Dictionary<int, StarSystem> _systemsById = new Dictionary<int, StarSystem>();
    private string? _dataFilePath;

    public SystemRepository(
        ILogger<SystemRepository> logger,
        IFileSystemService fileSystem,
        SafeFileWriter safeFileWriter)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _safeFileWriter = safeFileWriter;
    }

    /// <summary>Gets all loaded systems.</summary>
    public IReadOnlyCollection<StarSystem> Systems => _systemsById.Values;

    /// <summary>Gets the number of loaded systems.</summary>
    public int Count => _systemsById.Count;

    /// <summary>Gets a value indicating whether system data has been loaded.</summary>
    public bool IsLoaded => _systemsById.Count > 0;

    /// <summary>
    /// Loads system data from SystemData.json. Searches the same directory
    /// as PlayerData.json, then the solution root.
    /// </summary>
    public void Load()
    {
        var path = FindSystemDataFile();
        if (path is null)
        {
            _logger.LogWarning("SystemData.json not found; system repository is empty");
            return;
        }

        LoadFromFile(path);
    }

    /// <summary>
    /// Loads system data from a specific file path.
    /// </summary>
    public void LoadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var systems = JsonConvert.DeserializeObject<List<StarSystem>>(json);
            if (systems is not null)
            {
                _systemsById = systems.ToDictionary(s => s.Id);
                _dataFilePath = path;
                _logger.LogInformation("Loaded {Count} systems from {Path}", _systemsById.Count, path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load system data from {Path}", path);
        }
    }

    /// <summary>Finds systems by partial name match (case-insensitive).</summary>
    public IReadOnlyList<StarSystem> FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Array.Empty<StarSystem>();
        }

        return _systemsById.Values
            .Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Finds a system by exact ID.</summary>
    public StarSystem? FindById(int id)
    {
        _systemsById.TryGetValue(id, out var system);
        return system;
    }

    /// <summary>
    /// Updates a system's mutable fields and persists to disk.
    /// </summary>
    public bool UpdateSystem(int id, string factionName, bool hasSpaceport, bool hasStarbase)
    {
        if (!_systemsById.TryGetValue(id, out var system))
        {
            _logger.LogWarning("UpdateSystem: system {Id} not found", id);
            return false;
        }

        system.FactionName = factionName;
        system.HasSpaceport = hasSpaceport;
        system.HasStarbase = hasStarbase;

        var saved = Persist();
        if (saved)
        {
            WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage());
        }

        return saved;
    }

    /// <summary>
    /// Imports systems from an oe2-galaxy-systems.json file, replacing all current data.
    /// Returns the number of systems imported, or -1 on failure.
    /// </summary>
    public int ImportFromGalaxyFile(string sourcePath)
    {
        try
        {
            var json = File.ReadAllText(sourcePath);
            var rawSystems = JsonConvert.DeserializeObject<List<GalaxyImportEntry>>(json);
            if (rawSystems is null)
            {
                _logger.LogWarning("Import returned null from {Path}", sourcePath);
                return -1;
            }

            var systems = rawSystems.Select(r => new StarSystem
            {
                Id = r.Id,
                Name = r.Name,
                X = r.X,
                Y = r.Y,
                Quadrant = r.Quadrant,
                Sector = r.Sector,
                Region = r.Region,
                Locality = r.Locality,
                SpectralClass = r.SpectralClass,
                FactionId = r.FactionId,
                FactionName = r.FactionName,
                FactionColor = r.FactionColor,
                HasOrbital = r.Orbital != 0,
                HasSpaceport = r.Spaceport != 0,
                HasStarbase = r.Starbase != 0,
            }).ToList();

            _systemsById = systems.ToDictionary(s => s.Id);
            Persist();
            _logger.LogInformation("Imported {Count} systems from {Path}", systems.Count, sourcePath);
            WeakReferenceMessenger.Default.Send(new RefreshRequestedMessage());
            return systems.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import galaxy data from {Path}", sourcePath);
            return -1;
        }
    }

    private static string? FindSolutionDirectory(string startDir)
    {
        var dir = startDir;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "OE2EmpireTracker.sln")))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return null;
    }

    private bool Persist()
    {
        if (string.IsNullOrEmpty(_dataFilePath))
        {
            // Determine a default path
            var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
            if (!string.IsNullOrEmpty(dataService?.CurrentFilePath))
            {
                var dir = Path.GetDirectoryName(dataService.CurrentFilePath);
                if (dir is not null)
                {
                    _dataFilePath = Path.Combine(dir, "SystemData.json");
                }
            }

            if (string.IsNullOrEmpty(_dataFilePath))
            {
                var dataDir = _fileSystem.GetDataDirectory();
                _fileSystem.EnsureDirectoryExists(dataDir);
                _dataFilePath = Path.Combine(dataDir, "SystemData.json");
            }
        }

        var json = JsonConvert.SerializeObject(
            _systemsById.Values.ToList(),
            Formatting.Indented,
            new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });

        return _safeFileWriter.WriteAllText(_dataFilePath, json);
    }

    private string? FindSystemDataFile()
    {
        // 1. Same directory as PlayerData.json
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (!string.IsNullOrEmpty(dataService?.CurrentFilePath))
        {
            var dir = Path.GetDirectoryName(dataService.CurrentFilePath);
            if (dir is not null)
            {
                var path = Path.Combine(dir, "SystemData.json");
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        // 2. Solution root (development)
        var exeDir = AppContext.BaseDirectory;
        var solutionDir = FindSolutionDirectory(exeDir);
        if (solutionDir is not null)
        {
            var winFormsPath = Path.Combine(solutionDir, "OE2EmpireTracker", "SystemData.json");
            if (File.Exists(winFormsPath))
            {
                return winFormsPath;
            }
        }

        // 3. Platform data directory
        var dataDir = _fileSystem.GetDataDirectory();
        var dataPath = Path.Combine(dataDir, "SystemData.json");
        if (File.Exists(dataPath))
        {
            return dataPath;
        }

        return null;
    }

    /// <summary>
    /// Import entry matching the oe2-galaxy-systems.json format.
    /// </summary>
    private sealed class GalaxyImportEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("n")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("x")]
        public decimal X { get; set; }

        [JsonProperty("y")]
        public decimal Y { get; set; }

        [JsonProperty("q")]
        public int Quadrant { get; set; }

        [JsonProperty("s")]
        public int Sector { get; set; }

        [JsonProperty("r")]
        public int Region { get; set; }

        [JsonProperty("l")]
        public int Locality { get; set; }

        [JsonProperty("st")]
        public string SpectralClass { get; set; } = string.Empty;

        [JsonProperty("fid")]
        public int FactionId { get; set; }

        [JsonProperty("fn")]
        public string FactionName { get; set; } = string.Empty;

        [JsonProperty("fc")]
        public string FactionColor { get; set; } = string.Empty;

        [JsonProperty("o")]
        public int Orbital { get; set; }

        [JsonProperty("sp")]
        public int Spaceport { get; set; }

        [JsonProperty("sb")]
        public int Starbase { get; set; }
    }
}
