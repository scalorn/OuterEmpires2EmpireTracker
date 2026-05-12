using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for RouteStop. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyRouteStop
    {
        private readonly RouteStop _entity;

        public ReadOnlyRouteStop(RouteStop entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string ColonyUUID => _entity.ColonyUUID;
        public int Sequence => _entity.Sequence;
        public DestinationType DestinationType => _entity.DestinationType;
        public string DestinationUUID => _entity.DestinationUUID;
        public RouteStopPurpose Purpose => _entity.Purpose;
        public decimal FuelEstimate => _entity.FuelEstimate;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyRouteStop other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"Stop {_entity.Sequence}";
    }
}
