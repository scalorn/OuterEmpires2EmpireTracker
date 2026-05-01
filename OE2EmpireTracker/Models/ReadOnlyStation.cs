using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Station. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyStation
    {
        private readonly Station _entity;

        public ReadOnlyStation(Station entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public StationType StationType => _entity.StationType;
        public StationOwnership Ownership => _entity.Ownership;
        public string OwnerUUID => _entity.OwnerUUID;
        public string StationBlueprintUUID => _entity.StationBlueprintUUID;
        public int HullCurrentHP => _entity.HullCurrentHP;
        public int HullMaxHP => _entity.HullMaxHP;
        public decimal HullMaxRepairPercent => _entity.HullMaxRepairPercent;

        public IReadOnlyDictionary<string, ReadOnlyItemBag> Holds =>
            _entity.Holds?.ToDictionary(kvp => kvp.Key, kvp => new ReadOnlyItemBag(kvp.Value));

        public IReadOnlyList<ReadOnlyShipComponentSlot> Components =>
            _entity.Components?.Select(c => new ReadOnlyShipComponentSlot(c)).ToList();

        public ReadOnlyItemBag MunitionsHold =>
            _entity.MunitionsHold != null ? new ReadOnlyItemBag(_entity.MunitionsHold) : null;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyStation other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
