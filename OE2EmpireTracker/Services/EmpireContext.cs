using Amazon;
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
        public BindingList<BlueprintType> blueprintTypeList;
        public BindingSource bindingSourceBlueprintType;
        public BindingList<ShipClass> shipClassList;
        public BindingSource bindingSourceShipClass;
        public BindingList<TechLevel> techLevelList;
        public BindingSource bindingSourceTechLevel;
        public BindingList<string> evolutionList;
        public BindingSource bindingSourceEvolution;
        public BindingList<Resource> resourceList;
        public BindingSource bindingSourceResource;
        public BindingList<ResourceGroup> resourceGroupList;
        public BindingSource bindingSourceResourceGroup;
        public BindingList<ResourcePurity> resourcePurityList;
        public BindingSource bindingSourceResourcePurity;
        public int DataVersion { get; set; } = 0;
        public BaselineGameConstants GameConstants { get; set; }
        public BindingList<Blueprint> globalBlueprintList;
        public List<Commodity> commodityList;

        public static EmpireContext getInstance()
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
        public static EmpireContext getInstanceIfLoaded()
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
            PlayerContext = PlayerContext.getInstance();

            Log.Info("Loading baseline data from {0}", FilePath);
            string jsonContent = File.ReadAllText(FilePath);
            BaselineRoot baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(jsonContent);
            Log.Info("Baseline data loaded: {0} blueprint types, {1} ship classes, {2} tech levels",
                baselineRoot.BlueprintType?.Length ?? 0, baselineRoot.ShipClass?.Length ?? 0, baselineRoot.TechLevel?.Length ?? 0);

            DataVersion = baselineRoot.DataVersion;
            GameConstants = baselineRoot.GameConstants ?? new BaselineGameConstants();

            initBlueprintTypes(baselineRoot);
            initShipClasses(baselineRoot);
            initTechLevels(baselineRoot);
            initEvolutions(baselineRoot);
            InitResources(baselineRoot);
            initResourceGroups(baselineRoot);
            initResourcePurities(baselineRoot);
            InitCommodities(baselineRoot);
            InitGlobalBlueprints(baselineRoot);

            // Run migrations after both contexts are loaded
            int prevBaselineVersion = DataVersion;
            int prevPlayerVersion = PlayerContext.DataVersion;
            MigrationRunner.Run(this, PlayerContext);
            if (DataVersion != prevBaselineVersion)
            {
                writeContext();
            }
            if (PlayerContext.DataVersion != prevPlayerVersion)
            {
                PlayerContext.writeContext();
            }
        }
        public void writeContext()
        {
            BaselineRoot baselineRoot = new BaselineRoot();
            baselineRoot.DataVersion = DataVersion;
            baselineRoot.GameConstants = GameConstants;
            baselineRoot.ShipClass = shipClassList.ToArray();
            baselineRoot.BlueprintType = blueprintTypeList.ToArray();
            baselineRoot.Blueprint = globalBlueprintList.ToArray();
            baselineRoot.TechLevel = techLevelList.ToArray();
            baselineRoot.Commodity = commodityList?.ToArray();
            string jsonContent = JsonConvert.SerializeObject(baselineRoot, JsonSettings.SerializerSettings);
            SafeFileWriter.WriteAllText(FilePath, jsonContent);
            Log.Info("Baseline data saved to {0}", FilePath);
        }
        public void initBlueprintTypes(BaselineRoot baselineRoot)
        {
            List<BlueprintType> list = new List<BlueprintType>(baselineRoot.BlueprintType);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            blueprintTypeList = new BindingList<BlueprintType>(list);
            // Initialize the BindingSource component
            bindingSourceBlueprintType = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceBlueprintType.DataSource = blueprintTypeList;
        }

        public BlueprintType FindBlueprintType(string id)
        {
            var filteredList = blueprintTypeList
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
            return blueprintTypeList.FirstOrDefault(bt =>
                string.Equals(bt.IconPosition, iconPosition, StringComparison.Ordinal));
        }

        public void initShipClasses(BaselineRoot baselineRoot)
        {
            shipClassList = new BindingList<ShipClass>(baselineRoot.ShipClass);

            // Initialize the BindingSource component
            bindingSourceShipClass = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceShipClass.DataSource = shipClassList;
        }

        public ShipClass FindShipClass(int id)
        {
            var filteredList = shipClassList
                .Where(item => item.Id == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }
            return null;
        }

        public void initTechLevels(BaselineRoot baselineRoot)
        {
            List<TechLevel> list = new List<TechLevel>(baselineRoot.TechLevel);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            techLevelList = new BindingList<TechLevel>(list);
            // Initialize the BindingSource component
            bindingSourceTechLevel = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceTechLevel.DataSource = techLevelList;
        }
        public TechLevel FindTechLevel(string id)
        {
            var filteredList = techLevelList
                .Where(item => item.Name == id)
                .ToList();
            if (filteredList.Count == 1)
            {
                return filteredList[0];
            }
            return null;
        }

        public void initEvolutions(BaselineRoot baselineRoot)
        {
            List<string> list = new List<string>();
            for(int evo = 0; evo <= 15; evo++)
            {
                list.Add(evo.ToString());
            }
            evolutionList = new BindingList<string>(list);
            // Initialize the BindingSource component
            bindingSourceEvolution = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceEvolution.DataSource = evolutionList;
        }
        public string FindEvolution(int id)
        {
            string key = "" + id;
            var filteredList = evolutionList
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
            resourceList = new BindingList<Resource>(list);
            // Initialize the BindingSource component
            bindingSourceResource = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceResource.DataSource = resourceList;
        }
        public void initResourceGroups(BaselineRoot baselineRoot)
        {
            List<ResourceGroup> list = new List<ResourceGroup>(ResourceGroup.Groups);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            resourceGroupList = new BindingList<ResourceGroup>(list);
            // Initialize the BindingSource component
            bindingSourceResourceGroup = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceResourceGroup.DataSource = resourceGroupList;
        }
        public void initResourcePurities(BaselineRoot baselineRoot)
        {
            List<ResourcePurity> list = new List<ResourcePurity>(ResourcePurity.Purities);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            resourcePurityList = new BindingList<ResourcePurity>(list);
            bindingSourceResourcePurity = new BindingSource();
            bindingSourceResourcePurity.DataSource = resourcePurityList;
        }

        public void InitCommodities(BaselineRoot baselineRoot)
        {
            if (baselineRoot.Commodity != null && baselineRoot.Commodity.Length > 0)
            {
                commodityList = new List<Commodity>(baselineRoot.Commodity);
                Commodity.SetCommodities(commodityList);
                Log.Info("Loaded {0} commodities from baseline data", commodityList.Count);
            }
            else
            {
                commodityList = new List<Commodity>(Commodity.Commodities);
                Log.Info("Using hardcoded commodity list ({0} commodities)", commodityList.Count);
            }
        }

        public void InitGlobalBlueprints(BaselineRoot baselineRoot)
        {
            var list = new List<Blueprint>(baselineRoot.Blueprint ?? new Blueprint[0]);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            globalBlueprintList = new BindingList<Blueprint>(list);
            Log.Info("Loaded {0} global blueprints", globalBlueprintList.Count);
        }

        /// <summary>
        /// Searches global blueprints by UUID.
        /// </summary>
        public Blueprint FindGlobalBlueprint(string id)
        {
            return globalBlueprintList?.FirstOrDefault(b => b.UUID == id);
        }

    }
    public class BaselineRoot
    {
        public int DataVersion;
        public BaselineGameConstants GameConstants;
        public ShipClass[] ShipClass;
        public BlueprintType[] BlueprintType;
        public Blueprint[] Blueprint;
        public TechLevel[] TechLevel;
        public Commodity[] Commodity;
    }
}
