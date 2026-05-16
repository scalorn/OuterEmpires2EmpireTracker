using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all DeliveryPlan mutation. Neither form nor ViewModel touches the entity directly.
    /// Only this service (plus JSON deserialization and migration code) mutates DeliveryPlan objects.
    /// </summary>
    public class DeliveryPlanService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public DeliveryPlanService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // -------------------------------------------------------------------
        // CRUD
        // -------------------------------------------------------------------

        /// <summary>
        /// Creates a new delivery plan, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyDeliveryPlan Create(string name, string routeUUID)
        {
            var plan = new DeliveryPlan();
            plan.UUID = Guid.NewGuid().ToString();
            plan.OwnerUUID = _playerContext.CurrentPlayerUUID;
            plan.Name = name ?? string.Empty;
            plan.RouteUUID = routeUUID ?? string.Empty;

            Log.Info(
                "DeliveryPlanService.Create: name='{0}' routeUUID={1} UUID={2}",
                plan.Name,
                plan.RouteUUID,
                plan.UUID);

            _playerContext.AddDeliveryPlan(plan);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Removes a delivery plan. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                return;
            }

            Log.Info("DeliveryPlanService.Delete: UUID={0} name='{1}'", uuid, plan.Name);

            _playerContext.RemoveDeliveryPlan(plan);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
        }

        /// <summary>
        /// Applies changes from the update request to an existing delivery plan, persists, and fires the change event.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyDeliveryPlan UpdatePlan(string uuid, DeliveryPlanUpdateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            Log.Info(
                "DeliveryPlanService.UpdatePlan: UUID={0} name='{1}' -> '{2}'",
                uuid,
                plan.Name,
                request.Name);

            plan.Name = request.Name ?? string.Empty;
            plan.Stops = DeepCopyStops(request.Stops);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        // -------------------------------------------------------------------
        // Item Operations
        // -------------------------------------------------------------------

        /// <summary>
        /// Adds a drop-off item to the specified stop on the plan.
        /// Finds or creates the stop by destination info.
        /// </summary>
        public ReadOnlyDeliveryPlan AddDropOffItem(string uuid, StopDestinationInfo destInfo, DeliveryItemInfo itemInfo)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = FindOrCreateStop(plan, destInfo);
            stop.DropOff.Add(new DeliveryItem
            {
                ItemType = itemInfo.ItemType,
                BaseItemTypeID = itemInfo.BaseItemTypeID ?? string.Empty,
                Name = itemInfo.Name ?? string.Empty,
                Quantity = itemInfo.Quantity,
                ResourcePurity = itemInfo.ResourcePurity ?? string.Empty,
            });

            Log.Info(
                "DeliveryPlanService.AddDropOffItem: plan='{0}' stop={1} item='{2}' qty={3}",
                plan.Name,
                stop.Sequence,
                itemInfo.Name,
                itemInfo.Quantity);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Adds a pick-up item to the specified stop on the plan.
        /// Finds or creates the stop by destination info.
        /// </summary>
        public ReadOnlyDeliveryPlan AddPickUpItem(string uuid, StopDestinationInfo destInfo, DeliveryItemInfo itemInfo)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = FindOrCreateStop(plan, destInfo);
            stop.PickUp.Add(new DeliveryItem
            {
                ItemType = itemInfo.ItemType,
                BaseItemTypeID = itemInfo.BaseItemTypeID ?? string.Empty,
                Name = itemInfo.Name ?? string.Empty,
                Quantity = itemInfo.Quantity,
                ResourcePurity = itemInfo.ResourcePurity ?? string.Empty,
            });

            Log.Info(
                "DeliveryPlanService.AddPickUpItem: plan='{0}' stop={1} item='{2}' qty={3}",
                plan.Name,
                stop.Sequence,
                itemInfo.Name,
                itemInfo.Quantity);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Removes drop-off items at the specified indices from the matching stop.
        /// Indices are processed in descending order to preserve positions.
        /// </summary>
        public ReadOnlyDeliveryPlan RemoveDropOffItems(string uuid, StopDestinationInfo destInfo, IEnumerable<int> indices)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = FindStop(plan, destInfo);
            if (stop != null)
            {
                foreach (int idx in indices.OrderByDescending(i => i))
                {
                    if (idx >= 0 && idx < stop.DropOff.Count)
                    {
                        stop.DropOff.RemoveAt(idx);
                    }
                }
            }

            Log.Info(
                "DeliveryPlanService.RemoveDropOffItems: plan='{0}' destUUID={1}",
                plan.Name,
                destInfo?.DestinationUUID);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Removes pick-up items at the specified indices from the matching stop.
        /// Indices are processed in descending order to preserve positions.
        /// </summary>
        public ReadOnlyDeliveryPlan RemovePickUpItems(string uuid, StopDestinationInfo destInfo, IEnumerable<int> indices)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = FindStop(plan, destInfo);
            if (stop != null)
            {
                foreach (int idx in indices.OrderByDescending(i => i))
                {
                    if (idx >= 0 && idx < stop.PickUp.Count)
                    {
                        stop.PickUp.RemoveAt(idx);
                    }
                }
            }

            Log.Info(
                "DeliveryPlanService.RemovePickUpItems: plan='{0}' destUUID={1}",
                plan.Name,
                destInfo?.DestinationUUID);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        // -------------------------------------------------------------------
        // Execution Operations
        // -------------------------------------------------------------------

        /// <summary>
        /// Sets the Delivered flag on a specific item.
        /// </summary>
        public ReadOnlyDeliveryPlan MarkItemDelivered(string uuid, int stopSequence, int itemIndex, string listType, bool delivered)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = plan.Stops.FirstOrDefault(s => s.Sequence == stopSequence);
            if (stop == null)
            {
                throw new InvalidOperationException(
                    string.Format("Stop with sequence {0} not found in plan {1}", stopSequence, uuid));
            }

            List<DeliveryItem> itemList;
            if (string.Equals(listType, "DropOff", StringComparison.OrdinalIgnoreCase))
            {
                itemList = stop.DropOff;
            }
            else if (string.Equals(listType, "PickUp", StringComparison.OrdinalIgnoreCase))
            {
                itemList = stop.PickUp;
            }
            else
            {
                throw new ArgumentException("listType must be 'DropOff' or 'PickUp'", nameof(listType));
            }

            if (itemIndex < 0 || itemIndex >= itemList.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemIndex),
                    string.Format("Item index {0} out of range for {1} list (count={2})", itemIndex, listType, itemList.Count));
            }

            var item = itemList[itemIndex];
            item.Delivered = delivered;

            Log.Info(
                "DeliveryPlanService.MarkItemDelivered: plan='{0}' stop={1} {2}[{3}] delivered={4}",
                plan.Name,
                stopSequence,
                listType,
                itemIndex,
                delivered);

            // Side effects: fulfillment operations on the target colony/station
            ApplyDeliveryFulfillment(item, delivered, stop);

            // Auto-complete plan if all items are delivered
            bool allDelivered = plan.Stops.All(s =>
                s.DropOff.All(i => i.Delivered) && s.PickUp.All(i => i.Delivered));
            if (allDelivered && !plan.Completed)
            {
                plan.Completed = true;
                Log.Info("DeliveryPlanService: plan '{0}' auto-completed (all items delivered)", plan.Name);
            }

            _playerContext.WriteContext();
            _playerContext.CascadeResourceCheckDirty = true;
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Sets StopCompleted to true on the specified stop.
        /// </summary>
        public ReadOnlyDeliveryPlan MarkStopComplete(string uuid, int stopSequence)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var stop = plan.Stops.FirstOrDefault(s => s.Sequence == stopSequence);
            if (stop == null)
            {
                throw new InvalidOperationException(
                    string.Format("Stop with sequence {0} not found in plan {1}", stopSequence, uuid));
            }

            stop.StopCompleted = true;

            Log.Info(
                "DeliveryPlanService.MarkStopComplete: plan='{0}' stop={1}",
                plan.Name,
                stopSequence);

            _playerContext.WriteContext();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Sets Completed to true on the plan.
        /// </summary>
        public ReadOnlyDeliveryPlan MarkPlanComplete(string uuid)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            plan.Completed = true;

            Log.Info("DeliveryPlanService.MarkPlanComplete: plan='{0}'", plan.Name);

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryPlan(plan);
        }

        /// <summary>
        /// Sets the ShipUUID field on the plan.
        /// </summary>
        public ReadOnlyDeliveryPlan SetShipUUID(string uuid, string shipUUID)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            plan.ShipUUID = shipUUID ?? string.Empty;

            Log.Info(
                "DeliveryPlanService.SetShipUUID: plan='{0}' shipUUID={1}",
                plan.Name,
                shipUUID);

            _playerContext.WriteContext();
            return new ReadOnlyDeliveryPlan(plan);
        }

        // -------------------------------------------------------------------
        // Trip Splitting
        // -------------------------------------------------------------------

        /// <summary>
        /// Splits a plan into multiple trips based on cargo capacity.
        /// Creates new DeliveryPlan entities for each additional trip (Trip 2, Trip 3, etc.).
        /// Renames the original plan with a "(Trip 1)" suffix.
        /// </summary>
        public List<ReadOnlyDeliveryPlan> SplitTrips(string uuid, decimal cargoCapacity, Func<string, ReadOnlyBlueprint> blueprintFinder)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("Plan not found: " + uuid);
            }

            var loadItems = plan.CalculateLoadList();
            var trips = CargoVolumeService.SplitIntoTrips(loadItems, cargoCapacity, blueprintFinder);

            var newPlans = new List<ReadOnlyDeliveryPlan>();

            if (trips.Count <= 1)
            {
                return newPlans;
            }

            // Create new plans for trips 2..N
            for (int i = 1; i < trips.Count; i++)
            {
                var newPlan = new DeliveryPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = string.Format("{0} (Trip {1})", plan.Name, i + 1),
                    OwnerUUID = plan.OwnerUUID,
                    RouteUUID = plan.RouteUUID,
                    ShipUUID = plan.ShipUUID,
                };

                foreach (var stop in CollectionSortHelper.OrderPlanStops(plan.Stops))
                {
                    var newStop = new DeliveryPlanStop
                    {
                        ColonyUUID = stop.ColonyUUID,
                        Sequence = stop.Sequence,
                        DestinationType = stop.DestinationType,
                        DestinationUUID = stop.DestinationUUID,
                    };

                    foreach (var dropItem in stop.DropOff)
                    {
                        var tripItem = trips[i].FirstOrDefault(t =>
                            t.ItemType == dropItem.ItemType &&
                            t.BaseItemTypeID == dropItem.BaseItemTypeID &&
                            t.ResourcePurity == dropItem.ResourcePurity);
                        if (tripItem != null && tripItem.Quantity > 0)
                        {
                            int qty = Math.Min(tripItem.Quantity, dropItem.Quantity);
                            newStop.DropOff.Add(new DeliveryItem
                            {
                                ItemType = dropItem.ItemType,
                                BaseItemTypeID = dropItem.BaseItemTypeID,
                                Name = dropItem.Name,
                                ResourcePurity = dropItem.ResourcePurity,
                                Quantity = qty,
                            });
                            tripItem.Quantity -= qty;
                        }
                    }

                    foreach (var pickItem in stop.PickUp)
                    {
                        newStop.PickUp.Add(new DeliveryItem
                        {
                            ItemType = pickItem.ItemType,
                            BaseItemTypeID = pickItem.BaseItemTypeID,
                            Name = pickItem.Name,
                            ResourcePurity = pickItem.ResourcePurity,
                            Quantity = pickItem.Quantity,
                        });
                    }

                    if (newStop.DropOff.Count > 0 || newStop.PickUp.Count > 0)
                    {
                        newPlan.Stops.Add(newStop);
                    }
                }

                _playerContext.AddDeliveryPlan(newPlan);
                newPlans.Add(new ReadOnlyDeliveryPlan(newPlan));

                Log.Info(
                    "DeliveryPlanService.SplitTrips: created '{0}' (UUID={1})",
                    newPlan.Name,
                    newPlan.UUID);
            }

            // Rename original plan with Trip 1 suffix
            if (!plan.Name.Contains("(Trip"))
            {
                plan.Name = string.Format("{0} (Trip 1)", plan.Name);
            }

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return newPlans;
        }

        // -------------------------------------------------------------------
        // Plan Repair
        // -------------------------------------------------------------------

        /// <summary>
        /// Detects and repairs duplicate stops (same Sequence) in a plan by merging
        /// their items into a single stop. Call on plan load to fix legacy data.
        /// Returns true if repairs were made.
        /// </summary>
        public bool RepairDuplicateStops(string uuid)
        {
            var plan = _playerContext.FindMutableDeliveryPlan(uuid);
            if (plan == null)
            {
                return false;
            }

            var duplicateGroups = plan.Stops
                .GroupBy(s => s.Sequence)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateGroups.Count == 0)
            {
                return false;
            }

            foreach (var group in duplicateGroups)
            {
                var stops = group.ToList();
                var primary = stops[0];

                for (int i = 1; i < stops.Count; i++)
                {
                    var duplicate = stops[i];

                    foreach (var item in duplicate.DropOff)
                    {
                        primary.DropOff.Add(item);
                    }

                    foreach (var item in duplicate.PickUp)
                    {
                        primary.PickUp.Add(item);
                    }

                    if (string.IsNullOrEmpty(primary.DestinationUUID) && !string.IsNullOrEmpty(duplicate.DestinationUUID))
                    {
                        primary.DestinationUUID = duplicate.DestinationUUID;
                    }

                    if (string.IsNullOrEmpty(primary.ColonyUUID) && !string.IsNullOrEmpty(duplicate.ColonyUUID))
                    {
                        primary.ColonyUUID = duplicate.ColonyUUID;
                    }

                    plan.Stops.Remove(duplicate);
                }

                Log.Info(
                    "RepairDuplicateStops: merged {0} duplicate stop(s) at sequence {1} in plan '{2}'",
                    stops.Count - 1,
                    group.Key,
                    plan.Name);
            }

            _playerContext.WriteContext();
            return true;
        }

        // -------------------------------------------------------------------
        // Private Helpers
        // -------------------------------------------------------------------

        private static DeliveryPlanStop FindOrCreateStop(DeliveryPlan plan, StopDestinationInfo destInfo)
        {
            // Match by DestinationUUID first, then by ColonyUUID
            var stop = plan.Stops.FirstOrDefault(s =>
                !string.IsNullOrEmpty(destInfo.DestinationUUID) &&
                s.DestinationUUID == destInfo.DestinationUUID);

            if (stop == null)
            {
                stop = plan.Stops.FirstOrDefault(s =>
                    !string.IsNullOrEmpty(destInfo.ColonyUUID) &&
                    s.ColonyUUID == destInfo.ColonyUUID &&
                    string.IsNullOrEmpty(s.DestinationUUID));
            }

            if (stop == null)
            {
                stop = new DeliveryPlanStop
                {
                    ColonyUUID = destInfo.ColonyUUID ?? string.Empty,
                    Sequence = destInfo.Sequence,
                    DestinationType = destInfo.DestinationType,
                    DestinationUUID = destInfo.DestinationUUID ?? string.Empty,
                };
                plan.Stops.Add(stop);
            }

            return stop;
        }

        private static DeliveryPlanStop FindStop(DeliveryPlan plan, StopDestinationInfo destInfo)
        {
            // Match by DestinationUUID first, then by ColonyUUID
            var stop = plan.Stops.FirstOrDefault(s =>
                !string.IsNullOrEmpty(destInfo.DestinationUUID) &&
                s.DestinationUUID == destInfo.DestinationUUID);

            if (stop == null)
            {
                stop = plan.Stops.FirstOrDefault(s =>
                    !string.IsNullOrEmpty(destInfo.ColonyUUID) &&
                    s.ColonyUUID == destInfo.ColonyUUID &&
                    string.IsNullOrEmpty(s.DestinationUUID));
            }

            return stop;
        }

        private static List<DeliveryPlanStop> DeepCopyStops(List<DeliveryPlanStop> source)
        {
            var copy = new List<DeliveryPlanStop>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new DeliveryPlanStop
                    {
                        ColonyUUID = s.ColonyUUID,
                        Sequence = s.Sequence,
                        StopCompleted = s.StopCompleted,
                        DestinationType = s.DestinationType,
                        DestinationUUID = s.DestinationUUID,
                        DropOff = DeepCopyItems(s.DropOff),
                        PickUp = DeepCopyItems(s.PickUp),
                    });
                }
            }

            return copy;
        }

        private static List<DeliveryItem> DeepCopyItems(List<DeliveryItem> source)
        {
            var copy = new List<DeliveryItem>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    copy.Add(new DeliveryItem
                    {
                        ItemType = item.ItemType,
                        BaseItemTypeID = item.BaseItemTypeID,
                        Name = item.Name,
                        ResourcePurity = item.ResourcePurity,
                        Quantity = item.Quantity,
                        Delivered = item.Delivered,
                    });
                }
            }

            return copy;
        }

        /// <summary>
        /// Applies delivery fulfillment side effects when an item is marked delivered/undelivered.
        /// Handles commodity fulfillment, flatpack staging, worker delivery, resource delivery,
        /// and station hold updates.
        /// </summary>
        private void ApplyDeliveryFulfillment(DeliveryItem item, bool delivered, DeliveryPlanStop stop)
        {
            // Colony-based fulfillment
            if (item.ItemType == ItemType.ItemTypeEnum.Commodity)
            {
                var colony = _playerContext.FindColony(stop.ColonyUUID);
                if (colony != null)
                {
                    DeliveryFulfillment.FulfillCommodity(colony, item.BaseItemTypeID, delivered);
                    _playerContext.OnColonyDataChanged(stop.ColonyUUID);
                }
                else
                {
                    Log.Warn("Colony not found for stop {0} during commodity fulfillment", stop.ColonyUUID);
                }
            }

            if (item.ItemType == ItemType.ItemTypeEnum.Flatpack)
            {
                var colony = _playerContext.FindColony(stop.ColonyUUID);
                if (colony != null)
                {
                    DeliveryFulfillment.StageFlatpack(colony, item.BaseItemTypeID, delivered);
                    _playerContext.OnColonyDataChanged(stop.ColonyUUID);
                }
                else
                {
                    Log.Warn("Colony not found for stop {0} during flatpack staging", stop.ColonyUUID);
                }
            }

            if (item.ItemType == ItemType.ItemTypeEnum.WorkDetail)
            {
                var colony = _playerContext.FindColony(stop.ColonyUUID);
                if (colony != null)
                {
                    DeliveryFulfillment.DeliverWorkers(colony, item.BaseItemTypeID, item.Name, item.Quantity, delivered);
                    _playerContext.OnColonyDataChanged(stop.ColonyUUID);
                }
                else
                {
                    Log.Warn("Colony not found for stop {0} during worker delivery", stop.ColonyUUID);
                }
            }

            if (item.ItemType == ItemType.ItemTypeEnum.Resource)
            {
                var colony = _playerContext.FindColony(stop.ColonyUUID);
                if (colony != null)
                {
                    DeliveryFulfillment.DeliverResource(colony, item.BaseItemTypeID, item.ResourcePurity, item.Quantity, delivered);
                    _playerContext.OnColonyDataChanged(stop.ColonyUUID);
                }
                else
                {
                    Log.Warn("Colony not found for stop {0} during resource delivery", stop.ColonyUUID);
                }
            }

            // Station hold operations
            if (stop.DestinationType == DestinationType.Station)
            {
                ApplyStationHoldUpdate(item, delivered, stop);
            }
        }

        /// <summary>
        /// Updates station holds when delivery items are marked delivered at station stops.
        /// Drop-offs add items to the station hold; pick-ups remove items.
        /// </summary>
        private void ApplyStationHoldUpdate(DeliveryItem item, bool delivered, DeliveryPlanStop stop)
        {
            var station = _playerContext.FindStation(stop.DestinationUUID);
            if (station == null)
            {
                Log.Warn("Station not found for stop {0} during hold update", stop.DestinationUUID);
                return;
            }

            string playerUUID = _playerContext.CurrentPlayerUUID;
            if (string.IsNullOrEmpty(playerUUID)) return;

            ItemBag hold;
            if (!station.Holds.TryGetValue(playerUUID, out hold))
            {
                hold = new ItemBag();
                station.Holds[playerUUID] = hold;
            }

            bool isDropOff = stop.DropOff.Contains(item);

            if (isDropOff && delivered)
            {
                // Drop-off: add items to station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity += item.Quantity;
                }
                else
                {
                    var newItem = new Item(item.ItemType, item.BaseItemTypeID);
                    newItem.UUID = Guid.NewGuid().ToString();
                    newItem.BaseItemTypeID = item.BaseItemTypeID;
                    newItem.Name = item.Name;
                    newItem.Quantity = item.Quantity;
                    newItem.ResourcePurity = item.ResourcePurity;
                    hold.AddItem(newItem);
                }
            }
            else if (isDropOff && !delivered)
            {
                // Undo drop-off: remove items from station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity = Math.Max(0, existing[0].Quantity - item.Quantity);
                }
            }
            else if (!isDropOff && delivered)
            {
                // Pick-up: remove items from station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity = Math.Max(0, existing[0].Quantity - item.Quantity);
                }
            }
            else if (!isDropOff && !delivered)
            {
                // Undo pick-up: add items back to station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity += item.Quantity;
                }
                else
                {
                    var newItem = new Item(item.ItemType, item.BaseItemTypeID);
                    newItem.UUID = Guid.NewGuid().ToString();
                    newItem.BaseItemTypeID = item.BaseItemTypeID;
                    newItem.Name = item.Name;
                    newItem.Quantity = item.Quantity;
                    newItem.ResourcePurity = item.ResourcePurity;
                    hold.AddItem(newItem);
                }
            }

            Log.Info(
                "Station hold updated: station={0}, player={1}, item={2}, delivered={3}, isDropOff={4}",
                station.Name,
                playerUUID,
                item.BaseItemTypeID,
                delivered,
                isDropOff);
        }
    }
}
