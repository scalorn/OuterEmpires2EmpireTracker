using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Feature: pricing-plans, Property 8: Serialization round-trip
    ///
    /// For any valid PricingPlan object, serializing to JSON with JsonSettings.SerializerSettings
    /// and then deserializing back should produce an equivalent object.
    ///
    /// **Validates: Requirements 5.4**
    /// </summary>
    [TestFixture]
    public class PricingPlanSerializationPropertyTests
    {
        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "S1. Translanthanic Exotics", "S2. Element 126"
        };

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property SerializationRoundTrip_ProducesEquivalentObject()
        {
            return Prop.ForAll(ValidPricingPlanGen().ToArbitrary(), plan =>
            {
                string json = JsonConvert.SerializeObject(plan, JsonSettings.SerializerSettings);
                var deserialized = JsonConvert.DeserializeObject<PricingPlan>(json);

                bool uuidMatch = deserialized.UUID == plan.UUID;
                bool nameMatch = deserialized.Name == plan.Name;
                bool ownerMatch = deserialized.OwnerUUID == plan.OwnerUUID;
                // Description may be null after round-trip if empty (DefaultValueHandling.Ignore)
                bool descMatch = (deserialized.Description ?? string.Empty) == (plan.Description ?? string.Empty);
                bool fixedMatch = deserialized.FixedCostPerItem == plan.FixedCostPerItem;
                bool hourlyMatch = deserialized.HourlyCostRate == plan.HourlyCostRate;

                // ResourcePrices: zero-valued entries ARE serialized (they're in a dictionary, not default-value properties)
                bool pricesMatch = deserialized.ResourcePrices != null &&
                    deserialized.ResourcePrices.Count == plan.ResourcePrices.Count &&
                    plan.ResourcePrices.All(kv =>
                        deserialized.ResourcePrices.ContainsKey(kv.Key) &&
                        deserialized.ResourcePrices[kv.Key] == kv.Value);

                return (uuidMatch && nameMatch && ownerMatch && descMatch && fixedMatch && hourlyMatch && pricesMatch)
                    .Label($"uuid={uuidMatch}, name={nameMatch}, owner={ownerMatch}, desc={descMatch}, " +
                           $"fixed={fixedMatch}, hourly={hourlyMatch}, prices={pricesMatch}");
            });
        }

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
    }
}
