// <copyright file="DirtyTrackingPropertyTests.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using Bp = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for DirtyTracker correctness within the
    /// PlayerContext backend integration.
    /// Satisfies: Req 2, Criteria 4-7.
    /// </summary>
    [TestFixture]
    public class DirtyTrackingPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            PlayerContext.WritesBlocked = false;
            PlayerContext.IsServerOnlyMode = null;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerContext.Reset();
            PlayerContext.FilePath = string.Empty;
            PlayerContext.WritesBlocked = false;
            PlayerContext.IsServerOnlyMode = null;
        }

        // -------------------------------------------------------------------
        // Property 1: Write Idempotency
        // After WriteContext with no mutations, DirtyTracker.HasChanges == false
        // AND no backend Upsert calls made.
        // **Validates: Requirements 2.5, 2.7**
        // -------------------------------------------------------------------

        [Test]
        public void WriteIdempotency_NoMutations_NoDirtyCalls()
        {
            var backend = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-1", ColonyName = "Alpha" },
                },
            };

            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;
            ctx.CurrentPlayerUUID = "char-1";

            // Clear UpsertCalls recorded during load (if any)
            backend.UpsertCalls.Clear();

            // No mutations — call WriteContext
            ctx.WriteContext();

            Assert.That(ctx.DirtyTracker.HasChanges, Is.False,
                "No mutations means no dirty flags");
            Assert.That(backend.UpsertCalls, Is.Empty,
                "No dirty entities means zero Upsert calls");
        }

        // -------------------------------------------------------------------
        // Property 2: Dirty Flag Completeness
        // For a random colony name, creating a colony via ColonyService marks
        // that colony's UUID as dirty in DirtyTracker.
        // **Validates: Requirements 2.4, 2.5**
        // -------------------------------------------------------------------

        private static Gen<string> SafeColonyNameGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property DirtyFlagCompleteness_ColonyCreate_MarksDirty()
        {
            return Prop.ForAll(
                Arb.From(SafeColonyNameGen()),
                Arb.From(SafeColonyNameGen()),
                (colonyName, planetName) =>
            {
                PlayerContext.Reset();
                PlayerContext.FilePath = string.Empty;
                PlayerContext.WritesBlocked = false;
                PlayerContext.IsServerOnlyMode = null;

                var backend = new RecordingStorageBackend();
                var ctx = new PlayerContext(new PlayerRoot());
                ctx.StorageBackend = backend;
                ctx.CurrentPlayerUUID = "test-player-uuid";

                var service = new ColonyService(ctx);
                var request = new ColonyCreateRequest
                {
                    PlanetName = planetName,
                    ColonyName = colonyName,
                    SystemName = "TestSystem",
                };

                // Create calls WriteContext internally (which persists + clears dirty)
                // But we can verify that the colony was indeed persisted via UpsertCalls
                var result = service.Create(request);

                // The colony should have been upserted to the backend
                bool wasUpserted = backend.UpsertCalls.Any(
                    c => c.EntityType == typeof(Colony)
                      && c.EntityUUID == result.UUID);

                return wasUpserted.Label("Colony UUID present in UpsertCalls");
            });
        }

        // -------------------------------------------------------------------
        // Property 3: Load-Write Round Trip
        // LoadFromBackend then immediate WriteContext → zero dirty entities
        // → zero backend Upsert calls.
        // **Validates: Requirements 2.6, 2.7**
        // -------------------------------------------------------------------

        [Test]
        public void LoadWriteRoundTrip_FreshLoad_ZeroDirtyEntities_ZeroUpserts()
        {
            var backend = new RecordingStorageBackend
            {
                ColoniesToReturn = new List<Colony>
                {
                    new Colony { UUID = "col-1", ColonyName = "Alpha" },
                    new Colony { UUID = "col-2", ColonyName = "Beta" },
                },
                BlueprintsToReturn = new List<Bp>
                {
                    new Bp { UUID = "bp-1", Name = "LaserMk1" },
                },
            };

            var ctx = new PlayerContext(new PlayerRoot());
            ctx.StorageBackend = backend;

            // Setting CurrentPlayerUUID triggers LoadFromBackend
            ctx.CurrentPlayerUUID = "char-1";

            // After load, DirtyTracker should be clean
            Assert.That(ctx.DirtyTracker.HasChanges, Is.False,
                "Fresh load should start with zero dirty flags");

            // Clear any calls recorded during load setup
            backend.UpsertCalls.Clear();

            // Immediately WriteContext without any mutations
            ctx.WriteContext();

            Assert.That(backend.UpsertCalls, Is.Empty,
                "No dirty entities after fresh load means zero Upsert calls");
        }

        // -------------------------------------------------------------------
        // Property 4: WritesBlocked Monotonicity
        // After a StorageWriteException sets WritesBlocked = true, subsequent
        // WriteContext calls produce zero backend calls and no exceptions.
        // **Validates: Requirements 8.1, 8.2**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WritesBlocked_Monotonicity_NoBackendCallsAfterException()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 5)),
                subsequentCalls =>
                {
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    PlayerContext.WritesBlocked = false;
                    PlayerContext.IsServerOnlyMode = null;

                    var backend = new RecordingStorageBackend
                    {
                        ColoniesToReturn = new List<Colony>
                        {
                            new Colony { UUID = "col-1", ColonyName = "Alpha" },
                        },
                    };

                    var ctx = new PlayerContext(new PlayerRoot());
                    ctx.StorageBackend = backend;
                    ctx.CurrentPlayerUUID = "char-1";

                    // Mark dirty and trigger exception
                    ctx.MarkDirty<Colony>("col-1");
                    backend.ThrowOnUpsert = true;

                    try
                    {
                        ctx.WriteContext();
                    }
                    catch (StorageWriteException)
                    {
                        // Expected
                    }

                    // Assert: WritesBlocked is now true
                    bool blockedAfterException = PlayerContext.WritesBlocked;

                    // Clear upsert calls and mark dirty again
                    backend.UpsertCalls.Clear();
                    backend.ThrowOnUpsert = false;
                    ctx.MarkDirty<Colony>("col-1");

                    // Act: call WriteContext N more times
                    for (int i = 0; i < subsequentCalls; i++)
                    {
                        ctx.WriteContext();
                    }

                    int callsAfterBlocked = backend.UpsertCalls.Count;

                    return blockedAfterException
                        .Label("WritesBlocked should be true after exception")
                        .And((callsAfterBlocked == 0).Label(
                            string.Format(
                                "Expected 0 backend calls after block, got {0}",
                                callsAfterBlocked)));
                });
        }

        // -------------------------------------------------------------------
        // Property 5: WritesBlocked Reset
        // After setting WritesBlocked = false, the next WriteContext resumes
        // normal persistence behavior.
        // **Validates: Requirements 8.3, 8.4**
        // -------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property WritesBlocked_Reset_ResumesNormalBehavior()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 3)),
                writesAfterReset =>
                {
                    PlayerContext.Reset();
                    PlayerContext.FilePath = string.Empty;
                    PlayerContext.WritesBlocked = false;
                    PlayerContext.IsServerOnlyMode = null;

                    var backend = new RecordingStorageBackend
                    {
                        ColoniesToReturn = new List<Colony>
                        {
                            new Colony { UUID = "col-1", ColonyName = "Alpha" },
                        },
                    };

                    var ctx = new PlayerContext(new PlayerRoot());
                    ctx.StorageBackend = backend;
                    ctx.CurrentPlayerUUID = "char-1";

                    // Trigger exception to set WritesBlocked
                    ctx.MarkDirty<Colony>("col-1");
                    backend.ThrowOnUpsert = true;

                    try
                    {
                        ctx.WriteContext();
                    }
                    catch (StorageWriteException)
                    {
                        // Expected
                    }

                    // Reset WritesBlocked and backend
                    PlayerContext.WritesBlocked = false;
                    backend.ThrowOnUpsert = false;
                    backend.UpsertCalls.Clear();

                    // Mark dirty and write
                    ctx.MarkDirty<Colony>("col-1");
                    ctx.WriteContext();

                    int callsAfterReset = backend.UpsertCalls.Count;

                    return (callsAfterReset > 0).Label(
                        string.Format(
                            "Expected backend calls after reset, got {0}",
                            callsAfterReset));
                });
        }
    }
}
