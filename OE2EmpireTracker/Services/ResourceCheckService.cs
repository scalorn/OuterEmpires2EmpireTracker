using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for computing resource shortfalls on build items and plans.
    /// Compares resource requirements against location inventory to identify
    /// what resources are missing for manufacturing.
    /// </summary>
    public static class ResourceCheckService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Computes resource shortfalls for a single build item against a location inventory.
        /// Returns a dictionary of resource name ? shortfall quantity (positive values only).
        /// An empty dictionary means all resources are available.
        /// </summary>
        /// <param name="item">The build item to check.</param>
        /// <param name="locationInventory">The inventory at the build location (colony warehouse, ship cargo, or station hold).</param>
        /// <param name="blueprintFinder">Delegate to resolve a blueprint by UUID.</param>
        /// <returns>Dictionary of resource name ? shortfall quantity. Empty if all resources available.</returns>
        public static Dictionary<string, int> ComputeShortfalls(
            BuildItem item,
            ItemBag locationInventory,
            Func<string, Blueprint> blueprintFinder)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (locationInventory == null) throw new ArgumentNullException(nameof(locationInventory));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();

            Dictionary<string, int> shortfalls;
            switch (item.ItemType)
            {
                case BuildItemType.Manufactory:
                    shortfalls = ComputeManufactoryShortfalls(
                        item, locationInventory, blueprintFinder);
                    break;

                case BuildItemType.Commodity:
                    shortfalls = ComputeCommodityShortfalls(
                        item, locationInventory);
                    break;

                case BuildItemType.Mining:
                    // Mining produces resources — no shortfall check needed
                    shortfalls = new Dictionary<string, int>();
                    break;

                case BuildItemType.Refining:
                    shortfalls = ComputeRefiningShortfalls(
                        item, locationInventory);
                    break;

                case BuildItemType.Research:
                    shortfalls = ComputeResearchShortfalls(
                        item, locationInventory, blueprintFinder);
                    break;

                default:
                    Log.Debug("ComputeShortfalls: unsupported item type {0}, returning empty",
                        item.ItemType);
                    shortfalls = new Dictionary<string, int>();
                    break;
            }

            sw.Stop();
            Log.Debug("PERF ComputeShortfalls: {0} item UUID={1}, {2} shortfalls in {3}ms",
                item.ItemType, item.UUID, shortfalls.Count, sw.ElapsedMilliseconds);
            return shortfalls;
        }

        /// <summary>
        /// Computes shortfalls for all allocated items in a build plan.
        /// Resolves each item's build location inventory based on BuildLocationType.
        /// Returns per-item shortfall maps keyed by BuildItem UUID.
        /// Only items with a non-empty BuildLocationUUID are checked.
        /// </summary>
        /// <param name="plan">The build plan to check.</param>
        /// <param name="colonyFinder">Delegate to resolve a colony by UUID.</param>
        /// <param name="shipFinder">Delegate to resolve a ship by UUID (future use).</param>
        /// <param name="stationFinder">Delegate to resolve a station by UUID (future use).</param>
        /// <param name="currentPlayerUUID">Current player UUID for station hold lookup.</param>
        /// <param name="blueprintFinder">Delegate to resolve a blueprint by UUID.</param>
        /// <returns>Dictionary of BuildItem UUID ? resource shortfall map.</returns>
        public static Dictionary<string, Dictionary<string, int>> ComputePlanShortfalls(
            BuildPlan plan,
            Func<string, Colony> colonyFinder,
            Func<string, Ship> shipFinder,
            Func<string, Station> stationFinder,
            string currentPlayerUUID,
            Func<string, Blueprint> blueprintFinder)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var result = new Dictionary<string, Dictionary<string, int>>();

            foreach (var item in plan.Items)
            {
                if (string.IsNullOrEmpty(item.BuildLocationUUID))
                    continue;

                ItemBag inventory = ResolveLocationInventory(
                    item, colonyFinder, shipFinder, stationFinder,
                    currentPlayerUUID);

                if (inventory == null)
                {
                    Log.Warn("ComputePlanShortfalls: could not resolve inventory for item {0} at {1}:{2}",
                        item.UUID, item.BuildLocationType, item.BuildLocationUUID);
                    continue;
                }

                var shortfalls = ComputeShortfalls(item, inventory, blueprintFinder);
                if (shortfalls.Count > 0)
                {
                    result[item.UUID] = shortfalls;
                }
            }

            sw.Stop();
            Log.Info("PERF ComputePlanShortfalls: plan '{0}' ({1}), {2}/{3} items have shortfalls in {4}ms",
                plan.Name, plan.UUID, result.Count, plan.Items.Count, sw.ElapsedMilliseconds);
            return result;
        }

        /// <summary>
        /// Resolves the inventory (ItemBag) at a build item's location.
        /// Currently only Colony is supported. Ship and Station throw NotSupportedException.
        /// </summary>
        private static ItemBag ResolveLocationInventory(
            BuildItem item,
            Func<string, Colony> colonyFinder,
            Func<string, Ship> shipFinder,
            Func<string, Station> stationFinder,
            string currentPlayerUUID)
        {
            switch (item.BuildLocationType)
            {
                case DestinationType.Colony:
                    var colony = colonyFinder(item.BuildLocationUUID);
                    if (colony == null)
                    {
                        Log.Warn("ResolveLocationInventory: colony {0} not found",
                            item.BuildLocationUUID);
                        return null;
                    }

                    return colony.Items;

                case DestinationType.Ship:
                    throw new NotSupportedException(
                        "Ship build locations are not yet supported.");

                case DestinationType.Station:
                    throw new NotSupportedException(
                        "Station build locations are not yet supported.");

                default:
                    Log.Warn("ResolveLocationInventory: unknown location type {0}",
                        item.BuildLocationType);
                    return null;
            }
        }

        /// <summary>
        /// Computes shortfalls for a Manufactory build item by checking
        /// blueprint.Resources against the location inventory.
        /// </summary>
        private static Dictionary<string, int> ComputeManufactoryShortfalls(
            BuildItem item,
            ItemBag locationInventory,
            Func<string, Blueprint> blueprintFinder)
        {
            var shortfalls = new Dictionary<string, int>();

            Blueprint bp = blueprintFinder(item.BlueprintUUID);
            if (bp == null)
            {
                Log.Warn("ComputeManufactoryShortfalls: blueprint {0} not found for item {1}",
                    item.BlueprintUUID, item.UUID);
                return shortfalls;
            }

            if (bp.Resources == null || bp.Resources.Count == 0)
                return shortfalls;

            foreach (var entry in bp.Resources)
            {
                string resourceName = entry.Key;
                int perRun;
                if (!int.TryParse(entry.Value, out perRun) || perRun <= 0)
                    continue;

                int totalNeeded = perRun * item.Quantity;
                string purity = PriceCalculator.DeterminePurity(resourceName);
                int available = CountInventoryResource(
                    locationInventory, resourceName, purity);

                int shortfall = totalNeeded - available;
                if (shortfall > 0)
                {
                    shortfalls[resourceName] = shortfall;
                }
            }

            return shortfalls;
        }

        /// <summary>
        /// Computes shortfalls for a Commodity build item by checking
        /// commodity.ConstructionResources against the location inventory.
        /// Each run consumes the listed resources once.
        /// </summary>
        private static Dictionary<string, int> ComputeCommodityShortfalls(
            BuildItem item,
            ItemBag locationInventory)
        {
            var shortfalls = new Dictionary<string, int>();

            Commodity commodity;
            if (!Commodity.ResourceMapByString.TryGetValue(
                    item.CommodityName, out commodity))
            {
                Log.Warn("ComputeCommodityShortfalls: commodity '{0}' not found for item {1}",
                    item.CommodityName, item.UUID);
                return shortfalls;
            }

            if (commodity.ConstructionResources == null ||
                commodity.ConstructionResources.Count == 0)
                return shortfalls;

            foreach (var entry in commodity.ConstructionResources)
            {
                string resourceName = entry.Key;
                int perCycle;
                if (!int.TryParse(entry.Value, out perCycle) || perCycle <= 0)
                    continue;

                int totalNeeded = perCycle * item.Quantity;
                string purity = PriceCalculator.DeterminePurity(resourceName);
                int available = CountInventoryResource(
                    locationInventory, resourceName, purity);

                int shortfall = totalNeeded - available;
                if (shortfall > 0)
                {
                    shortfalls[resourceName] = shortfall;
                }
            }

            return shortfalls;
        }

        /// <summary>
        /// Computes shortfalls for a Refining build item.
        /// Normal refining consumes unrefined resources at the specified purity.
        /// Synthetic refining consumes refined resources per the recipe.
        /// </summary>
        private static Dictionary<string, int> ComputeRefiningShortfalls(
            BuildItem item,
            ItemBag locationInventory)
        {
            var shortfalls = new Dictionary<string, int>();

            if (string.IsNullOrEmpty(item.RefiningResource))
            {
                Log.Warn("ComputeRefiningShortfalls: item {0} has no RefiningResource", item.UUID);
                return shortfalls;
            }

            // Check for synthetic recipe (S1/S2 refining)
            var recipe = RefiningRecipes.FindByOutput(item.RefiningResource);
            if (recipe != null)
            {
                // Synthetic: consumes recipe.InputResource at recipe.InputPurity
                int totalNeeded = recipe.ConsumeRate * item.Quantity;
                string purity = recipe.InputPurity ?? GameConstants.PurityRefined;
                int available = CountInventoryResource(
                    locationInventory, recipe.InputResource, purity);

                int shortfall = totalNeeded - available;
                if (shortfall > 0)
                    shortfalls[recipe.InputResource] = shortfall;
            }
            else
            {
                // Normal refining: consumes unrefined resource at specified purity
                string purity = !string.IsNullOrEmpty(item.RefiningPurity)
                    ? item.RefiningPurity : GameConstants.PurityHigh;
                int totalNeeded = item.Quantity;
                int available = CountInventoryResource(
                    locationInventory, item.RefiningResource, purity);

                int shortfall = totalNeeded - available;
                if (shortfall > 0)
                    shortfalls[item.RefiningResource] = shortfall;
            }

            return shortfalls;
        }

        /// <summary>
        /// Computes shortfalls for a Research build item.
        /// Research items consume blueprint resources (same as Manufactory).
        /// </summary>
        private static Dictionary<string, int> ComputeResearchShortfalls(
            BuildItem item,
            ItemBag locationInventory,
            Func<string, Blueprint> blueprintFinder)
        {
            // Research uses the same resource consumption as Manufactory
            return ComputeManufactoryShortfalls(item, locationInventory, blueprintFinder);
        }

        /// <summary>
        /// Counts the total quantity of a resource at a given purity in an inventory.
        /// </summary>
        private static int CountInventoryResource(
            ItemBag inventory, string resourceName, string purity)
        {
            var items = inventory.FindResource(resourceName, purity);
            int total = 0;
            foreach (var item in items)
            {
                total += item.Quantity;
            }

            return total;
        }
    }
}
