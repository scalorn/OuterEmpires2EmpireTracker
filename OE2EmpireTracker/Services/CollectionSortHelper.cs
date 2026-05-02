using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides canonical domain-order sort methods for data model collections.
    /// Every ordered collection in the Sort Key Registry has a corresponding method here.
    /// Consumers MUST use these methods instead of inline .OrderBy() expressions.
    ///
    /// NOTE: This class sorts by domain-meaningful keys (Name, Sequence, BuildQueueSequence).
    /// SerializationSorter sorts by UUID for deterministic JSON diffs. They serve different purposes.
    /// </summary>
    public static class CollectionSortHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // ----------------------------------------------------------------
        //  Generic helpers (for game-constant types not in the Sort Key Registry)
        // ----------------------------------------------------------------

        /// <summary>
        /// Generic sort by a string key selector (ascending, OrdinalIgnoreCase).
        /// Used for game-constant types (BlueprintType, TechLevel, Resource, etc.)
        /// that have a Name property but are not data model collections.
        /// </summary>
        public static IReadOnlyList<T> OrderByName<T>(
            IEnumerable<T> items,
            Func<T, string> nameSelector)
        {
            if (items == null) return Array.Empty<T>();
            return items
                .OrderBy(i => nameSelector(i) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Colony Structures
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts colony structures by BuildQueueSequence (ascending).
        /// </summary>
        public static IReadOnlyList<ColonyStructure> OrderStructures(
            IEnumerable<ColonyStructure> structures)
        {
            if (structures == null) return Array.Empty<ColonyStructure>();
            return structures
                .OrderBy(s => s.BuildQueueSequence)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts structures by BuildQueueSequence descending (highest first).
        /// Used by ColonyInactivityCollector for underutilized refiner priority.
        /// </summary>
        public static IReadOnlyList<ColonyStructure> OrderStructuresDescending(
            IEnumerable<ColonyStructure> structures)
        {
            if (structures == null) return Array.Empty<ColonyStructure>();
            return structures
                .OrderByDescending(s => s.BuildQueueSequence)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Route / Plan Stops
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts route stops by Sequence (ascending).
        /// </summary>
        public static IReadOnlyList<RouteStop> OrderRouteStops(
            IEnumerable<RouteStop> stops)
        {
            if (stops == null) return Array.Empty<RouteStop>();
            return stops
                .OrderBy(s => s.Sequence)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts delivery plan stops by Sequence (ascending).
        /// </summary>
        public static IReadOnlyList<DeliveryPlanStop> OrderPlanStops(
            IEnumerable<DeliveryPlanStop> stops)
        {
            if (stops == null) return Array.Empty<DeliveryPlanStop>();
            return stops
                .OrderBy(s => s.Sequence)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Supply Chain
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts supply chain stages by Sequence (ascending).
        /// </summary>
        public static IReadOnlyList<SupplyChainStage> OrderSupplyChainStages(
            IEnumerable<SupplyChainStage> stages)
        {
            if (stages == null) return Array.Empty<SupplyChainStage>();
            return stages
                .OrderBy(s => s.Sequence)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Top-Level Entities (composite key sorts)
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts colonies by SystemName, then PlanetName, then ColonyName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Colony> OrderColonies(
            IEnumerable<Colony> colonies)
        {
            if (colonies == null) return Array.Empty<Colony>();
            return colonies
                .OrderBy(c => c.SystemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.PlanetName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.ColonyName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts surveys by PlanetName, then SurveyID (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Survey> OrderSurveys(
            IEnumerable<Survey> surveys)
        {
            if (surveys == null) return Array.Empty<Survey>();
            return surveys
                .OrderBy(s => s.PlanetName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.SurveyID ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts surveys by ExtendedName (ascending, OrdinalIgnoreCase).
        /// Used by ColonyStructureV2 for survey combo population.
        /// </summary>
        public static IReadOnlyList<Survey> OrderSurveysByExtendedName(
            IEnumerable<Survey> surveys)
        {
            if (surveys == null) return Array.Empty<Survey>();
            return surveys
                .OrderBy(s => s.ExtendedName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts survey resources by Resource name (ascending, OrdinalIgnoreCase).
        /// Used by ColonyStructureV2 for resource combo population.
        /// </summary>
        public static IReadOnlyList<SurveyResource> OrderSurveyResources(
            IEnumerable<SurveyResource> resources)
        {
            if (resources == null) return new List<SurveyResource>();
            return resources
                .OrderBy(r => r.Resource, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts ship/station/template components by SlotType, then SlotIndex (ascending).
        /// </summary>
        public static IReadOnlyList<ShipComponentSlot> OrderComponents(
            IEnumerable<ShipComponentSlot> components)
        {
            if (components == null) return Array.Empty<ShipComponentSlot>();
            return components
                .OrderBy(c => c.SlotType ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.SlotIndex)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Top-Level Entities (sorted by Name, string, ascending)
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts blueprints by ExtendedName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Blueprint> OrderBlueprints(
            IEnumerable<Blueprint> blueprints)
        {
            if (blueprints == null) return Array.Empty<Blueprint>();
            return blueprints
                .OrderBy(b => b.ExtendedName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts player profiles by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<PlayerProfile> OrderPlayerProfiles(
            IEnumerable<PlayerProfile> profiles)
        {
            if (profiles == null) return Array.Empty<PlayerProfile>();
            return profiles
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts delivery routes by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<DeliveryRoute> OrderDeliveryRoutes(
            IEnumerable<DeliveryRoute> routes)
        {
            if (routes == null) return Array.Empty<DeliveryRoute>();
            return routes
                .OrderBy(r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts delivery plans by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<DeliveryPlan> OrderDeliveryPlans(
            IEnumerable<DeliveryPlan> plans)
        {
            if (plans == null) return Array.Empty<DeliveryPlan>();
            return plans
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts pricing plans by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<PricingPlan> OrderPricingPlans(
            IEnumerable<PricingPlan> plans)
        {
            if (plans == null) return Array.Empty<PricingPlan>();
            return plans
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts build plans by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<BuildPlan> OrderBuildPlans(
            IEnumerable<BuildPlan> plans)
        {
            if (plans == null) return Array.Empty<BuildPlan>();
            return plans
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts ship templates by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<ShipTemplate> OrderShipTemplates(
            IEnumerable<ShipTemplate> templates)
        {
            if (templates == null) return Array.Empty<ShipTemplate>();
            return templates
                .OrderBy(t => t.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts ships by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Ship> OrderShips(
            IEnumerable<Ship> ships)
        {
            if (ships == null) return Array.Empty<Ship>();
            return ships
                .OrderBy(s => s.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts stations by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Station> OrderStations(
            IEnumerable<Station> stations)
        {
            if (stations == null) return Array.Empty<Station>();
            return stations
                .OrderBy(s => s.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts market listings by ItemName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<MarketListing> OrderMarketListings(
            IEnumerable<MarketListing> listings)
        {
            if (listings == null) return Array.Empty<MarketListing>();
            return listings
                .OrderBy(l => l.ItemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts market transactions by ItemName (ascending, OrdinalIgnoreCase). Default sort.
        /// </summary>
        public static IReadOnlyList<MarketTransaction> OrderMarketTransactions(
            IEnumerable<MarketTransaction> transactions)
        {
            if (transactions == null) return Array.Empty<MarketTransaction>();
            return transactions
                .OrderBy(t => t.ItemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts stock plans by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<StockPlan> OrderStockPlans(
            IEnumerable<StockPlan> plans)
        {
            if (plans == null) return Array.Empty<StockPlan>();
            return plans
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts stock profiles by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<StockProfile> OrderStockProfiles(
            IEnumerable<StockProfile> profiles)
        {
            if (profiles == null) return Array.Empty<StockProfile>();
            return profiles
                .OrderBy(p => p.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts supply chains by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<SupplyChain> OrderSupplyChains(
            IEnumerable<SupplyChain> chains)
        {
            if (chains == null) return Array.Empty<SupplyChain>();
            return chains
                .OrderBy(c => c.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts warehouse overflow rules by ResourceName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<WarehouseOverflowRule> OrderWarehouseOverflowRules(
            IEnumerable<WarehouseOverflowRule> rules)
        {
            if (rules == null) return Array.Empty<WarehouseOverflowRule>();
            return rules
                .OrderBy(r => r.ResourceName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts factions by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Faction> OrderFactions(
            IEnumerable<Faction> factions)
        {
            if (factions == null) return Array.Empty<Faction>();
            return factions
                .OrderBy(f => f.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts external characters by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<ExternalCharacter> OrderExternalCharacters(
            IEnumerable<ExternalCharacter> characters)
        {
            if (characters == null) return Array.Empty<ExternalCharacter>();
            return characters
                .OrderBy(c => c.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts asteroids by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<Asteroid> OrderAsteroids(
            IEnumerable<Asteroid> asteroids)
        {
            if (asteroids == null) return Array.Empty<Asteroid>();
            return asteroids
                .OrderBy(a => a.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Nested Collections
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts commodity requests by Name (ascending, OrdinalIgnoreCase). Default sort.
        /// </summary>
        public static IReadOnlyList<CommodityRequested> OrderCommodityRequests(
            IEnumerable<CommodityRequested> requests)
        {
            if (requests == null) return Array.Empty<CommodityRequested>();
            return requests
                .OrderBy(r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts commodity requests by NeedBy date (ascending). Used by ColonyActivity form.
        /// </summary>
        public static IReadOnlyList<CommodityRequested> OrderCommodityRequestsByNeedBy(
            IEnumerable<CommodityRequested> requests)
        {
            if (requests == null) return Array.Empty<CommodityRequested>();
            return requests
                .OrderBy(r => r.NeedBy)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts delivery items by Name (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<DeliveryItem> OrderDeliveryItems(
            IEnumerable<DeliveryItem> items)
        {
            if (items == null) return Array.Empty<DeliveryItem>();
            return items
                .OrderBy(i => i.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts build items by ItemName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<BuildItem> OrderBuildItems(
            IEnumerable<BuildItem> items)
        {
            if (items == null) return Array.Empty<BuildItem>();
            return items
                .OrderBy(i => i.ItemName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts build items by SequenceInStructure (ascending).
        /// Used by BuildPlanExecutionService for processing items in structure order.
        /// </summary>
        public static IReadOnlyList<BuildItem> OrderBuildItemsBySequence(
            IEnumerable<BuildItem> items)
        {
            if (items == null) return new List<BuildItem>();
            return items
                .OrderBy(i => i.SequenceInStructure)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts asteroid reserves by ResourceName (ascending, OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyList<AsteroidReserve> OrderAsteroidReserves(
            IEnumerable<AsteroidReserve> reserves)
        {
            if (reserves == null) return Array.Empty<AsteroidReserve>();
            return reserves
                .OrderBy(r => r.ResourceName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  ItemBag Dictionary Sorts
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts ItemBag entries by item Name (ascending, OrdinalIgnoreCase).
        /// Returns key-value pairs sorted by the item's Name property.
        /// Used for display iteration of ItemBag dictionaries.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, Item>> OrderItemBagEntries(
            IDictionary<string, Item> items)
        {
            if (items == null) return Array.Empty<KeyValuePair<string, Item>>();
            return items
                .OrderBy(kvp => kvp.Value?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        // ----------------------------------------------------------------
        //  Alternate sort orders
        // ----------------------------------------------------------------

        /// <summary>
        /// Sorts market transactions by Timestamp descending (most recent first).
        /// Used by FormMarket transaction display.
        /// </summary>
        public static IReadOnlyList<MarketTransaction> OrderMarketTransactionsByTimestamp(
            IEnumerable<MarketTransaction> transactions)
        {
            if (transactions == null) return Array.Empty<MarketTransaction>();
            return transactions
                .OrderByDescending(t => t.Timestamp ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts blueprints by Evolution descending (highest first).
        /// Used by BlueprintViewModel.GetEvolutionChain().
        /// </summary>
        public static IReadOnlyList<Blueprint> OrderBlueprintsByEvolutionDescending(
            IEnumerable<Blueprint> blueprints)
        {
            if (blueprints == null) return Array.Empty<Blueprint>();
            return blueprints
                .OrderByDescending(b => b.Evolution)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts read-only blueprints by Evolution descending (highest first).
        /// Used by BlueprintViewModel.GetBaseBlueprintCandidates().
        /// </summary>
        public static IReadOnlyList<ReadOnlyBlueprint> OrderReadOnlyBlueprintsByEvolutionDescending(
            IEnumerable<ReadOnlyBlueprint> blueprints)
        {
            if (blueprints == null) return Array.Empty<ReadOnlyBlueprint>();
            return blueprints
                .OrderByDescending(b => b.Evolution)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts blueprints by Evolution ascending.
        /// Used by EvolutionChainService.
        /// </summary>
        public static IReadOnlyList<Blueprint> OrderBlueprintsByEvolution(
            IEnumerable<Blueprint> blueprints)
        {
            if (blueprints == null) return Array.Empty<Blueprint>();
            return blueprints
                .OrderBy(b => b.Evolution)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts activity rows by time remaining ascending (soonest first).
        /// Used by ColonyAdminReportBuilder.
        /// </summary>
        public static IReadOnlyList<ActivityRow> OrderActivityRowsByTimeRemaining(
            IEnumerable<ActivityRow> rows)
        {
            if (rows == null) return Array.Empty<ActivityRow>();
            return rows
                .OrderBy(r => r.GetSecondsRemaining())
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Sorts countdown references by time remaining ascending (soonest first).
        /// Used by PlayerContext.ActiveCountdowns.
        /// </summary>
        public static IReadOnlyList<CountDownTimeReference> OrderCountdownsByTimeRemaining(
            IEnumerable<CountDownTimeReference> countdowns)
        {
            if (countdowns == null) return Array.Empty<CountDownTimeReference>();
            return countdowns
                .OrderBy(c => c.CountDownTime.TimeRemaining)
                .ToList()
                .AsReadOnly();
        }
    }
}