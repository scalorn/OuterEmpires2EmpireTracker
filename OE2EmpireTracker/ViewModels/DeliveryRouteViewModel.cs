using System;
using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    public class DeliveryRouteViewModel
    {
        private readonly PlayerContext _playerContext;

        private DeliveryRoute _route;

        public DeliveryRouteViewModel(DeliveryRoute route, PlayerContext playerContext)
        {
            _route = route ?? throw new ArgumentNullException(nameof(route));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        public DeliveryRoute Data => _route;

        public string Name { get => _route.Name; set => _route.Name = value; }

        public string UUID => _route.UUID;

        public IReadOnlyList<RouteStop> Stops => _route.Stops.AsReadOnly();

        public void AddStop(
            string destinationUUID,
            DestinationType destType = DestinationType.Colony,
            RouteStopPurpose purpose = RouteStopPurpose.Cargo)
        {
            var stop = new RouteStop
            {
                ColonyUUID = destType == DestinationType.Colony ? destinationUUID : string.Empty,
                DestinationType = destType,
                DestinationUUID = destinationUUID,
                Purpose = purpose,
                Sequence = _route.Stops.Count
            };

            _route.Stops.Add(stop);
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

        /// <summary>
        /// Removes multiple stops by index. Processes in descending order to avoid index shifting.
        /// </summary>
        public void RemoveStops(IEnumerable<int> indices)
        {
            foreach (int i in indices.OrderByDescending(x => x))
            {
                if (i >= 0 && i < _route.Stops.Count)
                    _route.Stops.RemoveAt(i);
            }

            RenumberStops();
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

        /// <summary>
        /// Moves a contiguous or non-contiguous set of selected indices up by one.
        /// Processes in ascending order so earlier moves don't shift later indices.
        /// Returns the new indices of the moved items.
        /// </summary>
        public List<int> MoveStopsUp(IEnumerable<int> indices)
        {
            var sorted = indices.OrderBy(x => x).ToList();
            var newIndices = new List<int>();
            foreach (int i in sorted)
            {
                if (i > 0 && i < _route.Stops.Count && !newIndices.Contains(i - 1))
                {
                    var stop = _route.Stops[i];
                    _route.Stops.RemoveAt(i);
                    _route.Stops.Insert(i - 1, stop);
                    newIndices.Add(i - 1);
                }
                else
                {
                    newIndices.Add(i); // can't move, stays in place
                }
            }

            RenumberStops();
            return newIndices;
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

        /// <summary>
        /// Moves a set of selected indices down by one.
        /// Processes in descending order so later moves don't shift earlier indices.
        /// Returns the new indices of the moved items.
        /// </summary>
        public List<int> MoveStopsDown(IEnumerable<int> indices)
        {
            var sorted = indices.OrderByDescending(x => x).ToList();
            var newIndices = new List<int>();
            foreach (int i in sorted)
            {
                if (i >= 0 && i < _route.Stops.Count - 1 && !newIndices.Contains(i + 1))
                {
                    var stop = _route.Stops[i];
                    _route.Stops.RemoveAt(i);
                    _route.Stops.Insert(i + 1, stop);
                    newIndices.Add(i + 1);
                }
                else
                {
                    newIndices.Add(i);
                }
            }

            RenumberStops();
            return newIndices;
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
                _playerContext.AddDeliveryRoute(_route);
            }

            if (string.IsNullOrEmpty(_route.OwnerUUID))
            {
                _route.OwnerUUID = _playerContext.CurrentPlayerUUID;
            }

            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
        }

        public void Delete()
        {
            if (string.IsNullOrEmpty(_route.UUID)) return;
            _playerContext.RemoveDeliveryRoute(_route);
            _playerContext.WriteContext();
            _playerContext.OnDeliveryDataChanged();
        }

        public void Reset()
        {
            _route = new DeliveryRoute();
        }

        public void SelectRoute(DeliveryRoute route)
        {
            _route = route ?? new DeliveryRoute();
        }

        private void RenumberStops()
        {
            for (int i = 0; i < _route.Stops.Count; i++)
                _route.Stops[i].Sequence = i;
        }
    }
}
