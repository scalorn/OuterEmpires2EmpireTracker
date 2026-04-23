using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Amazon;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services.Migration;
using Sgml;

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

            // Run migrations after both contexts are loaded
            int prevBaselineVersion = DataVersion;
            int prevPlayerVersion = PlayerContext.DataVersion;
            MigrationRunner.Run(this, PlayerContext);
            if (DataVersion != prevBaselineVersion)
            {
                WriteContext();
            }

            if (PlayerContext.DataVersion != prevPlayerVersion)
            {
                PlayerContext.WriteContext();
            }
        }

        public static string FilePath { get; set; } = "BaselineData.json";

        public static PlayerContext PlayerContext { get; set; }

        public IReadOnlyList<BlueprintType> BlueprintTypeList => _blueprintTypeList;

        public BindingSource BindingSourceBlueprintType { get; set; }

        public IReadOnlyList<ShipClass> ShipClassList => _shipClassList;

        public BindingSource BindingSourceShipClass { get; set; }

        public IReadOnlyList<TechLevel> TechLevelList => _techLevelList;

        public BindingSource BindingSourceTechLevel { get; set; }

        public IReadOnlyList<string> EvolutionList => _evolutionList;

        public BindingSource BindingSourceEvolution { get; set; }

        public IReadOnlyList<Resource> ResourceList => _resourceList;

        public BindingSource BindingSourceResource { get; set; }

        public IReadOnlyList<ResourceGroup> ResourceGroupList => _resourceGroupList;

        public BindingSource BindingSourceResourceGroup { get; set; }

        public IReadOnlyList<ResourcePurity> ResourcePurityList => _resourcePurityList;

        public BindingSource BindingSourceResourcePurity { get; set; }

        public int DataVersion { get; set; } = 0;

        public BaselineGameConstants GameConstants { get; set; }

        public IReadOnlyList<Blueprint> GlobalBlueprintList => _globalBlueprintList;

        public IReadOnlyList<Commodity> CommodityList => _commodityList;

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
            if (MigrationRunner.MigrationFailed)
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
            string jsonContent = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
            SafeFileWriter.WriteAllText(FilePath, jsonContent);
            Log.Info("Baseline data saved to {0}", FilePath);
        }

        public void InitBlueprintTypes(BaselineRoot baselineRoot)
        {
            List<BlueprintType> list = new List<BlueprintType>(baselineRoot.BlueprintType);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _blueprintTypeList = new List<BlueprintType>(list);
            // Initialize the BindingSource component
            BindingSourceBlueprintType = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceBlueprintType.DataSource = _blueprintTypeList;
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

            // Initialize the BindingSource component
            BindingSourceShipClass = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceShipClass.DataSource = _shipClassList;
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
            List<TechLevel> list = new List<TechLevel>(baselineRoot.TechLevel);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _techLevelList = new List<TechLevel>(list);
            // Initialize the BindingSource component
            BindingSourceTechLevel = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceTechLevel.DataSource = _techLevelList;
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
            // Initialize the BindingSource component
            BindingSourceEvolution = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceEvolution.DataSource = _evolutionList;
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
            List<Resource> list = new List<Resource>(Resource.Resources);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _resourceList = new List<Resource>(list);
            // Initialize the BindingSource component
            BindingSourceResource = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceResource.DataSource = _resourceList;
        }

        public void InitResourceGroups(BaselineRoot baselineRoot)
        {
            List<ResourceGroup> list = new List<ResourceGroup>(ResourceGroup.Groups);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _resourceGroupList = new List<ResourceGroup>(list);
            // Initialize the BindingSource component
            BindingSourceResourceGroup = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceResourceGroup.DataSource = _resourceGroupList;
        }

        public void InitResourcePurities(BaselineRoot baselineRoot)
        {
            List<ResourcePurity> list = new List<ResourcePurity>(ResourcePurity.Purities);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _resourcePurityList = new List<ResourcePurity>(list);
            BindingSourceResourcePurity = new BindingSource();
            BindingSourceResourcePurity.DataSource = _resourcePurityList;
        }

        public void InitCommodities(BaselineRoot baselineRoot)
        {
            if (baselineRoot.Commodity != null && baselineRoot.Commodity.Length > 0)
            {
                _commodityList = new List<Commodity>(baselineRoot.Commodity);
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
            var list = new List<Blueprint>(baselineRoot.Blueprint ?? new Blueprint[0]);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            _globalBlueprintList = new List<Blueprint>(list);
            InvalidateGlobalBlueprintCache();
            Log.Info("Loaded {0} global blueprints", _globalBlueprintList.Count);
        }

        /// <summary>
        /// Searches global blueprints by UUID using a dictionary cache for O(1) lookup.
        /// </summary>
        public Blueprint FindGlobalBlueprint(string id)
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
                return match;

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

        // ── Task 6.2: Mutation methods for GlobalBlueprint (UUID cache, no BindingSource) ──

        public void AddGlobalBlueprint(Blueprint item)
        {
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

        // ── Task 6.3: Mutation methods for Commodity (name cache with _commodityLock) ──

        public void AddCommodity(Commodity item)
        {
            lock (_commodityLock)
            {
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

        // ── Task 6.4: Mutation methods for BindingSource-only lists ──

        public void AddBlueprintType(BlueprintType item)
        {
            _blueprintTypeList.Add(item);
            BindingSourceBlueprintType?.ResetBindings(false);
        }

        public void RemoveBlueprintType(BlueprintType item)
        {
            _blueprintTypeList.Remove(item);
            BindingSourceBlueprintType?.ResetBindings(false);
        }

        public void AddShipClass(ShipClass item)
        {
            _shipClassList.Add(item);
            BindingSourceShipClass?.ResetBindings(false);
        }

        public void RemoveShipClass(ShipClass item)
        {
            _shipClassList.Remove(item);
            BindingSourceShipClass?.ResetBindings(false);
        }

        public void AddTechLevel(TechLevel item)
        {
            _techLevelList.Add(item);
            BindingSourceTechLevel?.ResetBindings(false);
        }

        public void RemoveTechLevel(TechLevel item)
        {
            _techLevelList.Remove(item);
            BindingSourceTechLevel?.ResetBindings(false);
        }

        public void AddEvolution(string item)
        {
            _evolutionList.Add(item);
            BindingSourceEvolution?.ResetBindings(false);
        }

        public void RemoveEvolution(string item)
        {
            _evolutionList.Remove(item);
            BindingSourceEvolution?.ResetBindings(false);
        }

        public void AddResource(Resource item)
        {
            _resourceList.Add(item);
            BindingSourceResource?.ResetBindings(false);
        }

        public void RemoveResource(Resource item)
        {
            _resourceList.Remove(item);
            BindingSourceResource?.ResetBindings(false);
        }

        public void AddResourceGroup(ResourceGroup item)
        {
            _resourceGroupList.Add(item);
            BindingSourceResourceGroup?.ResetBindings(false);
        }

        public void RemoveResourceGroup(ResourceGroup item)
        {
            _resourceGroupList.Remove(item);
            BindingSourceResourceGroup?.ResetBindings(false);
        }

        public void AddResourcePurity(ResourcePurity item)
        {
            _resourcePurityList.Add(item);
            BindingSourceResourcePurity?.ResetBindings(false);
        }

        public void RemoveResourcePurity(ResourcePurity item)
        {
            _resourcePurityList.Remove(item);
            BindingSourceResourcePurity?.ResetBindings(false);
        }
    }

    public class BaselineRoot
    {
        public int DataVersion { get; set; }
        public BaselineGameConstants GameConstants { get; set; }
        public ShipClass[] ShipClass { get; set; }
        public BlueprintType[] BlueprintType { get; set; }
        public Blueprint[] Blueprint { get; set; }
        public TechLevel[] TechLevel { get; set; }
        public Commodity[] Commodity { get; set; }
        public RefiningRecipe[] RefiningRecipe { get; set; }
        public ResearchTimeEntry[] ResearchTime { get; set; }
    }
}
