using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for MarketTransaction. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyMarketTransaction
    {
        private readonly MarketTransaction _entity;

        public ReadOnlyMarketTransaction(MarketTransaction entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string OwnerUUID => _entity.OwnerUUID;
        public TransactionType TransactionType => _entity.TransactionType;
        public ItemType.ItemTypeEnum ItemType => _entity.ItemType;
        public string ItemReferenceID => _entity.ItemReferenceID;
        public string ItemName => _entity.ItemName;
        public int Quantity => _entity.Quantity;
        public decimal PricePerUnit => _entity.PricePerUnit;
        public decimal TotalPrice => _entity.TotalPrice;
        public string Counterparty => _entity.Counterparty;
        public string CounterpartyFaction => _entity.CounterpartyFaction;
        public string StationUUID => _entity.StationUUID;
        public string Timestamp => _entity.Timestamp;
        public string Notes => _entity.Notes;
        public string ListingUUID => _entity.ListingUUID;
        public int CurrentHP => _entity.CurrentHP;
        public int MaxHP => _entity.MaxHP;
        public decimal MaxRepairPercent => _entity.MaxRepairPercent;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyMarketTransaction other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ItemName;
    }
}
