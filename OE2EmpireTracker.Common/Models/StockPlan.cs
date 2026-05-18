using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class StockPlan
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string ReplenishmentBuildPlanUUID { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;
        public List<StockTarget> Targets { get; internal set; } = new List<StockTarget>();
    }
}
