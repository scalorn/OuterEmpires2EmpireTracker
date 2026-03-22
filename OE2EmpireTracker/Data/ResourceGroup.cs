using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Data
{
    public class ResourceGroup
    {
        public enum ResourceGroupEnum
        {
            None = 0,
            Crystalline,
            Exotic,
            Inert,
            Metallic,
            NonCarbon,
            Organic,
            Reactive,
            Volatile,
            Synthetic
        }

        public ResourceGroupEnum ID { get; set; }
        public string Name { get; set; }
        public bool Synthetic { get; set; } = false;

        private static List<ResourceGroup> _groups = getGroups();
        private static Dictionary<ResourceGroupEnum, ResourceGroup> _groupMapByEnum;
        private static Dictionary<string, ResourceGroup> _groupMapByString;

        public static IReadOnlyList<ResourceGroup> Groups => _groups.AsReadOnly();
        public static IReadOnlyDictionary<ResourceGroupEnum, ResourceGroup> ResourceGroupMapByEnum => _groupMapByEnum;
        public static IReadOnlyDictionary<string, ResourceGroup> ResourceGroupMapByString => _groupMapByString;


        private static List<ResourceGroup> getGroups()
        {
            List<ResourceGroup> instance = new List<ResourceGroup>();
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Crystalline, Name = "Crystalline" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Exotic, Name = "Exotic" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Inert, Name = "Inert" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Metallic, Name = "Metallic" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.NonCarbon, Name = "Non-Carbon" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Organic, Name = "Organic" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Reactive, Name = "Reactive" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Volatile, Name = "Volatile" });
            instance.Add(new ResourceGroup() { ID = ResourceGroupEnum.Synthetic, Name = "Synthetic", Synthetic = true });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new ResourceGroup() { ID = ResourceGroupEnum.None, Name = "" });

            _groupMapByEnum = new Dictionary<ResourceGroupEnum, ResourceGroup>();
            _groupMapByString = new Dictionary<string, ResourceGroup>();

            foreach (ResourceGroup group in instance)
            {
                _groupMapByEnum[group.ID] = group;
                _groupMapByString[group.Name] = group;
            }

            return instance;
        }
    }
}
