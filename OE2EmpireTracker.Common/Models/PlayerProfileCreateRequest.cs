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

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Faction { get; set; }

        public decimal TotalCredits { get; set; }

        public int SkillPoints { get; set; }

        public string CitizenId { get; set; }

        public string RegistrationDate { get; set; }

        public string ActiveTime { get; set; }

        public int PublicRank { get; set; }

        public long PublicCurrentXp { get; set; }

        public long PublicXpToNextLevel { get; set; }

        public string PublicRankName { get; set; }

        public int PrivateRank { get; set; }

        public long PrivateCurrentXp { get; set; }

        public long PrivateXpToNextLevel { get; set; }

        public string PrivateRankName { get; set; }

        public int MilitaryRank { get; set; }

        public long MilitaryCurrentXp { get; set; }

        public long MilitaryXpToNextLevel { get; set; }

        public string MilitaryRankName { get; set; }

        public Dictionary<string, SkillUpdateData> Skills { get; set; }

        public Dictionary<string, bool> SkillGroups { get; set; }
    }
}
