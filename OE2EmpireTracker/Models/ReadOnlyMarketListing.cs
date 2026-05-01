using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for MarketListing. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyMarketListing
    {
        private readonly MarketListing _entity;

        public ReadOnlyMarketListing(MarketListing entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string OwnerUUID => _entity.OwnerUUID;
        public string StationUUID => _entity.StationUUID;
        public ItemType.ItemTypeEnum ItemType => _entity.ItemType;
        public string ItemReferenceID => _entity.ItemReferenceID;
        public string ItemName => _entity.ItemName;
        public int Quantity => _entity.Quantity;
        public decimal PricePerUnit => _entity.PricePerUnit;
        public int CurrentHP => _entity.CurrentHP;
        public int MaxHP => _entity.MaxHP;
        public decimal MaxRepairPercent => _entity.MaxRepairPercent;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyMarketListing other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ItemName;
    }
}
