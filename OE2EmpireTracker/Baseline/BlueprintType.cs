using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Baseline
{
    public class BlueprintType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Boolean Universal { get; set; }
        public string[] Properties { get; set; }
        public string[] ResearchableProperties { get; set; }

        /// <summary>
        /// The ItemType.ItemTypeEnum name of the item produced when a blueprint
        /// of this type is manufactured (e.g. "ShipHull", "Flatpack", "ShipPart").
        /// Empty string means no output item is defined yet.
        /// </summary>
        public string OutputItemType { get; set; } = string.Empty;

        public BlueprintType() {
            Properties = new string[0];
            ResearchableProperties = new string[0];
        }
    }
}
