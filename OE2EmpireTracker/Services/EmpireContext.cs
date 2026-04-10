using Amazon;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Services.Migration;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Models;
using Sgml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OE2EmpireTracker.Services
{
    public class EmpireContext
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static EmpireContext Instance;
        public static string FilePath { get; set; } = @"..\..\BaselineData.json";

        public static PlayerContext PlayerContext;
        public BindingList<BlueprintType> BlueprintTypeList;
        public BindingSource BindingSourceBlueprintType;
        public BindingList<ShipClass> ShipClassList;
        public BindingSource BindingSourceShipClass;
        public BindingList<TechLevel> TechLevelList;
        public BindingSource BindingSourceTechLevel;
        public BindingList<string> EvolutionList;
        public BindingSource BindingSourceEvolution;
        public BindingList<Resource> ResourceList;
        public BindingSource BindingSourceResource;
        public BindingList<ResourceGroup> ResourceGroupList;
        public BindingSource BindingSourceResourceGroup;
        public BindingList<ResourcePurity> ResourcePurityList;
        public BindingSource BindingSourceResourcePurity;
        public int DataVersion { get; set; } = 0;
        public BaselineGameConstants GameConstants { get; set; }
        public BindingList<Blueprint> GlobalBlueprintList;
        public List<Commodity> CommodityList;

        public static EmpireContext GetInstance()
        {
            if (Instance == null)
            {
                Instance = new EmpireContext();
            }
            return Instance;
        }

        /// <summary>
        /// Returns the current instance without creating one if it doesn't exist.
        /// Used by GameConstants to avoid triggering file I/O during early access.
        /// </summary>
        public static EmpireContext GetInstanceIfLoaded()
        {
            return Instance;
        }

        public static void Reset()
        {
            Instance = null;
            PlayerContext = null;
            OE2EmpireTracker.Services.PlayerContext.Reset();
        }

        private EmpireContext() : base()
        {
            Instance = this;
            PlayerContext = PlayerContext.GetInstance();

            Log.Info("Loading baseline data from {0}", FilePath);
            string jsonContent = File.ReadAllText(FilePath);
            BaselineRoot baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(jsonContent);
            Log.Info("Baseline data loaded: {0} blueprint types, {1} ship classes, {2} tech levels",
                baselineRoot.BlueprintType?.Length ?? 0, baselineRoot.ShipClass?.Length ?? 0, baselineRoot.TechLevel?.Length ?? 0);

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
        public void WriteContext()
        {
            if (MigrationRunner.MigrationFailed)
            {
                Log.Warn("WriteContext blocked â€” migration failed, saving disabled");
                return;
            }
            BaselineRoot baselineRoot = new BaselineRoot();
            baselineRoot.DataVersion = DataVersion;
            baselineRoot.GameConstants = GameConstants;
            baselineRoot.ShipClass = ShipClassList.ToArray();
            baselineRoot.BlueprintType = BlueprintTypeList.ToArray();
            baselineRoot.Blueprint = GlobalBlueprintList.ToArray();
            baselineRoot.TechLevel = TechLevelList.ToArray();
            baselineRoot.Commodity = CommodityList?.ToArray();
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
            BlueprintTypeList = new BindingList<BlueprintType>(list);
            // Initialize the BindingSource component
            BindingSourceBlueprintType = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceBlueprintType.DataSource = BlueprintTypeList;
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
            ShipClassList = new BindingList<ShipClass>(baselineRoot.ShipClass);

            // Initialize the BindingSource component
            BindingSourceShipClass = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceShipClass.DataSource = ShipClassList;
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
            TechLevelList = new BindingList<TechLevel>(list);
            // Initialize the BindingSource component
            BindingSourceTechLevel = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceTechLevel.DataSource = TechLevelList;
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
            for(int evo = 0; evo <= 15; evo++)
            {
                list.Add(evo.ToString());
            }
            EvolutionList = new BindingList<string>(list);
            // Initialize the BindingSource component
            BindingSourceEvolution = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceEvolution.DataSource = EvolutionList;
        }
        public string FindEvolution(int id)
        {
            string key = "" + id;
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
            ResourceList = new BindingList<Resource>(list);
            // Initialize the BindingSource component
            BindingSourceResource = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceResource.DataSource = ResourceList;
        }
        public void InitResourceGroups(BaselineRoot baselineRoot)
        {
            List<ResourceGroup> list = new List<ResourceGroup>(ResourceGroup.Groups);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            ResourceGroupList = new BindingList<ResourceGroup>(list);
            // Initialize the BindingSource component
            BindingSourceResourceGroup = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            BindingSourceResourceGroup.DataSource = ResourceGroupList;
        }
        public void InitResourcePurities(BaselineRoot baselineRoot)
        {
            List<ResourcePurity> list = new List<ResourcePurity>(ResourcePurity.Purities);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            ResourcePurityList = new BindingList<ResourcePurity>(list);
            BindingSourceResourcePurity = new BindingSource();
            BindingSourceResourcePurity.DataSource = ResourcePurityList;
        }

        public void InitCommodities(BaselineRoot baselineRoot)
        {
            if (baselineRoot.Commodity != null && baselineRoot.Commodity.Length > 0)
            {
                CommodityList = new List<Commodity>(baselineRoot.Commodity);
                Commodity.SetCommodities(CommodityList);
                Log.Info("Loaded {0} commodities from baseline data", CommodityList.Count);
            }
            else
            {
                CommodityList = new List<Commodity>(Commodity.Commodities);
                Log.Info("Using hardcoded commodity list ({0} commodities)", CommodityList.Count);
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
            GlobalBlueprintList = new BindingList<Blueprint>(list);
            Log.Info("Loaded {0} global blueprints", GlobalBlueprintList.Count);
        }

        /// <summary>
        /// Searches global blueprints by UUID.
        /// </summary>
        public Blueprint FindGlobalBlueprint(string id)
        {
            return GlobalBlueprintList?.FirstOrDefault(b => b.UUID == id);
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
