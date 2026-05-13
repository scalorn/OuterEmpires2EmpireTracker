using System.ComponentModel;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Represents a star system in the galaxy with coordinates, grid location,
    /// spectral class, faction ownership, and infrastructure flags.
    /// </summary>
    public class StarSystem
    {
        // Immutable properties (set at import, never changed)

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("n")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("x")]
        public decimal X { get; set; }

        [JsonProperty("y")]
        public decimal Y { get; set; }

        [JsonProperty("q")]
        public int Quadrant { get; set; }

        [JsonProperty("s")]
        public int Sector { get; set; }

        [JsonProperty("r")]
        public int Region { get; set; }

        [JsonProperty("l")]
        public int Locality { get; set; }

        [JsonProperty("st")]
        public string SpectralClass { get; set; } = string.Empty;

        // Mutable properties (editable by user)

        [JsonProperty("fid"), DefaultValue(0)]
        public int FactionId { get; set; }

        [JsonProperty("fn"), DefaultValue("")]
        public string FactionName { get; set; } = string.Empty;

        [JsonProperty("fc"), DefaultValue("")]
        public string FactionColor { get; set; } = string.Empty;

        [JsonProperty("o"), DefaultValue(false)]
        public bool HasOrbital { get; set; }

        [JsonProperty("sp"), DefaultValue(false)]
        public bool HasSpaceport { get; set; }

        [JsonProperty("sb"), DefaultValue(false)]
        public bool HasStarbase { get; set; }
    }
}
