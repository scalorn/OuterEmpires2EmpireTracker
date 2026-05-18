using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// A named, per-player pricing configuration containing base resource prices
    /// and optional time-cost parameters for computing item valuations.
    /// </summary>
    public class PricingPlan
    {
        public string UUID { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public string OwnerUUID { get; internal set; } = string.Empty;
        public string Description { get; internal set; } = string.Empty;
        public decimal FixedCostPerItem { get; internal set; } = 0m;
        public decimal HourlyCostRate { get; internal set; } = 0m;

        /// <summary>
        /// Base resource prices keyed by "{ResourceName}|{Purity}".
        /// Example: "Alkali Metals|Refined" -> 12.50m
        /// </summary>
        public Dictionary<string, decimal> ResourcePrices { get; internal set; }
            = new Dictionary<string, decimal>();
    }
}
