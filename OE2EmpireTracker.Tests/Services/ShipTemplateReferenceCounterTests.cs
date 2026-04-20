using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class ShipTemplateReferenceCounterTests
    {
        private const string TargetUUID = "template-target-uuid";

        [Test]
        public void CountReferences_EmptyData_ReturnsZero()
        {
            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_EmptyUUID_ReturnsZero()
        {
            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(""), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_ShipMatch_Counted()
        {
            var ships = new[] { new Ship { UUID = "s1", TemplateUUID = TargetUUID } };

            var counter = new ShipTemplateReferenceCounter(ships, Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemMatch_Counted()
        {
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", ShipTemplateUUID = TargetUUID }
                }
            };

            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(), new[] { plan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultipleShipsAndBuildItems_SumsAll()
        {
            var ships = new[]
            {
                new Ship { UUID = "s1", TemplateUUID = TargetUUID },
                new Ship { UUID = "s2", TemplateUUID = TargetUUID }
            };
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", ShipTemplateUUID = TargetUUID },
                    new BuildItem { UUID = "bi-2", ShipTemplateUUID = TargetUUID }
                }
            };

            var counter = new ShipTemplateReferenceCounter(ships, new[] { plan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(4));
        }

        [Test]
        public void CountReferences_NoMatch_ReturnsZero()
        {
            var ships = new[] { new Ship { UUID = "s1", TemplateUUID = "other-uuid" } };
            var plan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", ShipTemplateUUID = "other-uuid" }
                }
            };

            var counter = new ShipTemplateReferenceCounter(ships, new[] { plan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullInputs_HandledGracefully()
        {
            var counter = new ShipTemplateReferenceCounter(null, null);

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullBuildPlanItems_HandledGracefully()
        {
            var plan = new BuildPlan { UUID = "plan-1", Items = null };

            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(), new[] { plan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        // Completeness: StockPlan.Targets[].ShipTemplateUUID
        [Test]
        public void CountReferences_StockPlanTargetShipTemplateUUID_Counted()
        {
            var stockPlan = new StockPlan
            {
                UUID = "sp-1",
                Targets = new List<StockTarget>
                {
                    new StockTarget { ShipTemplateUUID = TargetUUID }
                }
            };

            var counter = new ShipTemplateReferenceCounter(
                Enumerable.Empty<Ship>(),
                Enumerable.Empty<BuildPlan>(),
                new[] { stockPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }
    }
}