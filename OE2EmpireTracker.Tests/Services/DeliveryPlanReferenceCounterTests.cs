using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Completeness tests for DeliveryPlanReferenceCounter.
    /// Verifies that every source listed in spec/design/reference-counting.md
    /// is actually counted by the counter.
    /// Sources: BuildPlan.DeliveryPlanUUID
    /// </summary>
    [TestFixture]
    public class DeliveryPlanReferenceCounterCompletenessTests
    {
        private const string TargetUUID = "dp-completeness-uuid";

        [Test]
        public void CountReferences_BuildPlanDeliveryPlanUUID_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
                DeliveryPlanUUID = TargetUUID
            };

            var counter = new DeliveryPlanReferenceCounter(new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_EmptyData_ReturnsZero()
        {
            var counter = new DeliveryPlanReferenceCounter(
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new DeliveryPlanReferenceCounter(
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullBuildPlans_HandledGracefully()
        {
            var counter = new DeliveryPlanReferenceCounter(null);

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_MultipleBuildPlansMatch_CountsAll()
        {
            var bp1 = new BuildPlan { UUID = "bp-1", DeliveryPlanUUID = TargetUUID };
            var bp2 = new BuildPlan { UUID = "bp-2", DeliveryPlanUUID = TargetUUID };
            var bp3 = new BuildPlan { UUID = "bp-3", DeliveryPlanUUID = "other-uuid" };

            var counter = new DeliveryPlanReferenceCounter(new[] { bp1, bp2, bp3 });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(2));
        }
    }
}
