using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class StockProfile
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<StockProfileEntry> Entries { get; set; } = new List<StockProfileEntry>();
    }

    public class StockProfileEntry
    {
        public string GroupID { get; set; } = string.Empty;
        public string StockPlanUUID { get; set; } = string.Empty;
    }
}
