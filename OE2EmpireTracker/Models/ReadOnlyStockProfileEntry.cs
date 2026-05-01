using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for StockProfileEntry. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyStockProfileEntry
    {
        private readonly StockProfileEntry _entity;

        public ReadOnlyStockProfileEntry(StockProfileEntry entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string GroupID => _entity.GroupID;
        public string StockPlanUUID => _entity.StockPlanUUID;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStockProfileEntry other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.GroupID;
    }
}
