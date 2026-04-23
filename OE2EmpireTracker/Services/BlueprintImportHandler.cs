using System;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Handles individual blueprint import routing: classify the import type,
    /// find the target blueprint (selected match, dedup match, or new),
    /// and merge + persist. Used by FormBlueprintV2.
    /// </summary>
    public static class BlueprintImportHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public enum ImportType
        {
            /// <summary>Only resources parsed, no properties (e.g. resources tab).</summary>
            ResourcesOnly,
            /// <summary>Full import with name, properties, and/or resources.</summary>
            Full,
            /// <summary>No name parsed from clipboard — fallback behavior.</summary>
            NoName
        }

        /// <summary>
        /// Classifies the parsed temp blueprint to determine the import path.
        /// </summary>
        public static ImportType ClassifyImport(Blueprint tempBP)
        {
            if (MarketBlueprintImporter.IsResourcesOnlyImport(tempBP))
                return ImportType.ResourcesOnly;
            if (string.IsNullOrEmpty(tempBP.Name))
                return ImportType.NoName;
            return ImportType.Full;
        }

        /// <summary>
        /// Logs the parsed temp blueprint data for diagnostics.
        /// </summary>
        public static void LogParsedBlueprint(Blueprint tempBP)
        {
            Log.Info("=== Individual Blueprint Import ===");
            Log.Info(
                "  Parsed: name='{0}' evo={1} type='{2}' class={3} tech='{4}'",
                tempBP.Name,
                tempBP.Evolution,
                tempBP.BluePrintType,
                tempBP.Class,
                tempBP.TechLevel);
            Log.Info(
                "  Parsed: {0} properties, {1} resources",
                tempBP.Properties?.Count ?? 0,
                tempBP.Resources?.Count ?? 0);
            if (tempBP.Properties != null)
                foreach (var prop in tempBP.Properties.Properties)
                    Log.Info("    prop: {0} = {1}", prop.Key, prop.Value);
            if (tempBP.Resources != null)
                foreach (var res in tempBP.Resources)
                    Log.Info("    resource: {0} = {1}", res.Key, res.Value);
        }

        /// <summary>
        /// Result of FindTarget — contains the matched blueprint (or null) and routing info.
        /// </summary>
        public class FindTargetResult
        {
            public Blueprint Target { get; set; }
            public bool IsNew => Target == null;
            public bool IsGlobal { get; set; }
            public bool IsSelectedMatch { get; set; }
        }

        /// <summary>
        /// Finds the target blueprint for a full import. Checks selected match first,
        /// then dedup in the appropriate list.
        /// </summary>
        public static FindTargetResult FindTarget(
            Blueprint tempBP, Blueprint selected,
            PlayerContext pc, EmpireContext ec)
        {
            Log.Info(
                "  Selected blueprint: name='{0}' evo={1} type='{2}' class={3} tech='{4}' UUID={5}",
                selected.Name,
                selected.Evolution,
                selected.BluePrintType,
                selected.Class,
                selected.TechLevel,
                selected.UUID ?? "(null)");

            // Relaxed match: Name + Evolution required.
            // BluePrintType matches if equal OR if the existing has no type.
            bool nameMatch = string.Equals(selected.Name, tempBP.Name, StringComparison.Ordinal);
            bool evoMatch = selected.Evolution == tempBP.Evolution;
            bool typeMatch = string.Equals(selected.BluePrintType, tempBP.BluePrintType, StringComparison.Ordinal)
                || string.IsNullOrEmpty(selected.BluePrintType);
            bool selectedMatch = !string.IsNullOrEmpty(selected.UUID)
                && !string.IsNullOrEmpty(tempBP.Name)
                && nameMatch && evoMatch && typeMatch;

            Log.Info(
                "  Selected match check: name={0} evo={1} type={2} hasUUID={3} hasName={4} => {5}",
                nameMatch,
                evoMatch,
                typeMatch,
                !string.IsNullOrEmpty(selected.UUID),
                !string.IsNullOrEmpty(tempBP.Name),
                selectedMatch);

            if (selectedMatch)
            {
                bool isGlobal = ec.GlobalBlueprintList.Any(b => b.UUID == selected.UUID);
                return new FindTargetResult
                {
                    Target = selected,
                    IsGlobal = isGlobal,
                    IsSelectedMatch = true
                };
            }

            // No selected match — route via market logic
            bool hasCurrentPlayer = !string.IsNullOrEmpty(pc.CurrentPlayerUUID);
            bool globalRoute = MarketBlueprintImporter.IsGlobalRoute(tempBP.Evolution, hasCurrentPlayer);
            Log.Info(
                "  No selected match -- routing: isGlobal={0} (evo={1}, hasPlayer={2})",
                globalRoute,
                tempBP.Evolution,
                hasCurrentPlayer);

            var targetList = globalRoute
                ? ec.GlobalBlueprintList
                : pc.BlueprintList;

            var existing = MarketBlueprintImporter.FindByDedupKey(targetList, tempBP);
            Log.Info(
                "  FindByDedupKey in {0} list ({1} blueprints): {2}",
                globalRoute ? "global" : "player",
                targetList.Count,
                existing != null ? $"MATCH UUID={existing.UUID}" : "NO MATCH");

            return new FindTargetResult
            {
                Target = existing,
                IsGlobal = globalRoute,
                IsSelectedMatch = false
            };
        }

        /// <summary>
        /// Merges the incoming blueprint into the target (or creates new) and persists.
        /// Returns the final blueprint object.
        /// </summary>
        public static Blueprint MergeAndPersist(
            FindTargetResult findResult, Blueprint tempBP,
            PlayerContext pc, EmpireContext ec)
        {
            Blueprint importedBP;

            if (findResult.Target != null)
            {
                MarketBlueprintImporter.UpdateExisting(findResult.Target, tempBP);
                importedBP = findResult.Target;
                Log.Info(
                    "Blueprint updated via dedup ({0}): {1} Ev{2} {3}",
                    findResult.IsSelectedMatch ? "selected match" : "list match",
                    importedBP.Name,
                    importedBP.Evolution,
                    importedBP.BluePrintType);
            }
            else
            {
                tempBP.UUID = findResult.IsGlobal
                    ? DeterministicUUID.Generate(tempBP)
                    : Guid.NewGuid().ToString();
                if (!findResult.IsGlobal)
                    tempBP.OwnerUUID = pc.CurrentPlayerUUID;

                if (findResult.IsGlobal)
                    ec.AddGlobalBlueprint(tempBP);
                else
                    pc.AddBlueprint(tempBP);
                importedBP = tempBP;
                Log.Info(
                    "New blueprint created: {0} Ev{1} {2} -> {3}",
                    importedBP.Name,
                    importedBP.Evolution,
                    importedBP.BluePrintType,
                    findResult.IsGlobal ? "Global" : "Player");
            }

            // Persist
            Log.Info(
                "Pre-save: {0} UUID={1} props={2} resources={3}",
                importedBP.Name,
                importedBP.UUID,
                importedBP.Properties?.Count ?? 0,
                importedBP.Resources?.Count ?? 0);

            if (findResult.IsGlobal)
                ec.WriteContext();
            else
                pc.WriteContext();

            Log.Info("Post-save complete for {0}", importedBP.Name);

            pc.OnBlueprintDataChanged(importedBP.UUID);
            return importedBP;
        }
    }
}
