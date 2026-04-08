namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Game-wide numeric and string constants used across the codebase.
    /// </summary>
    public static class GameConstants
    {
        // --- Refining ---

        /// <summary>Base refining rate per cycle (units consumed from source).</summary>
        public const int RefiningBaseRate = 25;

        // --- Workers ---

        /// <summary>Cargo volume per worker detail item.</summary>
        public const double WorkerVolume = 50;

        // --- Timers ---

        /// <summary>Seconds in one hour — used for top-of-hour timer alignment.</summary>
        public const long SecondsPerHour = 3600;

        // --- Commodity Manufacturing ---

        /// <summary>Number of commodities produced per cycle.</summary>
        public const int CommoditiesPerCycle = 10;

        /// <summary>Commodity manufacturing cycle time in seconds (10 minutes).</summary>
        public const long CommodityCycleSeconds = 600;

        // --- Structures ---

        /// <summary>Maximum structures per colony (game cap).</summary>
        public const int StructureCap = 65;

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
