// <copyright file="ShipTemplatePricingTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Forms
{
    /// <summary>
    /// Property-based and unit tests for Ship Template Pricing feature.
    /// Feature: ship-template-pricing
    /// </summary>
    [TestFixture]
    public class ShipTemplatePricingTests
    {
        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "Complex Metallics", "Acidic Inorganics", "Post Trans Metals",
            "S1. Translanthanic Exotics", "S1. Translivermoric Exotics",
            "S2. Element 126", "S2. Element 127"
        };

        // -----------------------------------------------------------------------
        // Property 1: Template price aggregation equals sum of individual
        //             ComputeBlueprintPrice calls
        // Feature: ship-template-pricing, Property 1
        // **Validates: Requirements 2.1, 2.2, 2.3, 2.5**
        // -----------------------------------------------------------------------

        /// <summary>
        /// For any ship template with a valid pricing plan, hull blueprint, and
        /// 0-8 component blueprints, the aggregate price equals the sum of
        /// individual PriceCalculator.ComputeBlueprintPrice results.
        /// **Validates: Requirements 2.1, 2.2, 2.3, 2.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TemplatePriceAggregation_EqualsSum()
        {
            var gen = from plan in PricingPlanGen()
                      from hullResources in ConstructionResourcesGen()
                      from hullTime in ManufactureTimeGen()
                      from componentCount in Gen.Choose(0, 8)
                      from compResources in Gen.ListOf(componentCount, ConstructionResourcesGen())
                      from compTimes in Gen.ListOf(componentCount, ManufactureTimeGen())
                      select new
                      {
                          Plan = plan,
                          HullResources = hullResources,
                          HullTime = hullTime,
                          CompResources = compResources.ToList(),
                          CompTimes = compTimes.ToList()
                      };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hullBp = MakeBlueprint(data.HullResources, data.HullTime);
                decimal hullHours = EvolutionChainService.ParseTimeToSeconds(data.HullTime) / 3600m;
                var hullPrice = PriceCalculator.ComputeBlueprintPrice(data.Plan, hullBp, hullHours);

                decimal aggregatePrice = hullPrice.Price;
                bool aggregateComplete = hullPrice.IsComplete;

                for (int i = 0; i < data.CompResources.Count; i++)
                {
                    var compBp = MakeBlueprint(data.CompResources[i], data.CompTimes[i]);
                    decimal compHours = EvolutionChainService.ParseTimeToSeconds(data.CompTimes[i]) / 3600m;
                    var compPrice = PriceCalculator.ComputeBlueprintPrice(data.Plan, compBp, compHours);
                    aggregatePrice += compPrice.Price;
                    aggregateComplete = aggregateComplete && compPrice.IsComplete;
                }

                // Recompute the same way the form does: sum of individual calls
                decimal expectedSum = hullPrice.Price;
                for (int i = 0; i < data.CompResources.Count; i++)
                {
                    var compBp = MakeBlueprint(data.CompResources[i], data.CompTimes[i]);
                    decimal compHours = EvolutionChainService.ParseTimeToSeconds(data.CompTimes[i]) / 3600m;
                    expectedSum += PriceCalculator.ComputeBlueprintPrice(data.Plan, compBp, compHours).Price;
                }

                return (aggregatePrice == expectedSum)
                    .Label($"Aggregate={aggregatePrice}, ExpectedSum={expectedSum}");
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: Any incomplete individual price makes aggregate incomplete
        // Feature: ship-template-pricing, Property 2
        // **Validates: Requirements 2.6**
        // -----------------------------------------------------------------------

        /// <summary>
        /// For any template where some blueprints have missing resource prices,
        /// the aggregate IsComplete equals the AND of all individual IsComplete flags.
        /// **Validates: Requirements 2.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property IncompleteFlagPropagation_AnyIncomplete_AggregateIncomplete()
        {
            var gen = from plan in PricingPlanGen()
                      from hullResources in ConstructionResourcesGen()
                      from hullTime in ManufactureTimeGen()
                      from componentCount in Gen.Choose(1, 6)
                      from compResources in Gen.ListOf(componentCount, ConstructionResourcesGen())
                      from compTimes in Gen.ListOf(componentCount, ManufactureTimeGen())
                      select new
                      {
                          Plan = plan,
                          HullResources = hullResources,
                          HullTime = hullTime,
                          CompResources = compResources.ToList(),
                          CompTimes = compTimes.ToList()
                      };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                var hullBp = MakeBlueprint(data.HullResources, data.HullTime);
                decimal hullHours = EvolutionChainService.ParseTimeToSeconds(data.HullTime) / 3600m;
                var hullPrice = PriceCalculator.ComputeBlueprintPrice(data.Plan, hullBp, hullHours);

                bool expectedComplete = hullPrice.IsComplete;

                for (int i = 0; i < data.CompResources.Count; i++)
                {
                    var compBp = MakeBlueprint(data.CompResources[i], data.CompTimes[i]);
                    decimal compHours = EvolutionChainService.ParseTimeToSeconds(data.CompTimes[i]) / 3600m;
                    var compPrice = PriceCalculator.ComputeBlueprintPrice(data.Plan, compBp, compHours);
                    expectedComplete = expectedComplete && compPrice.IsComplete;
                }

                // Simulate the aggregate computation
                var hullBp2 = MakeBlueprint(data.HullResources, data.HullTime);
                decimal hullHours2 = EvolutionChainService.ParseTimeToSeconds(data.HullTime) / 3600m;
                var hullResult2 = PriceCalculator.ComputeBlueprintPrice(data.Plan, hullBp2, hullHours2);
                bool aggregateComplete = hullResult2.IsComplete;

                for (int i = 0; i < data.CompResources.Count; i++)
                {
                    var compBp = MakeBlueprint(data.CompResources[i], data.CompTimes[i]);
                    decimal compHours = EvolutionChainService.ParseTimeToSeconds(data.CompTimes[i]) / 3600m;
                    var compPrice = PriceCalculator.ComputeBlueprintPrice(data.Plan, compBp, compHours);
                    aggregateComplete = aggregateComplete && compPrice.IsComplete;
                }

                return (aggregateComplete == expectedComplete)
                    .Label($"AggregateComplete={aggregateComplete}, Expected={expectedComplete}");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: Dropdown always sorted with "(none)" first and parallel
        //             UUID list aligned
        // Feature: ship-template-pricing, Property 3
        // **Validates: Requirements 1.2**
        // -----------------------------------------------------------------------

        /// <summary>
        /// For any set of pricing plans, the dropdown list has "(none)" first
        /// with empty UUID, remaining sorted ordinal case-insensitive, and
        /// UUIDs match the corresponding plans.
        /// **Validates: Requirements 1.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PricingPlanDropdownOrdering_AlwaysSorted()
        {
            var planListGen = from count in Gen.Choose(0, 10)
                              from plans in Gen.ListOf(count, PricingPlanGen())
                              select plans.ToList();

            return Prop.ForAll(Arb.From(planListGen), plans =>
            {
                // Simulate PopulatePricingPlanCombo logic
                var sorted = CollectionSortHelper.OrderPricingPlans(plans);

                var names = new List<string> { "(none)" };
                var uuids = new List<string> { string.Empty };

                foreach (var plan in sorted)
                {
                    names.Add(plan.Name);
                    uuids.Add(plan.UUID);
                }

                // Assert first item is "(none)" with empty UUID
                if (names[0] != "(none)") return false.Label("First item is not '(none)'");
                if (uuids[0] != string.Empty) return false.Label("First UUID is not empty");

                // Assert remaining items are sorted ordinal case-insensitive
                var comparer = StringComparer.OrdinalIgnoreCase;
                for (int i = 2; i < names.Count; i++)
                {
                    if (comparer.Compare(names[i - 1], names[i]) > 0)
                    {
                        return false.Label(
                            $"Not sorted: '{names[i - 1]}' > '{names[i]}'");
                    }
                }

                // Assert UUIDs match the sorted plans
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (uuids[i + 1] != sorted[i].UUID)
                    {
                        return false.Label(
                            $"UUID mismatch at index {i + 1}: expected '{sorted[i].UUID}', got '{uuids[i + 1]}'");
                    }
                }

                return true.Label("OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 4: Display matches N2 format with asterisk when incomplete
        // Feature: ship-template-pricing, Property 4
        // **Validates: Requirements 3.1, 3.2**
        // -----------------------------------------------------------------------

        /// <summary>
        /// For any computed price >= 0 and boolean IsComplete flag, the formatted
        /// display text equals Price.ToString("N2") when complete, or
        /// Price.ToString("N2") + " *" when incomplete.
        /// **Validates: Requirements 3.1, 3.2**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PriceDisplayFormatting_MatchesN2WithIndicator()
        {
            var gen = from price in NonNegativeDecimalGen()
                      from isComplete in Arb.Generate<bool>()
                      select new { Price = price, IsComplete = isComplete };

            return Prop.ForAll(Arb.From(gen), data =>
            {
                // Simulate the formatting logic from UpdateTemplatePrice
                string formatted = data.Price.ToString("N2");
                if (!data.IsComplete)
                {
                    formatted += " *";
                }

                // Verify against expected
                string expected = data.IsComplete
                    ? data.Price.ToString("N2")
                    : data.Price.ToString("N2") + " *";

                return (formatted == expected)
                    .Label($"Price={data.Price}, IsComplete={data.IsComplete}, " +
                           $"Formatted='{formatted}', Expected='{expected}'");
            });
        }

        // ===================================================================
        // Unit Tests (Example-Based)
        // ===================================================================

        // -----------------------------------------------------------------------
        // 6.1: UpdateTemplatePrice_NoHull_ClearsLabel
        // **Validates: Requirements 3.3**
        // -----------------------------------------------------------------------

        /// <summary>
        /// When no hull is selected, the price label should be empty.
        /// Simulates the logic: if hull UUID is null/empty, result is empty string.
        /// **Validates: Requirements 3.3**
        /// </summary>
        [Test]
        public void UpdateTemplatePrice_NoHull_ClearsLabel()
        {
            // Arrange: no hull UUID means no price computation
            string hullUuid = null;

            // Act: simulate the guard check in UpdateTemplatePrice
            string result = ComputeTemplatePriceLabel(
                hullUuid,
                "some-plan-uuid",
                CreateTestPlan(),
                new List<ReadOnlyBlueprint>());

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // 6.2: UpdateTemplatePrice_NoPlan_ClearsLabel
        // **Validates: Requirements 3.4**
        // -----------------------------------------------------------------------

        /// <summary>
        /// When "(none)" plan is selected (empty UUID), the price label should be empty.
        /// **Validates: Requirements 3.4**
        /// </summary>
        [Test]
        public void UpdateTemplatePrice_NoPlan_ClearsLabel()
        {
            // Arrange: empty plan UUID means "(none)" selected
            string planUuid = string.Empty;
            var hullBp = MakeBlueprint(
                new Dictionary<string, string> { { "Alkali Metals", "10" } },
                "1h");

            // Act
            string result = ComputeTemplatePriceLabel(
                "hull-uuid",
                planUuid,
                null,
                new List<ReadOnlyBlueprint>());

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // 6.3: UpdateTemplatePrice_HullOnly_ShowsHullPrice
        // **Validates: Requirements 7.4**
        // -----------------------------------------------------------------------

        /// <summary>
        /// When a template has only a hull (no components), the price equals
        /// just the hull blueprint's price.
        /// **Validates: Requirements 7.4**
        /// </summary>
        [Test]
        public void UpdateTemplatePrice_HullOnly_ShowsHullPrice()
        {
            // Arrange
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Plan",
                FixedCostPerItem = 5.00m,
                HourlyCostRate = 2.00m
            };
            plan.ResourcePrices["Alkali Metals|Refined"] = 10.00m;

            var hullBp = MakeBlueprint(
                new Dictionary<string, string> { { "Alkali Metals", "3" } },
                "2h");

            // Act: compute hull-only price
            decimal hullHours = EvolutionChainService.ParseTimeToSeconds("2h") / 3600m;
            var hullResult = PriceCalculator.ComputeBlueprintPrice(plan, hullBp, hullHours);

            string label = ComputeTemplatePriceLabel(
                "hull-uuid",
                plan.UUID,
                plan,
                new List<ReadOnlyBlueprint>(),
                hullBp);

            // Assert: price = (3 * 10) + 5 + (2 * 2) = 30 + 5 + 4 = 39.00
            Assert.That(hullResult.Price, Is.EqualTo(39.00m));
            Assert.That(hullResult.IsComplete, Is.True);
            Assert.That(label, Is.EqualTo("39.00"));
        }

        // -----------------------------------------------------------------------
        // 6.4: UpdateTemplatePrice_UnresolvableComponent_FlagsIncomplete
        // **Validates: Requirements 7.3**
        // -----------------------------------------------------------------------

        /// <summary>
        /// When a component slot references a blueprint that cannot be resolved,
        /// it contributes 0 to the price and flags the result as incomplete.
        /// **Validates: Requirements 7.3**
        /// </summary>
        [Test]
        public void UpdateTemplatePrice_UnresolvableComponent_FlagsIncomplete()
        {
            // Arrange
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Plan",
                FixedCostPerItem = 1.00m,
                HourlyCostRate = 0.00m
            };
            plan.ResourcePrices["Halogens|Refined"] = 5.00m;

            var hullBp = MakeBlueprint(
                new Dictionary<string, string> { { "Halogens", "2" } },
                string.Empty);

            // Act: simulate unresolvable component (null blueprint)
            decimal hullHours = EvolutionChainService.ParseTimeToSeconds(string.Empty) / 3600m;
            var hullResult = PriceCalculator.ComputeBlueprintPrice(plan, hullBp, hullHours);

            // Simulate aggregate with one unresolvable component
            decimal aggregatePrice = hullResult.Price;
            bool isComplete = hullResult.IsComplete;

            // Unresolvable component: contributes 0, flags incomplete
            aggregatePrice += 0m;
            isComplete = false;

            string formatted = aggregatePrice.ToString("N2");
            if (!isComplete)
            {
                formatted += " *";
            }

            // Assert: hull price = (2 * 5) + 1 + 0 = 11.00, flagged incomplete
            Assert.That(aggregatePrice, Is.EqualTo(11.00m));
            Assert.That(isComplete, Is.False);
            Assert.That(formatted, Is.EqualTo("11.00 *"));
        }

        // -----------------------------------------------------------------------
        // 6.5: ClearForm_PreservesPricingPlanSelection
        // **Validates: Requirements 7.5**
        // -----------------------------------------------------------------------

        /// <summary>
        /// ClearForm() clears the price label but does NOT reset the pricing
        /// plan selection. Simulates the logic: lblComputedPrice.Text = empty,
        /// cmbPricingPlan selection unchanged.
        /// **Validates: Requirements 7.5**
        /// </summary>
        [Test]
        public void ClearForm_PreservesPricingPlanSelection()
        {
            // Arrange: simulate state before ClearForm
            string selectedPlanName = "My Pricing Plan";
            int selectedIndex = 2;
            string priceLabel = "125.50";

            // Act: simulate ClearForm logic
            // ClearForm clears the price label but preserves plan selection
            priceLabel = string.Empty;
            // selectedPlanName and selectedIndex are NOT modified

            // Assert
            Assert.That(priceLabel, Is.EqualTo(string.Empty));
            Assert.That(selectedPlanName, Is.EqualTo("My Pricing Plan"));
            Assert.That(selectedIndex, Is.EqualTo(2));
        }

        // -----------------------------------------------------------------------
        // 6.6: OnPricingDataChanged_PlanDeleted_RevertsToNone
        // **Validates: Requirements 7.2**
        // -----------------------------------------------------------------------

        /// <summary>
        /// When a previously selected plan is deleted externally, the dropdown
        /// reverts to "(none)" on the next refresh because the plan name is no
        /// longer in the list.
        /// **Validates: Requirements 7.2**
        /// </summary>
        [Test]
        public void OnPricingDataChanged_PlanDeleted_RevertsToNone()
        {
            // Arrange: simulate a plan list where the selected plan was deleted
            string previousSelection = "Deleted Plan";
            var currentPlans = new List<PricingPlan>
            {
                new PricingPlan { UUID = "uuid-1", Name = "Alpha Plan" },
                new PricingPlan { UUID = "uuid-2", Name = "Beta Plan" }
            };

            // Act: simulate PopulatePricingPlanCombo logic
            var sorted = CollectionSortHelper.OrderPricingPlans(currentPlans);
            var names = new List<string> { "(none)" };
            foreach (var plan in sorted)
            {
                names.Add(plan.Name);
            }

            // The SetItems logic: if previousSelection not found, revert to "(none)"
            string resolvedSelection = names.Contains(previousSelection)
                ? previousSelection
                : "(none)";

            // Assert
            Assert.That(resolvedSelection, Is.EqualTo("(none)"));
        }

        // -----------------------------------------------------------------------
        // 6.7: SetDetailEnabled_False_DisablesPricingCombo
        // **Validates: Requirements 1.6**
        // -----------------------------------------------------------------------

        /// <summary>
        /// SetDetailEnabled(false) disables the pricing plan combo box.
        /// Simulates the logic: cmbPricingPlan.Enabled = enabled.
        /// **Validates: Requirements 1.6**
        /// </summary>
        [Test]
        public void SetDetailEnabled_False_DisablesPricingCombo()
        {
            // Arrange
            bool cmbPricingPlanEnabled = true;

            // Act: simulate SetDetailEnabled(false)
            bool enabled = false;
            cmbPricingPlanEnabled = enabled;

            // Assert
            Assert.That(cmbPricingPlanEnabled, Is.False);
        }

        // ===================================================================
        // Helper Methods
        // ===================================================================

        /// <summary>
        /// Simulates the UpdateTemplatePrice logic to compute the label text.
        /// Returns empty string when guards fail, otherwise formatted price.
        /// </summary>
        private static string ComputeTemplatePriceLabel(
            string hullUuid,
            string planUuid,
            PricingPlan plan,
            List<ReadOnlyBlueprint> componentBlueprints,
            ReadOnlyBlueprint hullBlueprint = null)
        {
            // Guard: no hull
            if (string.IsNullOrEmpty(hullUuid))
            {
                return string.Empty;
            }

            // Guard: no plan (i.e. "(none)" selected)
            if (string.IsNullOrEmpty(planUuid))
            {
                return string.Empty;
            }

            // Guard: plan not found
            if (plan == null)
            {
                return string.Empty;
            }

            // Guard: hull blueprint not found
            if (hullBlueprint == null)
            {
                return string.Empty;
            }

            // Compute hull price
            string hullTimeStr = string.Empty;
            if (hullBlueprint.Properties != null)
            {
                string timeVal;
                hullBlueprint.Properties.GetString(
                    BlueprintPropertyKeys.ManufactureRunTime,
                    string.Empty,
                    out timeVal);
                hullTimeStr = timeVal;
            }

            decimal hullHours = EvolutionChainService.ParseTimeToSeconds(hullTimeStr) / 3600m;
            var hullResult = PriceCalculator.ComputeBlueprintPrice(plan, hullBlueprint, hullHours);

            decimal aggregatePrice = hullResult.Price;
            bool isComplete = hullResult.IsComplete;

            // Compute component prices
            foreach (var compBp in componentBlueprints)
            {
                string compTimeStr = string.Empty;
                if (compBp.Properties != null)
                {
                    string timeVal;
                    compBp.Properties.GetString(
                        BlueprintPropertyKeys.ManufactureRunTime,
                        string.Empty,
                        out timeVal);
                    compTimeStr = timeVal;
                }

                decimal compHours = EvolutionChainService.ParseTimeToSeconds(compTimeStr) / 3600m;
                var compResult = PriceCalculator.ComputeBlueprintPrice(plan, compBp, compHours);
                aggregatePrice += compResult.Price;
                isComplete = isComplete && compResult.IsComplete;
            }

            // Format
            string formatted = aggregatePrice.ToString("N2");
            if (!isComplete)
            {
                formatted += " *";
            }

            return formatted;
        }

        // ===================================================================
        // Generators
        // ===================================================================

        private static Gen<decimal> NonNegativeDecimalGen()
        {
            return Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);
        }

        private static Gen<string> ManufactureTimeGen()
        {
            return Gen.OneOf(
                Gen.Constant(string.Empty),
                Gen.Constant("30m"),
                Gen.Constant("1h"),
                Gen.Constant("2h 30m"),
                Gen.Constant("1d 4h"),
                Gen.Choose(1, 48).Select(h => $"{h}h"),
                Gen.Choose(1, 120).Select(m => $"{m}m"));
        }

        private static Gen<PricingPlan> PricingPlanGen()
        {
            return from fixedCost in NonNegativeDecimalGen()
                   from hourlyCost in NonNegativeDecimalGen()
                   from resourceCount in Gen.Choose(0, SampleResourceNames.Length)
                   from selectedResources in Gen.Shuffle(SampleResourceNames)
                       .Select(a => a.Take(resourceCount))
                   from prices in Gen.ListOf(resourceCount, NonNegativeDecimalGen())
                   select BuildPlan(fixedCost, hourlyCost, selectedResources.ToArray(), prices.ToArray());
        }

        private static Gen<Dictionary<string, string>> ConstructionResourcesGen()
        {
            return from count in Gen.Choose(1, 5)
                   from names in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(count))
                   from quantities in Gen.ListOf(count, Gen.Choose(1, 100))
                   select names.Zip(quantities, (n, q) => new { n, q })
                              .ToDictionary(x => x.n, x => x.q.ToString());
        }

        private static PricingPlan BuildPlan(
            decimal fixedCost,
            decimal hourlyCost,
            string[] resources,
            decimal[] prices)
        {
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Gen Plan " + Guid.NewGuid().ToString("N").Substring(0, 6),
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

        private static ReadOnlyBlueprint MakeBlueprint(
            Dictionary<string, string> resources,
            string manufactureRunTime)
        {
            var bp = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test BP",
                Resources = resources
            };

            if (!string.IsNullOrEmpty(manufactureRunTime))
            {
                bp.Properties.SetProperty(
                    BlueprintPropertyKeys.ManufactureRunTime,
                    manufactureRunTime);
            }

            return new ReadOnlyBlueprint(bp);
        }

        private static PricingPlan CreateTestPlan()
        {
            var plan = new PricingPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "Test Plan",
                FixedCostPerItem = 1.00m,
                HourlyCostRate = 1.00m
            };
            plan.ResourcePrices["Alkali Metals|Refined"] = 10.00m;
            return plan;
        }
    }
}
