using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Ship. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyShip
    {
        private readonly Ship _entity;

        public ReadOnlyShip(Ship entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string OwnerUUID => _entity.OwnerUUID;
        public string TemplateUUID => _entity.TemplateUUID;
        public string HullBlueprintUUID => _entity.HullBlueprintUUID;

        public IReadOnlyList<ReadOnlyShipComponentSlot> Components =>
            _entity.Components.Select(c => new ReadOnlyShipComponentSlot(c)).ToList();

        public DestinationType LocationType => _entity.LocationType;
        public string LocationUUID => _entity.LocationUUID;

        public ReadOnlyItemBag Cargo => new ReadOnlyItemBag(_entity.Cargo);
        public ReadOnlyItemBag Hopper => new ReadOnlyItemBag(_entity.Hopper);

        public int HullCurrentHP => _entity.HullCurrentHP;
        public int HullMaxHP => _entity.HullMaxHP;
        public decimal HullMaxRepairPercent => _entity.HullMaxRepairPercent;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyShip other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
