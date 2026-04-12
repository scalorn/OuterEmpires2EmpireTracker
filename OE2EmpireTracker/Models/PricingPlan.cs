using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// A named, per-player pricing configuration containing base resource prices
    /// and optional time-cost parameters for computing item valuations.
    /// </summary>
    public class PricingPlan
    {
        public string UUID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerUUID { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal FixedCostPerItem { get; set; } = 0m;
        public decimal HourlyCostRate { get; set; } = 0m;

        /// <summary>
        /// Base resource prices keyed by "{ResourceName}|{Purity}".
        /// Example: "Alkali Metals|Refined" → 12.50m
        /// </summary>
        public Dictionary<string, decimal> ResourcePrices { get; set; }
            = new Dictionary<string, decimal>();
    }
}
