using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for StockPlan. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyStockPlan
    {
        private readonly StockPlan _entity;

        public ReadOnlyStockPlan(StockPlan entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string ReplenishmentBuildPlanUUID => _entity.ReplenishmentBuildPlanUUID;
        public bool IsActive => _entity.IsActive;

        public IReadOnlyList<ReadOnlyStockTarget> Targets =>
            _entity.Targets.Select(t => new ReadOnlyStockTarget(t)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStockPlan other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
