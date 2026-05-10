using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using static OE2EmpireTracker.Tests.Models.ReadOnlyWrapperGenerators;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Property-based tests for read-only wrapper correctness.
    /// Feature: readonly-data-wrappers
    /// </summary>
    [TestFixture]
    public class ReadOnlyWrapperPropertyTests
    {
        // ---------------------------------------------------------------
        // Property 1: Value and Enum Passthrough
        // For randomly generated entities, wrap in ReadOnly wrapper, verify
        // all value-type/string/enum properties return identical values.
        // **Validates: Requirements 1.5, 1.7, 4.1, 13.1**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Blueprint_ValuePassthrough(OE2EmpireTracker.Models.Blueprint bp)
        {
            var ro = new ReadOnlyBlueprint(bp);
            return (ro.UUID == bp.UUID &&
                    ro.Name == bp.Name &&
                    ro.OwnerUUID == bp.OwnerUUID &&
                    ro.BaseBlueprintUUID == bp.BaseBlueprintUUID &&
                    ro.LegacyUUID == bp.LegacyUUID &&
                    ro.BluePrintType == bp.BluePrintType &&
                    ro.Evolution == bp.Evolution &&
                    ro.TechLevel == bp.TechLevel &&
                    ro.Class == bp.Class &&
                    ro.CopyCost == bp.CopyCost &&
                    ro.NickName == bp.NickName &&
                    ro.Description == bp.Description &&
                    ro.ExtendedName == bp.ExtendedName &&
                    ro.OutputItemName == bp.OutputItemName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Colony_ValuePassthrough(Colony colony)
        {
            var ro = new ReadOnlyColony(colony);
            return (ro.UUID == colony.UUID &&
                    ro.OwnerUUID == colony.OwnerUUID &&
                    ro.LegacyUUID == colony.LegacyUUID &&
                    ro.PlanetName == colony.PlanetName &&
                    ro.SystemName == colony.SystemName &&
                    ro.ColonyName == colony.ColonyName &&
                    ro.LastImportDateTime == colony.LastImportDateTime &&
                    ro.Name == colony.ColonyName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property DeliveryRoute_ValuePassthrough(DeliveryRoute route)
        {
            var ro = new ReadOnlyDeliveryRoute(route);
            return (ro.UUID == route.UUID &&
                    ro.Name == route.Name &&
                    ro.OwnerUUID == route.OwnerUUID).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Ship_ValuePassthrough(Ship ship)
        {
            var ro = new ReadOnlyShip(ship);
            return (ro.UUID == ship.UUID &&
                    ro.Name == ship.Name &&
                    ro.OwnerUUID == ship.OwnerUUID &&
                    ro.TemplateUUID == ship.TemplateUUID &&
                    ro.HullBlueprintUUID == ship.HullBlueprintUUID &&
                    ro.LocationType == ship.LocationType &&
                    ro.LocationUUID == ship.LocationUUID &&
                    ro.HullCurrentHP == ship.HullCurrentHP &&
                    ro.HullMaxHP == ship.HullMaxHP &&
                    ro.HullMaxRepairPercent == ship.HullMaxRepairPercent).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property MarketListing_ValuePassthrough(MarketListing ml)
        {
            var ro = new ReadOnlyMarketListing(ml);
            return (ro.UUID == ml.UUID &&
                    ro.OwnerUUID == ml.OwnerUUID &&
                    ro.StationUUID == ml.StationUUID &&
                    ro.ItemType == ml.ItemType &&
                    ro.ItemReferenceID == ml.ItemReferenceID &&
                    ro.ItemName == ml.ItemName &&
                    ro.Quantity == ml.Quantity &&
                    ro.PricePerUnit == ml.PricePerUnit &&
                    ro.CurrentHP == ml.CurrentHP &&
                    ro.MaxHP == ml.MaxHP &&
                    ro.MaxRepairPercent == ml.MaxRepairPercent).ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 2: Nested Recursive Wrapping
        // For entities with nested types, verify children are wrapped as
        // ReadOnly types with matching properties.
        // **Validates: Requirements 1.8, 1.9, 1.10, 1.11, 5.9, 6.1-6.36**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Colony_NestedWrapping(Colony colony)
        {
            var ro = new ReadOnlyColony(colony);

            // Structures count matches
            bool structuresMatch = ro.Structures.Count == colony.Structures.Count;

            // Items bag count matches
            bool itemsMatch = ro.Items.Count() == colony.Items.Count();

            // Commodities count matches
            bool commoditiesMatch = ro.Commodities.Count == colony.Commodities.Count;

            return (structuresMatch && itemsMatch && commoditiesMatch).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Survey_NestedWrapping(Survey survey)
        {
            var ro = new ReadOnlySurvey(survey);

            // Resources dictionary count matches
            bool resourcesMatch = ro.Resources.Count == survey.Resources.Count;

            // Each resource key is present and values match
            bool keysMatch = survey.Resources.Keys.All(k => ro.Resources.ContainsKey(k));
            bool valuesMatch = survey.Resources.All(kvp =>
            {
                var roRes = ro.Resources[kvp.Key];
                return roRes.Resource == kvp.Value.Resource &&
                       roRes.Purity == kvp.Value.Purity &&
                       roRes.Amount == kvp.Value.Amount;
            });

            return (resourcesMatch && keysMatch && valuesMatch).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Ship_NestedWrapping(Ship ship)
        {
            var ro = new ReadOnlyShip(ship);

            // Components count matches
            bool componentsMatch = ro.Components.Count == ship.Components.Count;

            // Component properties match element-wise
            bool componentPropsMatch = ship.Components.Count == 0 ||
                Enumerable.Range(0, ship.Components.Count).All(i =>
                    ro.Components[i].SlotType == ship.Components[i].SlotType &&
                    ro.Components[i].SlotIndex == ship.Components[i].SlotIndex &&
                    ro.Components[i].BlueprintUUID == ship.Components[i].BlueprintUUID);

            // Cargo bag count matches
            bool cargoMatch = ro.Cargo.Count() == ship.Cargo.Count();

            return (componentsMatch && componentPropsMatch && cargoMatch).ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 3: Live Read-Through
        // Generate entity, wrap, mutate a property on the mutable entity,
        // verify wrapper reflects the new value.
        // **Validates: Requirements 3.2, 3.3**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Blueprint_LiveReadThrough(OE2EmpireTracker.Models.Blueprint bp, string newName)
        {
            var ro = new ReadOnlyBlueprint(bp);
            bp.Name = newName;
            return (ro.Name == newName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Colony_LiveReadThrough(Colony colony, string newName)
        {
            var ro = new ReadOnlyColony(colony);
            colony.ColonyName = newName;
            return (ro.ColonyName == newName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Ship_LiveReadThrough(Ship ship)
        {
            var ro = new ReadOnlyShip(ship);
            int newHP = ship.HullCurrentHP + 42;
            ship.HullCurrentHP = newHP;
            return (ro.HullCurrentHP == newHP).ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 4: Utility Container Query Delegation
        // For randomly populated PropertyBag/ItemBag/LockTracking/CountDownTime,
        // wrap and verify each query method returns same result as calling
        // directly on mutable container.
        // **Validates: Requirements 5.1, 5.3, 5.5, 5.7**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property PropertyBag_QueryDelegation(PropertyBag bag)
        {
            var ro = new ReadOnlyPropertyBag(bag);

            // Count matches
            bool countMatch = ro.Count == bag.Count;

            // ContainsKey matches for each key
            bool containsMatch = bag.Properties.Keys.All(k => ro.ContainsKey(k) == bag.ContainsKey(k));

            // GetDecimal delegation
            bool decimalMatch = true;
            foreach (var key in bag.Properties.Keys)
            {
                decimal roVal, bagVal;
                bool roRet = ro.GetDecimal(key, 0m, out roVal);
                bool bagRet = bag.GetDecimal(key, 0m, out bagVal);
                if (roRet != bagRet || roVal != bagVal)
                {
                    decimalMatch = false;
                    break;
                }
            }

            // GetString delegation
            bool stringMatch = true;
            foreach (var key in bag.Properties.Keys)
            {
                string roVal, bagVal;
                bool roRet = ro.GetString(key, string.Empty, out roVal);
                bool bagRet = bag.GetString(key, string.Empty, out bagVal);
                if (roRet != bagRet || roVal != bagVal)
                {
                    stringMatch = false;
                    break;
                }
            }

            return (countMatch && containsMatch && decimalMatch && stringMatch).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property ItemBag_QueryDelegation(ItemBag bag)
        {
            var ro = new ReadOnlyItemBag(bag);

            // Count matches
            bool countMatch = ro.Count() == bag.Count();

            // ContainsKey matches for each item UUID
            bool containsMatch = bag.Items.Keys.All(k => ro.ContainsKey(k) == bag.ContainsKey(k));

            // CountByType matches for each item
            bool countByTypeMatch = bag.Items.Values.All(item =>
                ro.CountByType(item.ItemType, item.BaseItemTypeID) ==
                bag.CountByType(item.ItemType, item.BaseItemTypeID));

            return (countMatch && containsMatch && countByTypeMatch).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property LockTracking_QueryDelegation(LockTracking tracking)
        {
            var ro = new ReadOnlyLockTracking(tracking);

            // GetLockedQuantity matches for a sample item type
            bool qtyMatch = ro.GetLockedQuantity(ItemType.ItemTypeEnum.Resource, "Alkali Metals") ==
                            tracking.GetLockedQuantity(ItemType.ItemTypeEnum.Resource, "Alkali Metals");

            // GetLocksForProcess matches for a non-existent process
            bool emptyMatch = ro.GetLocksForProcess("nonexistent").Count ==
                              tracking.GetLocksForProcess("nonexistent").Count;

            return (qtyMatch && emptyMatch).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property CountDownTime_QueryDelegation(CountDownTime cdt)
        {
            // Freeze the clock so time-sensitive properties (TimeRemaining, IntervalsPassed)
            // return identical values on both the wrapper and the entity.
            var frozen = SystemClock.UtcNow;
            SystemClock.UtcNowFunc = () => frozen;
            try
            {
                var ro = new ReadOnlyCountDownTime(cdt);

                return (ro.TimeRemaining == cdt.TimeRemaining &&
                        ro.TimeRemainingString == cdt.TimeRemainingString &&
                        ro.IntervalsPassed == cdt.IntervalsPassed &&
                        ro.IsRepeating == cdt.IsRepeating &&
                        ro.RepeatIntervalSeconds == cdt.RepeatIntervalSeconds &&
                        ro.StartTime == cdt.StartTime &&
                        ro.EndTime == cdt.EndTime).ToProperty();
            }
            finally
            {
                SystemClock.Reset();
            }
        }

        // ---------------------------------------------------------------
        // Property 7: Nullable Nested Handling
        // For entities with nullable nested properties, verify null returns
        // null wrapper, non-null returns non-null wrapper.
        // **Validates: Requirements 12.1-12.5**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property ColonyStructure_NullableBuildTime(ColonyStructure cs)
        {
            var ro = new ReadOnlyColonyStructure(cs);

            bool buildTimeCorrect;
            if (cs.BuildCompletionTime == null)
            {
                buildTimeCorrect = ro.BuildCompletionTime == null;
            }
            else
            {
                buildTimeCorrect = ro.BuildCompletionTime != null &&
                                   ro.BuildCompletionTime.StartTime == cs.BuildCompletionTime.StartTime;
            }

            bool processTimeCorrect;
            if (cs.ProcessCompletionTime == null)
            {
                processTimeCorrect = ro.ProcessCompletionTime == null;
            }
            else
            {
                processTimeCorrect = ro.ProcessCompletionTime != null &&
                                     ro.ProcessCompletionTime.StartTime == cs.ProcessCompletionTime.StartTime;
            }

            return (buildTimeCorrect && processTimeCorrect).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Item_NullableContents(Item item)
        {
            var ro = new ReadOnlyItem(item);

            bool contentsCorrect;
            if (item.Contents == null)
            {
                contentsCorrect = ro.Contents == null;
            }
            else
            {
                contentsCorrect = ro.Contents != null &&
                                  ro.Contents.Count() == item.Contents.Count();
            }

            return contentsCorrect.ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 8: Equality by UUID
        // Generate pairs of entities with same/different UUIDs, wrap,
        // verify Equals returns true for same UUID and false for different.
        // **Validates: Requirements 15.1, 15.2, 15.3**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Blueprint_EqualityByUUID(OE2EmpireTracker.Models.Blueprint bp1, OE2EmpireTracker.Models.Blueprint bp2)
        {
            // Same UUID test: copy UUID from bp1 to bp2
            string sharedUUID = bp1.UUID;
            bp2.UUID = sharedUUID;
            var ro1 = new ReadOnlyBlueprint(bp1);
            var ro2 = new ReadOnlyBlueprint(bp2);

            bool sameUUIDEquals = ro1.Equals(ro2);
            bool sameHashCode = ro1.GetHashCode() == ro2.GetHashCode();

            // Different UUID test: restore unique UUID
            bp2.UUID = Guid.NewGuid().ToString();
            var ro3 = new ReadOnlyBlueprint(bp2);
            bool differentUUIDNotEquals = !ro1.Equals(ro3);

            return (sameUUIDEquals && sameHashCode && differentUUIDNotEquals).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Colony_EqualityByUUID(Colony c1, Colony c2)
        {
            string sharedUUID = c1.UUID;
            c2.UUID = sharedUUID;
            var ro1 = new ReadOnlyColony(c1);
            var ro2 = new ReadOnlyColony(c2);

            bool sameUUIDEquals = ro1.Equals(ro2);
            bool sameHashCode = ro1.GetHashCode() == ro2.GetHashCode();

            c2.UUID = Guid.NewGuid().ToString();
            var ro3 = new ReadOnlyColony(c2);
            bool differentUUIDNotEquals = !ro1.Equals(ro3);

            return (sameUUIDEquals && sameHashCode && differentUUIDNotEquals).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Ship_EqualityByUUID(Ship s1, Ship s2)
        {
            string sharedUUID = s1.UUID;
            s2.UUID = sharedUUID;
            var ro1 = new ReadOnlyShip(s1);
            var ro2 = new ReadOnlyShip(s2);

            bool sameUUIDEquals = ro1.Equals(ro2);
            bool sameHashCode = ro1.GetHashCode() == ro2.GetHashCode();

            s2.UUID = Guid.NewGuid().ToString();
            var ro3 = new ReadOnlyShip(s2);
            bool differentUUIDNotEquals = !ro1.Equals(ro3);

            return (sameUUIDEquals && sameHashCode && differentUUIDNotEquals).ToProperty();
        }

        // ---------------------------------------------------------------
        // Property 9: ToString Matches Display Name
        // For entities with ExtendedName, verify wrapper ToString returns
        // ExtendedName; for entities with only Name, verify ToString returns Name.
        // **Validates: Requirements 15.4**
        // ---------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Blueprint_ToStringMatchesExtendedName(OE2EmpireTracker.Models.Blueprint bp)
        {
            var ro = new ReadOnlyBlueprint(bp);
            return (ro.ToString() == bp.ExtendedName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Survey_ToStringMatchesExtendedName(Survey survey)
        {
            var ro = new ReadOnlySurvey(survey);
            return (ro.ToString() == survey.ExtendedName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Colony_ToStringMatchesName(Colony colony)
        {
            var ro = new ReadOnlyColony(colony);
            return (ro.ToString() == colony.ColonyName).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property DeliveryRoute_ToStringMatchesName(DeliveryRoute route)
        {
            var ro = new ReadOnlyDeliveryRoute(route);
            return (ro.ToString() == route.Name).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property Ship_ToStringMatchesName(Ship ship)
        {
            var ro = new ReadOnlyShip(ship);
            return (ro.ToString() == ship.Name).ToProperty();
        }

        [FsCheck.NUnit.Property(MaxTest = 25, Arbitrary = new[] { typeof(ReadOnlyWrapperArbitraries) })]
        public Property MarketListing_ToStringMatchesItemName(MarketListing ml)
        {
            var ro = new ReadOnlyMarketListing(ml);
            return (ro.ToString() == ml.ItemName).ToProperty();
        }
    }
}
