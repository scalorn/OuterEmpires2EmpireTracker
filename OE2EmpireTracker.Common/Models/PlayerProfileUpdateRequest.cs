using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// DTO carrying the original snapshot and the current local state for updating an existing player profile.
    /// The service can diff against the original for field-level change detection if needed.
    /// </summary>
    public class PlayerProfileUpdateRequest
    {
        /// <summary>
        /// The original snapshot the edit was based on.
        /// Enables field-level dirty detection and optimistic concurrency.
        /// </summary>
        public ReadOnlyPlayerProfile Original { get; set; }

        public string Name { get; set; }

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
