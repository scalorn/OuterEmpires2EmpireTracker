using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ShipTemplate. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyShipTemplate
    {
        private readonly ShipTemplate _entity;

        public ReadOnlyShipTemplate(ShipTemplate entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string HullBlueprintUUID => _entity.HullBlueprintUUID;

        public IReadOnlyList<ReadOnlyShipComponentSlot> Components =>
            _entity.Components.Select(c => new ReadOnlyShipComponentSlot(c)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyShipTemplate other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
