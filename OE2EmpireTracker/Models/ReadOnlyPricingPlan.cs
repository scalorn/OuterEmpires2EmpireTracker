using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for PricingPlan. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyPricingPlan
    {
        private readonly PricingPlan _entity;

        public ReadOnlyPricingPlan(PricingPlan entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string Description => _entity.Description;
        public decimal FixedCostPerItem => _entity.FixedCostPerItem;
        public decimal HourlyCostRate => _entity.HourlyCostRate;

        public IReadOnlyDictionary<string, decimal> ResourcePrices => _entity.ResourcePrices;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPricingPlan other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
