using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for WarehouseOverflowRule. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyWarehouseOverflowRule
    {
        private readonly WarehouseOverflowRule _entity;

        public ReadOnlyWarehouseOverflowRule(WarehouseOverflowRule entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string OwnerUUID => _entity.OwnerUUID;
        public bool IsActive => _entity.IsActive;
        public string ColonyUUID => _entity.ColonyUUID;
        public string ResourceName => _entity.ResourceName;
        public string ResourcePurity => _entity.ResourcePurity;
        public int TriggerThreshold => _entity.TriggerThreshold;
        public DestinationType DestinationType => _entity.DestinationType;
        public string DestinationUUID => _entity.DestinationUUID;
        public string DeliveryRouteUUID => _entity.DeliveryRouteUUID;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyWarehouseOverflowRule other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"{_entity.ResourceName} overflow rule";
    }
}
