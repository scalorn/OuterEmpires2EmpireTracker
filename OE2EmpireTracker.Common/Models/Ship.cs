using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public class Ship
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string TemplateUUID { get; internal set; } = string.Empty;
        public string HullBlueprintUUID { get; internal set; } = string.Empty;
        public List<ShipComponentSlot> Components { get; internal set; } = new List<ShipComponentSlot>();

        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType LocationType { get; internal set; } = DestinationType.Station;
        public string LocationUUID { get; internal set; } = string.Empty;

        public ItemBag Cargo { get; internal set; } = new ItemBag();
        public ItemBag Hopper { get; internal set; } = new ItemBag();

        public int HullCurrentHP { get; internal set; } = 0;
        public int HullMaxHP { get; internal set; } = 0;
        public decimal HullMaxRepairPercent { get; internal set; } = 0m;
    }
}
