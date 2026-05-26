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

        public int SkillId => _entity.SkillId;

        public string EffectDescription => _entity.EffectDescription;

        public int AmountPerLevel => _entity.AmountPerLevel;

        public string SkillGroupName => _entity.SkillGroupName;

        public bool IsUnlocked => _entity.IsUnlocked;

        public int TargetLevel => _entity.TargetLevel;

        public int TrainingPercentageComplete => _entity.TrainingPercentageComplete;

        public int RemainingMinutes => _entity.RemainingMinutes;

        /// <summary>
        /// Gets a value indicating whether training has been started for this skill.
        /// </summary>
        public bool TrainingStarted => _entity.TrainingStarted;

        /// <summary>
        /// Gets the number of seconds remaining until training completes.
        /// </summary>
        public long CompletionTimeRemaining => _entity.CompletionTime.TimeRemaining;

        /// <summary>
        /// Gets the human-readable countdown string for training completion.
        /// </summary>
        public string CompletionTimeRemainingString => _entity.CompletionTime.TimeRemainingString;

        /// <summary>
        /// Gets the time when training was started.
        /// </summary>
        public DateTime CompletionStartTime => _entity.CompletionTime.StartTime;

        /// <summary>
        /// Gets the time when training is expected to complete.
        /// </summary>
        public DateTime CompletionEndTime => _entity.CompletionTime.EndTime;

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
