using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for Faction. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyFaction
    {
        private readonly Faction _entity;

        public ReadOnlyFaction(Faction entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string Description => _entity.Description;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyFaction other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
