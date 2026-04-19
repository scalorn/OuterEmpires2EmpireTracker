using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class AsteroidReferenceCounterTests
    {
        private const string TargetUUID = "asteroid-target-uuid";

        [Test]
        public void CountReferences_EmptyData_ReturnsZeroCounts()
        {
            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                Enumerable.Empty<BuildPlan>(),
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
            Assert.That(report.SurveyCount, Is.EqualTo(0));
            Assert.That(report.BuildItemCount, Is.EqualTo(0));
            Assert.That(report.RouteStopCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_NullUUID_ReturnsEmpty()
        {
            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                Enumerable.Empty<BuildPlan>(),
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(null);

            Assert.That(report, Is.SameAs(AsteroidReferenceReport.Empty));
        }

        [Test]
        public void CountReferences_SurveyMatch_Counted()
        {
            var survey = new Survey { AsteroidUUID = TargetUUID };

            var counter = new AsteroidReferenceCounter(
                new[] { survey },
                Enumerable.Empty<BuildPlan>(),
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.SurveyCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemAsteroidMatch_Counted()
        {
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        BuildLocationType = DestinationType.Asteroid,
                        BuildLocationUUID = TargetUUID
                    }
                }
            };

            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                new[] { buildPlan },
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_BuildItemColonyType_NotCounted()
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

            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                new[] { buildPlan },
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.BuildItemCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_RouteStopAsteroidMatch_Counted()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        DestinationType = DestinationType.Asteroid,
                        DestinationUUID = TargetUUID
                    }
                }
            };

            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                Enumerable.Empty<BuildPlan>(),
                new[] { route });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.RouteStopCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CountReferences_RouteStopStationType_NotCounted()
        {
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        DestinationType = DestinationType.Station,
                        DestinationUUID = TargetUUID
                    }
                }
            };

            var counter = new AsteroidReferenceCounter(
                Enumerable.Empty<Survey>(),
                Enumerable.Empty<BuildPlan>(),
                new[] { route });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.RouteStopCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_AllSourceTypes_SumsAll()
        {
            var survey = new Survey { AsteroidUUID = TargetUUID };
            var buildPlan = new BuildPlan
            {
                UUID = "plan-1",
                Items = new List<BuildItem>
                {
                    new BuildItem
                    {
                        UUID = "bi-1",
                        BuildLocationType = DestinationType.Asteroid,
                        BuildLocationUUID = TargetUUID
                    }
                }
            };
            var route = new DeliveryRoute
            {
                UUID = "route-1",
                Stops = new List<RouteStop>
                {
                    new RouteStop
                    {
                        DestinationType = DestinationType.Asteroid,
                        DestinationUUID = TargetUUID
                    }
                }
            };

            var counter = new AsteroidReferenceCounter(
                new[] { survey },
                new[] { buildPlan },
                new[] { route });

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.SurveyCount, Is.EqualTo(1));
            Assert.That(report.BuildItemCount, Is.EqualTo(1));
            Assert.That(report.RouteStopCount, Is.EqualTo(1));
            Assert.That(report.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public void CountReferences_NullInputs_HandledGracefully()
        {
            var counter = new AsteroidReferenceCounter(null, null, null);

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void CountReferences_MultipleSurveys_CountsAll()
        {
            var surveys = new[]
            {
                new Survey { AsteroidUUID = TargetUUID },
                new Survey { AsteroidUUID = TargetUUID },
                new Survey { AsteroidUUID = "other-uuid" }
            };

            var counter = new AsteroidReferenceCounter(
                surveys,
                Enumerable.Empty<BuildPlan>(),
                Enumerable.Empty<DeliveryRoute>());

            var report = counter.CountReferences(TargetUUID);

            Assert.That(report.SurveyCount, Is.EqualTo(2));
            Assert.That(report.TotalCount, Is.EqualTo(2));
        }
    }
}