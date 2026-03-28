using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Data
{
    public class PlayerProfile
    {
        public string Name { get; set; }
        public string Faction { get; set; }
        public Decimal TotalCredits { get; set; }

        public PlayerRank Public { get; set; } = new PlayerRank();
        public PlayerRank Private { get; set; } = new PlayerRank();
        public PlayerRank Military { get; set; } = new PlayerRank();

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
    }
}
