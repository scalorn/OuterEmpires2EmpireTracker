// <copyright file="CrateContentImporterPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for CrateContentImporter correctness properties.
    /// Uses FsCheck 2.16.6 with NUnit integration.
    /// </summary>
    [TestFixture]
    public class CrateContentImporterPropertyTests
    {
        /// <summary>
        /// Known TypeC codes that MapAssetTypeC handles (excluding ambiguous casing pairs).
        /// </summary>
        private static readonly string[] KnownTypeCCodes = new[]
        {
            AssetTypeCodes.Blueprint,
            AssetTypeCodes.Resource,
            AssetTypeCodes.Commodity,
            AssetTypeCodes.CommodityL,
            AssetTypeCodes.ShipPart,
            AssetTypeCodes.ShipHull,
            AssetTypeCodes.Ammunition,
            AssetTypeCodes.Flatpack,
            AssetTypeCodes.Workforce,
            AssetTypeCodes.Share,
            AssetTypeCodes.Deployable,
            AssetTypeCodes.Survey,
            AssetTypeCodes.Crate,
        };

        /// <summary>
        /// TypeC codes suitable for non-crate cargo generation (excludes Crate to avoid
        /// nested crate complexity in basic property tests).
        /// </summary>
        private static readonly string[] NonCrateTypeCCodes = new[]
        {
            AssetTypeCodes.Blueprint,
            AssetTypeCodes.Resource,
            AssetTypeCodes.Commodity,
            AssetTypeCodes.CommodityL,
            AssetTypeCodes.ShipPart,
            AssetTypeCodes.ShipHull,
            AssetTypeCodes.Ammunition,
            AssetTypeCodes.Flatpack,
            AssetTypeCodes.Workforce,
            AssetTypeCodes.Share,
            AssetTypeCodes.Deployable,
            AssetTypeCodes.Survey,
        };

        // -----------------------------------------------------------------------
        // Generators
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates a random GameApiAssetItemProperty.
        /// </summary>
        /// <returns>A generator for item properties.</returns>
        private static Gen<GameApiAssetItemProperty> PropertyGen()
        {
            return from modTypeId in Gen.Choose(1, 50)
                   from propValue in Gen.Choose(1, 1000)
                   from origValue in Gen.Choose(1, 500)
                   from resPositive in Arb.Generate<bool>()
                   from canResearch in Arb.Generate<bool>()
                   select new GameApiAssetItemProperty
                   {
                       ModTypeId = modTypeId,
                       PropertyName = "prop_" + modTypeId,
                       FriendlyPropertyName = "Property " + modTypeId,
                       PropertyValue = propValue,
                       OriginalPropertyValue = origValue,
                       Unit = "HP",
                       ResearchPositive = resPositive,
                       CanResearch = canResearch,
                   };
        }

        /// <summary>
        /// Generates a random GameApiAssetCargoItem with a valid TypeC code.
        /// Includes optional properties for Blueprint items.
        /// </summary>
        /// <returns>A generator for cargo items.</returns>
        private static Gen<GameApiAssetCargoItem> CargoItemGen()
        {
            return from id in Gen.Choose(1, 999999)
                   from typeCIdx in Gen.Choose(0, NonCrateTypeCCodes.Length - 1)
                   from amount in Gen.Choose(1, 5000)
                   from mass in Gen.Choose(1, 100)
                   from volume in Gen.Choose(1, 50)
                   from evolution in Gen.Choose(0, 5)
                   from propCount in Gen.Choose(0, 3)
                   from props in Gen.ListOf(propCount, PropertyGen())
                   let typeC = NonCrateTypeCCodes[typeCIdx]
                   let name = typeC == AssetTypeCodes.Resource
                       ? "Resource_" + id + " (High Purity)"
                       : "Item_" + id
                   select new GameApiAssetCargoItem
                   {
                       CargoItemId = id,
                       TypeC = typeC,
                       ResourceName = name,
                       Amount = amount,
                       Mass = mass,
                       Volume = volume,
                       Evolution = evolution,
                       ShipPartType = typeC == AssetTypeCodes.ShipPart ? "Cg" : string.Empty,
                       Properties = props.ToList(),
                   };
        }

        /// <summary>
        /// Generates a GameApiAssetDetailResponse with 0-50 random cargo items.
        /// </summary>
        /// <returns>A generator for crate API responses.</returns>
        private static Gen<GameApiAssetDetailResponse> CrateResponseGen()
        {
            return from count in Gen.Choose(0, 50)
                   from items in Gen.ListOf(count, CargoItemGen())
                   select new GameApiAssetDetailResponse
                   {
                       Cargo = items.ToList(),
                   };
        }

        // -----------------------------------------------------------------------
        // Property 1: Round-Trip Equivalence
        // **Validates: Requirements 10.1, 10.2**
        //
        // For any valid GameApiAssetDetailResponse JSON, parsing it into Items,
        // serializing those Items to JSON, and parsing the serialized JSON back
        // into Items produces objects with equivalent field values.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Property 1: Round-trip serialization preserves all item fields.
        /// Generate API response → import into Items → serialize → deserialize → compare.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RoundTrip_Equivalence()
        {
            return Prop.ForAll(
                Arb.From(CrateResponseGen()),
                response =>
                {
                    TestHelper.ResetWithCachedData();
                    var playerContext = PlayerContext.GetInstance();
                    var empireContext = EmpireContext.GetInstance();
                    playerContext.CurrentPlayerUUID = "test-player-uuid";
                    var bls = new BlueprintLinkageService(playerContext, empireContext);
                    var importer = new CrateContentImporter(playerContext, empireContext, bls);

                    string json = JsonConvert.SerializeObject(response);
                    var parentBag = new ItemBag();
                    var visited = new HashSet<int>();
                    int crateId = 99999;

                    var result = importer.Import(json, crateId, parentBag, "owner", visited);

                    // Get the crate item's Contents
                    Item crateItem = null;
                    foreach (var kvp in parentBag.Items)
                    {
                        if (kvp.Value.GameItemId == crateId)
                        {
                            crateItem = kvp.Value;
                            break;
                        }
                    }

                    if (crateItem == null || crateItem.Contents == null)
                    {
                        return (result.Imported == 0).Label("No crate with empty import");
                    }

                    // Serialize the contents to JSON and deserialize back
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                    };
                    string serialized = JsonConvert.SerializeObject(
                        crateItem.Contents.Items, settings);
                    var deserialized = JsonConvert.DeserializeObject<
                        Dictionary<string, Item>>(serialized, settings);

                    // Compare field-by-field
                    foreach (var kvp in crateItem.Contents.Items)
                    {
                        var original = kvp.Value;
                        if (!deserialized.ContainsKey(kvp.Key))
                        {
                            return false.Label("Missing key after round-trip: " + kvp.Key);
                        }

                        var roundTripped = deserialized[kvp.Key];

                        if (original.Name != roundTripped.Name)
                        {
                            return false.Label(
                                "Name mismatch: '" + original.Name
                                + "' vs '" + roundTripped.Name + "'");
                        }

                        if (original.ItemType != roundTripped.ItemType)
                        {
                            return false.Label(
                                "ItemType mismatch: " + original.ItemType
                                + " vs " + roundTripped.ItemType);
                        }

                        if (original.Quantity != roundTripped.Quantity)
                        {
                            return false.Label("Quantity mismatch");
                        }

                        if (original.ResourcePurity != roundTripped.ResourcePurity)
                        {
                            return false.Label("ResourcePurity mismatch");
                        }

                        if (original.Evolution != roundTripped.Evolution)
                        {
                            return false.Label("Evolution mismatch");
                        }

                        if (original.ShipPartType != roundTripped.ShipPartType)
                        {
                            return false.Label("ShipPartType mismatch");
                        }

                        if (original.ItemProperties.Count
                            != roundTripped.ItemProperties.Count)
                        {
                            return false.Label("ItemProperties count mismatch");
                        }
                    }

                    return true.Label("OK");
                });
        }

        // -----------------------------------------------------------------------
        // Property 2: Contents Bag Completeness
        // **Validates: Requirements 3.1, 3.4**
        //
        // After import: |Contents.Items| == result.Imported
        // result.Imported + result.Failed == response.Cargo.Count
        // No items duplicated or lost.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Property 2: Contents bag item count equals imported count, and
        /// imported + failed equals total cargo count.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ContentsBag_Completeness()
        {
            return Prop.ForAll(
                Arb.From(CrateResponseGen()),
                response =>
                {
                    TestHelper.ResetWithCachedData();
                    var playerContext = PlayerContext.GetInstance();
                    var empireContext = EmpireContext.GetInstance();
                    playerContext.CurrentPlayerUUID = "test-player-uuid";
                    var bls = new BlueprintLinkageService(playerContext, empireContext);
                    var importer = new CrateContentImporter(playerContext, empireContext, bls);

                    string json = JsonConvert.SerializeObject(response);
                    var parentBag = new ItemBag();
                    var visited = new HashSet<int>();
                    int crateId = 88888;

                    var result = importer.Import(json, crateId, parentBag, "owner", visited);

                    // Locate the crate item
                    Item crateItem = null;
                    foreach (var kvp in parentBag.Items)
                    {
                        if (kvp.Value.GameItemId == crateId)
                        {
                            crateItem = kvp.Value;
                            break;
                        }
                    }

                    int contentsCount = crateItem?.Contents?.Items?.Count ?? 0;

                    // |Contents.Items| == result.Imported
                    if (contentsCount != result.Imported)
                    {
                        return false.Label(
                            "Contents count=" + contentsCount
                            + " != Imported=" + result.Imported);
                    }

                    // Imported + Failed == Cargo.Count
                    int expectedTotal = response.Cargo?.Count ?? 0;
                    int actualTotal = result.Imported + result.Failed;
                    if (actualTotal != expectedTotal)
                    {
                        return false.Label(
                            "Imported(" + result.Imported + ") + Failed("
                            + result.Failed + ") = " + actualTotal
                            + " != Cargo.Count=" + expectedTotal);
                    }

                    // No duplicates: all UUIDs in contents are unique
                    if (crateItem?.Contents?.Items != null)
                    {
                        var uuids = crateItem.Contents.Items.Keys.ToList();
                        if (uuids.Count != uuids.Distinct().Count())
                        {
                            return false.Label("Duplicate UUIDs in contents");
                        }
                    }

                    return true.Label("OK");
                });
        }

        // -----------------------------------------------------------------------
        // Property 3: Type Mapping Determinism
        // **Validates: Requirements 2.2, 2.6**
        //
        // For any TypeC code, MapAssetTypeC returns the same result on
        // repeated calls. The mapping is a pure function.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Property 3: MapAssetTypeC is deterministic — same input always
        /// produces same output across multiple invocations.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property TypeMapping_Determinism()
        {
            var allCodes = KnownTypeCCodes
                .Concat(new[] { "ZZ", "XX", string.Empty, "unknown", "Bp ", " R" })
                .ToArray();

            return Prop.ForAll(
                Arb.From(Gen.Elements(allCodes)),
                typeC =>
                {
                    var result1 = AssetMergeService.MapAssetTypeC(typeC);
                    var result2 = AssetMergeService.MapAssetTypeC(typeC);
                    var result3 = AssetMergeService.MapAssetTypeC(typeC);

                    if (result1 != result2 || result2 != result3)
                    {
                        return false.Label(
                            "Non-deterministic for typeC='" + typeC
                            + "': " + result1 + " / " + result2
                            + " / " + result3);
                    }

                    return true.Label("OK");
                });
        }

        // -----------------------------------------------------------------------
        // Property 4: Cycle Detection Termination
        // **Validates: Requirements 5.2, 5.4**
        //
        // Generate arbitrary nested crate graphs (DAGs with optional cycles).
        // Import always terminates; each unique crate processed at most once.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates a random crate graph represented as a list of (crateId, nestedCrateIds).
        /// Depth 1-5 with optional back-edges creating cycles.
        /// </summary>
        /// <returns>A generator for crate graphs.</returns>
        private static Gen<List<CrateGraphNode>> CrateGraphGen()
        {
            return from nodeCount in Gen.Choose(1, 5)
                   from baseId in Gen.Choose(1000, 9000)
                   from seed in Gen.Choose(0, int.MaxValue - 1)
                   select BuildGraphFromSeed(baseId, nodeCount, seed);
        }

        /// <summary>
        /// Builds a crate graph from a seed value for deterministic edge generation.
        /// </summary>
        private static List<CrateGraphNode> BuildGraphFromSeed(
            int baseId,
            int nodeCount,
            int seed)
        {
            var nodeIds = Enumerable.Range(baseId, nodeCount).ToList();
            var rng = new System.Random(seed);
            var graph = new List<CrateGraphNode>();

            for (int i = 0; i < nodeCount; i++)
            {
                int numEdges = rng.Next(0, 4);
                var nested = new List<int>();
                for (int e = 0; e < numEdges; e++)
                {
                    int targetIdx = rng.Next(0, nodeCount);
                    nested.Add(nodeIds[targetIdx]);
                }

                graph.Add(new CrateGraphNode(nodeIds[i], nested));
            }

            return graph;
        }

        /// <summary>
        /// Property 4: For any crate graph, simulating cascade imports
        /// with cycle detection terminates and processes each crate at most once.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CycleDetection_Termination()
        {
            return Prop.ForAll(
                Arb.From(CrateGraphGen()),
                graph =>
                {
                    TestHelper.ResetWithCachedData();
                    var playerContext = PlayerContext.GetInstance();
                    var empireContext = EmpireContext.GetInstance();
                    playerContext.CurrentPlayerUUID = "test-player-uuid";
                    var bls = new BlueprintLinkageService(playerContext, empireContext);
                    var importer = new CrateContentImporter(playerContext, empireContext, bls);

                    // Build a lookup of crateId → nested crate IDs
                    var graphLookup = graph.ToDictionary(
                        n => n.CrateId,
                        n => n.NestedCrateIds);

                    var visited = new HashSet<int>();
                    var processedCrates = new List<int>();
                    var queue = new Queue<int>();

                    // Start with the first node
                    queue.Enqueue(graph[0].CrateId);

                    int maxIterations = (graph.Count * 2) + 10;
                    int iterations = 0;

                    while (queue.Count > 0 && iterations < maxIterations)
                    {
                        iterations++;
                        int currentId = queue.Dequeue();

                        if (visited.Contains(currentId))
                        {
                            continue;
                        }

                        // Build a response for this crate containing nested refs
                        var cargo = new List<GameApiAssetCargoItem>();
                        if (graphLookup.ContainsKey(currentId))
                        {
                            foreach (int nestedId in graphLookup[currentId])
                            {
                                cargo.Add(new GameApiAssetCargoItem
                                {
                                    CargoItemId = nestedId,
                                    TypeC = AssetTypeCodes.Crate,
                                    ResourceName = "Nested_" + nestedId,
                                    Amount = 1,
                                });
                            }
                        }

                        // Add a normal item so the crate isn't empty
                        cargo.Add(new GameApiAssetCargoItem
                        {
                            CargoItemId = currentId + 100000,
                            TypeC = AssetTypeCodes.Resource,
                            ResourceName = "Iron (High Purity)",
                            Amount = 100,
                        });

                        var response = new GameApiAssetDetailResponse { Cargo = cargo };
                        string json = JsonConvert.SerializeObject(response);
                        var parentBag = new ItemBag();

                        var result = importer.Import(
                            json, currentId, parentBag, "owner", visited);

                        processedCrates.Add(currentId);

                        // Enqueue nested crates for cascade
                        foreach (int nestedId in result.NestedCrateIds)
                        {
                            queue.Enqueue(nestedId);
                        }
                    }

                    // Verify termination (did not exceed max iterations)
                    if (iterations >= maxIterations)
                    {
                        return false.Label(
                            "Failed to terminate within " + maxIterations
                            + " iterations");
                    }

                    // Verify each crate processed at most once
                    var duplicates = processedCrates
                        .GroupBy(x => x)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicates.Count > 0)
                    {
                        return false.Label(
                            "Crate(s) processed more than once: "
                            + string.Join(", ", duplicates));
                    }

                    return true.Label("OK");
                });
        }

        // -----------------------------------------------------------------------
        // Property 5: Blueprint Dual-Presence
        // **Validates: Requirements 4.1, 4.3**
        //
        // For every blueprint-typed item in Contents after import, a
        // corresponding entry exists in the master blueprint list.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates a crate response containing at least one blueprint item
        /// plus random other items.
        /// </summary>
        /// <returns>A generator for responses with blueprints.</returns>
        private static Gen<GameApiAssetDetailResponse> BlueprintResponseGen()
        {
            return from bpCount in Gen.Choose(1, 5)
                   from otherCount in Gen.Choose(0, 10)
                   from bpItems in Gen.ListOf(bpCount, BlueprintCargoItemGen())
                   from otherItems in Gen.ListOf(otherCount, NonBlueprintCargoItemGen())
                   select new GameApiAssetDetailResponse
                   {
                       Cargo = bpItems.Concat(otherItems).ToList(),
                   };
        }

        /// <summary>
        /// Generates a blueprint cargo item with at least one property.
        /// </summary>
        /// <returns>A generator for blueprint cargo items.</returns>
        private static Gen<GameApiAssetCargoItem> BlueprintCargoItemGen()
        {
            return from id in Gen.Choose(1, 999999)
                   from evolution in Gen.Choose(0, 5)
                   from propCount in Gen.Choose(1, 3)
                   from props in Gen.ListOf(propCount, PropertyGen())
                   select new GameApiAssetCargoItem
                   {
                       CargoItemId = id,
                       TypeC = AssetTypeCodes.Blueprint,
                       ResourceName = "Blueprint_" + id,
                       Amount = 1,
                       Evolution = evolution,
                       ShipPartType = "Cg",
                       Mass = 0.5,
                       Volume = 0.2,
                       Properties = props.ToList(),
                   };
        }

        /// <summary>
        /// Generates a non-blueprint cargo item.
        /// </summary>
        /// <returns>A generator for non-blueprint cargo items.</returns>
        private static Gen<GameApiAssetCargoItem> NonBlueprintCargoItemGen()
        {
            var nonBpCodes = NonCrateTypeCCodes
                .Where(c => c != AssetTypeCodes.Blueprint)
                .ToArray();

            return from id in Gen.Choose(1, 999999)
                   from typeCIdx in Gen.Choose(0, nonBpCodes.Length - 1)
                   from amount in Gen.Choose(1, 100)
                   let typeC = nonBpCodes[typeCIdx]
                   let name = typeC == AssetTypeCodes.Resource
                       ? "Resource_" + id + " (High Purity)"
                       : "Item_" + id
                   select new GameApiAssetCargoItem
                   {
                       CargoItemId = id,
                       TypeC = typeC,
                       ResourceName = name,
                       Amount = amount,
                       Mass = 1.0,
                       Volume = 1.0,
                       ShipPartType = typeC == AssetTypeCodes.ShipPart ? "Cg" : string.Empty,
                   };
        }

        /// <summary>
        /// Property 5: Every blueprint-typed item in Contents after import
        /// has a corresponding entry in the master blueprint list.
        /// </summary>
        /// <returns>The FsCheck property to verify.</returns>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Blueprint_DualPresence()
        {
            return Prop.ForAll(
                Arb.From(BlueprintResponseGen()),
                response =>
                {
                    TestHelper.ResetWithCachedData();
                    var playerContext = PlayerContext.GetInstance();
                    var empireContext = EmpireContext.GetInstance();
                    playerContext.CurrentPlayerUUID = "test-player-uuid";
                    var bls = new BlueprintLinkageService(playerContext, empireContext);
                    var importer = new CrateContentImporter(playerContext, empireContext, bls);

                    string json = JsonConvert.SerializeObject(response);
                    var parentBag = new ItemBag();
                    var visited = new HashSet<int>();
                    int crateId = 77777;

                    var result = importer.Import(json, crateId, parentBag, "test-player-uuid", visited);

                    // Locate the crate
                    Item crateItem = null;
                    foreach (var kvp in parentBag.Items)
                    {
                        if (kvp.Value.GameItemId == crateId)
                        {
                            crateItem = kvp.Value;
                            break;
                        }
                    }

                    if (crateItem?.Contents?.Items == null)
                    {
                        return (result.Imported == 0).Label("No contents with 0 imports");
                    }

                    // Get all blueprint items in Contents
                    var blueprintItems = crateItem.Contents.Items.Values
                        .Where(i => i.ItemType == ItemType.ItemTypeEnum.Blueprint)
                        .ToList();

                    // Get all blueprints from master list (player + global)
                    var playerBlueprints = playerContext.GetCurrentPlayerBlueprints();
                    var globalBlueprints = empireContext.GlobalBlueprintList;
                    var allBlueprintUUIDs = new HashSet<string>();

                    foreach (var bp in playerBlueprints)
                    {
                        allBlueprintUUIDs.Add(bp.UUID);
                    }

                    foreach (var bp in globalBlueprints)
                    {
                        allBlueprintUUIDs.Add(bp.UUID);
                    }

                    // Verify each blueprint item in Contents has a matching
                    // entry in the master list
                    foreach (var bpItem in blueprintItems)
                    {
                        if (string.IsNullOrEmpty(bpItem.BaseItemTypeID))
                        {
                            return false.Label(
                                "Blueprint '" + bpItem.Name
                                + "' has empty BaseItemTypeID");
                        }

                        if (!allBlueprintUUIDs.Contains(bpItem.BaseItemTypeID))
                        {
                            return false.Label(
                                "Blueprint '" + bpItem.Name
                                + "' BaseItemTypeID="
                                + bpItem.BaseItemTypeID
                                + " not found in master list");
                        }
                    }

                    return true.Label("OK");
                });
        }

        // -----------------------------------------------------------------------
        // Helper types
        // -----------------------------------------------------------------------

        /// <summary>
        /// Represents a node in a crate graph for cycle detection testing.
        /// </summary>
        public class CrateGraphNode
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CrateGraphNode"/> class.
            /// </summary>
            /// <param name="crateId">The crate identifier.</param>
            /// <param name="nestedCrateIds">The nested crate identifiers.</param>
            public CrateGraphNode(int crateId, List<int> nestedCrateIds)
            {
                CrateId = crateId;
                NestedCrateIds = nestedCrateIds;
            }

            /// <summary>
            /// Gets the crate identifier.
            /// </summary>
            public int CrateId { get; }

            /// <summary>
            /// Gets the nested crate identifiers (edges in the graph).
            /// </summary>
            public List<int> NestedCrateIds { get; }
        }
    }
}
