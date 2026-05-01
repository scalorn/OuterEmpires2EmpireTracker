using System;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Read-only wrapper for PlayerProfile. Exposes only getter properties
    /// and read-only skill/rank access.
    /// Does NOT expose: any setters, SetSkillGroup, or mutation methods.
    /// </summary>
    public class ReadOnlyPlayerProfile
    {
        private readonly PlayerProfile _entity;

        public ReadOnlyPlayerProfile(PlayerProfile entity)
        {
            _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }

        public string UUID => _entity.UUID;
        public string Name => _entity.Name;
        public string FactionUUID => _entity.FactionUUID;

        // Rank properties - wrapped
        public ReadOnlyPlayerRank Public => new ReadOnlyPlayerRank(_entity.Public);
        public ReadOnlyPlayerRank Private => new ReadOnlyPlayerRank(_entity.Private);
        public ReadOnlyPlayerRank Military => new ReadOnlyPlayerRank(_entity.Military);

        // Skill access - returns read-only wrapper
        public ReadOnlyPlayerSkill GetSkill(string skillName) =>
            new ReadOnlyPlayerSkill(_entity.GetSkill(skillName));

        public ReadOnlyPlayerSkill GetSkill(SkillName skill) =>
            new ReadOnlyPlayerSkill(_entity.GetSkill(skill));

        // Skill group access - returns bool directly
        public bool GetSkillGroup(string skillGroup) =>
            _entity.GetSkillGroup(skillGroup);

        public bool GetSkillGroup(SkillGroupName group) =>
            _entity.GetSkillGroup(group);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPlayerProfile other)
                return string.Equals(_entity.UUID, other._entity.UUID);
            return false;
        }

        public override int GetHashCode() => _entity.UUID?.GetHashCode() ?? 0;

        public override string ToString() => _entity.Name;
    }
}
