using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for PricingPlanService.
    /// Feature: bl-123-pricingplan-readonly, Properties 4, 5, 6 from the design document.
    /// </summary>
    [TestFixture]
    public class PricingPlanServicePropertyTests
    {
        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "S1. Translanthanic Exotics", "S2. Element 126",
        };

        // -----------------------------------------------------------------------
        // Shared generators
        // -----------------------------------------------------------------------

        private static Gen<decimal> NonNegativeDecimalGen()
        {
            return Gen.Choose(0, 100000).Select(i => (decimal)i / 100m);
        }

        private static Gen<string> SafeStringGen()
        {
            return Gen.OneOf(
                Gen.Constant(string.Empty),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));
        }

        private static Gen<Dictionary<string, decimal>> ResourcePricesDictGen()
        {
            return from resourceCount in Gen.Choose(0, SampleResourceNames.Length)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from prices in Gen.ListOf(resourceCount, NonNegativeDecimalGen())
                   let resources = selectedResources.ToArray()
                   let priceArr = prices.ToArray()
                   select BuildResourcePrices(resources, priceArr);
        }

        private static Dictionary<string, decimal> BuildResourcePrices(string[] resources, decimal[] prices)
        {
            var dict = new Dictionary<string, decimal>();
            for (int i = 0; i < resources.Length && i < prices.Length; i++)
            {
                var purity = PriceCalculator.DeterminePurity(resources[i]);
                var key = PriceCalculator.MakeResourceKey(resources[i], purity);
                dict[key] = prices[i];
            }

            return dict;
        }

        private static Gen<PricingPlanUpdateRequest> UpdateRequestGen()
        {
            return from name in SafeStringGen()
                   from desc in SafeStringGen()
                   from fixedCost in NonNegativeDecimalGen()
                   from hourlyCost in NonNegativeDecimalGen()
                   from resPrices in ResourcePricesDictGen()
                   select new PricingPlanUpdateRequest
                   {
                       Name = name,
                       Description = desc,
                       FixedCostPerItem = fixedCost,
                       HourlyCostRate = hourlyCost,
                       ResourcePrices = resPrices,
                   };
        }

        private static Gen<PricingPlanCreateRequest> CreateRequestGen()
        {
            return from name in SafeStringGen()
                   from desc in SafeStringGen()
                   from fixedCost in NonNegativeDecimalGen()
                   from hourlyCost in NonNegativeDecimalGen()
                   from resPrices in ResourcePricesDictGen()
                   select new PricingPlanCreateRequest
                   {
                       Name = name,
                       Description = desc,
                       FixedCostPerItem = fixedCost,
                       HourlyCostRate = hourlyCost,
                       ResourcePrices = resPrices,
                   };
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.Update Round-Trip
        // Feature: bl-123-pricingplan-readonly, Property 4: Service.Update Round-Trip
        // **Validates: Requirements 12.3, 12.4, 12.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new PricingPlanService(ctx);

                var seed = new PricingPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    Name = "Seed",
                };
                ctx.AddPricingPlan(seed);

                var result = svc.Update(seed.UUID, request);

                if (result.Name != request.Name) return false;
                if (result.Description != request.Description) return false;
                if (result.FixedCostPerItem != request.FixedCostPerItem) return false;
                if (result.HourlyCostRate != request.HourlyCostRate) return false;

                // ResourcePrices
                if (result.ResourcePrices.Count != request.ResourcePrices.Count) return false;
                foreach (var kvp in request.ResourcePrices)
                {
                    if (!result.ResourcePrices.TryGetValue(kvp.Key, out decimal val)) return false;
                    if (val != kvp.Value) return false;
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Create Round-Trip
        // Feature: bl-123-pricingplan-readonly, Property 5: Service.Create Round-Trip
        // **Validates: Requirements 13.2, 13.4, 13.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new PricingPlanService(ctx);

                var result = svc.Create(request);

                if (string.IsNullOrEmpty(result.UUID)) return false;
                if (result.Name != request.Name) return false;
                if (result.Description != request.Description) return false;
                if (result.FixedCostPerItem != request.FixedCostPerItem) return false;
                if (result.HourlyCostRate != request.HourlyCostRate) return false;

                // ResourcePrices
                if (result.ResourcePrices.Count != request.ResourcePrices.Count) return false;
                foreach (var kvp in request.ResourcePrices)
                {
                    if (!result.ResourcePrices.TryGetValue(kvp.Key, out decimal val)) return false;
                    if (val != kvp.Value) return false;
                }

                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.Delete Removes Plan
        // Feature: bl-123-pricingplan-readonly, Property 6: Service.Delete Removes Plan
        // **Validates: Requirements 14.1, 14.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesPlan()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new PricingPlanService(ctx);

                var created = svc.Create(request);
                string uuid = created.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutablePricingPlan(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "PricingPlan still exists after Delete");
            });
        }
    }
}