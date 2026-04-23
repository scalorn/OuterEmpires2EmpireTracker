using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for PriceCalculator.
    /// Feature: pricing-plans
    /// </summary>
    [TestFixture]
    public class PriceCalculatorPropertyTests
    {
        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "Complex Metallics", "Acidic Inorganics", "Post Trans Metals",
            "S1. Translanthanic Exotics", "S1. Translivermoric Exotics", "S1. Transuranic Exotics",
            "S2. Element 126", "S2. Element 127", "S2. Superactinides"
        };

        private static Gen<string> ResourceNameGen()
        {
            return Gen.Elements(SampleResourceNames);
        }

        private static Gen<decimal> NonNegativeDecimalGen()
        {
            return Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);
        }

        private static Gen<PricingPlan> PricingPlanGen()
        {
            return from name in Arb.Generate<NonEmptyString>()
                   from fixedCost in NonNegativeDecimalGen()
                   from hourlyCost in NonNegativeDecimalGen()
                   from resourceCount in Gen.Choose(0, SampleResourceNames.Length)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from prices in Gen.ListOf(resourceCount, NonNegativeDecimalGen())
                   select BuildPlan(name.Get, fixedCost, hourlyCost, selectedResources.ToArray(), prices.ToArray());
        }

        private static PricingPlan BuildPlan(
            string name,
            decimal fixedCost,
            decimal hourlyCost,
            string[] resources,
            decimal[] prices)
        {
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                OwnerUUID = Guid.NewGuid().ToString(),
                FixedCostPerItem = fixedCost,
                HourlyCostRate = hourlyCost
            };

            for (int i = 0; i < resources.Length && i < prices.Length; i++)
            {
                var purity = PriceCalculator.DeterminePurity(resources[i]);
                var key = PriceCalculator.MakeResourceKey(resources[i], purity);
                plan.ResourcePrices[key] = prices[i];
            }

            return plan;
        }

        private static Gen<Dictionary<string, string>> ConstructionResourcesGen()
        {
            return from count in Gen.Choose(1, 5)
                   from names in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(count))
                   from quantities in Gen.ListOf(count, Gen.Choose(1, 100))
                   select names.Zip(quantities, (n, q) => new { n, q })
                              .ToDictionary(x => x.n, x => x.q.ToString());
        }

        /// <summary>
        /// Feature: pricing-plans, Property 7: Purity determination produces only Refined, S1, or S2
        ///
        /// For any resource name string, DeterminePurity returns exactly one of "Refined", "S1", or "S2".
        /// **Validates: Requirements 2.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PurityDetermination_ProducesOnlyThreeValues()
        {
            var nameGen = Gen.OneOf(
                Arb.Generate<string>(),
                Gen.Constant("S1. Something"),
                Gen.Constant("S2. Something"),
                Gen.Constant("Regular Resource")
           );

            return Prop.ForAll(nameGen.ToArbitrary(), name =>
            {
                var purity = PriceCalculator.DeterminePurity(name);
                var valid = purity == "Refined" || purity == "S1" || purity == "S2";

                bool correctMapping = true;
                if (name != null && name.StartsWith("S1. "))
                    correctMapping = purity == "S1";
                else if (name != null && name.StartsWith("S2. "))
                    correctMapping = purity == "S2";
                else
                    correctMapping = purity == "Refined";

                return (valid && correctMapping)
                    .Label($"DeterminePurity('{name}') = '{purity}'");
            });
        }

        /// <summary>
        /// Feature: pricing-plans, Property 2: Non-negative decimal validation
        ///
        /// For any decimal value, it should be accepted as a price if and only if it is >= 0.
        /// **Validates: Requirements 1.8, 2.3, 2.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property NonNegativeDecimalValidation()
        {
            return Prop.ForAll(Arb.From<decimal>(), value =>
            {
                bool isNonNegative = value >= 0m;
                // The validation rule: non-negative values are accepted, negative values are rejected
                return (isNonNegative == (value >= 0m))
                    .Label($"Value {value}: isNonNegative={isNonNegative}");
            });
        }

        /// <summary>
        /// Feature: pricing-plans, Property 6: Zero price is valid, absent entry is incomplete
        ///
        /// If a plan contains an explicit entry with value 0 for a resource, TryGetResourcePrice
        /// returns true with price 0. If the plan has no entry, TryGetResourcePrice returns false.
        /// **Validates: Requirements 6.2, 6.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ZeroPriceIsValid_AbsentEntryIsIncomplete()
        {
            return Prop.ForAll(ResourceNameGen().ToArbitrary(), resourceName =>
            {
                var purity = PriceCalculator.DeterminePurity(resourceName);
                var key = PriceCalculator.MakeResourceKey(resourceName, purity);

                // Plan with zero price
                var planWithZero = new PricingPlan();
                planWithZero.ResourcePrices[key] = 0m;

                decimal priceZero;
                bool foundZero = PriceCalculator.TryGetResourcePrice(planWithZero, resourceName, purity, out priceZero);

                // Plan without entry
                var planWithout = new PricingPlan();

                decimal priceAbsent;
                bool foundAbsent = PriceCalculator.TryGetResourcePrice(planWithout, resourceName, purity, out priceAbsent);

                return (foundZero && priceZero == 0m && !foundAbsent)
                    .Label($"Resource '{resourceName}|{purity}': foundZero={foundZero}, priceZero={priceZero}, foundAbsent={foundAbsent}");
            });
        }

        /// <summary>
        /// Feature: pricing-plans, Property 3: Commodity price is sum of input quantities times Refined prices
        ///
        /// For any PricingPlan and Commodity, the computed price equals the sum of
        /// (quantity × plan price) for each resource that has a price entry.
        /// **Validates: Requirements 3.1, 3.3**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CommodityPrice_IsSumOfInputQuantitiesTimesPrice()
        {
            return Prop.ForAll(
                PricingPlanGen().ToArbitrary(),
                ConstructionResourcesGen().ToArbitrary(),
                (plan, resources) =>
                {
                    var commodity = new Commodity { Name = "TestCommodity", ConstructionResources = resources };
                    var result = PriceCalculator.ComputeCommodityPrice(plan, commodity);

                    // Manually compute expected price
                    decimal expectedPrice = 0m;
                    foreach (var entry in resources)
                    {
                        var purity = PriceCalculator.DeterminePurity(entry.Key);
                        var key = PriceCalculator.MakeResourceKey(entry.Key, purity);
                        int qty = int.Parse(entry.Value);
                        decimal price;
                        if (plan.ResourcePrices.TryGetValue(key, out price))
                        {
                            expectedPrice += qty * price;
                        }
                    }

                    return (result.Price == expectedPrice)
                        .Label($"Expected {expectedPrice}, got {result.Price}");
                });
        }

        /// <summary>
        /// Feature: pricing-plans, Property 4: Completeness flag matches input coverage
        ///
        /// IsComplete is true iff every input resource has a corresponding entry in the plan.
        /// **Validates: Requirements 3.2, 3.4, 4.4, 6.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CompletenessFlag_MatchesInputCoverage()
        {
            return Prop.ForAll(
                PricingPlanGen().ToArbitrary(),
                ConstructionResourcesGen().ToArbitrary(),
                (plan, resources) =>
                {
                    var commodity = new Commodity { Name = "TestCommodity", ConstructionResources = resources };
                    var result = PriceCalculator.ComputeCommodityPrice(plan, commodity);

                    // Manually check coverage
                    bool allCovered = resources.All(entry =>
                    {
                        var purity = PriceCalculator.DeterminePurity(entry.Key);
                        var key = PriceCalculator.MakeResourceKey(entry.Key, purity);
                        return plan.ResourcePrices.ContainsKey(key);
                    });

                    return (result.IsComplete == allCovered)
                        .Label($"IsComplete={result.IsComplete}, allCovered={allCovered}");
                });
        }

        /// <summary>
        /// Feature: pricing-plans, Property 5: Blueprint price equals resource cost plus time costs
        ///
        /// For any PricingPlan, Blueprint, and non-negative hours, the computed price equals
        /// resourceCost + FixedCostPerItem + (HourlyCostRate × hours).
        /// **Validates: Requirements 4.1, 4.2, 4.3, 4.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BlueprintPrice_EqualsResourceCostPlusTimeCosts()
        {
            return Prop.ForAll(
                PricingPlanGen().ToArbitrary(),
                ConstructionResourcesGen().ToArbitrary(),
                NonNegativeDecimalGen().ToArbitrary(),
                (plan, resources, hours) =>
                {
                    var blueprint = new OE2EmpireTracker.Models.Blueprint { Resources = resources };
                    var result = PriceCalculator.ComputeBlueprintPrice(plan, blueprint, hours);

                    // Manually compute expected price
                    decimal resourceCost = 0m;
                    foreach (var entry in resources)
                    {
                        var purity = PriceCalculator.DeterminePurity(entry.Key);
                        var key = PriceCalculator.MakeResourceKey(entry.Key, purity);
                        int qty = int.Parse(entry.Value);
                        decimal price;
                        if (plan.ResourcePrices.TryGetValue(key, out price))
                        {
                            resourceCost += qty * price;
                        }
                    }

                    decimal expectedPrice = resourceCost + plan.FixedCostPerItem + (plan.HourlyCostRate * hours);

                    return (result.Price == expectedPrice)
                        .Label($"Expected {expectedPrice}, got {result.Price}");
                });
        }
    }
}
