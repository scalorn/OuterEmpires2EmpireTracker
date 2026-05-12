using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OE2EmpireTracker.Models
{
    public enum StationType
    {
        Outpost,
        Station,
        Starbase
    }

    public enum StationOwnership
    {
        Government,
        PlayerOwned
    }

    public class Station
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter))]
        public StationType StationType { get; set; } = Models.StationType.Station;

        [JsonConverter(typeof(StringEnumConverter))]
        public StationOwnership Ownership { get; set; } = StationOwnership.Government;

        public string OwnerUUID { get; set; } = string.Empty;

        public Dictionary<string, ItemBag> Holds { get; set; } = new Dictionary<string, ItemBag>();

        public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();
        public string StationBlueprintUUID { get; set; } = string.Empty;

        public ItemBag MunitionsHold { get; set; } = new ItemBag();

        public int HullCurrentHP { get; set; } = 0;
        public int HullMaxHP { get; set; } = 0;
        public decimal HullMaxRepairPercent { get; set; } = 0m;
    }
}
