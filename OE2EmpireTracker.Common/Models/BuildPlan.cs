using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class BuildPlan
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string Description { get; internal set; } = string.Empty;
        public string DeliveryPlanUUID { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;
        public List<BuildItem> Items { get; internal set; } = new List<BuildItem>();
    }
}
