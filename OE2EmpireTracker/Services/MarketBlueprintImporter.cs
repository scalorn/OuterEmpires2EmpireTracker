using NLog;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    public enum ImportAction { Created, Updated, Skipped }

    public class ImportResultEntry
    {
        public string Name { get; set; }
        public int Evolution { get; set; }
        public string BluePrintType { get; set; }
        public int Class { get; set; }
        public string TechLevel { get; set; }
        public string SellerName { get; set; }
        public ImportAction Action { get; set; }
        public string UUID { get; set; }
        public string SkipReason { get; set; }
        public string Storage { get; set; }
    }

    public class ImportResult
    {
        public List<ImportResultEntry> Entries { get; set; } = new List<ImportResultEntry>();
        public int CreatedCount => Entries.Count(e => e.Action == ImportAction.Created);
        public int UpdatedCount => Entries.Count(e => e.Action == ImportAction.Updated);
        public int SkippedCount => Entries.Count(e => e.Action == ImportAction.Skipped);
    }

    public static class MarketBlueprintImporter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Protected property names that are preserved when updating an existing blueprint.
        /// </summary>
        private static readonly HashSet<string> ProtectedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Manufacture Run Time",
            "Power Required"
        };

        public static ImportResult Import(
            List<MarketBlueprint> marketBlueprints,
            PlayerContext playerContext,
            EmpireContext empireContext)
        {
            var result = new ImportResult();
            bool globalChanged = false;
            bool playerChanged = false;

            foreach (var mb in marketBlueprints)
            {
                var bp = mb.Blueprint;
                var entry = new ImportResultEntry
                {
                    Name = bp.Name,
                    Evolution = bp.Evolution,
                    BluePrintType = bp.BluePrintType,
                    Class = bp.Class,
                    TechLevel = bp.TechLevel,
                    SellerName = mb.SellerName
                };

                // Skip unexpanded listings: zero properties, no BluePrintType
                if ((bp.Properties == null || bp.Properties.Count == 0)
                    && string.IsNullOrEmpty(bp.BluePrintType))
                {
                    entry.Action = ImportAction.Skipped;
                    entry.SkipReason = "Unexpanded listing";
                    Log.Info("Skipped unexpanded listing: {0}", bp.Name);
                    result.Entries.Add(entry);
                    continue;
                }

                // Route by seller
                bool isGlobal = string.Equals(mb.SellerName, "Government", StringComparison.OrdinalIgnoreCase);
                entry.Storage = isGlobal ? "Global" : "Player";

                if (!isGlobal && string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
                {
                    entry.Action = ImportAction.Skipped;
                    entry.SkipReason = "No current player selected";
                    Log.Warn("Skipped player blueprint (no current player): {0}", bp.Name);
                    result.Entries.Add(entry);
                    continue;
                }

                var targetList = isGlobal
                    ? empireContext.globalBlueprintList
                    : playerContext.blueprintList;

                // Dedup by Name + Evolution + BluePrintType + Class + TechLevel
                var existing = FindByDedupKey(targetList, bp);

                if (existing != null)
                {
                    UpdateExisting(existing, bp);
                    entry.Action = ImportAction.Updated;
                    entry.UUID = existing.UUID;
                    Log.Info("Updated {0} blueprint: {1} Ev{2} {3} C{4} TL={5} UUID={6}",
                        entry.Storage, bp.Name, bp.Evolution, bp.BluePrintType, bp.Class, bp.TechLevel, existing.UUID);

                    if (isGlobal) globalChanged = true;
                    else playerChanged = true;
                }
                else
                {
                    bp.UUID = Guid.NewGuid().ToString();
                    if (!isGlobal)
                    {
                        bp.OwnerUUID = playerContext.CurrentPlayerUUID;
                    }
                    targetList.Add(bp);
                    entry.Action = ImportAction.Created;
                    entry.UUID = bp.UUID;
                    Log.Info("Created {0} blueprint: {1} Ev{2} {3} C{4} TL={5} UUID={6}",
                        entry.Storage, bp.Name, bp.Evolution, bp.BluePrintType, bp.Class, bp.TechLevel, bp.UUID);

                    if (isGlobal) globalChanged = true;
                    else playerChanged = true;
                }

                result.Entries.Add(entry);
            }

            // Persist once at end
            if (globalChanged)
            {
                empireContext.writeContext();
                Log.Info("Persisted global blueprint changes");
            }
            if (playerChanged)
            {
                playerContext.writeContext();
                Log.Info("Persisted player blueprint changes");
            }

            Log.Info("Import complete: {0} created, {1} updated, {2} skipped",
                result.CreatedCount, result.UpdatedCount, result.SkippedCount);

            return result;
        }

        private static Models.Blueprint FindByDedupKey(
            System.ComponentModel.BindingList<Models.Blueprint> list,
            Models.Blueprint bp)
        {
            if (list == null) return null;

            return list.FirstOrDefault(existing =>
                string.Equals(existing.Name, bp.Name, StringComparison.Ordinal)
                && existing.Evolution == bp.Evolution
                && string.Equals(existing.BluePrintType, bp.BluePrintType, StringComparison.Ordinal)
                && existing.Class == bp.Class
                && string.Equals(existing.TechLevel, bp.TechLevel, StringComparison.Ordinal));
        }

        private static void UpdateExisting(Models.Blueprint existing, Models.Blueprint incoming)
        {
            // Preserve protected scalar fields: UUID, OwnerUUID, NickName, CopyCost, TechLevel, Description
            // (we simply don't overwrite them)

            // Overwrite properties, preserving protected property keys
            if (incoming.Properties != null)
            {
                // Collect protected values from existing before overwrite
                var preservedProps = new Dictionary<string, string>();
                foreach (var protectedKey in ProtectedProperties)
                {
                    string existingValue;
                    if (existing.Properties != null
                        && existing.Properties.getString(protectedKey, null, out existingValue)
                        && existingValue != null)
                    {
                        preservedProps[protectedKey] = existingValue;
                    }
                }

                // Replace properties with incoming
                existing.Properties = incoming.Properties;

                // Restore protected properties that existed before but may not be in incoming
                foreach (var kvp in preservedProps)
                {
                    if (!existing.Properties.ContainsKey(kvp.Key))
                    {
                        existing.Properties.setProperty(kvp.Key, kvp.Value);
                    }
                }
            }

            // Overwrite resources
            if (incoming.Resources != null)
            {
                existing.Resources = incoming.Resources;
            }

            // Overwrite non-protected scalar fields from incoming
            existing.Evolution = incoming.Evolution;
            existing.Class = incoming.Class;
            existing.BluePrintType = incoming.BluePrintType;
            existing.Name = incoming.Name;
        }
    }
}
