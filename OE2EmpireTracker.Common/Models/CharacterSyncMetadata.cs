using System.Collections.Generic;

namespace OE2EmpireTracker.Models
{
    public class CharacterSyncMetadata
    {
        public string CharacterUUID { get; set; } = string.Empty;

        public int SystemId { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public int RangeJas { get; set; }

        public HashSet<int> SystemsInRange { get; set; } = new HashSet<int>();

        public string LastSyncTimestamp { get; set; } = string.Empty;

        public List<string> GrantedScopes { get; set; } = new List<string>();
    }
}
