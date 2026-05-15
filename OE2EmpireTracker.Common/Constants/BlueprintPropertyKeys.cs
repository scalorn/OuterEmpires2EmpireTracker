namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Canonical blueprint property key strings used in ship/station stat computation,
    /// cargo volume lookups, and manufacturing calculations.
    /// These match the game-defined property names from blueprint HTML.
    /// </summary>
    public static class BlueprintPropertyKeys
    {
        public const string Mass = "Mass";
        public const string PowerGenerated = "Power Generated";
        public const string PowerConsumed = "Power Consumed";
        public const string CargoCapacity = "Cargo Capacity";
        public const string FuelCapacity = "Fuel Capacity";
        public const string RawMaterialCapacity = "Raw Material Capacity";
        public const string Health = "Health";
        public const string EnergyDefence = "Energy Defence";
        public const string KineticDefence = "Kinetic Defence";
        public const string MissileDefence = "Missile Defence";
        public const string ShieldHitpoints = "Shield Hitpoints";
        public const string ShieldRegen = "Shield Regen";
        public const string Acceleration = "Acceleration";
        public const string RotationalThrust = "Rotational Thrust";
        public const string JumpDistance = "Jump Distance";
        public const string FuelPerJump = "Fuel Per Jump";
        public const string MiningYield = "Mining Yield";
        public const string MiningCycleTime = "Mining Cycle Time";
        public const string ScanLevel = "Scan Level";
        public const string CargoVolumeSize = "Cargo Volume Size";
        public const string ManufactureRunTime = "Manufacture Run Time";
        public const string AmountManufactured = "Amount Manufactured";
        public const string LicenseCareer = "License Career";
        public const string LicenseLevel = "License Level";

        // Enhanced stats — power model and derived computations
        public const string EngCapacityRequired = "Eng Capacity Required";
        public const string EngCapacityAvailable = "Eng Capacity Available";
        public const string PowerProvided = "Power Provided";
        public const string PowerRegenRate = "Power Regeneration Rate";
        public const string PowerDrawPerSecond = "Power Draw Per Second";
        public const string JumpChargeTime = "Jump Charge Time";
        public const string FuelPerJASPerMass = "Fuel Used / JAS / Mass";
    }
}
