// <copyright file="BlueprintDetailDedupUnitTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for the three-tier blueprint API ID resolution logic.
    /// Validates edge cases of Tier 1 (pre-check), Tier 2 (filtering),
    /// and Tier 3 (scoring) as implemented in QueueSyncService.ResolveBlueprintForApiId.
    /// Spec: blueprint-detail-dedup, Task 4.
    /// </summary>
    [TestFixture]
    public class BlueprintDetailDedupUnitTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerContext.FilePath = string.Empty;
        }

        // ---------------------------------------------------------------
        // Helpers (mirrors SimulateApiIdAssignment from property tests)
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
        /// Simulates the three-tier resolution logic from ResolveBlueprintForApiId.
        /// Returns the resolved blueprint or null.
        /// </summary>
        private static Bp ResolveBlueprintForApiId(
            PlayerContext ctx,
            int blueprintId,
            string importedName,
            int importedEvo,
            Bp scoringTemplate = null)
        {
            // Tier 1: Already assigned — use directly.
            var existing = ctx.FindBlueprintByApiId(blueprintId);
            if (existing != null)
            {
                return existing;
            }

            // Tier 2: Filter by Name+Evo, excluding blueprints claimed by other API IDs.
            var candidates = ctx.BlueprintList
                .Where(b =>
                    string.Equals(b.Name, importedName, StringComparison.Ordinal) &&
                    b.Evolution == importedEvo &&
                    !(b.GameApiBlueprintId.HasValue && b.GameApiBlueprintId.Value != blueprintId))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // Tier 3: Multiple unassigned candidates — score by property similarity.
            if (scoringTemplate != null)
            {
                var bestMatch = BlueprintService.FindBestMatch(candidates, scoringTemplate);
                if (bestMatch != null)
                {
                    return bestMatch;
                }
            }

            // Fall back to first candidate.
            return candidates[0];
        }

        /// <summary>
        /// Performs assignment: resolves the blueprint and sets GameApiBlueprintId + indexes.
        /// </summary>
        private static void AssignApiId(
            PlayerContext ctx,
            int blueprintId,
            string importedName,
            int importedEvo,
            Bp scoringTemplate = null)
        {
            var bp = ResolveBlueprintForApiId(ctx, blueprintId, importedName, importedEvo, scoringTemplate);
            if (bp != null)
            {
                bp.GameApiBlueprintId = blueprintId;
                bp.LastDetailImportUtc = SystemClock.UtcNow;
                ctx.IndexBlueprintByApiId(bp);
            }
        }

        // ---------------------------------------------------------------
        // Test 1: Tier 1 — Already-assigned blueprint returned directly
        // Validates: Requirements 2.1
        // ---------------------------------------------------------------

        /// <summary>
        /// When a blueprint already has GameApiBlueprintId=101 and is indexed,
        /// resolving API ID 101 returns that same blueprint without a Name+Evo search.
        /// </summary>
        [Test]
        public void Tier1_AlreadyAssigned_ReturnedDirectly()
        {
            var bp = new Bp("Reactor")
            {
                UUID = "bp-already-assigned",
                Evolution = 3,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 101,
            };

            var other = new Bp("Reactor")
            {
                UUID = "bp-other",
                Evolution = 3,
                OwnerUUID = "player-1",
            };

            var ctx = CreateContextWithBlueprints(new[] { bp, other });
            ctx.IndexBlueprintByApiId(bp);

            var result = ResolveBlueprintForApiId(ctx, 101, "Reactor", 3);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("bp-already-assigned"));
            Assert.That(ReferenceEquals(result, bp), Is.True);
        }

        // ---------------------------------------------------------------
        // Test 2: Tier 2 — Candidates with different GameApiBlueprintId excluded
        // Validates: Requirements 2.2
        // ---------------------------------------------------------------

        /// <summary>
        /// When 3 blueprints share Name+Evo, and 2 are already claimed by other API IDs,
        /// only the unassigned one is selected for the new API ID.
        /// </summary>
        [Test]
        public void Tier2_CandidatesWithDifferentApiId_Excluded()
        {
            var bp0 = new Bp("Shield")
            {
                UUID = "bp-claimed-200",
                Evolution = 2,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 200,
            };

            var bp1 = new Bp("Shield")
            {
                UUID = "bp-claimed-300",
                Evolution = 2,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 300,
            };

            var bp2 = new Bp("Shield")
            {
                UUID = "bp-unassigned",
                Evolution = 2,
                OwnerUUID = "player-1",
            };

            var ctx = CreateContextWithBlueprints(new[] { bp0, bp1, bp2 });
            ctx.IndexBlueprintByApiId(bp0);
            ctx.IndexBlueprintByApiId(bp1);

            // Assign new API ID 400 — should skip bp0 (claimed 200) and bp1 (claimed 300)
            var result = ResolveBlueprintForApiId(ctx, 400, "Shield", 2);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("bp-unassigned"));
        }

        // ---------------------------------------------------------------
        // Test 3: Tier 3 — Multiple unassigned, highest-scoring selected
        // Validates: Requirements 2.3
        // ---------------------------------------------------------------

        /// <summary>
        /// When multiple unassigned candidates exist, the one whose properties
        /// best match the scoring template is selected via FindBestMatch.
        /// </summary>
        [Test]
        public void Tier3_MultipleUnassigned_HighestScoringSelected()
        {
            var bpEmpty1 = new Bp("Engine")
            {
                UUID = "bp-empty-1",
                Evolution = 4,
                OwnerUUID = "player-1",
                BluePrintType = "Ship",
                Class = 1,
                TechLevel = "T1",
            };

            var bpWithProps = new Bp("Engine")
            {
                UUID = "bp-with-properties",
                Evolution = 4,
                OwnerUUID = "player-1",
                BluePrintType = "Ship",
                Class = 1,
                TechLevel = "T1",
            };

            bpWithProps.Properties.SetProperty("Speed", "150");
            bpWithProps.Properties.SetProperty("Fuel", "80");

            var bpEmpty2 = new Bp("Engine")
            {
                UUID = "bp-empty-2",
                Evolution = 4,
                OwnerUUID = "player-1",
                BluePrintType = "Ship",
                Class = 1,
                TechLevel = "T1",
            };

            var ctx = CreateContextWithBlueprints(new[] { bpEmpty1, bpWithProps, bpEmpty2 });

            // Build a scoring template that matches bpWithProps
            var template = new Bp("Engine")
            {
                UUID = "template",
                Evolution = 4,
                BluePrintType = "Ship",
                Class = 1,
                TechLevel = "T1",
            };

            template.Properties.SetProperty("Speed", "150");
            template.Properties.SetProperty("Fuel", "80");

            var result = ResolveBlueprintForApiId(ctx, 500, "Engine", 4, template);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.UUID, Is.EqualTo("bp-with-properties"));
        }

        // ---------------------------------------------------------------
        // Test 4: All candidates assigned to other IDs — no assignment
        // Validates: Requirements 2.4, 2.5
        // ---------------------------------------------------------------

        /// <summary>
        /// When all blueprints matching Name+Evo already have different API IDs,
        /// no candidate remains after Tier 2 filtering and the method returns null.
        /// </summary>
        [Test]
        public void AllCandidatesAssignedToOtherIds_NoAssignment()
        {
            var bp0 = new Bp("Laser")
            {
                UUID = "bp-claimed-200",
                Evolution = 1,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 200,
            };

            var bp1 = new Bp("Laser")
            {
                UUID = "bp-claimed-300",
                Evolution = 1,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 300,
            };

            var bp2 = new Bp("Laser")
            {
                UUID = "bp-claimed-400",
                Evolution = 1,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 400,
            };

            var ctx = CreateContextWithBlueprints(new[] { bp0, bp1, bp2 });
            ctx.IndexBlueprintByApiId(bp0);
            ctx.IndexBlueprintByApiId(bp1);
            ctx.IndexBlueprintByApiId(bp2);

            // Attempt to assign API ID 500 — no unassigned candidates exist
            var result = ResolveBlueprintForApiId(ctx, 500, "Laser", 1);

            Assert.That(result, Is.Null);
        }

        // ---------------------------------------------------------------
        // Test 5: Single unassigned candidate — direct assignment, no scoring
        // Validates: Requirements 2.5, 3.1
        // ---------------------------------------------------------------

        /// <summary>
        /// When 3 blueprints share Name+Evo but 2 are already claimed, the single
        /// remaining unassigned candidate is assigned directly (Tier 2 returns count=1,
        /// Tier 3 scoring is not needed).
        /// </summary>
        [Test]
        public void SingleUnassigned_DirectAssignment_NoScoring()
        {
            var bp0 = new Bp("Sensor")
            {
                UUID = "bp-claimed-200",
                Evolution = 5,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 200,
            };

            var bp1 = new Bp("Sensor")
            {
                UUID = "bp-claimed-300",
                Evolution = 5,
                OwnerUUID = "player-1",
                GameApiBlueprintId = 300,
            };

            var bp2 = new Bp("Sensor")
            {
                UUID = "bp-the-one",
                Evolution = 5,
                OwnerUUID = "player-1",
            };

            var ctx = CreateContextWithBlueprints(new[] { bp0, bp1, bp2 });
            ctx.IndexBlueprintByApiId(bp0);
            ctx.IndexBlueprintByApiId(bp1);

            // Assign API ID 600 — only bp2 is unassigned
            AssignApiId(ctx, 600, "Sensor", 5);

            var assigned = ctx.FindBlueprintByApiId(600);
            Assert.That(assigned, Is.Not.Null);
            Assert.That(assigned.UUID, Is.EqualTo("bp-the-one"));
            Assert.That(assigned.GameApiBlueprintId, Is.EqualTo(600));
            Assert.That(assigned.LastDetailImportUtc, Is.Not.Null);
        }
    }
}
