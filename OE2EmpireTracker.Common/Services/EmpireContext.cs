using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;

namespace OE2EmpireTracker.Services
{
    public class EmpireContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static EmpireContext _instance;

        private readonly object _commodityLock = new object();

        // Task 6.1: Private backing fields with IReadOnlyList<T> properties
        private List<BlueprintType> _blueprintTypeList;

        private List<ShipClass> _shipClassList;

        private List<TechLevel> _techLevelList;

        private List<string> _evolutionList;

        private List<Resource> _resourceList;

        private List<ResourceGroup> _resourceGroupList;

        private List<ResourcePurity> _resourcePurityList;

        private List<Blueprint> _globalBlueprintList;

        private Dictionary<string, Blueprint> _globalBlueprintCache;

        private List<Commodity> _commodityList;

        private Dictionary<string, Commodity> _commodityNameCache;

        private List<PropertyTypeDefinition> _propertyTypeRegistry = new List<PropertyTypeDefinition>();

        private Dictionary<int, PropertyTypeDefinition> _propertyTypeCache;

        private SystemRepository _systemRepository;

        /// <summary>
        /// Internal constructor for test infrastructure. Accepts pre-parsed
        /// BaselineRoot and PlayerRoot so tests can skip disk I/O.
        /// </summary>
        internal EmpireContext(BaselineRoot baselineRoot, PlayerRoot playerRoot) : base()
        {
            _instance = this;

            // Create PlayerContext from pre-parsed PlayerRoot
            PlayerContext = new PlayerContext(playerRoot);

            DataVersion = baselineRoot.DataVersion;
            GameConstants = baselineRoot.GameConstants ?? new BaselineGameConstants();

            InitBlueprintTypes(baselineRoot);
            InitShipClasses(baselineRoot);
            InitTechLevels(baselineRoot);
            InitEvolutions(baselineRoot);
            InitResources(baselineRoot);
            InitResourceGroups(baselineRoot);
            InitResourcePurities(baselineRoot);
            InitCommodities(baselineRoot);
            InitRefiningRecipes(baselineRoot);
            InitResearchTimes(baselineRoot);
            InitGlobalBlueprints(baselineRoot);
            InitPropertyTypes(baselineRoot);

            // Wire up static dependencies for Common-portable models
            Constants.GameConstants.SetGameConfig(GameConstants);
            WireDisplayNameResolver();

            _systemRepository = new SystemRepository();
            _systemRepository.Load(SystemRepository.FilePath);

            // Run migrations via delegate (set by host application)
            RunMigrationsIfConfigured();
        }

        private EmpireContext() : base()
        {
            _instance = this;
            PlayerContext = PlayerContext.GetInstance();

            Log.Info("Loading baseline data from {0}", FilePath);
            string jsonContent = File.ReadAllText(FilePath);
            BaselineRoot baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(jsonContent);
            Log.Info(
                "Baseline data loaded: {0} blueprint types, {1} ship classes, {2} tech levels",
                baselineRoot.BlueprintType?.Length ?? 0,
                baselineRoot.ShipClass?.Length ?? 0,
                baselineRoot.TechLevel?.Length ?? 0);

            DataVersion = baselineRoot.DataVersion;
            GameConstants = baselineRoot.GameConstants ?? new BaselineGameConstants();

            InitBlueprintTypes(baselineRoot);
            InitShipClasses(baselineRoot);
            InitTechLevels(baselineRoot);
            InitEvolutions(baselineRoot);
            InitResources(baselineRoot);
            InitResourceGroups(baselineRoot);
            InitResourcePurities(baselineRoot);
            InitCommodities(baselineRoot);
            InitRefiningRecipes(baselineRoot);
            InitResearchTimes(baselineRoot);
            InitGlobalBlueprints(baselineRoot);
            InitPropertyTypes(baselineRoot);

            // Wire up static dependencies for Common-portable models
            Constants.GameConstants.SetGameConfig(GameConstants);
            WireDisplayNameResolver();

            _systemRepository = new SystemRepository();
            _systemRepository.Load(SystemRepository.FilePath);

            // Run migrations via delegate (set by host application)
            RunMigrationsIfConfigured();
        }

        public static string FilePath { get; set; } = "BaselineData.json";

        public static PlayerContext PlayerContext { get; set; }

