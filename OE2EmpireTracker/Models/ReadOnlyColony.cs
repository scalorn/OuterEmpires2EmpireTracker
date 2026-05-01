using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Colony. Exposes only getter properties.
    /// Does NOT expose: any setters, mutation methods, ProcessColony, or ColonyLock.
    /// </summary>
    public class ReadOnlyColony
    {
        private readonly Colony _entity;

        public ReadOnlyColony(Colony entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string OwnerUUID => _entity.OwnerUUID;
        public string LegacyUUID => _entity.LegacyUUID;
        public string PlanetName => _entity.PlanetName;
        public string SystemName => _entity.SystemName;
        public string ColonyName => _entity.ColonyName;
        public string LastImportDateTime => _entity.LastImportDateTime;

        /// <summary>
        /// Convenience property returning ColonyName for display purposes.
        /// </summary>
        public string Name => _entity.ColonyName;

        // Nested containers - wrapped
        public ReadOnlyItemBag Items => new ReadOnlyItemBag(_entity.Items);

        public IReadOnlyList<ReadOnlyColonyStructure> Structures =>
            _entity.Structures.Select(s => new ReadOnlyColonyStructure(s)).ToList();

        public IReadOnlyList<ReadOnlyCommodityRequested> Commodities =>
            _entity.Commodities.Select(c => new ReadOnlyCommodityRequested(c)).ToList();

        public ReadOnlyLockTracking Locks => new ReadOnlyLockTracking(_entity.Locks);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyColony other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.ColonyName;
    }
}
