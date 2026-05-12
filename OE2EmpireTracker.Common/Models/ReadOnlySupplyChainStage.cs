using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for SupplyChainStage. Exposes only getter properties.
    /// </summary>
    public class ReadOnlySupplyChainStage
    {
        private readonly SupplyChainStage _entity;

        public ReadOnlySupplyChainStage(SupplyChainStage entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int Sequence => _entity.Sequence;
        public SupplyChainStageType StageType => _entity.StageType;
        public DestinationType LocationType => _entity.LocationType;
        public string LocationUUID => _entity.LocationUUID;
        public string ResourceName => _entity.ResourceName;
        public string ResourcePurity => _entity.ResourcePurity;
        public int AccumulationThreshold => _entity.AccumulationThreshold;
        public decimal ProductionRatePerHour => _entity.ProductionRatePerHour;
        public string DeliveryRouteUUID => _entity.DeliveryRouteUUID;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlySupplyChainStage other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"Stage {_entity.Sequence}: {_entity.StageType}";
    }
}
