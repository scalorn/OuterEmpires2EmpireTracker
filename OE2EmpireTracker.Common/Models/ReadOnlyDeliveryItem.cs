using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for DeliveryItem. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyDeliveryItem
    {
        private readonly DeliveryItem _entity;

        public ReadOnlyDeliveryItem(DeliveryItem entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public ItemType.ItemTypeEnum ItemType => _entity.ItemType;
        public string BaseItemTypeID => _entity.BaseItemTypeID;
        public string Name => _entity.Name;
        public string ResourcePurity => _entity.ResourcePurity;
        public int Quantity => _entity.Quantity;
        public bool Delivered => _entity.Delivered;
        public string ExtendedName => _entity.ExtendedName;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyDeliveryItem other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ExtendedName;
    }
}
