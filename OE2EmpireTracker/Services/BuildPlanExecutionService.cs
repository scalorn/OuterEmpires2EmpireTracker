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
