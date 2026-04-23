using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Stateless service for build plan validation and colony build item generation.
    /// </summary>
    public static class BuildPlanService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Validates a build plan name. Rejects empty or whitespace-only names.
        /// </summary>
        /// <param name="name">The plan name to validate.</param>
        /// <returns>True if the name is valid; false otherwise.</returns>
        public static bool ValidatePlanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Warn("ValidatePlanName: rejected empty/whitespace name");
                return false;
            }

            Log.Debug("ValidatePlanName: accepted name '{0}'", name);
            return true;
        }

        /// <summary>
        /// Validates a build item. Checks that the item type has required fields
        /// populated and that quantity is at least 1.
        /// </summary>
        /// <param name="item">The build item to validate.</param>
        /// <returns>True if the item is valid; false otherwise.</returns>
        public static bool ValidateBuildItem(BuildItem item)
        {
            if (item == null)
            {
                Log.Warn("ValidateBuildItem: rejected null item");
                return false;
            }

            if (item.Quantity < 1)
            {
                Log.Warn("ValidateBuildItem: rejected item with Quantity={0}", item.Quantity);
                return false;
            }

            switch (item.ItemType)
            {
                case BuildItemType.Manufactory:
                    if (string.IsNullOrEmpty(item.BlueprintUUID))
                    {
                        Log.Warn("ValidateBuildItem: Manufactory item missing BlueprintUUID");
                        return false;
                    }

                    break;

                case BuildItemType.Commodity:
                    if (string.IsNullOrEmpty(item.CommodityName))
                    {
                        Log.Warn("ValidateBuildItem: Commodity item missing CommodityName");
                        return false;
                    }

                    break;

                case BuildItemType.ShipTemplate:
                    if (string.IsNullOrEmpty(item.ShipTemplateUUID))
                    {
                        Log.Warn("ValidateBuildItem: ShipTemplate item missing ShipTemplateUUID");
                        return false;
                    }

                    break;

                case BuildItemType.Mining:
                    if (string.IsNullOrEmpty(item.MiningResource))
                    {
                        Log.Warn("ValidateBuildItem: Mining item missing MiningResource");
                        return false;
                    }

                    break;

                case BuildItemType.Refining:
                    if (string.IsNullOrEmpty(item.RefiningResource))
                    {
                        Log.Warn("ValidateBuildItem: Refining item missing RefiningResource");
                        return false;
                    }

                    break;

                case BuildItemType.Research:
                    if (string.IsNullOrEmpty(item.BlueprintUUID))
                    {
                        Log.Warn("ValidateBuildItem: Research item missing BlueprintUUID");
                        return false;
                    }

                    break;
            }

            Log.Debug("ValidateBuildItem: accepted {0} item UUID={1}", item.ItemType, item.UUID);
            return true;
        }

        /// <summary>
        /// Scans a colony's structures for unstaged entries (not Staged, not Built)
        /// and generates Manufactory build items for their flatpack blueprints.
        /// Skips structures whose FlatpackBlueprintUUID already has a matching
        /// BuildItem in the target plan (dedup by blueprint + colony).
        /// Items are created unallocated (empty BuildLocationUUID) so the user
        /// can assign them to any manufacturing colony via the Build Planner.
        /// Returns the number of items added.
        /// </summary>
        /// <param name="colony">The colony to scan for unstaged structures.</param>
        /// <param name="targetPlan">The build plan to add items to.</param>
        /// <param name="blueprintFinder">Delegate to resolve blueprint by UUID.</param>
        /// <returns>The number of build items added to the plan.</returns>
        public static int GenerateColonyBuildItems(
            Colony colony,
            BuildPlan targetPlan,
            Func<string, Blueprint> blueprintFinder)
        {
            if (colony == null) throw new ArgumentNullException(nameof(colony));
            if (targetPlan == null) throw new ArgumentNullException(nameof(targetPlan));
            if (blueprintFinder == null) throw new ArgumentNullException(nameof(blueprintFinder));

            var sw = Stopwatch.StartNew();
            Log.Debug(
                "GenerateColonyBuildItems: scanning colony '{0}' ({1})",
                colony.ColonyName,
                colony.UUID);

            int added = 0;
            var existingBlueprintUUIDs = BuildExistingBlueprintSet(targetPlan, colony.UUID);

            foreach (var structure in colony.Structures)
            {
                if (IsUnstagedUnbuilt(structure) &&
                    TryCreateBuildItem(
                        structure,
                        colony,
                        targetPlan,
                        blueprintFinder,
                        existingBlueprintUUIDs))
                {
                    added++;
                }
            }

            sw.Stop();
            Log.Info(
                "PERF GenerateColonyBuildItems: {0} items added for colony '{1}' in {2}ms",
                added,
                colony.ColonyName,
                sw.ElapsedMilliseconds);
            return added;
        }

        /// <summary>
        /// Builds a set of BlueprintUUIDs already present in the plan for the given colony.
        /// Used for dedup  items whose Notes contain the colony UUID are considered covered.
        /// </summary>
        private static HashSet<string> BuildExistingBlueprintSet(
            BuildPlan plan, string colonyUUID)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in plan.Items)
            {
                if (item.ItemType == BuildItemType.Manufactory &&
                    !string.IsNullOrEmpty(item.BlueprintUUID) &&
                    !string.IsNullOrEmpty(item.Notes) &&
                    item.Notes.Contains(colonyUUID))
                {
                    set.Add(item.BlueprintUUID);
                }
            }

            return set;
        }

        /// <summary>
        /// Returns true if the structure is not Staged and not Built (unstaged, unbuilt).
        /// </summary>
        private static bool IsUnstagedUnbuilt(ColonyStructure structure)
        {
            bool staged;
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
            if (staged) return false;

            bool built;
            structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
            if (built) return false;

            return true;
        }

        /// <summary>
        /// Attempts to create a Manufactory BuildItem for the given structure.
        /// Returns true if an item was created and added; false if skipped.
        /// </summary>
        private static bool TryCreateBuildItem(
            ColonyStructure structure,
            Colony colony,
            BuildPlan targetPlan,
            Func<string, Blueprint> blueprintFinder,
            HashSet<string> existingBlueprintUUIDs)
        {
            if (string.IsNullOrEmpty(structure.FlatpackBlueprintUUID))
            {
                Log.Warn(
                    "GenerateColonyBuildItems: structure {0} has no FlatpackBlueprintUUID, skipping",
                    structure.UUID);
                return false;
            }

            if (existingBlueprintUUIDs.Contains(structure.FlatpackBlueprintUUID))
            {
                Log.Debug(
                    "GenerateColonyBuildItems: blueprint {0} already in plan for colony {1}, skipping",
                    structure.FlatpackBlueprintUUID,
                    colony.UUID);
                return false;
            }

            Blueprint bp = blueprintFinder(structure.FlatpackBlueprintUUID);
            string itemName = bp != null ? bp.Name : "Unknown Blueprint";

            var buildItem = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = BuildItemType.Manufactory,
                Status = BuildItemStatus.Staged,
                BlueprintUUID = structure.FlatpackBlueprintUUID,
                ItemName = itemName,
                Quantity = 1,
                BuildLocationType = DestinationType.Colony,
                BuildLocationUUID = string.Empty,
                Notes = string.Format("For colony: {0} ({1})", colony.ColonyName, colony.UUID)
            };

            targetPlan.Items.Add(buildItem);
            existingBlueprintUUIDs.Add(structure.FlatpackBlueprintUUID);
            return true;
        }
    }
}