        /// <summary>
        /// Delegate that runs data migrations. Set by the host application (WinForms)
        /// before creating the EmpireContext instance.
        /// Parameters: EmpireContext, PlayerContext.
        /// </summary>
        public static Action<EmpireContext, PlayerContext> RunMigrations { get; set; }

        /// <summary>
        /// Delegate that fixes up blueprint properties (e.g. flatpack properties).
        /// Set by the host application where parser logic is available.
        /// </summary>
        public static Action<Blueprint> FixupBlueprintProperties { get; set; }

        public IReadOnlyList<BlueprintType> BlueprintTypeList => _blueprintTypeList;

        public IReadOnlyList<ShipClass> ShipClassList => _shipClassList;

        public IReadOnlyList<TechLevel> TechLevelList => _techLevelList;

        public IReadOnlyList<string> EvolutionList => _evolutionList;

        public IReadOnlyList<Resource> ResourceList => _resourceList;

        public IReadOnlyList<ResourceGroup> ResourceGroupList => _resourceGroupList;

        public IReadOnlyList<ResourcePurity> ResourcePurityList => _resourcePurityList;

        public int DataVersion { get; set; } = 0;

        public BaselineGameConstants GameConstants { get; set; }

        public IReadOnlyList<Blueprint> GlobalBlueprintList => _globalBlueprintList;

        public IReadOnlyList<Commodity> CommodityList => _commodityList;

        public IReadOnlyList<PropertyTypeDefinition> PropertyTypeRegistry => _propertyTypeRegistry;

        public SystemRepository SystemRepository => _systemRepository;

        /// <summary>
        /// The storage backend used for baseline data persistence.
        /// When null, falls back to direct file I/O using FilePath.
        /// </summary>
        public IStorageBackend StorageBackend { get; set; }

        /// <summary>
        /// The backend type, used to select load/save strategy without runtime type checks.
        /// </summary>
        public StorageBackendType? StorageBackendType { get; set; }

        public static EmpireContext GetInstance()
        {
            if (_instance == null)
            {
                _instance = new EmpireContext();
            }

            return _instance;
        }

        /// <summary>
        /// Returns the current instance without creating one if it doesn't exist.
        /// Used by GameConstants to avoid triggering file I/O during early access.
        /// </summary>
        public static EmpireContext GetInstanceIfLoaded()
        {
            return _instance;
        }

        public static void Reset()
        {
            _instance = null;
            PlayerContext = null;
            OE2EmpireTracker.Services.PlayerContext.Reset();
        }

        public void WriteContext()
        {
            if (PlayerContext.WritesBlocked)
            {
                Log.Warn("WriteContext blocked -- migration failed, saving disabled");
                return;
            }

            BaselineRoot baselineRoot = new BaselineRoot();
            baselineRoot.DataVersion = DataVersion;
            baselineRoot.GameConstants = GameConstants;
            baselineRoot.ShipClass = _shipClassList.ToArray();
            baselineRoot.BlueprintType = _blueprintTypeList.ToArray();
            baselineRoot.Blueprint = _globalBlueprintList.ToArray();
            baselineRoot.TechLevel = _techLevelList.ToArray();
            baselineRoot.Commodity = _commodityList?.ToArray();
            baselineRoot.RefiningRecipe = new List<RefiningRecipe>(RefiningRecipes.Recipes).ToArray();
            baselineRoot.ResearchTime = new List<ResearchTimeEntry>(ResearchTimeLookup.ResearchTimes).ToArray();
            baselineRoot.PropertyType = _propertyTypeRegistry.ToArray();

            baselineRoot = SerializationSorter.SortBaselineRoot(baselineRoot);

            // Integrity check: validate all global blueprints before saving
            foreach (var bp in baselineRoot.Blueprint ?? Array.Empty<Blueprint>())
            {
                string error = bp.ValidateIntegrity();
                if (error != null)
                {
                    Log.Error("BLUEPRINT INTEGRITY VIOLATION in WriteContext (global): {0}", error);
                }
            }

            string jsonContent = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
            SafeFileWriter.WriteAllText(FilePath, jsonContent);
            Log.Info("Baseline data saved to {0}", FilePath);
        }

        public void InitBlueprintTypes(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderByName(baselineRoot.BlueprintType, x => x.Name);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var deduped = new List<BlueprintType>();
            foreach (var bt in sorted)
            {
                if (string.IsNullOrEmpty(bt.Id) || seen.Add(bt.Id))
                {
                    deduped.Add(bt);
                }
                else
                {
                    Log.Error("DUPLICATE Id on load: BlueprintType Id='{0}' Name='{1}' â€” skipping duplicate", bt.Id, bt.Name);
                }
            }

            _blueprintTypeList = deduped;
        }

