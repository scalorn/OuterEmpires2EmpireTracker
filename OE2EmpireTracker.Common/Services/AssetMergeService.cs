// <copyright file="AssetMergeService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides merge logic for synchronizing asset API data with local models.
    /// Handles TypeC code mapping and resource purity extraction.
    /// </summary>
    public static class AssetMergeService
    {
        /// <summary>
        /// The default hold name used for station asset storage.
        /// </summary>
        public const string DefaultHoldName = "default";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly Regex PurityRegex = new Regex(
            @"\(((?:Unrefined|Refined)(?:,\s*)?)?(?:(High Purity|Med Purity|Low Purity))?\)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Maps an asset API TypeC code to the local <see cref="ItemType.ItemTypeEnum"/>.
        /// Trims whitespace and uses case-insensitive matching to handle mixed-case API codes.
        /// For the ambiguous pair "SH" (ShipHull) vs "Sh" (Share), exact case is checked first.
        /// Unknown codes map to <see cref="ItemType.ItemTypeEnum.None"/> with a warning logged.
        /// </summary>
        /// <param name="typeC">The TypeC code from the asset API response.</param>
        /// <returns>The corresponding <see cref="ItemType.ItemTypeEnum"/> value.</returns>
        public static ItemType.ItemTypeEnum MapAssetTypeC(string typeC)
        {
            var trimmed = typeC?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                Log.Warn("MapAssetTypeC: typeC is null or empty, mapping to None");
                return ItemType.ItemTypeEnum.None;
            }

            // Handle the ambiguous "SH" vs "Sh" pair with exact-case check first.
            // "SH" (both uppercase) = ShipHull; "Sh" (capital S, lowercase h) = Share.
            if (trimmed.Length == 2
                && trimmed[0] == 'S'
                && (trimmed[1] == 'H' || trimmed[1] == 'h'))
            {
                if (trimmed[1] == 'H')
                {
                    return ItemType.ItemTypeEnum.ShipHull;
                }

                return ItemType.ItemTypeEnum.Share;
            }

            // Handle the ambiguous "Sc" vs "S" — "Sc" must be checked before "S"
            // since case-insensitive "S" would not conflict with "Sc" (different lengths).
            if (string.Equals(trimmed, AssetTypeCodes.Survey, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Survey;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Resource, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Resource;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Commodity, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Commodity;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Flatpack, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Flatpack;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Blueprint, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Blueprint;
            }

            if (string.Equals(trimmed, AssetTypeCodes.ShipPart, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.ShipPart;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Workforce, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.WorkDetail;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Ammunition, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Munition;
            }

            if (string.Equals(trimmed, AssetTypeCodes.Crate, StringComparison.OrdinalIgnoreCase))
            {
                return ItemType.ItemTypeEnum.Crate;
            }

            Log.Warn("MapAssetTypeC: unknown typeC '{0}', mapping to None", typeC);
            return ItemType.ItemTypeEnum.None;
        }

        /// <summary>
        /// Extracts the base resource name and purity descriptor from a resource name string.
        /// Resource names may contain a parenthesized purity descriptor such as
        /// "Heavy Post-Trans Metals (Unrefined, Med Purity)".
        /// </summary>
        /// <param name="resourceName">The full resource name from the API.</param>
        /// <returns>
        /// A tuple of (baseName, purity) where baseName is the resource name without the
        /// purity parenthetical (trimmed), and purity is "High Purity", "Med Purity",
        /// "Low Purity", or empty string if no purity descriptor is found.
        /// </returns>
        public static (string baseName, string purity) ExtractResourcePurity(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return (string.Empty, string.Empty);
            }

            var match = PurityRegex.Match(resourceName);
            if (!match.Success)
            {
                return (resourceName, string.Empty);
            }

            // Find the last '(' to extract the base name
            int lastParen = resourceName.LastIndexOf('(');
            if (lastParen <= 0)
            {
                return (resourceName, string.Empty);
            }

            string baseName = resourceName.Substring(0, lastParen).TrimEnd();

            string refinementState = match.Groups[1].Value.TrimEnd(',', ' ');
            string purityValue = match.Groups[2].Value;

            string purity;
            if (string.Equals(refinementState, GameConstants.PurityRefined, System.StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(purityValue))
            {
                // "Alkali Inorganics (Refined)" -> purity = "Refined"
                purity = GameConstants.PurityRefined;
            }
            else if (!string.IsNullOrEmpty(purityValue))
            {
                // "Heavy Post-Trans Metals (Unrefined, Med Purity)" or "Iron (High Purity)"
                purity = purityValue;

                // Normalize: strip " Purity" suffix and normalize abbreviations
                if (purity.EndsWith(" Purity", System.StringComparison.OrdinalIgnoreCase))
                {
                    purity = purity.Substring(0, purity.Length - " Purity".Length).Trim();
                }

                purity = SurveyParser.NormalizePurity(purity);
            }
            else
            {
                purity = string.Empty;
            }

            return (baseName, purity);
        }

        /// <summary>
        /// Merges a list of asset API cargo items into a colony's item bag.
        /// Iterates each cargo item and calls <see cref="ProcessSingleAssetItem"/> on the colony's Items.
        /// </summary>
        /// <param name="apiItems">The list of cargo items from the asset API response.</param>
        /// <param name="colony">The target colony to merge items into.</param>
        /// <returns>True if any changes were made to the colony's items; false otherwise.</returns>
        public static bool MergeColonyAssets(ICollection<AssetCargoItem> apiItems, Colony colony)
        {
            if (colony == null)
            {
                Log.Warn("MergeColonyAssets: colony is null, skipping merge");
                return false;
            }

            if (apiItems == null || apiItems.Count == 0)
            {
                return false;
            }

            bool anyChanged = false;
            foreach (var apiItem in apiItems)
            {
                anyChanged |= ProcessSingleAssetItem(apiItem, colony.Items);
            }

            return anyChanged;
        }

        /// <summary>
        /// Merges a list of asset API cargo items into a station's target hold.
        /// Iterates through each cargo item and calls <see cref="ProcessSingleAssetItem"/>
        /// to update or create items in the provided hold.
        /// </summary>
        /// <param name="apiItems">The list of cargo items from the asset API response.</param>
        /// <param name="station">The target station to merge into.</param>
        /// <param name="targetHold">The item bag (hold) to merge cargo items into.</param>
        /// <returns>True if any changes were made to the station's hold; false otherwise.</returns>
        public static bool MergeStationAssets(ICollection<AssetCargoItem> apiItems, Station station, ItemBag targetHold)
        {
            if (station == null || targetHold == null)
            {
                Log.Warn("MergeStationAssets: station or targetHold is null, skipping merge");
                return false;
            }

            if (apiItems == null || apiItems.Count == 0)
            {
                return false;
            }

            bool anyChanges = false;
            foreach (var apiItem in apiItems)
            {
                anyChanges |= ProcessSingleAssetItem(apiItem, targetHold);
            }

            return anyChanges;
        }

        /// <summary>
        /// Merges a list of asset API cargo items into a ship's cargo hold.
        /// Iterates through each cargo item and calls <see cref="ProcessSingleAssetItem"/>
        /// to update or create items in the ship's <see cref="Ship.Cargo"/> bag.
        /// </summary>
        /// <param name="apiItems">The list of cargo items from the asset API response.</param>
        /// <param name="ship">The target ship to merge cargo into.</param>
        /// <returns>True if any changes were made to the ship's cargo; false otherwise.</returns>
        public static bool MergeShipAssets(ICollection<AssetCargoItem> apiItems, Ship ship)
        {
            if (ship == null)
            {
                Log.Warn("MergeShipAssets: ship is null, skipping merge");
                return false;
            }

            if (apiItems == null || apiItems.Count == 0)
            {
                return false;
            }

            bool anyChanges = false;
            foreach (var apiItem in apiItems)
            {
                anyChanges |= ProcessSingleAssetItem(apiItem, ship.Cargo);
            }

            return anyChanges;
        }

        /// <summary>
        /// Processes a single asset cargo item against the target item bag.
        /// Matches by GameItemId and routes to update or create as appropriate.
        /// </summary>
        /// <param name="apiItem">The cargo item from the asset API response.</param>
        /// <param name="targetBag">The item bag to merge into.</param>
        /// <returns>True if any changes were made (item created or updated); false otherwise.</returns>
        internal static bool ProcessSingleAssetItem(AssetCargoItem apiItem, ItemBag targetBag)
        {
            var mappedType = MapAssetTypeC(apiItem.TypeC);

            var match = targetBag.Items.Values.FirstOrDefault(i => i.GameItemId == apiItem.Id);

            if (match != null)
            {
                Log.Debug(
                    "AssetMerge: MATCHED cargoItemId={0} name='{1}' to local UUID={2} Name='{3}'",
                    apiItem.Id,
                    apiItem.ResourceName,
                    match.UUID,
                    match.Name);
                return UpdateExistingAssetItem(match, apiItem);
            }

            // Log existing items with similar names for diagnosis
            var similarItems = targetBag.Items.Values
                .Where(i => i.Name != null && apiItem.ResourceName != null &&
                    i.Name.Contains(apiItem.ResourceName.Split('(')[0].Trim()))
                .Select(i => $"UUID={i.UUID} Name='{i.Name}' GameItemId={i.GameItemId} Qty={i.Quantity}")
                .ToList();

            Log.Debug(
                "AssetMerge: NO MATCH for cargoItemId={0} name='{1}' type={2} — creating new. Similar local items: [{3}]",
                apiItem.Id,
                apiItem.ResourceName,
                mappedType,
                similarItems.Count > 0 ? string.Join("; ", similarItems) : "none");

            var newItem = CreateAssetItem(apiItem, mappedType);
            targetBag.AddItem(newItem);
            return true;
        }

        /// <summary>
        /// Processes a single asset cargo item: matches by GameItemId or creates new.
        /// Returns the UUID of the item that was created or updated (for tracking touched items).
        /// </summary>
        internal static string ProcessSingleAssetItemAndReturnUUID(AssetCargoItem apiItem, ItemBag targetBag)
        {
            var mappedType = MapAssetTypeC(apiItem.TypeC);

            var match = targetBag.Items.Values.FirstOrDefault(i => i.GameItemId == apiItem.Id);

            if (match != null)
            {
                Log.Debug(
                    "AssetMerge: MATCHED cargoItemId={0} name='{1}' to local UUID={2} Name='{3}'",
                    apiItem.Id,
                    apiItem.ResourceName,
                    match.UUID,
                    match.Name);
                UpdateExistingAssetItem(match, apiItem);
                return match.UUID;
            }

            // Log existing items with similar names for diagnosis
            var similarItems = targetBag.Items.Values
                .Where(i => i.Name != null && apiItem.ResourceName != null &&
                    i.Name.Contains(apiItem.ResourceName.Split('(')[0].Trim()))
                .Select(i => $"UUID={i.UUID} Name='{i.Name}' GameItemId={i.GameItemId} Qty={i.Quantity}")
                .ToList();

            Log.Debug(
                "AssetMerge: NO MATCH for cargoItemId={0} name='{1}' type={2} — creating new. Similar local items: [{3}]",
                apiItem.Id,
                apiItem.ResourceName,
                mappedType,
                similarItems.Count > 0 ? string.Join("; ", similarItems) : "none");

            var newItem = CreateAssetItem(apiItem, mappedType);
            targetBag.AddItem(newItem);
            return newItem.UUID;
        }

        /// <summary>
        /// Updates an existing local item with data from the asset API response.
        /// Preserves local-only fields (NickName, Description).
        /// </summary>
        /// <param name="local">The existing local item to update.</param>
        /// <param name="apiItem">The cargo item from the asset API response.</param>
        /// <returns>True if any field was actually changed; false if all fields already matched.</returns>
        internal static bool UpdateExistingAssetItem(Item local, AssetCargoItem apiItem)
        {
            bool changed = false;

            var mappedType = MapAssetTypeC(apiItem.TypeC);
            string newName = apiItem.ResourceName;
            string newPurity = string.Empty;

            if (mappedType == ItemType.ItemTypeEnum.Resource)
            {
                var (baseName, purity) = ExtractResourcePurity(apiItem.ResourceName);
                newName = baseName;
                newPurity = purity;
            }

            changed |= SetIfDifferent(ref local, l => l.Name, newName, (l, v) => l.Name = v);
            changed |= SetQuantityIfDifferent(local, apiItem.Amount);
            changed |= SetMassIfDifferent(local, apiItem.Mass);
            changed |= SetVolumeIfDifferent(local, apiItem.Volume);
            changed |= SetHealthIfDifferent(local, apiItem.HealthPercentage);
            changed |= SetLastRepairHealthIfDifferent(local, apiItem.LastRepairHealthPercentage);
            changed |= SetEvolutionIfDifferent(local, apiItem.Evolution);
            changed |= SetStringIfDifferent(ref local, l => l.ShipPartType, apiItem.ShipPartType, (l, v) => l.ShipPartType = v);
            changed |= SetNullableIntIfDifferent(ref local, l => l.JobRef, apiItem.JobRef, (l, v) => l.JobRef = v);
            changed |= SetNullableIntIfDifferent(ref local, l => l.JobDeliveryLoc, apiItem.JobDeliveryLoc, (l, v) => l.JobDeliveryLoc = v);
            changed |= SetStringIfDifferent(ref local, l => l.JobName, apiItem.JobName, (l, v) => l.JobName = v);
            changed |= SetStringIfDifferent(ref local, l => l.JobTrack, apiItem.JobTrack, (l, v) => l.JobTrack = v);
            // For flatpacks, resolve BaseItemTypeID to the blueprint UUID (not the display name).
            // This allows Phase 2 staged matching to correlate warehouse items with structure FlatpackBlueprintUUID.
            string resolvedBaseItemTypeId = newName;
            if (mappedType == ItemType.ItemTypeEnum.Flatpack)
            {
                string flatpackName = newName;
                if (flatpackName.StartsWith("Flatpack: ", StringComparison.OrdinalIgnoreCase))
                {
                    flatpackName = flatpackName.Substring("Flatpack: ".Length);
                }

                var empireContext = EmpireContext.GetInstance();
                if (empireContext != null)
                {
                    var flatpackLookup = ColonyParser.BuildFlatpackLookup(empireContext);
                    if (flatpackLookup.TryGetValue(flatpackName, out string blueprintUuid))
                    {
                        resolvedBaseItemTypeId = blueprintUuid;
                    }
                }
            }

            changed |= SetStringIfDifferent(ref local, l => l.BaseItemTypeID, resolvedBaseItemTypeId, (l, v) => l.BaseItemTypeID = v);
            changed |= SetStringIfDifferent(ref local, l => l.ResourcePurity, newPurity, (l, v) => l.ResourcePurity = v);
            changed |= UpdateItemProperties(local, apiItem.Properties);

            // Hulls have typeC="S" but shipPartType="Hu" — override to ShipHull
            if (mappedType == ItemType.ItemTypeEnum.ShipPart &&
                string.Equals(apiItem.ShipPartType?.Trim(), "Hu", StringComparison.OrdinalIgnoreCase))
            {
                mappedType = ItemType.ItemTypeEnum.ShipHull;
            }

            if (local.ItemType != mappedType)
            {
                local.ItemType = mappedType;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Creates a new local Item from an asset API cargo item.
        /// Generates a new UUID and maps all fields from the API response.
        /// </summary>
        /// <param name="apiItem">The cargo item from the asset API response.</param>
        /// <param name="mappedType">The mapped item type from the TypeC code.</param>
        /// <returns>A new <see cref="Item"/> populated from the API data.</returns>
        internal static Item CreateAssetItem(AssetCargoItem apiItem, ItemType.ItemTypeEnum mappedType)
        {
            string name = apiItem.ResourceName;
            string purity = string.Empty;

            if (mappedType == ItemType.ItemTypeEnum.Resource)
            {
                var (baseName, extractedPurity) = ExtractResourcePurity(apiItem.ResourceName);
                name = baseName;
                purity = extractedPurity;
            }

            // Hulls have typeC="S" but shipPartType="Hu" — override to ShipHull
            if (mappedType == ItemType.ItemTypeEnum.ShipPart &&
                string.Equals(apiItem.ShipPartType?.Trim(), "Hu", StringComparison.OrdinalIgnoreCase))
            {
                mappedType = ItemType.ItemTypeEnum.ShipHull;
            }

            // BaseItemTypeID is used as the lookup key in ItemBag.CountByType/FindByType.
            // For most item types, it should be the item name (matching what the tool uses internally).
            // The numeric TypeId is stored but not used for lookups.
            // For flatpacks: resolve the blueprint UUID so Phase 2 staged matching works.
            // API names are "Flatpack: Refinery" — strip prefix and look up via BuildFlatpackLookup.
            string baseItemTypeId = name;

            if (mappedType == ItemType.ItemTypeEnum.Flatpack)
            {
                string flatpackName = name;

                // Strip "Flatpack: " prefix if present (API format)
                if (flatpackName.StartsWith("Flatpack: ", StringComparison.OrdinalIgnoreCase))
                {
                    flatpackName = flatpackName.Substring("Flatpack: ".Length);
                }

                var empireContext = EmpireContext.GetInstance();
                if (empireContext != null)
                {
                    var flatpackLookup = ColonyParser.BuildFlatpackLookup(empireContext);
                    if (flatpackLookup.TryGetValue(flatpackName, out string blueprintUuid))
                    {
                        baseItemTypeId = blueprintUuid;
                        Log.Debug(
                            "AssetMerge: Flatpack '{0}' resolved to blueprint UUID={1}",
                            name,
                            blueprintUuid);
                    }
                    else
                    {
                        Log.Warn(
                            "AssetMerge: Flatpack '{0}' (stripped='{1}') not found in flatpack lookup — BaseItemTypeID left as name",
                            name,
                            flatpackName);
                    }
                }
                else
                {
                    Log.Warn(
                        "AssetMerge: EmpireContext not loaded, cannot resolve flatpack '{0}' to blueprint UUID",
                        name);
                }
            }

            Log.Debug(
                "AssetMerge: CreateAssetItem type={0} name='{1}' baseItemTypeId='{2}' typeId={3} amount={4}",
                mappedType,
                apiItem.ResourceName,
                baseItemTypeId,
                apiItem.TypeId,
                apiItem.Amount);

            var item = new Item
            {
                UUID = Guid.NewGuid().ToString(),
                GameItemId = apiItem.Id,
                ItemType = mappedType,
                Name = name,
                Quantity = apiItem.Amount,
                Mass = apiItem.Mass.HasValue ? (decimal)apiItem.Mass.Value : (decimal?)null,
                Volume = apiItem.Volume.HasValue ? (decimal)apiItem.Volume.Value : 0m,
                HealthPercentage = apiItem.HealthPercentage.HasValue ? (decimal)apiItem.HealthPercentage.Value : (decimal?)null,
                LastRepairHealthPercentage = apiItem.LastRepairHealthPercentage.HasValue ? (decimal)apiItem.LastRepairHealthPercentage.Value : (decimal?)null,
                Evolution = apiItem.Evolution,
                ShipPartType = apiItem.ShipPartType ?? string.Empty,
                JobRef = apiItem.JobRef,
                JobDeliveryLoc = apiItem.JobDeliveryLoc,
                JobName = apiItem.JobName ?? string.Empty,
                JobTrack = apiItem.JobTrack ?? string.Empty,
                BaseItemTypeID = baseItemTypeId,
                ResourcePurity = purity,
                ItemProperties = MapProperties(apiItem.Properties),
            };

            return item;
        }

        private static bool SetIfDifferent(ref Item local, Func<Item, string> getter, string newValue, Action<Item, string> setter)
        {
            var current = getter(local) ?? string.Empty;
            var target = newValue ?? string.Empty;
            if (!string.Equals(current, target, StringComparison.Ordinal))
            {
                setter(local, target);
                return true;
            }

            return false;
        }

        private static bool SetStringIfDifferent(ref Item local, Func<Item, string> getter, string newValue, Action<Item, string> setter)
        {
            return SetIfDifferent(ref local, getter, newValue, setter);
        }

        private static bool SetQuantityIfDifferent(Item local, int newQuantity)
        {
            if (local.Quantity != newQuantity)
            {
                local.Quantity = newQuantity;
                return true;
            }

            return false;
        }

        private static bool SetMassIfDifferent(Item local, double? newMass)
        {
            decimal? target = newMass.HasValue ? (decimal)newMass.Value : (decimal?)null;
            if (local.Mass != target)
            {
                local.Mass = target;
                return true;
            }

            return false;
        }

        private static bool SetVolumeIfDifferent(Item local, double? newVolume)
        {
            decimal target = newVolume.HasValue ? (decimal)newVolume.Value : 0m;
            if (local.Volume != target)
            {
                local.Volume = target;
                return true;
            }

            return false;
        }

        private static bool SetHealthIfDifferent(Item local, double? newHealth)
        {
            decimal? target = newHealth.HasValue ? (decimal)newHealth.Value : (decimal?)null;
            if (local.HealthPercentage != target)
            {
                local.HealthPercentage = target;
                return true;
            }

            return false;
        }

        private static bool SetLastRepairHealthIfDifferent(Item local, double? newHealth)
        {
            decimal? target = newHealth.HasValue ? (decimal)newHealth.Value : (decimal?)null;
            if (local.LastRepairHealthPercentage != target)
            {
                local.LastRepairHealthPercentage = target;
                return true;
            }

            return false;
        }

        private static bool SetEvolutionIfDifferent(Item local, int? newEvolution)
        {
            if (local.Evolution != newEvolution)
            {
                local.Evolution = newEvolution;
                return true;
            }

            return false;
        }

        private static bool SetNullableIntIfDifferent(ref Item local, Func<Item, int?> getter, int? newValue, Action<Item, int?> setter)
        {
            if (getter(local) != newValue)
            {
                setter(local, newValue);
                return true;
            }

            return false;
        }

        private static bool UpdateItemProperties(Item local, ICollection<AssetCargoProperty> apiProperties)
        {
            var newProps = MapProperties(apiProperties);
            if (ArePropertiesEqual(local.ItemProperties, newProps))
            {
                return false;
            }

            local.ItemProperties = newProps;
            return true;
        }

        private static List<ItemProperty> MapProperties(ICollection<AssetCargoProperty> apiProperties)
        {
            if (apiProperties == null || apiProperties.Count == 0)
            {
                return new List<ItemProperty>();
            }

            var result = new List<ItemProperty>(apiProperties.Count);
            foreach (var prop in apiProperties)
            {
                result.Add(new ItemProperty
                {
                    ModTypeId = prop.ModTypeId,
                    PropertyName = prop.PropertyName ?? string.Empty,
                    FriendlyPropertyName = prop.FriendlyPropertyName ?? string.Empty,
                    PropertyValue = prop.PropertyValue.ToString(),
                    OriginalPropertyValue = prop.OriginalPropertyValue.ToString(),
                    Unit = prop.Unit ?? string.Empty,
                    ResearchPositive = prop.ResearchPositive,
                    CanResearch = prop.CanResearch,
                });
            }

            return result;
        }

        private static bool ArePropertiesEqual(List<ItemProperty> a, List<ItemProperty> b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].ModTypeId != b[i].ModTypeId
                    || !string.Equals(a[i].PropertyName, b[i].PropertyName, StringComparison.Ordinal)
                    || !string.Equals(a[i].PropertyValue, b[i].PropertyValue, StringComparison.Ordinal)
                    || !string.Equals(a[i].OriginalPropertyValue, b[i].OriginalPropertyValue, StringComparison.Ordinal)
                    || !string.Equals(a[i].Unit, b[i].Unit, StringComparison.Ordinal)
                    || !string.Equals(a[i].FriendlyPropertyName, b[i].FriendlyPropertyName, StringComparison.Ordinal)
                    || a[i].ResearchPositive != b[i].ResearchPositive
                    || a[i].CanResearch != b[i].CanResearch)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
