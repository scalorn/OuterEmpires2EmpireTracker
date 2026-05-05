using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    public class PlayerContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static PlayerContext _instance;

        private readonly object _listLock = new object();

        private string _currentPlayerUUID = string.Empty;

        private Dictionary<string, Blueprint> _blueprintCache;
        private Dictionary<string, Survey> _surveyCache;
        private Dictionary<string, Colony> _colonyCache;
        private List<Blueprint> _allBlueprintsCache;
        private Dictionary<string, Station> _stationCache;
        private Dictionary<string, ShipTemplate> _shipTemplateCache;
        private Dictionary<string, Ship> _shipCache;
        private Dictionary<string, BuildPlan> _buildPlanCache;
        private Dictionary<string, Asteroid> _asteroidCache;
        private Dictionary<string, Faction> _factionCache;
        private Dictionary<string, MarketListing> _marketListingCache;
        private Dictionary<string, PlayerProfile> _playerProfileCache;
        private Dictionary<string, DeliveryRoute> _deliveryRouteCache;
        private Dictionary<string, DeliveryPlan> _deliveryPlanCache;
        private Dictionary<string, PricingPlan> _pricingPlanCache;
        private Dictionary<string, MarketTransaction> _marketTransactionCache;
        private Dictionary<string, StockPlan> _stockPlanCache;
        private Dictionary<string, StockProfile> _stockProfileCache;
        private Dictionary<string, SupplyChain> _supplyChainCache;
        private Dictionary<string, WarehouseOverflowRule> _warehouseOverflowRuleCache;
        private Dictionary<string, ExternalCharacter> _externalCharacterCache;
        private Dictionary<string, int> _blueprintTypeCountCache;
        private Dictionary<string, List<BuildItem>> _blueprintBuildItemIndex;
        private Dictionary<string, List<BuildItem>> _buildLocationBuildItemIndex;
        private List<PlayerProfile> _playerProfileList;
        private List<Blueprint> _blueprintList;
        private List<Survey> _surveyList;
        private List<Colony> _colonyList;
        private List<DeliveryRoute> _deliveryRouteList;
        private List<DeliveryPlan> _deliveryPlanList;
        private List<PricingPlan> _pricingPlanList;
        private List<BuildPlan> _buildPlanList = new List<BuildPlan>();
        private List<ShipTemplate> _shipTemplateList = new List<ShipTemplate>();
        private List<Ship> _shipList = new List<Ship>();
        private List<Station> _stationList = new List<Station>();
        private List<MarketListing> _marketListingList = new List<MarketListing>();
        private List<MarketTransaction> _marketTransactionList = new List<MarketTransaction>();
        private List<StockPlan> _stockPlanList = new List<StockPlan>();
        private List<StockProfile> _stockProfileList = new List<StockProfile>();
        private List<SupplyChain> _supplyChainList = new List<SupplyChain>();
        private List<WarehouseOverflowRule> _warehouseOverflowRuleList = new List<WarehouseOverflowRule>();
        private List<Faction> _factionList = new List<Faction>();
        private List<ExternalCharacter> _externalCharacterList = new List<ExternalCharacter>();
        private List<Asteroid> _asteroidList = new List<Asteroid>();

        /// <summary>
        /// Internal constructor for test infrastructure. Accepts a pre-parsed
        /// PlayerRoot so tests can skip File.ReadAllText and JsonConvert.
        /// </summary>
        internal PlayerContext(PlayerRoot playerRoot) : base()
        {
            _instance = this;

            InitPlayerProfiles(playerRoot);
            InitBlueprints(playerRoot);
            InitSurveys(playerRoot);
            InitColonies(playerRoot);
            InitDeliveryRoutes(playerRoot);
            InitDeliveryPlans(playerRoot);
            InitPricingPlans(playerRoot);
            InitBuildPlans(playerRoot);
            InitShipTemplates(playerRoot);
            InitShips(playerRoot);
            InitStations(playerRoot);
            InitMarketListings(playerRoot);
            InitMarketTransactions(playerRoot);
            InitStockPlans(playerRoot);
            InitStockProfiles(playerRoot);
            InitSupplyChains(playerRoot);
            InitWarehouseOverflowRules(playerRoot);
            InitFactions(playerRoot);
            InitExternalCharacters(playerRoot);
            InitAsteroids(playerRoot);
            DataVersion = playerRoot.DataVersion;

            // Migrate and restore current player
            MigrateOwnerUUIDs();
            CleanupOrphanedData();
            RestoreCurrentPlayer(playerRoot.CurrentPlayerUUID);
        }

        private PlayerContext() : base()
        {
            _instance = this;

            PlayerRoot playerRoot = null;
            if (File.Exists(FilePath))
            {
                Log.Info("Loading player data from {0}", FilePath);
                string jsonContent = File.ReadAllText(FilePath);
                playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(jsonContent);
                Log.Info(
                    "Loaded {0} profiles, {1} blueprints, {2} surveys, {3} colonies",
                    playerRoot.PlayerProfile.Length,
                    playerRoot.Blueprint.Length,
                    playerRoot.Survey.Length,
                    playerRoot.Colony.Length);
            }
            else
            {
                Log.Warn("Player data file not found at {0}, starting with empty data", FilePath);
                playerRoot = new PlayerRoot();
            }

            InitPlayerProfiles(playerRoot);
            InitBlueprints(playerRoot);
            InitSurveys(playerRoot);
            InitColonies(playerRoot);
            InitDeliveryRoutes(playerRoot);
            InitDeliveryPlans(playerRoot);
            InitPricingPlans(playerRoot);
            InitBuildPlans(playerRoot);
            InitShipTemplates(playerRoot);
            InitShips(playerRoot);
            InitStations(playerRoot);
            InitMarketListings(playerRoot);
            InitMarketTransactions(playerRoot);
            InitStockPlans(playerRoot);
            InitStockProfiles(playerRoot);
            InitSupplyChains(playerRoot);
            InitWarehouseOverflowRules(playerRoot);
            InitFactions(playerRoot);
            InitExternalCharacters(playerRoot);
            InitAsteroids(playerRoot);
            DataVersion = playerRoot.DataVersion;

            // Migrate and restore current player
            MigrateOwnerUUIDs();
            CleanupOrphanedData();
            RestoreCurrentPlayer(playerRoot.CurrentPlayerUUID);
        }

        /// <summary>
        /// Fired when CurrentPlayerUUID changes. Forms subscribe to refresh their data.
        /// </summary>
        public event EventHandler CurrentPlayerChanged;

        /// <summary>
        /// Fired when the player profile list changes (add/rename/delete).
        /// MainWindow subscribes to refresh the player dropdown.
        /// </summary>
        public event EventHandler PlayerProfilesChanged;

        /// <summary>
        /// Fired when colony data is modified externally (e.g. delivery fulfillment).
        /// Forms showing colony data subscribe to refresh. EventArgs carries the colony UUID.
        /// </summary>
        public event EventHandler<ColonyDataChangedEventArgs> ColonyDataChanged;

        /// <summary>
        /// Fired when blueprint data is modified externally (e.g. research completion, scan import).
        /// </summary>
        public event EventHandler<BlueprintDataChangedEventArgs> BlueprintDataChanged;

        /// <summary>
        /// Fired when survey data is modified externally.
        /// </summary>
        public event EventHandler<SurveyDataChangedEventArgs> SurveyDataChanged;

        /// <summary>
        /// Fired when delivery routes or plans are modified externally.
        /// </summary>
        public event EventHandler DeliveryDataChanged;

        /// <summary>
        /// Fired when pricing plan data is modified (resource prices, plan settings).
        /// </summary>
        public event EventHandler PricingDataChanged;

        /// <summary>
        /// Fired when build plan data is modified (add/edit/delete/status change).
        /// </summary>
        public event EventHandler<BuildPlanDataChangedEventArgs> BuildPlanDataChanged;

        /// <summary>
        /// Fired when market data is modified (listings or transactions).
        /// </summary>
        public event EventHandler MarketDataChanged;

        /// <summary>
        /// Fired when station data is modified.
        /// </summary>
        public event EventHandler StationDataChanged;

        /// <summary>
        /// Fired when asteroid data is modified (created, updated, or deleted).
        /// </summary>
        public event EventHandler<AsteroidDataChangedEventArgs> AsteroidDataChanged;

        /// <summary>
        /// Fired when a player profile is modified externally (e.g. skill training completion).
        /// </summary>
        public event EventHandler<PlayerProfileDataChangedEventArgs> PlayerProfileDataChanged;

        /// <summary>
        /// Fired when ship template data is modified (created, updated, or deleted).
        /// </summary>
        public event EventHandler<ShipTemplateDataChangedEventArgs> ShipTemplateDataChanged;

        /// <summary>
        /// Fired when ship instance data is modified (created, updated, or deleted).
        /// </summary>
        public event EventHandler<ShipDataChangedEventArgs> ShipDataChanged;

        /// <summary>
        /// Fired when stock plan or stock profile data is modified.
        /// </summary>
        public event EventHandler<StockDataChangedEventArgs> StockDataChanged;

        /// <summary>
        /// Fired when supply chain data is modified.
        /// </summary>
        public event EventHandler<SupplyChainDataChangedEventArgs> SupplyChainDataChanged;

        /// <summary>
        /// Fired when external character (contacts) data is modified.
        /// </summary>
        public event EventHandler<ContactDataChangedEventArgs> ContactDataChanged;

        /// <summary>
        /// When true, suppresses MessageBox dialogs (e.g. during unit tests).
        /// </summary>
        public static bool SuppressUI { get; set; }

        public static string FilePath { get; set; } = "PlayerData.json";

        /// <summary>
        /// UUID of the currently selected player. Forms filter data by this value.
        /// </summary>
        public string CurrentPlayerUUID
        {
            get => _currentPlayerUUID;
            set
            {
                if (_currentPlayerUUID != value)
                {
                    _currentPlayerUUID = value ?? string.Empty;
                    Log.Info("Current player changed to {0}", _currentPlayerUUID);
                    CurrentPlayerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Returns the PlayerProfile for the currently selected player, or null.
        /// </summary>
        public PlayerProfile CurrentPlayer
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPlayerUUID)) return null;
                return _playerProfileList.FirstOrDefault(p => p.UUID == _currentPlayerUUID);
            }
        }

        /// <summary>
        /// Runtime flag: set when stock targets need recalculation after a data change.
        /// Not serialized.
        /// </summary>
        [JsonIgnore]
        public bool CascadeStockTargetsDirty { get; set; } = false;

        /// <summary>
        /// Runtime flag: set when resource checks need recalculation after a data change.
        /// Not serialized.
        /// </summary>
        [JsonIgnore]
        public bool CascadeResourceCheckDirty { get; set; } = false;

        public int DataVersion { get; set; } = 0;

        public IReadOnlyList<PlayerProfile> PlayerProfileList => _playerProfileList;

        public BindingSource BindingSourcePlayerProfile { get; set; }

        public IReadOnlyList<Blueprint> BlueprintList => _blueprintList;

        public BindingSource BindingSourceBlueprint { get; set; }

        public IReadOnlyList<Survey> SurveyList => _surveyList;

        public BindingSource BindingSourceSurvey { get; set; }

        public IReadOnlyList<Colony> ColonyList => _colonyList;

        public BindingSource BindingSourceColony { get; set; }

        public IReadOnlyList<DeliveryRoute> DeliveryRouteList => _deliveryRouteList;

        public IReadOnlyList<DeliveryPlan> DeliveryPlanList => _deliveryPlanList;

        public IReadOnlyList<PricingPlan> PricingPlanList => _pricingPlanList;

        public IReadOnlyList<BuildPlan> BuildPlanList => _buildPlanList;

        public IReadOnlyList<ShipTemplate> ShipTemplateList => _shipTemplateList;

        public IReadOnlyList<Ship> ShipList => _shipList;

        public IReadOnlyList<Station> StationList => _stationList;

        public IReadOnlyList<MarketListing> MarketListingList => _marketListingList;

        public IReadOnlyList<MarketTransaction> MarketTransactionList => _marketTransactionList;

        public IReadOnlyList<StockPlan> StockPlanList => _stockPlanList;

        public IReadOnlyList<StockProfile> StockProfileList => _stockProfileList;

        public IReadOnlyList<SupplyChain> SupplyChainList => _supplyChainList;

        public IReadOnlyList<WarehouseOverflowRule> WarehouseOverflowRuleList => _warehouseOverflowRuleList;

        public IReadOnlyList<Faction> FactionList => _factionList;

        public IReadOnlyList<ExternalCharacter> ExternalCharacterList => _externalCharacterList;

        public IReadOnlyList<Asteroid> AsteroidList => _asteroidList;

        public IEnumerable<CountDownTimeReference> ActiveCountdowns => CollectionSortHelper.OrderCountdownsByTimeRemaining(
            AllCountdownSources()
                .Where(c => c.CountDownTime.TimeRemaining > 0));

        public static PlayerContext GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PlayerContext();
            }

            return _instance;
        }

        public static void Reset()
        {
            _instance = null;
        }

        /// <summary>
        /// Notifies subscribers that the player profile list has changed.
        /// </summary>
        public void OnPlayerProfilesChanged()
        {
            PlayerProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies subscribers that colony data has changed externally.
        /// </summary>
        public void OnColonyDataChanged(string colonyUUID)
        {
            ColonyDataChanged?.Invoke(this, new ColonyDataChangedEventArgs(colonyUUID));
        }

        /// <summary>
        /// Notifies subscribers that blueprint data has changed externally.
        /// </summary>
        public void OnBlueprintDataChanged(string blueprintUUID)
        {
            BlueprintDataChanged?.Invoke(this, new BlueprintDataChangedEventArgs(blueprintUUID));
        }

        /// <summary>
        /// Notifies subscribers that survey data has changed externally.
        /// </summary>
        public void OnSurveyDataChanged(string surveyUUID)
        {
            SurveyDataChanged?.Invoke(this, new SurveyDataChangedEventArgs(surveyUUID));
        }

        /// <summary>
        /// Notifies subscribers that delivery data has changed externally.
        /// </summary>
        public void OnDeliveryDataChanged()
        {
            DeliveryDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies subscribers that pricing plan data has changed.
        /// </summary>
        public void OnPricingDataChanged()
        {
            PricingDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies subscribers that a player profile has changed externally.
        /// </summary>
        public void OnPlayerProfileDataChanged(string playerUUID)
        {
            PlayerProfileDataChanged?.Invoke(this, new PlayerProfileDataChangedEventArgs(playerUUID));
        }

        /// <summary>
        /// Notifies subscribers that build plan data has changed externally.
        /// </summary>
        public void OnBuildPlanDataChanged(string buildPlanUUID)
        {
            BuildPlanDataChanged?.Invoke(this, new BuildPlanDataChangedEventArgs(buildPlanUUID));
        }

        /// <summary>
        /// Notifies subscribers that market data has changed externally.
        /// </summary>
        public void OnMarketDataChanged()
        {
            MarketDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies subscribers that station data has changed externally.
        /// </summary>
        public void OnStationDataChanged()
        {
            StationDataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Notifies subscribers that asteroid data has changed externally.
        /// </summary>
        public void OnAsteroidDataChanged(string asteroidUUID)
        {
            AsteroidDataChanged?.Invoke(this, new AsteroidDataChangedEventArgs(asteroidUUID));
        }

        /// <summary>
        /// Notifies subscribers that ship template data has changed externally.
        /// </summary>
        public void OnShipTemplateDataChanged(string shipTemplateUUID)
        {
            ShipTemplateDataChanged?.Invoke(this, new ShipTemplateDataChangedEventArgs(shipTemplateUUID));
        }

        /// <summary>
        /// Notifies subscribers that ship instance data has changed externally.
        /// </summary>
        public void OnShipDataChanged(string shipUUID)
        {
            ShipDataChanged?.Invoke(this, new ShipDataChangedEventArgs(shipUUID));
        }

        /// <summary>
        /// Notifies subscribers that stock plan/profile data has changed externally.
        /// </summary>
        public void OnStockDataChanged(string stockPlanUUID)
        {
            StockDataChanged?.Invoke(this, new StockDataChangedEventArgs(stockPlanUUID));
        }

        /// <summary>
        /// Notifies subscribers that supply chain data has changed externally.
        /// </summary>
        public void OnSupplyChainDataChanged(string supplyChainUUID)
        {
            SupplyChainDataChanged?.Invoke(this, new SupplyChainDataChangedEventArgs(supplyChainUUID));
        }

        /// <summary>
        /// Notifies subscribers that contact (external character) data has changed externally.
        /// </summary>
        public void OnContactDataChanged(string characterUUID)
        {
            ContactDataChanged?.Invoke(this, new ContactDataChangedEventArgs(characterUUID));
        }

        public void WriteContext()
        {
            if (MigrationRunner.MigrationFailed)
            {
                Log.Warn("WriteContext blocked -- migration failed, saving disabled");
                return;
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                Log.Debug("WriteContext skipped -- no file path set (not yet saved)");
                return;
            }

            PlayerRoot playerRoot;
            lock (_listLock)
            {
                playerRoot = new PlayerRoot();
                playerRoot.DataVersion = DataVersion;
                playerRoot.CurrentPlayerUUID = _currentPlayerUUID;
                playerRoot.PlayerProfile = _playerProfileList.ToArray();
                playerRoot.Blueprint = _blueprintList.ToArray();
                playerRoot.Survey = _surveyList.ToArray();
                playerRoot.Colony = _colonyList.ToArray();
                playerRoot.DeliveryRoute = _deliveryRouteList.ToArray();
                playerRoot.DeliveryPlan = _deliveryPlanList.ToArray();
                playerRoot.PricingPlan = _pricingPlanList.ToArray();
                playerRoot.BuildPlan = _buildPlanList.ToArray();
                playerRoot.ShipTemplate = _shipTemplateList.ToArray();
                playerRoot.Ship = _shipList.ToArray();
                playerRoot.Station = _stationList.ToArray();
                playerRoot.MarketListing = _marketListingList.ToArray();
                playerRoot.MarketTransaction = _marketTransactionList.ToArray();
                playerRoot.StockPlan = _stockPlanList.ToArray();
                playerRoot.StockProfile = _stockProfileList.ToArray();
                playerRoot.SupplyChain = _supplyChainList.ToArray();
                playerRoot.WarehouseOverflowRule = _warehouseOverflowRuleList.ToArray();
                playerRoot.Faction = _factionList.ToArray();
                playerRoot.ExternalCharacter = _externalCharacterList.ToArray();
                playerRoot.Asteroid = _asteroidList.ToArray();
            }

            playerRoot = SerializationSorter.SortPlayerRoot(playerRoot);

            // Integrity check: validate all blueprints before saving
            foreach (var bp in playerRoot.Blueprint ?? Array.Empty<Blueprint>())
            {
                string error = bp.ValidateIntegrity();
                if (error != null)
                {
                    Log.Error("BLUEPRINT INTEGRITY VIOLATION in WriteContext (player): {0}", error);
                    if (!SuppressUI)
                    {
                        System.Windows.Forms.MessageBox.Show(
                            "Blueprint corruption detected before save:\n\n" + error +
                            "\n\nThe save will proceed but this data may be corrupted. Please report this.",
                            "Blueprint Integrity Violation",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
            }

            string jsonContent = JsonConvert.SerializeObject(playerRoot, JsonSettings.SerializerSettings);

            SafeFileWriter.WriteAllText(FilePath, jsonContent);
            Log.Info("Player data saved to {0}", FilePath);
        }

        public void InitPlayerProfiles(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderPlayerProfiles(playerRoot.PlayerProfile);
            _playerProfileList = new List<PlayerProfile>(sorted);
            // Initialize the BindingSource component
            BindingSourcePlayerProfile = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourcePlayerProfile.DataSource = _playerProfileList;
        }

        public void InitBlueprints(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderBlueprints(playerRoot.Blueprint);
            _blueprintList = new List<Blueprint>(sorted);
            // Initialize the BindingSource component
            BindingSourceBlueprint = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceBlueprint.DataSource = _blueprintList;
            InvalidateBlueprintCache();
        }

        public ReadOnlyBlueprint FindBlueprint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_listLock)
            {
                if (_blueprintCache == null)
                {
                    _blueprintCache = new Dictionary<string, Blueprint>();
                    foreach (var bp in _blueprintList)
                    {
                        if (bp.UUID != null && !_blueprintCache.ContainsKey(bp.UUID))
                            _blueprintCache[bp.UUID] = bp;
                    }
                }

                if (_blueprintCache.TryGetValue(id, out var match))
                    return new ReadOnlyBlueprint(match);
            }

            // Fall back to global blueprints outside the lock
            return EmpireContext.GetInstance()?.FindGlobalBlueprint(id);
        }

        public void InvalidateBlueprintCache()
        {
            lock (_listLock)
            {
                _blueprintCache = null;
            }

            InvalidateAllBlueprintsCache();
        }

        public void AddBlueprint(Blueprint item)
        {
            lock (_listLock)
            {
                _blueprintList.Add(item);
                if (_blueprintCache != null && item.UUID != null)
                    _blueprintCache[item.UUID] = item;
                _allBlueprintsCache = null;
                _blueprintTypeCountCache = null;
            }

            BindingSourceBlueprint?.ResetBindings(false);
        }

        public void RemoveBlueprint(Blueprint item)
        {
            lock (_listLock)
            {
                _blueprintList.Remove(item);
                if (_blueprintCache != null && item.UUID != null)
                    _blueprintCache.Remove(item.UUID);
                _allBlueprintsCache = null;
                _blueprintTypeCountCache = null;
            }

            BindingSourceBlueprint?.ResetBindings(false);
        }

        public void InitSurveys(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderSurveys(playerRoot.Survey);
            _surveyList = new List<Survey>(sorted);
            // Initialize the BindingSource component
            BindingSourceSurvey = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceSurvey.DataSource = _surveyList;
            InvalidateSurveyCache();
        }

        public Survey FindSurvey(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_listLock)
            {
                if (_surveyCache == null)
                {
                    _surveyCache = new Dictionary<string, Survey>();
                    foreach (var s in _surveyList)
                    {
                        if (s.UUID != null && !_surveyCache.ContainsKey(s.UUID))
                            _surveyCache[s.UUID] = s;
                    }
                }

                _surveyCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateSurveyCache()
        {
            lock (_listLock)
            {
                _surveyCache = null;
            }
        }

        public void AddSurvey(Survey item)
        {
            lock (_listLock)
            {
                _surveyList.Add(item);
                if (_surveyCache != null && item.UUID != null)
                    _surveyCache[item.UUID] = item;
            }

            BindingSourceSurvey?.ResetBindings(false);
        }

        public void RemoveSurvey(Survey item)
        {
            lock (_listLock)
            {
                _surveyList.Remove(item);
                if (_surveyCache != null && item.UUID != null)
                    _surveyCache.Remove(item.UUID);
            }

            BindingSourceSurvey?.ResetBindings(false);
        }

        public void InitColonies(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderColonies(playerRoot.Colony);
            var list = new List<Colony>(sorted);

            // Stamp BuildQueueSequence for existing data where values are all zero (migration).
            // Don't sort the list â€” consumers sort by BuildQueueSequence themselves.
            foreach (var colony in list)
            {
                if (colony.Structures != null && colony.Structures.Count > 0
                    && colony.Structures[0].BuildQueueSequence == 0)
                {
                    colony.StampBuildQueueSequence();
                }
            }

            _colonyList = new List<Colony>(list);
            // Initialize the BindingSource component
            BindingSourceColony = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceColony.DataSource = _colonyList;
            InvalidateColonyCache();
        }

        public void InitDeliveryRoutes(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderDeliveryRoutes(playerRoot.DeliveryRoute ?? new DeliveryRoute[0]);
            _deliveryRouteList = new List<DeliveryRoute>(sorted);
        }

        public void InitDeliveryPlans(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderDeliveryPlans(playerRoot.DeliveryPlan ?? new DeliveryPlan[0]);
            _deliveryPlanList = new List<DeliveryPlan>(sorted);
        }

        public void InitPricingPlans(PlayerRoot playerRoot)
        {
            var sorted = CollectionSortHelper.OrderPricingPlans(playerRoot.PricingPlan ?? new PricingPlan[0]);
            _pricingPlanList = new List<PricingPlan>(sorted);
        }

        public void InitBuildPlans(PlayerRoot playerRoot)
        {
            _buildPlanList = new List<BuildPlan>(playerRoot.BuildPlan ?? new BuildPlan[0]);
        }

        public void InitShipTemplates(PlayerRoot playerRoot)
        {
            _shipTemplateList = new List<ShipTemplate>(playerRoot.ShipTemplate ?? new ShipTemplate[0]);
        }

        public void InitShips(PlayerRoot playerRoot)
        {
            _shipList = new List<Ship>(playerRoot.Ship ?? new Ship[0]);
        }

        public void InitStations(PlayerRoot playerRoot)
        {
            _stationList = new List<Station>(playerRoot.Station ?? new Station[0]);
        }

        public void InitMarketListings(PlayerRoot playerRoot)
        {
            _marketListingList = new List<MarketListing>(playerRoot.MarketListing ?? new MarketListing[0]);
        }

        public void InitMarketTransactions(PlayerRoot playerRoot)
        {
            _marketTransactionList = new List<MarketTransaction>(playerRoot.MarketTransaction ?? new MarketTransaction[0]);
        }

        public void InitStockPlans(PlayerRoot playerRoot)
        {
            _stockPlanList = new List<StockPlan>(playerRoot.StockPlan ?? new StockPlan[0]);
        }

        public void InitStockProfiles(PlayerRoot playerRoot)
        {
            _stockProfileList = new List<StockProfile>(playerRoot.StockProfile ?? new StockProfile[0]);
        }

        public void InitSupplyChains(PlayerRoot playerRoot)
        {
            _supplyChainList = new List<SupplyChain>(playerRoot.SupplyChain ?? new SupplyChain[0]);
        }

        public void InitWarehouseOverflowRules(PlayerRoot playerRoot)
        {
            _warehouseOverflowRuleList = new List<WarehouseOverflowRule>(playerRoot.WarehouseOverflowRule ?? new WarehouseOverflowRule[0]);
        }

        public void InitFactions(PlayerRoot playerRoot)
        {
            _factionList = new List<Faction>(playerRoot.Faction ?? new Faction[0]);
        }

        public void InitExternalCharacters(PlayerRoot playerRoot)
        {
            _externalCharacterList = new List<ExternalCharacter>(playerRoot.ExternalCharacter ?? new ExternalCharacter[0]);
        }

        public void InitAsteroids(PlayerRoot playerRoot)
        {
            _asteroidList = new List<Asteroid>(playerRoot.Asteroid ?? new Asteroid[0]);
        }

        public Colony FindColony(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_listLock)
            {
                if (_colonyCache == null)
                {
                    _colonyCache = new Dictionary<string, Colony>();
                    foreach (var c in _colonyList)
                    {
                        if (c.UUID != null && !_colonyCache.ContainsKey(c.UUID))
                            _colonyCache[c.UUID] = c;
                    }
                }

                _colonyCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateColonyCache()
        {
            lock (_listLock)
            {
                _colonyCache = null;
            }
        }

        public void AddColony(Colony item)
        {
            lock (_listLock)
            {
                _colonyList.Add(item);
                if (_colonyCache != null && item.UUID != null)
                    _colonyCache[item.UUID] = item;
            }

            BindingSourceColony?.ResetBindings(false);
        }

        public void RemoveColony(Colony item)
        {
            lock (_listLock)
            {
                _colonyList.Remove(item);
                if (_colonyCache != null && item.UUID != null)
                    _colonyCache.Remove(item.UUID);
            }

            BindingSourceColony?.ResetBindings(false);
        }

        // --- Task 4.1: PlayerProfile mutation methods (Pattern E: UUID cache + BindingSource) ---

        public void AddPlayerProfile(PlayerProfile item)
        {
            lock (_listLock)
            {
                _playerProfileList.Add(item);
                if (_playerProfileCache != null && item.UUID != null)
                    _playerProfileCache[item.UUID] = item;
            }

            BindingSourcePlayerProfile?.ResetBindings(false);
        }

        public void RemovePlayerProfile(PlayerProfile item)
        {
            lock (_listLock)
            {
                _playerProfileList.Remove(item);
                if (_playerProfileCache != null && item.UUID != null)
                    _playerProfileCache.Remove(item.UUID);
            }

            BindingSourcePlayerProfile?.ResetBindings(false);
        }

        // --- Task 4.2: BuildPlan mutation methods (Pattern D: UUID cache + derived caches) ---

        public void AddBuildPlan(BuildPlan item)
        {
            lock (_listLock)
            {
                _buildPlanList.Add(item);
                if (_buildPlanCache != null && item.UUID != null)
                    _buildPlanCache[item.UUID] = item;
                _blueprintBuildItemIndex = null;
                _buildLocationBuildItemIndex = null;
            }
        }

        public void RemoveBuildPlan(BuildPlan item)
        {
            lock (_listLock)
            {
                _buildPlanList.Remove(item);
                if (_buildPlanCache != null && item.UUID != null)
                    _buildPlanCache.Remove(item.UUID);
                _blueprintBuildItemIndex = null;
                _buildLocationBuildItemIndex = null;
            }
        }

        // --- Task 4.3: Pattern C mutation methods (UUID cache only) ---

        public void AddStation(Station item)
        {
            lock (_listLock)
            {
                _stationList.Add(item);
                if (_stationCache != null && item.UUID != null)
                    _stationCache[item.UUID] = item;
            }
        }

        public void RemoveStation(Station item)
        {
            lock (_listLock)
            {
                _stationList.Remove(item);
                if (_stationCache != null && item.UUID != null)
                    _stationCache.Remove(item.UUID);
            }
        }

        public void AddShipTemplate(ShipTemplate item)
        {
            lock (_listLock)
            {
                _shipTemplateList.Add(item);
                if (_shipTemplateCache != null && item.UUID != null)
                    _shipTemplateCache[item.UUID] = item;
            }
        }

        public void RemoveShipTemplate(ShipTemplate item)
        {
            lock (_listLock)
            {
                _shipTemplateList.Remove(item);
                if (_shipTemplateCache != null && item.UUID != null)
                    _shipTemplateCache.Remove(item.UUID);
            }
        }

        public void AddShip(Ship item)
        {
            lock (_listLock)
            {
                _shipList.Add(item);
                if (_shipCache != null && item.UUID != null)
                    _shipCache[item.UUID] = item;
            }
        }

        public void RemoveShip(Ship item)
        {
            lock (_listLock)
            {
                _shipList.Remove(item);
                if (_shipCache != null && item.UUID != null)
                    _shipCache.Remove(item.UUID);
            }
        }

        public void AddAsteroid(Asteroid item)
        {
            lock (_listLock)
            {
                _asteroidList.Add(item);
                if (_asteroidCache != null && item.UUID != null)
                    _asteroidCache[item.UUID] = item;
            }
        }

        public void RemoveAsteroid(Asteroid item)
        {
            lock (_listLock)
            {
                _asteroidList.Remove(item);
                if (_asteroidCache != null && item.UUID != null)
                    _asteroidCache.Remove(item.UUID);
            }
        }

        public void AddFaction(Faction item)
        {
            lock (_listLock)
            {
                _factionList.Add(item);
                if (_factionCache != null && item.UUID != null)
                    _factionCache[item.UUID] = item;
            }
        }

        public void RemoveFaction(Faction item)
        {
            lock (_listLock)
            {
                _factionList.Remove(item);
                if (_factionCache != null && item.UUID != null)
                    _factionCache.Remove(item.UUID);
            }
        }

        public void AddMarketListing(MarketListing item)
        {
            lock (_listLock)
            {
                _marketListingList.Add(item);
                if (_marketListingCache != null && item.UUID != null)
                    _marketListingCache[item.UUID] = item;
            }
        }

        public void RemoveMarketListing(MarketListing item)
        {
            lock (_listLock)
            {
                _marketListingList.Remove(item);
                if (_marketListingCache != null && item.UUID != null)
                    _marketListingCache.Remove(item.UUID);
            }
        }

        // --- Task 4.4: Pattern C mutation methods for new-cache entities ---

        public void AddDeliveryRoute(DeliveryRoute item)
        {
            lock (_listLock)
            {
                _deliveryRouteList.Add(item);
                if (_deliveryRouteCache != null && item.UUID != null)
                    _deliveryRouteCache[item.UUID] = item;
            }
        }

        public void RemoveDeliveryRoute(DeliveryRoute item)
        {
            lock (_listLock)
            {
                _deliveryRouteList.Remove(item);
                if (_deliveryRouteCache != null && item.UUID != null)
                    _deliveryRouteCache.Remove(item.UUID);
            }
        }

        public void AddDeliveryPlan(DeliveryPlan item)
        {
            lock (_listLock)
            {
                _deliveryPlanList.Add(item);
                if (_deliveryPlanCache != null && item.UUID != null)
                    _deliveryPlanCache[item.UUID] = item;
            }
        }

        public void RemoveDeliveryPlan(DeliveryPlan item)
        {
            lock (_listLock)
            {
                _deliveryPlanList.Remove(item);
                if (_deliveryPlanCache != null && item.UUID != null)
                    _deliveryPlanCache.Remove(item.UUID);
            }
        }

        public void AddPricingPlan(PricingPlan item)
        {
            lock (_listLock)
            {
                _pricingPlanList.Add(item);
                if (_pricingPlanCache != null && item.UUID != null)
                    _pricingPlanCache[item.UUID] = item;
            }
        }

        public void RemovePricingPlan(PricingPlan item)
        {
            lock (_listLock)
            {
                _pricingPlanList.Remove(item);
                if (_pricingPlanCache != null && item.UUID != null)
                    _pricingPlanCache.Remove(item.UUID);
            }
        }

        public void AddMarketTransaction(MarketTransaction item)
        {
            lock (_listLock)
            {
                _marketTransactionList.Add(item);
                if (_marketTransactionCache != null && item.UUID != null)
                    _marketTransactionCache[item.UUID] = item;
            }
        }

        public void RemoveMarketTransaction(MarketTransaction item)
        {
            lock (_listLock)
            {
                _marketTransactionList.Remove(item);
                if (_marketTransactionCache != null && item.UUID != null)
                    _marketTransactionCache.Remove(item.UUID);
            }
        }

        public void AddStockPlan(StockPlan item)
        {
            lock (_listLock)
            {
                _stockPlanList.Add(item);
                if (_stockPlanCache != null && item.UUID != null)
                    _stockPlanCache[item.UUID] = item;
            }
        }

        public void RemoveStockPlan(StockPlan item)
        {
            lock (_listLock)
            {
                _stockPlanList.Remove(item);
                if (_stockPlanCache != null && item.UUID != null)
                    _stockPlanCache.Remove(item.UUID);
            }
        }

        public void AddStockProfile(StockProfile item)
        {
            lock (_listLock)
            {
                _stockProfileList.Add(item);
                if (_stockProfileCache != null && item.UUID != null)
                    _stockProfileCache[item.UUID] = item;
            }
        }

        public void RemoveStockProfile(StockProfile item)
        {
            lock (_listLock)
            {
                _stockProfileList.Remove(item);
                if (_stockProfileCache != null && item.UUID != null)
                    _stockProfileCache.Remove(item.UUID);
            }
        }

        public void AddSupplyChain(SupplyChain item)
        {
            lock (_listLock)
            {
                _supplyChainList.Add(item);
                if (_supplyChainCache != null && item.UUID != null)
                    _supplyChainCache[item.UUID] = item;
            }
        }

        public void RemoveSupplyChain(SupplyChain item)
        {
            lock (_listLock)
            {
                _supplyChainList.Remove(item);
                if (_supplyChainCache != null && item.UUID != null)
                    _supplyChainCache.Remove(item.UUID);
            }
        }

        public void AddWarehouseOverflowRule(WarehouseOverflowRule item)
        {
            lock (_listLock)
            {
                _warehouseOverflowRuleList.Add(item);
                if (_warehouseOverflowRuleCache != null && item.UUID != null)
                    _warehouseOverflowRuleCache[item.UUID] = item;
            }
        }

        public void RemoveWarehouseOverflowRule(WarehouseOverflowRule item)
        {
            lock (_listLock)
            {
                _warehouseOverflowRuleList.Remove(item);
                if (_warehouseOverflowRuleCache != null && item.UUID != null)
                    _warehouseOverflowRuleCache.Remove(item.UUID);
            }
        }

        public void AddExternalCharacter(ExternalCharacter item)
        {
            lock (_listLock)
            {
                _externalCharacterList.Add(item);
                if (_externalCharacterCache != null && item.UUID != null)
                    _externalCharacterCache[item.UUID] = item;
            }
        }

        public void RemoveExternalCharacter(ExternalCharacter item)
        {
            lock (_listLock)
            {
                _externalCharacterList.Remove(item);
                if (_externalCharacterCache != null && item.UUID != null)
                    _externalCharacterCache.Remove(item.UUID);
            }
        }

        /// <summary>
        /// Finds a Station by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public Station FindStation(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_stationCache == null)
                {
                    _stationCache = new Dictionary<string, Station>();
                    foreach (var s in _stationList)
                    {
                        if (s.UUID != null && !_stationCache.ContainsKey(s.UUID))
                        {
                            _stationCache[s.UUID] = s;
                        }
                    }
                }

                _stationCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateStationCache()
        {
            lock (_listLock)
            {
                _stationCache = null;
            }
        }

        /// <summary>
        /// Finds a ShipTemplate by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public ShipTemplate FindShipTemplate(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_shipTemplateCache == null)
                {
                    _shipTemplateCache = new Dictionary<string, ShipTemplate>();
                    foreach (var st in _shipTemplateList)
                    {
                        if (st.UUID != null && !_shipTemplateCache.ContainsKey(st.UUID))
                        {
                            _shipTemplateCache[st.UUID] = st;
                        }
                    }
                }

                _shipTemplateCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateShipTemplateCache()
        {
            lock (_listLock)
            {
                _shipTemplateCache = null;
            }
        }

        /// <summary>
        /// Finds a Ship by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public Ship FindShip(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_shipCache == null)
                {
                    _shipCache = new Dictionary<string, Ship>();
                    foreach (var s in _shipList)
                    {
                        if (s.UUID != null && !_shipCache.ContainsKey(s.UUID))
                        {
                            _shipCache[s.UUID] = s;
                        }
                    }
                }

                _shipCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateShipCache()
        {
            lock (_listLock)
            {
                _shipCache = null;
            }
        }

        /// <summary>
        /// Finds a BuildPlan by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public BuildPlan FindBuildPlan(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            {
            lock (_listLock)
            {
                if (_buildPlanCache == null)
                {
                    _buildPlanCache = new Dictionary<string, BuildPlan>();
                    foreach (var bp in _buildPlanList)
                    {
                        if (bp.UUID != null && !_buildPlanCache.ContainsKey(bp.UUID))
                        {
                            _buildPlanCache[bp.UUID] = bp;
                        }
                    }
                }

                _buildPlanCache.TryGetValue(id, out var match);
                return match;
            }
            }
        }

        public void InvalidateBuildPlanCache()
        {
            lock (_listLock)
            {
                _buildPlanCache = null;
            }
        }

        /// <summary>
        /// Finds an Asteroid by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public Asteroid FindAsteroid(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_asteroidCache == null)
                {
                    _asteroidCache = new Dictionary<string, Asteroid>();
                    foreach (var a in _asteroidList)
                    {
                        if (a.UUID != null && !_asteroidCache.ContainsKey(a.UUID))
                        {
                            _asteroidCache[a.UUID] = a;
                        }
                    }
                }

                _asteroidCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateAsteroidCache()
        {
            lock (_listLock)
            {
                _asteroidCache = null;
            }
        }

        /// <summary>
        /// Finds a Faction by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public Faction FindFaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_factionCache == null)
                {
                    _factionCache = new Dictionary<string, Faction>();
                    foreach (var f in _factionList)
                    {
                        if (f.UUID != null && !_factionCache.ContainsKey(f.UUID))
                        {
                            _factionCache[f.UUID] = f;
                        }
                    }
                }

                _factionCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateFactionCache()
        {
            lock (_listLock)
            {
                _factionCache = null;
            }
        }

        /// <summary>
        /// Finds a MarketListing by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public MarketListing FindMarketListing(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_marketListingCache == null)
                {
                    _marketListingCache = new Dictionary<string, MarketListing>();
                    foreach (var ml in _marketListingList)
                    {
                        if (ml.UUID != null && !_marketListingCache.ContainsKey(ml.UUID))
                        {
                            _marketListingCache[ml.UUID] = ml;
                        }
                    }
                }

                _marketListingCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateMarketListingCache()
        {
            lock (_listLock)
            {
                _marketListingCache = null;
            }
        }

        public PlayerProfile FindPlayerProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_playerProfileCache == null)
                {
                    _playerProfileCache = new Dictionary<string, PlayerProfile>();
                    foreach (var r in _playerProfileList)
                    {
                        if (r.UUID != null && !_playerProfileCache.ContainsKey(r.UUID))
                        {
                            _playerProfileCache[r.UUID] = r;
                        }
                    }
                }

                _playerProfileCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidatePlayerProfileCache()
        {
            lock (_listLock)
            {
                _playerProfileCache = null;
            }
        }

        public DeliveryRoute FindDeliveryRoute(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_deliveryRouteCache == null)
                {
                    _deliveryRouteCache = new Dictionary<string, DeliveryRoute>();
                    foreach (var r in _deliveryRouteList)
                    {
                        if (r.UUID != null && !_deliveryRouteCache.ContainsKey(r.UUID))
                        {
                            _deliveryRouteCache[r.UUID] = r;
                        }
                    }
                }

                _deliveryRouteCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateDeliveryRouteCache()
        {
            lock (_listLock)
            {
                _deliveryRouteCache = null;
            }
        }

        public DeliveryPlan FindDeliveryPlan(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_deliveryPlanCache == null)
                {
                    _deliveryPlanCache = new Dictionary<string, DeliveryPlan>();
                    foreach (var r in _deliveryPlanList)
                    {
                        if (r.UUID != null && !_deliveryPlanCache.ContainsKey(r.UUID))
                        {
                            _deliveryPlanCache[r.UUID] = r;
                        }
                    }
                }

                _deliveryPlanCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateDeliveryPlanCache()
        {
            lock (_listLock)
            {
                _deliveryPlanCache = null;
            }
        }

        public PricingPlan FindPricingPlan(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_pricingPlanCache == null)
                {
                    _pricingPlanCache = new Dictionary<string, PricingPlan>();
                    foreach (var r in _pricingPlanList)
                    {
                        if (r.UUID != null && !_pricingPlanCache.ContainsKey(r.UUID))
                        {
                            _pricingPlanCache[r.UUID] = r;
                        }
                    }
                }

                _pricingPlanCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidatePricingPlanCache()
        {
            lock (_listLock)
            {
                _pricingPlanCache = null;
            }
        }

        public MarketTransaction FindMarketTransaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_marketTransactionCache == null)
                {
                    _marketTransactionCache = new Dictionary<string, MarketTransaction>();
                    foreach (var r in _marketTransactionList)
                    {
                        if (r.UUID != null && !_marketTransactionCache.ContainsKey(r.UUID))
                        {
                            _marketTransactionCache[r.UUID] = r;
                        }
                    }
                }

                _marketTransactionCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateMarketTransactionCache()
        {
            lock (_listLock)
            {
                _marketTransactionCache = null;
            }
        }

        public StockPlan FindStockPlan(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_stockPlanCache == null)
                {
                    _stockPlanCache = new Dictionary<string, StockPlan>();
                    foreach (var r in _stockPlanList)
                    {
                        if (r.UUID != null && !_stockPlanCache.ContainsKey(r.UUID))
                        {
                            _stockPlanCache[r.UUID] = r;
                        }
                    }
                }

                _stockPlanCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateStockPlanCache()
        {
            lock (_listLock)
            {
                _stockPlanCache = null;
            }
        }

        public StockProfile FindStockProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_stockProfileCache == null)
                {
                    _stockProfileCache = new Dictionary<string, StockProfile>();
                    foreach (var r in _stockProfileList)
                    {
                        if (r.UUID != null && !_stockProfileCache.ContainsKey(r.UUID))
                        {
                            _stockProfileCache[r.UUID] = r;
                        }
                    }
                }

                _stockProfileCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateStockProfileCache()
        {
            lock (_listLock)
            {
                _stockProfileCache = null;
            }
        }

        public SupplyChain FindSupplyChain(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_supplyChainCache == null)
                {
                    _supplyChainCache = new Dictionary<string, SupplyChain>();
                    foreach (var r in _supplyChainList)
                    {
                        if (r.UUID != null && !_supplyChainCache.ContainsKey(r.UUID))
                        {
                            _supplyChainCache[r.UUID] = r;
                        }
                    }
                }

                _supplyChainCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateSupplyChainCache()
        {
            lock (_listLock)
            {
                _supplyChainCache = null;
            }
        }

        public WarehouseOverflowRule FindWarehouseOverflowRule(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_warehouseOverflowRuleCache == null)
                {
                    _warehouseOverflowRuleCache = new Dictionary<string, WarehouseOverflowRule>();
                    foreach (var r in _warehouseOverflowRuleList)
                    {
                        if (r.UUID != null && !_warehouseOverflowRuleCache.ContainsKey(r.UUID))
                        {
                            _warehouseOverflowRuleCache[r.UUID] = r;
                        }
                    }
                }

                _warehouseOverflowRuleCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateWarehouseOverflowRuleCache()
        {
            lock (_listLock)
            {
                _warehouseOverflowRuleCache = null;
            }
        }

        public ExternalCharacter FindExternalCharacter(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_externalCharacterCache == null)
                {
                    _externalCharacterCache = new Dictionary<string, ExternalCharacter>();
                    foreach (var r in _externalCharacterList)
                    {
                        if (r.UUID != null && !_externalCharacterCache.ContainsKey(r.UUID))
                        {
                            _externalCharacterCache[r.UUID] = r;
                        }
                    }
                }

                _externalCharacterCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateExternalCharacterCache()
        {
            lock (_listLock)
            {
                _externalCharacterCache = null;
            }
        }

        /// <summary>
        /// Returns the count of blueprints with the given BluePrintType for the current player.
        /// Used by auto-assign copy constraint.
        /// </summary>
        public int CountBlueprintsByType(string blueprintType)
        {
            lock (_listLock)
            {
                if (_blueprintTypeCountCache == null)
                {
                    _blueprintTypeCountCache = new Dictionary<string, int>();
                    foreach (var bp in GetCurrentPlayerBlueprints())
                    {
                        var key = bp.BluePrintType ?? string.Empty;
                        if (_blueprintTypeCountCache.ContainsKey(key))
                            _blueprintTypeCountCache[key]++;
                        else
                            _blueprintTypeCountCache[key] = 1;
                    }
                }

                return _blueprintTypeCountCache.TryGetValue(blueprintType ?? string.Empty, out var count) ? count : 0;
            }
        }

        public void InvalidateBlueprintTypeCountCache()
        {
            lock (_listLock)
            {
                _blueprintTypeCountCache = null;
            }
        }

        /// <summary>
        /// Returns BuildItems across all plans that reference the given BlueprintUUID.
        /// </summary>
        public List<BuildItem> GetBuildItemsByBlueprint(string blueprintUUID)
        {
            if (string.IsNullOrEmpty(blueprintUUID)) return new List<BuildItem>();
            lock (_listLock)
            {
                if (_blueprintBuildItemIndex == null)
                    RebuildBuildItemIndexes();
                _blueprintBuildItemIndex.TryGetValue(blueprintUUID, out var items);
                return items != null ? new List<BuildItem>(items) : new List<BuildItem>();
            }
        }

        /// <summary>
        /// Returns BuildItems across all plans that reference the given BuildLocationUUID.
        /// </summary>
        public List<BuildItem> GetBuildItemsByLocation(string locationUUID)
        {
            if (string.IsNullOrEmpty(locationUUID)) return new List<BuildItem>();
            lock (_listLock)
            {
                if (_buildLocationBuildItemIndex == null)
                    RebuildBuildItemIndexes();
                _buildLocationBuildItemIndex.TryGetValue(locationUUID, out var items);
                return items != null ? new List<BuildItem>(items) : new List<BuildItem>();
            }
        }

        public void InvalidateBuildItemIndexes()
        {
            lock (_listLock)
            {
                _blueprintBuildItemIndex = null;
                _buildLocationBuildItemIndex = null;
            }
        }

        /// <summary>
        /// Returns a snapshot of ColonyList for safe iteration outside the lock.
        /// </summary>
        public List<Colony> SnapshotColonyList()
        {
            lock (_listLock)
            {
                return new List<Colony>(_colonyList);
            }
        }

        /// <summary>
        /// Returns a snapshot of BuildPlanList for safe iteration outside the lock.
        /// </summary>
        public List<BuildPlan> SnapshotBuildPlanList()
        {
            lock (_listLock)
            {
                return new List<BuildPlan>(_buildPlanList);
            }
        }

        /// <summary>
        /// Returns a snapshot of ShipTemplateList for safe iteration outside the lock.
        /// </summary>
        public List<ShipTemplate> SnapshotShipTemplateList()
        {
            lock (_listLock)
            {
                return new List<ShipTemplate>(_shipTemplateList);
            }
        }

        /// <summary>
        /// Returns a snapshot of ShipList for safe iteration outside the lock.
        /// </summary>
        public List<Ship> SnapshotShipList()
        {
            lock (_listLock)
            {
                return new List<Ship>(_shipList);
            }
        }

        /// <summary>
        /// Returns a snapshot of StationList for safe iteration outside the lock.
        /// </summary>
        public List<Station> SnapshotStationList()
        {
            lock (_listLock)
            {
                return new List<Station>(_stationList);
            }
        }

        /// <summary>
        /// Returns a snapshot of MarketListingList for safe iteration outside the lock.
        /// </summary>
        public List<MarketListing> SnapshotMarketListingList()
        {
            lock (_listLock)
            {
                return new List<MarketListing>(_marketListingList);
            }
        }

        /// <summary>
        /// Returns a snapshot of MarketTransactionList for safe iteration outside the lock.
        /// </summary>
        public List<MarketTransaction> SnapshotMarketTransactionList()
        {
            lock (_listLock)
            {
                return new List<MarketTransaction>(_marketTransactionList);
            }
        }

        /// <summary>
        /// Returns a snapshot of StockPlanList for safe iteration outside the lock.
        /// </summary>
        public List<StockPlan> SnapshotStockPlanList()
        {
            lock (_listLock)
            {
                return new List<StockPlan>(_stockPlanList);
            }
        }

        /// <summary>
        /// Returns a snapshot of StockProfileList for safe iteration outside the lock.
        /// </summary>
        public List<StockProfile> SnapshotStockProfileList()
        {
            lock (_listLock)
            {
                return new List<StockProfile>(_stockProfileList);
            }
        }

        /// <summary>
        /// Returns a snapshot of SupplyChainList for safe iteration outside the lock.
        /// </summary>
        public List<SupplyChain> SnapshotSupplyChainList()
        {
            lock (_listLock)
            {
                return new List<SupplyChain>(_supplyChainList);
            }
        }

        /// <summary>
        /// Returns a snapshot of WarehouseOverflowRuleList for safe iteration outside the lock.
        /// </summary>
        public List<WarehouseOverflowRule> SnapshotWarehouseOverflowRuleList()
        {
            lock (_listLock)
            {
                return new List<WarehouseOverflowRule>(_warehouseOverflowRuleList);
            }
        }

        /// <summary>
        /// Returns a snapshot of FactionList for safe iteration outside the lock.
        /// </summary>
        public List<Faction> SnapshotFactionList()
        {
            lock (_listLock)
            {
                return new List<Faction>(_factionList);
            }
        }

        /// <summary>
        /// Returns a snapshot of ExternalCharacterList for safe iteration outside the lock.
        /// </summary>
        public List<ExternalCharacter> SnapshotExternalCharacterList()
        {
            lock (_listLock)
            {
                return new List<ExternalCharacter>(_externalCharacterList);
            }
        }

        /// <summary>
        /// Returns a snapshot of AsteroidList for safe iteration outside the lock.
        /// </summary>
        public List<Asteroid> SnapshotAsteroidList()
        {
            lock (_listLock)
            {
                return new List<Asteroid>(_asteroidList);
            }
        }

        /// <summary>
        /// Removes all colonies, blueprints, surveys, and routes owned by the given player UUID.
        /// Called during cascade delete.
        /// </summary>
        public void CascadeDeletePlayer(string playerUUID)
        {
            if (string.IsNullOrEmpty(playerUUID)) return;

            int removed = 0;
            foreach (var colony in _colonyList.Where(c => c.OwnerUUID == playerUUID).ToList())
            {
                _colonyList.Remove(colony);
                removed++;
            }

            foreach (var bp in _blueprintList.Where(b => b.OwnerUUID == playerUUID).ToList())
            {
                _blueprintList.Remove(bp);
                removed++;
            }

            foreach (var survey in _surveyList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _surveyList.Remove(survey);
                removed++;
            }

            foreach (var route in _deliveryRouteList.Where(r => r.OwnerUUID == playerUUID).ToList())
            {
                _deliveryRouteList.Remove(route);
                removed++;
            }

            foreach (var plan in _deliveryPlanList.Where(p => p.OwnerUUID == playerUUID).ToList())
            {
                _deliveryPlanList.Remove(plan);
                removed++;
            }

            foreach (var pp in _pricingPlanList.Where(p => p.OwnerUUID == playerUUID).ToList())
            {
                _pricingPlanList.Remove(pp);
                removed++;
            }

            foreach (var bp2 in _buildPlanList.Where(b => b.OwnerUUID == playerUUID).ToList())
            {
                _buildPlanList.Remove(bp2);
                removed++;
            }

            foreach (var st in _shipTemplateList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _shipTemplateList.Remove(st);
                removed++;
            }

            foreach (var ship in _shipList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _shipList.Remove(ship);
                removed++;
            }

            foreach (var station in _stationList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _stationList.Remove(station);
                removed++;
            }

            foreach (var ml in _marketListingList.Where(m => m.OwnerUUID == playerUUID).ToList())
            {
                _marketListingList.Remove(ml);
                removed++;
            }

            foreach (var mt in _marketTransactionList.Where(m => m.OwnerUUID == playerUUID).ToList())
            {
                _marketTransactionList.Remove(mt);
                removed++;
            }

            foreach (var sp in _stockPlanList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _stockPlanList.Remove(sp);
                removed++;
            }

            foreach (var spf in _stockProfileList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _stockProfileList.Remove(spf);
                removed++;
            }

            foreach (var sc in _supplyChainList.Where(s => s.OwnerUUID == playerUUID).ToList())
            {
                _supplyChainList.Remove(sc);
                removed++;
            }

            foreach (var wor in _warehouseOverflowRuleList.Where(w => w.OwnerUUID == playerUUID).ToList())
            {
                _warehouseOverflowRuleList.Remove(wor);
                removed++;
            }

            if (removed > 0)
                Log.Info("Cascade deleted {0} items for player {1}", removed, playerUUID);

            InvalidateBlueprintCache();
            InvalidateSurveyCache();
            InvalidateColonyCache();
        }

        /// <summary>
        /// Returns colonies owned by the current player.
        /// </summary>
        public List<Colony> GetCurrentPlayerColonies()
        {
            return _colonyList.Where(c => c.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns blueprints owned by the current player.
        /// </summary>
        public List<Blueprint> GetCurrentPlayerBlueprints()
        {
            return _blueprintList.Where(b => b.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns all blueprints: current player's + global.
        /// Use this instead of accessing BlueprintList directly.
        /// Cached; invalidated when either blueprint list changes.
        /// </summary>
        public List<Blueprint> GetAllBlueprints()
        {
            if (_allBlueprintsCache == null)
            {
                _allBlueprintsCache = new List<Blueprint>(_blueprintList);
                var ec = EmpireContext.GetInstance();
                if (ec?.GlobalBlueprintList != null)
                    _allBlueprintsCache.AddRange(ec.GlobalBlueprintList);
            }

            return _allBlueprintsCache;
        }

        /// <summary>
        /// Clears the cached merged blueprint list so the next GetAllBlueprints() call rebuilds it.
        /// </summary>
        public void InvalidateAllBlueprintsCache()
        {
            _allBlueprintsCache = null;
        }

        /// <summary>
        /// Returns surveys owned by the current player.
        /// </summary>
        public List<Survey> GetCurrentPlayerSurveys()
        {
            return _surveyList.Where(s => s.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns delivery routes owned by the current player.
        /// </summary>
        public List<DeliveryRoute> GetCurrentPlayerRoutes()
        {
            return _deliveryRouteList.Where(r => r.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns delivery plans owned by the current player.
        /// </summary>
        public List<DeliveryPlan> GetCurrentPlayerPlans()
        {
            return _deliveryPlanList.Where(p => p.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns pricing plans owned by the current player.
        /// </summary>
        public List<PricingPlan> GetCurrentPlayerPricingPlans()
        {
            return _pricingPlanList.Where(p => p.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns build plans owned by the current player.
        /// </summary>
        public List<BuildPlan> GetCurrentPlayerBuildPlans()
        {
            lock (_listLock)
            {
                return _buildPlanList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns ship templates owned by the current player.
        /// </summary>
        public List<ShipTemplate> GetCurrentPlayerShipTemplates()
        {
            lock (_listLock)
            {
                return _shipTemplateList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns ships owned by the current player.
        /// </summary>
        public List<Ship> GetCurrentPlayerShips()
        {
            lock (_listLock)
            {
                return _shipList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns stations relevant to the current player. Includes government stations
        /// (shared infrastructure) and player-owned stations.
        /// </summary>
        public List<Station> GetCurrentPlayerStations()
        {
            lock (_listLock)
            {
                return _stationList.Where(x =>
                    x.Ownership == StationOwnership.Government ||
                    x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns market listings owned by the current player.
        /// </summary>
        public List<MarketListing> GetCurrentPlayerListings()
        {
            lock (_listLock)
            {
                return _marketListingList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns market transactions owned by the current player.
        /// </summary>
        public List<MarketTransaction> GetCurrentPlayerTransactions()
        {
            lock (_listLock)
            {
                return _marketTransactionList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns stock plans owned by the current player.
        /// </summary>
        public List<StockPlan> GetCurrentPlayerStockPlans()
        {
            lock (_listLock)
            {
                return _stockPlanList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns stock profiles owned by the current player.
        /// </summary>
        public List<StockProfile> GetCurrentPlayerStockProfiles()
        {
            lock (_listLock)
            {
                return _stockProfileList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns supply chains owned by the current player.
        /// </summary>
        public List<SupplyChain> GetCurrentPlayerSupplyChains()
        {
            lock (_listLock)
            {
                return _supplyChainList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns warehouse overflow rules owned by the current player.
        /// </summary>
        public List<WarehouseOverflowRule> GetCurrentPlayerOverflowRules()
        {
            lock (_listLock)
            {
                return _warehouseOverflowRuleList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        // -- Task 5.1: Full-list GetReadOnly methods --

        public IReadOnlyList<ReadOnlyBlueprint> GetReadOnlyBlueprintList()
        {
            lock (_listLock)
            {
                return _blueprintList.Select(b => new ReadOnlyBlueprint(b)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyColony> GetReadOnlyColonyList()
        {
            lock (_listLock)
            {
                return _colonyList.Select(c => new ReadOnlyColony(c)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlySurvey> GetReadOnlySurveyList()
        {
            lock (_listLock)
            {
                return _surveyList.Select(s => new ReadOnlySurvey(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyPlayerProfile> GetReadOnlyPlayerProfileList()
        {
            lock (_listLock)
            {
                return _playerProfileList.Select(p => new ReadOnlyPlayerProfile(p)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyDeliveryRoute> GetReadOnlyDeliveryRouteList()
        {
            lock (_listLock)
            {
                return _deliveryRouteList.Select(r => new ReadOnlyDeliveryRoute(r)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyDeliveryPlan> GetReadOnlyDeliveryPlanList()
        {
            lock (_listLock)
            {
                return _deliveryPlanList.Select(p => new ReadOnlyDeliveryPlan(p)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyPricingPlan> GetReadOnlyPricingPlanList()
        {
            lock (_listLock)
            {
                return _pricingPlanList.Select(p => new ReadOnlyPricingPlan(p)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyBuildPlan> GetReadOnlyBuildPlanList()
        {
            lock (_listLock)
            {
                return _buildPlanList.Select(b => new ReadOnlyBuildPlan(b)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyShipTemplate> GetReadOnlyShipTemplateList()
        {
            lock (_listLock)
            {
                return _shipTemplateList.Select(s => new ReadOnlyShipTemplate(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyShip> GetReadOnlyShipList()
        {
            lock (_listLock)
            {
                return _shipList.Select(s => new ReadOnlyShip(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyStation> GetReadOnlyStationList()
        {
            lock (_listLock)
            {
                return _stationList.Select(s => new ReadOnlyStation(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyMarketListing> GetReadOnlyMarketListingList()
        {
            lock (_listLock)
            {
                return _marketListingList.Select(m => new ReadOnlyMarketListing(m)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyMarketTransaction> GetReadOnlyMarketTransactionList()
        {
            lock (_listLock)
            {
                return _marketTransactionList.Select(m => new ReadOnlyMarketTransaction(m)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyStockPlan> GetReadOnlyStockPlanList()
        {
            lock (_listLock)
            {
                return _stockPlanList.Select(s => new ReadOnlyStockPlan(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyStockProfile> GetReadOnlyStockProfileList()
        {
            lock (_listLock)
            {
                return _stockProfileList.Select(s => new ReadOnlyStockProfile(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlySupplyChain> GetReadOnlySupplyChainList()
        {
            lock (_listLock)
            {
                return _supplyChainList.Select(s => new ReadOnlySupplyChain(s)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyWarehouseOverflowRule> GetReadOnlyWarehouseOverflowRuleList()
        {
            lock (_listLock)
            {
                return _warehouseOverflowRuleList.Select(w => new ReadOnlyWarehouseOverflowRule(w)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyFaction> GetReadOnlyFactionList()
        {
            lock (_listLock)
            {
                return _factionList.Select(f => new ReadOnlyFaction(f)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyExternalCharacter> GetReadOnlyExternalCharacterList()
        {
            lock (_listLock)
            {
                return _externalCharacterList.Select(e => new ReadOnlyExternalCharacter(e)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyAsteroid> GetReadOnlyAsteroidList()
        {
            lock (_listLock)
            {
                return _asteroidList.Select(a => new ReadOnlyAsteroid(a)).ToList();
            }
        }

        // -- Task 5.2: FindReadOnly methods --

        public ReadOnlyBlueprint FindReadOnlyBlueprint(string id)
        {
            return FindBlueprint(id);
        }

        public ReadOnlyColony FindReadOnlyColony(string id)
        {
            var entity = FindColony(id);
            return entity != null ? new ReadOnlyColony(entity) : null;
        }

        public ReadOnlySurvey FindReadOnlySurvey(string id)
        {
            var entity = FindSurvey(id);
            return entity != null ? new ReadOnlySurvey(entity) : null;
        }

        public ReadOnlyPlayerProfile FindReadOnlyPlayerProfile(string id)
        {
            var entity = FindPlayerProfile(id);
            return entity != null ? new ReadOnlyPlayerProfile(entity) : null;
        }

        public ReadOnlyDeliveryRoute FindReadOnlyDeliveryRoute(string id)
        {
            var entity = FindDeliveryRoute(id);
            return entity != null ? new ReadOnlyDeliveryRoute(entity) : null;
        }

        public ReadOnlyDeliveryPlan FindReadOnlyDeliveryPlan(string id)
        {
            var entity = FindDeliveryPlan(id);
            return entity != null ? new ReadOnlyDeliveryPlan(entity) : null;
        }

        public ReadOnlyPricingPlan FindReadOnlyPricingPlan(string id)
        {
            var entity = FindPricingPlan(id);
            return entity != null ? new ReadOnlyPricingPlan(entity) : null;
        }

        public ReadOnlyBuildPlan FindReadOnlyBuildPlan(string id)
        {
            var entity = FindBuildPlan(id);
            return entity != null ? new ReadOnlyBuildPlan(entity) : null;
        }

        public ReadOnlyShipTemplate FindReadOnlyShipTemplate(string id)
        {
            var entity = FindShipTemplate(id);
            return entity != null ? new ReadOnlyShipTemplate(entity) : null;
        }

        public ReadOnlyShip FindReadOnlyShip(string id)
        {
            var entity = FindShip(id);
            return entity != null ? new ReadOnlyShip(entity) : null;
        }

        public ReadOnlyStation FindReadOnlyStation(string id)
        {
            var entity = FindStation(id);
            return entity != null ? new ReadOnlyStation(entity) : null;
        }

        public ReadOnlyMarketListing FindReadOnlyMarketListing(string id)
        {
            var entity = FindMarketListing(id);
            return entity != null ? new ReadOnlyMarketListing(entity) : null;
        }

        public ReadOnlyMarketTransaction FindReadOnlyMarketTransaction(string id)
        {
            var entity = FindMarketTransaction(id);
            return entity != null ? new ReadOnlyMarketTransaction(entity) : null;
        }

        public ReadOnlyStockPlan FindReadOnlyStockPlan(string id)
        {
            var entity = FindStockPlan(id);
            return entity != null ? new ReadOnlyStockPlan(entity) : null;
        }

        public ReadOnlyStockProfile FindReadOnlyStockProfile(string id)
        {
            var entity = FindStockProfile(id);
            return entity != null ? new ReadOnlyStockProfile(entity) : null;
        }

        public ReadOnlySupplyChain FindReadOnlySupplyChain(string id)
        {
            var entity = FindSupplyChain(id);
            return entity != null ? new ReadOnlySupplyChain(entity) : null;
        }

        public ReadOnlyWarehouseOverflowRule FindReadOnlyWarehouseOverflowRule(string id)
        {
            var entity = FindWarehouseOverflowRule(id);
            return entity != null ? new ReadOnlyWarehouseOverflowRule(entity) : null;
        }

        public ReadOnlyFaction FindReadOnlyFaction(string id)
        {
            var entity = FindFaction(id);
            return entity != null ? new ReadOnlyFaction(entity) : null;
        }

        public ReadOnlyExternalCharacter FindReadOnlyExternalCharacter(string id)
        {
            var entity = FindExternalCharacter(id);
            return entity != null ? new ReadOnlyExternalCharacter(entity) : null;
        }

        public ReadOnlyAsteroid FindReadOnlyAsteroid(string id)
        {
            var entity = FindAsteroid(id);
            return entity != null ? new ReadOnlyAsteroid(entity) : null;
        }

        // -- Task 5.3: Current-player filtered read-only methods --

        public List<ReadOnlyColony> GetCurrentPlayerReadOnlyColonies()
        {
            lock (_listLock)
            {
                return _colonyList
                    .Where(c => c.OwnerUUID == CurrentPlayerUUID)
                    .Select(c => new ReadOnlyColony(c))
                    .ToList();
            }
        }

        public List<ReadOnlyBlueprint> GetCurrentPlayerReadOnlyBlueprints()
        {
            lock (_listLock)
            {
                return _blueprintList
                    .Where(b => b.OwnerUUID == CurrentPlayerUUID)
                    .Select(b => new ReadOnlyBlueprint(b))
                    .ToList();
            }
        }

        public List<ReadOnlySurvey> GetCurrentPlayerReadOnlySurveys()
        {
            lock (_listLock)
            {
                return _surveyList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlySurvey(s))
                    .ToList();
            }
        }

        public List<ReadOnlyDeliveryRoute> GetCurrentPlayerReadOnlyRoutes()
        {
            lock (_listLock)
            {
                return _deliveryRouteList
                    .Where(r => r.OwnerUUID == CurrentPlayerUUID)
                    .Select(r => new ReadOnlyDeliveryRoute(r))
                    .ToList();
            }
        }

        public List<ReadOnlyDeliveryPlan> GetCurrentPlayerReadOnlyPlans()
        {
            lock (_listLock)
            {
                return _deliveryPlanList
                    .Where(p => p.OwnerUUID == CurrentPlayerUUID)
                    .Select(p => new ReadOnlyDeliveryPlan(p))
                    .ToList();
            }
        }

        public List<ReadOnlyPricingPlan> GetCurrentPlayerReadOnlyPricingPlans()
        {
            lock (_listLock)
            {
                return _pricingPlanList
                    .Where(p => p.OwnerUUID == CurrentPlayerUUID)
                    .Select(p => new ReadOnlyPricingPlan(p))
                    .ToList();
            }
        }

        public List<ReadOnlyBuildPlan> GetCurrentPlayerReadOnlyBuildPlans()
        {
            lock (_listLock)
            {
                return _buildPlanList
                    .Where(b => b.OwnerUUID == CurrentPlayerUUID)
                    .Select(b => new ReadOnlyBuildPlan(b))
                    .ToList();
            }
        }

        public List<ReadOnlyShipTemplate> GetCurrentPlayerReadOnlyShipTemplates()
        {
            lock (_listLock)
            {
                return _shipTemplateList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlyShipTemplate(s))
                    .ToList();
            }
        }

        public List<ReadOnlyShip> GetCurrentPlayerReadOnlyShips()
        {
            lock (_listLock)
            {
                return _shipList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlyShip(s))
                    .ToList();
            }
        }

        public List<ReadOnlyMarketListing> GetCurrentPlayerReadOnlyListings()
        {
            lock (_listLock)
            {
                return _marketListingList
                    .Where(m => m.OwnerUUID == CurrentPlayerUUID)
                    .Select(m => new ReadOnlyMarketListing(m))
                    .ToList();
            }
        }

        public List<ReadOnlyMarketTransaction> GetCurrentPlayerReadOnlyTransactions()
        {
            lock (_listLock)
            {
                return _marketTransactionList
                    .Where(m => m.OwnerUUID == CurrentPlayerUUID)
                    .Select(m => new ReadOnlyMarketTransaction(m))
                    .ToList();
            }
        }

        public List<ReadOnlyStockPlan> GetCurrentPlayerReadOnlyStockPlans()
        {
            lock (_listLock)
            {
                return _stockPlanList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlyStockPlan(s))
                    .ToList();
            }
        }

        public List<ReadOnlyStockProfile> GetCurrentPlayerReadOnlyStockProfiles()
        {
            lock (_listLock)
            {
                return _stockProfileList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlyStockProfile(s))
                    .ToList();
            }
        }

        public List<ReadOnlySupplyChain> GetCurrentPlayerReadOnlySupplyChains()
        {
            lock (_listLock)
            {
                return _supplyChainList
                    .Where(s => s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlySupplyChain(s))
                    .ToList();
            }
        }

        public List<ReadOnlyWarehouseOverflowRule> GetCurrentPlayerReadOnlyOverflowRules()
        {
            lock (_listLock)
            {
                return _warehouseOverflowRuleList
                    .Where(w => w.OwnerUUID == CurrentPlayerUUID)
                    .Select(w => new ReadOnlyWarehouseOverflowRule(w))
                    .ToList();
            }
        }

        public List<ReadOnlyStation> GetCurrentPlayerReadOnlyStations()
        {
            lock (_listLock)
            {
                return _stationList
                    .Where(s =>
                        s.Ownership == StationOwnership.Government ||
                        s.OwnerUUID == CurrentPlayerUUID)
                    .Select(s => new ReadOnlyStation(s))
                    .ToList();
            }
        }

        /// <summary>
        /// Returns all blueprints (current player's + global) as read-only wrappers.
        /// </summary>
        public IReadOnlyList<ReadOnlyBlueprint> GetAllReadOnlyBlueprints()
        {
            lock (_listLock)
            {
                var result = _blueprintList
                    .Select(b => new ReadOnlyBlueprint(b))
                    .ToList();
                var ec = EmpireContext.GetInstance();
                if (ec?.GlobalBlueprintList != null)
                {
                    foreach (var gb in ec.GlobalBlueprintList)
                    {
                        result.Add(new ReadOnlyBlueprint(gb));
                    }
                }

                return result;
            }
        }

        public List<CountDownTimeReference> AllCountdownSources()
        {
            List<CountDownTimeReference> countdowns = new List<CountDownTimeReference>();
            foreach (var player in _playerProfileList)
            {
                if (player.Skills != null)
                {
                    foreach (var skill in player.Skills)
                    {
                        if (skill.Value.CompletionTime != null && skill.Value.CompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.Source = CountDownTimeReference.SourceType.Player;
                            reference.SourceUUID = player.UUID;
                            reference.InternalUUID = skill.Key;
                            reference.CountDownTime = skill.Value.CompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }

            foreach (var colony in _colonyList)
            {
                if (colony.Structures != null)
                {
                    foreach (var structure in colony.Structures)
                    {
                        if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.Source = CountDownTimeReference.SourceType.Colony;
                            reference.SourceUUID = colony.UUID;
                            reference.InternalUUID = structure.UUID;
                            reference.CountDownTime = structure.ProcessCompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }

            return countdowns;
        }

        /// <summary>
        /// Returns the mutable ShipTemplate entity. Only called by ShipTemplateService.
        /// </summary>
        /// <summary>
        /// Returns the mutable Ship entity by UUID. Internal so only the service can access it.
        /// </summary>
        /// <summary>
        /// Returns the mutable Station entity by UUID. Internal so only the service can access it.
        /// </summary>
        internal Station FindMutableStation(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_stationCache == null)
                {
                    _stationCache = new Dictionary<string, Station>();
                    foreach (var s in _stationList)
                    {
                        if (s.UUID != null && !_stationCache.ContainsKey(s.UUID))
                            _stationCache[s.UUID] = s;
                    }
                }

                _stationCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable Ship entity by UUID. Internal so only the service can access it.
        /// </summary>
        internal Ship FindMutableShip(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_shipCache == null)
                {
                    _shipCache = new Dictionary<string, Ship>();
                    foreach (var s in _shipList)
                    {
                        if (s.UUID != null && !_shipCache.ContainsKey(s.UUID))
                            _shipCache[s.UUID] = s;
                    }
                }

                _shipCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        internal ShipTemplate FindMutableShipTemplate(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_shipTemplateCache == null)
                {
                    _shipTemplateCache = new Dictionary<string, ShipTemplate>();
                    foreach (var st in _shipTemplateList)
                    {
                        if (st.UUID != null && !_shipTemplateCache.ContainsKey(st.UUID))
                            _shipTemplateCache[st.UUID] = st;
                    }
                }

                _shipTemplateCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable Blueprint entity for the given UUID from the player list only.
        /// Does NOT fall back to global blueprints. Only called by BlueprintService.
        /// </summary>
        internal Blueprint FindMutableBlueprint(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_blueprintCache == null)
                {
                    _blueprintCache = new Dictionary<string, Blueprint>();
                    foreach (var bp in _blueprintList)
                    {
                        if (bp.UUID != null && !_blueprintCache.ContainsKey(bp.UUID))
                            _blueprintCache[bp.UUID] = bp;
                    }
                }

                _blueprintCache.TryGetValue(uuid, out Blueprint bp2);
                return bp2;
            }
        }

        /// <summary>
        /// Returns the mutable PlayerProfile entity. Only called by PlayerProfileService.
        /// </summary>
        internal PlayerProfile FindMutablePlayerProfile(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_playerProfileCache == null)
                {
                    _playerProfileCache = new Dictionary<string, PlayerProfile>();
                    foreach (var r in _playerProfileList)
                    {
                        if (r.UUID != null && !_playerProfileCache.ContainsKey(r.UUID))
                            _playerProfileCache[r.UUID] = r;
                    }
                }

                _playerProfileCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable PricingPlan entity. Only called by PricingPlanService.
        /// </summary>
        internal PricingPlan FindMutablePricingPlan(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_pricingPlanCache == null)
                {
                    _pricingPlanCache = new Dictionary<string, PricingPlan>();
                    foreach (var r in _pricingPlanList)
                    {
                        if (r.UUID != null && !_pricingPlanCache.ContainsKey(r.UUID))
                            _pricingPlanCache[r.UUID] = r;
                    }
                }

                _pricingPlanCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable Survey entity. Only called by SurveyService.
        /// </summary>
        internal Survey FindMutableSurvey(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_surveyCache == null)
                {
                    _surveyCache = new Dictionary<string, Survey>();
                    foreach (var s in _surveyList)
                    {
                        if (s.UUID != null && !_surveyCache.ContainsKey(s.UUID))
                            _surveyCache[s.UUID] = s;
                    }
                }

                _surveyCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable Colony entity. Only called by ColonyService.
        /// </summary>
        internal Colony FindMutableColony(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_colonyCache == null)
                {
                    _colonyCache = new Dictionary<string, Colony>();
                    foreach (var c in _colonyList)
                    {
                        if (c.UUID != null && !_colonyCache.ContainsKey(c.UUID))
                            _colonyCache[c.UUID] = c;
                    }
                }

                _colonyCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable DeliveryRoute entity. Only called by DeliveryRouteService.
        /// </summary>
        internal DeliveryRoute FindMutableDeliveryRoute(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_deliveryRouteCache == null)
                {
                    _deliveryRouteCache = new Dictionary<string, DeliveryRoute>();
                    foreach (var r in _deliveryRouteList)
                    {
                        if (r.UUID != null && !_deliveryRouteCache.ContainsKey(r.UUID))
                            _deliveryRouteCache[r.UUID] = r;
                    }
                }

                _deliveryRouteCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable DeliveryPlan entity. Only called by DeliveryPlanService.
        /// </summary>
        internal DeliveryPlan FindMutableDeliveryPlan(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_deliveryPlanCache == null)
                {
                    _deliveryPlanCache = new Dictionary<string, DeliveryPlan>();
                    foreach (var p in _deliveryPlanList)
                    {
                        if (p.UUID != null && !_deliveryPlanCache.ContainsKey(p.UUID))
                            _deliveryPlanCache[p.UUID] = p;
                    }
                }

                _deliveryPlanCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        /// <summary>
        /// Returns the mutable MarketListing entity. Only called by MarketListingService.
        /// </summary>
        /// <summary>
        /// Returns the mutable BuildPlan entity by UUID. Internal so only the service can access it.
        /// </summary>
        internal BuildPlan FindMutableBuildPlan(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_buildPlanCache == null)
                {
                    _buildPlanCache = new Dictionary<string, BuildPlan>();
                    foreach (var bp in _buildPlanList)
                    {
                        if (bp.UUID != null && !_buildPlanCache.ContainsKey(bp.UUID))
                            _buildPlanCache[bp.UUID] = bp;
                    }
                }

                _buildPlanCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        internal MarketListing FindMutableMarketListing(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            lock (_listLock)
            {
                if (_marketListingCache == null)
                {
                    _marketListingCache = new Dictionary<string, MarketListing>();
                    foreach (var ml in _marketListingList)
                    {
                        if (ml.UUID != null && !_marketListingCache.ContainsKey(ml.UUID))
                            _marketListingCache[ml.UUID] = ml;
                    }
                }

                _marketListingCache.TryGetValue(uuid, out var match);
                return match;
            }
        }

        private void RebuildBuildItemIndexes()
        {
            _blueprintBuildItemIndex = new Dictionary<string, List<BuildItem>>();
            _buildLocationBuildItemIndex = new Dictionary<string, List<BuildItem>>();
            foreach (var plan in _buildPlanList)
            {
                if (plan.Items == null) continue;
                foreach (var item in plan.Items)
                {
                    if (!string.IsNullOrEmpty(item.BlueprintUUID))
                    {
                        if (!_blueprintBuildItemIndex.ContainsKey(item.BlueprintUUID))
                            _blueprintBuildItemIndex[item.BlueprintUUID] = new List<BuildItem>();
                        _blueprintBuildItemIndex[item.BlueprintUUID].Add(item);
                    }

                    if (!string.IsNullOrEmpty(item.BuildLocationUUID))
                    {
                        if (!_buildLocationBuildItemIndex.ContainsKey(item.BuildLocationUUID))
                            _buildLocationBuildItemIndex[item.BuildLocationUUID] = new List<BuildItem>();
                        _buildLocationBuildItemIndex[item.BuildLocationUUID].Add(item);
                    }
                }
            }
        }

        // -----------------------------------------------------------------------
        // Player Selection & Migration
        // -----------------------------------------------------------------------

        /// <summary>
        /// Auto-assigns empty OwnerUUID on colonies, blueprints, and surveys
        /// to the first player profile (alphabetically). Handles migration of
        /// existing save files that predate multi-player support.
        /// </summary>
        private void MigrateOwnerUUIDs()
        {
            if (_playerProfileList.Count == 0) return;

            string firstPlayerUUID = _playerProfileList[0].UUID;
            if (string.IsNullOrEmpty(firstPlayerUUID)) return;

            int migrated = 0;
            foreach (var colony in _colonyList)
            {
                if (string.IsNullOrEmpty(colony.OwnerUUID))
                {
                    colony.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }

            foreach (var blueprint in _blueprintList)
            {
                if (string.IsNullOrEmpty(blueprint.OwnerUUID))
                {
                    blueprint.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }

            foreach (var survey in _surveyList)
            {
                if (string.IsNullOrEmpty(survey.OwnerUUID))
                {
                    survey.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }

            if (migrated > 0)
            {
                Log.Info("Migrated {0} items to player {1}", migrated, _playerProfileList[0].Name);
            }
        }

        /// <summary>
        /// Removes data owned by players that no longer exist.
        /// Called on load after all lists are initialized.
        /// </summary>
        private void CleanupOrphanedData()
        {
            var validUUIDs = new HashSet<string>(_playerProfileList.Select(p => p.UUID));
            int removed = 0;

            foreach (var colony in _colonyList.Where(c => !string.IsNullOrEmpty(c.OwnerUUID) && !validUUIDs.Contains(c.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned colony: {0} ({1}) owner={2}", colony.PlanetName, colony.ColonyName, colony.OwnerUUID);

                _colonyList.Remove(colony);
                removed++;
            }

            foreach (var bp in _blueprintList.Where(b => !string.IsNullOrEmpty(b.OwnerUUID) && !validUUIDs.Contains(b.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned blueprint: {0} owner={1}", bp.ExtendedName, bp.OwnerUUID);

                _blueprintList.Remove(bp);
                removed++;
            }

            foreach (var survey in _surveyList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned survey: {0} owner={1}", survey.ExtendedName, survey.OwnerUUID);

                _surveyList.Remove(survey);
                removed++;
            }

            foreach (var route in _deliveryRouteList.Where(r => !string.IsNullOrEmpty(r.OwnerUUID) && !validUUIDs.Contains(r.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned route: {0} owner={1}", route.Name, route.OwnerUUID);

                _deliveryRouteList.Remove(route);
                removed++;
            }

            foreach (var plan in _deliveryPlanList.Where(p => !string.IsNullOrEmpty(p.OwnerUUID) && !validUUIDs.Contains(p.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned delivery plan: {0} owner={1}", plan.Name, plan.OwnerUUID);

                _deliveryPlanList.Remove(plan);
                removed++;
            }

            foreach (var pp in _pricingPlanList.Where(p => !string.IsNullOrEmpty(p.OwnerUUID) && !validUUIDs.Contains(p.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned pricing plan: {0} owner={1}", pp.Name, pp.OwnerUUID);

                _pricingPlanList.Remove(pp);
                removed++;
            }

            foreach (var bp2 in _buildPlanList.Where(b => !string.IsNullOrEmpty(b.OwnerUUID) && !validUUIDs.Contains(b.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned build plan: {0} owner={1}", bp2.Name, bp2.OwnerUUID);

                _buildPlanList.Remove(bp2);
                removed++;
            }

            foreach (var st in _shipTemplateList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned ship template: {0} owner={1}", st.Name, st.OwnerUUID);

                _shipTemplateList.Remove(st);
                removed++;
            }

            foreach (var ship in _shipList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned ship: {0} owner={1}", ship.Name, ship.OwnerUUID);

                _shipList.Remove(ship);
                removed++;
            }

            foreach (var station in _stationList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned station: {0} owner={1}", station.Name, station.OwnerUUID);

                _stationList.Remove(station);
                removed++;
            }

            foreach (var ml in _marketListingList.Where(m => !string.IsNullOrEmpty(m.OwnerUUID) && !validUUIDs.Contains(m.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned market listing: {0} owner={1}", ml.UUID, ml.OwnerUUID);

                _marketListingList.Remove(ml);
                removed++;
            }

            foreach (var mt in _marketTransactionList.Where(m => !string.IsNullOrEmpty(m.OwnerUUID) && !validUUIDs.Contains(m.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned market transaction: {0} owner={1}", mt.UUID, mt.OwnerUUID);

                _marketTransactionList.Remove(mt);
                removed++;
            }

            foreach (var sp in _stockPlanList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned stock plan: {0} owner={1}", sp.Name, sp.OwnerUUID);

                _stockPlanList.Remove(sp);
                removed++;
            }

            foreach (var spf in _stockProfileList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned stock profile: {0} owner={1}", spf.Name, spf.OwnerUUID);

                _stockProfileList.Remove(spf);
                removed++;
            }

            foreach (var sc in _supplyChainList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned supply chain: {0} owner={1}", sc.Name, sc.OwnerUUID);

                _supplyChainList.Remove(sc);
                removed++;
            }

            foreach (var wor in _warehouseOverflowRuleList.Where(w => !string.IsNullOrEmpty(w.OwnerUUID) && !validUUIDs.Contains(w.OwnerUUID)).ToList())
            {
                Log.Warn("Removing orphaned overflow rule: {0} owner={1}", wor.UUID, wor.OwnerUUID);
                _warehouseOverflowRuleList.Remove(wor);
                removed++;
            }

            if (removed > 0)
                Log.Info("Cleaned up {0} orphaned items on load", removed);

            InvalidateBlueprintCache();
            InvalidateSurveyCache();
            InvalidateColonyCache();
        }

        /// <summary>
        /// Restores the current player from the saved UUID, falling back to
        /// the first player if the saved UUID is invalid or empty.
        /// Does not fire CurrentPlayerChanged (called during construction).
        /// </summary>
        private void RestoreCurrentPlayer(string savedUUID)
        {
            if (!string.IsNullOrEmpty(savedUUID) &&
                _playerProfileList.Any(p => p.UUID == savedUUID))
            {
                _currentPlayerUUID = savedUUID;
            }
            else if (_playerProfileList.Count > 0)
            {
                _currentPlayerUUID = _playerProfileList[0].UUID ?? string.Empty;
            }

            Log.Info("Current player restored: {0}", _currentPlayerUUID);
        }
    }

    public class PlayerRoot
    {
        public PlayerRoot()
        {
            DataVersion = 0;
            CurrentPlayerUUID = string.Empty;
            PlayerProfile = new PlayerProfile[0];
            Blueprint = new Blueprint[0];
            Survey = new Survey[0];
            Colony = new Colony[0];
            DeliveryRoute = new DeliveryRoute[0];
            DeliveryPlan = new DeliveryPlan[0];
            PricingPlan = new PricingPlan[0];
            BuildPlan = new BuildPlan[0];
            ShipTemplate = new ShipTemplate[0];
            Ship = new Ship[0];
            Station = new Station[0];
            MarketListing = new MarketListing[0];
            MarketTransaction = new MarketTransaction[0];
            StockPlan = new StockPlan[0];
            StockProfile = new StockProfile[0];
            SupplyChain = new SupplyChain[0];
            WarehouseOverflowRule = new WarehouseOverflowRule[0];
            Faction = new Faction[0];
            ExternalCharacter = new ExternalCharacter[0];
            Asteroid = new Asteroid[0];
        }

        public int DataVersion { get; set; }

        public string CurrentPlayerUUID { get; set; }

        public PlayerProfile[] PlayerProfile { get; set; }

        public Blueprint[] Blueprint { get; set; }

        public Survey[] Survey { get; set; }

        public Colony[] Colony { get; set; }

        public DeliveryRoute[] DeliveryRoute { get; set; }

        public DeliveryPlan[] DeliveryPlan { get; set; }

        public PricingPlan[] PricingPlan { get; set; }

        public BuildPlan[] BuildPlan { get; set; }

        public ShipTemplate[] ShipTemplate { get; set; }

        public Ship[] Ship { get; set; }

        public Station[] Station { get; set; }

        public MarketListing[] MarketListing { get; set; }

        public MarketTransaction[] MarketTransaction { get; set; }

        public StockPlan[] StockPlan { get; set; }

        public StockProfile[] StockProfile { get; set; }

        public SupplyChain[] SupplyChain { get; set; }

        public WarehouseOverflowRule[] WarehouseOverflowRule { get; set; }

        public Faction[] Faction { get; set; }

        public ExternalCharacter[] ExternalCharacter { get; set; }

        public Asteroid[] Asteroid { get; set; }
    }

    public class CountDownTimeReference
    {
        public enum SourceType
        {
            None,
            Player,
            Colony
        }

        public SourceType Source { get; set; }
        public string SourceUUID { get; set; }
        public string InternalUUID { get; set; }
        public CountDownTime CountDownTime { get; set; }
    }

    public class ColonyDataChangedEventArgs : EventArgs
    {
        public ColonyDataChangedEventArgs(string colonyUUID) { ColonyUUID = colonyUUID; }

        public string ColonyUUID { get; }
    }

    public class BlueprintDataChangedEventArgs : EventArgs
    {
        public BlueprintDataChangedEventArgs(string blueprintUUID) { BlueprintUUID = blueprintUUID; }

        public string BlueprintUUID { get; }
    }

    public class SurveyDataChangedEventArgs : EventArgs
    {
        public SurveyDataChangedEventArgs(string surveyUUID) { SurveyUUID = surveyUUID; }

        public string SurveyUUID { get; }
    }

    public class PlayerProfileDataChangedEventArgs : EventArgs
    {
        public PlayerProfileDataChangedEventArgs(string playerUUID) { PlayerUUID = playerUUID; }

        public string PlayerUUID { get; }
    }

    public class BuildPlanDataChangedEventArgs : EventArgs
    {
        public BuildPlanDataChangedEventArgs(string uuid) { BuildPlanUUID = uuid; }

        public string BuildPlanUUID { get; }
    }

    public class AsteroidDataChangedEventArgs : EventArgs
    {
        public AsteroidDataChangedEventArgs(string asteroidUUID) { AsteroidUUID = asteroidUUID; }

        public string AsteroidUUID { get; }
    }

    public class ShipTemplateDataChangedEventArgs : EventArgs
    {
        public ShipTemplateDataChangedEventArgs(string uuid) { ShipTemplateUUID = uuid; }

        public string ShipTemplateUUID { get; }
    }

    public class ShipDataChangedEventArgs : EventArgs
    {
        public ShipDataChangedEventArgs(string uuid) { ShipUUID = uuid; }

        public string ShipUUID { get; }
    }

    public class StockDataChangedEventArgs : EventArgs
    {
        public StockDataChangedEventArgs(string uuid) { StockPlanUUID = uuid; }

        public string StockPlanUUID { get; }
    }

    public class SupplyChainDataChangedEventArgs : EventArgs
    {
        public SupplyChainDataChangedEventArgs(string uuid) { SupplyChainUUID = uuid; }

        public string SupplyChainUUID { get; }
    }

    public class ContactDataChangedEventArgs : EventArgs
    {
        public ContactDataChangedEventArgs(string uuid) { CharacterUUID = uuid; }

        public string CharacterUUID { get; }
    }
}
