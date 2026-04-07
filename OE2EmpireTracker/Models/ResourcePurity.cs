using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class ResourcePurity
    {
        public enum PurityEnum
        {
            None = 0,
            Refined,
            UnrefinedHigh,
            UnrefinedMedium,
            UnrefinedLow
        }

        public PurityEnum ID { get; set; }
        public string Name { get; set; }
        public bool Refined { get; set; } = false;

        private static List<ResourcePurity> _purities = getPurities();
        private static Dictionary<PurityEnum, ResourcePurity> _purityMapByEnum;
        private static Dictionary<string, ResourcePurity> _purityMapByString;

        public static IReadOnlyList<ResourcePurity> Purities => _purities.AsReadOnly();
        public static IReadOnlyDictionary<PurityEnum, ResourcePurity> ItemTypeMapByEnum => _purityMapByEnum;
        public static IReadOnlyDictionary<string, ResourcePurity> ItemTypeMapByString => _purityMapByString;


        private static List<ResourcePurity> getPurities()
        {
            List<ResourcePurity> instance = new List<ResourcePurity>();
            instance.Add(new ResourcePurity() { ID = PurityEnum.Refined, Name = "Refined", Refined = true });
            instance.Add(new ResourcePurity() { ID = PurityEnum.UnrefinedHigh, Name = "High" });
            instance.Add(new ResourcePurity() { ID = PurityEnum.UnrefinedMedium, Name = "Medium" });
            instance.Add(new ResourcePurity() { ID = PurityEnum.UnrefinedLow, Name = "Low" });

            instance.Sort((x, y) => x.Name.CompareTo(y.Name));

            // Make sure the blank none entry is first.
            instance.Insert(0, new ResourcePurity() { ID = PurityEnum.None, Name = "" });

            _purityMapByEnum = new Dictionary<PurityEnum, ResourcePurity>();
            _purityMapByString = new Dictionary<string, ResourcePurity>();

            foreach (ResourcePurity purity in instance)
            {
                _purityMapByEnum[purity.ID] = purity;
                _purityMapByString[purity.Name] = purity;
            }

            return instance;
        }
    }
}
