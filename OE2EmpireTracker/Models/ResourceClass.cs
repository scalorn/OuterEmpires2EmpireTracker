using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OE2EmpireTracker.Models.ResourcePurity;

namespace OE2EmpireTracker.Models
{
    public class ResourceClass
    {
        private static List<ResourceClass> _classes = GetClasses();

        private static Dictionary<ResourceClassEnum, ResourceClass> _classMapByEnum;

        private static Dictionary<string, ResourceClass> _classMapByString;

        public enum ResourceClassEnum
        {
            None,
            CommonElements,
            UncommonElements,
            RareElements,
            VeryRareElements,
            SyntheticElements,
        }

        public static IReadOnlyList<ResourceClass> Classes => _classes.AsReadOnly();

        public static IReadOnlyDictionary<ResourceClassEnum, ResourceClass> ClassMapByEnum => _classMapByEnum;

        public static IReadOnlyDictionary<string, ResourceClass> ClassMapByString => _classMapByString;

        public ResourceClassEnum ID { get; set; }

        public string Name { get; set; }

        private static List<ResourceClass> GetClasses()
        {
            List<ResourceClass> instance = new List<ResourceClass>();
            instance.Add(new ResourceClass() { ID = ResourceClassEnum.CommonElements, Name = "Common Elements" });
            instance.Add(new ResourceClass() { ID = ResourceClassEnum.UncommonElements, Name = "Uncommon Elements" });
            instance.Add(new ResourceClass() { ID = ResourceClassEnum.RareElements, Name = "Rare Elements" });
            instance.Add(new ResourceClass() { ID = ResourceClassEnum.VeryRareElements, Name = "Very Rare Elements" });
            instance.Add(new ResourceClass() { ID = ResourceClassEnum.SyntheticElements, Name = "Synthetic Elements" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new ResourceClass() { ID = ResourceClassEnum.None, Name = string.Empty });

            _classMapByEnum = new Dictionary<ResourceClassEnum, ResourceClass>();
            _classMapByString = new Dictionary<string, ResourceClass>();

            foreach (ResourceClass resourceClass in instance)
            {
                _classMapByEnum[resourceClass.ID] = resourceClass;
                _classMapByString[resourceClass.Name] = resourceClass;
            }

            return instance;
        }
    }
}
