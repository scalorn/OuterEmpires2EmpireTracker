using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Static helper methods for determining colony build eligibility.
    /// Uses ColonyStructureViewModel internally to read IsStaged/IsBuilt per REQ-ARCH-010.
    /// </summary>
    public static class ColonyBuildEligibility
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Returns true if the structure is currently staged (ready to build).
        /// IsStaged=true AND IsBuilt=false.
        /// </summary>
        public static bool IsStagedStructure(ColonyStructure structure, PlayerContext pc)
        {
            var vm = new ColonyStructureViewModel(structure, pc);
            return vm.IsStaged && !vm.IsBuilt;
        }

        /// <summary>
        /// Returns true if the structure is currently building.
        /// IsStaged=false AND IsBuilt=false AND BuildCompletionTime != null AND TimeRemaining > 0.
        /// </summary>
        public static bool IsBuildingStructure(ColonyStructure structure, PlayerContext pc)
        {
            var vm = new ColonyStructureViewModel(structure, pc);
            return !vm.IsStaged && !vm.IsBuilt
                && structure.BuildCompletionTime != null
                && structure.BuildCompletionTime.TimeRemaining > 0;
        }

        /// <summary>
        /// Returns true if the colony is eligible for building:
        /// has at least one staged structure AND no building structure.
        /// </summary>
        public static bool IsEligible(Colony colony, PlayerContext pc)
        {
            bool hasStaged = colony.Structures.Any(s => IsStagedStructure(s, pc));
            bool hasBuilding = colony.Structures.Any(s => IsBuildingStructure(s, pc));
            return hasStaged && !hasBuilding;
        }

        /// <summary>
        /// Returns the first staged structure in the colony's Structures list order,
        /// or null if none found.
        /// </summary>
        public static ColonyStructure GetFirstStagedStructure(Colony colony, PlayerContext pc)
        {
            // Sort by BuildQueueSequence — never trust the raw list order
            var ordered = CollectionSortHelper.OrderStructures(colony.Structures);

            Log.Info(
                "GetFirstStagedStructure: colony={0} ({1}), {2} structures",
                colony.ColonyName ?? colony.PlanetName ?? "(unknown)",
                colony.UUID,
                ordered.Count);

            foreach (var s in ordered)
            {
                var bp = pc.FindBlueprint(s.FlatpackBlueprintUUID);
                string bpName = bp?.ExtendedName ?? s.FlatpackBlueprintUUID ?? "?";
                bool isStaged = IsStagedStructure(s, pc);
                bool isBuilt = new ColonyStructureViewModel(s, pc).IsBuilt;

                Log.Info(
                    "  seq={0} uuid={1} bp={2} staged={3} built={4}",
                    s.BuildQueueSequence,
                    s.UUID,
                    bpName,
                    isStaged,
                    isBuilt);

                if (isStaged)
                {
                    Log.Info("  -> SELECTED as first staged structure");
                    return s;
                }
            }

            Log.Info("  -> No staged structure found");
            return null;
        }
    }
}
