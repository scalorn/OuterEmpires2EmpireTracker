using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for generating delivery plans from build plan shortfalls.
    /// Supports single-plan resource delivery, consolidated multi-plan resource delivery,
    /// and flatpack delivery for completed build items.
    /// </summary>
    public static class DeliveryGenerationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates or updates a delivery plan for a single build plan's resource shortfalls.
        /// Groups shortfalls by build location and creates drop-off stops on the route.
        /// If the build plan already has an associated delivery plan, updates it.
        /// Names the plan "Build: {buildPlan.Name}".
        /// Updates affected build items to Delivering status.
        /// Stores the delivery plan UUID on the build plan.
        /// </summary>
        /// <param name="buildPlan">The build plan with resource shortfalls.</param>
        /// <param name="route">The delivery route to use for stop ordering.</param>
        /// <param name="shortfalls">Per-item shortfall maps keyed by BuildItem UUID.</param>
        /// <param name="colonyFinder">Delegate to resolve a colony by UUID.</param>
        /// <param name="playerContext">PlayerContext for looking up/storing delivery plans.</param>
        /// <returns>The created or updated DeliveryPlan.</returns>
        public static DeliveryPlan GenerateDeliveryPlan(
            BuildPlan buildPlan,
            DeliveryRoute route,
            Dictionary<string, Dictionary<string, int>> shortfalls,
            Func<string, Colony> colonyFinder,
            PlayerContext playerContext)
        {
            if (buildPlan == null) throw new ArgumentNullException(nameof(buildPlan));
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (shortfalls == null) throw new ArgumentNullException(nameof(shortfalls));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (playerContext == null) throw new ArgumentNullException(nameof(playerContext));

            var sw = Stopwatch.StartNew();

            // Group shortfalls by build location UUID
            var locationShortfalls = GroupShortfallsByLocation(buildPlan, shortfalls);

            // Find or create the delivery plan
            DeliveryPlan plan = FindOrCreatePlan(
                buildPlan.DeliveryPlanUUID,
                route,
                playerContext,
                string.Format("Build: {0}", buildPlan.Name));

            // Rebuild stops from shortfalls
            RebuildStopsFromShortfalls(plan, route, locationShortfalls, colonyFinder);

            // Store the delivery plan UUID on the build plan
            buildPlan.DeliveryPlanUUID = plan.UUID;

            // Update affected build items to Delivering
            UpdateItemStatusToDelivering(buildPlan, shortfalls);

            sw.Stop();
            Log.Info(
                "PERF GenerateDeliveryPlan: plan '{0}' ({1}), {2} stops in {3}ms",
                plan.Name,
                plan.UUID,
                plan.Stops.Count,
                sw.ElapsedMilliseconds);
            return plan;
        }

        /// <summary>
        /// Consolidates shortfalls across multiple build plans into a single delivery plan.
        /// Groups all resource needs by destination (manufacturing colony), merging
        /// duplicate resources across plans. Returns one consolidated delivery plan.
        /// </summary>
        /// <param name="buildPlans">The build plans to consolidate.</param>
        /// <param name="route">The delivery route to use for stop ordering.</param>
        /// <param name="shortfallProvider">Delegate that returns shortfalls for a given plan.</param>
        /// <param name="colonyFinder">Delegate to resolve a colony by UUID.</param>
        /// <param name="playerContext">PlayerContext for storing the delivery plan.</param>
        /// <param name="planName">Name for the consolidated delivery plan.</param>
        /// <returns>The created consolidated DeliveryPlan.</returns>
        public static DeliveryPlan GenerateConsolidatedDeliveryPlan(
            IEnumerable<BuildPlan> buildPlans,
            DeliveryRoute route,
            Func<BuildPlan, Dictionary<string, Dictionary<string, int>>> shortfallProvider,
            Func<string, Colony> colonyFinder,
            PlayerContext playerContext,
            string planName)
        {
            if (buildPlans == null) throw new ArgumentNullException(nameof(buildPlans));
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (shortfallProvider == null) throw new ArgumentNullException(nameof(shortfallProvider));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (playerContext == null) throw new ArgumentNullException(nameof(playerContext));

            var sw = Stopwatch.StartNew();

            // Merge shortfalls across all plans by location
            var mergedByLocation = MergeShortfallsAcrossPlans(buildPlans, shortfallProvider);

            // Create a new delivery plan
            var plan = CreateNewPlan(route, playerContext, planName ?? "Consolidated Delivery");

            // Build stops from merged shortfalls
            RebuildStopsFromShortfalls(plan, route, mergedByLocation, colonyFinder);

            sw.Stop();
            Log.Info(
                "PERF GenerateConsolidatedDeliveryPlan: '{0}' ({1}), {2} stops in {3}ms",
                plan.Name,
                plan.UUID,
                plan.Stops.Count,
                sw.ElapsedMilliseconds);
            return plan;
        }

        /// <summary>
        /// Generates a flatpack delivery plan for completed build items.
        /// Scans build items with Status=Completed across the provided plans,
        /// extracts the destination colony UUID from the item's Notes field,
        /// and creates drop-off items to deliver flatpacks to their target colonies.
        /// </summary>
        /// <param name="buildPlans">The build plans to scan for completed items.</param>
        /// <param name="route">The delivery route to use for stop ordering.</param>
        /// <param name="colonyFinder">Delegate to resolve a colony by UUID.</param>
        /// <param name="blueprintFinder">Delegate to resolve a blueprint by UUID.</param>
        /// <param name="playerContext">PlayerContext for storing the delivery plan.</param>
        /// <param name="planName">Name for the flatpack delivery plan.</param>
        /// <returns>The created flatpack DeliveryPlan.</returns>
        public static DeliveryPlan GenerateFlatpackDeliveryPlan(
            IEnumerable<BuildPlan> buildPlans,
            DeliveryRoute route,
            Func<string, Colony> colonyFinder,
            Func<string, Blueprint> blueprintFinder,
            PlayerContext playerContext,
            string planName)
        {
            if (buildPlans == null) throw new ArgumentNullException(nameof(buildPlans));
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));
            if (playerContext == null) throw new ArgumentNullException(nameof(playerContext));

            var sw = Stopwatch.StartNew();

            // Collect completed flatpack items grouped by destination colony
            var flatpacksByDestination = CollectCompletedFlatpacks(
                buildPlans, blueprintFinder);

            // Create a new delivery plan
            var plan = CreateNewPlan(route, playerContext, planName ?? "Flatpack Delivery");

            // Build stops from flatpack groups
            BuildFlatpackStops(plan, route, flatpacksByDestination, colonyFinder);

            sw.Stop();
            Log.Info(
                "PERF GenerateFlatpackDeliveryPlan: '{0}' ({1}), {2} stops in {3}ms",
                plan.Name,
                plan.UUID,
                plan.Stops.Count,
                sw.ElapsedMilliseconds);
            return plan;
        }

        /// <summary>
        /// Groups shortfalls by build location UUID from a single build plan.
        /// Returns a dictionary of locationUUID -> (resourceName -> totalShortfall).
        /// </summary>
        private static Dictionary<string, Dictionary<string, int>> GroupShortfallsByLocation(
            BuildPlan buildPlan,
            Dictionary<string, Dictionary<string, int>> shortfalls)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();

            foreach (var item in buildPlan.Items)
            {
                Dictionary<string, int> itemShortfalls;
                if (!shortfalls.TryGetValue(item.UUID, out itemShortfalls))
                    continue;
                if (string.IsNullOrEmpty(item.BuildLocationUUID))
                    continue;

                Dictionary<string, int> locationMap;
                if (!result.TryGetValue(item.BuildLocationUUID, out locationMap))
                {
                    locationMap = new Dictionary<string, int>();
                    result[item.BuildLocationUUID] = locationMap;
                }

                foreach (var kvp in itemShortfalls)
                {
                    int existing;
                    locationMap.TryGetValue(kvp.Key, out existing);
                    locationMap[kvp.Key] = existing + kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Merges shortfalls across multiple build plans by location.
        /// </summary>
        private static Dictionary<string, Dictionary<string, int>> MergeShortfallsAcrossPlans(
            IEnumerable<BuildPlan> buildPlans,
            Func<BuildPlan, Dictionary<string, Dictionary<string, int>>> shortfallProvider)
        {
            var merged = new Dictionary<string, Dictionary<string, int>>();

            foreach (var plan in buildPlans)
            {
                var planShortfalls = shortfallProvider(plan);
                if (planShortfalls == null) continue;

                var locationShortfalls = GroupShortfallsByLocation(plan, planShortfalls);
                MergeInto(merged, locationShortfalls);
            }

            return merged;
        }

        /// <summary>
        /// Merges source location shortfalls into the target dictionary.
        /// </summary>
        private static void MergeInto(
            Dictionary<string, Dictionary<string, int>> target,
            Dictionary<string, Dictionary<string, int>> source)
        {
            foreach (var locKvp in source)
            {
                Dictionary<string, int> targetMap;
                if (!target.TryGetValue(locKvp.Key, out targetMap))
                {
                    targetMap = new Dictionary<string, int>();
                    target[locKvp.Key] = targetMap;
                }

                foreach (var resKvp in locKvp.Value)
                {
                    int existing;
                    targetMap.TryGetValue(resKvp.Key, out existing);
                    targetMap[resKvp.Key] = existing + resKvp.Value;
                }
            }
        }

        /// <summary>
        /// Finds an existing delivery plan or creates a new one.
        /// </summary>
        private static DeliveryPlan FindOrCreatePlan(
            string existingPlanUUID,
            DeliveryRoute route,
            PlayerContext playerContext,
            string planName)
        {
            if (!string.IsNullOrEmpty(existingPlanUUID))
            {
                var existing = playerContext.DeliveryPlanList
                    .FirstOrDefault(p => p.UUID == existingPlanUUID);
                if (existing != null)
                {
                    Log.Debug(
                        "FindOrCreatePlan: updating existing plan '{0}' ({1})",
                        existing.Name,
                        existing.UUID);
                    existing.Name = planName;
                    existing.RouteUUID = route.UUID;
                    existing.Stops.Clear();
                    return existing;
                }
            }

            return CreateNewPlan(route, playerContext, planName);
        }

        /// <summary>
        /// Creates a new delivery plan and adds it to the player context.
        /// </summary>
        private static DeliveryPlan CreateNewPlan(
            DeliveryRoute route,
            PlayerContext playerContext,
            string planName)
        {
            var plan = new DeliveryPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = planName,
                OwnerUUID = route.OwnerUUID,
                RouteUUID = route.UUID
            };

            playerContext.AddDeliveryPlan(plan);
            Log.Debug("CreateNewPlan: created '{0}' ({1})", plan.Name, plan.UUID);
            return plan;
        }

        /// <summary>
        /// Rebuilds delivery plan stops from location-grouped shortfalls.
        /// Only creates stops for locations that appear on the route.
        /// </summary>
        private static void RebuildStopsFromShortfalls(
            DeliveryPlan plan,
            DeliveryRoute route,
            Dictionary<string, Dictionary<string, int>> locationShortfalls,
            Func<string, Colony> colonyFinder)
        {
            plan.Stops.Clear();

            foreach (var routeStop in CollectionSortHelper.OrderRouteStops(route.Stops))
            {
                string locationUUID = ResolveStopLocationUUID(routeStop);
                Dictionary<string, int> resources;
                if (!locationShortfalls.TryGetValue(locationUUID, out resources))
                    continue;

                var planStop = CreatePlanStop(routeStop);
                AddResourceDropOffs(planStop, resources, colonyFinder, locationUUID);

                if (planStop.DropOff.Count > 0)
                    plan.Stops.Add(planStop);
            }
        }

        /// <summary>
        /// Resolves the location UUID from a route stop, preferring DestinationUUID
        /// and falling back to ColonyUUID for legacy data.
        /// </summary>
        private static string ResolveStopLocationUUID(RouteStop routeStop)
        {
            if (!string.IsNullOrEmpty(routeStop.DestinationUUID))
                return routeStop.DestinationUUID;
            return routeStop.ColonyUUID ?? string.Empty;
        }

        /// <summary>
        /// Creates a DeliveryPlanStop from a RouteStop.
        /// </summary>
        private static DeliveryPlanStop CreatePlanStop(RouteStop routeStop)
        {
            return new DeliveryPlanStop
            {
                ColonyUUID = routeStop.ColonyUUID,
                Sequence = routeStop.Sequence,
                DestinationType = routeStop.DestinationType,
                DestinationUUID = ResolveStopLocationUUID(routeStop)
            };
        }

        /// <summary>
        /// Adds resource drop-off items to a delivery plan stop.
        /// </summary>
        private static void AddResourceDropOffs(
            DeliveryPlanStop planStop,
            Dictionary<string, int> resources,
            Func<string, Colony> colonyFinder,
            string locationUUID)
        {
            foreach (var kvp in resources.OrderBy(r => r.Key))
            {
                if (kvp.Value <= 0) continue;

                string purity = PriceCalculator.DeterminePurity(kvp.Key);
                var deliveryItem = new DeliveryItem
                {
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    BaseItemTypeID = kvp.Key,
                    Name = kvp.Key,
                    ResourcePurity = purity,
                    Quantity = kvp.Value
                };

                planStop.DropOff.Add(deliveryItem);
            }
        }

        /// <summary>
        /// Updates build items that have shortfalls to Delivering status.
        /// </summary>
        private static void UpdateItemStatusToDelivering(
            BuildPlan buildPlan,
            Dictionary<string, Dictionary<string, int>> shortfalls)
        {
            foreach (var item in buildPlan.Items)
            {
                if (shortfalls.ContainsKey(item.UUID))
                {
                    item.Status = BuildItemStatus.Delivering;
                }
            }
        }

        /// <summary>
        /// Collects completed flatpack items grouped by destination colony UUID.
        /// Extracts the destination colony UUID from the item's Notes field.
        /// Returns destinationColonyUUID -> list of (itemName, blueprintUUID, quantity).
        /// </summary>
        private static Dictionary<string, List<FlatpackInfo>> CollectCompletedFlatpacks(
            IEnumerable<BuildPlan> buildPlans,
            Func<string, Blueprint> blueprintFinder)
        {
            var result = new Dictionary<string, List<FlatpackInfo>>();

            foreach (var plan in buildPlans)
            {
                foreach (var item in plan.Items)
                {
                    if (item.Status != BuildItemStatus.Completed)
                        continue;
                    if (item.ItemType != BuildItemType.Manufactory)
                        continue;

                    string destColonyUUID = ExtractDestinationColonyUUID(item.Notes);
                    if (string.IsNullOrEmpty(destColonyUUID))
                        continue;

                    List<FlatpackInfo> list;
                    if (!result.TryGetValue(destColonyUUID, out list))
                    {
                        list = new List<FlatpackInfo>();
                        result[destColonyUUID] = list;
                    }

                    string itemName = item.ItemName;
                    if (string.IsNullOrEmpty(itemName))
                    {
                        var bp = blueprintFinder(item.BlueprintUUID);
                        itemName = bp != null ? bp.Name : "Unknown";
                    }

                    list.Add(new FlatpackInfo
                    {
                        ItemName = itemName,
                        BlueprintUUID = item.BlueprintUUID,
                        Quantity = item.Quantity
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a colony UUID from a build item's Notes field.
        /// Notes format: "For colony: ColonyName (colony-uuid)"
        /// </summary>
        private static string ExtractDestinationColonyUUID(string notes)
        {
            if (string.IsNullOrEmpty(notes)) return null;

            int openParen = notes.LastIndexOf('(');
            int closeParen = notes.LastIndexOf(')');
            if (openParen < 0 || closeParen <= openParen)
                return null;

            return notes.Substring(openParen + 1, closeParen - openParen - 1);
        }

        /// <summary>
        /// Builds flatpack delivery stops on the plan.
        /// </summary>
        private static void BuildFlatpackStops(
            DeliveryPlan plan,
            DeliveryRoute route,
            Dictionary<string, List<FlatpackInfo>> flatpacksByDestination,
            Func<string, Colony> colonyFinder)
        {
            plan.Stops.Clear();

            foreach (var routeStop in CollectionSortHelper.OrderRouteStops(route.Stops))
            {
                string locationUUID = ResolveStopLocationUUID(routeStop);
                List<FlatpackInfo> flatpacks;
                if (!flatpacksByDestination.TryGetValue(locationUUID, out flatpacks))
                    continue;

                var planStop = CreatePlanStop(routeStop);

                foreach (var fp in CollectionSortHelper.OrderByName(flatpacks, f => f.ItemName))
                {
                    var deliveryItem = new DeliveryItem
                    {
                        ItemType = ItemType.ItemTypeEnum.Blueprint,
                        BaseItemTypeID = fp.BlueprintUUID,
                        Name = fp.ItemName,
                        Quantity = fp.Quantity
                    };

                    planStop.DropOff.Add(deliveryItem);
                }

                if (planStop.DropOff.Count > 0)
                    plan.Stops.Add(planStop);
            }
        }

        /// <summary>
        /// Internal data class for flatpack collection.
        /// </summary>
        private class FlatpackInfo
        {
            public string ItemName { get; set; }
            public string BlueprintUUID { get; set; }
            public int Quantity { get; set; }
        }
    }
}
