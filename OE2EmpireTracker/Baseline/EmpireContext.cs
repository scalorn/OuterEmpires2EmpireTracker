using Amazon;
using Newtonsoft.Json;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using Sgml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
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
using static OE2EmpireTracker.Baseline.BlueprintField;

namespace OE2EmpireTracker.Baseline
{
    public class EmpireContext
    {
        public static EmpireContext Instance;
        public static string filePath = @"..\..\BaselineData.json";

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

        public EmpireContext() : base()
        {
            Instance = this;
            PlayerContext = new PlayerContext();

            // Read the file content into a string
            string jsonContent = File.ReadAllText(filePath);
            BaselineRoot baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(jsonContent);

            initBlueprintTypes(baselineRoot);
            initShipClasses(baselineRoot);
            initTechLevels(baselineRoot);
            initEvolutions(baselineRoot);
            initResources(baselineRoot);
            initResourceGroups(baselineRoot);
            initResourcePurities(baselineRoot);
        }
        public void writeContext()
        {
            BaselineRoot baselineRoot = new BaselineRoot();
            baselineRoot.ShipClass = shipClassList.ToArray();
            baselineRoot.BlueprintType = blueprintTypeList.ToArray();
            baselineRoot.ResourceGroup = resourceGroupList.ToArray();
            baselineRoot.ResourcePurity = resourcePurityList.ToArray();
            baselineRoot.Resource = resourceList.ToArray();
            baselineRoot.TechLevel = techLevelList.ToArray();
        string jsonContent = JsonConvert.SerializeObject(baselineRoot, Formatting.Indented);
            File.WriteAllText(filePath + ".new", jsonContent);
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
        public void initShipClasses(BaselineRoot baselineRoot)
        {
            shipClassList = new BindingList<ShipClass>(baselineRoot.ShipClass);

            // Initialize the BindingSource component
            bindingSourceShipClass = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceShipClass.DataSource = shipClassList;
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
        public void initResources(BaselineRoot baselineRoot)
        {
            List<Resource> list = new List<Resource>(baselineRoot.Resource);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            resourceList = new BindingList<Resource>(list);
            // Initialize the BindingSource component
            bindingSourceResource = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceResource.DataSource = resourceList;
        }
        public void initResourceGroups(BaselineRoot baselineRoot)
        {
            List<ResourceGroup> list = new List<ResourceGroup>(baselineRoot.ResourceGroup);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            resourceGroupList = new BindingList<ResourceGroup>(list);
            // Initialize the BindingSource component
            bindingSourceResourceGroup = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceResourceGroup.DataSource = resourceGroupList;
        }
        public void initResourcePurities(BaselineRoot baselineRoot)
        {
            List<ResourcePurity> list = new List<ResourcePurity>(baselineRoot.ResourcePurity);
            list.Sort((x, y) => x.Name.CompareTo(y.Name));
            resourcePurityList = new BindingList<ResourcePurity>(list);
            // Initialize the BindingSource component
            bindingSourceResourcePurity = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            bindingSourceResourcePurity.DataSource = resourcePurityList;
        }

    }
    public class BaselineRoot
    {
        public ShipClass[] ShipClass;
        public BlueprintType[] BlueprintType;
        public ResourceGroup[] ResourceGroup;
        public ResourcePurity[] ResourcePurity;
        public Resource[] Resource;
        public TechLevel[] TechLevel;
    }
}
