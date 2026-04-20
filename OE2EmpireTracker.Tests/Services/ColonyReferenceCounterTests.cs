using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ColonyReferenceCounterTests
    {
        private const string TargetUUID = "colony-target-uuid";

        [Test]
        public void CountReferences_EmptyData_ReturnsZeroCounts()
        {
            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
            Assert.That(report.RouteCount, Is.EqualTo(0));
            Assert.That(report.PlanCount, Is.EqualTo(0));
            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsEmpty()
        {
            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>());

            var report = counter.CountReferences(null);

            Assert.That(report, Is.SameAs(ColonyReferenceReport.Empty));
        }

        [Test]
        public void CountReferences_BuildItemColonyMatch_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        BuildLocationType = DestinationType.Colony,
                        BuildLocationUUID = TargetUUID
                    }
                }
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemStationType_NotCounted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        BuildLocationType = DestinationType.Station,
                        BuildLocationUUID = TargetUUID
                    }
                }
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_MultipleBuildItemMatches_CountsAll()
        {
            var plan1 = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", BuildLocationType = DestinationType.Colony, BuildLocationUUID = TargetUUID },
                    new BuildItem { UUID = "bi-2", BuildLocationType = DestinationType.Colony, BuildLocationUUID = TargetUUID }
                }
            };
            var plan2 = new BuildPlan
            {
                UUID = "plan-2",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-3", BuildLocationType = DestinationType.Colony, BuildLocationUUID = TargetUUID }
                }
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                new[] { plan1, plan2 });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(3));
            Assert.That(report.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public void CountReferences_NullBuildPlans_HandledGracefully()
        {
            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                null);

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_RouteAndBuildItem_SumsAll()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { ColonyUUID = TargetUUID }
                }
            };
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", BuildLocationType = DestinationType.Colony, BuildLocationUUID = TargetUUID }
                }
            };

            var counter = new ColonyReferenceCounter(
                new[] { route },
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.RouteCount, Is.EqualTo(1));
            Assert.That(report.BuildItemCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(2));
        }

        // Completeness: SupplyChainStage.LocationUUID (Colony)
        [Test]
        public void CountReferences_SupplyChainStageLocationColony_Counted()
        {
            var chain = new SupplyChain
            {
                UUID = "sc-1",
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage { LocationType = DestinationType.Colony, LocationUUID = TargetUUID }
                }
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                supplyChains: new[] { chain });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.SupplyChainCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }

        // Completeness: WarehouseOverflowRule.DestinationUUID (Colony)
        [Test]
        public void CountReferences_WarehouseOverflowRuleDestinationColony_Counted()
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = "wor-1",
                DestinationType = DestinationType.Colony,
                DestinationUUID = TargetUUID
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                overflowRules: new[] { rule });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }

        // Completeness: WarehouseOverflowRule.ColonyUUID
        [Test]
        public void CountReferences_WarehouseOverflowRuleColonyUUID_Counted()
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = "wor-1",
                ColonyUUID = TargetUUID
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                overflowRules: new[] { rule });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }

        // Completeness: StockPlan.Targets[].LocationUUID (Colony)
        [Test]
        public void CountReferences_StockPlanTargetLocationColony_Counted()
        {
            var stockPlan = new StockPlan
            {
                UUID = "sp-1",
                Targets = new List<StockTarget>
                {
                    new StockTarget { Scope = StockTargetScope.Colony, LocationUUID = TargetUUID }
                }
            };

            var counter = new ColonyReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                stockPlans: new[] { stockPlan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }
    }
}
