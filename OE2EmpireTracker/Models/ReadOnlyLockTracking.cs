using System;
using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for LockTracking. Exposes only query methods.
    /// Does NOT expose: LockItem, LockItems, ClearLocksForProcess, RawLocks.
    /// </summary>
    public class ReadOnlyLockTracking
    {
        private readonly LockTracking _entity;

        public ReadOnlyLockTracking(LockTracking entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int GetLockedQuantity(ItemType.ItemTypeEnum itemType, string baseItemTypeID)
            => _entity.GetLockedQuantity(itemType, baseItemTypeID);

        public IReadOnlyList<ItemLock> GetLocksForProcess(string processUUID)
            => _entity.GetLocksForProcess(processUUID);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyLockTracking other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
