using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Data
{
    public class PlayerProfile
    {
        public string UUID { get; set; } = string.Empty;
        public string Name { get; set; } =  string.Empty;
        public string Faction { get; set; } = string.Empty;
        public Decimal TotalCredits { get; set; } = new Decimal(0);

        public PlayerRank Public { get; set; } = new PlayerRank();
        public PlayerRank Private { get; set; } = new PlayerRank();
        public PlayerRank Military { get; set; } = new PlayerRank();
        public int SkillPoints { get; set; } = 0;

        private Dictionary<string, bool> SkillGroups { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, PlayerSkill> Skills { get; set; } = new Dictionary<string, PlayerSkill>();

        public PlayerProfile()
        {
        }

        public PlayerSkill GetSkill(string skillName)
        {
            if (!Skills.ContainsKey(skillName))
            {
                Skills[skillName] = new PlayerSkill();
            }
            return Skills[skillName];
        }
        public bool GetSkillGroup(string skillGroup)
        {
            if (!SkillGroups.ContainsKey(skillGroup))
            {
                SkillGroups[skillGroup] = false;
            }
            return SkillGroups[skillGroup];
        }
        public void SetSkillGroup(string skillGroup, bool value)
        {
            SkillGroups[skillGroup] = value;
        }
    }
}
