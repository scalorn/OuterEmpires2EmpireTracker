using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class CommodityIndustry
    {
        public enum CommodityIndustryEnum
        {
            None = 0,
            AdministrationBlock,
            Agridome,
            CentreOfEconomics,
            EngineeringBlock,
            HealthcareInstitute,
            InstituteOfDefence,
            LeisureIndustryCentre,
            LogisticsCentre,
            ManufacturingIndustryCentre,
            MiningIndustryCentre,
            OffWorldLivingInstitute,
            RefiningIndustryCentre,
            ScienceCentre,
            TechnologyInstitute
        }

        public CommodityIndustryEnum ID { get; set; } = CommodityIndustryEnum.None;
        public string Name { get; set; } =  string.Empty;

        private static List<CommodityIndustry> _commodityIndustries = getCommodityIndustries();
        private static Dictionary<CommodityIndustryEnum, CommodityIndustry> _commodityIndustryMapByEnum;
        private static Dictionary<string, CommodityIndustry> _commodityIndustryMapByString;

        public static IReadOnlyList<CommodityIndustry> Groups => _commodityIndustries.AsReadOnly();
        public static IReadOnlyDictionary<CommodityIndustryEnum, CommodityIndustry> CommodityIndustryMapByEnum => _commodityIndustryMapByEnum;
        public static IReadOnlyDictionary<string, CommodityIndustry> CommodityIndustryMapByString => _commodityIndustryMapByString;


        private static List<CommodityIndustry> getCommodityIndustries()
        {
            List<CommodityIndustry> instance = new List<CommodityIndustry>();
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.AdministrationBlock, Name = "Administration Block" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.Agridome, Name = "Agridome" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.CentreOfEconomics, Name = "Centre of Economics" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.EngineeringBlock, Name = "Engineering Block" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.HealthcareInstitute, Name = "Healthcare Institute" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.InstituteOfDefence, Name = "Institute of Defence" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.LeisureIndustryCentre, Name = "Leisure Industry Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.LogisticsCentre, Name = "Logistics Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.ManufacturingIndustryCentre, Name = "Manufacturing Industry Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.MiningIndustryCentre, Name = "Mining Industry Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.OffWorldLivingInstitute, Name = "OffWorld Living Institute" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.RefiningIndustryCentre, Name = "Refining Industry Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.ScienceCentre, Name = "Science Centre" });
            instance.Add(new CommodityIndustry() { ID = CommodityIndustryEnum.TechnologyInstitute, Name = "Technology Institute" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new CommodityIndustry() { ID = CommodityIndustryEnum.None, Name = "" });

            _commodityIndustryMapByEnum = new Dictionary<CommodityIndustryEnum, CommodityIndustry>();
            _commodityIndustryMapByString = new Dictionary<string, CommodityIndustry>();

            foreach (CommodityIndustry commodityIndustry in instance)
            {
                _commodityIndustryMapByEnum[commodityIndustry.ID] = commodityIndustry;
                _commodityIndustryMapByString[commodityIndustry.Name] = commodityIndustry;
            }

            return instance;
        }
    }
}