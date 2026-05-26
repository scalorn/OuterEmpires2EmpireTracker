using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OE2EmpireTracker.Models
{
    public class PlayerProfile
    {
        public PlayerProfile()
        {
        }

        public string UUID { get; set; } = string.Empty;

        public string Name { get; set; } =  string.Empty;

        public string Faction { get; set; } = string.Empty;

        public string FactionUUID { get; set; } = string.Empty;

        public decimal TotalCredits { get; set; } = new decimal(0);

        public PlayerRank Public { get; set; } = new PlayerRank();

        public PlayerRank Private { get; set; } = new PlayerRank();

        public PlayerRank Military { get; set; } = new PlayerRank();

        public int SkillPoints { get; set; } = 0;

        public string CitizenId { get; set; } = string.Empty;

        public string RegistrationDate { get; set; } = string.Empty;

        public string ActiveTime { get; set; } = string.Empty;

        public int CharacterId { get; set; } = 0;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int ActiveTimeMinutes { get; set; } = 0;

        public Dictionary<string, PlayerSkill> Skills { get; set; } = new Dictionary<string, PlayerSkill>();

        private Dictionary<string, bool> SkillGroups { get; set; } = new Dictionary<string, bool>();

        public PlayerSkill GetSkill(string skillName)
        {
            if (!Skills.ContainsKey(skillName))
            {
                Skills[skillName] = new PlayerSkill();
            }

            return Skills[skillName];
        }

        public PlayerSkill GetSkill(SkillName skill) => GetSkill(skill.ToDisplayName());

        public bool GetSkillGroup(string skillGroup)
        {
            if (!SkillGroups.ContainsKey(skillGroup))
            {
                SkillGroups[skillGroup] = false;
            }

            return SkillGroups[skillGroup];
        }

        public bool GetSkillGroup(SkillGroupName group) => GetSkillGroup(group.ToDisplayName());

        public void SetSkillGroup(string skillGroup, bool value)
        {
            SkillGroups[skillGroup] = value;
        }

        public void SetSkillGroup(SkillGroupName group, bool value) => SetSkillGroup(group.ToDisplayName(), value);
    }
}
