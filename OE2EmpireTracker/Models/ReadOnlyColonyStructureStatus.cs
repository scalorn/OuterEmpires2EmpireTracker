using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ColonyStructureStatus. Exposes only getter properties.
    /// Does NOT expose: SetUnallocatedPresent or any mutation methods.
    /// </summary>
    public class ReadOnlyColonyStructureStatus
    {
        private readonly ColonyStructureStatus _entity;

        public ReadOnlyColonyStructureStatus(ColonyStructureStatus entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public decimal PowerProvided => _entity.PowerProvided;
        public decimal PowerRequired => _entity.PowerRequired;
        public decimal HabitationProvision => _entity.HabitationProvision;
        public decimal HabitationRequired => _entity.HabitationRequired;
        public decimal FoodProvision => _entity.FoodProvision;
        public decimal FoodRequired => _entity.FoodRequired;
        public decimal EntertainmentProvided => _entity.EntertainmentProvided;
        public decimal EntertainmentRequired => _entity.EntertainmentRequired;
        public decimal WarehouseCapacity => _entity.WarehouseCapacity;
        public decimal WarehouseRequired => _entity.WarehouseRequired;
        public bool UnallocatedBlueCollarPresent => _entity.UnallocatedBlueCollarPresent;
        public bool UnallocatedWhiteCollarPresent => _entity.UnallocatedWhiteCollarPresent;
        public bool UnallocatedSpecialistPresent => _entity.UnallocatedSpecialistPresent;

        public bool GetUnallocatedPresent(string detailKey) => _entity.GetUnallocatedPresent(detailKey);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyColonyStructureStatus other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => _entity.ToString();
    }
}
