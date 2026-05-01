using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for StockTarget. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyStockTarget
    {
        private readonly StockTarget _entity;

        public ReadOnlyStockTarget(StockTarget entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public ItemType.ItemTypeEnum ItemType => _entity.ItemType;
        public string ItemReferenceID => _entity.ItemReferenceID;
        public string ItemName => _entity.ItemName;
        public string ShipTemplateUUID => _entity.ShipTemplateUUID;
        public int TargetQuantity => _entity.TargetQuantity;
        public int CriticalThreshold => _entity.CriticalThreshold;
        public StockTargetScope Scope => _entity.Scope;
        public string LocationUUID => _entity.LocationUUID;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStockTarget other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ItemName;
    }
}
