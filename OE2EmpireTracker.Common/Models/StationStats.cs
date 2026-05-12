namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Computed stats for a player-owned station. Subset of ShipStats —
    /// stations have reactors, shields, weapons, hull plating, and thrusters
    /// but no drives, cargo pods, fuel tanks, jump drives, mining equipment,
    /// or scanners.
    /// </summary>
    public class StationStats
    {
        // Core
        public decimal TotalMass { get; set; }
        public decimal PowerGenerated { get; set; }
        public decimal PowerConsumed { get; set; }
        public decimal PowerBalance { get; set; }
        public decimal EngCapacityUsed { get; set; }

        // Defence
        public decimal TotalHealth { get; set; }
        public decimal EnergyDefence { get; set; }
        public decimal KineticDefence { get; set; }
        public decimal MissileDefence { get; set; }
        public decimal ShieldHitpoints { get; set; }
        public decimal ShieldRegen { get; set; }

        // Weapons summary
        public int SmallWeaponsInstalled { get; set; }
        public int MediumWeaponsInstalled { get; set; }
        public int LargeWeaponsInstalled { get; set; }

        // Slot usage
        public string SlotSummary { get; set; } = string.Empty;
    }
}
