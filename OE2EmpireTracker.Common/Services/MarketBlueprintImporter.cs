using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    public static class MarketBlueprintImporter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
                var existing = BlueprintService.FindUnambiguousMatch(targetList, bp);

                if (existing != null)
                {
                    BlueprintService.UpdateExisting(existing, bp);
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
                            BlueprintService.UpdateExisting(alreadyExists, bp);
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

        // ─────────────────────────────────────────────────────────────────────
        // Delegating stubs — logic now lives in BlueprintService.
        // These remain for backward compatibility with callers that reference
        // MarketBlueprintImporter.X() (will be removed in a future cleanup).
        // ─────────────────────────────────────────────────────────────────────

        internal static Models.Blueprint FindByDedupKey(
            IEnumerable<Models.Blueprint> list,
            Models.Blueprint bp)
        {
            return BlueprintService.FindByDedupKey(list, bp);
        }

        internal static Models.Blueprint FindUnambiguousMatch(
            IEnumerable<Models.Blueprint> list,
            Models.Blueprint bp)
        {
            return BlueprintService.FindUnambiguousMatch(list, bp);
        }

        internal static Models.Blueprint FindBestMatch(
            IEnumerable<Models.Blueprint> list,
            Models.Blueprint incoming)
        {
            return BlueprintService.FindBestMatch(list, incoming);
        }

        internal static int ScorePropertyMatch(Models.Blueprint existing, Models.Blueprint incoming)
        {
            return BlueprintService.ScorePropertyMatch(existing, incoming);
        }

        internal static bool IsGlobalRoute(int evolution, bool hasCurrentPlayer)
        {
            return BlueprintService.IsGlobalRoute(evolution, hasCurrentPlayer);
        }

        internal static bool IsResourcesOnlyImport(Models.Blueprint bp)
        {
            return BlueprintService.IsResourcesOnlyImport(bp);
        }

        internal static void MergeResourcesOnly(Models.Blueprint target, Models.Blueprint incoming)
        {
            BlueprintService.MergeResourcesOnlyStatic(target, incoming);
        }

        internal static void UpdateExisting(Models.Blueprint existing, Models.Blueprint incoming)
        {
            BlueprintService.UpdateExisting(existing, incoming);
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
