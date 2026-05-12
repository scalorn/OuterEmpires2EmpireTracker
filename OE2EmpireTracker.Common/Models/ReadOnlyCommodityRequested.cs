using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for CommodityRequested. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyCommodityRequested
    {
        private readonly CommodityRequested _entity;

        public ReadOnlyCommodityRequested(CommodityRequested entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string Name => _entity.Name;
        public int Requested => _entity.Requested;
        public int Delivered => _entity.Delivered;
        public DateTime NeedBy => _entity.NeedBy;
        public bool Fulfilled => _entity.Fulfilled;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyCommodityRequested other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.Name;
    }
}
