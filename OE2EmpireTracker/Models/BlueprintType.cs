using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class BlueprintType
    {
        public BlueprintType()
        {
            Properties = new string[0];
            ResearchableProperties = new string[0];
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public bool Universal { get; set; }

        public string[] Properties { get; set; }

        public string[] ResearchableProperties { get; set; }

        /// <summary>
        /// The CSS sprite position of the icon used in the game UI for this blueprint type
        /// (e.g. "-328px -62px" for Hull). Used to map market listing icons to blueprint types.
        /// Null/empty means no icon mapping is known yet.
        /// </summary>
        public string IconPosition { get; set; }

        /// <summary>
        /// The ItemType.ItemTypeEnum name of the item produced when a blueprint
        /// of this type is manufactured (e.g. "ShipHull", "Flatpack", "ShipPart").
        /// Empty string means no output item is defined yet.
        /// </summary>
        public string OutputItemType { get; set; } = string.Empty;
    }
}
