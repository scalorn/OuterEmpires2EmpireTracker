using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class ShipTemplate
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string HullBlueprintUUID { get; set; } = string.Empty;
        public List<ShipComponentSlot> Components { get; set; } = new List<ShipComponentSlot>();
    }

    public class ShipComponentSlot
    {
        public string SlotType { get; set; } = string.Empty;
        public int SlotIndex { get; set; } = 0;
        public string BlueprintUUID { get; set; } = string.Empty;

        public int CurrentHP { get; set; } = 0;
        public int MaxHP { get; set; } = 0;
        public decimal MaxRepairPercent { get; set; } = 0m;
    }
}
