using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Blueprint mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus deserialization and migration) mutates Blueprint objects.
    /// </summary>
    public class BlueprintService
    {
        /// <summary>
        /// Protected property names that are preserved when merging resources into an existing blueprint.
        /// </summary>
        internal static readonly HashSet<string> ProtectedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BlueprintPropertyKeys.ManufactureRunTime,
            GameConstants.PropPowerRequired
        };

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;

        public BlueprintService(PlayerContext playerContext, EmpireContext empireContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _empireContext = empireContext ?? throw new ArgumentNullException(nameof(empireContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing blueprint, persists, and fires the change event.
        /// </summary>
        public ReadOnlyBlueprint Update(string uuid, BlueprintUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var bp = _playerContext.FindMutableBlueprint(uuid)
                  ?? _empireContext.FindMutableGlobalBlueprint(uuid);
            if (bp == null) throw new InvalidOperationException("Blueprint not found: " + uuid);

            Log.Info(
                "BlueprintService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid, bp.Name, request.Name);

            bp.Name = request.Name;
            bp.NickName = request.NickName;
            bp.Description = request.Description;
            bp.BluePrintType = request.BluePrintType;
            bp.Evolution = request.Evolution;
            bp.TechLevel = request.TechLevel;
            bp.Class = request.Class;
            bp.CopyCost = request.CopyCost;
            bp.BaseBlueprintUUID = request.BaseBlueprintUUID;

            // Rebuild properties
            bp.Properties = new PropertyBag();
            if (request.Properties != null)
            {
                foreach (var kvp in request.Properties)
                    bp.Properties.SetProperty(kvp.Key, kvp.Value);
            }

            // Rebuild resources
            bp.Resources = request.Resources != null
                ? new Dictionary<string, string>(request.Resources)
                : new Dictionary<string, string>();

            // Persist to the correct context
            bool isGlobal = _empireContext.GlobalBlueprintList.Contains(bp);
            if (isGlobal)
                _empireContext.WriteContext();
            else
                _playerContext.WriteContext();

            _playerContext.OnBlueprintDataChanged(uuid);
            return new ReadOnlyBlueprint(bp);
        }

        /// <summary>
        /// Creates a new blueprint, assigns a UUID, adds to the appropriate list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyBlueprint Create(BlueprintCreateRequest request, bool isGlobal)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var bp = new Blueprint();
            bp.Name = request.Name;
            bp.NickName = request.NickName;
            bp.Description = request.Description;
            bp.BluePrintType = request.BluePrintType;
            bp.Evolution = request.Evolution;
            bp.TechLevel = request.TechLevel;
            bp.Class = request.Class;
            bp.CopyCost = request.CopyCost;
            bp.BaseBlueprintUUID = request.BaseBlueprintUUID;

            // Build properties
            bp.Properties = new PropertyBag();
            if (request.Properties != null)
            {
                foreach (var kvp in request.Properties)
                    bp.Properties.SetProperty(kvp.Key, kvp.Value);
            }

            // Build resources
            bp.Resources = request.Resources != null
                ? new Dictionary<string, string>(request.Resources)
                : new Dictionary<string, string>();

            // Assign UUID
            bp.UUID = isGlobal
                ? DeterministicUUID.Generate(bp)
                : Guid.NewGuid().ToString();

            Log.Info(
                "BlueprintService.Create: name='{0}' UUID={1} isGlobal={2}",
                bp.Name, bp.UUID, isGlobal);

            if (isGlobal)
            {
                bp.OwnerUUID = string.Empty;
                _empireContext.AddGlobalBlueprint(bp);
                _empireContext.WriteContext();
            }
            else
            {
                bp.OwnerUUID = _playerContext.CurrentPlayerUUID;
                _playerContext.AddBlueprint(bp);
                _playerContext.WriteContext();
            }

            _playerContext.OnBlueprintDataChanged(bp.UUID);
            return new ReadOnlyBlueprint(bp);
        }

        /// <summary>
        /// Removes a blueprint from the appropriate list, persists, and fires the change event.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));

            Log.Info("BlueprintService.Delete: UUID={0}", uuid);

            var bp = _playerContext.FindMutableBlueprint(uuid);
            if (bp != null)
            {
                _playerContext.RemoveBlueprint(bp);
                _playerContext.WriteContext();
            }
            else
            {
                var globalBp = _empireContext.FindMutableGlobalBlueprint(uuid);
                if (globalBp != null)
                {
                    _empireContext.RemoveGlobalBlueprint(globalBp);
                    _empireContext.WriteContext();
                }
                else
                {
                    Log.Warn("BlueprintService.Delete: blueprint not found UUID={0}", uuid);
                    return;
                }
            }

            _playerContext.OnBlueprintDataChanged(uuid);
        }

        /// <summary>
        /// Handles clipboard import: delegates to BlueprintImportHandler for routing and merge.
        /// Returns the final blueprint as ReadOnlyBlueprint.
        /// </summary>
        public ReadOnlyBlueprint Import(Blueprint temp, ReadOnlyBlueprint selectedTarget)
        {
            if (temp == null) throw new ArgumentNullException(nameof(temp));

            // Build a mutable selected blueprint for FindTarget compatibility
            Blueprint selectedMutable;
            if (selectedTarget != null)
            {
                selectedMutable = _playerContext.FindMutableBlueprint(selectedTarget.UUID)
                    ?? _empireContext.FindMutableGlobalBlueprint(selectedTarget.UUID)
                    ?? new Blueprint();
            }
            else
            {
                selectedMutable = new Blueprint();
            }

            var findResult = BlueprintImportHandler.FindTarget(
                temp, selectedMutable, _playerContext, _empireContext);

            var importedBP = BlueprintImportHandler.MergeAndPersist(
                findResult, temp, _playerContext, _empireContext);

            return new ReadOnlyBlueprint(importedBP);
        }

        /// <summary>
        /// Moves a player blueprint to the global list.
        /// </summary>
        public void MoveToGlobal(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));

            var bp = _playerContext.FindMutableBlueprint(uuid);
            if (bp == null)
            {
                Log.Warn("BlueprintService.MoveToGlobal: blueprint not found in player list UUID={0}", uuid);
                return;
            }

            Log.Info("BlueprintService.MoveToGlobal: UUID={0} name='{1}'", uuid, bp.Name);

            _playerContext.RemoveBlueprint(bp);
            bp.OwnerUUID = string.Empty;
            _empireContext.AddGlobalBlueprint(bp);

            _playerContext.WriteContext();
            _empireContext.WriteContext();
            _playerContext.OnBlueprintDataChanged(uuid);
        }

        /// <summary>
        /// Moves a global blueprint to the current player's list.
        /// </summary>
        public void MoveToPlayer(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));

            var bp = _empireContext.FindMutableGlobalBlueprint(uuid);
            if (bp == null)
            {
                Log.Warn("BlueprintService.MoveToPlayer: blueprint not found in global list UUID={0}", uuid);
                return;
            }

            Log.Info("BlueprintService.MoveToPlayer: UUID={0} name='{1}'", uuid, bp.Name);

            _empireContext.RemoveGlobalBlueprint(bp);
            bp.OwnerUUID = _playerContext.CurrentPlayerUUID;
            _playerContext.AddBlueprint(bp);

            _empireContext.WriteContext();
            _playerContext.WriteContext();
            _playerContext.OnBlueprintDataChanged(uuid);
        }

        /// <summary>
        /// Merges resources from a parsed temp blueprint into an existing blueprint.
        /// Used for resources-only clipboard import.
        /// </summary>
        public ReadOnlyBlueprint MergeResources(string uuid, Blueprint tempBP)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));
            if (tempBP == null) throw new ArgumentNullException(nameof(tempBP));

            var bp = _playerContext.FindMutableBlueprint(uuid)
                  ?? _empireContext.FindMutableGlobalBlueprint(uuid);
            if (bp == null) throw new InvalidOperationException("Blueprint not found: " + uuid);

            Log.Info("BlueprintService.MergeResources: UUID={0} name='{1}'", uuid, bp.Name);

            MergeResourcesOnly(bp, tempBP);

            bool isGlobal = _empireContext.GlobalBlueprintList.Contains(bp);
            if (isGlobal)
                _empireContext.WriteContext();
            else
                _playerContext.WriteContext();

            _playerContext.OnBlueprintDataChanged(uuid);
            return new ReadOnlyBlueprint(bp);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pure data methods (shared by MarketBlueprintImporter, CrateImporter,
        // and BlueprintImportHandler). No clipboard or UI dependency.
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds an existing blueprint by the dedup key (Name + Evolution + BluePrintType + Class + TechLevel).
        /// Returns the first match or null.
        /// </summary>
        internal static Blueprint FindByDedupKey(
            IEnumerable<Blueprint> list,
            Blueprint bp)
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
        internal static Blueprint FindUnambiguousMatch(
            IEnumerable<Blueprint> list,
            Blueprint bp)
        {
            if (list == null) return null;

            Blueprint first = null;
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
        internal static Blueprint FindBestMatch(
            IEnumerable<Blueprint> list,
            Blueprint incoming)
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

            Blueprint bestMatch = null;
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
        internal static int ScorePropertyMatch(Blueprint existing, Blueprint incoming)
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
        internal static bool IsResourcesOnlyImport(Blueprint bp)
        {
            return bp.Resources != null && bp.Resources.Count > 0
                && string.IsNullOrEmpty(bp.BluePrintType)
                && bp.Class == 0
                && string.IsNullOrEmpty(bp.TechLevel);
        }

        /// <summary>
        /// Fixes up game data quirks in flatpack blueprint properties.
        /// Reactor Core: "Power Required" is actually "Power Provided" in the game UI.
        /// </summary>
        internal static void FixupFlatpackProperties(Blueprint blueprint)
        {
            if (blueprint == null || string.IsNullOrEmpty(blueprint.BluePrintType))
                return;

            if (blueprint.BluePrintType == "Flatpacks/ReactorCore")
            {
                string powerValue;
                if (blueprint.Properties.GetString(GameConstants.PropPowerRequired, null, out powerValue)
                    && !string.IsNullOrEmpty(powerValue))
                {
                    string existingProvided;
                    blueprint.Properties.GetString(GameConstants.PropPowerProvided, null, out existingProvided);
                    if (string.IsNullOrEmpty(existingProvided))
                    {
                        blueprint.Properties.SetProperty(GameConstants.PropPowerProvided, powerValue);
                        blueprint.Properties.Remove(GameConstants.PropPowerRequired);
                        Log.Info(
                            "FixupFlatpackProperties: Reactor '{0}' — remapped Power Required={1} to Power Provided",
                            blueprint.Name,
                            powerValue);
                    }
                }
            }
        }

        /// <summary>
        /// Merges resources (and any parsed properties) from incoming into target,
        /// preserving all existing scalar fields and protected properties.
        /// Unlike UpdateExisting, this does NOT overwrite Name, Evolution, Class, or BluePrintType.
        /// </summary>
        internal static void MergeResourcesOnlyStatic(Blueprint target, Blueprint incoming)
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

        /// <summary>
        /// Merges resources (and any parsed properties) from incoming into target,
        /// preserving all existing scalar fields and protected properties.
        /// </summary>
        internal static void UpdateExisting(Blueprint existing, Blueprint incoming)
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

        /// <summary>
        /// Merges resources and non-protected properties from incoming into target,
        /// preserving all existing scalar fields and protected properties.
        /// </summary>
        private static void MergeResourcesOnly(Blueprint target, Blueprint incoming)
        {
            target.Resources = incoming.Resources;

            if (incoming.Properties != null && incoming.Properties.Count > 0)
            {
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

                foreach (var kvp in preservedProps)
                {
                    target.Properties.SetProperty(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
