using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class StationReferenceCounterTests
    {
        private const string TargetUUID = "station-target-uuid";

        [Test]
        public void CountReferences_EmptyData_ReturnsZero()
        {
            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsZero()
        {
            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(null), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_RouteStopStation_Counted()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { DestinationType = DestinationType.Station, DestinationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                new[] { route },
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_RouteStopColony_NotCounted()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { DestinationType = DestinationType.Colony, DestinationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                new[] { route },
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_DeliveryPlanStopStation_Counted()
        {
            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop { DestinationType = DestinationType.Station, DestinationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                new[] { plan },
                Enumerable.Empty<BuildPlan>());

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemAssemblyStation_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        AssemblyLocationType = DestinationType.Station,
                        AssemblyLocationUUID = TargetUUID
                    }
                }
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemBuildStation_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
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

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_MultipleSourceTypes_SumsAll()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop { DestinationType = DestinationType.Station, DestinationUUID = TargetUUID }
                }
            };

            var plan = new DeliveryPlan
            {
                UUID = "plan-1",
                Stops = new List<DeliveryPlanStop>
                {
                    new DeliveryPlanStop { DestinationType = DestinationType.Station, DestinationUUID = TargetUUID }
                }
            };

            var buildPlan = new BuildPlan
            {
                UUID = "bp-1",
                Items = new List<BuildItem>
                {
                    new BuildItem { UUID = "bi-1", AssemblyLocationType = DestinationType.Station, AssemblyLocationUUID = TargetUUID },
                    new BuildItem { UUID = "bi-2", BuildLocationType = DestinationType.Station, BuildLocationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                new[] { route },
                new[] { plan },
                new[] { buildPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(4));
        }

        [Test]
        public void CountReferences_NullCollections_HandledGracefully()
        {
            var counter = new StationReferenceCounter(null, null, null);
            Assert.That(counter.CountReferences(TargetUUID), Is.EqualTo(0));
        }
    }
}
