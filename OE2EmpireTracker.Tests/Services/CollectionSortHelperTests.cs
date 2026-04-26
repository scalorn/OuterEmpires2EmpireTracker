using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for CollectionSortHelper edge cases.
    /// Tests null input, empty input, single-element, already-sorted,
    /// reverse-sorted, null sort keys, and smoke tests for all methods.
    /// </summary>
    [TestFixture]
    public class CollectionSortHelperTests
    {
        // -----------------------------------------------------------------------
        // Null input returns empty list
        // -----------------------------------------------------------------------

        [Test]
        public void OrderStructures_NullInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderStructures(null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void OrderRouteStops_NullInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderRouteStops(null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void OrderColonies_NullInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderColonies(null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void OrderComponents_NullInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderComponents(null);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Empty input returns empty list
        // -----------------------------------------------------------------------

        [Test]
        public void OrderStructures_EmptyInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderStructures(new List<ColonyStructure>());
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void OrderRouteStops_EmptyInput_ReturnsEmpty()
        {
            var result = CollectionSortHelper.OrderRouteStops(new List<RouteStop>());
            Assert.That(result.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Single-element input returns single-element list
        // -----------------------------------------------------------------------

        [Test]
        public void OrderStructures_SingleElement_ReturnsSingleElement()
        {
            var input = new List<ColonyStructure>
            {
                new ColonyStructure { BuildQueueSequence = 42 }
            };
            var result = CollectionSortHelper.OrderStructures(input);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].BuildQueueSequence, Is.EqualTo(42));
        }

        [Test]
        public void OrderRouteStops_SingleElement_ReturnsSingleElement()
        {
            var input = new List<RouteStop>
            {
                new RouteStop { Sequence = 7 }
            };
            var result = CollectionSortHelper.OrderRouteStops(input);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Sequence, Is.EqualTo(7));
        }

        // -----------------------------------------------------------------------
        // Already-sorted input returns same order
        // -----------------------------------------------------------------------

        [Test]
        public void OrderStructures_AlreadySorted_ReturnsSameOrder()
        {
            var input = new List<ColonyStructure>
            {
                new ColonyStructure { BuildQueueSequence = 1 },
                new ColonyStructure { BuildQueueSequence = 2 },
                new ColonyStructure { BuildQueueSequence = 3 }
            };
            var result = CollectionSortHelper.OrderStructures(input);
            Assert.That(result[0].BuildQueueSequence, Is.EqualTo(1));
            Assert.That(result[1].BuildQueueSequence, Is.EqualTo(2));
            Assert.That(result[2].BuildQueueSequence, Is.EqualTo(3));
        }

        // -----------------------------------------------------------------------
        // Reverse-sorted input returns correct order
        // -----------------------------------------------------------------------

        [Test]
        public void OrderStructures_ReverseSorted_ReturnsSortedOrder()
        {
            var input = new List<ColonyStructure>
            {
                new ColonyStructure { BuildQueueSequence = 3 },
                new ColonyStructure { BuildQueueSequence = 2 },
                new ColonyStructure { BuildQueueSequence = 1 }
            };
            var result = CollectionSortHelper.OrderStructures(input);
            Assert.That(result[0].BuildQueueSequence, Is.EqualTo(1));
            Assert.That(result[1].BuildQueueSequence, Is.EqualTo(2));
            Assert.That(result[2].BuildQueueSequence, Is.EqualTo(3));
        }

        [Test]
        public void OrderRouteStops_ReverseSorted_ReturnsSortedOrder()
        {
            var input = new List<RouteStop>
            {
                new RouteStop { Sequence = 3 },
                new RouteStop { Sequence = 2 },
                new RouteStop { Sequence = 1 }
            };
            var result = CollectionSortHelper.OrderRouteStops(input);
            Assert.That(result[0].Sequence, Is.EqualTo(1));
            Assert.That(result[1].Sequence, Is.EqualTo(2));
            Assert.That(result[2].Sequence, Is.EqualTo(3));
        }

        // -----------------------------------------------------------------------
        // Null sort key values sort to beginning (empty string)
        // -----------------------------------------------------------------------

        [Test]
        public void OrderColonies_NullSortKeys_SortToBeginning()
        {
            var input = new List<Colony>
            {
                new Colony { SystemName = "Zeta", PlanetName = "P1", ColonyName = "C1" },
                new Colony { SystemName = null, PlanetName = null, ColonyName = null },
                new Colony { SystemName = "Alpha", PlanetName = "P2", ColonyName = "C2" }
            };
            var result = CollectionSortHelper.OrderColonies(input);
            // Null coalesces to empty string, which sorts before "Alpha"
            Assert.That(result[0].SystemName, Is.Null);
            Assert.That(result[1].SystemName, Is.EqualTo("Alpha"));
            Assert.That(result[2].SystemName, Is.EqualTo("Zeta"));
        }

        [Test]
        public void OrderComponents_NullSlotType_SortsToBeginning()
        {
            var input = new List<ShipComponentSlot>
            {
                new ShipComponentSlot { SlotType = "Weapon", SlotIndex = 1 },
                new ShipComponentSlot { SlotType = null, SlotIndex = 0 },
                new ShipComponentSlot { SlotType = "Drive", SlotIndex = 0 }
            };
            var result = CollectionSortHelper.OrderComponents(input);
            // Null coalesces to empty string, which sorts before "Drive"
            Assert.That(result[0].SlotType, Is.Null);
            Assert.That(result[1].SlotType, Is.EqualTo("Drive"));
            Assert.That(result[2].SlotType, Is.EqualTo("Weapon"));
        }

        // -----------------------------------------------------------------------
        // Smoke tests: each sort helper method exists and compiles
        // -----------------------------------------------------------------------

        [Test]
        public void SmokeTest_AllSortHelperMethods_AcceptEmptyList()
        {
            // Primary sort methods
            Assert.That(CollectionSortHelper.OrderStructures(new List<ColonyStructure>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderStructuresDescending(new List<ColonyStructure>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderRouteStops(new List<RouteStop>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderPlanStops(new List<DeliveryPlanStop>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderSupplyChainStages(new List<SupplyChainStage>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderColonies(new List<Colony>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderBlueprints(new List<OE2EmpireTracker.Models.Blueprint>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderPlayerProfiles(new List<PlayerProfile>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderSurveys(new List<Survey>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderCommodityRequests(new List<CommodityRequested>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderCommodityRequestsByNeedBy(new List<CommodityRequested>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderDeliveryItems(new List<DeliveryItem>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderBuildItems(new List<BuildItem>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderAsteroidReserves(new List<AsteroidReserve>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderComponents(new List<ShipComponentSlot>()).Count, Is.EqualTo(0));

            // Top-level entity sort methods
            Assert.That(CollectionSortHelper.OrderDeliveryRoutes(new List<DeliveryRoute>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderDeliveryPlans(new List<DeliveryPlan>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderPricingPlans(new List<PricingPlan>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderBuildPlans(new List<BuildPlan>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderShipTemplates(new List<ShipTemplate>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderShips(new List<Ship>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderStations(new List<Station>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderMarketListings(new List<MarketListing>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderMarketTransactions(new List<MarketTransaction>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderStockPlans(new List<StockPlan>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderStockProfiles(new List<StockProfile>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderSupplyChains(new List<SupplyChain>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderWarehouseOverflowRules(new List<WarehouseOverflowRule>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderFactions(new List<Faction>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderExternalCharacters(new List<ExternalCharacter>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderAsteroids(new List<Asteroid>()).Count, Is.EqualTo(0));

            // Alternate sort methods
            Assert.That(CollectionSortHelper.OrderMarketTransactionsByTimestamp(new List<MarketTransaction>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderBlueprintsByEvolutionDescending(new List<OE2EmpireTracker.Models.Blueprint>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderBlueprintsByEvolution(new List<OE2EmpireTracker.Models.Blueprint>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderActivityRowsByTimeRemaining(new List<ActivityRow>()).Count, Is.EqualTo(0));
            Assert.That(CollectionSortHelper.OrderCountdownsByTimeRemaining(new List<CountDownTimeReference>()).Count, Is.EqualTo(0));
        }
    }
}