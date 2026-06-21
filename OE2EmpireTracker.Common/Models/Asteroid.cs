using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    public class Asteroid
    {
        public string UUID { get; set; }
        public string OwnerUUID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public List<AsteroidReserve> Reserves { get; set; } = new List<AsteroidReserve>();

        [JsonProperty("SystemObjectId")]
        [DefaultValue(0)]
        public int SystemObjectId { get; set; }
    }

    public class AsteroidReserve
    {
        public string ResourceName { get; set; } = string.Empty;
        public string Purity { get; set; } = string.Empty;
        public int MaxReserve { get; set; } = 0;
        public int CurrentReserve { get; set; } = 0;
        public string ResetTimestamp { get; set; } = string.Empty;
    }
}
