using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for PlayerSkill. Exposes only getter properties.
    /// Does NOT expose: TrainingStarted setter, CompletionTime setter.
    /// </summary>
    public class ReadOnlyPlayerSkill
    {
        private readonly PlayerSkill _entity;

        public ReadOnlyPlayerSkill(PlayerSkill entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public int Level => _entity.Level;

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPlayerSkill other)
                return ReferenceEquals(_entity, other._entity);
            return false;
        }

        public override int GetHashCode() => _entity.GetHashCode();

        public override string ToString() => $"Level {_entity.Level}";
    }
}
