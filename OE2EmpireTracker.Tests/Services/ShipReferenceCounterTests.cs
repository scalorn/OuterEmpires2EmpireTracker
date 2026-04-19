using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ShipReferenceCounterTests
    {
        private const string TargetUUID = "ship-target-uuid";

        [Test]
        public void CountReferences_EmptyData_ReturnsZero()
        {
            var counter = new ShipReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new ShipReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_DeliveryPlanShipMatch_Counted()
        {
            var plan = new DeliveryPlan { UUID = "dp-1", ShipUUID = TargetUUID };

            var counter = new ShipReferenceCounter(
                new[] { plan },
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemShipMatch_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        BuildLocationType = DestinationType.Ship,
                        BuildLocationUUID = TargetUUID
                    }
                }
            };

            var counter = new ShipReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemColonyType_NotCounted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
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

            var counter = new ShipReferenceCounter(
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_MultipleMatches_SumsAll()
        {
            var dp1 = new DeliveryPlan { UUID = "dp-1", ShipUUID = TargetUUID };
            var dp2 = new DeliveryPlan { UUID = "dp-2", ShipUUID = TargetUUID };
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", BuildLocationType = DestinationType.Ship, BuildLocationUUID = TargetUUID }
                }
            };

            var counter = new ShipReferenceCounter(
                new[] { dp1, dp2 },
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(3));
        }

        [Test]
        public void CountReferences_NullInputs_HandledGracefully()
        {
            var counter = new ShipReferenceCounter(null, null);
            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_DifferentUUID_ReturnsZero()
        {
            var plan = new DeliveryPlan { UUID = "dp-1", ShipUUID = "other-uuid" };

            var counter = new ShipReferenceCounter(
                new[] { plan },
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }
    }
}