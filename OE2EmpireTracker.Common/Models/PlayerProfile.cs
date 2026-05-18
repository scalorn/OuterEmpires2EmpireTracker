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

        public string UUID { get; internal set; } = string.Empty;

        public string Name { get; internal set; } =  string.Empty;

        public string Faction { get; internal set; } = string.Empty;

        public string FactionUUID { get; internal set; } = string.Empty;

        public decimal TotalCredits { get; internal set; } = new decimal(0);

        public PlayerRank Public { get; internal set; } = new PlayerRank();

        public PlayerRank Private { get; internal set; } = new PlayerRank();

        public PlayerRank Military { get; internal set; } = new PlayerRank();

        public int SkillPoints { get; internal set; } = 0;

        public string CitizenId { get; internal set; } = string.Empty;

        public string RegistrationDate { get; internal set; } = string.Empty;

        public string ActiveTime { get; internal set; } = string.Empty;

        public Dictionary<string, PlayerSkill> Skills { get; internal set; } = new Dictionary<string, PlayerSkill>();

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
