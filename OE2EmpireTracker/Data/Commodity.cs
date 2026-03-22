using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime.Documents;
using Sgml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OE2EmpireTracker.Data.Resource;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace OE2EmpireTracker.Data
{
    public class Commodity
    {
        [Required]
        public Data.CommodityGroup.CommodityGroupEnum CommodityGroup { get; set; }

        public string ID { get; set; }
        public string Name { get; set; }

        public Dictionary<string, string> ConstructionResources { get; set; }


        private static List<Commodity> _commodities = getCommodities();
        private static Dictionary<string, Commodity> _commodityMapByEnum;
        private static Dictionary<string, Commodity> _commodityMapByString;

        public static IReadOnlyList<Commodity> Commodities => _commodities.AsReadOnly();
        public static IReadOnlyDictionary<string, Commodity> ResourceMapByEnum => _commodityMapByEnum;
        public static IReadOnlyDictionary<string, Commodity> ResourceMapByString => _commodityMapByString;


        private static List<Commodity> getCommodities()
        {
            List<Commodity> instance = new List<Commodity>();
            instance.Add(new Commodity()
            {
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.EngineeringBlock,
                ID = "Advanced Biolubricants",
                Name = "Advanced Biolubricants",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Strong Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.EngineeringBlock,
                ID = "Advanced Materials Simulators",
                Name = "Advanced Materials Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.TechnologyInstitute,
                ID = "AGI Archives",
                Name = "AGI Archives",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "a", "" },
                    { "b", "" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.ManufacturingIndustryCentre,
                ID = "AGI Manu-Augments",
                Name = "AGI Manu-Augments",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "a", "" },
                    { "b", "" }
                }
            });
            /*
            Agridome Ops Units  Agridome
            AI Finance Cubes    Healthcare Institute
            AI Inventory Systems Logistics Centre
            AmmoMate Munitions Printer  Institute of Defence
            Aquacore Pumps Agridome
            Assemblatrons Manufacturing Industry Centre
            AssembleMate Crucibles  Manufacturing Industry Centre
            Atomsmasher Crucibles Refining Industry Centre
            Autonomous Trader Bots Centre of Economics
            BattleCom Interconnects Institute of Defence
            Biochem Delivery Systems    Mining Industry Centre
            Biochip Arrays Healthcare Institute
            Biomass Harvester Controllers   Agridome
            Biosuspension Fluids Healthcare Institute
            Biotech Vision Systems  Science Centre
            Brainwave Scintillators Science Centre
            Branding Systems    Off - World Living Institute
            Calalyst Reaction Simulators Refining Industry Centre
            Cargo Drones    Logistics Centre
            Caustic Fluids  Refining Industry Centre
            ChemLab Units Agridome
            Clean Air Units Off-World Living Institute
            Climate Hubs    Off - World Living Institute
            Clothing Printers   Leisure Industry Centre
            Comfortcore Arrays Off-World Living Institute
            Concentrax Containment Units Refining Industry Centre
            Conceptcore Drivers Science Centre
            Confectionery Leisure Industry Centre
            Constructex Waldos  Engineering Block
            Containment Units   Institute of Defence
            Core Dyagnostic Component   Engineering Block
            Coreseeker Guidance Bits Mining Industry Centre
            CRM Units   Administration Block
            Cryogenic Coolant Tanks Institute of Defence
            Cryptochips Science Centre
            Data Accumulation Systems   Technology Institute
            DataScope Assemblies    Science Centre
            Datasphere Frames   Centre of Economics
            DegasTech Reforgers Manufacturing Industry Centre
            Delivery Systems    Logistics Centre
            Demand Drivers  Manufacturing Industry Centre
            Design AGI Chassis  Off - World Living Institute
            Diagnocores Off-World Living Institute
            Dockmaster Drones   Logistics Centre
            Document Calibrators    Administration Block
            Drillcore Chassis   Mining Industry Centre
            Earthmover Fabricators Mining Industry Centre
            EconoSim Units  Centre of Economics
            Efficiency Systems Manufacturing Industry Centre
            Energy Storage systems Off-World Living Institute
            Enriched Agar Gels Science Centre
            Entertainmate Interconnects Leisure Industry Centre
            Equity Analyzers    Centre of Economics
            Events AGI Chassis  Leisure Industry Centre
            Exoskeleton Waldos Manufacturing Industry Centre
            Fermented Beverages Leisure Industry Centre
            Filterator Chassis Refining Industry Centre
            Finance AutoMods    Manufacturing Industry Centre
            Fold Containers Technology Institute
            Funding Systems Healthcare Institute
            G - Suites    Leisure Industry Centre
            Gameonix Simulators Leisure Industry Centre
            Gene - Seed Packets Agridome
            Genecore Processors Healthcare Institute
            General Control Systems Engineering Block
            Geoengineering Slurry Mining Industry Centre
            Gigagro Fertilizers Agridome
            Gravimeters Science Centre
            Handheld Infrartek Units Refining Industry Centre
            Haptic Equipment    Centre of Economics
            Hazard Bunkers Refining Industry Centre
            Hazard Disposal Systems Refining Industry Centre
            Hazard Gear Refining Industry Centre
            Health Scanners Healthcare Institute
            Heavy Alloy Printers    Manufacturing Industry Centre
            Heavy Harvesters Agridome
            Heavy Laser Drill Parts Mining Industry Centre
            Heavy Mover Kits    Mining Industry Centre
            Heavy Robot Bays    Engineering Block
            Heavy Unit Manipulators Manufacturing Industry Centre
            HEV Duty Exosuits Mining Industry Centre
            HEV - Battledress Institute of Defence
            Hibernation Pods Off-World Living Institute
            HoloDesks Administration Block
            Holomap Units Logistics Centre
            Hometech Supplies Off-World Living Institute
            Human Facilities    Science Centre
            Hydrobots Off-World Living Institute
            Hydroponics Quarters    Agridome
            Hyper - Alloy Pliers Refining Industry Centre
            Ideaforge Interconnects Science Centre
            Impact Analysis Archivers Centre of Economics
            Infonomicon Clusters    Centre of Economics
            Instrument Calibrators Engineering Block
            Joybot Parts Leisure Industry Centre
            Labtrackers Science Centre
            Livestock Links Agridome
            Loader Smartlinks   Logistics Centre
            Logicon Units   Technology Institute
            Macroscope Chassis  Centre of Economics
            Marketlink Systems Logistics Centre
            Marketpulse Readers Centre of Economics
            Mass Manipulators   Mining Industry Centre
            Material Handling Plotters  Logistics Centre
            Material Lances Technology Institute
            Matter Simulators   Technology Institute
            Medicbot Operations Ports Healthcare Institute
            Memochips   Administration Block
            Mess Hall Kits Institute of Defence
            Metallics Market Analyzers Centre of Economics
            Microarray Printers Science Centre
            Mineral Processors  Technology Institute
            Modular Building Units Off-World Living Institute
            Modular Cargo Kits Agridome
            Modular Control Centres Manufacturing Industry Centre
            Modular Storage Kits Technology Institute
            Molecular Printer Healthcare Institute
            Molecule Assemblers Engineering Block
            Monobonding Shields Refining Industry Centre
            Nano - Welding Tools Institute of Defence
            Nanobot Reservoirs  Engineering Block
            Nanocure Supplies   Off - World Living Institute
            NanoLab Units   Science Centre
            Nanomachine Swarmers    Technology Institute
            Narcotics Leisure Industry Centre
            Nervegear Technology Institute
            Netboost Nodes Technology Institute
            Neural Interfaces Technology Institute
            Neural Scanners Manufacturing Industry Centre
            NeuroInterfaze Units    Engineering Block
            Next - Gen Logibots Logistics Centre
            Next - Gen Management Units   Agridome
            Next - Gen Sound Systems  Leisure Industry Centre
            Noise Jammers Administration Block
            Note Beamers Administration Block
            Oasis Control Bots  Administration Block
            Ore Analyzers   Mining Industry Centre
            Oretech Systems Refining Industry Centre
            Organibots Administration Block
            Particle Scanners Science Centre
            Pathology Simulators Healthcare Institute
            Pharmaceuticals Synthesizer Healthcare Institute
            Plasma Cutters Engineering Block
            Playport Displays Leisure Industry Centre
            Policy Governance Systems Centre of Economics
            Power Cell Casemates Institute of Defence
            Processing Control Systems Refining Industry Centre
            Projectors Leisure Industry Centre
            Proto - Alloys    Technology Institute
            Prototyper Systems  Engineering Block
            Reactant Calibrators    Institute of Defence
            Regenerative Nanobot Hives  Healthcare Institute
            Resource Allocators Administration Block
            Rig Living Space Kits   Mining Industry Centre
            RiskMinder Bots Centre of Economics
            Rockminer Drones    Mining Industry Centre
            Safety Equipment Refining Industry Centre
            Scanner Chassis Mining Industry Centre
            Sciencelink Housings Science Centre
            Security Systems Leisure Industry Centre
            Self - healing Materials Off-World Living Institute
            Self - Replicating Jigs Engineering Block
            Shift Hab Units Logistics Centre
            Silo Control Systems Institute of Defence
            Sim - Meat Consignments Centre of Economics
            Small Arms  Institute of Defence
            Smart Conveyors Refining Industry Centre
            Smart Systems   Administration Block
            Smart Toolsets  Manufacturing Industry Centre
            SolarScythe Trimmers Agridome
            Starshield Clothing Agridome
            Stress Analyzers Technology Institute
            Supply Chain AGI Chassis Logistics Centre
            Supply Keylocks Administration Block
            Survival Pods Mining Industry Centre
            Suspension Fluids   Manufacturing Industry Centre
            TacticalNet C3 Systems  Institute of Defence
            Target Lock Relays  Institute of Defence
            Tasklinks   Administration Block
            Telemedicine Consoles   Healthcare Institute
            Teleoffice Suites   Centre of Economics
            Telepresence Equipment Mining Industry Centre
            Testing Simulators  Manufacturing Industry Centre
            Theoretical Freightlink Systems Logistics Centre
            Thrilltech Suites   Leisure Industry Centre
            Tissue Printers Healthcare Institute
            Tooltech Printers Engineering Block
            Trauma Systems Institute of Defence
            Trend AGI Projectors Centre of Economics
            Triarge Facilities  Healthcare Institute
            Valuation Vaults    Off - World Living Institute
            VAST Arrays Science Centre
            Vehicle Bolts   Engineering Block
            Virtual Assistants  Administration Block
            Vrtrek Units    Leisure Industry Centre
            Waffers Technology Institute
            Warehouse AutoMods  Logistics Centre
            Warehouse Control Cabins Logistics Centre
            WarTech Housings Institute of Defence
            Waste Management Fluids Off-World Living Institute
            Water - Hive Irrigators Agridome
            Waypointers Logistics Centre
            Weathermate Interconnects Agridome
            Welfare Risk Assessors Administration Block
            Workflow Automaters Administration Block
            */

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new Commodity() { CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.None,  ID = "", Name = ""});

            _commodityMapByEnum = new Dictionary<string, Commodity>();
            _commodityMapByString = new Dictionary<string, Commodity>();

            foreach (Commodity commodity in instance)
            {
                _commodityMapByEnum[commodity.ID] = commodity;
                _commodityMapByString[commodity.Name] = commodity;
            }

            return instance;
        }
    }
}

