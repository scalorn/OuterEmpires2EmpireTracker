using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Services
{
    public class PlayerContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static PlayerContext Instance;
        public static string FilePath { get; set; } = "PlayerData.json";

        private string _currentPlayerUUID = string.Empty;

        private readonly object _listLock = new object();

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

        private Dictionary<string, int> _blueprintTypeCountCache;

        private Dictionary<string, List<BuildItem>> _blueprintBuildItemIndex;
        private Dictionary<string, List<BuildItem>> _buildLocationBuildItemIndex;

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
        /// Fired when a player profile is modified externally (e.g. skill training completion).
        /// </summary>
        public event EventHandler<PlayerProfileDataChangedEventArgs> PlayerProfileDataChanged;

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
        /// Returns the PlayerProfile for the currently selected player, or null.
        /// </summary>
        public PlayerProfile CurrentPlayer
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPlayerUUID)) return null;
                return PlayerProfileList.FirstOrDefault(p => p.UUID == _currentPlayerUUID);
            }
        }

        public int DataVersion { get; set; } = 0;
        public BindingList<PlayerProfile> PlayerProfileList;
        public BindingSource BindingSourcePlayerProfile;
        public BindingList<Blueprint> BlueprintList;
        public BindingSource BindingSourceBlueprint;
        public BindingList<Survey> SurveyList;
        public BindingSource BindingSourceSurvey;
        public BindingList<Colony> ColonyList;
        public BindingSource BindingSourceColony;
        public BindingList<DeliveryRoute> DeliveryRouteList;
        public BindingList<DeliveryPlan> DeliveryPlanList;
        public BindingList<PricingPlan> PricingPlanList;

        public List<BuildPlan> BuildPlanList = new List<BuildPlan>();
        public List<ShipTemplate> ShipTemplateList = new List<ShipTemplate>();
        public List<Ship> ShipList = new List<Ship>();
        public List<Station> StationList = new List<Station>();
        public List<MarketListing> MarketListingList = new List<MarketListing>();
        public List<MarketTransaction> MarketTransactionList = new List<MarketTransaction>();
        public List<StockPlan> StockPlanList = new List<StockPlan>();
        public List<StockProfile> StockProfileList = new List<StockProfile>();
        public List<SupplyChain> SupplyChainList = new List<SupplyChain>();
        public List<WarehouseOverflowRule> WarehouseOverflowRuleList = new List<WarehouseOverflowRule>();
        public List<Faction> FactionList = new List<Faction>();
        public List<ExternalCharacter> ExternalCharacterList = new List<ExternalCharacter>();
        public List<Asteroid> AsteroidList = new List<Asteroid>();

        public IEnumerable<CountDownTimeReference> ActiveCountdowns => AllCountdownSources()
            .Where(c => c.countDownTime.TimeRemaining > 0)
            .OrderBy(c => c.countDownTime.TimeRemaining);

        public static PlayerContext GetInstance()
        {
            if (Instance == null)
            {
                Instance = new PlayerContext();
            }
            return Instance;
        }

        public static void Reset()
        {
            Instance = null;
        }

        private PlayerContext() : base()
        {
            Instance = this;

            PlayerRoot playerRoot = null;
            if (File.Exists(FilePath))
            {
                Log.Info("Loading player data from {0}", FilePath);
                string jsonContent = File.ReadAllText(FilePath);
                playerRoot = JsonConvert.DeserializeObject<PlayerRoot>(jsonContent);
                Log.Info("Loaded {0} profiles, {1} blueprints, {2} surveys, {3} colonies",
                    playerRoot.PlayerProfile.Length, playerRoot.Blueprint.Length,
                    playerRoot.Survey.Length, playerRoot.Colony.Length);
            }
            else
            {
                Log.Warn("Player data file not found at {0}, starting with empty data", FilePath);
                playerRoot = new PlayerRoot();
            }
            initPlayerProfiles(playerRoot);
            InitBlueprints(playerRoot);
            InitSurveys(playerRoot);
            initColonies(playerRoot);
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
                playerRoot.PlayerProfile = PlayerProfileList.ToArray();
                playerRoot.Blueprint = BlueprintList.ToArray();
                playerRoot.Survey = SurveyList.ToArray();
                playerRoot.Colony = ColonyList.ToArray();
                playerRoot.DeliveryRoute = DeliveryRouteList.ToArray();
                playerRoot.DeliveryPlan = DeliveryPlanList.ToArray();
                playerRoot.PricingPlan = PricingPlanList.ToArray();
                playerRoot.BuildPlan = BuildPlanList.ToArray();
                playerRoot.ShipTemplate = ShipTemplateList.ToArray();
                playerRoot.Ship = ShipList.ToArray();
                playerRoot.Station = StationList.ToArray();
                playerRoot.MarketListing = MarketListingList.ToArray();
                playerRoot.MarketTransaction = MarketTransactionList.ToArray();
                playerRoot.StockPlan = StockPlanList.ToArray();
                playerRoot.StockProfile = StockProfileList.ToArray();
                playerRoot.SupplyChain = SupplyChainList.ToArray();
                playerRoot.WarehouseOverflowRule = WarehouseOverflowRuleList.ToArray();
                playerRoot.Faction = FactionList.ToArray();
                playerRoot.ExternalCharacter = ExternalCharacterList.ToArray();
                playerRoot.Asteroid = AsteroidList.ToArray();
            }

            string jsonContent = JsonConvert.SerializeObject(playerRoot, JsonSettings.SerializerSettings);
            SafeFileWriter.WriteAllText(FilePath, jsonContent);
            Log.Info("Player data saved to {0}", FilePath);
        }
        public void initPlayerProfiles(PlayerRoot playerRoot)
        {
            List<PlayerProfile> list = new List<PlayerProfile>(playerRoot.PlayerProfile);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            PlayerProfileList = new BindingList<PlayerProfile>(list);
            // Initialize the BindingSource component
            BindingSourcePlayerProfile = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourcePlayerProfile.DataSource = PlayerProfileList;
        }
        public void InitBlueprints(PlayerRoot playerRoot)
        {
            List<Blueprint> list = new List<Blueprint>(playerRoot.Blueprint);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            BlueprintList = new BindingList<Blueprint>(list);
            BlueprintList.ListChanged += (s, e) => InvalidateBlueprintCache();
            // Initialize the BindingSource component
            BindingSourceBlueprint = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceBlueprint.DataSource = BlueprintList;
            InvalidateBlueprintCache();
        }
        public Blueprint FindBlueprint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_listLock)
            {
                if (_blueprintCache == null)
                {
                    _blueprintCache = new Dictionary<string, Blueprint>();
                    foreach (var bp in BlueprintList)
                    {
                        if (bp.UUID != null && !_blueprintCache.ContainsKey(bp.UUID))
                            _blueprintCache[bp.UUID] = bp;
                    }
                }

                if (_blueprintCache.TryGetValue(id, out var match))
                    return match;
            }

            // Fall back to global blueprints outside the lock
            return EmpireContext.GetInstance()?.FindGlobalBlueprint(id);
        }

        public void InvalidateBlueprintCache()
        {
            lock (_listLock) { _blueprintCache = null; }
            InvalidateAllBlueprintsCache();
        }

        public void InitSurveys(PlayerRoot playerRoot)
        {
            List<Survey> list = new List<Survey>(playerRoot.Survey);
            list = list.OrderBy(p => p.PlanetName).ThenBy(p => p.DateTime).ToList();

            SurveyList = new BindingList<Survey>(list);
            SurveyList.ListChanged += (s, e) => InvalidateSurveyCache();
            // Initialize the BindingSource component
            BindingSourceSurvey = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceSurvey.DataSource = SurveyList;
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
                    foreach (var s in SurveyList)
                    {
                        if (s.UUID != null && !_surveyCache.ContainsKey(s.UUID))
                            _surveyCache[s.UUID] = s;
                    }
                }

                if (_surveyCache.TryGetValue(id, out var match))
                    return match;
            }

            return null;
        }

        public void InvalidateSurveyCache()
        {
            lock (_listLock) { _surveyCache = null; }
        }

        public void initColonies(PlayerRoot playerRoot)
        {
            List<Colony> list = new List<Colony>(playerRoot.Colony);
            list = list.OrderBy(p => p.PlanetName).ToList();

            ColonyList = new BindingList<Colony>(list);
            ColonyList.ListChanged += (s, e) => InvalidateColonyCache();
            // Initialize the BindingSource component
            BindingSourceColony = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceColony.DataSource = ColonyList;
            InvalidateColonyCache();
        }

        public void InitDeliveryRoutes(PlayerRoot playerRoot)
        {
            var list = new List<DeliveryRoute>(playerRoot.DeliveryRoute ?? new DeliveryRoute[0]);
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            DeliveryRouteList = new BindingList<DeliveryRoute>(list);
        }

        public void InitDeliveryPlans(PlayerRoot playerRoot)
        {
            var list = new List<DeliveryPlan>(playerRoot.DeliveryPlan ?? new DeliveryPlan[0]);
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            DeliveryPlanList = new BindingList<DeliveryPlan>(list);
        }

        public void InitPricingPlans(PlayerRoot playerRoot)
        {
            var list = new List<PricingPlan>(playerRoot.PricingPlan ?? new PricingPlan[0]);
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
            PricingPlanList = new BindingList<PricingPlan>(list);
        }

        public void InitBuildPlans(PlayerRoot playerRoot)
        {
            BuildPlanList = new List<BuildPlan>(playerRoot.BuildPlan ?? new BuildPlan[0]);
        }

        public void InitShipTemplates(PlayerRoot playerRoot)
        {
            ShipTemplateList = new List<ShipTemplate>(playerRoot.ShipTemplate ?? new ShipTemplate[0]);
        }

        public void InitShips(PlayerRoot playerRoot)
        {
            ShipList = new List<Ship>(playerRoot.Ship ?? new Ship[0]);
        }

        public void InitStations(PlayerRoot playerRoot)
        {
            StationList = new List<Station>(playerRoot.Station ?? new Station[0]);
        }

        public void InitMarketListings(PlayerRoot playerRoot)
        {
            MarketListingList = new List<MarketListing>(playerRoot.MarketListing ?? new MarketListing[0]);
        }

        public void InitMarketTransactions(PlayerRoot playerRoot)
        {
            MarketTransactionList = new List<MarketTransaction>(playerRoot.MarketTransaction ?? new MarketTransaction[0]);
        }

        public void InitStockPlans(PlayerRoot playerRoot)
        {
            StockPlanList = new List<StockPlan>(playerRoot.StockPlan ?? new StockPlan[0]);
        }

        public void InitStockProfiles(PlayerRoot playerRoot)
        {
            StockProfileList = new List<StockProfile>(playerRoot.StockProfile ?? new StockProfile[0]);
        }

        public void InitSupplyChains(PlayerRoot playerRoot)
        {
            SupplyChainList = new List<SupplyChain>(playerRoot.SupplyChain ?? new SupplyChain[0]);
        }

        public void InitWarehouseOverflowRules(PlayerRoot playerRoot)
        {
            WarehouseOverflowRuleList = new List<WarehouseOverflowRule>(playerRoot.WarehouseOverflowRule ?? new WarehouseOverflowRule[0]);
        }

        public void InitFactions(PlayerRoot playerRoot)
        {
            FactionList = new List<Faction>(playerRoot.Faction ?? new Faction[0]);
        }

        public void InitExternalCharacters(PlayerRoot playerRoot)
        {
            ExternalCharacterList = new List<ExternalCharacter>(playerRoot.ExternalCharacter ?? new ExternalCharacter[0]);
        }

        public void InitAsteroids(PlayerRoot playerRoot)
        {
            AsteroidList = new List<Asteroid>(playerRoot.Asteroid ?? new Asteroid[0]);
        }

        public Colony FindColony(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (_listLock)
            {
                if (_colonyCache == null)
                {
                    _colonyCache = new Dictionary<string, Colony>();
                    foreach (var c in ColonyList)
                    {
                        if (c.UUID != null && !_colonyCache.ContainsKey(c.UUID))
                            _colonyCache[c.UUID] = c;
                    }
                }

                if (_colonyCache.TryGetValue(id, out var match))
                    return match;
            }

            return null;
        }

        public void InvalidateColonyCache()
        {
            lock (_listLock) { _colonyCache = null; }
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
                    foreach (var s in StationList)
                        if (s.UUID != null && !_stationCache.ContainsKey(s.UUID))
                            _stationCache[s.UUID] = s;
                }
                _stationCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateStationCache() { lock (_listLock) { _stationCache = null; } }

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
                    foreach (var st in ShipTemplateList)
                        if (st.UUID != null && !_shipTemplateCache.ContainsKey(st.UUID))
                            _shipTemplateCache[st.UUID] = st;
                }
                _shipTemplateCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateShipTemplateCache() { lock (_listLock) { _shipTemplateCache = null; } }

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
                    foreach (var s in ShipList)
                        if (s.UUID != null && !_shipCache.ContainsKey(s.UUID))
                            _shipCache[s.UUID] = s;
                }
                _shipCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateShipCache() { lock (_listLock) { _shipCache = null; } }

        /// <summary>
        /// Finds a BuildPlan by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public BuildPlan FindBuildPlan(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            lock (_listLock)
            {
                if (_buildPlanCache == null)
                {
                    _buildPlanCache = new Dictionary<string, BuildPlan>();
                    foreach (var bp in BuildPlanList)
                        if (bp.UUID != null && !_buildPlanCache.ContainsKey(bp.UUID))
                            _buildPlanCache[bp.UUID] = bp;
                }
                _buildPlanCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateBuildPlanCache() { lock (_listLock) { _buildPlanCache = null; } }

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
                    foreach (var a in AsteroidList)
                        if (a.UUID != null && !_asteroidCache.ContainsKey(a.UUID))
                            _asteroidCache[a.UUID] = a;
                }
                _asteroidCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateAsteroidCache() { lock (_listLock) { _asteroidCache = null; } }

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
                    foreach (var f in FactionList)
                        if (f.UUID != null && !_factionCache.ContainsKey(f.UUID))
                            _factionCache[f.UUID] = f;
                }
                _factionCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateFactionCache() { lock (_listLock) { _factionCache = null; } }

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
                    foreach (var ml in MarketListingList)
                        if (ml.UUID != null && !_marketListingCache.ContainsKey(ml.UUID))
                            _marketListingCache[ml.UUID] = ml;
                }
                _marketListingCache.TryGetValue(id, out var match);
                return match;
            }
        }

        public void InvalidateMarketListingCache() { lock (_listLock) { _marketListingCache = null; } }

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
                        var key = bp.BluePrintType ?? "";
                        if (_blueprintTypeCountCache.ContainsKey(key))
                            _blueprintTypeCountCache[key]++;
                        else
                            _blueprintTypeCountCache[key] = 1;
                    }
                }
                return _blueprintTypeCountCache.TryGetValue(blueprintType ?? "", out var count) ? count : 0;
            }
        }

        public void InvalidateBlueprintTypeCountCache() { lock (_listLock) { _blueprintTypeCountCache = null; } }

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

        public void InvalidateBuildItemIndexes() { lock (_listLock) { _blueprintBuildItemIndex = null; _buildLocationBuildItemIndex = null; } }

        private void RebuildBuildItemIndexes()
        {
            _blueprintBuildItemIndex = new Dictionary<string, List<BuildItem>>();
            _buildLocationBuildItemIndex = new Dictionary<string, List<BuildItem>>();
            foreach (var plan in BuildPlanList)
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

        /// <summary>
        /// Returns a snapshot of ColonyList for safe iteration outside the lock.
        /// </summary>
        public List<Colony> SnapshotColonyList()
        {
            lock (_listLock)
            {
                return new List<Colony>(ColonyList);
            }
        }

        /// <summary>
        /// Returns a snapshot of BuildPlanList for safe iteration outside the lock.
        /// </summary>
        public List<BuildPlan> SnapshotBuildPlanList()
        {
            lock (_listLock) { return new List<BuildPlan>(BuildPlanList); }
        }

        /// <summary>
        /// Returns a snapshot of ShipTemplateList for safe iteration outside the lock.
        /// </summary>
        public List<ShipTemplate> SnapshotShipTemplateList()
        {
            lock (_listLock) { return new List<ShipTemplate>(ShipTemplateList); }
        }

        /// <summary>
        /// Returns a snapshot of ShipList for safe iteration outside the lock.
        /// </summary>
        public List<Ship> SnapshotShipList()
        {
            lock (_listLock) { return new List<Ship>(ShipList); }
        }

        /// <summary>
        /// Returns a snapshot of StationList for safe iteration outside the lock.
        /// </summary>
        public List<Station> SnapshotStationList()
        {
            lock (_listLock) { return new List<Station>(StationList); }
        }

        /// <summary>
        /// Returns a snapshot of MarketListingList for safe iteration outside the lock.
        /// </summary>
        public List<MarketListing> SnapshotMarketListingList()
        {
            lock (_listLock) { return new List<MarketListing>(MarketListingList); }
        }

        /// <summary>
        /// Returns a snapshot of MarketTransactionList for safe iteration outside the lock.
        /// </summary>
        public List<MarketTransaction> SnapshotMarketTransactionList()
        {
            lock (_listLock) { return new List<MarketTransaction>(MarketTransactionList); }
        }

        /// <summary>
        /// Returns a snapshot of StockPlanList for safe iteration outside the lock.
        /// </summary>
        public List<StockPlan> SnapshotStockPlanList()
        {
            lock (_listLock) { return new List<StockPlan>(StockPlanList); }
        }

        /// <summary>
        /// Returns a snapshot of StockProfileList for safe iteration outside the lock.
        /// </summary>
        public List<StockProfile> SnapshotStockProfileList()
        {
            lock (_listLock) { return new List<StockProfile>(StockProfileList); }
        }

        /// <summary>
        /// Returns a snapshot of SupplyChainList for safe iteration outside the lock.
        /// </summary>
        public List<SupplyChain> SnapshotSupplyChainList()
        {
            lock (_listLock) { return new List<SupplyChain>(SupplyChainList); }
        }

        /// <summary>
        /// Returns a snapshot of WarehouseOverflowRuleList for safe iteration outside the lock.
        /// </summary>
        public List<WarehouseOverflowRule> SnapshotWarehouseOverflowRuleList()
        {
            lock (_listLock) { return new List<WarehouseOverflowRule>(WarehouseOverflowRuleList); }
        }

        /// <summary>
        /// Returns a snapshot of FactionList for safe iteration outside the lock.
        /// </summary>
        public List<Faction> SnapshotFactionList()
        {
            lock (_listLock) { return new List<Faction>(FactionList); }
        }

        /// <summary>
        /// Returns a snapshot of ExternalCharacterList for safe iteration outside the lock.
        /// </summary>
        public List<ExternalCharacter> SnapshotExternalCharacterList()
        {
            lock (_listLock) { return new List<ExternalCharacter>(ExternalCharacterList); }
        }

        /// <summary>
        /// Returns a snapshot of AsteroidList for safe iteration outside the lock.
        /// </summary>
        public List<Asteroid> SnapshotAsteroidList()
        {
            lock (_listLock) { return new List<Asteroid>(AsteroidList); }
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
            if (PlayerProfileList.Count == 0) return;

            string firstPlayerUUID = PlayerProfileList[0].UUID;
            if (string.IsNullOrEmpty(firstPlayerUUID)) return;

            int migrated = 0;
            foreach (var colony in ColonyList)
            {
                if (string.IsNullOrEmpty(colony.OwnerUUID))
                {
                    colony.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }
            foreach (var blueprint in BlueprintList)
            {
                if (string.IsNullOrEmpty(blueprint.OwnerUUID))
                {
                    blueprint.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }
            foreach (var survey in SurveyList)
            {
                if (string.IsNullOrEmpty(survey.OwnerUUID))
                {
                    survey.OwnerUUID = firstPlayerUUID;
                    migrated++;
                }
            }

            if (migrated > 0)
            {
                Log.Info("Migrated {0} items to player {1}", migrated, PlayerProfileList[0].Name);
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
            foreach (var colony in ColonyList.Where(c => c.OwnerUUID == playerUUID).ToList())
            { ColonyList.Remove(colony); removed++; }
            foreach (var bp in BlueprintList.Where(b => b.OwnerUUID == playerUUID).ToList())
            { BlueprintList.Remove(bp); removed++; }
            foreach (var survey in SurveyList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { SurveyList.Remove(survey); removed++; }
            foreach (var route in DeliveryRouteList.Where(r => r.OwnerUUID == playerUUID).ToList())
            { DeliveryRouteList.Remove(route); removed++; }
            foreach (var plan in DeliveryPlanList.Where(p => p.OwnerUUID == playerUUID).ToList())
            { DeliveryPlanList.Remove(plan); removed++; }
            foreach (var pp in PricingPlanList.Where(p => p.OwnerUUID == playerUUID).ToList())
            { PricingPlanList.Remove(pp); removed++; }
            foreach (var bp2 in BuildPlanList.Where(b => b.OwnerUUID == playerUUID).ToList())
            { BuildPlanList.Remove(bp2); removed++; }
            foreach (var st in ShipTemplateList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { ShipTemplateList.Remove(st); removed++; }
            foreach (var ship in ShipList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { ShipList.Remove(ship); removed++; }
            foreach (var station in StationList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { StationList.Remove(station); removed++; }
            foreach (var ml in MarketListingList.Where(m => m.OwnerUUID == playerUUID).ToList())
            { MarketListingList.Remove(ml); removed++; }
            foreach (var mt in MarketTransactionList.Where(m => m.OwnerUUID == playerUUID).ToList())
            { MarketTransactionList.Remove(mt); removed++; }
            foreach (var sp in StockPlanList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { StockPlanList.Remove(sp); removed++; }
            foreach (var spf in StockProfileList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { StockProfileList.Remove(spf); removed++; }
            foreach (var sc in SupplyChainList.Where(s => s.OwnerUUID == playerUUID).ToList())
            { SupplyChainList.Remove(sc); removed++; }
            foreach (var wor in WarehouseOverflowRuleList.Where(w => w.OwnerUUID == playerUUID).ToList())
            { WarehouseOverflowRuleList.Remove(wor); removed++; }

            if (removed > 0)
                Log.Info("Cascade deleted {0} items for player {1}", removed, playerUUID);

            InvalidateBlueprintCache();
            InvalidateSurveyCache();
            InvalidateColonyCache();
        }

        /// <summary>
        /// Removes data owned by players that no longer exist.
        /// Called on load after all lists are initialized.
        /// </summary>
        private void CleanupOrphanedData()
        {
            var validUUIDs = new HashSet<string>(PlayerProfileList.Select(p => p.UUID));
            int removed = 0;

            foreach (var colony in ColonyList.Where(c => !string.IsNullOrEmpty(c.OwnerUUID) && !validUUIDs.Contains(c.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned colony: {0} ({1}) owner={2}", colony.PlanetName, colony.ColonyName, colony.OwnerUUID); ColonyList.Remove(colony); removed++; }
            foreach (var bp in BlueprintList.Where(b => !string.IsNullOrEmpty(b.OwnerUUID) && !validUUIDs.Contains(b.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned blueprint: {0} owner={1}", bp.ExtendedName, bp.OwnerUUID); BlueprintList.Remove(bp); removed++; }
            foreach (var survey in SurveyList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned survey: {0} owner={1}", survey.ExtendedName, survey.OwnerUUID); SurveyList.Remove(survey); removed++; }
            foreach (var route in DeliveryRouteList.Where(r => !string.IsNullOrEmpty(r.OwnerUUID) && !validUUIDs.Contains(r.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned route: {0} owner={1}", route.Name, route.OwnerUUID); DeliveryRouteList.Remove(route); removed++; }
            foreach (var plan in DeliveryPlanList.Where(p => !string.IsNullOrEmpty(p.OwnerUUID) && !validUUIDs.Contains(p.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned delivery plan: {0} owner={1}", plan.Name, plan.OwnerUUID); DeliveryPlanList.Remove(plan); removed++; }
            foreach (var pp in PricingPlanList.Where(p => !string.IsNullOrEmpty(p.OwnerUUID) && !validUUIDs.Contains(p.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned pricing plan: {0} owner={1}", pp.Name, pp.OwnerUUID); PricingPlanList.Remove(pp); removed++; }
            foreach (var bp2 in BuildPlanList.Where(b => !string.IsNullOrEmpty(b.OwnerUUID) && !validUUIDs.Contains(b.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned build plan: {0} owner={1}", bp2.Name, bp2.OwnerUUID); BuildPlanList.Remove(bp2); removed++; }
            foreach (var st in ShipTemplateList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned ship template: {0} owner={1}", st.Name, st.OwnerUUID); ShipTemplateList.Remove(st); removed++; }
            foreach (var ship in ShipList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned ship: {0} owner={1}", ship.Name, ship.OwnerUUID); ShipList.Remove(ship); removed++; }
            foreach (var station in StationList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned station: {0} owner={1}", station.Name, station.OwnerUUID); StationList.Remove(station); removed++; }
            foreach (var ml in MarketListingList.Where(m => !string.IsNullOrEmpty(m.OwnerUUID) && !validUUIDs.Contains(m.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned market listing: {0} owner={1}", ml.UUID, ml.OwnerUUID); MarketListingList.Remove(ml); removed++; }
            foreach (var mt in MarketTransactionList.Where(m => !string.IsNullOrEmpty(m.OwnerUUID) && !validUUIDs.Contains(m.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned market transaction: {0} owner={1}", mt.UUID, mt.OwnerUUID); MarketTransactionList.Remove(mt); removed++; }
            foreach (var sp in StockPlanList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned stock plan: {0} owner={1}", sp.Name, sp.OwnerUUID); StockPlanList.Remove(sp); removed++; }
            foreach (var spf in StockProfileList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned stock profile: {0} owner={1}", spf.Name, spf.OwnerUUID); StockProfileList.Remove(spf); removed++; }
            foreach (var sc in SupplyChainList.Where(s => !string.IsNullOrEmpty(s.OwnerUUID) && !validUUIDs.Contains(s.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned supply chain: {0} owner={1}", sc.Name, sc.OwnerUUID); SupplyChainList.Remove(sc); removed++; }
            foreach (var wor in WarehouseOverflowRuleList.Where(w => !string.IsNullOrEmpty(w.OwnerUUID) && !validUUIDs.Contains(w.OwnerUUID)).ToList())
            { Log.Warn("Removing orphaned overflow rule: {0} owner={1}", wor.UUID, wor.OwnerUUID); WarehouseOverflowRuleList.Remove(wor); removed++; }

            if (removed > 0)
                Log.Info("Cleaned up {0} orphaned items on load", removed);
        }

        /// <summary>
        /// Restores the current player from the saved UUID, falling back to
        /// the first player if the saved UUID is invalid or empty.
        /// Does not fire CurrentPlayerChanged (called during construction).
        /// </summary>
        private void RestoreCurrentPlayer(string savedUUID)
        {
            if (!string.IsNullOrEmpty(savedUUID) &&
                PlayerProfileList.Any(p => p.UUID == savedUUID))
            {
                _currentPlayerUUID = savedUUID;
            }
            else if (PlayerProfileList.Count > 0)
            {
                _currentPlayerUUID = PlayerProfileList[0].UUID ?? string.Empty;
            }
            Log.Info("Current player restored: {0}", _currentPlayerUUID);
        }

        /// <summary>
        /// Returns colonies owned by the current player.
        /// </summary>
        public List<Colony> GetCurrentPlayerColonies()
        {
            return ColonyList.Where(c => c.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns blueprints owned by the current player.
        /// </summary>
        public List<Blueprint> GetCurrentPlayerBlueprints()
        {
            return BlueprintList.Where(b => b.OwnerUUID == _currentPlayerUUID).ToList();
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
                _allBlueprintsCache = new List<Blueprint>(BlueprintList);
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
            return SurveyList.Where(s => s.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns delivery routes owned by the current player.
        /// </summary>
        public List<DeliveryRoute> GetCurrentPlayerRoutes()
        {
            return DeliveryRouteList.Where(r => r.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns delivery plans owned by the current player.
        /// </summary>
        public List<DeliveryPlan> GetCurrentPlayerPlans()
        {
            return DeliveryPlanList.Where(p => p.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns pricing plans owned by the current player.
        /// </summary>
        public List<PricingPlan> GetCurrentPlayerPricingPlans()
        {
            return PricingPlanList.Where(p => p.OwnerUUID == _currentPlayerUUID).ToList();
        }

        /// <summary>
        /// Returns build plans owned by the current player.
        /// </summary>
        public List<BuildPlan> GetCurrentPlayerBuildPlans()
        {
            lock (_listLock) { return BuildPlanList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns ship templates owned by the current player.
        /// </summary>
        public List<ShipTemplate> GetCurrentPlayerShipTemplates()
        {
            lock (_listLock) { return ShipTemplateList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns ships owned by the current player.
        /// </summary>
        public List<Ship> GetCurrentPlayerShips()
        {
            lock (_listLock) { return ShipList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns stations relevant to the current player. Includes government stations
        /// (shared infrastructure) and player-owned stations.
        /// </summary>
        public List<Station> GetCurrentPlayerStations()
        {
            lock (_listLock)
            {
                return StationList.Where(x =>
                    x.Ownership == StationOwnership.Government ||
                    x.OwnerUUID == CurrentPlayerUUID).ToList();
            }
        }

        /// <summary>
        /// Returns market listings owned by the current player.
        /// </summary>
        public List<MarketListing> GetCurrentPlayerListings()
        {
            lock (_listLock) { return MarketListingList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns market transactions owned by the current player.
        /// </summary>
        public List<MarketTransaction> GetCurrentPlayerTransactions()
        {
            lock (_listLock) { return MarketTransactionList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns stock plans owned by the current player.
        /// </summary>
        public List<StockPlan> GetCurrentPlayerStockPlans()
        {
            lock (_listLock) { return StockPlanList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns stock profiles owned by the current player.
        /// </summary>
        public List<StockProfile> GetCurrentPlayerStockProfiles()
        {
            lock (_listLock) { return StockProfileList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns supply chains owned by the current player.
        /// </summary>
        public List<SupplyChain> GetCurrentPlayerSupplyChains()
        {
            lock (_listLock) { return SupplyChainList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }

        /// <summary>
        /// Returns warehouse overflow rules owned by the current player.
        /// </summary>
        public List<WarehouseOverflowRule> GetCurrentPlayerOverflowRules()
        {
            lock (_listLock) { return WarehouseOverflowRuleList.Where(x => x.OwnerUUID == CurrentPlayerUUID).ToList(); }
        }


        public List<CountDownTimeReference> AllCountdownSources()
        {
            List<CountDownTimeReference> countdowns = new List<CountDownTimeReference>();
            foreach (var player in PlayerProfileList)
            {
                if (player.Skills != null)
                {
                    foreach (var skill in player.Skills)
                    {
                        if (skill.Value.CompletionTime != null && skill.Value.CompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.source = CountDownTimeReference.SourceType.Player;
                            reference.sourceUUID = player.UUID;
                            reference.internalUUID = skill.Key;
                            reference.countDownTime = skill.Value.CompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }
            foreach (var colony in ColonyList)
            {
                if (colony.Structures != null)
                {
                    foreach (var structure in colony.Structures)
                    {
                        if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.TimeRemaining > 0)
                        {
                            CountDownTimeReference reference = new CountDownTimeReference();
                            reference.source = CountDownTimeReference.SourceType.Colony;
                            reference.sourceUUID = colony.UUID;
                            reference.internalUUID = structure.UUID;
                            reference.countDownTime = structure.ProcessCompletionTime;
                            countdowns.Add(reference);
                        }
                    }
                }
            }
            return countdowns;
        }
    }

    public class PlayerRoot
    {
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
    }

    public class CountDownTimeReference
    {
        public enum SourceType
        {
            None,
            Player,
            Colony
        }

        public SourceType source { get; set; }
        public string sourceUUID { get; set; }
        public string internalUUID { get; set; }
        public CountDownTime countDownTime { get; set; }
    }

    public class ColonyDataChangedEventArgs : EventArgs
    {
        public string ColonyUUID { get; }
        public ColonyDataChangedEventArgs(string colonyUUID) { ColonyUUID = colonyUUID; }
    }

    public class BlueprintDataChangedEventArgs : EventArgs
    {
        public string BlueprintUUID { get; }
        public BlueprintDataChangedEventArgs(string blueprintUUID) { BlueprintUUID = blueprintUUID; }
    }

    public class SurveyDataChangedEventArgs : EventArgs
    {
        public string SurveyUUID { get; }
        public SurveyDataChangedEventArgs(string surveyUUID) { SurveyUUID = surveyUUID; }
    }

    public class PlayerProfileDataChangedEventArgs : EventArgs
    {
        public string PlayerUUID { get; }
        public PlayerProfileDataChangedEventArgs(string playerUUID) { PlayerUUID = playerUUID; }
    }

    public class BuildPlanDataChangedEventArgs : EventArgs
    {
        public string BuildPlanUUID { get; }
        public BuildPlanDataChangedEventArgs(string uuid) { BuildPlanUUID = uuid; }
    }

}
