using OE2EmpireTracker.Interfaces;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Game-derived constants that are externalized into BaselineData.json
    /// so users can fix game balance changes without code modifications.
    /// </summary>
    public class BaselineGameConstants : IGameConfig
    {
        public int RefiningBaseRate { get; set; } = 25;
        public int CommoditiesPerCycle { get; set; } = 10;
        public long CommodityCycleSeconds { get; set; } = 600;
        public int StructureCap { get; set; } = 65;
        public decimal WorkerVolume { get; set; } = 50m;
    }
}
