using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Asteroid. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyAsteroid
    {
        private readonly Asteroid _entity;

        public ReadOnlyAsteroid(Asteroid entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string SystemName => _entity.SystemName;

        public IReadOnlyList<ReadOnlyAsteroidReserve> Reserves =>
            _entity.Reserves.Select(r => new ReadOnlyAsteroidReserve(r)).ToList();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyAsteroid other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
