using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all DeliveryRoute mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus JSON deserialization and migration code) mutates DeliveryRoute objects.
    /// </summary>
    public class DeliveryRouteService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public DeliveryRouteService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing delivery route, persists, and fires the change event.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyDeliveryRoute Update(string uuid, DeliveryRouteUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var route = _playerContext.FindMutableDeliveryRoute(uuid);
            if (route == null)
            {
                throw new InvalidOperationException("Route not found: " + uuid);
            }

            Log.Info(
                "DeliveryRouteService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid,
                route.Name,
                request.Name);

            route.Name = request.Name;
            route.Stops = DeepCopyStops(request.Stops);
            RenumberStops(route.Stops);

            _playerContext.MarkDirty<DeliveryRoute>(route.UUID);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryRoute(route);
        }

        /// <summary>
        /// Creates a new delivery route, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyDeliveryRoute Create(DeliveryRouteCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var route = new DeliveryRoute();
            route.UUID = Guid.NewGuid().ToString();
            route.OwnerUUID = _playerContext.CurrentPlayerUUID;
            route.Name = request.Name;
            route.Stops = DeepCopyStops(request.Stops);
            RenumberStops(route.Stops);

            Log.Info(
                "DeliveryRouteService.Create: name='{0}' UUID={1}",
                route.Name,
                route.UUID);

            _playerContext.AddDeliveryRoute(route);
            _playerContext.MarkDirty<DeliveryRoute>(route.UUID);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
            return new ReadOnlyDeliveryRoute(route);
        }

        /// <summary>
        /// Removes a delivery route. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var route = _playerContext.FindMutableDeliveryRoute(uuid);
            if (route == null)
            {
                return;
            }

            Log.Info("DeliveryRouteService.Delete: UUID={0} name='{1}'", uuid, route.Name);

            _playerContext.RemoveDeliveryRoute(route);
            _playerContext.MarkDeleted<DeliveryRoute>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
        }

        private static List<RouteStop> DeepCopyStops(List<RouteStop> source)
        {
            var copy = new List<RouteStop>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new RouteStop
                    {
                        ColonyUUID = s.ColonyUUID,
                        Sequence = s.Sequence,
                        DestinationType = s.DestinationType,
                        DestinationUUID = s.DestinationUUID,
                        Purpose = s.Purpose,
                        FuelEstimate = s.FuelEstimate,
                    });
                }
            }

            return copy;
        }

        private static void RenumberStops(List<RouteStop> stops)
        {
            for (int i = 0; i < stops.Count; i++)
            {
                stops[i].Sequence = i + 1;
            }
        }
    }
}
