using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Commodity. Exposes only getter properties.
    /// Does NOT expose: any setters, static members, or mutation methods.
    /// </summary>
    public class ReadOnlyCommodity
    {
        private readonly Commodity _entity;

        public ReadOnlyCommodity(Commodity entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Name => _entity.Name;
        public string ExtendedName => _entity.ExtendedName;
        public CommodityIndustry.CommodityIndustryEnum Industry => _entity.CommodityIndustry;
        public CommodityGroup.CommodityGroupEnum Group => _entity.CommodityGroup;

        public IReadOnlyDictionary<string, string> ConstructionResources => _entity.ConstructionResources;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyCommodity other)
                return string.Equals(_entity.Name, other._entity.Name);
            return false;
        }

        public override int GetHashCode() => _entity.Name?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ExtendedName ?? _entity.Name;
    }
}
