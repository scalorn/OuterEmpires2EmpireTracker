using System;
using System.Collections.Generic;
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
    /// Property tests for RemapUUID colony reference walking.
    /// **Validates: Requirements 2.5**
    /// </summary>
    [TestFixture]
    public class RemapUUIDColonyPropertyTests
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

        #region Property 3: RemapUUID walks all colony references

        /// <summary>
        /// For any set of colonies, delivery routes, and delivery plans whose
        /// ColonyUUID fields match oldUUID, after RemapUUID.Remap all those
        /// references shall equal newUUID with no oldUUID remaining.
        /// **Validates: Requirements 2.5**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property RemapWalksAllColonyReferences()
        {
            var gen = from oldUuid in Arb.Default.NonEmptyString().Generator.Select(s => "old-" + s.Get)
                      from newUuid in Arb.Default.NonEmptyString().Generator.Select(s => "new-" + s.Get)
                      from colonyCount in Gen.Choose(1, 5)
                      from routeCount in Gen.Choose(0, 4)
                      from stopsPerRoute in Gen.Choose(1, 4)
                      from planCount in Gen.Choose(0, 4)
                      from stopsPerPlan in Gen.Choose(1, 4)
                      from plantMask in Gen.Choose(0, 255)
                      select new
                      {
                          OldUuid = oldUuid,
                          NewUuid = newUuid,
                          ColonyCount = colonyCount,
                          RouteCount = routeCount,
                          StopsPerRoute = stopsPerRoute,
                          PlanCount = planCount,
                          StopsPerPlan = stopsPerPlan,
                          PlantMask = plantMask
                      };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                EmpireContext.Reset();
                PlayerContext.Reset();
                TestHelper.SetAllFilePaths();
                var ec = EmpireContext.GetInstance();
                var pc = PlayerContext.GetInstance();

                int bitIndex = 0;
                int plantedCount = 0;

                // Add colonies — some with UUID == oldUuid
                for (int i = 0; i < data.ColonyCount; i++)
                {
                    var colony = new Colony();
                    bool plant = ((data.PlantMask >> (bitIndex++ % 8)) & 1) == 1;
                    colony.UUID = plant ? data.OldUuid : Guid.NewGuid().ToString();
                    colony.PlanetName = $"Planet_{i}";
                    colony.SystemName = $"System_{i}";
                    colony.ColonyName = $"Colony_{i}";
                    if (plant) plantedCount++;
                    pc.ColonyList.Add(colony);
                }

                // Add delivery routes with stops — some ColonyUUID == oldUuid
                for (int r = 0; r < data.RouteCount; r++)
                {
                    var route = new DeliveryRoute();
                    route.UUID = Guid.NewGuid().ToString();
                    route.Name = $"Route_{r}";
                    for (int s = 0; s < data.StopsPerRoute; s++)
                    {
                        var stop = new RouteStop();
                        bool plant = ((data.PlantMask >> (bitIndex++ % 8)) & 1) == 1;
                        stop.ColonyUUID = plant ? data.OldUuid : Guid.NewGuid().ToString();
                        stop.Sequence = s;
                        if (plant) plantedCount++;
                        route.Stops.Add(stop);
                    }
                    pc.DeliveryRouteList.Add(route);
                }

                // Add delivery plans with stops — some ColonyUUID == oldUuid
                for (int p = 0; p < data.PlanCount; p++)
                {
                    var plan = new DeliveryPlan();
                    plan.UUID = Guid.NewGuid().ToString();
                    plan.Name = $"Plan_{p}";
                    for (int s = 0; s < data.StopsPerPlan; s++)
                    {
                        var stop = new DeliveryPlanStop();
                        bool plant = ((data.PlantMask >> (bitIndex++ % 8)) & 1) == 1;
                        stop.ColonyUUID = plant ? data.OldUuid : Guid.NewGuid().ToString();
                        stop.Sequence = s;
                        if (plant) plantedCount++;
                        stop.DropOff = new List<DeliveryItem>();
                        stop.PickUp = new List<DeliveryItem>();
                        plan.Stops.Add(stop);
                    }
                    pc.DeliveryPlanList.Add(plan);
                }

                // Act
                RemapUUID.Remap(ec, pc, data.OldUuid, data.NewUuid);

                // Collect all colony-related UUID fields
                var colonyUuids = pc.ColonyList.Select(c => c.UUID).ToList();
                var routeStopUuids = pc.DeliveryRouteList
                    .SelectMany(r => r.Stops)
                    .Select(s => s.ColonyUUID).ToList();
                var planStopUuids = pc.DeliveryPlanList
                    .SelectMany(p => p.Stops)
                    .Select(s => s.ColonyUUID).ToList();

                var allUuids = colonyUuids.Concat(routeStopUuids).Concat(planStopUuids).ToList();
                bool noOldRemains = !allUuids.Any(u => u == data.OldUuid);

                return noOldRemains
                    .Label($"Old UUID '{data.OldUuid}' still found after Remap. " +
                           $"Planted {plantedCount} refs. " +
                           $"Remaining: colonies={colonyUuids.Count(u => u == data.OldUuid)}, " +
                           $"routeStops={routeStopUuids.Count(u => u == data.OldUuid)}, " +
                           $"planStops={planStopUuids.Count(u => u == data.OldUuid)}");
            });
        }

        #endregion
    }
}
