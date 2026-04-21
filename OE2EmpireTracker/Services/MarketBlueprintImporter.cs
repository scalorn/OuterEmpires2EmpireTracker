using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;
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
            BlueprintPropertyKeys.ManufactureRunTime,
            GameConstants.PropPowerRequired
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

                // Route: Evo 0 blueprints are always global regardless of seller
                bool isGlobal = bp.Evolution == 0
                    || string.Equals(mb.SellerName, "Government", StringComparison.OrdinalIgnoreCase);
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
                    ? empireContext.GlobalBlueprintList
                    : playerContext.BlueprintList;

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
                    bp.UUID = isGlobal
                        ? DeterministicUUID.Generate(bp)
                        : Guid.NewGuid().ToString();
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
                empireContext.WriteContext();
                Log.Info("Persisted global blueprint changes");
            }
            if (playerChanged)
            {
                playerContext.WriteContext();
                Log.Info("Persisted player blueprint changes");
            }

            Log.Info("Import complete: {0} created, {1} updated, {2} skipped",
                result.CreatedCount, result.UpdatedCount, result.SkippedCount);

            return result;
        }

        internal static Models.Blueprint FindByDedupKey(
            IList<Models.Blueprint> list,
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

        /// <summary>
        /// Determines whether a blueprint should be stored in the global or player list.
        /// Returns true for global, false for player.
        /// </summary>
        internal static bool IsGlobalRoute(int evolution, bool hasCurrentPlayer)
        {
            return evolution == 0 || !hasCurrentPlayer;
        }

        /// <summary>
        /// Returns true when the parsed blueprint has resources but is missing
        /// the key dedup fields, indicating it came from the game's resources tab.
        /// </summary>
        internal static bool IsResourcesOnlyImport(Models.Blueprint bp)
        {
            return bp.Resources != null && bp.Resources.Count > 0
                && string.IsNullOrEmpty(bp.BluePrintType)
                && bp.Class == 0
                && string.IsNullOrEmpty(bp.TechLevel);
        }

        /// <summary>
        /// Merges resources (and any parsed properties) from incoming into target,
        /// preserving all existing scalar fields and protected properties.
        /// </summary>
        internal static void MergeResourcesOnly(Models.Blueprint target, Models.Blueprint incoming)
        {
            // Replace resources
            target.Resources = incoming.Resources;

            // Merge properties using the same protected-property logic as UpdateExisting
            if (incoming.Properties != null && incoming.Properties.Count > 0)
            {
                // Collect protected values from target before merge
                var preservedProps = new Dictionary<string, string>();
                foreach (var protectedKey in ProtectedProperties)
                {
                    string existingValue;
                    if (target.Properties != null
                        && target.Properties.getString(protectedKey, null, out existingValue)
                        && existingValue != null)
                    {
                        preservedProps[protectedKey] = existingValue;
                    }
                }

                // Merge incoming properties into target (add/overwrite non-protected keys)
                if (target.Properties == null)
                {
                    target.Properties = new PropertyBag();
                }
                foreach (var kvp in incoming.Properties.Properties)
                {
                    if (!ProtectedProperties.Contains(kvp.Key)
                        || !preservedProps.ContainsKey(kvp.Key))
                    {
                        target.Properties.setProperty(kvp.Key, kvp.Value);
                    }
                }

                // Restore protected properties that existed before
                foreach (var kvp in preservedProps)
                {
                    target.Properties.setProperty(kvp.Key, kvp.Value);
                }
            }

            // Do NOT overwrite any scalar fields
        }

        internal static void UpdateExisting(Models.Blueprint existing, Models.Blueprint incoming)
        {
            // Preserve protected scalar fields: UUID, OwnerUUID, NickName, CopyCost, TechLevel, Description
            // (we simply don't overwrite them)

            // Merge properties -- add/overwrite incoming keys but preserve existing keys
            // not present in incoming. This prevents a partial parse (e.g. resources page
            // that only extracts 1 property) from wiping out a full property set.
            if (incoming.Properties != null && incoming.Properties.Count > 0)
            {
                Log.Info("UpdateExisting: merging properties ({0} incoming into {1} existing) for {2} (hashcode={3})",
                    incoming.Properties.Count, existing.Properties?.Count ?? 0, existing.Name, existing.GetHashCode());

                if (existing.Properties == null)
                    existing.Properties = new PropertyBag();

                foreach (var kvp in incoming.Properties.Properties)
                {
                    if (!ProtectedProperties.Contains(kvp.Key))
                    {
                        existing.Properties.setProperty(kvp.Key, kvp.Value);
                    }
                    else if (!existing.Properties.ContainsKey(kvp.Key))
                    {
                        // Protected property, but existing doesn't have it yet — write it
                        existing.Properties.setProperty(kvp.Key, kvp.Value);
                    }
                }
            }
            else
            {
                Log.Info("UpdateExisting: incoming has no properties, preserving existing ({0} props) for {1}",
                    existing.Properties?.Count ?? 0, existing.Name);
            }

            // Merge resources -- add/overwrite incoming keys but preserve existing keys
            // not present in incoming. Same rationale as properties.
            if (incoming.Resources != null && incoming.Resources.Count > 0)
            {
                Log.Info("UpdateExisting: merging resources ({0} incoming into {1} existing) for {2}",
                    incoming.Resources.Count, existing.Resources?.Count ?? 0, existing.Name);

                if (existing.Resources == null)
                    existing.Resources = new Dictionary<string, string>();

                foreach (var kvp in incoming.Resources)
                {
                    existing.Resources[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                Log.Info("UpdateExisting: incoming has no resources, preserving existing ({0} resources) for {1}",
                    existing.Resources?.Count ?? 0, existing.Name);
            }

            // Overwrite non-protected scalar fields from incoming, but only if the
            // incoming value is non-default. Partial parses (e.g. resources tab) produce
            // default values (Class=0, empty BluePrintType) that should not overwrite real data.
            if (incoming.Evolution > 0)
                existing.Evolution = incoming.Evolution;
            if (incoming.Class > 0)
                existing.Class = incoming.Class;
            if (!string.IsNullOrEmpty(incoming.BluePrintType))
                existing.BluePrintType = incoming.BluePrintType;
            if (!string.IsNullOrEmpty(incoming.Name))
                existing.Name = incoming.Name;
        }
    }
}
