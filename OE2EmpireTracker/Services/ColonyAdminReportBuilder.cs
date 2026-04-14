using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Builds a per-colony admin status report as an RTF string for display
    /// in a RichTextBox on the Colony form's Administration tab.
    /// </summary>
    public static class ColonyAdminReportBuilder
    {
        // Color palette for report sections
        private static readonly Color HeaderColor = Color.FromArgb(0, 120, 215);
        private static readonly Color CountdownColor = Color.FromArgb(200, 120, 0);
        private static readonly Color CompletionTimeColor = Color.FromArgb(0, 150, 80);
        private static readonly Color TextColor = Color.FromArgb(60, 60, 60);
        private static readonly Color DetailColor = Color.FromArgb(100, 100, 100);

        /// <summary>
        /// Builds the full admin report RTF string for a single colony.
        /// Returns empty string if colony is null or has no UUID.
        /// </summary>
        public static string BuildReport(Colony colony, PlayerContext playerContext)
        {
            if (colony == null || string.IsNullOrEmpty(colony.UUID))
                return string.Empty;

            var activities = ColonyActivityCollector.CollectActivities(
                new[] { colony }, playerContext);
            var inactivities = ColonyInactivityCollector.CollectInactivities(
                new[] { colony }, playerContext);

            // Partition activity rows
            var buildingRows = activities.Where(r => r.Type == ActivityType.Building).ToList();
            var commodityRequestRows = activities.Where(r => r.Type == ActivityType.CommodityRequest).ToList();
            var otherActivityRows = activities
                .Where(r => r.Type != ActivityType.Building && r.Type != ActivityType.CommodityRequest)
                .ToList();

            var builder = new RtfBuilder();
            bool hasContent = false;

            // Section 1: Building
            hasContent |= RenderBuildingSection(builder, buildingRows, hasContent);

            // Section 2: Commodity Requests
            hasContent |= RenderCommodityRequestSection(builder, commodityRequestRows, hasContent);

            // Section 3: Inactivity
            hasContent |= RenderInactivitySection(builder, inactivities, hasContent);

            // Section 4: Activity (non-repeating + aggregated mining/refining)
            hasContent |= RenderActivitySection(builder, otherActivityRows, colony, playerContext, hasContent);

            if (!hasContent)
                return string.Empty;

            return builder.ToRtf();
        }

        // ----- Building Section -----

        private static bool RenderBuildingSection(RtfBuilder builder, List<ActivityRow> rows, bool needsLeadingNewline)
        {
            if (rows.Count == 0) return false;

            var sorted = rows.OrderBy(r => r.GetSecondsRemaining()).ToList();

            if (needsLeadingNewline) builder.Append("\n", TextColor);
            builder.Append("Building\n", HeaderColor);

            foreach (var row in sorted)
            {
                builder.Append("  " + row.SourceName + " -- ", TextColor);
                builder.Append(row.GetTimeRemainingString(), CountdownColor);
                if (row.CountDown != null)
                {
                    string localTime = row.CountDown.EndTime.ToLocalTime().ToString("HH:mm ddd");
                    builder.Append(" (" + localTime + ")", CompletionTimeColor);
                }
                builder.Append("\n", TextColor);
            }

            return true;
        }

        // ----- Commodity Requests Section -----

        private static bool RenderCommodityRequestSection(RtfBuilder builder, List<ActivityRow> rows, bool needsLeadingNewline)
        {
            if (rows.Count == 0) return false;

            if (needsLeadingNewline) builder.Append("\n", TextColor);
            builder.Append("Commodity Requests\n", HeaderColor);

            foreach (var row in rows)
            {
                builder.Append("  " + row.ProcessDetails, TextColor);
                if (row.NeedBy != DateTime.MinValue)
                {
                    builder.Append(" -- due " + row.NeedBy.ToLocalTime().ToString("ddMMMyy-h:mmtt").ToLower(), DetailColor);
                }
                builder.Append("\n", TextColor);
            }

            return true;
        }

        // ----- Inactivity Section -----

        /// <summary>
        /// Fixed order for inactivity groups matching requirements 3.2.
        /// </summary>
        private static readonly (ActivityType type, string header)[] InactivityGroupOrder = new[]
        {
            (ActivityType.ColonyImportStaleness, "Colony Import Staleness"),
            (ActivityType.Mining,                "Idle Mining"),
            (ActivityType.Refining,              "Idle Refining"),
            (ActivityType.Manufacturing,         "Idle Manufacturing"),
            (ActivityType.CommodityManufacturing,"Idle Commodity Manufacturing"),
            (ActivityType.Research,              "Idle Research"),
        };

        private static bool RenderInactivitySection(RtfBuilder builder, List<ActivityRow> rows, bool needsLeadingNewline)
        {
            if (rows.Count == 0) return false;

            // Separate underutilized refiners (have "Underutilized" in ProcessDetails)
            var underutilized = rows.Where(r => r.Type == ActivityType.Refining &&
                r.ProcessDetails != null && r.ProcessDetails.StartsWith("Underutilized")).ToList();
            var normalRows = rows.Except(underutilized).ToList();

            bool anyRendered = false;

            foreach (var group in InactivityGroupOrder)
            {
                var groupRows = normalRows.Where(r => r.Type == group.type).ToList();
                if (groupRows.Count == 0) continue;

                if (needsLeadingNewline || anyRendered) builder.Append("\n", TextColor);
                builder.Append(group.header + "\n", HeaderColor);

                foreach (var row in groupRows)
                {
                    builder.Append("  " + row.SourceName + " -- " + row.ProcessDetails + "\n", TextColor);
                }
                anyRendered = true;
            }

            // Underutilized Refining group (last in order)
            if (underutilized.Count > 0)
            {
                if (needsLeadingNewline || anyRendered) builder.Append("\n", TextColor);
                builder.Append("Underutilized Refining\n", HeaderColor);

                foreach (var row in underutilized)
                {
                    builder.Append("  " + row.SourceName + " -- " + row.ProcessDetails + "\n", TextColor);
                }
                anyRendered = true;
            }

            return anyRendered;
        }

        // ----- Activity Section -----

        private static bool RenderActivitySection(RtfBuilder builder, List<ActivityRow> rows,
            Colony colony, PlayerContext playerContext, bool needsLeadingNewline)
        {
            // Split into non-repeating (Manufacturing, CommodityManufacturing, Research)
            // and repeating (Mining, Refining) which get aggregated
            var nonRepeating = rows
                .Where(r => r.Type == ActivityType.Manufacturing ||
                            r.Type == ActivityType.CommodityManufacturing ||
                            r.Type == ActivityType.Research)
                .OrderBy(r => r.GetSecondsRemaining())
                .ToList();

            var miningRows = rows.Where(r => r.Type == ActivityType.Mining).ToList();
            var refiningRows = rows.Where(r => r.Type == ActivityType.Refining).ToList();

            bool anyRendered = false;

            // Non-repeating activity rows by type
            anyRendered |= RenderNonRepeatingGroup(builder, nonRepeating,
                ActivityType.Manufacturing, "Manufacturing", colony, needsLeadingNewline || anyRendered);
            anyRendered |= RenderNonRepeatingGroup(builder, nonRepeating,
                ActivityType.CommodityManufacturing, "Commodity Manufacturing", colony, needsLeadingNewline || anyRendered);
            anyRendered |= RenderNonRepeatingGroup(builder, nonRepeating,
                ActivityType.Research, "Research", colony, needsLeadingNewline || anyRendered);

            // Aggregated mining
            anyRendered |= RenderMiningAggregation(builder, colony, playerContext, needsLeadingNewline || anyRendered);

            // Aggregated refining
            anyRendered |= RenderRefiningAggregation(builder, colony, playerContext, needsLeadingNewline || anyRendered);

            return anyRendered;
        }

        private static bool RenderNonRepeatingGroup(RtfBuilder builder, List<ActivityRow> allRows,
            ActivityType type, string header, Colony colony, bool needsLeadingNewline)
        {
            var rows = allRows.Where(r => r.Type == type).ToList();
            if (rows.Count == 0) return false;

            if (needsLeadingNewline) builder.Append("\n", TextColor);
            builder.Append(header + "\n", HeaderColor);

            foreach (var row in rows)
            {
                builder.Append("  " + row.SourceName + " -- " + row.ProcessDetails + "\n", TextColor);

                // Completion time
                builder.Append("    ", TextColor);
                if (type == ActivityType.Manufacturing || type == ActivityType.CommodityManufacturing)
                {
                    RenderManufacturingCompletionTime(builder, row, colony);
                }
                else
                {
                    // Research: single completion time
                    builder.Append(row.GetTimeRemainingString(), CountdownColor);
                    if (row.CountDown != null)
                    {
                        string localTime = row.CountDown.EndTime.ToLocalTime().ToString("HH:mm ddd");
                        builder.Append(" (" + localTime + ")", CompletionTimeColor);
                    }
                    builder.Append("\n", TextColor);
                }
            }

            return true;
        }

        // ----- Manufacturing Batch Completion -----

        private static void RenderManufacturingCompletionTime(RtfBuilder builder, ActivityRow row, Colony colony)
        {
            // Next item completion
            builder.Append("Next: ", DetailColor);
            builder.Append(row.GetTimeRemainingString(), CountdownColor);
            if (row.CountDown != null)
            {
                string localTime = row.CountDown.EndTime.ToLocalTime().ToString("HH:mm ddd");
                builder.Append(" (" + localTime + ")", CompletionTimeColor);
            }
            builder.Append("\n", TextColor);

            // Batch completion -- find the matching structure
            if (colony.Structures == null || row.CountDown == null) return;

            var structure = FindMatchingStructure(colony, row);
            if (structure == null) return;

            if (structure.ManufacturingQuantity <= 1) return;
            if (structure.ProcessCompletionTime == null) return;
            if (structure.ProcessCompletionTime.RepeatIntervalSeconds <= 0) return;

            int remainingCycles = structure.ManufacturingQuantity - structure.ManufacturingCompleted - 1;
            if (remainingCycles <= 0) return;

            long currentCycleRemaining = Math.Max(0, row.CountDown.TimeRemaining);
            long batchSeconds = remainingCycles * structure.ProcessCompletionTime.RepeatIntervalSeconds + currentCycleRemaining;

            builder.Append("    Batch: ", DetailColor);
            builder.Append(ActivityRow.FormatSeconds(batchSeconds), CountdownColor);
            DateTime batchEnd = DateTime.UtcNow.AddSeconds(batchSeconds).ToLocalTime();
            builder.Append(" (" + batchEnd.ToString("HH:mm ddd") + ")", CompletionTimeColor);
            builder.Append("\n", TextColor);
        }

        /// <summary>
        /// Finds the ColonyStructure that matches an ActivityRow by comparing
        /// the ProcessCompletionTime reference.
        /// </summary>
        private static ColonyStructure FindMatchingStructure(Colony colony, ActivityRow row)
        {
            if (row.CountDown == null || colony.Structures == null) return null;

            foreach (var structure in colony.Structures)
            {
                if (structure.ProcessCompletionTime == row.CountDown)
                    return structure;
            }
            return null;
        }

        // ----- Mining Aggregation -----

        private static bool RenderMiningAggregation(RtfBuilder builder, Colony colony,
            PlayerContext playerContext, bool needsLeadingNewline)
        {
            if (colony.Structures == null) return false;

            // Collect active miners and compute rates grouped by (Resource, Purity)
            var miningGroups = new Dictionary<string, (string resource, string purity, decimal totalRate)>();

            int extractionFocusLevel = GetExtractionFocusLevel(colony, playerContext);

            foreach (var structure in colony.Structures)
            {
                Blueprint blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.MiningRig) continue;
                if (structure.ProcessCompletionTime == null || structure.ProcessCompletionTime.TimeRemaining <= 0) continue;
                if (string.IsNullOrEmpty(structure.MiningSurvey) || string.IsNullOrEmpty(structure.MiningSurveyResource)) continue;

                Survey survey = playerContext.FindSurvey(structure.MiningSurvey);
                if (survey == null || !survey.Resources.ContainsKey(structure.MiningSurveyResource)) continue;

                SurveyResource resource = survey.Resources[structure.MiningSurveyResource];
                decimal amount;
                if (!decimal.TryParse(resource.Amount, out amount)) continue;

                decimal rate = amount * (1.0m + extractionFocusLevel * 0.01m);
                string key = resource.Resource + "|" + resource.Purity;

                if (miningGroups.ContainsKey(key))
                {
                    var existing = miningGroups[key];
                    miningGroups[key] = (existing.resource, existing.purity, existing.totalRate + rate);
                }
                else
                {
                    miningGroups[key] = (resource.Resource, resource.Purity, rate);
                }
            }

            if (miningGroups.Count == 0) return false;

            if (needsLeadingNewline) builder.Append("\n", TextColor);
            builder.Append("Mining\n", HeaderColor);

            foreach (var group in miningGroups.Values.OrderBy(g => g.resource).ThenBy(g => g.purity))
            {
                int displayRate = (int)Math.Round(group.totalRate);
                builder.Append($"  {group.resource} ({group.purity}) -- {group.totalRate:F2}/h\n", TextColor);
            }

            return true;
        }

        private static int GetExtractionFocusLevel(Colony colony, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(colony.OwnerUUID)) return 0;

            var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == colony.OwnerUUID);
            if (owner == null) return 0;

            return owner.GetSkill(SkillName.ExtractionFocus).Level;
        }

        // ----- Refining Aggregation -----

        private static bool RenderRefiningAggregation(RtfBuilder builder, Colony colony,
            PlayerContext playerContext, bool needsLeadingNewline)
        {
            if (colony.Structures == null) return false;

            var refiningGroups = new Dictionary<string, (string resource, string purity, int count, int totalConsume, int totalProduce, string outputResource)>();

            foreach (var structure in colony.Structures)
            {
                Blueprint blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.Refinery) continue;
                if (structure.ProcessCompletionTime == null || structure.ProcessCompletionTime.TimeRemaining <= 0) continue;
                if (string.IsNullOrEmpty(structure.RefiningResource) || string.IsNullOrEmpty(structure.RefiningResourcePurity)) continue;

                string key = structure.RefiningResource + "|" + structure.RefiningResourcePurity;

                int consumeRate;
                int produceRate;
                string outputResource;

                var recipe = RefiningRecipes.FindByInput(structure.RefiningResource, structure.RefiningResourcePurity);
                if (recipe != null)
                {
                    consumeRate = recipe.ConsumeRate;
                    produceRate = recipe.ProduceRate;
                    outputResource = recipe.OutputResource;
                }
                else
                {
                    consumeRate = GameConstants.RefiningBaseRate;
                    produceRate = GetRefiningOutputRate(structure.RefiningResourcePurity, consumeRate);
                    outputResource = structure.RefiningResource;
                }

                if (refiningGroups.ContainsKey(key))
                {
                    var existing = refiningGroups[key];
                    refiningGroups[key] = (existing.resource, existing.purity,
                        existing.count + 1, existing.totalConsume + consumeRate,
                        existing.totalProduce + produceRate, existing.outputResource);
                }
                else
                {
                    refiningGroups[key] = (structure.RefiningResource, structure.RefiningResourcePurity,
                        1, consumeRate, produceRate, outputResource);
                }
            }

            if (refiningGroups.Count == 0) return false;

            if (needsLeadingNewline) builder.Append("\n", TextColor);
            builder.Append("Refining\n", HeaderColor);

            foreach (var group in refiningGroups.Values.OrderBy(g => g.resource).ThenBy(g => g.purity))
            {
                builder.Append($"  {group.count}x {group.resource} ({group.purity}) -- {group.totalConsume:F2}:{group.totalProduce:F2} {group.outputResource}\n", TextColor);
            }

            return true;
        }

        private static int GetRefiningOutputRate(string purity, int baseRate)
        {
            switch (purity)
            {
                case "Low": return baseRate;
                case "Medium": return baseRate * 3;
                case "High": return baseRate * 5;
                default: return baseRate;
            }
        }
    }
}
