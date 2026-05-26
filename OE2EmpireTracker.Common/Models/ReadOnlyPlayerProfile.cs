using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public string Faction => _entity.Faction;

        public string FactionUUID => _entity.FactionUUID;

        public decimal TotalCredits => _entity.TotalCredits;

        public int SkillPoints => _entity.SkillPoints;

        public string CitizenId => _entity.CitizenId;

        public string RegistrationDate => _entity.RegistrationDate;

        public string ActiveTime => _entity.ActiveTime;

        public int CharacterId => _entity.CharacterId;

        public string FirstName => _entity.FirstName;

        public string LastName => _entity.LastName;

        public int ActiveTimeMinutes => _entity.ActiveTimeMinutes;

        // Rank properties - wrapped
        public ReadOnlyPlayerRank Public => new ReadOnlyPlayerRank(_entity.Public);
        public ReadOnlyPlayerRank Private => new ReadOnlyPlayerRank(_entity.Private);
        public ReadOnlyPlayerRank Military => new ReadOnlyPlayerRank(_entity.Military);

        // Skills dictionary - wrapped as IReadOnlyDictionary
        public IReadOnlyDictionary<string, ReadOnlyPlayerSkill> Skills =>
            new ReadOnlyDictionary<string, ReadOnlyPlayerSkill>(
                _entity.Skills.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ReadOnlyPlayerSkill(kvp.Value)));

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
