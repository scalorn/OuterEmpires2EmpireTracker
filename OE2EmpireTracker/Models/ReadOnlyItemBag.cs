using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ItemBag. Exposes only query methods.
    /// Does NOT expose: AddItem, Remove, Clear, Items dictionary.
    /// </summary>
    public class ReadOnlyItemBag
    {
        private readonly ItemBag _entity;

        public ReadOnlyItemBag(ItemBag entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int Count() => _entity.Count();

        public bool ContainsKey(string uuid) => _entity.ContainsKey(uuid);

        public int CountByType(ItemType.ItemTypeEnum itemType, string baseItemTypeID)
            => _entity.CountByType(itemType, baseItemTypeID);

        public IReadOnlyList<ReadOnlyItem> FindByType(ItemType.ItemTypeEnum itemType, string baseItemTypeID)
            => _entity.FindByType(itemType, baseItemTypeID)
                .Select(i => new ReadOnlyItem(i))
                .ToList();

        public IReadOnlyList<ReadOnlyItem> FindResource(string resource, string purity)
            => _entity.FindResource(resource, purity)
                .Select(i => new ReadOnlyItem(i))
                .ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyItemBag other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
