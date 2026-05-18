using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class ShipTemplate
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string HullBlueprintUUID { get; internal set; } = string.Empty;
        public List<ShipComponentSlot> Components { get; internal set; } = new List<ShipComponentSlot>();
    }

    public class ShipComponentSlot
    {
        public string SlotType { get; internal set; } = string.Empty;
        public int SlotIndex { get; internal set; } = 0;
        public string BlueprintUUID { get; internal set; } = string.Empty;

        public int CurrentHP { get; internal set; } = 0;
        public int MaxHP { get; internal set; } = 0;
        public decimal MaxRepairPercent { get; internal set; } = 0m;
    }
}
