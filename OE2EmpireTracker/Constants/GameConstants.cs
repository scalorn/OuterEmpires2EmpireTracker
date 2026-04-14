using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Game-wide numeric and string constants used across the codebase.
    /// The five game-derived values read from EmpireContext.GameConstants
    /// (externalized to BaselineData.json) with hardcoded fallback defaults.
    /// </summary>
    public static class GameConstants
    {
        // --- Refining ---

        /// <summary>Base refining rate per cycle (units consumed from source).</summary>
        public static int RefiningBaseRate =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.RefiningBaseRate ?? 25;

        // --- Workers ---

        /// <summary>Cargo volume per worker detail item.</summary>
        public static decimal WorkerVolume =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.WorkerVolume ?? 50m;

        // --- Timers ---

        /// <summary>Seconds in one hour — used for top-of-hour timer alignment.</summary>
        public const long SecondsPerHour = 3600;

        // --- Commodity Manufacturing ---

        /// <summary>Number of commodities produced per cycle.</summary>
        public static int CommoditiesPerCycle =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.CommoditiesPerCycle ?? 10;

        /// <summary>Commodity manufacturing cycle time in seconds (10 minutes).</summary>
        public static long CommodityCycleSeconds =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.CommodityCycleSeconds ?? 600;

        // --- Structures ---

        /// <summary>Maximum structures per colony (game cap).</summary>
        public static int StructureCap =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.StructureCap ?? 65;

        // --- Structure Property Keys ---

        /// <summary>Property key indicating a structure has been built.</summary>
        public const string PropBuilt = "Built";

        /// <summary>Property key indicating a structure is staged for construction.</summary>
        public const string PropStaged = "Staged";

        /// <summary>Property key indicating a structure is online and operational.</summary>
        public const string PropOnline = "Online";

        // --- Status Dictionary Keys ---

        /// <summary>Key for the actual (real worker state) status in ColonyStructure.Statuses.</summary>
        public const string StatusActual = "Actual";

        /// <summary>Key for the ideal (all workers assigned) status in ColonyStructure.Statuses.</summary>
        public const string StatusIdeal = "Ideal";

        // --- Resource Purity ---

        /// <summary>Purity value for refined resources.</summary>
        public const string PurityRefined = "Refined";

        // --- Blueprint Property Keys ---
        // Canonical names with spaces, matching BaselineData.json type definitions
        // and BlueprintPropertyValidation. All code should use these constants
        // instead of hardcoded strings.

        public const string PropPowerProvided = "Power Provided";
        public const string PropPowerRequired = "Power Required";
        public const string PropHabitationProvision = "Habitation Provision";
        public const string PropFoodProvision = "Food Provision";
        public const string PropEntertainmentProvided = "Entertainment Provided";
        public const string PropWarehouseCapacity = "Warehouse Capacity";
        public const string PropMaxPerColony = "Max Per Colony";

        // Worker detail property keys (blueprint properties, with spaces)
        public const string PropBlueCollarDetail = "Blue Collar Detail";
        public const string PropWhiteCollarDetail = "White Collar Detail";
        public const string PropSpecialistDetail = "Specialist Detail";
        public const string PropUnassignedBlueCollarDetail = "Unassigned Blue Collar Detail";
        public const string PropUnassignedWhiteCollarDetail = "Unassigned White Collar Detail";
        public const string PropUnassignedSpecialistDetail = "Unassigned Specialist Detail";

        // Worker detail item type IDs (no spaces, used as BaseItemTypeID for WorkDetail items)
        public const string WorkerIdBlueCollar = "BlueCollarDetail";
        public const string WorkerIdWhiteCollar = "WhiteCollarDetail";
        public const string WorkerIdSpecialist = "SpecialistDetail";
    }
}
