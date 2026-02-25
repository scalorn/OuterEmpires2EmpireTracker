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
        public BindingList<BlueprintType> blueprintTypeList;
        public BindingSource bindingSourceBlueprintType;
        public BindingList<ShipClass> shipClassList;
        public BindingSource bindingSourceShipClass;
        public BindingList<TechLevel> techLevelList;
        public BindingSource bindingSourceTechLevel;
        public BindingList<string> evolutionList;
        public BindingSource bindingSourceEvolution;

        public EmpireContext() : base()
        {
            Instance = this;

            string filePath = @"..\..\BaselineData.json";

            // Read the file content into a string
            string jsonContent = File.ReadAllText(filePath);
            BaselineRoot baselineRoot = JsonConvert.DeserializeObject<BaselineRoot>(jsonContent);


            initBlueprintTypes(baselineRoot);
            initShipClasses(baselineRoot);
            initTechLevels(baselineRoot);
            initEvolutions(baselineRoot);
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
