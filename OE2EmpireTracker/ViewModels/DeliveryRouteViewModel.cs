using OE2EmpireTracker.Baseline;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    public class DeliveryRouteViewModel
    {
        private readonly PlayerContext _playerContext;
        private DeliveryRoute _route;

        public DeliveryRoute Data => _route;

        public DeliveryRouteViewModel(DeliveryRoute route, PlayerContext playerContext)
        {
            _route = route ?? throw new ArgumentNullException(nameof(route));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        public string Name { get => _route.Name; set => _route.Name = value; }
        public string UUID => _route.UUID;

        public IReadOnlyList<RouteStop> Stops => _route.Stops.AsReadOnly();

        public void AddStop(string colonyUUID)
        {
            _route.Stops.Add(new RouteStop
            {
                ColonyUUID = colonyUUID,
                Sequence = _route.Stops.Count
            });
            RenumberStops();
        }

        public void RemoveStop(int index)
        {
            if (index >= 0 && index < _route.Stops.Count)
            {
                _route.Stops.RemoveAt(index);
                RenumberStops();
            }
        }

        public void MoveStopUp(int index)
        {
            if (index > 0 && index < _route.Stops.Count)
            {
                var stop = _route.Stops[index];
                _route.Stops.RemoveAt(index);
                _route.Stops.Insert(index - 1, stop);
                RenumberStops();
            }
        }

        public void MoveStopDown(int index)
        {
            if (index >= 0 && index < _route.Stops.Count - 1)
            {
                var stop = _route.Stops[index];
                _route.Stops.RemoveAt(index);
                _route.Stops.Insert(index + 1, stop);
                RenumberStops();
            }
        }

        private void RenumberStops()
        {
            for (int i = 0; i < _route.Stops.Count; i++)
                _route.Stops[i].Sequence = i;
        }

        public IReadOnlyList<DeliveryRoute> GetFilteredRoutes(string nameFilter)
        {
            var list = _playerContext.GetCurrentPlayerRoutes();
            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(r => r.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            return list.AsReadOnly();
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_route.UUID))
            {
                _route.UUID = Guid.NewGuid().ToString();
                _playerContext.deliveryRouteList.Add(_route);
            }
            if (string.IsNullOrEmpty(_route.OwnerUUID))
            {
                _route.OwnerUUID = _playerContext.CurrentPlayerUUID;
            }
            _playerContext.writeContext();
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(_route.UUID)) return;
            _playerContext.deliveryRouteList.Remove(_route);
            _playerContext.writeContext();
        }

        public void Reset()
        {
            _route = new DeliveryRoute();
        }

        public void SelectRoute(DeliveryRoute route)
        {
            _route = route ?? new DeliveryRoute();
        }
    }
}
