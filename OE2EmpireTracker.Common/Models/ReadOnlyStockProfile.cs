using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for StockProfile. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyStockProfile
    {
        private readonly StockProfile _entity;

        public ReadOnlyStockProfile(StockProfile entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public bool IsActive => _entity.IsActive;

        public IReadOnlyList<ReadOnlyStockProfileEntry> Entries =>
            _entity.Entries.Select(e => new ReadOnlyStockProfileEntry(e)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStockProfile other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
