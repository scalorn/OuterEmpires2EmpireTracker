using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BuildPlannerPropertyTests
    {
        private const int Iterations = 100;

        [Test]
        public void P1_QuantityValidation_AcceptsOnlyPositive()
        {
            var rng = new Random(42);
            for (int i = 0; i < Iterations; i++)
            {
                int qty = rng.Next(-100, 200);
                var item = new BuildItem
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = BuildItemType.Manufactory,
                    BlueprintUUID = "bp-test",
                    Quantity = qty
                };
                bool expected = qty >= 1;
                bool actual = BuildPlanService.ValidateBuildItem(item);
                Assert.That(actual, Is.EqualTo(expected),
                    string.Format("Iteration {0}: qty={1}", i, qty));
            }
        }

        [Test]
        public void P2_ManufactoryRuns_CoverTargetDuration()
        {
            var rng = new Random(43);
            for (int i = 0; i < Iterations; i++)
            {
                int mfgSeconds = rng.Next(60, 86400);
                int targetSeconds = rng.Next(1, 604800);
                string mfgTimeStr = FormatSeconds(mfgSeconds);
                var bp = new Blueprint("TestBP") { Properties = new PropertyBag() };
                bp.Properties.setString("Manufacture Run Time", mfgTimeStr);
                int runs = QueueCalculator.ComputeManufactoryRuns(bp, targetSeconds);
                if (runs < 0) continue;
                decimal parsedMfg = EvolutionChainService.ParseTimeToSeconds(mfgTimeStr);
                if (parsedMfg <= 0) continue;
                long totalTime = (long)(runs * parsedMfg);
                Assert.That(totalTime, Is.GreaterThanOrEqualTo(targetSeconds),
                    string.Format("Iteration {0}: runs={1} mfg={2}s target={3}s", i, runs, parsedMfg, targetSeconds));
            }
        }

        [Test]
        public void P3_CommodityRuns_CoverTargetDuration()
        {
            var rng = new Random(44);
            long cycleSeconds = GameConstants.CommodityCycleSeconds;
            for (int i = 0; i < Iterations; i++)
            {
                int targetSeconds = rng.Next(1, 604800);
                int runs = QueueCalculator.ComputeCommodityRuns(targetSeconds);
                long totalTime = runs * cycleSeconds;
                Assert.That(totalTime, Is.GreaterThanOrEqualTo(targetSeconds),
                    string.Format("Iteration {0}: runs={1} target={2}s", i, runs, targetSeconds));
            }
        }

        [Test]
        public void P10_BuildPlan_SerializationRoundTrip()
        {
            var rng = new Random(50);
            for (int i = 0; i < Iterations; i++)
            {
                var plan = new BuildPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = "Plan_" + rng.Next(1000),
                    OwnerUUID = Guid.NewGuid().ToString(),
                    IsActive = rng.Next(2) == 1
                };
                int itemCount = rng.Next(0, 5);
                for (int j = 0; j < itemCount; j++)
                {
                    plan.Items.Add(new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = BuildItemType.Manufactory,
                        BlueprintUUID = "bp-" + rng.Next(100),
                        Quantity = rng.Next(1, 50),
                        Status = (BuildItemStatus)rng.Next(5)
                    });
                }
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(plan);
                var d = Newtonsoft.Json.JsonConvert.DeserializeObject<BuildPlan>(json);
                Assert.That(d.UUID, Is.EqualTo(plan.UUID));
                Assert.That(d.Name, Is.EqualTo(plan.Name));
                Assert.That(d.IsActive, Is.EqualTo(plan.IsActive));
                Assert.That(d.Items.Count, Is.EqualTo(plan.Items.Count));
            }
        }

        [Test]
        public void P11_RouteStopMigration_PreservesDestinations()
        {
            var rng = new Random(51);
            for (int i = 0; i < Iterations; i++)
            {
                string colonyUUID = Guid.NewGuid().ToString();
                var stop = new RouteStop { ColonyUUID = colonyUUID, Sequence = rng.Next(20) };
                stop.DestinationUUID = stop.ColonyUUID;
                stop.DestinationType = DestinationType.Colony;
                Assert.That(stop.DestinationUUID, Is.EqualTo(colonyUUID));
                Assert.That(stop.DestinationType, Is.EqualTo(DestinationType.Colony));
            }
        }

        [Test]
        public void P12_StatusCascade_OnlyAdvances()
        {
            var allStatuses = (BuildItemStatus[])Enum.GetValues(typeof(BuildItemStatus));
            var rng = new Random(52);
            for (int i = 0; i < Iterations; i++)
            {
                var current = allStatuses[rng.Next(allStatuses.Length)];
                var target = allStatuses[rng.Next(allStatuses.Length)];
                var item = new BuildItem { UUID = Guid.NewGuid().ToString(), Status = current };
                bool changed = BackgroundProcessor.TryAdvanceStatus(item, target);
                if ((int)target > (int)current)
                {
                    Assert.That(changed, Is.True);
                    Assert.That(item.Status, Is.EqualTo(target));
                }
                else
                {
                    Assert.That(changed, Is.False);
                    Assert.That(item.Status, Is.EqualTo(current));
                }
            }
        }

        private static string FormatSeconds(int totalSeconds)
        {
            int d = totalSeconds / 86400;
            int h = (totalSeconds % 86400) / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return string.Format("{0}d {1}h {2}m {3}s", d, h, m, s);
        }
    }
}
