using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class CommodityGroup
    {
        public enum CommodityGroupEnum
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

        public CommodityGroupEnum ID { get; set; } = CommodityGroupEnum.None;
        public string Name { get; set; } =  string.Empty;

        private static List<CommodityGroup> _commodityGroups = getCommodityGroups();
        private static Dictionary<CommodityGroupEnum, CommodityGroup> _commodityGroupMapByEnum;
        private static Dictionary<string, CommodityGroup> _commodityGroupMapByString;

        public static IReadOnlyList<CommodityGroup> Groups => _commodityGroups.AsReadOnly();
        public static IReadOnlyDictionary<CommodityGroupEnum, CommodityGroup> CommodityGroupMapByEnum => _commodityGroupMapByEnum;
        public static IReadOnlyDictionary<string, CommodityGroup> CommodityGroupMapByString => _commodityGroupMapByString;


        private static List<CommodityGroup> getCommodityGroups()
        {
            List<CommodityGroup> instance = new List<CommodityGroup>();
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.AdministrationBlock, Name = "Administration Block" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Agridome, Name = "Agridome" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.CentreOfEconomics, Name = "Centre of Economics" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.EngineeringBlock, Name = "Engineering Block" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.HealthcareInstitute, Name = "Healthcare Institute" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.LeisureIndustryCentre, Name = "Leisure Industry Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.LogisticsCentre, Name = "Logistics Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.ManufacturingIndustryCentre, Name = "Manufacturing Industry Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.MiningIndustryCentre, Name = "Mining Industry Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.OffWorldLivingInstitute, Name = "OffWorld Living Institute" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.RefiningIndustryCentre, Name = "Refining Industry Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.ScienceCentre, Name = "Science Centre" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.TechnologyInstitute, Name = "Technology Institute" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new CommodityGroup() { ID = CommodityGroupEnum.None, Name = "" });

            _commodityGroupMapByEnum = new Dictionary<CommodityGroupEnum, CommodityGroup>();
            _commodityGroupMapByString = new Dictionary<string, CommodityGroup>();

            foreach (CommodityGroup commodityGroup in instance)
            {
                _commodityGroupMapByEnum[commodityGroup.ID] = commodityGroup;
                _commodityGroupMapByString[commodityGroup.Name] = commodityGroup;
            }

            return instance;
        }
    }
}
