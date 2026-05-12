namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Computed stats for a ship, derived from hull blueprint + installed components.
    /// Properties that don't apply to a given ship (e.g. mining stats on a combat ship) are zero.
    /// </summary>
    public class ShipStats
    {
        // Core
        public decimal TotalMass { get; set; }
        public decimal PowerGenerated { get; set; }
        public decimal PowerConsumed { get; set; }
        public decimal PowerBalance { get; set; }
        public decimal EngCapacityUsed { get; set; }
        public decimal EngCapacityAvailable { get; set; }

        // Capacity
        public decimal CargoCapacity { get; set; }
        public decimal FuelCapacity { get; set; }
        public decimal HopperCapacity { get; set; }
        public int CrewSupported { get; set; }

        // Defence
        public decimal TotalHealth { get; set; }
        public decimal EnergyDefence { get; set; }
        public decimal KineticDefence { get; set; }
        public decimal MissileDefence { get; set; }
        public decimal ShieldHitpoints { get; set; }
        public decimal ShieldRegen { get; set; }

        // Propulsion
        public decimal Acceleration { get; set; }
        public decimal RotationalThrust { get; set; }
        public decimal MaxJumpDistance { get; set; }
        public decimal FuelPerJump { get; set; }

        // Mining
        public decimal MiningYield { get; set; }
        public decimal MiningCycleTime { get; set; }
        public decimal MiningYieldIncrease { get; set; }

        // Scanning
        public int ScanLevel { get; set; }
        public decimal SensorAbundanceFactor { get; set; }
        public decimal PurityModifier { get; set; }

        // Weapons summary
        public int SmallWeaponsInstalled { get; set; }
        public int MediumWeaponsInstalled { get; set; }
        public int LargeWeaponsInstalled { get; set; }

        // Slot usage
        public string SlotSummary { get; set; } = string.Empty;

        // Hull identity
        public string LicenseCareer { get; set; } = string.Empty;
        public int LicenseLevel { get; set; }
    }
}
