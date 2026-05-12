using System.Collections.Generic;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Defines ship component slot type string constants and the mapping
    /// from hull blueprint properties to slot types.
    /// See design.md OQ-31 for the complete mapping table.
    /// </summary>
    public static class SlotTypes
    {
        // Core component slot types
        public const string Reactor = "Reactor";
        public const string MainDrive = "MainDrive";
        public const string Thruster = "Thruster";
        public const string JumpDrive = "JumpDrive";
        public const string NavComp = "NavComp";
        public const string Scanner = "Scanner";
        public const string Shield = "Shield";
        public const string CargoPod = "CargoPod";
        public const string FuelTank = "FuelTank";
        public const string Coupler = "Coupler";
        public const string GERTY = "GERTY";

        // Weapon slot types (size-encoded per OQ-32)
        public const string WeaponSmall = "WeaponSmall";
        public const string WeaponMedium = "WeaponMedium";
        public const string WeaponLarge = "WeaponLarge";

        // Hull modification slot types
        public const string HullPlating = "HullPlating";
        public const string HullReinforcement = "HullReinforcement";
        public const string HullSealant = "HullSealant";

        // Mining slot types
        public const string MiningLaser = "MiningLaser";
        public const string MiningGrapple = "MiningGrapple";
        public const string OreHopper = "OreHopper";

        /// <summary>
        /// Maps hull blueprint property names to their corresponding slot type strings.
        /// e.g. "Reactor Slots" on a hull → Reactor slot type.
        /// </summary>
        public static readonly Dictionary<string, string> HullPropertyToSlotType = new Dictionary<string, string>
        {
            { "Reactor Slots", Reactor },
            { "Main Drive Slots", MainDrive },
            { "Thruster Slots", Thruster },
            { "Jump Drive Slots", JumpDrive },
            { "Nav Comp Slots", NavComp },
            { "Scanner Slots", Scanner },
            { "Shield Slots", Shield },
            { "Cargo Pod Slots", CargoPod },
            { "Fuel Tank Slots", FuelTank },
            { "Coupler Slots", Coupler },
            { "GERTY Slots", GERTY },
            { "Small Weapon Mounts", WeaponSmall },
            { "Medium Weapon Mounts", WeaponMedium },
            { "Large Weapon Mounts", WeaponLarge },
            { "Max Hull Plating", HullPlating },
            { "Max Hull Reinforcement", HullReinforcement },
            { "Max Hull Sealant Units", HullSealant },
            { "Max Mining Lasers", MiningLaser },
            { "Max Mining Grapples", MiningGrapple },
            { "Max Ore Hoppers", OreHopper },
        };

        /// <summary>
        /// Maps BlueprintType IDs to their corresponding slot type strings.
        /// Used when installing a component to determine which slot type it occupies.
        /// </summary>
        public static readonly Dictionary<string, string> BlueprintTypeToSlotType = new Dictionary<string, string>
        {
            { "Reactor", Reactor },
            { "MainDrive", MainDrive },
            { "Thruster", Thruster },
            { "JumpDrive", JumpDrive },
            { "NavComp", NavComp },
            { "SystemObjectScanner", Scanner },
            { "Shield", Shield },
            { "CargoPod", CargoPod },
            { "FuelTank", FuelTank },
            { "UniversalCoupler", Coupler },
            { "GERTYDroneRack", GERTY },
            { "HullPlating", HullPlating },
            { "HullReinforcement", HullReinforcement },
            { "HullSealantInjectionUnit", HullSealant },
            { "MiningLaser", MiningLaser },
            { "AsteroidGrapple", MiningGrapple },
            { "OreHopper", OreHopper },
            // Weapon types — size suffix determines slot type
            { "Beamer/Small", WeaponSmall },
            { "Railgun/Small", WeaponSmall },
            { "CoilGun/Small", WeaponSmall },
            { "MissileLauncher/Small", WeaponSmall },
            { "TorpedoLauncher/Small", WeaponSmall },
            { "Beamer/Medium", WeaponMedium },
            { "Railgun/Medium", WeaponMedium },
            { "CoilGun/Medium", WeaponMedium },
            { "MissileLauncher/Medium", WeaponMedium },
            { "TorpedoLauncher/Medium", WeaponMedium },
            { "Beamer/Large", WeaponLarge },
            { "Railgun/Large", WeaponLarge },
            { "CoilGun/Large", WeaponLarge },
            { "MissileLauncher/Large", WeaponLarge },
            { "TorpedoLauncher/Large", WeaponLarge },
        };
    }
}
