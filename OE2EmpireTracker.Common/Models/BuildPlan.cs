using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class BuildPlan
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DeliveryPlanUUID { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<BuildItem> Items { get; set; } = new List<BuildItem>();
    }
}
