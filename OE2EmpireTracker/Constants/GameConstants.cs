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
            EmpireContext.getInstanceIfLoaded()?.GameConstants?.RefiningBaseRate ?? 25;

        // --- Workers ---

        /// <summary>Cargo volume per worker detail item.</summary>
        public static decimal WorkerVolume =>
            EmpireContext.getInstanceIfLoaded()?.GameConstants?.WorkerVolume ?? 50m;

        // --- Timers ---

        /// <summary>Seconds in one hour — used for top-of-hour timer alignment.</summary>
        public const long SecondsPerHour = 3600;

        // --- Commodity Manufacturing ---

        /// <summary>Number of commodities produced per cycle.</summary>
        public static int CommoditiesPerCycle =>
            EmpireContext.getInstanceIfLoaded()?.GameConstants?.CommoditiesPerCycle ?? 10;

        /// <summary>Commodity manufacturing cycle time in seconds (10 minutes).</summary>
        public static long CommodityCycleSeconds =>
            EmpireContext.getInstanceIfLoaded()?.GameConstants?.CommodityCycleSeconds ?? 600;

        // --- Structures ---

        /// <summary>Maximum structures per colony (game cap).</summary>
        public static int StructureCap =>
            EmpireContext.getInstanceIfLoaded()?.GameConstants?.StructureCap ?? 65;

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
    }
}
