using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Imports blueprints from a JSON file produced by the OE2 Blueprint Scraper
    /// (browser console script). Each entry contains name, evolution, techLevel,
    /// description, iconClass, properties, and resources scraped from the game UI.
    ///
    /// Uses the same dedup and merge logic as the market importer to avoid duplicates.
    /// </summary>
    public static class CrateImporter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Property key remapping — matches BlueprintScanner._propertyRemap
        /// for labels that differ between the game UI and our canonical keys.
        /// </summary>
        private static readonly Dictionary<string, string> PropertyRemap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Health (Hitpoints)", BlueprintPropertyKeys.Health },
            { "Maximum Damage Repair %", "Maximum Damage Repair" },
            { "The number of crew supported", "Crew Supported" },
            { "Eng. Capacity Required", BlueprintPropertyKeys.EngCapacityRequired },
            { "Eng. Capacity Available", BlueprintPropertyKeys.EngCapacityAvailable },
            { "Power regeneration rate", BlueprintPropertyKeys.PowerRegenRate },
            { "Blue Collar Detail(s)", GameConstants.PropBlueCollarDetail },
            { "Unassigned White Collar Detail(s)", GameConstants.PropUnassignedWhiteCollarDetail },
            { "Unassigned Specialist Detail(s)", GameConstants.PropUnassignedSpecialistDetail },
            { "Specialist Detail(s)", GameConstants.PropSpecialistDetail },
            { "White Collar Detail(s)", GameConstants.PropWhiteCollarDetail },
            { "Warehousing Capacity", GameConstants.PropWarehouseCapacity },
        };

        /// <summary>
        /// Imports blueprints from a JSON file path.
        /// </summary>
        public static CrateImportResult ImportFromFile(
            string filePath,
            PlayerContext playerContext,
            EmpireContext empireContext)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return new CrateImportResult
                {
                    Errors = { $"File not found: {filePath}" }
                };
            }

            string json = File.ReadAllText(filePath);
            return ImportFromJson(json, playerContext, empireContext);
        }

        /// <summary>
        /// Imports blueprints from a JSON string.
        /// </summary>
        public static CrateImportResult ImportFromJson(
            string json,
            PlayerContext playerContext,
            EmpireContext empireContext)
        {
            var result = new CrateImportResult();

            JArray entries;
            try
            {
                entries = JArray.Parse(json);
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"Invalid JSON: {ex.Message}");
                return result;
            }

            result.TotalInFile = entries.Count;
            Log.Info("=== Crate Import: {0} entries ===", entries.Count);

            // Snapshot existing blueprints BEFORE the import loop.
            // Each JSON entry is a distinct blueprint from the game — we must not
            // dedup entries within the same file against each other. Only dedup
            // against blueprints that existed before this import started.
            // Use HashSet to track which UUIDs have already been matched in this
            // import, so the same existing blueprint isn't matched twice.
            var preExistingGlobal = empireContext.GlobalBlueprintList.ToList();
            var preExistingPlayer = playerContext.BlueprintList.ToList();
            var matchedUUIDs = new HashSet<string>();

            foreach (JObject entry in entries)
            {
                try
                {
                    var importEntry = ImportSingleEntry(entry, playerContext, empireContext, preExistingGlobal, preExistingPlayer, matchedUUIDs);
                    result.Entries.Add(importEntry);

                    switch (importEntry.Action)
                    {
                        case ImportAction.Created:
                            result.Created++;
                            break;
                        case ImportAction.Updated:
                            result.Updated++;
                            break;
                        case ImportAction.Skipped:
                            result.Skipped++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    string name = entry["name"]?.ToString() ?? "(unknown)";
                    result.Errors.Add($"Failed to import '{name}': {ex.Message}");
                    Log.Error(ex, "Crate import failed for entry: {0}", name);
                }
            }

            // Persist once at the end (not per-entry)
            if (result.Created > 0 || result.Updated > 0)
            {
                playerContext.WriteContext();
                empireContext.WriteContext();
                playerContext.OnBlueprintDataChanged(null);
            }

            Log.Info(
                "Crate import complete: {0} created, {1} updated, {2} skipped, {3} failed",
                result.Created,
                result.Updated,
                result.Skipped,
                result.Failed);

            return result;
        }

        private static CrateImportEntry ImportSingleEntry(
            JObject entry,
            PlayerContext playerContext,
            EmpireContext empireContext,
            IReadOnlyList<Blueprint> preExistingGlobal,
            IReadOnlyList<Blueprint> preExistingPlayer,
            HashSet<string> matchedUUIDs)
        {
            string name = entry["name"]?.ToString()?.Trim() ?? string.Empty;
            int evolution = entry["evolution"]?.Value<int>() ?? 0;
            string techLevel = entry["techLevel"]?.ToString()?.Trim() ?? string.Empty;
            string description = entry["description"]?.ToString()?.Trim() ?? string.Empty;
            string iconClass = entry["iconClass"]?.ToString()?.Trim() ?? string.Empty;
            string iconPosition = entry["iconPosition"]?.ToString()?.Trim() ?? string.Empty;

            // Normalize empty strings to null for dedup key fields.
            // Existing blueprints may have null TechLevel (C# default) while
            // JSON parsing produces "" — FindByDedupKey uses string.Equals
            // which treats null != "".
            if (string.IsNullOrEmpty(techLevel)) techLevel = null;

            var importEntry = new CrateImportEntry
            {
                Name = name,
                Evolution = evolution,
                TechLevel = techLevel,
            };

            if (string.IsNullOrEmpty(name))
            {
                importEntry.Action = ImportAction.Skipped;
                importEntry.SkipReason = "No name";
                return importEntry;
            }

            // Build a temporary Blueprint from the JSON data
            var tempBP = new Blueprint(name)
            {
                Evolution = evolution,
                TechLevel = techLevel,
                Description = description,
            };

            // Resolve blueprint type:
            // 1. Try matching blueprint name against BlueprintType.Name (most reliable)
            // 2. Try icon sprite position (works when sprite sheets match)
            // 3. Try description-based matching
            // 4. Fall back to name-based classification
            string resolvedType = null;

            // Try name match first — the BlueprintType.Name in baseline data
            // matches the game's blueprint name (e.g. "Warehouse Flatpack")
            // For flatpacks, the game title omits " Flatpack" so try both forms
            resolvedType = FindTypeByName(name, empireContext);
            if (string.IsNullOrEmpty(resolvedType) && !name.EndsWith(" Flatpack", StringComparison.OrdinalIgnoreCase))
            {
                resolvedType = FindTypeByName(name + " Flatpack", empireContext);
            }

            if (string.IsNullOrEmpty(resolvedType) && !string.IsNullOrEmpty(iconPosition))
            {
                var bpType = empireContext.FindBlueprintTypeByIcon(iconPosition);
                if (bpType != null)
                {
                    resolvedType = bpType.Id;
                    Log.Info("    Icon position '{0}' -> type '{1}'", iconPosition, resolvedType);
                }
            }

            if (!string.IsNullOrEmpty(iconPosition))
            {
                tempBP.Properties.SetProperty("_IconPosition", iconPosition);
            }

            if (string.IsNullOrEmpty(resolvedType) && !string.IsNullOrEmpty(description))
            {
                resolvedType = FindTypeByDescription(description, empireContext);
            }

            if (string.IsNullOrEmpty(resolvedType))
            {
                resolvedType = ReclassifyByName(null, name);
            }

            // Apply name-based reclassification (e.g. flatpack subtypes)
            resolvedType = ReclassifyByName(resolvedType, name);
            tempBP.BluePrintType = resolvedType;

            if (!string.IsNullOrEmpty(iconClass))
            {
                tempBP.Properties.SetProperty("_IconClass", iconClass);
            }

            // Flatpack name normalization
            if (!string.IsNullOrEmpty(tempBP.BluePrintType) &&
                tempBP.BluePrintType.IsFlatpack() &&
                !name.EndsWith(" Flatpack", StringComparison.OrdinalIgnoreCase))
            {
                tempBP.Name = name + " Flatpack";
                importEntry.Name = tempBP.Name;
            }

            // Parse properties
            var propsObj = entry["properties"] as JObject;
            if (propsObj != null)
            {
                foreach (var prop in propsObj)
                {
                    string key = prop.Key;
                    string value = prop.Value?.ToString() ?? string.Empty;

                    // Remap property keys
                    if (PropertyRemap.TryGetValue(key, out string remappedKey))
                    {
                        key = remappedKey;
                    }

                    // Normalize property values
                    value = NormalizePropertyValue(key, value);

                    // Extract Class from properties
                    if (key == "Class")
                    {
                        if (int.TryParse(value, out int cls))
                        {
                            tempBP.Class = cls;
                        }

                        continue; // Don't store Class in PropertyBag
                    }

                    tempBP.Properties.SetProperty(key, value);
                }
            }

            // Parse resources
            var resObj = entry["resources"] as JObject;
            if (resObj != null)
            {
                foreach (var res in resObj)
                {
                    string resName = res.Key;
                    string qty = res.Value?.ToString() ?? string.Empty;
                    // Normalize: strip non-digits
                    string normalized = new string(qty.Where(c => char.IsDigit(c)).ToArray());
                    tempBP.Resources[resName] = string.IsNullOrEmpty(normalized) ? qty : normalized;
                }
            }

            Log.Info(
                "  Crate entry: name='{0}' evo={1} tech='{2}' type='{3}' class={4} props={5} res={6}",
                tempBP.Name,
                tempBP.Evolution,
                tempBP.TechLevel,
                tempBP.BluePrintType,
                tempBP.Class,
                tempBP.Properties?.Count ?? 0,
                tempBP.Resources?.Count ?? 0);

            // Fix up game data quirks (e.g. Reactor "Power Required" → "Power Provided")
            BlueprintService.FixupFlatpackProperties(tempBP);

            // Route: evo 0 → global, evo > 0 → player
            bool hasPlayer = !string.IsNullOrEmpty(playerContext.CurrentPlayerUUID);
            bool isGlobal = BlueprintService.IsGlobalRoute(tempBP.Evolution, hasPlayer);

            var targetList = isGlobal
                ? (IEnumerable<Blueprint>)preExistingGlobal
                : (IEnumerable<Blueprint>)preExistingPlayer;

            // Dedup: find the best-matching existing blueprint.
            // Exclude blueprints already matched by a previous entry in this import
            // to prevent two distinct incoming entries from collapsing into the same target.
            var availableTargets = targetList.Where(b => !matchedUUIDs.Contains(b.UUID));
            var existing = BlueprintService.FindBestMatch(availableTargets, tempBP);

            if (existing != null)
            {
                // Mark this UUID as matched so no other entry in this file can claim it
                matchedUUIDs.Add(existing.UUID);

                // Update existing
                BlueprintService.UpdateExisting(existing, tempBP);
                importEntry.Action = ImportAction.Updated;
                importEntry.Storage = isGlobal ? "Global" : "Player";
                Log.Info("    -> Updated existing: UUID={0}", existing.UUID);
            }
            else
            {
                // Create new
                tempBP.UUID = isGlobal
                    ? DeterministicUUID.Generate(tempBP)
                    : Guid.NewGuid().ToString();

                if (!isGlobal)
                    tempBP.OwnerUUID = playerContext.CurrentPlayerUUID;

                // Check for UUID collision before adding
                if (isGlobal)
                {
                    var alreadyExists = empireContext.FindMutableGlobalBlueprint(tempBP.UUID);
                    if (alreadyExists != null)
                    {
                        BlueprintService.UpdateExisting(alreadyExists, tempBP);
                        importEntry.Action = ImportAction.Updated;
                        importEntry.Storage = "Global";
                        Log.Info("    -> UUID collision — updated existing: UUID={0}", alreadyExists.UUID);
                    }
                    else
                    {
                        empireContext.AddGlobalBlueprint(tempBP);
                        importEntry.Action = ImportAction.Created;
                        importEntry.Storage = "Global";
                        Log.Info("    -> Created new: UUID={0} (Global)", tempBP.UUID);
                    }
                }
                else
                {
                    playerContext.AddBlueprint(tempBP);
                    importEntry.Action = ImportAction.Created;
                    importEntry.Storage = "Player";
                    Log.Info("    -> Created new: UUID={0} (Player)", tempBP.UUID);
                }
            }

            return importEntry;
        }

        /// <summary>
        /// Finds a BlueprintType by exact name match against BlueprintType.Name.
        /// </summary>
        private static string FindTypeByName(string blueprintName, EmpireContext ec)
        {
            if (string.IsNullOrEmpty(blueprintName) || ec == null)
                return null;

            var match = ec.BlueprintTypeList.FirstOrDefault(bt =>
                string.Equals(bt.Name, blueprintName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                Log.Info("    Name '{0}' matched type '{1}' directly", blueprintName, match.Id);
                return match.Id;
            }

            return null;
        }

        /// <summary>
        /// Finds a BlueprintType by matching the game description against type names.
        /// The game description (e.g. "Coilgun Ammunition") is matched against
        /// BlueprintType.Name (e.g. "Coilgun Munitions") using keyword overlap.
        /// </summary>
        private static string FindTypeByDescription(string description, EmpireContext ec)
        {
            if (string.IsNullOrEmpty(description) || ec == null)
                return null;

            // Direct name match first (case-insensitive)
            var directMatch = ec.BlueprintTypeList.FirstOrDefault(bt =>
                string.Equals(bt.Name, description, StringComparison.OrdinalIgnoreCase));
            if (directMatch != null)
                return directMatch.Id;

            // Keyword match: extract the first word of the description and match
            // against BlueprintType names. E.g. "Coilgun Ammunition" → "Coilgun" → "Coilgun Munitions"
            string descLower = description.ToLowerInvariant();
            string firstWord = descLower.Split(' ')[0];

            if (!string.IsNullOrEmpty(firstWord) && firstWord.Length >= 3)
            {
                var keywordMatch = ec.BlueprintTypeList.FirstOrDefault(bt =>
                    bt.Name != null && bt.Name.ToLowerInvariant().Contains(firstWord));
                if (keywordMatch != null)
                {
                    Log.Info(
                        "    Description '{0}' matched type '{1}' via keyword '{2}'",
                        description,
                        keywordMatch.Id,
                        firstWord);
                    return keywordMatch.Id;
                }
            }

            // Try "contains" match on the full description
            var containsMatch = ec.BlueprintTypeList.FirstOrDefault(bt =>
                bt.Name != null && descLower.Contains(bt.Name.ToLowerInvariant()));
            if (containsMatch != null)
            {
                Log.Info(
                    "    Description '{0}' matched type '{1}' via contains",
                    description,
                    containsMatch.Id);
                return containsMatch.Id;
            }

            Log.Warn("    No type match for description '{0}'", description);
            return null;
        }

        /// <summary>
        /// Normalize property values — strip units, clean whitespace.
        /// Uses BlueprintPropertyValidation to determine value type, then strips
        /// units the same way BlueprintScanner.NormalizePropertyValue does.
        /// </summary>
        private static string NormalizePropertyValue(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var propType = BlueprintPropertyValidation.GetPropertyType(key);

            switch (propType)
            {
                case PropertyValueType.Time:
                    // "9 hours" -> "9h", "30 minutes" -> "30m", "1 hours 30 minutes" -> "1h 30m"
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\s*hours?\s*", "h ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\s*minutes?\s*", "m ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\s*seconds?\s*", "s ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    value = System.Text.RegularExpressions.Regex.Replace(value, @"\s*days?\s*", "d ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return value.Trim();

                case PropertyValueType.Decimal:
                    // Strip units: "31.5MW/s" -> "31.5", "2.959%" -> "2.959"
                    var decMatch = System.Text.RegularExpressions.Regex.Match(value, @"[+-]?\d+(\.\d+)?");
                    return decMatch.Success ? decMatch.Value : value;

                case PropertyValueType.Integer:
                    // Strip any non-digit characters except leading +/-: "100/run" -> "100"
                    var intMatch = System.Text.RegularExpressions.Regex.Match(value, @"[+-]?\d+");
                    return intMatch.Success ? intMatch.Value : value;

                default:
                    return value.Trim();
            }
        }

        /// <summary>
        /// Name-based blueprint type reclassification.
        /// Mirrors BlueprintScanner.ReclassifyByName.
        /// </summary>
        private static string ReclassifyByName(string resolvedType, string blueprintName)
        {
            if (string.IsNullOrEmpty(blueprintName))
                return resolvedType;

            string lower = blueprintName.ToLowerInvariant();

            // Flatpack detection by name suffix
            if (lower.EndsWith(" flatpack"))
            {
                string baseName = blueprintName.Substring(0, blueprintName.Length - " Flatpack".Length).Trim();

                if (baseName.Equals("Mining Rig", StringComparison.OrdinalIgnoreCase))
                    return BlueprintTypes.MiningRig;
                if (baseName.Equals("Refinery", StringComparison.OrdinalIgnoreCase))
                    return BlueprintTypes.Refinery;
                if (baseName.Equals("Research Laboratory", StringComparison.OrdinalIgnoreCase))
                    return BlueprintTypes.ResearchLaboratory;
                if (baseName.Equals("Manufactory", StringComparison.OrdinalIgnoreCase))
                    return BlueprintTypes.Manufactory;
                if (baseName.Equals("Colony Command Centre", StringComparison.OrdinalIgnoreCase))
                    return BlueprintTypes.ColonyCommandCentre;
            }

            // Component detection by name keywords
            if (lower.Contains("ore hopper"))
                return BlueprintTypes.OreHopper;
            if (lower.Contains("mining laser"))
                return BlueprintTypes.MiningLaser;
            if (lower.Contains("asteroid grapple"))
                return BlueprintTypes.AsteroidGrapple;

            return resolvedType;
        }

        /// <summary>
        /// Result of a crate import operation.
        /// </summary>
        public class CrateImportResult
        {
            public int TotalInFile { get; set; }
            public int Created { get; set; }
            public int Updated { get; set; }
            public int Skipped { get; set; }
            public int Failed { get; set; }
            public List<string> Errors { get; } = new List<string>();
            public List<CrateImportEntry> Entries { get; } = new List<CrateImportEntry>();
        }

        public class CrateImportEntry
        {
            public string Name { get; set; }
            public int Evolution { get; set; }
            public string TechLevel { get; set; }
            public ImportAction Action { get; set; }
            public string Storage { get; set; }
            public string SkipReason { get; set; }
        }
    }
}
