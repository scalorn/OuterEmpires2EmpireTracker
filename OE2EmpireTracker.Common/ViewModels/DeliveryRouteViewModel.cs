using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class DeliveryRouteViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyDeliveryRoute _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private List<RouteStop> _stops = new List<RouteStop>();

        public string UUID => _uuid;

        public string OwnerUUID => _ownerUUID;

        public ReadOnlyDeliveryRoute Original => _original;

        public bool IsNew => _original == null;

        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name) || _stops.Count > 0;
                }

                if (_name != (_original.Name ?? string.Empty))
                {
                    return true;
                }

                var originalStops = _original.Stops;
                if (_stops.Count != originalStops.Count)
                {
                    return true;
                }

                for (int i = 0; i < _stops.Count; i++)
                {
                    var local = _stops[i];
                    var orig = originalStops[i];
                    if (local.ColonyUUID != orig.ColonyUUID)
                    {
                        return true;
                    }

                    if (local.Sequence != orig.Sequence)
                    {
                        return true;
                    }

                    if (local.DestinationType != orig.DestinationType)
                    {
                        return true;
                    }

                    if (local.DestinationUUID != orig.DestinationUUID)
                    {
                        return true;
                    }

                    if (local.Purpose != orig.Purpose)
                    {
                        return true;
                    }

                    if (local.FuelEstimate != orig.FuelEstimate)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public List<RouteStop> Stops => _stops;

        public void LoadFrom(ReadOnlyDeliveryRoute ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _stops = new List<RouteStop>();
            foreach (var s in ro.Stops)
            {
                _stops.Add(new RouteStop
                {
                    ColonyUUID = s.ColonyUUID ?? string.Empty,
                    Sequence = s.Sequence,
                    DestinationType = s.DestinationType,
                    DestinationUUID = s.DestinationUUID ?? string.Empty,
                    Purpose = s.Purpose,
                    FuelEstimate = s.FuelEstimate,
                });
            }
        }

        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _stops = new List<RouteStop>();
        }

        public DeliveryRouteUpdateRequest BuildUpdateRequest()
        {
            return new DeliveryRouteUpdateRequest
            {
                Original = _original,
                Name = _name,
                Stops = DeepCopyStops(_stops),
            };
        }

        public DeliveryRouteCreateRequest BuildCreateRequest()
        {
            return new DeliveryRouteCreateRequest
            {
                Name = _name,
                Stops = DeepCopyStops(_stops),
            };
        }

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
                Sequence = _stops.Count,
            };

            _stops.Add(stop);
            RenumberStops();
        }

        public void RemoveStop(int index)
        {
            if (index >= 0 && index < _stops.Count)
            {
                _stops.RemoveAt(index);
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
                if (i >= 0 && i < _stops.Count)
                {
                    _stops.RemoveAt(i);
                }
            }

            RenumberStops();
        }

        public void MoveStopUp(int index)
        {
            if (index > 0 && index < _stops.Count)
            {
                var stop = _stops[index];
                _stops.RemoveAt(index);
                _stops.Insert(index - 1, stop);
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
                if (i > 0 && i < _stops.Count && !newIndices.Contains(i - 1))
                {
                    var stop = _stops[i];
                    _stops.RemoveAt(i);
                    _stops.Insert(i - 1, stop);
                    newIndices.Add(i - 1);
                }
                else
                {
                    newIndices.Add(i);
                }
            }

            RenumberStops();
            return newIndices;
        }

        public void MoveStopDown(int index)
        {
            if (index >= 0 && index < _stops.Count - 1)
            {
                var stop = _stops[index];
                _stops.RemoveAt(index);
                _stops.Insert(index + 1, stop);
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
                if (i >= 0 && i < _stops.Count - 1 && !newIndices.Contains(i + 1))
                {
                    var stop = _stops[i];
                    _stops.RemoveAt(i);
                    _stops.Insert(i + 1, stop);
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

        private static List<RouteStop> DeepCopyStops(List<RouteStop> source)
        {
            var copy = new List<RouteStop>(source.Count);
            foreach (var s in source)
            {
                copy.Add(new RouteStop
                {
                    ColonyUUID = s.ColonyUUID ?? string.Empty,
                    Sequence = s.Sequence,
                    DestinationType = s.DestinationType,
                    DestinationUUID = s.DestinationUUID ?? string.Empty,
                    Purpose = s.Purpose,
                    FuelEstimate = s.FuelEstimate,
                });
            }

            return copy;
        }

        private void RenumberStops()
        {
            for (int i = 0; i < _stops.Count; i++)
            {
                _stops[i].Sequence = i;
            }
        }
    }
}
