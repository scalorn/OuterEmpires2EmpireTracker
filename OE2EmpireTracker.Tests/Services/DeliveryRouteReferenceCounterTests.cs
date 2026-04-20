using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class DeliveryRouteReferenceCounterTests
    {
        private const string TargetUUID = "route-target-uuid";

        #region Helpers

        private static DeliveryPlan MakePlan(string routeUUID)
        {
            return new DeliveryPlan
            {
                UUID = System.Guid.NewGuid().ToString(),
                RouteUUID = routeUUID
            };
        }

        #endregion

        [Test]
        public void CountReferences_EmptyData_ReturnsZeroCounts()
        {
            var counter = new DeliveryRouteReferenceCounter(
                Enumerable.Empty<DeliveryPlan>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
            Assert.That(report.DeliveryPlanCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_SinglePlanMatch_PlanCountIsOne()
        {
            var plan = MakePlan(TargetUUID);

            var counter = new DeliveryRouteReferenceCounter(new[] { plan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.DeliveryPlanCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultiplePlanMatches_CorrectCount()
        {
            var plan1 = MakePlan(TargetUUID);
            var plan2 = MakePlan(TargetUUID);
            var plan3 = MakePlan("other-route-uuid");

            var counter = new DeliveryRouteReferenceCounter(
                new[] { plan1, plan2, plan3 });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.DeliveryPlanCount, Is.EqualTo(2));
            Assert.That(report.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsEmptyReport()
        {
            var counter = new DeliveryRouteReferenceCounter(
                Enumerable.Empty<DeliveryPlan>());

            var report = counter.CountReferences(null);

            Assert.That(report, Is.SameAs(DeliveryRouteReferenceReport.Empty));
        }

        [Test]
        public void CountReferences_EmptyStringUUID_ReturnsEmptyReport()
        {
            var counter = new DeliveryRouteReferenceCounter(
                Enumerable.Empty<DeliveryPlan>());

            var report = counter.CountReferences("");

            Assert.That(report, Is.SameAs(DeliveryRouteReferenceReport.Empty));
        }

        [Test]
        public void CountReferences_NullPlansCollection_HandledGracefully()
        {
            var counter = new DeliveryRouteReferenceCounter(null);

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NoMatchingPlans_ReturnsZero()
        {
            var plan = MakePlan("different-route-uuid");

            var counter = new DeliveryRouteReferenceCounter(new[] { plan });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
            Assert.That(report.DeliveryPlanCount, Is.EqualTo(0));
        }

        [Test]
        public void EmptyReport_HasZeroCounts()
        {
            var empty = DeliveryRouteReferenceReport.Empty;

            Assert.That(empty.TotalCount, Is.EqualTo(0));
            Assert.That(empty.DeliveryPlanCount, Is.EqualTo(0));
        }

        // Completeness: WarehouseOverflowRule.DeliveryRouteUUID
        [Test]
        public void CountReferences_WarehouseOverflowRuleDeliveryRouteUUID_Counted()
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = "wor-1",
                DeliveryRouteUUID = TargetUUID
            };

            var counter = new DeliveryRouteReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                new[] { rule });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.OverflowRuleCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }

        // Completeness: SupplyChainStage.DeliveryRouteUUID
        [Test]
        public void CountReferences_SupplyChainStageDeliveryRouteUUID_Counted()
        {
            var chain = new SupplyChain
            {
                UUID = "sc-1",
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage { DeliveryRouteUUID = TargetUUID }
                }
            };

            var counter = new DeliveryRouteReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                supplyChains: new[] { chain });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.SupplyChainStageCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.GreaterThan(0));
        }
    }
}
