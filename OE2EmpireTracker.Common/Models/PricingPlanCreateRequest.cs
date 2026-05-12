using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new pricing plan.
    /// No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).
    /// </summary>
    public class PricingPlanCreateRequest
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public decimal FixedCostPerItem { get; set; }

        public decimal HourlyCostRate { get; set; }

        public Dictionary<string, decimal> ResourcePrices { get; set; }
    }
}
