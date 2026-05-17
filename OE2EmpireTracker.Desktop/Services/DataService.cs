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

    /// <summary>Gets or sets the path the current data was loaded from (or saved to).</summary>
    public string? CurrentFilePath { get; set; }

    /// <summary>Gets or sets a value indicating whether data has been modified since last save.</summary>
    public bool IsDirty { get; set; }

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

    /// <summary>Gets all market transactions.</summary>
    public IReadOnlyList<MarketTransaction> MarketTransactions =>
        _playerRoot?.MarketTransaction ?? Array.Empty<MarketTransaction>();

    /// <summary>Gets all stock plans.</summary>
    public IReadOnlyList<StockPlan> StockPlans =>
        _playerRoot?.StockPlan ?? Array.Empty<StockPlan>();

    /// <summary>Gets all stock profiles.</summary>
    public IReadOnlyList<StockProfile> StockProfiles =>
        _playerRoot?.StockProfile ?? Array.Empty<StockProfile>();

    /// <summary>Gets all supply chains.</summary>
    public IReadOnlyList<SupplyChain> SupplyChains =>
        _playerRoot?.SupplyChain ?? Array.Empty<SupplyChain>();

    /// <summary>Gets all warehouse overflow rules.</summary>
    public IReadOnlyList<WarehouseOverflowRule> WarehouseOverflowRules =>
        _playerRoot?.WarehouseOverflowRule ?? Array.Empty<WarehouseOverflowRule>();

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

    /// <summary>Gets or sets a value indicating whether stock targets need re-evaluation (REQ-BP-070).</summary>
    public bool CascadeStockTargetsDirty { get; set; }

    /// <summary>Gets whether data has been loaded.</summary>
    public bool IsLoaded => _playerRoot is not null;

    /// <summary>
    /// Sets the current player UUID. Called when the user switches players via the combo box.
    /// Fires <see cref="PlayerChangedMessage"/> after updating.
    /// </summary>
    public void SetCurrentPlayer(string playerUuid)
    {
        if (_playerRoot is not null)
        {
            _playerRoot.CurrentPlayerUUID = playerUuid;
            _logger.LogInformation("Current player set to {UUID}", playerUuid);
            OnCurrentPlayerChanged();
        }
    }

    // --- Data change notification methods (A2.1) ---

    /// <summary>Sends <see cref="PlayerChangedMessage"/> to all subscribers.</summary>
    public void OnCurrentPlayerChanged()
    {
        WeakReferenceMessenger.Default.Send(new PlayerChangedMessage(CurrentPlayerUUID));
    }

    /// <summary>Sends <see cref="ColonyDataChangedMessage"/> to all subscribers.</summary>
    public void OnColonyDataChanged(string colonyUuid)
    {
        WeakReferenceMessenger.Default.Send(new ColonyDataChangedMessage(colonyUuid));
    }

    /// <summary>Sends <see cref="BlueprintDataChangedMessage"/> to all subscribers.</summary>
    public void OnBlueprintDataChanged(string blueprintUuid)
    {
        WeakReferenceMessenger.Default.Send(new BlueprintDataChangedMessage(blueprintUuid));
    }

    /// <summary>Sends <see cref="SurveyDataChangedMessage"/> to all subscribers.</summary>
    public void OnSurveyDataChanged(string surveyUuid)
    {
        WeakReferenceMessenger.Default.Send(new SurveyDataChangedMessage(surveyUuid));
    }

    /// <summary>Sends <see cref="DeliveryDataChangedMessage"/> to all subscribers.</summary>
    public void OnDeliveryDataChanged()
    {
        WeakReferenceMessenger.Default.Send(new DeliveryDataChangedMessage());
    }

    /// <summary>Sends <see cref="PlayerProfileDataChangedMessage"/> to all subscribers.</summary>
    public void OnPlayerProfileDataChanged(string playerUuid)
    {
        WeakReferenceMessenger.Default.Send(new PlayerProfileDataChangedMessage(playerUuid));
    }

    /// <summary>Sends <see cref="PricingDataChangedMessage"/> to all subscribers.</summary>
    public void OnPricingDataChanged()
    {
        WeakReferenceMessenger.Default.Send(new PricingDataChangedMessage());
    }

    /// <summary>Sends <see cref="BuildPlanDataChangedMessage"/> to all subscribers.</summary>
    public void OnBuildPlanDataChanged(string buildPlanUuid)
    {
        WeakReferenceMessenger.Default.Send(new BuildPlanDataChangedMessage(buildPlanUuid));
    }

    /// <summary>Sends <see cref="MarketDataChangedMessage"/> to all subscribers.</summary>
    public void OnMarketDataChanged()
    {
        WeakReferenceMessenger.Default.Send(new MarketDataChangedMessage());
    }

    /// <summary>Sends <see cref="StationDataChangedMessage"/> to all subscribers.</summary>
    public void OnStationDataChanged()
    {
        WeakReferenceMessenger.Default.Send(new StationDataChangedMessage());
    }

    /// <summary>Sends <see cref="AsteroidDataChangedMessage"/> to all subscribers.</summary>
    public void OnAsteroidDataChanged(string asteroidUuid)
    {
        WeakReferenceMessenger.Default.Send(new AsteroidDataChangedMessage(asteroidUuid));
    }

    /// <summary>Sends <see cref="ShipTemplateDataChangedMessage"/> to all subscribers.</summary>
    public void OnShipTemplateDataChanged(string shipTemplateUuid)
    {
        WeakReferenceMessenger.Default.Send(new ShipTemplateDataChangedMessage(shipTemplateUuid));
    }

    /// <summary>Sends <see cref="ShipDataChangedMessage"/> to all subscribers.</summary>
    public void OnShipDataChanged(string shipUuid)
    {
        WeakReferenceMessenger.Default.Send(new ShipDataChangedMessage(shipUuid));
    }

    /// <summary>Sends <see cref="StockDataChangedMessage"/> to all subscribers.</summary>
    public void OnStockDataChanged(string stockPlanUuid)
    {
        WeakReferenceMessenger.Default.Send(new StockDataChangedMessage(stockPlanUuid));
    }

    /// <summary>Sends <see cref="SupplyChainDataChangedMessage"/> to all subscribers.</summary>
    public void OnSupplyChainDataChanged(string supplyChainUuid)
    {
        WeakReferenceMessenger.Default.Send(new SupplyChainDataChangedMessage(supplyChainUuid));
    }

    /// <summary>Sends <see cref="ContactDataChangedMessage"/> to all subscribers.</summary>
    public void OnContactDataChanged(string characterUuid)
    {
        WeakReferenceMessenger.Default.Send(new ContactDataChangedMessage(characterUuid));
    }

    /// <summary>
    /// Serializes the current PlayerRoot to JSON and writes it via SafeFileWriter.
    /// Uses <see cref="CurrentFilePath"/> as the target.
    /// </summary>
    /// <returns>True if the write succeeded; false otherwise.</returns>
    public bool WriteContext()
    {
        if (_playerRoot is null)
        {
            _logger.LogWarning("WriteContext called with no data loaded");
            return false;
        }

        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            _logger.LogWarning("WriteContext called with no CurrentFilePath set");
            return false;
        }

        var json = JsonConvert.SerializeObject(_playerRoot, Formatting.Indented);
        try
        {
            OE2EmpireTracker.Persistence.SafeFileWriter.WriteAllText(CurrentFilePath, json);
            IsDirty = false;
            _logger.LogInformation("WriteContext completed to {Path}", CurrentFilePath);
            return true;
        }
        catch (System.IO.IOException ex)
        {
            _logger.LogError(ex, "WriteContext failed for {Path}", CurrentFilePath);
            return false;
        }
    }

    /// <summary>
    /// Creates a new empty context, clearing all data.
    /// </summary>
    public void NewContext()
    {
        _playerRoot = new PlayerRoot();
        CurrentFilePath = null;
        IsDirty = false;
        _logger.LogInformation("New context created");
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
            CurrentFilePath = playerDataPath;
            IsDirty = false;
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
                CurrentFilePath = playerPath;
                IsDirty = false;
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

    // --- Mutation helpers for service layer ---

    /// <summary>Adds a colony to the player root.</summary>
    public void AddColony(Colony colony)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Colony.ToList();
        list.Add(colony);
        _playerRoot.Colony = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a colony by UUID.</summary>
    public void RemoveColony(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Colony = _playerRoot.Colony.Where(c => c.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a blueprint to the player root.</summary>
    public void AddBlueprint(Blueprint blueprint)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Blueprint.ToList();
        list.Add(blueprint);
        _playerRoot.Blueprint = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a blueprint by UUID.</summary>
    public void RemoveBlueprint(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Blueprint = _playerRoot.Blueprint.Where(b => b.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a survey to the player root.</summary>
    public void AddSurvey(Survey survey)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Survey.ToList();
        list.Add(survey);
        _playerRoot.Survey = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a survey by UUID.</summary>
    public void RemoveSurvey(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Survey = _playerRoot.Survey.Where(s => s.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a player profile to the player root.</summary>
    public void AddPlayerProfile(PlayerProfile profile)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.PlayerProfile.ToList();
        list.Add(profile);
        _playerRoot.PlayerProfile = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a player profile by UUID.</summary>
    public void RemovePlayerProfile(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.PlayerProfile = _playerRoot.PlayerProfile.Where(p => p.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a delivery route to the player root.</summary>
    public void AddDeliveryRoute(DeliveryRoute route)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.DeliveryRoute.ToList();
        list.Add(route);
        _playerRoot.DeliveryRoute = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a delivery route by UUID.</summary>
    public void RemoveDeliveryRoute(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.DeliveryRoute = _playerRoot.DeliveryRoute.Where(r => r.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a delivery plan to the player root.</summary>
    public void AddDeliveryPlan(DeliveryPlan plan)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.DeliveryPlan.ToList();
        list.Add(plan);
        _playerRoot.DeliveryPlan = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a delivery plan by UUID.</summary>
    public void RemoveDeliveryPlan(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.DeliveryPlan = _playerRoot.DeliveryPlan.Where(p => p.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a pricing plan to the player root.</summary>
    public void AddPricingPlan(PricingPlan plan)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.PricingPlan.ToList();
        list.Add(plan);
        _playerRoot.PricingPlan = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a pricing plan by UUID.</summary>
    public void RemovePricingPlan(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.PricingPlan = _playerRoot.PricingPlan.Where(p => p.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a build plan to the player root.</summary>
    public void AddBuildPlan(BuildPlan plan)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.BuildPlan.ToList();
        list.Add(plan);
        _playerRoot.BuildPlan = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a build plan by UUID.</summary>
    public void RemoveBuildPlan(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.BuildPlan = _playerRoot.BuildPlan.Where(p => p.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a market listing to the player root.</summary>
    public void AddMarketListing(MarketListing listing)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.MarketListing.ToList();
        list.Add(listing);
        _playerRoot.MarketListing = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a market listing by UUID.</summary>
    public void RemoveMarketListing(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.MarketListing = _playerRoot.MarketListing.Where(m => m.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a market transaction to the player root.</summary>
    public void AddMarketTransaction(MarketTransaction transaction)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.MarketTransaction.ToList();
        list.Add(transaction);
        _playerRoot.MarketTransaction = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a station to the player root.</summary>
    public void AddStation(Station station)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Station.ToList();
        list.Add(station);
        _playerRoot.Station = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a station by UUID.</summary>
    public void RemoveStation(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Station = _playerRoot.Station.Where(s => s.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a ship template to the player root.</summary>
    public void AddShipTemplate(ShipTemplate template)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.ShipTemplate.ToList();
        list.Add(template);
        _playerRoot.ShipTemplate = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a ship template by UUID.</summary>
    public void RemoveShipTemplate(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.ShipTemplate = _playerRoot.ShipTemplate.Where(t => t.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a ship to the player root.</summary>
    public void AddShip(Ship ship)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Ship.ToList();
        list.Add(ship);
        _playerRoot.Ship = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a ship by UUID.</summary>
    public void RemoveShip(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Ship = _playerRoot.Ship.Where(s => s.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a stock plan to the player root.</summary>
    public void AddStockPlan(StockPlan plan)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.StockPlan.ToList();
        list.Add(plan);
        _playerRoot.StockPlan = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a stock plan by UUID.</summary>
    public void RemoveStockPlan(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.StockPlan = _playerRoot.StockPlan.Where(p => p.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a supply chain to the player root.</summary>
    public void AddSupplyChain(SupplyChain chain)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.SupplyChain.ToList();
        list.Add(chain);
        _playerRoot.SupplyChain = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a supply chain by UUID.</summary>
    public void RemoveSupplyChain(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.SupplyChain = _playerRoot.SupplyChain.Where(c => c.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a faction to the player root.</summary>
    public void AddFaction(Faction faction)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Faction.ToList();
        list.Add(faction);
        _playerRoot.Faction = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a faction by UUID.</summary>
    public void RemoveFaction(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Faction = _playerRoot.Faction.Where(f => f.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds an external character to the player root.</summary>
    public void AddExternalCharacter(ExternalCharacter character)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.ExternalCharacter.ToList();
        list.Add(character);
        _playerRoot.ExternalCharacter = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes an external character by UUID.</summary>
    public void RemoveExternalCharacter(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.ExternalCharacter = _playerRoot.ExternalCharacter.Where(c => c.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds an asteroid to the player root.</summary>
    public void AddAsteroid(Asteroid asteroid)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.Asteroid.ToList();
        list.Add(asteroid);
        _playerRoot.Asteroid = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes an asteroid by UUID.</summary>
    public void RemoveAsteroid(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.Asteroid = _playerRoot.Asteroid.Where(a => a.UUID != uuid).ToArray();
        IsDirty = true;
    }

    /// <summary>Adds a warehouse overflow rule to the player root.</summary>
    public void AddWarehouseOverflowRule(WarehouseOverflowRule rule)
    {
        if (_playerRoot is null) return;
        var list = _playerRoot.WarehouseOverflowRule.ToList();
        list.Add(rule);
        _playerRoot.WarehouseOverflowRule = list.ToArray();
        IsDirty = true;
    }

    /// <summary>Removes a warehouse overflow rule by UUID.</summary>
    public void RemoveWarehouseOverflowRule(string uuid)
    {
        if (_playerRoot is null) return;
        _playerRoot.WarehouseOverflowRule = _playerRoot.WarehouseOverflowRule.Where(r => r.UUID != uuid).ToArray();
        IsDirty = true;
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
