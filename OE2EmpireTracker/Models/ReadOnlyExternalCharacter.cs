using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for ExternalCharacter. Exposes only getter properties.
    /// Does NOT expose: any setters or mutation methods.
    /// </summary>
    public class ReadOnlyExternalCharacter
    {
        private readonly ExternalCharacter _entity;

        public ReadOnlyExternalCharacter(ExternalCharacter entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string FactionUUID => _entity.FactionUUID;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyExternalCharacter other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
