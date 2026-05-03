using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for PricingPlanViewModel edit buffer.
    /// Feature: bl-123-pricingplan-readonly
    /// Validates: LoadFrom round-trip, IsDirty false after LoadFrom, IsDirty detects changes.
    /// </summary>
    [TestFixture]
    public class PricingPlanViewModelPropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random PricingPlan with all fields populated
        // Reuses the ValidPricingPlanGen() pattern from PricingPlanSerializationPropertyTests
        // -----------------------------------------------------------------------

        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "S1. Translanthanic Exotics", "S2. Element 126"
        };

        private static Gen<decimal> NonNegativeDecimalGen()
        {
            return Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);
        }

        private static Gen<PricingPlan> ValidPricingPlanGen()
        {
            return from name in Arb.Generate<NonEmptyString>()
                   from desc in Arb.Generate<string>()
                   from fixedCost in NonNegativeDecimalGen()
                   from hourlyCost in NonNegativeDecimalGen()
                   from resourceCount in Gen.Choose(0, SampleResourceNames.Length)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from prices in Gen.ListOf(resourceCount, NonNegativeDecimalGen())
                   select BuildPlan(
                       name.Get,
                       desc ?? string.Empty,
                       fixedCost,
                       hourlyCost,
                       selectedResources.ToArray(),
                       prices.ToArray());
        }

        private static PricingPlan BuildPlan(
            string name,
            string desc,
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
                Description = desc,
                FixedCostPerItem = fixedCost,
                HourlyCostRate = hourlyCost,
            };

            for (int i = 0; i < resources.Length && i < prices.Length; i++)
            {
                var purity = PriceCalculator.DeterminePurity(resources[i]);
                var key = PriceCalculator.MakeResourceKey(resources[i], purity);
                plan.ResourcePrices[key] = prices[i];
            }

            return plan;
        }

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all fields
        // Feature: bl-123-pricingplan-readonly, Property 1
        // **Validates: Requirements 4.1, 4.2, 4.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidPricingPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyPricingPlan(plan);
                var vm = new PricingPlanViewModel();
                vm.LoadFrom(ro);

                bool uuidMatch = vm.UUID == ro.UUID;
                bool ownerMatch = vm.OwnerUUID == ro.OwnerUUID;
                bool nameMatch = vm.Name == ro.Name;
                bool descMatch = vm.Description == ro.Description;
                bool fixedMatch = vm.FixedCostPerItem == ro.FixedCostPerItem;
                bool hourlyMatch = vm.HourlyCostRate == ro.HourlyCostRate;

                bool pricesMatch = vm.ResourcePrices.Count == ro.ResourcePrices.Count
                    && ro.ResourcePrices.All(kv =>
                        vm.ResourcePrices.ContainsKey(kv.Key)
                        && vm.ResourcePrices[kv.Key] == kv.Value);

                return (uuidMatch && ownerMatch && nameMatch && descMatch
                    && fixedMatch && hourlyMatch && pricesMatch)
                    .Label($"uuid={uuidMatch}, owner={ownerMatch}, name={nameMatch}, " +
                           $"desc={descMatch}, fixed={fixedMatch}, hourly={hourlyMatch}, " +
                           $"prices={pricesMatch}");
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: IsDirty false immediately after LoadFrom
        // Feature: bl-123-pricingplan-readonly, Property 2
        // **Validates: Requirements 7.1, 7.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidPricingPlanGen().ToArbitrary(), plan =>
            {
                var ro = new ReadOnlyPricingPlan(plan);
                var vm = new PricingPlanViewModel();
                vm.LoadFrom(ro);

                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects any single field change
        // Feature: bl-123-pricingplan-readonly, Property 3
        // **Validates: Requirements 7.1, 7.2, 7.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsAnySingleFieldChange()
        {
            // 4 scalar fields + 1 resource price change = 5 cases
            var fieldIndexGen = Gen.Choose(0, 4);

            return Prop.ForAll(
                ValidPricingPlanGen().ToArbitrary(),
                Arb.From(fieldIndexGen),
                (plan, fieldIndex) =>
                {
                    var ro = new ReadOnlyPricingPlan(plan);
                    var vm = new PricingPlanViewModel();
                    vm.LoadFrom(ro);

                    string changedField = MutateSingleField(vm, ro, fieldIndex);

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing " + changedField);
                });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string MutateSingleField(
            PricingPlanViewModel vm,
            ReadOnlyPricingPlan ro,
            int fieldIndex)
        {
            switch (fieldIndex % 5)
            {
                case 0:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name";
                case 1:
                    vm.Description = (ro.Description ?? string.Empty) + "X";
                    return "Description";
                case 2:
                    vm.FixedCostPerItem = ro.FixedCostPerItem + 1m;
                    return "FixedCostPerItem";
                case 3:
                    vm.HourlyCostRate = ro.HourlyCostRate + 1m;
                    return "HourlyCostRate";
                case 4:
                    // Add a new resource price entry to make ResourcePrices differ
                    vm.ResourcePrices["__test_resource__|TestPurity"] = 999.99m;
                    return "ResourcePrices";
                default:
                    vm.Name = (ro.Name ?? string.Empty) + "X";
                    return "Name(default)";
            }
        }
    }
}