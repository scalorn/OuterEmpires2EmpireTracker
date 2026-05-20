using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class Ship
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string TemplateUUID { get; set; } = string.Empty;
        public string HullBlueprintUUID { get; set; } = string.Empty;
        public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();

        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType LocationType { get; set; } = DestinationType.Station;
        public string LocationUUID { get; set; } = string.Empty;

        public ItemBag Cargo { get; set; } = new ItemBag();
        public ItemBag Hopper { get; set; } = new ItemBag();

        public int HullCurrentHP { get; set; } = 0;
        public int HullMaxHP { get; set; } = 0;
        public decimal HullMaxRepairPercent { get; set; } = 0m;
    }
}
