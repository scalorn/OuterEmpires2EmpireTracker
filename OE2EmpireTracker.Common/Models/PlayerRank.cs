using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    [JsonConverter(typeof(PlayerRankJsonConverter))]
    public class PlayerRank
    {
        public int Rank { get; set; } = 0;

        [JsonProperty("CurrentXp")]
        public long CurrentXp { get; set; } = 0;

        [JsonProperty("XpToNextLevel")]
        public long XpToNextLevel { get; set; } = 0;

        [JsonProperty("RankName")]
        public string RankName { get; set; } = string.Empty;
    }
}
