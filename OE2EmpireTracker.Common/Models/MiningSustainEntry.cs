namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Per-type mining laser sustainability data for ship stats display.
    /// </summary>
    public class MiningSustainEntry
    {
        /// <summary>Gets or sets the mining laser blueprint type name.</summary>
        public string LaserType { get; set; } = string.Empty;

        /// <summary>Gets or sets the power draw per second for this laser type.</summary>
        public decimal PowerDrawPerSecond { get; set; }

        /// <summary>Gets or sets the number of this laser type installed.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets the sustainable count (PowerRegenRate / PowerDrawPerSecond).</summary>
        public decimal SustainableCount { get; set; }
    }
}
