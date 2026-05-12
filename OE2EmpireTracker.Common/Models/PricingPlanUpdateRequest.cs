using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing pricing plan.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class PricingPlanUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyPricingPlan Original { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal FixedCostPerItem { get; set; }

        public decimal HourlyCostRate { get; set; }

        public Dictionary<string, decimal> ResourcePrices { get; set; }
    }
}