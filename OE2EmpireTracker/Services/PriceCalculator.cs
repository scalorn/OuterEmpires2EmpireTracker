using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service that computes commodity and blueprint prices
    /// from a PricingPlan's base resource prices.
    /// </summary>
    public static class PriceCalculator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Builds the composite key used in PricingPlan.ResourcePrices.
        /// </summary>
        public static string MakeResourceKey(string resourceName, string purity)
        {
            return $"{resourceName}|{purity}";
        }

        /// <summary>
        /// Determines the purity tier from a resource name.
        /// "S1. " prefix → "S1", "S2. " prefix → "S2", otherwise "Refined".
        /// </summary>
        public static string DeterminePurity(string resourceName)
        {
            if (resourceName != null && resourceName.StartsWith("S1. "))
                return "S1";
            if (resourceName != null && resourceName.StartsWith("S2. "))
                return "S2";
            return "Refined";
        }

        /// <summary>
        /// Looks up a single resource price from the plan's ResourcePrices dictionary.
        /// Returns true if found (including zero), false if absent.
        /// </summary>
        public static bool TryGetResourcePrice(PricingPlan plan, string resourceName, string purity, out decimal price)
        {
            price = 0m;
            if (plan?.ResourcePrices == null)
                return false;
            var key = MakeResourceKey(resourceName, purity);
            return plan.ResourcePrices.TryGetValue(key, out price);
        }

        /// <summary>
        /// Computes the price of a commodity from its ConstructionResources.
        /// All commodity inputs use Refined purity.
        /// </summary>
        public static ComputedPrice ComputeCommodityPrice(PricingPlan plan, Commodity commodity)
        {
            var result = new ComputedPrice { Price = 0m, IsComplete = true };

            if (commodity?.ConstructionResources == null || commodity.ConstructionResources.Count == 0)
                return result;

            foreach (var entry in commodity.ConstructionResources)
            {
                var resourceName = entry.Key;
                var purity = DeterminePurity(resourceName);

                int quantity;
                if (!int.TryParse(entry.Value, out quantity))
                {
                    Log.Warn($"Unparseable quantity '{entry.Value}' for resource '{resourceName}' in commodity '{commodity.Name}'; skipping.");
                    result.IsComplete = false;
                    continue;
                }

                decimal price;
                if (TryGetResourcePrice(plan, resourceName, purity, out price))
                {
                    result.Price += quantity * price;
                }
                else
                {
                    result.IsComplete = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Computes the price of a manufactured item from its blueprint resources
        /// plus optional time costs (FixedCostPerItem + HourlyCostRate × hours).
        /// </summary>
        public static ComputedPrice ComputeBlueprintPrice(PricingPlan plan, Blueprint blueprint, decimal manufacturingHours)
        {
            var result = new ComputedPrice { Price = 0m, IsComplete = true };

            if (blueprint?.Resources != null)
            {
                foreach (var entry in blueprint.Resources)
                {
                    var resourceName = entry.Key;
                    var purity = DeterminePurity(resourceName);

                    int quantity;
                    if (!int.TryParse(entry.Value, out quantity))
                    {
                        Log.Warn($"Unparseable quantity '{entry.Value}' for resource '{resourceName}' in blueprint '{blueprint.Name}'; skipping.");
                        result.IsComplete = false;
                        continue;
                    }

                    decimal price;
                    if (TryGetResourcePrice(plan, resourceName, purity, out price))
                    {
                        result.Price += quantity * price;
                    }
                    else
                    {
                        result.IsComplete = false;
                    }
                }
            }

            if (plan != null)
            {
                result.Price += plan.FixedCostPerItem;
                result.Price += plan.HourlyCostRate * manufacturingHours;
            }

            return result;
        }
    }
}