        public BlueprintType FindBlueprintType(string id)
        {
            var filteredList = BlueprintTypeList
                .Where(item => item.Id == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }

            return null;
        }

        /// <summary>
        /// Finds a BlueprintType by its icon sprite position (e.g. "-328px -62px").
        /// Returns null if no type matches.
        /// </summary>
        public BlueprintType FindBlueprintTypeByIcon(string iconPosition)
        {
            if (string.IsNullOrEmpty(iconPosition)) return null;
            return BlueprintTypeList.FirstOrDefault(bt =>
                string.Equals(bt.IconPosition, iconPosition, StringComparison.Ordinal));
        }

        public void InitShipClasses(BaselineRoot baselineRoot)
        {
            _shipClassList = new List<ShipClass>(baselineRoot.ShipClass);
        }

        public ShipClass FindShipClass(int id)
        {
            var filteredList = ShipClassList
                .Where(item => item.Id == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }

            return null;
        }

        public void InitTechLevels(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderByName(baselineRoot.TechLevel, x => x.Name);
            _techLevelList = new List<TechLevel>(sorted);
        }

        public TechLevel FindTechLevel(string id)
        {
            var filteredList = TechLevelList
                .Where(item => item.Name == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }

            return null;
        }

        public void InitEvolutions(BaselineRoot baselineRoot)
        {
            List<string> list = new List<string>();
            for (int evo = 0; evo <= 15; evo++)
            {
                list.Add(evo.ToString());
            }

            _evolutionList = new List<string>(list);
        }

        public string FindEvolution(int id)
        {
            string key = string.Empty + id;
            var filteredList = EvolutionList
                .Where(item => item == key)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }

