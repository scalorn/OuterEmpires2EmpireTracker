using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Tests.Services.Migration
{
    /// <summary>
    /// Property tests for Migration002_ColonyDeterministicUUIDs.
    /// Covers stale FlatpackBlueprintUUID cleanup, colony UUID migration
    /// with LegacyUUID preservation, and migration idempotency.
    /// </summary>
    [TestFixture]
    public class Migration002PropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            TestHelper.SetAllFilePaths();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
        }

        #region Property 4: Stale FlatpackBlueprintUUID cleanup

        /// <summary>
        /// For any colony structure whose FlatpackBlueprintUUID matches a
        /// global blueprint's LegacyUUID, after Migration002 runs the
        /// structure's FlatpackBlueprintUUID should equal that blueprint's
        /// current UUID.
        /// **Validates: Requirements 2.1**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property StaleFlatpackBlueprintUUIDsAreRemapped()
        {
            var gen = from bpCount in Gen.Choose(1, 5)
                      from colonyCount in Gen.Choose(1, 4)
                      from structsPerColony in Gen.Choose(1, 4)
                      from staleMask in Gen.Choose(1, 255) // at least one stale
                      select new
                      {
                          BpCount = bpCount,
                          ColonyCount = colonyCount,
                          StructsPerColony = structsPerColony,
                          StaleMask = staleMask
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                EmpireContext.Reset();
                PlayerContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Clear existing data
                foreach (var item in ec.GlobalBlueprintList.ToList()) ec.RemoveGlobalBlueprint(item);
                foreach (var item in pc.ColonyList.ToList()) pc.RemoveColony(item);

                // Create blueprints with LegacyUUIDs (simulating post-Migration001 state)
                var blueprints = new List<OE2EmpireTracker.Models.Blueprint>();
                for (int i = 0; i < data.BpCount; i++)
                {
                    var bp = new OE2EmpireTracker.Models.Blueprint($"TestBP_{i}");
                    bp.UUID = $"current-uuid-{i}";
                    bp.LegacyUUID = $"legacy-uuid-{i}";
                    bp.BluePrintType = "Flatpack Building";
                    bp.Evolution = 0;
                    bp.TechLevel = "Low";
                    bp.Class = 1;
                    blueprints.Add(bp);
                    ec.AddGlobalBlueprint(bp);
                }

                // Create colonies with structures -- some referencing legacy UUIDs
                int bitIndex = 0;
                var expectedMappings = new Dictionary<string, string>(); // structKey -> expected UUID
                for (int c = 0; c < data.ColonyCount; c++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.PlanetName = $"Planet_{c}";
                    colony.SystemName = $"System_{c}";
                    colony.OwnerUUID = "owner-1";

                    for (int s = 0; s < data.StructsPerColony; s++)
                    {
                        var structure = new ColonyStructure();
                        structure.UUID = Guid.NewGuid().ToString();
                        int bpIdx = s % data.BpCount;
                        bool stale = ((data.StaleMask >> (bitIndex++ % 8)) & 1) == 1;

                        if (stale)
                        {
                            // Reference the legacy UUID (stale)
                            structure.FlatpackBlueprintUUID = blueprints[bpIdx].LegacyUUID;
                            expectedMappings[$"{c}_{s}"] = blueprints[bpIdx].UUID;
                        }
                        else
                        {
                            // Already current
                            structure.FlatpackBlueprintUUID = blueprints[bpIdx].UUID;
                            expectedMappings[$"{c}_{s}"] = blueprints[bpIdx].UUID;
                        }

                        structure.displaySequence = s;
                        colony.Structures.Add(structure);
                    }

                    pc.AddColony(colony);
                }

                // Act
                Migration002_ColonyDeterministicUUIDs.Run(ec, pc);

                // Assert: all FlatpackBlueprintUUIDs should now be current UUIDs
                bool allRemapped = true;
                string failMsg = "";
                for (int c = 0; c < pc.ColonyList.Count; c++)
                {
                    for (int s = 0; s < pc.ColonyList[c].Structures.Count; s++)
                    {
                        var structure = pc.ColonyList[c].Structures[s];
                        string expected = expectedMappings[$"{c}_{s}"];
                        if (structure.FlatpackBlueprintUUID != expected)
                        {
                            allRemapped = false;
                            failMsg = $"Colony {c} Structure {s}: expected '{expected}', got '{structure.FlatpackBlueprintUUID}'";
                            break;
                        }
                    }

                    if (!allRemapped) break;
                }

                // Also verify no structure references any legacy UUID
                var legacyUuids = new HashSet<string>(blueprints.Select(bp => bp.LegacyUUID));
                bool noLegacyRemains = pc.ColonyList
                    .SelectMany(col => col.Structures)
                    .All(st => !legacyUuids.Contains(st.FlatpackBlueprintUUID));

                return (allRemapped && noLegacyRemains)
                    .Label(failMsg.Length > 0 ? failMsg : "Legacy UUID still found in structures");
            });
        }

        #endregion

        #region Property 2: Colony UUID migration preserves LegacyUUID

        /// <summary>
        /// For any colony with a random UUID, after migration the colony's
        /// LegacyUUID should equal the original random UUID, and the colony's
        /// UUID should equal the deterministic UUID. Running migration again
        /// should not change LegacyUUID.
        /// **Validates: Requirements 2.4, 2.6**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property ColonyUUIDMigrationPreservesLegacyUUID()
        {
            var gen = from colonyCount in Gen.Choose(1, 5)
                      select colonyCount;

            return Prop.ForAll(gen.ToArbitrary(), colonyCount =>
            {
                EmpireContext.Reset();
                PlayerContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Clear existing data
                foreach (var item in pc.ColonyList.ToList()) pc.RemoveColony(item);
                foreach (var item in pc.DeliveryRouteList.ToList()) pc.RemoveDeliveryRoute(item);
                foreach (var item in pc.DeliveryPlanList.ToList()) pc.RemoveDeliveryPlan(item);

                // Create colonies with random UUIDs
                var originalUUIDs = new Dictionary<int, string>();
                for (int i = 0; i < colonyCount; i++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.PlanetName = $"Planet_{i}";
                    colony.SystemName = $"System_{i}";
                    colony.OwnerUUID = "owner-1";
                    originalUUIDs[i] = colony.UUID;
                    pc.AddColony(colony);
                }

                // Act -- first migration run
                Migration002_ColonyDeterministicUUIDs.Run(ec, pc);

                // Assert after first run
                bool firstRunOk = true;
                string failMsg = "";
                for (int i = 0; i < colonyCount; i++)
                {
                    var colony = pc.ColonyList[i];
                    string expectedDeterministic = DeterministicUUID.Generate(
                        "owner-1", $"Planet_{i}", $"System_{i}");

                    if (colony.UUID != expectedDeterministic)
                    {
                        firstRunOk = false;
                        failMsg = $"Colony {i}: UUID expected '{expectedDeterministic}', got '{colony.UUID}'";
                        break;
                    }

                    if (colony.LegacyUUID != originalUUIDs[i])
                    {
                        firstRunOk = false;
                        failMsg = $"Colony {i}: LegacyUUID expected '{originalUUIDs[i]}', got '{colony.LegacyUUID}'";
                        break;
                    }
                }

                if (!firstRunOk)
                    return false.Label(failMsg);

                // Capture LegacyUUIDs after first run
                var legacyAfterFirst = pc.ColonyList
                    .Select(c => c.LegacyUUID).ToList();

                // Act -- second migration run
                Migration002_ColonyDeterministicUUIDs.Run(ec, pc);

                // Assert LegacyUUID unchanged after second run
                bool secondRunOk = true;
                for (int i = 0; i < colonyCount; i++)
                {
                    if (pc.ColonyList[i].LegacyUUID != legacyAfterFirst[i])
                    {
                        secondRunOk = false;
                        failMsg = $"Colony {i}: LegacyUUID changed from '{legacyAfterFirst[i]}' to '{pc.ColonyList[i].LegacyUUID}' on second run";
                        break;
                    }
                }

                return secondRunOk.Label(failMsg);
            });
        }

        #endregion

        #region Property 5: Migration idempotency

        /// <summary>
        /// Running Migration002 twice on the same data should produce
        /// identical colony UUIDs, LegacyUUIDs, and all delivery route/plan
        /// references after both runs.
        /// **Validates: Requirements 3.4**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MigrationIsIdempotent()
        {
            var gen = from colonyCount in Gen.Choose(1, 4)
                      from routeCount in Gen.Choose(0, 3)
                      from stopsPerRoute in Gen.Choose(1, 3)
                      from planCount in Gen.Choose(0, 3)
                      from stopsPerPlan in Gen.Choose(1, 3)
                      select new
                      {
                          ColonyCount = colonyCount,
                          RouteCount = routeCount,
                          StopsPerRoute = stopsPerRoute,
                          PlanCount = planCount,
                          StopsPerPlan = stopsPerPlan
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                EmpireContext.Reset();
                PlayerContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                // Clear existing data
                foreach (var item in pc.ColonyList.ToList()) pc.RemoveColony(item);
                foreach (var item in pc.DeliveryRouteList.ToList()) pc.RemoveDeliveryRoute(item);
                foreach (var item in pc.DeliveryPlanList.ToList()) pc.RemoveDeliveryPlan(item);

                // Create colonies with random UUIDs
                var colonyUUIDs = new List<string>();
                for (int i = 0; i < data.ColonyCount; i++)
                {
                    var colony = new Colony();
                    colony.UUID = Guid.NewGuid().ToString();
                    colony.PlanetName = $"Planet_{i}";
                    colony.SystemName = $"System_{i}";
                    colony.OwnerUUID = "owner-1";
                    colonyUUIDs.Add(colony.UUID);
                    pc.AddColony(colony);
                }

                // Create delivery routes referencing colony UUIDs
                for (int r = 0; r < data.RouteCount; r++)
                {
                    var route = new DeliveryRoute();
                    route.UUID = Guid.NewGuid().ToString();
                    route.Name = $"Route_{r}";
                    for (int s = 0; s < data.StopsPerRoute; s++)
                    {
                        var stop = new RouteStop();
                        stop.ColonyUUID = colonyUUIDs[s % data.ColonyCount];
                        stop.Sequence = s;
                        route.Stops.Add(stop);
                    }

                    pc.AddDeliveryRoute(route);
                }

                // Create delivery plans referencing colony UUIDs
                for (int p = 0; p < data.PlanCount; p++)
                {
                    var plan = new DeliveryPlan();
                    plan.UUID = Guid.NewGuid().ToString();
                    plan.Name = $"Plan_{p}";
                    for (int s = 0; s < data.StopsPerPlan; s++)
                    {
                        var stop = new DeliveryPlanStop();
                        stop.ColonyUUID = colonyUUIDs[s % data.ColonyCount];
                        stop.Sequence = s;
                        stop.DropOff = new List<DeliveryItem>();
                        stop.PickUp = new List<DeliveryItem>();
                        plan.Stops.Add(stop);
                    }

                    pc.AddDeliveryPlan(plan);
                }

                // Act -- first migration run
                Migration002_ColonyDeterministicUUIDs.Run(ec, pc);

                // Snapshot state after first run
                var uuidsAfterFirst = pc.ColonyList.Select(c => c.UUID).ToList();
                var legacyAfterFirst = pc.ColonyList.Select(c => c.LegacyUUID).ToList();
                var routeStopUuidsAfterFirst = pc.DeliveryRouteList
                    .SelectMany(r => r.Stops).Select(s => s.ColonyUUID).ToList();
                var planStopUuidsAfterFirst = pc.DeliveryPlanList
                    .SelectMany(p => p.Stops).Select(s => s.ColonyUUID).ToList();

                // Act -- second migration run
                Migration002_ColonyDeterministicUUIDs.Run(ec, pc);

                // Snapshot state after second run
                var uuidsAfterSecond = pc.ColonyList.Select(c => c.UUID).ToList();
                var legacyAfterSecond = pc.ColonyList.Select(c => c.LegacyUUID).ToList();
                var routeStopUuidsAfterSecond = pc.DeliveryRouteList
                    .SelectMany(r => r.Stops).Select(s => s.ColonyUUID).ToList();
                var planStopUuidsAfterSecond = pc.DeliveryPlanList
                    .SelectMany(p => p.Stops).Select(s => s.ColonyUUID).ToList();

                // Assert all identical
                bool uuidsMatch = uuidsAfterFirst.SequenceEqual(uuidsAfterSecond);
                bool legacyMatch = legacyAfterFirst.SequenceEqual(legacyAfterSecond);
                bool routeStopsMatch = routeStopUuidsAfterFirst.SequenceEqual(routeStopUuidsAfterSecond);
                bool planStopsMatch = planStopUuidsAfterFirst.SequenceEqual(planStopUuidsAfterSecond);

                return (uuidsMatch && legacyMatch && routeStopsMatch && planStopsMatch)
                    .Label($"Idempotency failed: UUIDs={uuidsMatch}, Legacy={legacyMatch}, " +
                           $"RouteStops={routeStopsMatch}, PlanStops={planStopsMatch}");
            });
        }

        #endregion
    }
}
