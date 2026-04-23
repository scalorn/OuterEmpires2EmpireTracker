using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Completeness tests for StationReferenceCounter.
    /// Verifies that every source listed in spec/design/reference-counting.md
    /// is actually counted by the counter.
    /// </summary>
    [TestFixture]
    public class StationReferenceCounterCompletenessTests
    {
        private const string TargetUUID = "station-completeness-uuid";

        [Test]
        public void CountReferences_DeliveryRouteStopStation_Counted()
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

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
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

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_ShipLocationStation_Counted()
        {
            var ship = new Ship
            {
                UUID = "ship-1",
                LocationType = DestinationType.Station,
                LocationUUID = TargetUUID
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                ships: new[] { ship });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_MarketListingStationUUID_Counted()
        {
            var listing = new MarketListing { UUID = "ml-1", StationUUID = TargetUUID };
            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                marketListings: new[] { listing });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_MarketTransactionStationUUID_Counted()
        {
            var tx = new MarketTransaction { UUID = "mt-1", StationUUID = TargetUUID };
            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                marketTransactions: new[] { tx });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_BuildItemAssemblyLocationStation_Counted()
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

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_SupplyChainStageLocationStation_Counted()
        {
            var chain = new SupplyChain
            {
                UUID = "sc-1",
                Stages = new List<SupplyChainStage>
                {
                    new SupplyChainStage { LocationType = DestinationType.Station, LocationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                supplyChains: new[] { chain });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_StockPlanTargetLocationStation_Counted()
        {
            var stockPlan = new StockPlan
            {
                UUID = "sp-1",
                Targets = new List<StockTarget>
                {
                    new StockTarget { Scope = StockTargetScope.Station, LocationUUID = TargetUUID }
                }
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                stockPlans: new[] { stockPlan });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }

        [Test]
        public void CountReferences_WarehouseOverflowRuleDestinationStation_Counted()
        {
            var rule = new WarehouseOverflowRule
            {
                UUID = "wor-1",
                DestinationType = DestinationType.Station,
                DestinationUUID = TargetUUID
            };

            var counter = new StationReferenceCounter(
                Enumerable.Empty<DeliveryRoute>(),
                Enumerable.Empty<DeliveryPlan>(),
                Enumerable.Empty<BuildPlan>(),
                overflowRules: new[] { rule });

            Assert.That(counter.CountReferences(TargetUUID), Is.GreaterThan(0));
        }
    }
}