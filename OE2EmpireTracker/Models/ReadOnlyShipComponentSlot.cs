using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ShipComponentSlot. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyShipComponentSlot
    {
        private readonly ShipComponentSlot _entity;

        public ReadOnlyShipComponentSlot(ShipComponentSlot entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string SlotType => _entity.SlotType;
        public int SlotIndex => _entity.SlotIndex;
        public string BlueprintUUID => _entity.BlueprintUUID;
        public int CurrentHP => _entity.CurrentHP;
        public int MaxHP => _entity.MaxHP;
        public decimal MaxRepairPercent => _entity.MaxRepairPercent;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyShipComponentSlot other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"{_entity.SlotType} [{_entity.SlotIndex}]";
    }
}
