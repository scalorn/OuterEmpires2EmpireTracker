using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for AsteroidReserve. Exposes only getter properties.
    /// </summary>
    public class ReadOnlyAsteroidReserve
    {
        private readonly AsteroidReserve _entity;

        public ReadOnlyAsteroidReserve(AsteroidReserve entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string ResourceName => _entity.ResourceName;
        public string Purity => _entity.Purity;
        public int MaxReserve => _entity.MaxReserve;
        public int CurrentReserve => _entity.CurrentReserve;
        public string ResetTimestamp => _entity.ResetTimestamp;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyAsteroidReserve other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"{_entity.ResourceName} ({_entity.Purity})";
    }
}
