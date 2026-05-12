using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new stock plan.
    /// No Original snapshot (it doesn't exist yet). No UUID (the service assigns it).
    /// No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class StockPlanCreateRequest
    {
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public string ReplenishmentBuildPlanUUID { get; set; }

        public List<StockTarget> Targets { get; set; }
    }
}
