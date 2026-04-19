using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class StockPlan
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string ReplenishmentBuildPlanUUID { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<StockTarget> Targets { get; set; } = new List<StockTarget>();
    }
}
