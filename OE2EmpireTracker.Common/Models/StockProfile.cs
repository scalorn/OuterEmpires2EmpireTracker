using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class StockProfile
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public bool IsActive { get; internal set; } = true;
        public List<StockProfileEntry> Entries { get; internal set; } = new List<StockProfileEntry>();
    }

    public class StockProfileEntry
    {
        public string GroupID { get; internal set; } = string.Empty;
        public string StockPlanUUID { get; internal set; } = string.Empty;
    }
}
