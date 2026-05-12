using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO for creating a new player profile.
    /// No Original snapshot (it does not exist yet). No UUID (the service assigns it).
    /// </summary>
    public class PlayerProfileCreateRequest
    {
        public string Name { get; set; }

        public string Faction { get; set; }

        public decimal TotalCredits { get; set; }

        public int SkillPoints { get; set; }

        public string CitizenId { get; set; }

        public string RegistrationDate { get; set; }

        public string ActiveTime { get; set; }

        public int PublicRank { get; set; }

        public long PublicCurrentXP { get; set; }

        public long PublicNextXP { get; set; }

        public string PublicTitle { get; set; }

        public int PrivateRank { get; set; }

        public long PrivateCurrentXP { get; set; }

        public long PrivateNextXP { get; set; }

        public string PrivateTitle { get; set; }

        public int MilitaryRank { get; set; }

        public long MilitaryCurrentXP { get; set; }

        public long MilitaryNextXP { get; set; }

        public string MilitaryTitle { get; set; }

        public Dictionary<string, SkillUpdateData> Skills { get; set; }

        public Dictionary<string, bool> SkillGroups { get; set; }
    }
}
