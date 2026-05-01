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
        // --- Timers ---

        /// <summary>Seconds in one hour -- used for top-of-hour timer alignment.</summary>
        public const long SecondsPerHour = 3600;

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

        /// <summary>Purity value for high-purity resources.</summary>
        public const string PurityHigh = "High";

        /// <summary>Purity value for medium-purity resources.</summary>
        public const string PurityMedium = "Medium";

        /// <summary>Purity value for low-purity resources.</summary>
        public const string PurityLow = "Low";

        /// <summary>Refining output multiplier for low-purity resources.</summary>
        public const int PurityMultiplierLow = 1;

        /// <summary>Refining output multiplier for medium-purity resources.</summary>
        public const int PurityMultiplierMedium = 3;

        /// <summary>Refining output multiplier for high-purity resources.</summary>
        public const int PurityMultiplierHigh = 5;

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

        // --- Item Volume by Type (REQ-DM-025) ---

        public const decimal VolumeResource = 1.0m;

        public const decimal VolumeCommodity = 10.0m;

        public const decimal VolumeWorkDetail = 50.0m;

        public const decimal VolumeBlueprint = 0.0m;

        public const decimal VolumeSurvey = 0.0m;

        // --- Item Mass by Type ---

        public const decimal MassResource = 1.0m;

        public const decimal MassCommodity = 5.0m;

        public const decimal MassWorkDetail = 10.0m;

        // --- Skill Multiplier Rates (per level) ---

        /// <summary>ExtractionFocus: +1% per level.</summary>
        public const decimal ExtractionFocusRatePerLevel = 0.01m;

        /// <summary>RefiningFocus: +2% per level.</summary>
        public const decimal RefiningFocusRatePerLevel = 0.02m;

        /// <summary>ProductionFocus: +3% per level (manufacturing and commodity time reduction).</summary>
        public const decimal ProductionFocusRatePerLevel = 0.03m;

        /// <summary>Builder: +2% per level (structure build time reduction).</summary>
        public const decimal BuilderRatePerLevel = 0.02m;

        /// <summary>Base structure build time in seconds (1 day).</summary>
        public const long BaseBuildTimeSeconds = 86400L;

        // --- Refining ---

        /// <summary>Base refining rate per cycle (units consumed from source).</summary>
        public static int RefiningBaseRate =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.RefiningBaseRate ?? 25;

        // --- Workers ---

        /// <summary>Cargo volume per worker detail item.</summary>
        public static decimal WorkerVolume =>
            EmpireContext.GetInstanceIfLoaded()?.GameConstants?.WorkerVolume ?? 50m;

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

        /// <summary>
        /// Computes the refining output rate for a given purity and base rate.
        /// </summary>
        public static int GetRefiningOutputRate(string purity, int baseRate)
        {
            switch (purity)
            {
                case PurityLow: return baseRate * PurityMultiplierLow;
                case PurityMedium: return baseRate * PurityMultiplierMedium;
                case PurityHigh: return baseRate * PurityMultiplierHigh;
                default: return baseRate * PurityMultiplierLow;
            }
        }
    }
}
