namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Per-type weapon sustainability data for ship stats display.
    /// </summary>
    public class WeaponSustainEntry
    {
        /// <summary>Gets or sets the weapon blueprint type name.</summary>
        public string WeaponType { get; set; } = string.Empty;

        /// <summary>Gets or sets the power draw per second for this weapon type.</summary>
        public decimal PowerDrawPerSecond { get; set; }

        /// <summary>Gets or sets the number of this weapon type installed.</summary>
        public int Count { get; set; }

        /// <summary>Gets or sets the sustainable count (PowerRegenRate / PowerDrawPerSecond).</summary>
        public decimal SustainableCount { get; set; }
    }
}
