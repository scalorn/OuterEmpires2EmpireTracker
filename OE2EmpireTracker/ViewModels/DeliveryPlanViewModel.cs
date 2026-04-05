using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    public class DeliveryPlanViewModel
    {
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
        /// Gets or creates the DeliveryPlanStop for the given colony UUID.
        /// </summary>
        public DeliveryPlanStop GetOrCreateStop(string colonyUUID, int sequence)
        {
            var stop = _plan.Stops.FirstOrDefault(s => s.ColonyUUID == colonyUUID);
            if (stop == null)
            {
                stop = new DeliveryPlanStop
                {
                    ColonyUUID = colonyUUID,
                    Sequence = sequence
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
            var existing = playerContext.deliveryPlanList
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
            if (!_playerContext.deliveryPlanList.Contains(_plan))
            {
                _playerContext.deliveryPlanList.Add(_plan);
            }
            _playerContext.writeContext();
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
    }
}
