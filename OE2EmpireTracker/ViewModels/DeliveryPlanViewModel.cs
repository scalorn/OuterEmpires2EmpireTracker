using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    public class DeliveryPlanViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly PlayerContext _playerContext;
        private DeliveryPlan _plan;

        public DeliveryPlan Data => _plan;
        public string UUID => _plan.UUID;

        public DeliveryPlanViewModel(DeliveryPlan plan, PlayerContext playerContext)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Gets or creates the DeliveryPlanStop for the given destination.
        /// </summary>
        public DeliveryPlanStop GetOrCreateStop(string colonyUUID, int sequence,
            DestinationType destType = DestinationType.Colony, string destinationUUID = "")
        {
            // Match by DestinationUUID first if available, then fall back to ColonyUUID
            DeliveryPlanStop stop = null;
            if (!string.IsNullOrEmpty(destinationUUID))
                stop = _plan.Stops.FirstOrDefault(s => s.DestinationUUID == destinationUUID);
            if (stop == null)
                stop = _plan.Stops.FirstOrDefault(s => s.ColonyUUID == colonyUUID && string.IsNullOrEmpty(s.DestinationUUID));
            if (stop == null)
            {
                stop = new DeliveryPlanStop
                {
                    ColonyUUID = colonyUUID,
                    Sequence = sequence,
                    DestinationType = destType,
                    DestinationUUID = destinationUUID ?? ""
                };
                _plan.Stops.Add(stop);
            }
            return stop;
        }

        public void AddDropOffItem(DeliveryPlanStop stop, ItemType.ItemTypeEnum itemType,
            string baseItemTypeID, string name, int quantity, string resourcePurity = "")
        {
            stop.DropOff.Add(new DeliveryItem
            {
                ItemType = itemType,
                BaseItemTypeID = baseItemTypeID,
                Name = name,
                Quantity = quantity,
                ResourcePurity = resourcePurity
            });
        }

        public void AddPickUpItem(DeliveryPlanStop stop, ItemType.ItemTypeEnum itemType,
            string baseItemTypeID, string name, int quantity, string resourcePurity = "")
        {
            stop.PickUp.Add(new DeliveryItem
            {
                ItemType = itemType,
                BaseItemTypeID = baseItemTypeID,
                Name = name,
                Quantity = quantity,
                ResourcePurity = resourcePurity
            });
        }

        public void RemoveDropOffItems(DeliveryPlanStop stop, IEnumerable<int> indices)
        {
            foreach (int i in indices.OrderByDescending(x => x))
                if (i >= 0 && i < stop.DropOff.Count)
                    stop.DropOff.RemoveAt(i);
        }

        public void RemovePickUpItems(DeliveryPlanStop stop, IEnumerable<int> indices)
        {
            foreach (int i in indices.OrderByDescending(x => x))
                if (i >= 0 && i < stop.PickUp.Count)
                    stop.PickUp.RemoveAt(i);
        }

        /// <summary>
        /// Finds or creates a DeliveryPlan for the given route.
        /// </summary>
        public static DeliveryPlanViewModel FindOrCreateForRoute(string routeUUID, PlayerContext playerContext)
        {
            var existing = playerContext.DeliveryPlanList
                .FirstOrDefault(p => p.RouteUUID == routeUUID && p.OwnerUUID == playerContext.CurrentPlayerUUID);
            if (existing != null)
                return new DeliveryPlanViewModel(existing, playerContext);

            var plan = new DeliveryPlan
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = playerContext.CurrentPlayerUUID,
                RouteUUID = routeUUID
            };
            return new DeliveryPlanViewModel(plan, playerContext);
        }

        public void Save()
        {
            if (!_playerContext.DeliveryPlanList.Contains(_plan))
            {
                _playerContext.AddDeliveryPlan(_plan);
            }
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
        }

        /// <summary>
        /// Scans each stop's colony for unfulfilled CommodityRequested entries
        /// and adds drop-off DeliveryItems for the shortfall quantities.
        /// </summary>
        /// <param name="routeStops">Route stops providing ColonyUUID and Sequence.</param>
        /// <param name="colonyFinder">Delegate to find a Colony by UUID.</param>
        /// <returns>Number of items added.</returns>
        public int AutoFillCommodities(IEnumerable<RouteStop> routeStops, Func<string, Colony> colonyFinder)
        {
            int added = 0;
            foreach (var routeStop in routeStops.OrderBy(s => s.Sequence))
            {
                var colony = colonyFinder(routeStop.ColonyUUID);
                if (colony == null) continue;

                var stop = GetOrCreateStop(routeStop.ColonyUUID, routeStop.Sequence);

                foreach (var cr in colony.Commodities)
                {
                    if (cr.Fulfilled) continue;
                    int shortfall = cr.Requested - cr.Delivered;
                    if (shortfall <= 0) continue;

                    AddDropOffItem(stop, ItemType.ItemTypeEnum.Commodity, cr.Name, cr.Name, shortfall);
                    added++;
                }
            }
            return added;
        }

        /// <summary>
        /// Scans each stop's colony for unbuilt+unstaged structures and adds
        /// flatpack drop-off items for each one.
        /// </summary>
        public int AutoFillFlatpacks(IEnumerable<RouteStop> routeStops, Func<string, Colony> colonyFinder,
            int timeHorizonHours = 0)
        {
            int added = 0;
            foreach (var routeStop in routeStops.OrderBy(s => s.Sequence))
            {
                var colony = colonyFinder(routeStop.ColonyUUID);
                if (colony == null) { Log.Debug("AutoFillFlatpacks: colony not found for {0}", routeStop.ColonyUUID); continue; }

                Log.Debug("AutoFillFlatpacks: colony {0} has {1} structures", colony.ColonyName, colony.Structures.Count);

                // Aggregate flatpack counts by blueprint UUID
                var flatpackCounts = new Dictionary<string, int>();
                var flatpackNames = new Dictionary<string, string>();

                foreach (var structure in colony.Structures)
                {
                    var vm = new ColonyStructureViewModel(structure, _playerContext);
                    Log.Debug("  Structure {0}: IsBuilt={1}, IsStaged={2}, FlatpackBP={3}",
                        structure.UUID, vm.IsBuilt, vm.IsStaged, structure.FlatpackBlueprintUUID);
                    if (vm.IsBuilt || vm.IsStaged) continue;

                    // Time horizon filter: skip structures that won't complete within the horizon
                    if (timeHorizonHours > 0 && structure.BuildCompletionTime != null)
                    {
                        var completionTime = structure.BuildCompletionTime.EndTime;
                        if (completionTime > SystemClock.UtcNow.AddHours(timeHorizonHours))
                        {
                            Log.Debug("  Skipped (outside time horizon): completion={0}", completionTime);
                            continue;
                        }
                    }

                    var blueprint = _playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                    if (blueprint == null) { Log.Debug("  Blueprint not found: {0}", structure.FlatpackBlueprintUUID); continue; }

                    string bpUUID = structure.FlatpackBlueprintUUID;
                    int count = 0;
                    flatpackCounts.TryGetValue(bpUUID, out count);
                    flatpackCounts[bpUUID] = count + 1;
                    flatpackNames[bpUUID] = blueprint.ExtendedName;
                }

                if (flatpackCounts.Count == 0) continue;

                var stop = GetOrCreateStop(routeStop.ColonyUUID, routeStop.Sequence);
                foreach (var entry in flatpackCounts)
                {
                    AddDropOffItem(stop, ItemType.ItemTypeEnum.Flatpack,
                        entry.Key, flatpackNames[entry.Key], entry.Value);
                    added++;
                    Log.Debug("  Added flatpack: {0} x{1}", flatpackNames[entry.Key], entry.Value);
                }
            }
            Log.Debug("AutoFillFlatpacks: total added={0}", added);
            return added;
        }

        /// <summary>
        /// Scans each stop's colony for structures with StagingResources=true,
        /// calculates resource shortfalls, and adds drop-off items for Refined resources.
        /// When a route stop is a station, checks station holds for available inventory.
        /// </summary>
        public int AutoFillManufacturingResources(IEnumerable<RouteStop> routeStops,
            Func<string, Colony> colonyFinder, Func<string, Blueprint> blueprintFinder,
            Func<string, Station> stationFinder = null, string currentPlayerUUID = null)
        {
            int added = 0;
            foreach (var routeStop in routeStops.OrderBy(s => s.Sequence))
            {
                var colony = colonyFinder(routeStop.ColonyUUID);
                if (colony == null) continue;

                // Aggregate resource needs per colony
                var resourceNeeds = new Dictionary<string, int>();

                foreach (var structure in colony.Structures)
                {
                    if (!structure.StagingResources) continue;
                    if (structure.ManufacturingQuantity <= 0) continue;

                    var flatpackBp = blueprintFinder(structure.FlatpackBlueprintUUID);
                    if (flatpackBp == null) continue;

                    if (flatpackBp.BluePrintType == BlueprintTypes.Manufactory)
                    {
                        if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)) continue;
                        var mfgBp = blueprintFinder(structure.ManufacturingBlueprintUUID);
                        if (mfgBp == null || mfgBp.Resources == null) continue;

                        foreach (var resource in mfgBp.Resources)
                        {
                            int perUnit = 0;
                            int.TryParse(resource.Value, out perUnit);
                            if (perUnit <= 0) continue;

                            int total = perUnit * structure.ManufacturingQuantity;
                            int current = 0;
                            resourceNeeds.TryGetValue(resource.Key, out current);
                            resourceNeeds[resource.Key] = current + total;
                        }
                    }
                    else if (flatpackBp.BluePrintType.IsCommodityFactory())
                    {
                        if (string.IsNullOrEmpty(structure.ManufacturingCommodityName)) continue;
                        Commodity commodity;
                        if (!Commodity.ResourceMapByString.TryGetValue(structure.ManufacturingCommodityName, out commodity))
                            continue;

                        foreach (var resource in commodity.ConstructionResources)
                        {
                            int perCycle = 0;
                            int.TryParse(resource.Value, out perCycle);
                            if (perCycle <= 0) continue;

                            int total = perCycle * structure.ManufacturingQuantity;
                            int current = 0;
                            resourceNeeds.TryGetValue(resource.Key, out current);
                            resourceNeeds[resource.Key] = current + total;
                        }
                    }
                }

                if (resourceNeeds.Count == 0) continue;

                var stop = GetOrCreateStop(routeStop.ColonyUUID, routeStop.Sequence);

                foreach (var need in resourceNeeds)
                {
                    // Subtract warehouse stock of Refined resources
                    var warehouseItems = colony.Items.FindResource(need.Key, GameConstants.PurityRefined);
                    int warehouseQty = warehouseItems.Sum(i => i.Quantity);

                    // Also check station inventory if this stop has a station nearby
                    int stationQty = 0;
                    if (stationFinder != null && !string.IsNullOrEmpty(currentPlayerUUID)
                        && routeStop.DestinationType == DestinationType.Station)
                    {
                        var station = stationFinder(routeStop.DestinationUUID);
                        if (station != null)
                        {
                            ItemBag hold;
                            if (station.Holds.TryGetValue(currentPlayerUUID, out hold))
                            {
                                var stationItems = hold.FindResource(need.Key, GameConstants.PurityRefined);
                                stationQty = stationItems.Sum(i => i.Quantity);
                            }
                        }
                    }

                    int shortfall = need.Value - warehouseQty - stationQty;
                    if (shortfall <= 0) continue;

                    AddDropOffItem(stop, ItemType.ItemTypeEnum.Resource,
                        need.Key, need.Key, shortfall, GameConstants.PurityRefined);
                    added++;
                }
            }
            return added;
        }

        /// <summary>
        /// Computes ideal vs actual worker gaps per colony and adds
        /// drop-off items for worker shortfalls.
        /// </summary>
        public int AutoFillWorkers(IEnumerable<RouteStop> routeStops,
            Func<string, Colony> colonyFinder, PlayerContext playerContext)
        {
            int added = 0;
            foreach (var routeStop in routeStops.OrderBy(s => s.Sequence))
            {
                var colony = colonyFinder(routeStop.ColonyUUID);
                if (colony == null) continue;
                if (colony.Structures.Count == 0) continue;

                var calc = new ColonyStatusCalculator(colony);
                calc.CalculateBuilt();
                calc.CalculateIdeal();

                if (calc.finalIdealStatus == null || calc.finalActualStatus == null) continue;
                if (calc.finalIdealStatus.HabitationRequired <= calc.finalActualStatus.HabitationRequired)
                    continue;

                var stop = GetOrCreateStop(routeStop.ColonyUUID, routeStop.Sequence);

                foreach (var wt in WorkerDetail.WorkerTypes)
                {
                    // Sum ideal worker count across all structures
                    int idealCount = 0;
                    int actualCount = 0;

                    foreach (var structure in colony.Structures)
                    {
                        var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                        if (blueprint == null) continue;
                        if (!blueprint.Properties.ContainsKey(wt.PropertyKey)) continue;

                        long slotCount = 0;
                        blueprint.Properties.getLong(wt.PropertyKey, 0, out slotCount);
                        idealCount += (int)slotCount;

                        // Count actual assigned workers
                        for (int i = 1; i <= slotCount; i++)
                        {
                            string key = wt.WorkerPrefix + i;
                            bool assigned = false;
                            structure.AssignedWorkers.getBoolean(key, false, out assigned);
                            if (assigned) actualCount++;
                        }
                    }

                    int gap = idealCount - actualCount;
                    if (gap <= 0) continue;

                    WorkerDetail workerDetail;
                    if (!WorkerDetail.WorkerDetailMapByID.TryGetValue(wt.DetailKey, out workerDetail))
                        continue;

                    AddDropOffItem(stop, ItemType.ItemTypeEnum.WorkDetail,
                        wt.DetailKey, workerDetail.Name, gap);
                    added++;
                }
            }
            return added;
        }
    }
}
