using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for build plan execution: status detection, manufacturing
    /// pre-configuration, and progress tracking. Follows the same static-class pattern
    /// as ResourceCheckService with dependencies passed as parameters.
    /// </summary>
    public static class BuildPlanExecutionService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Computes a status summary for a build plan.
        /// Returns counts per BuildItemStatus. All enum values are present
        /// in the dictionary, defaulting to zero.
        /// </summary>
        /// <param name="plan">The build plan to summarize.</param>
        /// <returns>Dictionary of BuildItemStatus to item count.</returns>
        public static Dictionary<BuildItemStatus, int> ComputeStatusSummary(BuildPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var summary = new Dictionary<BuildItemStatus, int>();

            // Initialize all enum values to zero
            foreach (BuildItemStatus status in Enum.GetValues(typeof(BuildItemStatus)))
            {
                summary[status] = 0;
            }

            // Count items per status
            foreach (var item in plan.Items)
            {
                summary[item.Status]++;
            }

            return summary;
        }

        /// <summary>
        /// Checks if a plan is fully complete (all items Completed).
        /// Returns false for plans with no items.
        /// </summary>
        /// <param name="plan">The build plan to check.</param>
        /// <returns>True if all items have Status == Completed and the plan has at least one item.</returns>
        public static bool IsPlanComplete(BuildPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (plan.Items.Count == 0)
            {
                return false;
            }

            foreach (var item in plan.Items)
            {
                if (item.Status != BuildItemStatus.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Detects cross-plan contention: finds items from other plans
        /// assigned to the same structure as the given item.
        /// Only checks active plans. Does not include items from the plan
        /// that contains the given item (matched by item UUID).
        /// </summary>
        /// <param name="item">The build item to check for contention.</param>
        /// <param name="allPlans">All build plans to search for contention.</param>
        /// <returns>List of ContentionInfo for items from other plans on the same structure.</returns>
        public static List<ContentionInfo> DetectContention(
            BuildItem item,
            IEnumerable<BuildPlan> allPlans)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (allPlans == null)
            {
                throw new ArgumentNullException(nameof(allPlans));
            }

            var contentions = new List<ContentionInfo>();

            if (string.IsNullOrEmpty(item.StructureUUID))
            {
                return contentions;
            }

            foreach (var plan in allPlans)
            {
                if (!plan.IsActive)
                {
                    continue;
                }

                // Check if this plan contains the item itself; if so, skip it
                bool planContainsItem = false;
                foreach (var planItem in plan.Items)
                {
                    if (planItem.UUID == item.UUID)
                    {
                        planContainsItem = true;
                        break;
                    }
                }

                if (planContainsItem)
                {
                    continue;
                }

                // Find items in this plan on the same structure
                foreach (var otherItem in plan.Items)
                {
                    if (otherItem.StructureUUID == item.StructureUUID
                        && !string.IsNullOrEmpty(otherItem.StructureUUID))
                    {
                        contentions.Add(new ContentionInfo
                        {
                            PlanName = plan.Name,
                            ItemName = otherItem.ItemName,
                            ItemUUID = otherItem.UUID
                        });
                    }
                }
            }

            if (contentions.Count > 0)
            {
                Log.Debug(
                    "DetectContention: item {0} on structure {1} has {2} contending items from other plans",
                    item.UUID,
                    item.StructureUUID,
                    contentions.Count);
            }

            return contentions;
        }

        /// <summary>
        /// Advances build item statuses for a single plan based on colony structure state.
        /// Handles three detection phases:
        ///   Phase 1: Staged+allocated items with zero shortfalls advance to Ready (skip Delivering).
        ///   Phase 2: Ready items advance to InProgress when structure has matching active job.
        ///   Phase 3: InProgress items advance to Completed when structure job finishes.
        /// Returns true if any item status changed.
        /// </summary>
        /// <param name="plan">The build plan to process.</param>
        /// <param name="colonyFinder">Delegate to resolve a colony by UUID.</param>
        /// <param name="blueprintFinder">Delegate to resolve a blueprint by UUID.</param>
        /// <param name="shipFinder">Delegate to resolve a ship by UUID (future use).</param>
        /// <param name="stationFinder">Delegate to resolve a station by UUID (future use).</param>
        /// <param name="currentPlayerUUID">Current player UUID for location resolution.</param>
        /// <returns>True if any item status changed.</returns>
        public static bool AdvanceBuildItemStatuses(
            BuildPlan plan,
            Func<string, Colony> colonyFinder,
            Func<string, Blueprint> blueprintFinder,
            Func<string, Ship> shipFinder,
            Func<string, Station> stationFinder,
            string currentPlayerUUID)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (colonyFinder == null) throw new ArgumentNullException(nameof(colonyFinder));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            bool anyChanged = false;

            // Phase 1: Staged+allocated to Ready (skip Delivering)
            foreach (var item in plan.Items)
            {
                if (item.Status != BuildItemStatus.Staged)
                    continue;
                if (string.IsNullOrEmpty(item.BuildLocationUUID) || string.IsNullOrEmpty(item.StructureUUID))
                    continue;

                try
                {
                    var colony = colonyFinder(item.BuildLocationUUID);
                    if (colony == null)
                    {
                        Log.Warn(
                            "AdvanceBuildItemStatuses Phase1: colony {0} not found for item {1}",
                            item.BuildLocationUUID, item.UUID);
                        continue;
                    }

                    var shortfalls = ResourceCheckService.ComputeShortfalls(
                        item, colony.Items, blueprintFinder);

                    if (shortfalls.Count == 0)
                    {
                        if (BackgroundProcessor.TryAdvanceStatus(item, BuildItemStatus.Ready))
                        {
                            anyChanged = true;
                            Log.Info(
                                "AdvanceBuildItemStatuses Phase1: item {0} in plan '{1}' on structure {2} advanced Staged->Ready (resources present)",
                                item.UUID, plan.Name, item.StructureUUID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "AdvanceBuildItemStatuses Phase1: error processing item {0} in plan '{1}'",
                        item.UUID, plan.Name);
                }
            }

            // Phase 2: Ready to InProgress (detect manufacturing start)
            foreach (var item in plan.Items)
            {
                if (item.Status != BuildItemStatus.Ready)
                    continue;
                if (string.IsNullOrEmpty(item.StructureUUID))
                    continue;

                try
                {
                    // Resolve the ColonyStructure
                    var colony = colonyFinder(item.BuildLocationUUID);
                    if (colony == null)
                    {
                        Log.Warn(
                            "AdvanceBuildItemStatuses Phase2: colony {0} not found for item {1}",
                            item.BuildLocationUUID, item.UUID);
                        continue;
                    }

                    ColonyStructure structure = FindStructureByUUID(colony, item.StructureUUID);
                    if (structure == null)
                    {
                        Log.Warn(
                            "AdvanceBuildItemStatuses Phase2: structure {0} not found in colony {1} for item {2}",
                            item.StructureUUID, item.BuildLocationUUID, item.UUID);
                        continue;
                    }

                    // Check dependency: if DependsOnUUID is set, verify dependency is Completed
                    if (!string.IsNullOrEmpty(item.DependsOnUUID))
                    {
                        BuildItem depItem = null;
                        foreach (var candidate in plan.Items)
                        {
                            if (candidate.UUID == item.DependsOnUUID)
                            {
                                depItem = candidate;
                                break;
                            }
                        }

                        if (depItem == null)
                        {
                            Log.Warn(
                                "AdvanceBuildItemStatuses Phase2: dependency {0} not found in plan '{1}' for item {2}, treating as satisfied",
                                item.DependsOnUUID, plan.Name, item.UUID);
                        }
                        else if (depItem.Status != BuildItemStatus.Completed)
                        {
                            continue; // Dependency not yet completed, skip this item
                        }
                    }

                    // Check sequence: item must be lowest SequenceInStructure among Ready items on same structure
                    bool isLowestSequence = true;
                    foreach (var other in plan.Items)
                    {
                        if (other.UUID == item.UUID)
                            continue;
                        if (other.Status != BuildItemStatus.Ready)
                            continue;
                        if (other.StructureUUID != item.StructureUUID)
                            continue;
                        if (other.SequenceInStructure < item.SequenceInStructure)
                        {
                            isLowestSequence = false;
                            break;
                        }
                    }

                    if (!isLowestSequence)
                        continue;

                    // Check structure state by item type
                    bool structureMatches = false;
                    switch (item.ItemType)
                    {
                        case BuildItemType.Manufactory:
                            structureMatches = structure.ProcessCompletionTime != null
                                && structure.ManufacturingBlueprintUUID == item.BlueprintUUID;
                            break;

                        case BuildItemType.Commodity:
                            structureMatches = structure.ProcessCompletionTime != null
                                && structure.ManufacturingCommodityName == item.CommodityName;
                            break;

                        case BuildItemType.Research:
                            structureMatches = structure.ProcessCompletionTime != null
                                && structure.ResearchingBlueprintUUID == item.BlueprintUUID;
                            break;

                        case BuildItemType.Mining:
                            structureMatches = structure.ProcessCompletionTime != null;
                            break;

                        case BuildItemType.Refining:
                            structureMatches = structure.ProcessCompletionTime != null;
                            break;
                    }

                    if (structureMatches)
                    {
                        if (BackgroundProcessor.TryAdvanceStatus(item, BuildItemStatus.InProgress))
                        {
                            anyChanged = true;
                            Log.Info(
                                "AdvanceBuildItemStatuses Phase2: item {0} ({1}) in plan '{2}' on structure {3} advanced Ready->InProgress",
                                item.UUID, item.ItemType, plan.Name, item.StructureUUID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "AdvanceBuildItemStatuses Phase2: error processing item {0} in plan '{1}'",
                        item.UUID, plan.Name);
                }
            }

            // Phase 3: InProgress to Completed (detect manufacturing finish)
            foreach (var item in plan.Items)
            {
                if (item.Status != BuildItemStatus.InProgress)
                    continue;
                if (string.IsNullOrEmpty(item.StructureUUID))
                    continue;

                try
                {
                    // Resolve the ColonyStructure
                    var colony = colonyFinder(item.BuildLocationUUID);
                    if (colony == null)
                    {
                        Log.Warn(
                            "AdvanceBuildItemStatuses Phase3: colony {0} not found for item {1}",
                            item.BuildLocationUUID, item.UUID);
                        continue;
                    }

                    ColonyStructure structure = FindStructureByUUID(colony, item.StructureUUID);
                    if (structure == null)
                    {
                        Log.Warn(
                            "AdvanceBuildItemStatuses Phase3: structure {0} not found in colony {1} for item {2}",
                            item.StructureUUID, item.BuildLocationUUID, item.UUID);
                        continue;
                    }

                    // Check completion by item type
                    bool isCompleted = false;
                    switch (item.ItemType)
                    {
                        case BuildItemType.Manufactory:
                            isCompleted =
                                (structure.ProcessCompletionTime == null
                                    && string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                                || structure.ManufacturingCompleted >= item.Quantity;
                            break;

                        case BuildItemType.Commodity:
                            isCompleted = structure.ProcessCompletionTime == null
                                && string.IsNullOrEmpty(structure.ManufacturingCommodityName);
                            break;

                        case BuildItemType.Research:
                            isCompleted = structure.ProcessCompletionTime == null
                                && string.IsNullOrEmpty(structure.ResearchingBlueprintUUID);
                            break;

                        case BuildItemType.Mining:
                            isCompleted = structure.ProcessCompletionTime == null;
                            break;

                        case BuildItemType.Refining:
                            isCompleted = structure.ProcessCompletionTime == null;
                            break;
                    }

                    if (isCompleted)
                    {
                        if (BackgroundProcessor.TryAdvanceStatus(item, BuildItemStatus.Completed))
                        {
                            anyChanged = true;
                            Log.Info(
                                "AdvanceBuildItemStatuses Phase3: item {0} ({1}) in plan '{2}' on structure {3} advanced InProgress->Completed",
                                item.UUID, item.ItemType, plan.Name, item.StructureUUID);

                            // Log that next item in sequence is now eligible (if one exists)
                            LogNextEligibleItem(plan, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "AdvanceBuildItemStatuses Phase3: error processing item {0} in plan '{1}'",
                        item.UUID, plan.Name);
                }
            }

            return anyChanged;
        }

        /// <summary>
        /// Finds a ColonyStructure by UUID within a colony's Structures list.
        /// </summary>
        private static ColonyStructure FindStructureByUUID(Colony colony, string structureUUID)
        {
            if (colony == null || colony.Structures == null || string.IsNullOrEmpty(structureUUID))
                return null;

            foreach (var structure in colony.Structures)
            {
                if (structure.UUID == structureUUID)
                    return structure;
            }

            return null;
        }

        /// <summary>
        /// Logs that the next item in sequence on the same structure is now eligible for manufacturing.
        /// </summary>
        private static void LogNextEligibleItem(BuildPlan plan, BuildItem completedItem)
        {
            BuildItem nextItem = null;
            int lowestSeq = int.MaxValue;

            foreach (var candidate in plan.Items)
            {
                if (candidate.UUID == completedItem.UUID)
                    continue;
                if (candidate.StructureUUID != completedItem.StructureUUID)
                    continue;
                if (candidate.Status != BuildItemStatus.Ready)
                    continue;
                if (candidate.SequenceInStructure < lowestSeq)
                {
                    lowestSeq = candidate.SequenceInStructure;
                    nextItem = candidate;
                }
            }

            if (nextItem != null)
            {
                Log.Info(
                    "AdvanceBuildItemStatuses: next eligible item on structure {0} is {1} (seq {2}) in plan '{3}'",
                    completedItem.StructureUUID, nextItem.UUID, nextItem.SequenceInStructure, plan.Name);
            }
        }

        /// <summary>
        /// Result of a single StartManufacturing operation.
        /// </summary>
        public class StartManufacturingResult
        {
            public bool Success { get; set; }

            public string ErrorMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// Result of a batch StartAllReady operation.
        /// </summary>
        public class BatchStartResult
        {
            public int StartedCount { get; set; }

            public int SkippedCount { get; set; }

            public List<string> SkippedReasons { get; set; } = new List<string>();
        }

        /// <summary>
        /// Information about a cross-plan contention on a structure.
        /// </summary>
        public class ContentionInfo
        {
            public string PlanName { get; set; } = string.Empty;

            public string ItemName { get; set; } = string.Empty;

            public string ItemUUID { get; set; } = string.Empty;
        }
    }
}
