using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    public enum ImportAction
    {
        Created,
        Updated,
        Skipped
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
                // Returns null when multiple candidates exist (ambiguous — create new)
                var existing = FindUnambiguousMatch(targetList, bp);

                if (existing != null)
                {
                    UpdateExisting(existing, bp);
                    entry.Action = ImportAction.Updated;
                    entry.UUID = existing.UUID;
                    Log.Info(
                        "Updated {0} blueprint: {1} Ev{2} {3} C{4} TL={5} UUID={6}",
                        entry.Storage,
                        bp.Name,
                        bp.Evolution,
                        bp.BluePrintType,
                        bp.Class,
                        bp.TechLevel,
                        existing.UUID);

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

                    // Check for UUID collision on global blueprints (deterministic UUID may already exist)
                    if (isGlobal)
                    {
                        var alreadyExists = empireContext.FindMutableGlobalBlueprint(bp.UUID);
                        if (alreadyExists != null)
                        {
                            UpdateExisting(alreadyExists, bp);
                            entry.Action = ImportAction.Updated;
                            entry.UUID = alreadyExists.UUID;
                            Log.Info(
                                "UUID collision — updated existing {0} blueprint: {1} Ev{2} {3} C{4} TL={5} UUID={6}",
                                entry.Storage,
                                bp.Name,
                                bp.Evolution,
                                bp.BluePrintType,
                                bp.Class,
                                bp.TechLevel,
                                alreadyExists.UUID);
                            globalChanged = true;
                            result.Entries.Add(entry);
                            continue;
                        }
                    }

                    if (isGlobal)
                        empireContext.AddGlobalBlueprint(bp);
                    else
                        playerContext.AddBlueprint(bp);
                    entry.Action = ImportAction.Created;
                    entry.UUID = bp.UUID;
                    Log.Info(
                        "Created {0} blueprint: {1} Ev{2} {3} C{4} TL={5} UUID={6}",
                        entry.Storage,
                        bp.Name,
                        bp.Evolution,
                        bp.BluePrintType,
                        bp.Class,
                        bp.TechLevel,
                        bp.UUID);

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

            Log.Info(
                "Import complete: {0} created, {1} updated, {2} skipped",
                result.CreatedCount,
                result.UpdatedCount,
                result.SkippedCount);

            return result;
        }

        internal static Models.Blueprint FindByDedupKey(
            IEnumerable<Models.Blueprint> list,
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
        /// Finds an unambiguous dedup match. Returns the existing blueprint only when
        /// exactly one candidate matches the dedup key. When multiple candidates exist
        /// (same Name+Evo+Type+Class+TechLevel but different evolution paths), returns
        /// null so the caller creates a new blueprint rather than guessing which to update.
        /// </summary>
        internal static Models.Blueprint FindUnambiguousMatch(
            IEnumerable<Models.Blueprint> list,
            Models.Blueprint bp)
        {
            if (list == null) return null;

            Models.Blueprint first = null;
            bool multiple = false;

            foreach (var existing in list)
            {
                if (string.Equals(existing.Name, bp.Name, StringComparison.Ordinal)
                    && existing.Evolution == bp.Evolution
                    && string.Equals(existing.BluePrintType, bp.BluePrintType, StringComparison.Ordinal)
                    && existing.Class == bp.Class
                    && string.Equals(existing.TechLevel, bp.TechLevel, StringComparison.Ordinal))
                {
                    if (first == null)
                    {
                        first = existing;
                    }
                    else
                    {
                        multiple = true;
                        break;
                    }
                }
            }

            if (multiple)
            {
                Log.Info(
                    "    FindUnambiguousMatch: multiple candidates for '{0}' Evo{1} — treating as new",
                    bp.Name,
                    bp.Evolution);
                return null;
            }

            return first;
        }

        /// <summary>
        /// Finds the best-matching existing blueprint for batch re-import dedup.
        /// Used by CrateImporter where we need to pair each incoming entry with its
        /// specific existing counterpart among multiple candidates sharing the same
        /// dedup key. Scores candidates by property similarity.
        /// For individual/market imports, use FindUnambiguousMatch instead.
        /// Returns null if no candidate matches the dedup key.
        /// </summary>
        internal static Models.Blueprint FindBestMatch(
            IEnumerable<Models.Blueprint> list,
            Models.Blueprint incoming)
        {
            if (list == null) return null;

            var candidates = list.Where(existing =>
                string.Equals(existing.Name, incoming.Name, StringComparison.Ordinal)
                && existing.Evolution == incoming.Evolution
                && string.Equals(existing.BluePrintType, incoming.BluePrintType, StringComparison.Ordinal)
                && existing.Class == incoming.Class
                && string.Equals(existing.TechLevel, incoming.TechLevel, StringComparison.Ordinal))
                .ToList();

            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0];

            // Multiple candidates — score each by property similarity
            Log.Info(
                "    FindBestMatch: {0} candidates for '{1}' Evo{2}, scoring by properties",
                candidates.Count,
                incoming.Name,
                incoming.Evolution);

            Models.Blueprint bestMatch = null;
            int bestScore = -1;

            foreach (var candidate in candidates)
            {
                int score = ScorePropertyMatch(candidate, incoming);
                Log.Info("      UUID={0} score={1}", candidate.UUID, score);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = candidate;
                }
            }

            if (bestScore > 0)
            {
                Log.Info("    FindBestMatch: best UUID={0} score={1}", bestMatch.UUID, bestScore);
                return bestMatch;
            }

            Log.Info("    FindBestMatch: no candidate scored > 0, treating as new");
            return null;
        }

        /// <summary>
        /// Scores how well an existing blueprint's properties match the incoming one.
        /// Compares normalized property values. Higher score = better match.
        /// </summary>
        internal static int ScorePropertyMatch(Models.Blueprint existing, Models.Blueprint incoming)
        {
            if (existing.Properties == null || incoming.Properties == null)
                return 0;

            int matches = 0;

            foreach (var kvp in incoming.Properties.Properties)
            {
                if (kvp.Key.StartsWith("_")) continue;

                string existingValue;
                if (existing.Properties.GetString(kvp.Key, null, out existingValue) && existingValue != null)
                {
                    if (string.Equals(existingValue, kvp.Value, StringComparison.Ordinal))
                    {
                        matches++;
                    }
                }
            }

            return matches;
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
                        && target.Properties.GetString(protectedKey, null, out existingValue)
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
                        target.Properties.SetProperty(kvp.Key, kvp.Value);
                    }
                }

                // Restore protected properties that existed before
                foreach (var kvp in preservedProps)
                {
                    target.Properties.SetProperty(kvp.Key, kvp.Value);
                }
            }

            // Do NOT overwrite any scalar fields
        }

        internal static void UpdateExisting(Models.Blueprint existing, Models.Blueprint incoming)
        {
            // Preserve protected scalar fields: UUID, OwnerUUID, NickName, CopyCost, TechLevel, Description
            // (we simply don't overwrite them)

            // Replace properties -- the incoming blueprint has the definitive property list.
            // After game rebalancing, old property keys that no longer exist must be removed.
            // Preserve internal properties (starting with _) that aren't in the incoming data.
            // For protected properties: if incoming has the key, keep the existing value
            // (prevents partial-parse overwrite); if incoming doesn't have the key, remove it
            // (the game dropped the property).
            // Properties are added in sorted order to maintain deterministic serialization.
            // When incoming has no properties (e.g. resources-only import), preserve existing.
            // Also preserve when incoming only has internal properties (starting with _),
            // which indicates a partial parse (e.g. resources tab with icon position).
            bool hasNonInternalProps = incoming.Properties != null
                && incoming.Properties.Properties.Keys.Any(k => !k.StartsWith("_"));
            if (incoming.Properties != null && incoming.Properties.Count > 0 && hasNonInternalProps)
            {
                Log.Info(
                    "UpdateExisting: replacing properties ({0} incoming, was {1} existing) for {2} (hashcode={3})",
                    incoming.Properties.Count,
                    existing.Properties?.Count ?? 0,
                    existing.Name,
                    existing.GetHashCode());

                // Collect internal properties from existing that should be preserved
                var internalProps = new Dictionary<string, string>();
                if (existing.Properties != null)
                {
                    foreach (var kvp in existing.Properties.Properties)
                    {
                        if (kvp.Key.StartsWith("_") && !incoming.Properties.ContainsKey(kvp.Key))
                        {
                            internalProps[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Collect protected property values from existing (only if incoming also has the key)
                var protectedValues = new Dictionary<string, string>();
                if (existing.Properties != null)
                {
                    foreach (var protectedKey in ProtectedProperties)
                    {
                        if (incoming.Properties.ContainsKey(protectedKey))
                        {
                            string existingValue;
                            if (existing.Properties.GetString(protectedKey, null, out existingValue)
                                && !string.IsNullOrEmpty(existingValue))
                            {
                                protectedValues[protectedKey] = existingValue;
                            }
                        }
                    }
                }

                // Build new property bag from incoming in sorted order
                var sortedKeys = incoming.Properties.Properties.Keys
                    .OrderBy(k => k, StringComparer.Ordinal).ToList();
                existing.Properties = new PropertyBag();

                foreach (var key in sortedKeys)
                {
                    if (protectedValues.ContainsKey(key))
                    {
                        existing.Properties.SetProperty(key, protectedValues[key]);
                    }
                    else
                    {
                        existing.Properties.SetProperty(key, incoming.Properties.Properties[key]);
                    }
                }

                // Restore internal properties in sorted order
                foreach (var kvp in internalProps.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    existing.Properties.SetProperty(kvp.Key, kvp.Value);
                }
            }
            else
            {
                Log.Info(
                    "UpdateExisting: incoming has no non-internal properties ({0} total, {1} internal), preserving existing ({2} props) for {3}",
                    incoming.Properties?.Count ?? 0,
                    incoming.Properties?.Properties.Keys.Count(k => k.StartsWith("_")) ?? 0,
                    existing.Properties?.Count ?? 0,
                    existing.Name);

                // Still merge any internal properties from incoming into existing
                if (incoming.Properties != null && existing.Properties != null)
                {
                    foreach (var kvp in incoming.Properties.Properties)
                    {
                        if (kvp.Key.StartsWith("_"))
                        {
                            existing.Properties.SetProperty(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            // Replace resources -- the incoming blueprint has the definitive resource list.
            // After game rebalancing, old resource keys that no longer exist must be removed.
            // When incoming has no resources (e.g. statistics-only import), preserve existing.
            if (incoming.Resources != null && incoming.Resources.Count > 0)
            {
                Log.Info(
                    "UpdateExisting: replacing resources ({0} incoming, was {1} existing) for {2}",
                    incoming.Resources.Count,
                    existing.Resources?.Count ?? 0,
                    existing.Name);

                if (existing.Resources != null)
                {
                    foreach (var kvp in existing.Resources)
                        Log.Debug("  existing resource: {0} = {1}", kvp.Key, kvp.Value);
                }

                foreach (var kvp in incoming.Resources)
                    Log.Debug("  incoming resource: {0} = {1}", kvp.Key, kvp.Value);

                existing.Resources = new Dictionary<string, string>(incoming.Resources);

                foreach (var kvp in existing.Resources)
                    Log.Debug("  final resource: {0} = {1}", kvp.Key, kvp.Value);
            }
            else
            {
                Log.Info(
                    "UpdateExisting: incoming has no resources, preserving existing ({0} resources) for {1}",
                    existing.Resources?.Count ?? 0,
                    existing.Name);
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
}
