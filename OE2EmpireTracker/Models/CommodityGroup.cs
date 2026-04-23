using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class CommodityGroup
    {
        public enum CommodityGroupEnum
        {
            None = 0,
            Administration,
            Agriculture,
            Economy,
            Engineering,
            Medical,
            Defence,
            Leisure,
            Logistics,
            Manufacturing,
            Mining,
            Habitation,
            Refining,
            Research,
            HiTech
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
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Administration, Name = "Administration" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Agriculture, Name = "Agriculture" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Economy, Name = "Economy" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Engineering, Name = "Engineering" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Medical, Name = "Medical" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Defence, Name = "Defence" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Leisure, Name = "Leisure" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Logistics, Name = "Logistics" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Manufacturing, Name = "Manufacturing" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Mining, Name = "Mining" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Habitation, Name = "Habitation" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Refining, Name = "Refining" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.Research, Name = "Research" });
            instance.Add(new CommodityGroup() { ID = CommodityGroupEnum.HiTech, Name = "Hi-Tech" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new CommodityGroup() { ID = CommodityGroupEnum.None, Name = string.Empty });

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