            return null;
        }

        public void InitResources(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderByName(Resource.Resources, x => x.Name);
            _resourceList = new List<Resource>(sorted);
        }

        public void InitResourceGroups(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderByName(ResourceGroup.Groups, x => x.Name);
            _resourceGroupList = new List<ResourceGroup>(sorted);
        }

        public void InitResourcePurities(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderByName(ResourcePurity.Purities, x => x.Name);
            _resourcePurityList = new List<ResourcePurity>(sorted);
        }

        public void InitCommodities(BaselineRoot baselineRoot)
        {
            if (baselineRoot.Commodity != null && baselineRoot.Commodity.Length > 0)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var deduped = new List<Commodity>();
                foreach (var c in baselineRoot.Commodity)
                {
                    if (string.IsNullOrEmpty(c.Name) || seen.Add(c.Name))
                    {
                        deduped.Add(c);
                    }
                    else
                    {
                        Log.Error("DUPLICATE Name on load: Commodity Name='{0}' â€” skipping duplicate", c.Name);
                    }
                }

                _commodityList = deduped;
                Commodity.SetCommodities(_commodityList);
                Log.Info("Loaded {0} commodities from baseline data", _commodityList.Count);
            }
            else
            {
                _commodityList = new List<Commodity>(Commodity.Commodities);
                Log.Info("Using hardcoded commodity list ({0} commodities)", _commodityList.Count);
            }
        }

        public void InitRefiningRecipes(BaselineRoot baselineRoot)
        {
            if (baselineRoot.RefiningRecipe != null && baselineRoot.RefiningRecipe.Length > 0)
            {
                var recipes = new List<RefiningRecipe>(baselineRoot.RefiningRecipe);
                RefiningRecipes.SetRecipes(recipes);
                Log.Info("Loaded {0} refining recipes from baseline data", recipes.Count);
            }
            else
            {
                Log.Info("Using hardcoded refining recipe list ({0} recipes)", RefiningRecipes.Recipes.Count);
            }
        }

        public void InitResearchTimes(BaselineRoot baselineRoot)
        {
            if (baselineRoot.ResearchTime != null && baselineRoot.ResearchTime.Length > 0)
            {
                var entries = new List<ResearchTimeEntry>(baselineRoot.ResearchTime);
                ResearchTimeLookup.SetResearchTimes(entries);
                Log.Info("Loaded {0} research time entries from baseline data", entries.Count);
            }
            else
            {
                Log.Info("Using hardcoded research time lookup ({0} entries)", ResearchTimeLookup.GetFallbackResearchTimes().Count);
            }
        }

        public void InitGlobalBlueprints(BaselineRoot baselineRoot)
        {
            var sorted = CollectionSortHelper.OrderBlueprints(baselineRoot.Blueprint ?? new Blueprint[0]);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var deduped = new List<Blueprint>();
            foreach (var bp in sorted)
            {
                if (string.IsNullOrEmpty(bp.UUID) || seen.Add(bp.UUID))
                {
                    deduped.Add(bp);
                }
                else
                {
                    Log.Error("DUPLICATE UUID on load: GlobalBlueprint UUID={0} Name='{1}' â€” skipping duplicate", bp.UUID, bp.Name);
                }
            }

            _globalBlueprintList = deduped;
            InvalidateGlobalBlueprintCache();

            // Fix up game data quirks on existing blueprints
            // (e.g. Reactor "Power Required" â†’ "Power Provided")
            foreach (var bp in _globalBlueprintList)
            {
                FixupBlueprintProperties?.Invoke(bp);
            }

            Log.Info("Loaded {0} global blueprints", _globalBlueprintList.Count);
        }

        /// <summary>
        /// Searches global blueprints by UUID using a dictionary cache for O(1) lookup.
        /// Returns a ReadOnlyBlueprint wrapper.
        /// </summary>
        public ReadOnlyBlueprint FindGlobalBlueprint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_globalBlueprintList == null) return null;

            if (_globalBlueprintCache == null)
            {
                _globalBlueprintCache = new Dictionary<string, Blueprint>();
                foreach (var bp in _globalBlueprintList)
                {
                    if (bp.UUID != null && !_globalBlueprintCache.ContainsKey(bp.UUID))
                        _globalBlueprintCache[bp.UUID] = bp;
                }
            }

            if (_globalBlueprintCache.TryGetValue(id, out var match))
                return new ReadOnlyBlueprint(match);

            return null;
        }

        public void InvalidateGlobalBlueprintCache()
        {
            _globalBlueprintCache = null;
        }

        /// <summary>
        /// Finds a Commodity by name (case-insensitive) using a dictionary cache for O(1) lookup.
        /// </summary>
        public Commodity FindCommodity(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            lock (_commodityLock)
            {
                if (_commodityNameCache == null)
                {
                    _commodityNameCache = new Dictionary<string, Commodity>(StringComparer.OrdinalIgnoreCase);
                    if (_commodityList != null)
                    {
                        foreach (var c in _commodityList)
                        {
                            if (!string.IsNullOrEmpty(c.Name) && !_commodityNameCache.ContainsKey(c.Name))
                            {
                                _commodityNameCache[c.Name] = c;
                            }
                        }
                    }
                }

                _commodityNameCache.TryGetValue(name, out var match);
                return match;
            }
        }

        public void InvalidateCommodityNameCache()
        {
            lock (_commodityLock)
            {
                _commodityNameCache = null;
            }
        }

        // â”€â”€ Task 6.2: Mutation methods for GlobalBlueprint (UUID cache) â”€â”€

        /// <summary>
        /// Finds a PropertyTypeDefinition by its ModTypeId using a dictionary cache for O(1) lookup.
        /// Returns null if no definition matches.
        /// </summary>
        public PropertyTypeDefinition FindPropertyType(int modTypeId)
        {
            if (_propertyTypeCache == null)
            {
                _propertyTypeCache = _propertyTypeRegistry.ToDictionary(p => p.ModTypeId);
            }

            _propertyTypeCache.TryGetValue(modTypeId, out var result);
            return result;
        }

        /// <summary>
        /// Creates or updates a PropertyTypeDefinition in the registry keyed by ModTypeId.
        /// If a definition with the same ModTypeId exists, its metadata fields are updated.
        /// Otherwise, the definition is added to the registry.
        /// </summary>
        public void UpsertPropertyType(PropertyTypeDefinition definition)
        {
            if (_propertyTypeCache == null)
            {
                _propertyTypeCache = _propertyTypeRegistry.ToDictionary(p => p.ModTypeId);
            }

            if (_propertyTypeCache.TryGetValue(definition.ModTypeId, out var existing))
            {
                existing.PropertyName = definition.PropertyName;
                existing.FriendlyPropertyName = definition.FriendlyPropertyName;
                existing.Unit = definition.Unit;
                existing.ResearchPositive = definition.ResearchPositive;
                existing.CanResearch = definition.CanResearch;
            }
            else
            {
                _propertyTypeRegistry.Add(definition);
                _propertyTypeCache[definition.ModTypeId] = definition;
            }
        }

        public void AddGlobalBlueprint(Blueprint item)
        {
            if (!string.IsNullOrEmpty(item.UUID))
            {
                if (_globalBlueprintCache != null)
                {
                    if (_globalBlueprintCache.ContainsKey(item.UUID))
                    {
                        throw new InvalidOperationException(
                            string.Format("Duplicate UUID in GlobalBlueprint collection: {0} (Name: {1})", item.UUID, item.Name));
                    }
                }
                else if (_globalBlueprintList.Any(x => x.UUID == item.UUID))
                {
                    throw new InvalidOperationException(
                        string.Format("Duplicate UUID in GlobalBlueprint collection: {0} (Name: {1})", item.UUID, item.Name));
                }
            }

            _globalBlueprintList.Add(item);
            if (_globalBlueprintCache != null && item.UUID != null)
                _globalBlueprintCache[item.UUID] = item;
        }

        public void RemoveGlobalBlueprint(Blueprint item)
        {
            _globalBlueprintList.Remove(item);
            if (_globalBlueprintCache != null && item.UUID != null)
                _globalBlueprintCache.Remove(item.UUID);
        }

        // â”€â”€ Task 6.3: Mutation methods for Commodity (name cache with _commodityLock) â”€â”€

        public void AddCommodity(Commodity item)
        {
            lock (_commodityLock)
            {
                if (!string.IsNullOrEmpty(item.Name))
                {
                    if (_commodityNameCache != null)
                    {
                        if (_commodityNameCache.ContainsKey(item.Name))
                        {
                            throw new InvalidOperationException(
                                string.Format("Duplicate Name in Commodity collection: {0}", item.Name));
                        }
                    }
                    else if (_commodityList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
                    {
                        throw new InvalidOperationException(
                            string.Format("Duplicate Name in Commodity collection: {0}", item.Name));
                    }
                }

                _commodityList.Add(item);
                if (_commodityNameCache != null && !string.IsNullOrEmpty(item.Name))
                    _commodityNameCache[item.Name] = item;
            }
        }

        public void RemoveCommodity(Commodity item)
        {
            lock (_commodityLock)
            {
                _commodityList.Remove(item);
                if (_commodityNameCache != null && !string.IsNullOrEmpty(item.Name))
                    _commodityNameCache.Remove(item.Name);
            }
        }

        // â”€â”€ Task 6.4: Mutation methods for lookup lists â”€â”€

        public void AddBlueprintType(BlueprintType item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _blueprintTypeList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in BlueprintType collection: {0}", item.Name));
            }

            _blueprintTypeList.Add(item);
        }

        public void RemoveBlueprintType(BlueprintType item)
        {
            _blueprintTypeList.Remove(item);
        }

        public void AddShipClass(ShipClass item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _shipClassList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in ShipClass collection: {0}", item.Name));
            }

            _shipClassList.Add(item);
        }

        public void RemoveShipClass(ShipClass item)
        {
            _shipClassList.Remove(item);
        }

        public void AddTechLevel(TechLevel item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _techLevelList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in TechLevel collection: {0}", item.Name));
            }

            _techLevelList.Add(item);
        }

        public void RemoveTechLevel(TechLevel item)
        {
            _techLevelList.Remove(item);
        }

        public void AddEvolution(string item)
        {
            _evolutionList.Add(item);
        }

        public void RemoveEvolution(string item)
        {
            _evolutionList.Remove(item);
        }

        public void AddResource(Resource item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _resourceList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in Resource collection: {0}", item.Name));
            }

            _resourceList.Add(item);
        }

        public void RemoveResource(Resource item)
        {
            _resourceList.Remove(item);
        }

        public void AddResourceGroup(ResourceGroup item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _resourceGroupList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in ResourceGroup collection: {0}", item.Name));
            }

            _resourceGroupList.Add(item);
        }

        public void RemoveResourceGroup(ResourceGroup item)
        {
            _resourceGroupList.Remove(item);
        }

        public void AddResourcePurity(ResourcePurity item)
        {
            if (!string.IsNullOrEmpty(item.Name) && _resourcePurityList.Any(x => string.Equals(x.Name, item.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    string.Format("Duplicate Name in ResourcePurity collection: {0}", item.Name));
            }

            _resourcePurityList.Add(item);
        }

        public void RemoveResourcePurity(ResourcePurity item)
        {
            _resourcePurityList.Remove(item);
        }

        // -- Task 6.1: GetReadOnly and FindReadOnly methods --

        public IReadOnlyList<ReadOnlyCommodity> GetReadOnlyCommodityList()
        {
            lock (_commodityLock)
            {
                return _commodityList.Select(c => new ReadOnlyCommodity(c)).ToList();
            }
        }

        public IReadOnlyList<ReadOnlyResource> GetReadOnlyResourceList()
        {
            return _resourceList.Select(r => new ReadOnlyResource(r)).ToList();
        }

        public IReadOnlyList<ReadOnlyBlueprint> GetReadOnlyGlobalBlueprintList()
        {
            return _globalBlueprintList.Select(b => new ReadOnlyBlueprint(b)).ToList();
        }

        public ReadOnlyCommodity FindReadOnlyCommodity(string name)
        {
            var entity = FindCommodity(name);
            return entity != null ? new ReadOnlyCommodity(entity) : null;
        }

        public ReadOnlyBlueprint FindReadOnlyGlobalBlueprint(string id)
        {
            return FindGlobalBlueprint(id);
        }

        /// <summary>
        /// Returns the mutable Blueprint entity for the given UUID from the global list only.
        /// Only called by BlueprintService.
        /// </summary>
        internal Blueprint FindMutableGlobalBlueprint(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return null;
            if (_globalBlueprintList == null) return null;

            if (_globalBlueprintCache == null)
            {
                _globalBlueprintCache = new Dictionary<string, Blueprint>();
                foreach (var bp in _globalBlueprintList)
                {
                    if (bp.UUID != null && !_globalBlueprintCache.ContainsKey(bp.UUID))
                        _globalBlueprintCache[bp.UUID] = bp;
                }
            }

            _globalBlueprintCache.TryGetValue(uuid, out Blueprint bp2);
            return bp2;
        }

        /// <summary>
        /// Wires up Item.DisplayNameResolver so Item.ExtendedName can resolve
        /// Survey and Blueprint display names without a direct singleton reference.
        /// </summary>
        private static void WireDisplayNameResolver()
        {
            Item.DisplayNameResolver = (itemType, uuid) =>
            {
                if (itemType == ItemType.ItemTypeEnum.Survey)
                {
                    var survey = PlayerContext?.FindSurvey(uuid);
                    if (survey != null)
                    {
                        string name = $"{survey.PlanetName} ({survey.SurveyID})";
                        if (!string.IsNullOrEmpty(survey.NickName))
                        {
                            name += $" [{survey.NickName}]";
                        }

                        return name;
                    }
                }

                if (itemType == ItemType.ItemTypeEnum.Blueprint)
                {
                    var blueprint = PlayerContext?.FindBlueprint(uuid);
                    if (blueprint != null)
                    {
                        string name = string.Empty;
                        if (blueprint.Class > 0)
                        {
                            name += $"C{blueprint.Class} ";
                        }

                        if (blueprint.Evolution > 0)
                        {
                            name += $"(Ev{blueprint.Evolution}) ";
                        }

                        name += blueprint.Name + " ";
                        if (!string.IsNullOrEmpty(blueprint.TechLevel))
                        {
                            name += $"({blueprint.TechLevel}) ";
                        }

                        if (!string.IsNullOrEmpty(blueprint.NickName))
                        {
                            name += $"[{blueprint.NickName}] ";
                        }

                        return name.Trim();
                    }
                }

                return null;
            };
        }

        /// <summary>
        /// Initializes the PropertyTypeRegistry from baseline data.
        /// </summary>
        private void InitPropertyTypes(BaselineRoot root)
        {
            _propertyTypeRegistry = root.PropertyType != null
                ? new List<PropertyTypeDefinition>(root.PropertyType)
                : new List<PropertyTypeDefinition>();
            _propertyTypeCache = null;
        }

        private void RunMigrationsIfConfigured()
        {
            if (RunMigrations == null) return;

            int prevBaselineVersion = DataVersion;
            int prevPlayerVersion = PlayerContext.DataVersion;
            RunMigrations(this, PlayerContext);
            if (DataVersion != prevBaselineVersion)
            {
                WriteContext();
            }

            if (PlayerContext.DataVersion != prevPlayerVersion)
            {
                PlayerContext.WriteContext();
            }
        }
    }
}