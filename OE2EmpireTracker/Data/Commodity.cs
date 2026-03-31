using Newtonsoft.Json;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime.Documents;
using Sgml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
        public Data.CommodityIndustry.CommodityIndustryEnum CommodityIndustry { get; set; }
        public Data.CommodityGroup.CommodityGroupEnum CommodityGroup { get; set; }

        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonIgnore]
        public string ExtendedName { 
            get {
                if (string.IsNullOrEmpty(Name))
                {
                    return "";
                }
                string extendedName = Name;

                CommodityIndustry industryResult = null;
                Data.CommodityIndustry.CommodityIndustryMapByEnum.TryGetValue(CommodityIndustry, out industryResult);
                if (industryResult != null)
                {
                    extendedName += $" ({industryResult.Name})";
                }

                CommodityGroup groupResult = null;
                Data.CommodityGroup.CommodityGroupMapByEnum.TryGetValue(CommodityGroup, out groupResult);
                if (groupResult != null)
                {
                    extendedName += $" [{groupResult.Name}]";
                }
                return extendedName;
            }
        }

        public Dictionary<string, string> ConstructionResources { get; set; } = new Dictionary<string, string>();


        private static List<Commodity> _commodities = getCommodities();
        private static Dictionary<string, Commodity> _commodityMapByEnum;
        private static Dictionary<string, Commodity> _commodityMapByString;

        public static IReadOnlyList<Commodity> Commodities => _commodities.AsReadOnly();
        public static IReadOnlyDictionary<string, Commodity> ResourceMapByEnum => _commodityMapByEnum;
        public static IReadOnlyDictionary<string, Commodity> ResourceMapByString => _commodityMapByString;


        public Commodity()
        {
        }

        private static List<Commodity> getCommodities()
        {
            List<Commodity> instance = new List<Commodity>();
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
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
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
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
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "AGI Archives",
                Name = "AGI Archives",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "AGI Manu-Augments",
                Name = "AGI Manu-Augments",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Agridome Ops Units",
                Name = "Agridome Ops Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "AI Finance Cubes",
                Name = "AI Finance Cubes",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "AI Inventory Systems",
                Name = "AI Inventory Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "AmmoMate Munitions Printer",
                Name = "AmmoMate Munitions Printer",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Aquacore Pumps",
                Name = "Aquacore Pumps",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Assemblatrons",
                Name = "Assemblatrons",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "AssembleMate Crucibles",
                Name = "AssembleMate Crucibles",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Atomsmasher Crucibles",
                Name = "Atomsmasher Crucibles",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Autonomous Trader Bots",
                Name = "Autonomous Trader Bots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "BattleCom Interconnects",
                Name = "BattleCom Interconnects",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Biochem Delivery Systems",
                Name = "Biochem Delivery Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Biochip Arrays",
                Name = "Biochip Arrays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Biomass Harvester Controllers",
                Name = "Biomass Harvester Controllers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Biosuspension Fluids",
                Name = "Biosuspension Fluids",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Biotech Vision Systems",
                Name = "Biotech Vision Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Brainwave Scintillators",
                Name = "Brainwave Scintillators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Branding Systems",
                Name = "Branding Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Calalyst Reaction Simulators",
                Name = "Calalyst Reaction Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Cargo Drones",
                Name = "Cargo Drones",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Caustic Fluids",
                Name = "Caustic Fluids",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "ChemLab Units",
                Name = "ChemLab Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Clean Air Units",
                Name = "Clean Air Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Climate Hubs",
                Name = "Climate Hubs",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Clothing Printers",
                Name = "Clothing Printers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Comfortcore Arrays",
                Name = "Comfortcore Arrays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Concentrax Containment Units",
                Name = "Concentrax Containment Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Conceptcore Drivers",
                Name = "Conceptcore Drivers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Confectionery",
                Name = "Confectionery",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Constructex Waldos",
                Name = "Constructex Waldos",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Containment Units",
                Name = "Containment Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Core Dyagnostic Component",
                Name = "Core Dyagnostic Component",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Coreseeker Guidance Bits",
                Name = "Coreseeker Guidance Bits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "CRM Units",
                Name = "CRM Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Cryogenic Coolant Tanks",
                Name = "Cryogenic Coolant Tanks",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Cryptochips",
                Name = "Cryptochips",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Data Accumulation Systems",
                Name = "Data Accumulation Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "DataScope Assemblies",
                Name = "DataScope Assemblies",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Datasphere Frames",
                Name = "Datasphere Frames",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "DegasTech Reforgers",
                Name = "DegasTech Reforgers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Delivery Systems",
                Name = "Delivery Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Demand Drivers",
                Name = "Demand Drivers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Design AGI Chassis",
                Name = "Design AGI Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Diagnocores",
                Name = "Diagnocores",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Dockmaster Drones",
                Name = "Dockmaster Drones",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Document Calibrators",
                Name = "Document Calibrators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Drillcore Chassis",
                Name = "Drillcore Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Earthmover Fabricators",
                Name = "Earthmover Fabricators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "EconoSim Units",
                Name = "EconoSim Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Efficiency Systems",
                Name = "Efficiency Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Energy Storage systems",
                Name = "Energy Storage systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Enriched Agar Gels",
                Name = "Enriched Agar Gels",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Entertainmate Interconnects",
                Name = "Entertainmate Interconnects",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Equity Analyzers",
                Name = "Equity Analyzers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Events AGI Chassis",
                Name = "Events AGI Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Exoskeleton Waldos",
                Name = "Exoskeleton Waldos",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Fermented Beverages",
                Name = "Fermented Beverages",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Filterator Chassis",
                Name = "Filterator Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Finance AutoMods",
                Name = "Finance AutoMods",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Fold Containers",
                Name = "Fold Containers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Funding Systems",
                Name = "Funding Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "G-Suites",
                Name = "G-Suites",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Gameonix Simulators",
                Name = "Gameonix Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Gene-Seed Packets",
                Name = "Gene-Seed Packets",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Genecore Processors",
                Name = "Genecore Processors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Defence,
                ID = "General Control Systems",
                Name = "General Control Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Transuranic Volatiles", "2" },
                    { "Superheavy Exotics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Geoengineering Slurry",
                Name = "Geoengineering Slurry",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Gigagro Fertilizers",
                Name = "Gigagro Fertilizers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Gravimeters",
                Name = "Gravimeters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Handheld Infrartek Units",
                Name = "Handheld Infrartek Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Haptic Equipment",
                Name = "Haptic Equipment",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Hazard Bunkers",
                Name = "Hazard Bunkers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Hazard Disposal Systems",
                Name = "Hazard Disposal Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Hazard Gear",
                Name = "Hazard Gear",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Health Scanners",
                Name = "Health Scanners",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Heavy Alloy Printers",
                Name = "Heavy Alloy Printers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Heavy Harvesters",
                Name = "Heavy Harvesters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Defence,
                ID = "Heavy Laser Drill Parts",
                Name = "Heavy Laser Drill Parts",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Transuranic Volatiles", "2" },
                    { "Superheavy Exotics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Defence,
                ID = "Heavy Mover Kits",
                Name = "Heavy Mover Kits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Transuranic Volatiles", "2" },
                    { "Superheavy Exotics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Heavy Robot Bays",
                Name = "Heavy Robot Bays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Defence,
                ID = "Heavy Unit Manipulators",
                Name = "Heavy Unit Manipulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Transuranic Volatiles", "2" },
                    { "Superheavy Exotics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "HEV Duty Exosuits",
                Name = "HEV Duty Exosuits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "HEV-Battledress",
                Name = "HEV-Battledress",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Hibernation Pods",
                Name = "Hibernation Pods",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "HoloDesks",
                Name = "HoloDesks",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Holomap Units",
                Name = "Holomap Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Hometech Supplies",
                Name = "Hometech Supplies",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Human Facilities",
                Name = "Human Facilities",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Hydrobots",
                Name = "Hydrobots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Hydroponics Quarters",
                Name = "Hydroponics Quarters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Hyper-Alloy Pliers",
                Name = "Hyper-Alloy Pliers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Ideaforge Interconnects",
                Name = "Ideaforge Interconnects",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Impact Analysis Archivers",
                Name = "Impact Analysis Archivers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Infonomicon Clusters",
                Name = "Infonomicon Clusters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Instrument Calibrators",
                Name = "Instrument Calibrators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Joybot Parts",
                Name = "Joybot Parts",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Labtrackers",
                Name = "Labtrackers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Livestock Links",
                Name = "Livestock Links",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Loader Smartlinks",
                Name = "Loader Smartlinks",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Logicon Units",
                Name = "Logicon Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Macroscope Chassis",
                Name = "Macroscope Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Marketlink Systems",
                Name = "Marketlink Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Marketpulse Readers",
                Name = "Marketpulse Readers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Mass Manipulators",
                Name = "Mass Manipulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Material Handling Plotters",
                Name = "Material Handling Plotters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Material Lances",
                Name = "Material Lances",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Matter Simulators",
                Name = "Matter Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Medicbot Operations Ports",
                Name = "Medicbot Operations Ports",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Memochips",
                Name = "Memochips",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Mess Hall Kits",
                Name = "Mess Hall Kits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Metallics Market Analyzers",
                Name = "Metallics Market Analyzers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Microarray Printers",
                Name = "Microarray Printers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Mineral Processors",
                Name = "Mineral Processors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Modular Building Units",
                Name = "Modular Building Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Modular Cargo Kits",
                Name = "Modular Cargo Kits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Modular Control Centres",
                Name = "Modular Control Centres",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Modular Storage Kits",
                Name = "Modular Storage Kits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Molecular Printer",
                Name = "Molecular Printer",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Molecule Assemblers",
                Name = "Molecule Assemblers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Monobonding Shields",
                Name = "Monobonding Shields",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Nano-Welding Tools",
                Name = "Nano-Welding Tools",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Nanobot Reservoirs",
                Name = "Nanobot Reservoirs",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Nanocure Supplies",
                Name = "Nanocure Supplies",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "NanoLab Units",
                Name = "NanoLab Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Nanomachine Swarmers",
                Name = "Nanomachine Swarmers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Narcotics",
                Name = "Narcotics",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Nervegear",
                Name = "Nervegear",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Netboost Nodes",
                Name = "Netboost Nodes",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Neural Interfaces",
                Name = "Neural Interfaces",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Neural Scanners",
                Name = "Neural Scanners",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "NeuroInterfaze Units",
                Name = "NeuroInterfaze Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Next-Gen Logibots",
                Name = "Next-Gen Logibots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Next-Gen Management Units",
                Name = "Next-Gen Management Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Next-Gen Sound Systems",
                Name = "Next-Gen Sound Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Noise Jammers",
                Name = "Noise Jammers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Note Beamers",
                Name = "Note Beamers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Oasis Control Bots",
                Name = "Oasis Control Bots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Ore Analyzers",
                Name = "Ore Analyzers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Oretech Systems",
                Name = "Oretech Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Organibots",
                Name = "Organibots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Particle Scanners",
                Name = "Particle Scanners",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Pathology Simulators",
                Name = "Pathology Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Pharmaceuticals Synthesizer",
                Name = "Pharmaceuticals Synthesizer",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Plasma Cutters",
                Name = "Plasma Cutters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Playport Displays",
                Name = "Playport Displays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Policy Governance Systems",
                Name = "Policy Governance Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Power Cell Casemates",
                Name = "Power Cell Casemates",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Processing Control Systems",
                Name = "Processing Control Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Projectors",
                Name = "Projectors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Proto-Alloys",
                Name = "Proto-Alloys",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Prototyper Systems",
                Name = "Prototyper Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Reactant Calibrators",
                Name = "Reactant Calibrators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Regenerative Nanobot Hives",
                Name = "Regenerative Nanobot Hives",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Resource Allocators",
                Name = "Resource Allocators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Rig Living Space Kits",
                Name = "Rig Living Space Kits",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "RiskMinder Bots",
                Name = "RiskMinder Bots",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Rockminer Drones",
                Name = "Rockminer Drones",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Safety Equipment",
                Name = "Safety Equipment",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Defence,
                ID = "Scanner Chassis",
                Name = "Scanner Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Transuranic Volatiles", "2" },
                    { "Superheavy Exotics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Sciencelink Housings",
                Name = "Sciencelink Housings",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Security Systems",
                Name = "Security Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Self-healing Materials",
                Name = "Self-healing Materials",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Self-Replicating Jigs",
                Name = "Self-Replicating Jigs",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Shift Hab Units",
                Name = "Shift Hab Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Silo Control Systems",
                Name = "Silo Control Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Sim-Meat Consignments",
                Name = "Sim-Meat Consignments",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Small Arms",
                Name = "Small Arms",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.RefiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Mining,
                ID = "Smart Conveyors",
                Name = "Smart Conveyors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Organics", "2" },
                    { "Lanthanide Volatiles", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Smart Systems",
                Name = "Smart Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Smart Toolsets",
                Name = "Smart Toolsets",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "SolarScythe Trimmers",
                Name = "SolarScythe Trimmers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Starshield Clothing",
                Name = "Starshield Clothing",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Stress Analyzers",
                Name = "Stress Analyzers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Supply Chain AGI Chassis",
                Name = "Supply Chain AGI Chassis",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Supply Keylocks",
                Name = "Supply Keylocks",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Survival Pods",
                Name = "Survival Pods",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Suspension Fluids",
                Name = "Suspension Fluids",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "TacticalNet C3 Systems",
                Name = "TacticalNet C3 Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Target Lock Relays",
                Name = "Target Lock Relays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Tasklinks",
                Name = "Tasklinks",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Telemedicine Consoles",
                Name = "Telemedicine Consoles",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Teleoffice Suites",
                Name = "Teleoffice Suites",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.MiningIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.HiTech,
                ID = "Telepresence Equipment",
                Name = "Telepresence Equipment",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Metals", "2" },
                    { "Heavy Noble Gases", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ManufacturingIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Testing Simulators",
                Name = "Testing Simulators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Research,
                ID = "Theoretical Freightlink Systems",
                Name = "Theoretical Freightlink Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Noble Gases", "2" },
                    { "Halogens", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Thrilltech Suites",
                Name = "Thrilltech Suites",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Engineering,
                ID = "Tissue Printers",
                Name = "Tissue Printers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Non-Metallics", "2" },
                    { "Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Tooltech Printers",
                Name = "Tooltech Printers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Medical,
                ID = "Trauma Systems",
                Name = "Trauma Systems",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Alkali Metals", "2" },
                    { "Lanthanides", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.CentreOfEconomics,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Trend AGI Projectors",
                Name = "Trend AGI Projectors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.HealthcareInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Triarge Facilities",
                Name = "Triarge Facilities",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Valuation Vaults",
                Name = "Valuation Vaults",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.ScienceCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "VAST Arrays",
                Name = "VAST Arrays",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.EngineeringBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Logistics,
                ID = "Vehicle Bolts",
                Name = "Vehicle Bolts",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Metaloids", "2" },
                    { "Complex Non-Metallics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Virtual Assistants",
                Name = "Virtual Assistants",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LeisureIndustryCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Economy,
                ID = "Vrtrek Units",
                Name = "Vrtrek Units",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Alkali Inorganics", "2" },
                    { "Heavy Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.TechnologyInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Refining,
                ID = "Waffers",
                Name = "Waffers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Strong Acidic Inorganics", "2" },
                    { "Heavy Alkaline Earth Metals", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Warehouse AutoMods",
                Name = "Warehouse AutoMods",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Warehouse Control Cabins",
                Name = "Warehouse Control Cabins",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.InstituteOfDefence,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "WarTech Housings",
                Name = "WarTech Housings",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.OffWorldLivingInstitute,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Agriculture,
                ID = "Waste Management Fluids",
                Name = "Waste Management Fluids",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Organics", "2" },
                    { "Acidic Organics", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Manufacturing,
                ID = "Water-Hive Irrigators",
                Name = "Water-Hive Irrigators",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Light Halogens", "2" },
                    { "Alkaline Earth Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.LogisticsCentre,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Waypointers",
                Name = "Waypointers",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.Agridome,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Administration,
                ID = "Weathermate Interconnects",
                Name = "Weathermate Interconnects",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Trans-Metals", "2" },
                    { "Post-Trans Metals", "2" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Leisure,
                ID = "Welfare Risk Assessors",
                Name = "Welfare Risk Assessors",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Heavy Trans-Metals", "2" },
                    { "Complex Metallics", "1" }
                }
            });
            instance.Add(new Commodity()
            {
                CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.AdministrationBlock,
                CommodityGroup = Data.CommodityGroup.CommodityGroupEnum.Habitation,
                ID = "Workflow Automaters",
                Name = "Workflow Automaters",
                ConstructionResources = new Dictionary<string, string>()
                {
                    { "Alkali Inorganics", "2" },
                    { "Acidic Inorganics", "2" }
                }
            });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            //instance.Insert(0, new Commodity() { CommodityIndustry = Data.CommodityIndustry.CommodityIndustryEnum.None,  ID = "", Name = ""});

            _commodityMapByEnum = new Dictionary<string, Commodity>();
            _commodityMapByString = new Dictionary<string, Commodity>();

            foreach (Commodity commodity in instance)
            {
                if (commodity.CommodityIndustry == Data.CommodityIndustry.CommodityIndustryEnum.None)
                {
                    Debug.Print($"Commodity with name {commodity.Name} has no industry configured!");
                }
                if (commodity.CommodityGroup == Data.CommodityGroup.CommodityGroupEnum.None)
                {
                    Debug.Print($"Commodity with name {commodity.Name} has no group configured!");
                }
                foreach (KeyValuePair<string, string> constructionResource in commodity.ConstructionResources)
                {
                    string resourceName = constructionResource.Key;
                    string resourceAmount = constructionResource.Value;
                    if (string.IsNullOrEmpty(constructionResource.Value))
                    {
                        Debug.Print($"Commodity with name {commodity.Name} and resource {resourceName} not fully configured!");
                        continue;
                    }

                    Resource resource = null;
                    if (!Resource.ResourceMapByString.TryGetValue(resourceName, out resource))
                    {
                        throw new Exception($"Failed to find resource with name {resourceName} for commodity {commodity.Name}");
                    }

                    int amount = 0;
                    if (!int.TryParse(resourceAmount, out amount))
                    {
                        throw new Exception($"Failed to parse amount with value {resourceAmount} for commodity {commodity.Name}");
                    }
                }
                _commodityMapByEnum[commodity.ID] = commodity;
                _commodityMapByString[commodity.Name] = commodity;
            }

            return instance;
        }
    }
}

