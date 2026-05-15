using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Lightweight data service that loads the same JSON files as the WinForms client.
/// Provides read-only access to player and baseline data for the Avalonia UI.
/// </summary>
public sealed class DataService
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<DataService> _logger;

    private PlayerRoot? _playerRoot;
    private BaselineRoot? _baselineRoot;

    public DataService(IFileSystemService fileSystem, ILogger<DataService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>Gets the current player UUID.</summary>
    public string CurrentPlayerUUID => _playerRoot?.CurrentPlayerUUID ?? string.Empty;

    /// <summary>Gets all player profiles.</summary>
    public IReadOnlyList<PlayerProfile> PlayerProfiles =>
        _playerRoot?.PlayerProfile ?? Array.Empty<PlayerProfile>();

    /// <summary>Gets all blueprints.</summary>
    public IReadOnlyList<Blueprint> Blueprints =>
        _playerRoot?.Blueprint ?? Array.Empty<Blueprint>();

    /// <summary>Gets all colonies.</summary>
    public IReadOnlyList<Colony> Colonies =>
        _playerRoot?.Colony ?? Array.Empty<Colony>();

    /// <summary>Gets all surveys.</summary>
    public IReadOnlyList<Survey> Surveys =>
        _playerRoot?.Survey ?? Array.Empty<Survey>();

    /// <summary>Gets all delivery routes.</summary>
    public IReadOnlyList<DeliveryRoute> DeliveryRoutes =>
        _playerRoot?.DeliveryRoute ?? Array.Empty<DeliveryRoute>();

    /// <summary>Gets all delivery plans.</summary>
    public IReadOnlyList<DeliveryPlan> DeliveryPlans =>
        _playerRoot?.DeliveryPlan ?? Array.Empty<DeliveryPlan>();

    /// <summary>Gets all ship templates.</summary>
    public IReadOnlyList<ShipTemplate> ShipTemplates =>
        _playerRoot?.ShipTemplate ?? Array.Empty<ShipTemplate>();

    /// <summary>Gets all ships.</summary>
    public IReadOnlyList<Ship> Ships =>
        _playerRoot?.Ship ?? Array.Empty<Ship>();

    /// <summary>Gets all stations.</summary>
    public IReadOnlyList<Station> Stations =>
        _playerRoot?.Station ?? Array.Empty<Station>();

    /// <summary>Gets all pricing plans.</summary>
    public IReadOnlyList<PricingPlan> PricingPlans =>
        _playerRoot?.PricingPlan ?? Array.Empty<PricingPlan>();

    /// <summary>Gets all build plans.</summary>
    public IReadOnlyList<BuildPlan> BuildPlans =>
        _playerRoot?.BuildPlan ?? Array.Empty<BuildPlan>();

    /// <summary>Gets all market listings.</summary>
    public IReadOnlyList<MarketListing> MarketListings =>
        _playerRoot?.MarketListing ?? Array.Empty<MarketListing>();

    /// <summary>Gets all stock plans.</summary>
    public IReadOnlyList<StockPlan> StockPlans =>
        _playerRoot?.StockPlan ?? Array.Empty<StockPlan>();

    /// <summary>Gets all stock profiles.</summary>
    public IReadOnlyList<StockProfile> StockProfiles =>
        _playerRoot?.StockProfile ?? Array.Empty<StockProfile>();

    /// <summary>Gets all supply chains.</summary>
    public IReadOnlyList<SupplyChain> SupplyChains =>
        _playerRoot?.SupplyChain ?? Array.Empty<SupplyChain>();

    /// <summary>Gets all asteroids.</summary>
    public IReadOnlyList<Asteroid> Asteroids =>
        _playerRoot?.Asteroid ?? Array.Empty<Asteroid>();

    /// <summary>Gets all factions.</summary>
    public IReadOnlyList<Faction> Factions =>
        _playerRoot?.Faction ?? Array.Empty<Faction>();

    /// <summary>Gets all external characters.</summary>
    public IReadOnlyList<ExternalCharacter> ExternalCharacters =>
        _playerRoot?.ExternalCharacter ?? Array.Empty<ExternalCharacter>();

    // --- Baseline data ---

    /// <summary>Gets all blueprint types.</summary>
    public IReadOnlyList<BlueprintType> BlueprintTypes =>
        _baselineRoot?.BlueprintType ?? Array.Empty<BlueprintType>();

    /// <summary>Gets all ship classes.</summary>
    public IReadOnlyList<ShipClass> ShipClasses =>
        _baselineRoot?.ShipClass ?? Array.Empty<ShipClass>();

    /// <summary>Gets all commodities.</summary>
    public IReadOnlyList<Commodity> Commodities =>
        _baselineRoot?.Commodity ?? Array.Empty<Commodity>();

    /// <summary>Gets whether data has been loaded.</summary>
    public bool IsLoaded => _playerRoot is not null;

    /// <summary>
    /// Sets the current player UUID. Called when the user switches players via the combo box.
    /// </summary>
    public void SetCurrentPlayer(string playerUuid)
    {
        if (_playerRoot is not null)
        {
            _playerRoot.CurrentPlayerUUID = playerUuid;
            _logger.LogInformation("Current player set to {UUID}", playerUuid);
        }
    }

    /// <summary>
    /// Loads player data from a specific file path (File → Open).
    /// Also looks for BaselineData.json in the same directory.
    /// </summary>
    public void LoadFromFile(string playerDataPath)
    {
        try
        {
            var json = File.ReadAllText(playerDataPath);
            _playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(json);
            _logger.LogInformation("Loaded player data from {Path}: {Colonies} colonies, {Blueprints} blueprints",
                playerDataPath,
                _playerRoot?.Colony?.Length ?? 0,
                _playerRoot?.Blueprint?.Length ?? 0);

            // Try to load BaselineData.json from the same directory
            var dir = Path.GetDirectoryName(playerDataPath);
            if (dir is not null)
            {
                var baselinePath = Path.Combine(dir, "BaselineData.json");
                if (File.Exists(baselinePath))
                {
                    var baselineJson = File.ReadAllText(baselinePath);
                    _baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(baselineJson);
                    _logger.LogInformation("Loaded baseline data from {Path}", baselinePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load player data from {Path}", playerDataPath);
        }
    }

    /// <summary>
    /// Loads player and baseline data from JSON files.
    /// Looks in the WinForms project directory first (for development),
    /// then falls back to the platform data directory.
    /// </summary>
    public void LoadData()
    {
        var playerPath = FindDataFile("PlayerData.json");
        var baselinePath = FindDataFile("BaselineData.json");

        if (playerPath is not null)
        {
            try
            {
                var json = File.ReadAllText(playerPath);
                _playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(json);
                _logger.LogInformation("Loaded player data from {Path}: {Count} profiles",
                    playerPath, _playerRoot?.PlayerProfile?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load player data from {Path}", playerPath);
            }
        }
        else
        {
            _logger.LogWarning("PlayerData.json not found");
        }

        if (baselinePath is not null)
        {
            try
            {
                var json = File.ReadAllText(baselinePath);
                _baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(json);
                _logger.LogInformation("Loaded baseline data from {Path}: {Count} blueprint types",
                    baselinePath, _baselineRoot?.BlueprintType?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load baseline data from {Path}", baselinePath);
            }
        }
        else
        {
            _logger.LogWarning("BaselineData.json not found");
        }
    }

    /// <summary>Gets colonies for the current player.</summary>
    public List<Colony> GetCurrentPlayerColonies()
    {
        if (_playerRoot is null)
        {
            return new List<Colony>();
        }

        return Colonies
            .Where(c => c.OwnerUUID == CurrentPlayerUUID)
            .ToList();
    }

    /// <summary>Gets blueprints for the current player.</summary>
    public List<Blueprint> GetCurrentPlayerBlueprints()
    {
        if (_playerRoot is null)
        {
            return new List<Blueprint>();
        }

        return Blueprints
            .Where(b => b.OwnerUUID == CurrentPlayerUUID)
            .ToList();
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

    private string? FindDataFile(string fileName)
    {
        // 1. Check alongside the executable (development)
        var exeDir = AppContext.BaseDirectory;
        var exePath = Path.Combine(exeDir, fileName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        // 2. Check the WinForms project directory (development — sibling project)
        var solutionDir = FindSolutionDirectory(exeDir);
        if (solutionDir is not null)
        {
            var winFormsPath = Path.Combine(solutionDir, "OE2EmpireTracker", fileName);
            if (File.Exists(winFormsPath))
            {
                return winFormsPath;
            }
        }

        // 3. Check platform data directory
        var dataDir = _fileSystem.GetDataDirectory();
        var dataPath = Path.Combine(dataDir, fileName);
        if (File.Exists(dataPath))
        {
            return dataPath;
        }

        return null;
    }
}

/// <summary>
/// JSON root for PlayerData.json. Mirrors the WinForms PlayerRoot structure.
/// </summary>
public sealed class PlayerRoot
{
    public int DataVersion { get; set; }

    public string CurrentPlayerUUID { get; set; } = string.Empty;

    public PlayerProfile[] PlayerProfile { get; set; } = Array.Empty<PlayerProfile>();

    public Blueprint[] Blueprint { get; set; } = Array.Empty<Blueprint>();

    public Survey[] Survey { get; set; } = Array.Empty<Survey>();

    public Colony[] Colony { get; set; } = Array.Empty<Colony>();

    public DeliveryRoute[] DeliveryRoute { get; set; } = Array.Empty<DeliveryRoute>();

    public DeliveryPlan[] DeliveryPlan { get; set; } = Array.Empty<DeliveryPlan>();

    public PricingPlan[] PricingPlan { get; set; } = Array.Empty<PricingPlan>();

    public BuildPlan[] BuildPlan { get; set; } = Array.Empty<BuildPlan>();

    public ShipTemplate[] ShipTemplate { get; set; } = Array.Empty<ShipTemplate>();

    public Ship[] Ship { get; set; } = Array.Empty<Ship>();

    public Station[] Station { get; set; } = Array.Empty<Station>();

    public MarketListing[] MarketListing { get; set; } = Array.Empty<MarketListing>();

    public MarketTransaction[] MarketTransaction { get; set; } = Array.Empty<MarketTransaction>();

    public StockPlan[] StockPlan { get; set; } = Array.Empty<StockPlan>();

    public StockProfile[] StockProfile { get; set; } = Array.Empty<StockProfile>();

    public SupplyChain[] SupplyChain { get; set; } = Array.Empty<SupplyChain>();

    public WarehouseOverflowRule[] WarehouseOverflowRule { get; set; } = Array.Empty<WarehouseOverflowRule>();

    public Faction[] Faction { get; set; } = Array.Empty<Faction>();

    public ExternalCharacter[] ExternalCharacter { get; set; } = Array.Empty<ExternalCharacter>();

    public Asteroid[] Asteroid { get; set; } = Array.Empty<Asteroid>();
}

/// <summary>
/// JSON root for BaselineData.json. Mirrors the WinForms BaselineRoot structure.
/// </summary>
public sealed class BaselineRoot
{
    public int DataVersion { get; set; }

    public BaselineGameConstants? GameConstants { get; set; }

    public ShipClass[] ShipClass { get; set; } = Array.Empty<ShipClass>();

    public BlueprintType[] BlueprintType { get; set; } = Array.Empty<BlueprintType>();

    public Blueprint[] Blueprint { get; set; } = Array.Empty<Blueprint>();

    public TechLevel[] TechLevel { get; set; } = Array.Empty<TechLevel>();

    public Commodity[] Commodity { get; set; } = Array.Empty<Commodity>();

    public RefiningRecipe[] RefiningRecipe { get; set; } = Array.Empty<RefiningRecipe>();

    public ResearchTimeEntry[] ResearchTime { get; set; } = Array.Empty<ResearchTimeEntry>();
}
