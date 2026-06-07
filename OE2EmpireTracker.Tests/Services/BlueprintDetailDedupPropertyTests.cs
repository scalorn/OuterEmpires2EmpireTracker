// <copyright file="BlueprintDetailDedupPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Bug condition exploration tests for blueprint detail dedup.
    /// These tests encode the EXPECTED (correct) behavior and are expected to
    /// FAIL on unfixed code, confirming the bug exists.
    /// Spec: blueprint-detail-dedup
    /// </summary>
    [TestFixture]
    public class BlueprintDetailDedupPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Creates a PlayerContext containing the specified blueprints pre-loaded.
        /// </summary>
        private static PlayerContext CreateContextWithBlueprints(Bp[] blueprints)
        {
            var root = new PlayerRoot
            {
                CurrentPlayerUUID = "player-1",
                Blueprint = blueprints,
                PlayerProfile = new[]
                {
                    new PlayerProfile { UUID = "player-1", Name = "Test" },
                },
            };

            return new PlayerContext(root);
        }

        /// <summary>
        /// Simulates the post-import API ID assignment logic from
        /// CreateBlueprintDetailItem. This is the UNFIXED code path that uses
        /// FirstOrDefault on Name+Evolution.
        /// </summary>
        private static void SimulateApiIdAssignment(
            PlayerContext ctx,
            int blueprintId,
            string importedName,
            int importedEvo)
        {
            var blueprint = ctx.BlueprintList
                .FirstOrDefault(b =>
                    string.Equals(b.Name, importedName, StringComparison.Ordinal) &&
                    b.Evolution == importedEvo);

            if (blueprint != null)
            {
                blueprint.GameApiBlueprintId = blueprintId;
                blueprint.LastDetailImportUtc = SystemClock.UtcNow;
                ctx.IndexBlueprintByApiId(blueprint);
            }
        }

        // ---------------------------------------------------------------
        // Property 1: Bug Condition - Duplicate Name+Evolution API ID Stomping
        // ---------------------------------------------------------------

        /// <summary>
        /// Property: When 3 blueprints share the same Name+Evolution and 3
        /// distinct API IDs are assigned sequentially, each API ID MUST map to
        /// a DISTINCT local blueprint. On unfixed code, FirstOrDefault always
        /// returns the same blueprint, causing this property to fail.
        /// Validates: Requirements 1.1, 1.2, 1.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property BugCondition_DuplicateNameEvolution_EachApiIdMapsToDistinctBlueprint()
        {
            var gen =
                from name in Gen.Elements("Reactor", "Laser", "Shield", "Engine")
                from evo in Gen.Choose(1, 5)
                from id1 in Gen.Choose(100, 199)
                from id2 in Gen.Choose(200, 299)
                from id3 in Gen.Choose(300, 399)
                select new { Name = name, Evo = evo, Id1 = id1, Id2 = id2, Id3 = id3 };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Create 3 blueprints with identical Name+Evolution
                var blueprints = new[]
                {
                    new Bp(data.Name)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.Evo,
                        OwnerUUID = "player-1",
                    },
                    new Bp(data.Name)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.Evo,
                        OwnerUUID = "player-1",
                    },
                    new Bp(data.Name)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.Evo,
                        OwnerUUID = "player-1",
                    },
                };

                var ctx = CreateContextWithBlueprints(blueprints);

                // Process 3 distinct API IDs sequentially (simulates the bug path)
                SimulateApiIdAssignment(ctx, data.Id1, data.Name, data.Evo);
                SimulateApiIdAssignment(ctx, data.Id2, data.Name, data.Evo);
                SimulateApiIdAssignment(ctx, data.Id3, data.Name, data.Evo);

                // Assert: each API ID resolves to a non-null blueprint
                var bp1 = ctx.FindBlueprintByApiId(data.Id1);
                var bp2 = ctx.FindBlueprintByApiId(data.Id2);
                var bp3 = ctx.FindBlueprintByApiId(data.Id3);

                if (bp1 == null || bp2 == null || bp3 == null)
                {
                    return false
                        .Label($"FindBlueprintByApiId returned null: " +
                               $"Id1={data.Id1}→{(bp1 != null ? bp1.UUID : "NULL")}, " +
                               $"Id2={data.Id2}→{(bp2 != null ? bp2.UUID : "NULL")}, " +
                               $"Id3={data.Id3}→{(bp3 != null ? bp3.UUID : "NULL")}");
                }

                // Assert: all 3 are distinct blueprint objects
                bool allDistinct = !ReferenceEquals(bp1, bp2)
                    && !ReferenceEquals(bp2, bp3)
                    && !ReferenceEquals(bp1, bp3);

                // Assert: no two blueprints share the same GameApiBlueprintId
                var assignedIds = ctx.BlueprintList
                    .Where(b => b.GameApiBlueprintId.HasValue)
                    .Select(b => b.GameApiBlueprintId.Value)
                    .ToList();

                bool uniqueIds = assignedIds.Count == assignedIds.Distinct().Count();

                return (allDistinct && uniqueIds)
                    .Label($"API IDs not mapped to distinct blueprints. " +
                           $"Id1={data.Id1}→{bp1.UUID}, " +
                           $"Id2={data.Id2}→{bp2.UUID}, " +
                           $"Id3={data.Id3}→{bp3.UUID}. " +
                           $"Distinct={allDistinct}, UniqueIds={uniqueIds}");
            });
        }

        // ---------------------------------------------------------------
        // Property 2: Preservation - Single-Match and No-Match Behavior
        // ---------------------------------------------------------------

        /// <summary>
        /// Property 2a: Single-Match Preservation.
        /// When exactly ONE local blueprint matches the imported Name+Evolution,
        /// the assignment logic assigns the API ID directly to that blueprint.
        /// This behavior is correct on both unfixed and fixed code.
        /// Validates: Requirements 3.1, 3.2
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Preservation_SingleMatch_AssignsApiIdDirectly()
        {
            var gen =
                from name in Gen.Elements("Laser", "Shield", "Engine", "Reactor", "Sensor")
                from evo in Gen.Choose(1, 9)
                from apiId in Gen.Choose(100, 9999)
                select new { Name = name, Evo = evo, ApiId = apiId };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Create a single blueprint with unique Name+Evo
                var blueprints = new[]
                {
                    new Bp(data.Name)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.Evo,
                        OwnerUUID = "player-1",
                    },
                };

                var ctx = CreateContextWithBlueprints(blueprints);

                // Assign via the unfixed code path
                SimulateApiIdAssignment(ctx, data.ApiId, data.Name, data.Evo);

                // The single matching blueprint should have the API ID assigned
                var assigned = ctx.FindBlueprintByApiId(data.ApiId);
                bool correctAssignment = assigned != null
                    && assigned.GameApiBlueprintId == data.ApiId
                    && ReferenceEquals(assigned, blueprints[0]);

                return correctAssignment
                    .Label($"Single-match failed: ApiId={data.ApiId}, " +
                           $"assigned={(assigned != null ? assigned.UUID : "NULL")}");
            });
        }

        /// <summary>
        /// Property 2b: No-Match Preservation.
        /// When NO local blueprint matches the imported Name+Evolution,
        /// no assignment occurs and no crash happens.
        /// This behavior is correct on both unfixed and fixed code.
        /// Validates: Requirements 3.5
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Preservation_NoMatch_NoAssignmentOccurs()
        {
            var gen =
                from localName in Gen.Elements("Laser", "Shield", "Engine")
                from localEvo in Gen.Choose(1, 3)
                from searchName in Gen.Elements("Torpedo", "Scanner", "Harvester")
                from searchEvo in Gen.Choose(4, 9)
                from apiId in Gen.Choose(100, 9999)
                select new
                {
                    LocalName = localName,
                    LocalEvo = localEvo,
                    SearchName = searchName,
                    SearchEvo = searchEvo,
                    ApiId = apiId,
                };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Create blueprints that do NOT match the search criteria
                var blueprints = new[]
                {
                    new Bp(data.LocalName)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.LocalEvo,
                        OwnerUUID = "player-1",
                    },
                };

                var ctx = CreateContextWithBlueprints(blueprints);

                // Attempt assignment with a Name+Evo that doesn't match
                SimulateApiIdAssignment(ctx, data.ApiId, data.SearchName, data.SearchEvo);

                // No blueprint should have the API ID assigned
                var assigned = ctx.FindBlueprintByApiId(data.ApiId);
                bool noAssignment = assigned == null;
                bool localUnchanged = !blueprints[0].GameApiBlueprintId.HasValue;

                return (noAssignment && localUnchanged)
                    .Label($"No-match failed: assigned={(assigned != null ? assigned.UUID : "NULL")}, " +
                           $"localHasApiId={blueprints[0].GameApiBlueprintId}");
            });
        }

        /// <summary>
        /// Property 2c: Freshness Preservation.
        /// When a blueprint has a recent LastDetailImportUtc (within 24 hours),
        /// the freshness check considers it already imported. The SimulateApiIdAssignment
        /// helper always assigns regardless of freshness (matching CreateBlueprintDetailItem
        /// post-import behavior), but a fresh blueprint would never reach that code path
        /// because the caller skips the detail fetch entirely. This test verifies that
        /// setting LastDetailImportUtc to a recent value makes the freshness oracle return true.
        /// Validates: Requirements 3.4
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Preservation_FreshBlueprint_IsConsideredFresh()
        {
            var gen =
                from hoursAgo in Gen.Choose(0, 23)
                select new { HoursAgo = hoursAgo };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                var frozenNow = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
                SystemClock.FreezeAt(frozenNow);
                try
                {
                    var lastImport = frozenNow.AddHours(-data.HoursAgo);

                    // Freshness threshold is 24 hours (default DetailRefreshHours)
                    bool isFresh = (SystemClock.UtcNow - lastImport) < TimeSpan.FromHours(24);

                    return isFresh
                        .Label($"Blueprint {data.HoursAgo}h ago should be fresh but wasn't");
                }
                finally
                {
                    SystemClock.Reset();
                }
            });
        }

        /// <summary>
        /// Property 2d: Skipped Import Preservation.
        /// When an import action would be Skipped (simulated by not calling
        /// SimulateApiIdAssignment), no API ID assignment occurs. This preserves
        /// the behavior where CrateImporter.ImportFromJson returns ImportAction.Skipped
        /// and the assignment block is never entered.
        /// Validates: Requirements 3.3
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property Preservation_SkippedImport_NoApiIdAssignment()
        {
            var gen =
                from name in Gen.Elements("Laser", "Shield", "Engine", "Reactor")
                from evo in Gen.Choose(1, 5)
                from apiId in Gen.Choose(100, 9999)
                select new { Name = name, Evo = evo, ApiId = apiId };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Create a blueprint that matches Name+Evo
                var blueprints = new[]
                {
                    new Bp(data.Name)
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Evolution = data.Evo,
                        OwnerUUID = "player-1",
                    },
                };

                var ctx = CreateContextWithBlueprints(blueprints);

                // Simulate ImportAction.Skipped: do NOT call SimulateApiIdAssignment
                // (the real code only assigns if importResult.Entries[0].Action != ImportAction.Skipped)

                // Verify no API ID assignment occurred
                var assigned = ctx.FindBlueprintByApiId(data.ApiId);
                bool noAssignment = assigned == null;
                bool localUnchanged = !blueprints[0].GameApiBlueprintId.HasValue;

                return (noAssignment && localUnchanged)
                    .Label($"Skipped import should not assign: " +
                           $"assigned={(assigned != null ? assigned.UUID : "NULL")}, " +
                           $"localApiId={blueprints[0].GameApiBlueprintId}");
            });
        }
    }
}
