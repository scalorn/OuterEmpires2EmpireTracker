using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new ship template.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class ShipTemplateCreateRequest
    {
        public string Name { get; set; }

        public string HullBlueprintUUID { get; set; }

        public List<ShipComponentSlot> Components { get; set; }
    }
}