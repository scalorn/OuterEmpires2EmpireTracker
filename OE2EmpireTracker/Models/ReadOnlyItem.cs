using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Item. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyItem
    {
        private readonly Item _entity;

        public ReadOnlyItem(Item entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public ItemType.ItemTypeEnum ItemType => _entity.ItemType;
        public string BaseItemTypeID => _entity.BaseItemTypeID;
        public string Name => _entity.Name;
        public string NickName => _entity.NickName;
        public string Description => _entity.Description;
        public int Quantity => _entity.Quantity;
        public string ResourcePurity => _entity.ResourcePurity;
        public decimal Volume => _entity.Volume;
        public int CurrentHP => _entity.CurrentHP;
        public int MaxHP => _entity.MaxHP;
        public decimal MaxRepairPercent => _entity.MaxRepairPercent;

        public ReadOnlyItemBag Contents => _entity.Contents != null ? new ReadOnlyItemBag(_entity.Contents) : null;

        public string ExtendedName => _entity.ExtendedName;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyItem other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ExtendedName;
    }
}
