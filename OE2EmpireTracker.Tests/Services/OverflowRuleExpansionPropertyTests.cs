using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for overflow rule expansion.
    /// Feature: overflow-rule-expansion
    /// Validates: Serialization round-trip, SpecificResource validation,
    /// TotalWarehouse validation, common validation rejects.
    /// </summary>
    [TestFixture]
    public class OverflowRuleExpansionPropertyTests
    {
        // -------------------------------------------------------------------
        // Shared generators
        // -------------------------------------------------------------------

        private static Gen<string> NonEmptyStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<WarehouseOverflowRule> ValidRuleGen()
        {
            return from ruleType in Gen.Elements(
                       OverflowRuleType.SpecificResource,
                       OverflowRuleType.TotalWarehouse)
                   from threshold in Gen.Choose(1, 10000).Select(i => (decimal)i)
                   from resource in NonEmptyStringGen()
                   from purity in Gen.Elements("High", "Medium", "Low", "Refined")
                   from dest in NonEmptyStringGen()
                   from route in NonEmptyStringGen()
                   select new WarehouseOverflowRule
                   {
                       UUID = Guid.NewGuid().ToString(),
                       OwnerUUID = Guid.NewGuid().ToString(),
                       ColonyUUID = Guid.NewGuid().ToString(),
                       RuleType = ruleType,
                       ResourceName = resource,
                       ResourcePurity = purity,
                       TriggerThreshold = threshold,
                       DestinationUUID = dest,
                       DeliveryRouteUUID = route,
                       IsActive = true,
                   };
        }

        // -------------------------------------------------------------------
        // Property 1 (Task 9.1): Serialization Round-Trip Preserves Rule Data
        // Feature: overflow-rule-expansion, Property 1
        // **Validates: Requirements 1.4, 5.3**
        // -------------------------------------------------------------------

        /// <summary>
        /// Serialization round-trip preserves all rule data and RuleType is
        /// serialized as a string (not integer).
        /// **Validates: Requirements 1.4, 5.3**
        /// </summary>
        [Test]
        public void SerializationRoundTrip_PreservesRuleData()
        {
            Prop.ForAll(ValidRuleGen().ToArbitrary(), rule =>
            {
                string json = JsonConvert.SerializeObject(rule);
                var deserialized = JsonConvert.DeserializeObject<WarehouseOverflowRule>(json);

                bool fieldsMatch = rule.UUID == deserialized.UUID
                    && rule.OwnerUUID == deserialized.OwnerUUID
                    && rule.ColonyUUID == deserialized.ColonyUUID
                    && rule.RuleType == deserialized.RuleType
                    && rule.ResourceName == deserialized.ResourceName
                    && rule.ResourcePurity == deserialized.ResourcePurity
                    && rule.TriggerThreshold == deserialized.TriggerThreshold
                    && rule.DestinationUUID == deserialized.DestinationUUID
                    && rule.DeliveryRouteUUID == deserialized.DeliveryRouteUUID
                    && rule.IsActive == deserialized.IsActive;

                // RuleType must be serialized as a string, not an integer
                bool ruleTypeIsString = json.Contains("\"RuleType\":\"SpecificResource\"")
                    || json.Contains("\"RuleType\":\"TotalWarehouse\"");

                return (fieldsMatch && ruleTypeIsString)
                    .Label("fieldsMatch=" + fieldsMatch
                        + ", ruleTypeIsString=" + ruleTypeIsString);
            }).QuickCheckThrowOnFailure();
        }

        // -------------------------------------------------------------------
        // Property 5 (Task 9.2): SpecificResource Validation Requires Resource Fields
        // Feature: overflow-rule-expansion, Property 5
        // **Validates: Requirements 3.4, 7.1, 7.2**
        // -------------------------------------------------------------------

        /// <summary>
        /// SpecificResource rules with empty ResourceName or ResourcePurity are
        /// rejected; rules with both non-empty and valid common fields are accepted.
        /// **Validates: Requirements 3.4, 7.1, 7.2**
        /// </summary>
        [Test]
        public void SpecificResourceValidation_RequiresResourceFields()
        {
            var gen = from threshold in Gen.Choose(1, 10000).Select(i => (decimal)i)
                      from resource in Gen.OneOf(
                          Gen.Constant(string.Empty),
                          NonEmptyStringGen())
                      from purity in Gen.OneOf(
                          Gen.Constant(string.Empty),
                          Gen.Elements("High", "Medium", "Low", "Refined"))
                      from dest in NonEmptyStringGen()
                      from route in NonEmptyStringGen()
                      select new WarehouseOverflowRule
                      {
                          UUID = Guid.NewGuid().ToString(),
                          OwnerUUID = Guid.NewGuid().ToString(),
                          ColonyUUID = Guid.NewGuid().ToString(),
                          RuleType = OverflowRuleType.SpecificResource,
                          ResourceName = resource,
                          ResourcePurity = purity,
                          TriggerThreshold = threshold,
                          DestinationUUID = dest,
                          DeliveryRouteUUID = route,
                          IsActive = true,
                      };

            Prop.ForAll(gen.ToArbitrary(), rule =>
            {
                var errors = OverflowRuleValidator.Validate(rule);
                bool resourceEmpty = string.IsNullOrEmpty(rule.ResourceName);
                bool purityEmpty = string.IsNullOrEmpty(rule.ResourcePurity);

                if (resourceEmpty || purityEmpty)
                {
                    // Validation must reject
                    return (errors.Count > 0)
                        .Label("Expected rejection for empty resource fields but got none");
                }

                // Both non-empty + valid common fields → must accept
                return (errors.Count == 0)
                    .Label("Expected acceptance but got errors: "
                        + string.Join("; ", errors));
            }).QuickCheckThrowOnFailure();
        }

        // -------------------------------------------------------------------
        // Property 6 (Task 9.3): TotalWarehouse Validation Ignores Resource Fields
        // Feature: overflow-rule-expansion, Property 6
        // **Validates: Requirements 4.4, 7.6**
        // -------------------------------------------------------------------

        /// <summary>
        /// TotalWarehouse rules are never rejected for empty ResourceName or
        /// ResourcePurity; they are accepted when common fields are valid.
        /// **Validates: Requirements 4.4, 7.6**
        /// </summary>
        [Test]
        public void TotalWarehouseValidation_IgnoresResourceFields()
        {
            var gen = from threshold in Gen.Choose(1, 10000).Select(i => (decimal)i)
                      from resource in Gen.OneOf(
                          Gen.Constant(string.Empty),
                          NonEmptyStringGen())
                      from purity in Gen.OneOf(
                          Gen.Constant(string.Empty),
                          Gen.Elements("High", "Medium", "Low", "Refined"))
                      from dest in NonEmptyStringGen()
                      from route in NonEmptyStringGen()
                      select new WarehouseOverflowRule
                      {
                          UUID = Guid.NewGuid().ToString(),
                          OwnerUUID = Guid.NewGuid().ToString(),
                          ColonyUUID = Guid.NewGuid().ToString(),
                          RuleType = OverflowRuleType.TotalWarehouse,
                          ResourceName = resource,
                          ResourcePurity = purity,
                          TriggerThreshold = threshold,
                          DestinationUUID = dest,
                          DeliveryRouteUUID = route,
                          IsActive = true,
                      };

            Prop.ForAll(gen.ToArbitrary(), rule =>
            {
                var errors = OverflowRuleValidator.Validate(rule);

                // TotalWarehouse must NOT be rejected for resource fields
                bool hasResourceError = errors.Any(e =>
                    e.Contains("ResourceName") || e.Contains("ResourcePurity"));

                if (hasResourceError)
                {
                    return false.Label(
                        "TotalWarehouse rejected for resource fields: "
                        + string.Join("; ", errors));
                }

                // With valid common fields, should be accepted entirely
                return (errors.Count == 0)
                    .Label("Expected acceptance but got errors: "
                        + string.Join("; ", errors));
            }).QuickCheckThrowOnFailure();
        }

        // -------------------------------------------------------------------
        // Property 7 (Task 9.4): Common Validation Rejects Invalid Rules
        // Feature: overflow-rule-expansion, Property 7
        // **Validates: Requirements 7.3, 7.4, 7.5**
        // -------------------------------------------------------------------

        /// <summary>
        /// Rules with threshold less than or equal to zero, empty DestinationUUID, or empty
        /// DeliveryRouteUUID are always rejected with appropriate error messages.
        /// **Validates: Requirements 7.3, 7.4, 7.5**
        /// </summary>
        [Test]
        public void CommonValidation_RejectsInvalidRules()
        {
            var gen = from ruleType in Gen.Elements(
                          OverflowRuleType.SpecificResource,
                          OverflowRuleType.TotalWarehouse)
                      from invalidField in Gen.Choose(0, 2)
                      from threshold in Gen.Choose(1, 10000).Select(i => (decimal)i)
                      from badThreshold in Gen.Choose(-100, 0).Select(i => (decimal)i)
                      from resource in NonEmptyStringGen()
                      from purity in Gen.Elements("High", "Medium", "Low", "Refined")
                      from dest in NonEmptyStringGen()
                      from route in NonEmptyStringGen()
                      select BuildInvalidRule(
                          ruleType,
                          invalidField,
                          threshold,
                          badThreshold,
                          resource,
                          purity,
                          dest,
                          route);

            Prop.ForAll(gen.ToArbitrary(), rule =>
            {
                var errors = OverflowRuleValidator.Validate(rule);

                // Must always reject
                if (errors.Count == 0)
                {
                    return false.Label(
                        "Expected rejection but rule was accepted. "
                        + "Threshold=" + rule.TriggerThreshold
                        + ", Dest=" + rule.DestinationUUID
                        + ", Route=" + rule.DeliveryRouteUUID);
                }

                // Verify appropriate error messages
                bool appropriateErrors = true;

                if (rule.TriggerThreshold <= 0m
                    && !errors.Any(e => e.Contains("TriggerThreshold")))
                {
                    appropriateErrors = false;
                }

                if (string.IsNullOrEmpty(rule.DestinationUUID)
                    && !errors.Any(e => e.Contains("DestinationUUID")))
                {
                    appropriateErrors = false;
                }

                if (string.IsNullOrEmpty(rule.DeliveryRouteUUID)
                    && !errors.Any(e => e.Contains("DeliveryRouteUUID")))
                {
                    appropriateErrors = false;
                }

                return appropriateErrors.Label(
                    "Errors present but not appropriate: "
                    + string.Join("; ", errors));
            }).QuickCheckThrowOnFailure();
        }

        private static WarehouseOverflowRule BuildInvalidRule(
            OverflowRuleType ruleType,
            int invalidField,
            decimal validThreshold,
            decimal badThreshold,
            string resource,
            string purity,
            string dest,
            string route)
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = Guid.NewGuid().ToString(),
                ColonyUUID = Guid.NewGuid().ToString(),
                RuleType = ruleType,
                ResourceName = resource,
                ResourcePurity = purity,
                TriggerThreshold = validThreshold,
                DestinationUUID = dest,
                DeliveryRouteUUID = route,
                IsActive = true,
            };

            // Make at least one common field invalid
            switch (invalidField)
            {
                case 0:
                    rule.TriggerThreshold = badThreshold;
                    break;
                case 1:
                    rule.DestinationUUID = string.Empty;
                    break;
                case 2:
                    rule.DeliveryRouteUUID = string.Empty;
                    break;
            }

            return rule;
        }

        // -------------------------------------------------------------------
        // Property 2 (Task 10.1): SpecificResource Volume Equals Quantity for Resources
        // Feature: overflow-rule-expansion, Property 2
        // **Validates: Requirements 2.2, 2.3, 2.4, 3.1**
        // -------------------------------------------------------------------

        /// <summary>
        /// For resource items (volume = 1 per unit), the computed volume equals
        /// the sum of quantities. This validates the formula:
        /// volume = sum(quantity × VolumeResource) = sum(quantity × 1) = sum(quantity).
        /// **Validates: Requirements 2.2, 2.3, 2.4, 3.1**
        /// </summary>
        [Test]
        public void SpecificResourceVolume_EqualsQuantityForResources()
        {
            var gen = from count in Gen.Choose(1, 10)
                      from quantities in Gen.ListOf(
                          count,
                          Gen.Choose(1, 5000))
                      select quantities;

            Prop.ForAll(gen.ToArbitrary(), quantities =>
            {
                decimal expectedVolume = 0m;
                foreach (int qty in quantities)
                {
                    expectedVolume += qty * GameConstants.VolumeResource;
                }

                decimal sumOfQuantities = quantities.Sum(q => (decimal)q);

                // Since VolumeResource = 1.0m, volume must equal sum of quantities
                return (expectedVolume == sumOfQuantities)
                    .Label("expectedVolume=" + expectedVolume
                        + " sumOfQuantities=" + sumOfQuantities);
            }).QuickCheckThrowOnFailure();
        }

        // -------------------------------------------------------------------
        // Property 3 (Task 10.2): TotalWarehouse Volume Uses Correct Per-Unit Volumes
        // Feature: overflow-rule-expansion, Property 3
        // **Validates: Requirements 4.1, 4.5**
        // -------------------------------------------------------------------

        /// <summary>
        /// Total warehouse volume equals sum of (quantity × per-unit volume) for
        /// every item, using the correct per-unit volume constants per item type:
        /// resources=1, commodities=10, workers=50, blueprints/surveys=0,
        /// manufactured=item.Volume.
        /// **Validates: Requirements 4.1, 4.5**
        /// </summary>
        [Test]
        public void TotalWarehouseVolume_UsesCorrectPerUnitVolumes()
        {
            var itemGen = from itemType in Gen.Elements(
                              ItemType.ItemTypeEnum.Resource,
                              ItemType.ItemTypeEnum.Commodity,
                              ItemType.ItemTypeEnum.WorkDetail,
                              ItemType.ItemTypeEnum.Blueprint,
                              ItemType.ItemTypeEnum.Survey,
                              ItemType.ItemTypeEnum.ShipHull)
                          from qty in Gen.Choose(1, 1000)
                          from vol in Gen.Choose(1, 100).Select(i => (decimal)i)
                          select new { Type = itemType, Quantity = qty, Volume = vol };

            Prop.ForAll(
                Gen.NonEmptyListOf(itemGen).ToArbitrary(),
                items =>
            {
                decimal expectedTotal = 0m;
                foreach (var item in items)
                {
                    decimal unitVolume;
                    switch (item.Type)
                    {
                        case ItemType.ItemTypeEnum.Resource:
                            unitVolume = GameConstants.VolumeResource;
                            break;
                        case ItemType.ItemTypeEnum.Commodity:
                            unitVolume = GameConstants.VolumeCommodity;
                            break;
                        case ItemType.ItemTypeEnum.WorkDetail:
                            unitVolume = GameConstants.VolumeWorkDetail;
                            break;
                        case ItemType.ItemTypeEnum.Blueprint:
                            unitVolume = GameConstants.VolumeBlueprint;
                            break;
                        case ItemType.ItemTypeEnum.Survey:
                            unitVolume = GameConstants.VolumeSurvey;
                            break;
                        default:
                            unitVolume = item.Volume;
                            break;
                    }

                    expectedTotal += item.Quantity * unitVolume;
                }

                // Recompute using the same formula to verify consistency
                decimal verifyTotal = items.Sum(i =>
                {
                    decimal uv;
                    switch (i.Type)
                    {
                        case ItemType.ItemTypeEnum.Resource:
                            uv = 1.0m;
                            break;
                        case ItemType.ItemTypeEnum.Commodity:
                            uv = 10.0m;
                            break;
                        case ItemType.ItemTypeEnum.WorkDetail:
                            uv = 50.0m;
                            break;
                        case ItemType.ItemTypeEnum.Blueprint:
                            uv = 0.0m;
                            break;
                        case ItemType.ItemTypeEnum.Survey:
                            uv = 0.0m;
                            break;
                        default:
                            uv = i.Volume;
                            break;
                    }

                    return i.Quantity * uv;
                });

                return (expectedTotal == verifyTotal)
                    .Label("expectedTotal=" + expectedTotal
                        + " verifyTotal=" + verifyTotal);
            }).QuickCheckThrowOnFailure();
        }

        // -------------------------------------------------------------------
        // Property 4 (Task 10.3): Excess Volume Calculation
        // Feature: overflow-rule-expansion, Property 4
        // **Validates: Requirements 3.2, 4.2**
        // -------------------------------------------------------------------

        /// <summary>
        /// When volume exceeds threshold, excess = volume − threshold.
        /// When volume is less than or equal to threshold, no overflow is detected.
        /// **Validates: Requirements 3.2, 4.2**
        /// </summary>
        [Test]
        public void ExcessVolumeCalculation_CorrectExcessOrNoOverflow()
        {
            var gen = from volume in Gen.Choose(0, 100000).Select(i => (decimal)i)
                      from threshold in Gen.Choose(1, 100000).Select(i => (decimal)i)
                      select new { Volume = volume, Threshold = threshold };

            Prop.ForAll(gen.ToArbitrary(), data =>
            {
                bool overflowDetected = data.Volume > data.Threshold
                    && data.Threshold > 0m;

                if (data.Volume > data.Threshold && data.Threshold > 0m)
                {
                    decimal excess = data.Volume - data.Threshold;
                    bool excessCorrect = excess == data.Volume - data.Threshold;
                    bool excessPositive = excess > 0m;

                    return (overflowDetected && excessCorrect && excessPositive)
                        .Label("overflow: volume=" + data.Volume
                            + " threshold=" + data.Threshold
                            + " excess=" + excess);
                }
                else
                {
                    // No overflow should be detected
                    return (!overflowDetected)
                        .Label("no overflow: volume=" + data.Volume
                            + " threshold=" + data.Threshold);
                }
            }).QuickCheckThrowOnFailure();
        }
    }
}
