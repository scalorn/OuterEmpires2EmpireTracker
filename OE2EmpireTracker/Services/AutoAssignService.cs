using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Proposed assignment of a build item to a specific structure.
    /// Returned by <see cref="AutoAssignService.ProposeAssignments"/>
    /// for user review before applying.
    /// </summary>
    public class AssignmentProposal
    {
        /// <summary>UUID of the build item being assigned.</summary>
        public string BuildItemUUID { get; set; }

        /// <summary>Type of the build location (Colony, Ship, Station).</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DestinationType BuildLocationType { get; set; } = DestinationType.Colony;

        /// <summary>UUID of the build location (colony, ship, or station).</summary>
        public string BuildLocationUUID { get; set; }

        /// <summary>UUID of the structure within the location.</summary>
        public string StructureUUID { get; set; }

        /// <summary>
        /// Sequence position when multiple items share a structure.
        /// 0 = first/only item; higher values run after earlier items complete.
        /// </summary>
        public int SequenceInStructure { get; set; }

        /// <summary>Human-readable reason for this assignment choice.</summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stateless service that proposes structure assignments for unallocated
    /// build items, minimizing total completion time while respecting
    /// blueprint copy limits. Currently only Colony locations are supported.
    /// </summary>
    public static class AutoAssignService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Proposes structure assignments for unallocated build items,
        /// minimizing total completion time while respecting blueprint
        /// copy limits. Considers structures at build locations on the
        /// specified delivery route. Currently only Colony locations
        /// are supported; Ship and Station are deferred.
        /// </summary>
        /// <param name="plan">The build plan containing items to assign.</param>
        /// <param name="route">Delivery route whose stops define eligible locations.</param>
        /// <param name="colonyFinder">Resolves a colony UUID to a Colony.</param>
        /// <param name="shipFinder">Resolves a ship UUID to a Ship (future use).</param>
        /// <param name="stationFinder">Resolves a station UUID to a Station (future use).</param>
        /// <param name="blueprintFinder">Resolves a blueprint UUID to a Blueprint.</param>
        /// <returns>
        /// List of proposed assignments for user review. Empty if no
        /// unallocated items or no eligible structures.
        /// </returns>
        public static List<AssignmentProposal> ProposeAssignments(
            BuildPlan plan,
            DeliveryRoute route,
            Func<string, Colony> colonyFinder,
            Func<string, Ship> shipFinder,
            Func<string, Station> stationFinder,
            Func<string, Blueprint> blueprintFinder)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (route == null) throw new ArgumentNullException(nameof(route));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            var proposals = new List<AssignmentProposal>();

            var unallocated = CollectUnallocatedItems(plan);
            if (unallocated.Count == 0)
            {
                Log.Info("ProposeAssignments: no unallocated items in plan '{0}'",
                    plan.Name);
                sw.Stop();
                Log.Debug("PERF ProposeAssignments: {0}ms", sw.ElapsedMilliseconds);
                return proposals;
            }

            var structures = CollectEligibleStructures(
                route, colonyFinder, blueprintFinder);

            AssignManufactoryItems(unallocated, structures, blueprintFinder, proposals);
            AssignCommodityItems(unallocated, structures, proposals);

            sw.Stop();
            Log.Info("PERF ProposeAssignments: {0} proposals for plan '{1}' in {2}ms",
                proposals.Count, plan.Name, sw.ElapsedMilliseconds);
            return proposals;
        }

        /// <summary>
        /// Collects unallocated build items (empty BuildLocationUUID and
        /// empty StructureUUID) of Manufactory or Commodity type.
        /// </summary>
        private static List<BuildItem> CollectUnallocatedItems(BuildPlan plan)
        {
            var items = new List<BuildItem>();
            foreach (var item in plan.Items)
            {
                if (!string.IsNullOrEmpty(item.BuildLocationUUID) ||
                    !string.IsNullOrEmpty(item.StructureUUID))
                    continue;

                if (item.ItemType == BuildItemType.Manufactory ||
                    item.ItemType == BuildItemType.Commodity)
                {
                    items.Add(item);
                }
            }
            Log.Debug("CollectUnallocatedItems: {0} unallocated items", items.Count);
            return items;
        }

        /// <summary>
        /// Collects all built-and-online idle structures from colonies on
        /// the delivery route, grouped by structure type.
        /// </summary>
        private static EligibleStructures CollectEligibleStructures(
            DeliveryRoute route,
            Func<string, Colony> colonyFinder,
            Func<string, Blueprint> blueprintFinder)
        {
            var result = new EligibleStructures();

            foreach (var stop in route.Stops)
            {
                if (stop.DestinationType != DestinationType.Colony)
                {
                    Log.Debug("CollectEligibleStructures: skipping non-colony stop {0}",
                        stop.DestinationUUID);
                    continue;
                }

                string colonyUUID = !string.IsNullOrEmpty(stop.DestinationUUID)
                    ? stop.DestinationUUID
                    : stop.ColonyUUID;

                if (string.IsNullOrEmpty(colonyUUID))
                    continue;

                Colony colony = colonyFinder(colonyUUID);
                if (colony == null)
                {
                    Log.Warn("CollectEligibleStructures: colony {0} not found",
                        colonyUUID);
                    continue;
                }

                foreach (var structure in colony.Structures)
                {
                    if (!structure.IsBuiltAndOnline)
                        continue;

                    if (!IsIdle(structure))
                        continue;

                    if (string.IsNullOrEmpty(structure.FlatpackBlueprintUUID))
                        continue;

                    Blueprint bp = blueprintFinder(structure.FlatpackBlueprintUUID);
                    if (bp == null)
                        continue;

                    var info = new StructureInfo
                    {
                        ColonyUUID = colony.UUID,
                        StructureUUID = structure.UUID,
                        BlueprintType = bp.BluePrintType
                    };

                    if (bp.BluePrintType == BlueprintTypes.Manufactory)
                        result.Manufactories.Add(info);
                    else if (bp.BluePrintType.IsCommodityFactory())
                        result.CommodityFactories.Add(info);
                }
            }

            Log.Debug("CollectEligibleStructures: {0} manufactories, {1} commodity factories",
                result.Manufactories.Count, result.CommodityFactories.Count);
            return result;
        }

        /// <summary>
        /// Returns true if the structure has no active manufacturing or
        /// commodity job running.
        /// </summary>
        private static bool IsIdle(ColonyStructure structure)
        {
            if (!string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                return false;
            if (!string.IsNullOrEmpty(structure.ManufacturingCommodityName))
                return false;
            return true;
        }

        /// <summary>
        /// Assigns Manufactory build items to available manufactories,
        /// respecting the blueprint copy constraint.
        /// </summary>
        private static void AssignManufactoryItems(
            List<BuildItem> unallocated,
            EligibleStructures structures,
            Func<string, Blueprint> blueprintFinder,
            List<AssignmentProposal> proposals)
        {
            var mfgItems = unallocated
                .Where(i => i.ItemType == BuildItemType.Manufactory)
                .ToList();

            if (mfgItems.Count == 0 || structures.Manufactories.Count == 0)
                return;

            // Track how many items are assigned to each structure
            // Key = StructureUUID, Value = count of items assigned
            var structureLoad = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in structures.Manufactories)
                structureLoad[s.StructureUUID] = 0;

            // Group items by BlueprintUUID to apply copy constraint per blueprint
            var byBlueprint = new Dictionary<string, List<BuildItem>>(StringComparer.Ordinal);
            foreach (var item in mfgItems)
            {
                if (string.IsNullOrEmpty(item.BlueprintUUID))
                    continue;
                if (!byBlueprint.ContainsKey(item.BlueprintUUID))
                    byBlueprint[item.BlueprintUUID] = new List<BuildItem>();
                byBlueprint[item.BlueprintUUID].Add(item);
            }

            foreach (var kvp in byBlueprint)
            {
                string bpUUID = kvp.Key;
                var items = kvp.Value;

                // Count blueprint copies the player owns
                Blueprint bp = blueprintFinder(bpUUID);
                int copyCount = CountBlueprintCopies(bpUUID, blueprintFinder);
                if (copyCount < 1) copyCount = 1;

                // Max parallel = min(available structures, blueprint copies)
                int maxParallel = Math.Min(structures.Manufactories.Count, copyCount);

                // Sort structures by current load (least loaded first)
                var sortedStructures = structures.Manufactories
                    .OrderBy(s => structureLoad[s.StructureUUID])
                    .ToList();

                // Assign each item to the least-loaded structure, cycling
                // through up to maxParallel structures
                for (int i = 0; i < items.Count; i++)
                {
                    int structIdx = i % maxParallel;
                    // Re-sort to always pick least loaded
                    sortedStructures = structures.Manufactories
                        .OrderBy(s => structureLoad[s.StructureUUID])
                        .ToList();

                    var target = sortedStructures[structIdx % sortedStructures.Count];
                    int seq = structureLoad[target.StructureUUID];

                    string reason = copyCount < structures.Manufactories.Count
                        ? string.Format("Blueprint has {0} copies; limited to {0} parallel structures",
                            copyCount)
                        : string.Format("Assigned to least-loaded structure (seq {0})", seq);

                    proposals.Add(new AssignmentProposal
                    {
                        BuildItemUUID = items[i].UUID,
                        BuildLocationType = DestinationType.Colony,
                        BuildLocationUUID = target.ColonyUUID,
                        StructureUUID = target.StructureUUID,
                        SequenceInStructure = seq,
                        Reason = reason
                    });

                    structureLoad[target.StructureUUID] = seq + 1;
                }
            }
        }

        /// <summary>
        /// Assigns Commodity build items to available commodity factories.
        /// No copy constraint ? distributes round-robin across all factories.
        /// </summary>
        private static void AssignCommodityItems(
            List<BuildItem> unallocated,
            EligibleStructures structures,
            List<AssignmentProposal> proposals)
        {
            var commodityItems = unallocated
                .Where(i => i.ItemType == BuildItemType.Commodity)
                .ToList();

            if (commodityItems.Count == 0 || structures.CommodityFactories.Count == 0)
                return;

            // Track load per structure
            var structureLoad = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in structures.CommodityFactories)
                structureLoad[s.StructureUUID] = 0;

            for (int i = 0; i < commodityItems.Count; i++)
            {
                // Pick least-loaded factory
                var target = structures.CommodityFactories
                    .OrderBy(s => structureLoad[s.StructureUUID])
                    .First();

                int seq = structureLoad[target.StructureUUID];

                proposals.Add(new AssignmentProposal
                {
                    BuildItemUUID = commodityItems[i].UUID,
                    BuildLocationType = DestinationType.Colony,
                    BuildLocationUUID = target.ColonyUUID,
                    StructureUUID = target.StructureUUID,
                    SequenceInStructure = seq,
                    Reason = string.Format("Commodity factory (no copy constraint, seq {0})", seq)
                });

                structureLoad[target.StructureUUID] = seq + 1;
            }
        }

        /// <summary>
        /// Counts how many copies of a blueprint the player owns.
        /// Uses the blueprint's Name to find all copies in the collection
        /// via the blueprintFinder. Returns at least 1.
        /// </summary>
        /// <remarks>
        /// This is a simplified count. The blueprintFinder resolves a single
        /// UUID, so we return 1 as the baseline. When the _blueprintTypeCountCache
        /// is available in PlayerContext, this can be replaced with a direct
        /// cache lookup for accurate counts.
        /// </remarks>
        private static int CountBlueprintCopies(
            string blueprintUUID,
            Func<string, Blueprint> blueprintFinder)
        {
            // The blueprintFinder resolves by UUID. We can only confirm
            // the blueprint exists. The actual copy count requires scanning
            // the full blueprint list, which is done via PlayerContext cache.
            // For now, return 1 as the minimum ? the caller can provide a
            // blueprintFinder that wraps PlayerContext.CountBlueprintCopies
            // for accurate counts.
            Blueprint bp = blueprintFinder(blueprintUUID);
            if (bp == null) return 1;
            return Math.Max(1, bp.Quantity);
        }

        /// <summary>Holds categorized eligible structures from route stops.</summary>
        private class EligibleStructures
        {
            public List<StructureInfo> Manufactories { get; } = new List<StructureInfo>();
            public List<StructureInfo> CommodityFactories { get; } = new List<StructureInfo>();
        }

        /// <summary>Identifies a structure and its location.</summary>
        private class StructureInfo
        {
            public string ColonyUUID { get; set; }
            public string StructureUUID { get; set; }
            public string BlueprintType { get; set; }
        }
    }
}
