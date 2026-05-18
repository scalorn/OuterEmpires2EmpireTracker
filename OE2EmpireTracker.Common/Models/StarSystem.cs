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
        public int Id { get; internal set; }

        [JsonProperty("n")]
        public string Name { get; internal set; } = string.Empty;

        [JsonProperty("x")]
        public decimal X { get; internal set; }

        [JsonProperty("y")]
        public decimal Y { get; internal set; }

        [JsonProperty("q")]
        public int Quadrant { get; internal set; }

        [JsonProperty("s")]
        public int Sector { get; internal set; }

        [JsonProperty("r")]
        public int Region { get; internal set; }

        [JsonProperty("l")]
        public int Locality { get; internal set; }

        [JsonProperty("st")]
        public string SpectralClass { get; internal set; } = string.Empty;

        // Mutable properties (editable by user)

        [JsonProperty("fid")]
        [DefaultValue(0)]
        public int FactionId { get; internal set; }

        [JsonProperty("fn")]
        [DefaultValue("")]
        public string FactionName { get; internal set; } = string.Empty;

        [JsonProperty("fc")]
        [DefaultValue("")]
        public string FactionColor { get; internal set; } = string.Empty;

        [JsonProperty("o")]
        [DefaultValue(false)]
        public bool HasOrbital { get; internal set; }

        [JsonProperty("sp")]
        [DefaultValue(false)]
        public bool HasSpaceport { get; internal set; }

        [JsonProperty("sb")]
        [DefaultValue(false)]
        public bool HasStarbase { get; internal set; }
    }
}
