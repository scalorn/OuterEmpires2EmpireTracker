using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for CollectionSortHelper.
    /// Validates correctness properties from the design document:
    ///   Property 1: Sort helper produces correctly ordered output
    ///   Property 2: Sort helper and SerializationSorter agree on relative order
    ///   Property 3: GetFirstStagedStructure returns lowest-sequence staged structure
    ///   Property 4: CalculateLoadList is order-independent
    /// </summary>
    [TestFixture]
    public class CollectionSortHelperPropertyTests
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetAllFilePaths();
            EmpireContext.Reset();
            empireContext = EmpireContext.GetInstance();
            playerContext = PlayerContext.GetInstance();
        }

        // -----------------------------------------------------------------------
        // Property 1: Sort helper produces correctly ordered output (structures)
        // Feature: data-model-ordering-invariant, Property 1: Sort helper produces correctly ordered output (structures)
        // **Validates: Requirements 3.1, 8.2, 8.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OrderStructures_AlwaysSortedByBuildQueueSequence()
        {
            return Prop.ForAll(
                Arb.From<int[]>(),
                sequences =>
                {
                    var structures = sequences.Select(s => new ColonyStructure { BuildQueueSequence = s }).ToList();
                    var result = CollectionSortHelper.OrderStructures(structures);
                    for (int i = 1; i < result.Count; i++)
                    {
                        if (result[i].BuildQueueSequence < result[i - 1].BuildQueueSequence)
                            return false;
                    }
                    return true;
                });
        }

        // -----------------------------------------------------------------------
        // Property 1: Sort helper produces correctly ordered output (route stops)
        // Feature: data-model-ordering-invariant, Property 1: Sort helper produces correctly ordered output (route stops)
        // **Validates: Requirements 3.1, 8.2, 8.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OrderRouteStops_AlwaysSortedBySequence()
        {
            return Prop.ForAll(
                Arb.From<int[]>(),
                sequences =>
                {
                    var stops = sequences.Select(s => new RouteStop { Sequence = s }).ToList();
                    var result = CollectionSortHelper.OrderRouteStops(stops);
                    for (int i = 1; i < result.Count; i++)
                    {
                        if (result[i].Sequence < result[i - 1].Sequence)
                            return false;
                    }
                    return true;
                });
        }

        // -----------------------------------------------------------------------
        // Property 1: Sort helper produces correctly ordered output (colonies)
        // Feature: data-model-ordering-invariant, Property 1: Sort helper produces correctly ordered output (colonies)
        // **Validates: Requirements 3.1, 8.2, 8.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OrderColonies_AlwaysSortedByCompositeKey()
        {
            var stringGen = Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty),
                Arb.From<NonEmptyString>().Generator.Select(s => s.Get));

            var colonyGen = from sys in stringGen
                            from planet in stringGen
                            from colony in stringGen
                            select new Colony
                            {
                                SystemName = sys,
                                PlanetName = planet,
                                ColonyName = colony
                            };

            var listGen = Gen.ListOf(colonyGen);

            return Prop.ForAll(Arb.From(listGen), colonies =>
            {
                var result = CollectionSortHelper.OrderColonies(colonies);
                var comparer = StringComparer.OrdinalIgnoreCase;
                for (int i = 1; i < result.Count; i++)
                {
                    string prevKey = (result[i - 1].SystemName ?? string.Empty) + "\0"
                                   + (result[i - 1].PlanetName ?? string.Empty) + "\0"
                                   + (result[i - 1].ColonyName ?? string.Empty);
                    string currKey = (result[i].SystemName ?? string.Empty) + "\0"
                                   + (result[i].PlanetName ?? string.Empty) + "\0"
                                   + (result[i].ColonyName ?? string.Empty);

                    // Verify ordering by checking each component in order
                    int cmpSys = comparer.Compare(
                        result[i - 1].SystemName ?? string.Empty,
                        result[i].SystemName ?? string.Empty);
                    if (cmpSys > 0) return false;
                    if (cmpSys == 0)
                    {
                        int cmpPlanet = comparer.Compare(
                            result[i - 1].PlanetName ?? string.Empty,
                            result[i].PlanetName ?? string.Empty);
                        if (cmpPlanet > 0) return false;
                        if (cmpPlanet == 0)
                        {
                            int cmpColony = comparer.Compare(
                                result[i - 1].ColonyName ?? string.Empty,
                                result[i].ColonyName ?? string.Empty);
                            if (cmpColony > 0) return false;
                        }
                    }
                }
                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 1: Sort helper produces correctly ordered output (components)
        // Feature: data-model-ordering-invariant, Property 1: Sort helper produces correctly ordered output (components)
        // **Validates: Requirements 3.1, 8.2, 8.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property OrderComponents_AlwaysSortedBySlotTypeThenIndex()
        {
            var slotTypeGen = Gen.OneOf(
                Gen.Constant((string)null),
                Gen.Constant(string.Empty),
                Gen.Elements("Weapon", "Shield", "Drive", "Reactor", "Cargo", "Utility"));

            var componentGen = from slotType in slotTypeGen
                               from slotIndex in Arb.From<int>().Generator
                               select new ShipComponentSlot
                               {
                                   SlotType = slotType,
                                   SlotIndex = slotIndex
                               };

            var listGen = Gen.ListOf(componentGen);

            return Prop.ForAll(Arb.From(listGen), components =>
            {
                var result = CollectionSortHelper.OrderComponents(components);
                var comparer = StringComparer.OrdinalIgnoreCase;
                for (int i = 1; i < result.Count; i++)
                {
                    int cmpType = comparer.Compare(
                        result[i - 1].SlotType ?? string.Empty,
                        result[i].SlotType ?? string.Empty);
                    if (cmpType > 0) return false;
                    if (cmpType == 0)
                    {
                        if (result[i].SlotIndex < result[i - 1].SlotIndex)
                            return false;
                    }
                }
                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 2: Sort helper and SerializationSorter agree on RouteStop order
        // Feature: data-model-ordering-invariant, Property 2: Sort helper and SerializationSorter agree on relative order for shared keys
        // **Validates: Requirements 3.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SortHelper_And_SerializationSorter_AgreeOnRouteStopOrder()
        {
            return Prop.ForAll(
                Arb.From<int[]>(),
                sequences =>
                {
                    var stops = sequences.Select(s => new RouteStop { Sequence = s }).ToArray();

                    // CollectionSortHelper result
                    var helperResult = CollectionSortHelper.OrderRouteStops(stops);

                    // SerializationSorter result (uses SortByInt with Sequence)
                    var sorterResult = SerializationSorter.SortByInt(
                        stops.ToArray(), x => x.Sequence);

                    if (helperResult.Count != sorterResult.Length)
                        return false;

                    // Both sort by Sequence ascending, so relative order should match
                    for (int i = 0; i < helperResult.Count; i++)
                    {
                        if (helperResult[i].Sequence != sorterResult[i].Sequence)
                            return false;
                    }
                    return true;
                });
        }

        // -----------------------------------------------------------------------
        // Property 2: Sort helper and SerializationSorter agree on Component order
        // Feature: data-model-ordering-invariant, Property 2: Sort helper and SerializationSorter agree on relative order for shared keys
        // **Validates: Requirements 3.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property SortHelper_And_SerializationSorter_AgreeOnComponentOrder()
        {
            var slotTypeGen = Gen.Elements("Weapon", "Shield", "Drive", "Reactor", "Cargo", "Utility");

            var componentGen = from slotType in slotTypeGen
                               from slotIndex in Gen.Choose(0, 20)
                               select new ShipComponentSlot
                               {
                                   SlotType = slotType,
                                   SlotIndex = slotIndex
                               };

            var arrayGen = Gen.ArrayOf(componentGen);

            return Prop.ForAll(Arb.From(arrayGen), components =>
            {
                // CollectionSortHelper result
                var helperResult = CollectionSortHelper.OrderComponents(components);

                // SerializationSorter result (uses SortByStringThenInt with SlotType, SlotIndex)
                var sorterResult = SerializationSorter.SortByStringThenInt(
                    components.ToArray(), x => x.SlotType, x => x.SlotIndex);

                if (helperResult.Count != sorterResult.Length)
                    return false;

                // Both sort by SlotType then SlotIndex, so relative order should match.
                // Note: CollectionSortHelper uses OrdinalIgnoreCase, SerializationSorter uses Ordinal.
                // For the non-null, non-mixed-case slot types we generate, these agree.
                for (int i = 0; i < helperResult.Count; i++)
                {
                    if (helperResult[i].SlotType != sorterResult[i].SlotType)
                        return false;
                    if (helperResult[i].SlotIndex != sorterResult[i].SlotIndex)
                        return false;
                }
                return true;
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: GetFirstStagedStructure returns lowest-sequence staged structure
        // Feature: data-model-ordering-invariant, Property 3: GetFirstStagedStructure returns the lowest-sequence staged structure
        // **Validates: Requirements 4.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property GetFirstStagedStructure_ReturnsLowestSequenceStaged()
        {
            // Generate 1-10 structures with random BuildQueueSequence values.
            // At least one will be staged.
            var countGen = Gen.Choose(1, 10);
            var seqGen = Gen.Choose(1, 1000);
            var stagedGen = Arb.From<bool>().Generator;

            var colonyGen = from count in countGen
                            from seqs in Gen.ArrayOf(count, seqGen)
                            from stagedFlags in Gen.ArrayOf(count, stagedGen)
                            // Ensure at least one is staged
                            let ensuredFlags = stagedFlags.All(f => !f)
                                ? stagedFlags.Select((f, i) => i == 0 ? true : f).ToArray()
                                : stagedFlags
                            select new { Sequences = seqs, StagedFlags = ensuredFlags };

            return Prop.ForAll(Arb.From(colonyGen), config =>
            {
                var colony = new Colony { UUID = Guid.NewGuid().ToString() };
                var stagedStructures = new List<ColonyStructure>();

                for (int i = 0; i < config.Sequences.Length; i++)
                {
                    var s = new ColonyStructure
                    {
                        UUID = Guid.NewGuid().ToString(),
                        BuildQueueSequence = config.Sequences[i],
                        // Need a valid blueprint UUID for ColonyBuildEligibility
                        FlatpackBlueprintUUID = null
                    };

                    if (config.StagedFlags[i])
                    {
                        // Mark as staged: Staged=true, Built=false
                        s.Properties.SetProperty(GameConstants.PropStaged, true);
                        s.Properties.SetProperty(GameConstants.PropBuilt, false);
                        stagedStructures.Add(s);
                    }
                    else
                    {
                        // Mark as built: Staged=false, Built=true
                        s.Properties.SetProperty(GameConstants.PropStaged, false);
                        s.Properties.SetProperty(GameConstants.PropBuilt, true);
                    }

                    colony.Structures.Add(s);
                }

                // Shuffle the structures list to randomize order
                var rng = new System.Random(config.Sequences.Sum());
                colony.Structures = colony.Structures.OrderBy(_ => rng.Next()).ToList();

                var result = ColonyBuildEligibility.GetFirstStagedStructure(colony, playerContext);

                if (result == null)
                    return false.Label("GetFirstStagedStructure returned null but staged structures exist");

                // The result should be the staged structure with the lowest BuildQueueSequence
                int expectedMinSeq = stagedStructures.Min(s => s.BuildQueueSequence);
                bool correct = result.BuildQueueSequence == expectedMinSeq;
                return correct.Label(
                    correct ? "OK" : $"Expected seq={expectedMinSeq}, got seq={result.BuildQueueSequence}");
            });
        }

        // -----------------------------------------------------------------------
        // Property 4: CalculateLoadList is order-independent
        // Feature: data-model-ordering-invariant, Property 4: CalculateLoadList is order-independent
        // **Validates: Requirements 4.6**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CalculateLoadList_OrderIndependent()
        {
            var stopCountGen = Gen.Choose(1, 5);
            var quantityGen = Gen.Choose(1, 50);

            var planGen = from stopCount in stopCountGen
                          from dropQuantities in Gen.ArrayOf(stopCount, quantityGen)
                          from pickQuantities in Gen.ArrayOf(stopCount, quantityGen)
                          select new { StopCount = stopCount, DropQuantities = dropQuantities, PickQuantities = pickQuantities };

            return Prop.ForAll(Arb.From(planGen), config =>
            {
                // Build a canonical plan with stops in sequence order
                var canonicalPlan = new DeliveryPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "TestPlan"
                };

                for (int i = 0; i < config.StopCount; i++)
                {
                    var stop = new DeliveryPlanStop
                    {
                        Sequence = i + 1,
                        ColonyUUID = Guid.NewGuid().ToString()
                    };
                    stop.DropOff.Add(new DeliveryItem
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        BaseItemTypeID = "Iron",
                        Name = "Iron",
                        ResourcePurity = "Refined",
                        Quantity = config.DropQuantities[i]
                    });
                    stop.PickUp.Add(new DeliveryItem
                    {
                        ItemType = ItemType.ItemTypeEnum.Resource,
                        BaseItemTypeID = "Iron",
                        Name = "Iron",
                        ResourcePurity = "Refined",
                        Quantity = config.PickQuantities[i]
                    });
                    canonicalPlan.Stops.Add(stop);
                }

                // Calculate load list with canonical order
                var canonicalResult = canonicalPlan.CalculateLoadList();

                // Build a shuffled plan with stops in reverse order
                var shuffledPlan = new DeliveryPlan
                {
                    UUID = canonicalPlan.UUID,
                    Name = canonicalPlan.Name
                };
                shuffledPlan.Stops = new List<DeliveryPlanStop>(canonicalPlan.Stops);
                shuffledPlan.Stops.Reverse();

                // Calculate load list with shuffled order
                var shuffledResult = shuffledPlan.CalculateLoadList();

                // Results should be identical
                if (canonicalResult.Count != shuffledResult.Count)
                    return false.Label($"Count mismatch: {canonicalResult.Count} vs {shuffledResult.Count}");

                for (int i = 0; i < canonicalResult.Count; i++)
                {
                    if (canonicalResult[i].Name != shuffledResult[i].Name ||
                        canonicalResult[i].Quantity != shuffledResult[i].Quantity)
                        return false.Label(
                            $"Item mismatch at [{i}]: {canonicalResult[i].Name}x{canonicalResult[i].Quantity} vs {shuffledResult[i].Name}x{shuffledResult[i].Quantity}");
                }

                return true.Label("OK");
            });
        }
    }
}