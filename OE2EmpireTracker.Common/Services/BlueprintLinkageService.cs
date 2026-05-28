// <copyright file="BlueprintLinkageService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Client;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Creates or updates Blueprint entities from API cargo items with properties.
    /// Registers property type metadata in the global registry.
    /// Links warehouse items to their source blueprint via BaseItemTypeID.
    /// </summary>
    public class BlueprintLinkageService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;
        private readonly EmpireContext _empireContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlueprintLinkageService"/> class.
        /// </summary>
        /// <param name="playerContext">The player context for player-owned blueprints.</param>
        /// <param name="empireContext">The empire context for global blueprints and property registry.</param>
        public BlueprintLinkageService(PlayerContext playerContext, EmpireContext empireContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _empireContext = empireContext ?? throw new ArgumentNullException(nameof(empireContext));
        }

        /// <summary>
        /// Processes a cargo item from the API, creating or updating a Blueprint entity
        /// and linking the local item to it via BaseItemTypeID.
        /// </summary>
        /// <param name="apiItem">The API cargo item with properties.</param>
        /// <param name="localItem">The local warehouse item to link.</param>
        /// <param name="ownerUUID">The current player's UUID for ownership assignment.</param>
        /// <returns>True if a blueprint was created or updated; false if skipped.</returns>
        public bool ProcessItem(GameApiAssetCargoItem apiItem, Item localItem, string ownerUUID)
        {
            // 1. Skip if no properties
            if (apiItem.Properties == null || apiItem.Properties.Count == 0)
            {
                return false;
            }

            // 2. Register property type metadata
            RegisterPropertyTypes(apiItem.Properties);

            // 3. Build candidate blueprint
            var candidate = BuildCandidateBlueprint(apiItem);

            // 4. Determine routing (global vs player)
            bool isGlobal = BlueprintService.IsGlobalRoute(
                candidate.Evolution,
                !string.IsNullOrEmpty(ownerUUID));
            var searchList = isGlobal
                ? (IEnumerable<Blueprint>)_empireContext.GlobalBlueprintList
                : _playerContext.GetCurrentPlayerBlueprints();

            // 5. Find existing match
            var existing = BlueprintService.FindUnambiguousMatch(searchList, candidate);

            // 6. Create or update
            Blueprint target;
            if (existing != null)
            {
                target = existing;
                MergeProperties(target, apiItem.Properties);
                Log.Info(
                    "BlueprintLinkage: updated '{0}' UUID={1}, {2} properties",
                    target.Name,
                    target.UUID,
                    apiItem.Properties.Count);
            }
            else
            {
                candidate.UUID = Guid.NewGuid().ToString();
                candidate.OwnerUUID = isGlobal ? string.Empty : ownerUUID;
                BuildPropertyBag(candidate, apiItem.Properties);

                if (isGlobal)
                {
                    _empireContext.AddGlobalBlueprint(candidate);
                }
                else
                {
                    _playerContext.AddBlueprint(candidate);
                }

                target = candidate;
                Log.Info(
                    "BlueprintLinkage: created '{0}' UUID={1} type={2} evo={3}",
                    target.Name,
                    target.UUID,
                    target.BluePrintType,
                    target.Evolution);
            }

            // 7. Link item to blueprint
            localItem.BaseItemTypeID = target.UUID;

            return true;
        }

        /// <summary>
        /// Builds a candidate Blueprint from the API cargo item data.
        /// Handles both typeC="Bp" (blueprint items) and typeC="S" (ship part/hull items).
        /// </summary>
        /// <param name="apiItem">The API cargo item.</param>
        /// <returns>A candidate Blueprint with Name, Evolution, and BluePrintType set.</returns>
        internal Blueprint BuildCandidateBlueprint(GameApiAssetCargoItem apiItem)
        {
            var candidate = new Blueprint
            {
                Name = apiItem.ResourceName ?? string.Empty,
                Evolution = apiItem.Evolution ?? 0,
            };

            var typeC = (apiItem.TypeC ?? string.Empty).Trim();

            if (string.Equals(typeC, "Bp", StringComparison.OrdinalIgnoreCase))
            {
                candidate.BluePrintType = ClassifyBlueprintType(apiItem);
            }
            else if (string.Equals(typeC, "S", StringComparison.OrdinalIgnoreCase))
            {
                candidate.BluePrintType = ClassifyBlueprintType(apiItem);
            }
            else
            {
                candidate.BluePrintType = ClassifyBlueprintType(apiItem);
            }

            return candidate;
        }

        /// <summary>
        /// Classifies the BluePrintType for a cargo item based on its shipPartType and resourceName.
        /// Priority: Hu → Hull, Flatpack detection, fallback to MapShipPartType.
        /// </summary>
        /// <param name="apiItem">The API cargo item.</param>
        /// <returns>The classified BluePrintType string.</returns>
        internal string ClassifyBlueprintType(GameApiAssetCargoItem apiItem)
        {
            var shipPartType = (apiItem.ShipPartType ?? string.Empty).Trim();
            var resourceName = apiItem.ResourceName ?? string.Empty;

            // Hull detection: shipPartType == "Hu"
            if (string.Equals(shipPartType, "Hu", StringComparison.OrdinalIgnoreCase))
            {
                return "Hull";
            }

            // Flatpack detection: resourceName contains "Flatpack"
            if (resourceName.IndexOf("Flatpack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ParseFlatpackType(resourceName);
            }

            // Fallback to ship part type mapping
            if (!string.IsNullOrEmpty(shipPartType))
            {
                return MapShipPartType(shipPartType);
            }

            return string.Empty;
        }

        /// <summary>
        /// Parses a flatpack type from the resource name.
        /// Extracts the structure type from names like "Mining Rig Flatpack" → "Flatpacks/MiningRig".
        /// </summary>
        /// <param name="resourceName">The resource name containing "Flatpack".</param>
        /// <returns>The flatpack BluePrintType string.</returns>
        private static string ParseFlatpackType(string resourceName)
        {
            // Remove " Flatpack" suffix to get the structure name
            var idx = resourceName.IndexOf(" Flatpack", StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                var structureName = resourceName.Substring(0, idx).Trim();
                var normalized = structureName.Replace(" ", string.Empty);
                return BlueprintTypePrefixes.Flatpacks + normalized;
            }

            return BlueprintTypePrefixes.Flatpacks + resourceName.Replace(" ", string.Empty);
        }

        /// <summary>
        /// Maps a shipPartType code to the corresponding BlueprintTypes constant.
        /// Stub implementation — full lookup table will be added in task 5.2.
        /// </summary>
        /// <param name="shipPartType">The ship part type code from the API.</param>
        /// <returns>The mapped BluePrintType string.</returns>
        private string MapShipPartType(string shipPartType)
        {
            // Full implementation in task 5.2
            switch (shipPartType)
            {
                case "Sh": return BlueprintTypes.Shield;
                case "Re": return BlueprintTypes.Reactor;
                case "Nc": return BlueprintTypes.NavComp;
                case "Jd": return BlueprintTypes.JumpDrive;
                case "Oh": return BlueprintTypes.OreHopper;
                case "Ml": return BlueprintTypes.MiningLaser;
                case "Ag": return BlueprintTypes.AsteroidGrapple;
                case "Be": return BlueprintTypes.Beamer;
                case "Cg": return BlueprintTypes.Coilgun;
                case "Rg": return BlueprintTypes.Railgun;
                case "Ms": return BlueprintTypes.MissileLauncher;
                case "Tp": return BlueprintTypes.TorpedoLauncher;
                case "Hu": return "Hull";
                default:
                    Log.Warn(
                        "BlueprintLinkage: unknown shipPartType '{0}', using raw value",
                        shipPartType);
                    return shipPartType;
            }
        }

        /// <summary>
        /// Merges API properties into an existing blueprint's PropertyBag (additive merge).
        /// Stub implementation — full logic will be added in task 5.2.
        /// </summary>
        /// <param name="blueprint">The existing blueprint to update.</param>
        /// <param name="apiProperties">The API properties to merge.</param>
        private void MergeProperties(Blueprint blueprint, List<GameApiAssetItemProperty> apiProperties)
        {
            // Full implementation in task 5.2
            if (blueprint.Properties == null)
            {
                blueprint.Properties = new PropertyBag();
            }

            foreach (var prop in apiProperties)
            {
                var key = prop.FriendlyPropertyName;
                if (string.IsNullOrEmpty(key))
                {
                    key = prop.PropertyName;
                }

                blueprint.Properties.SetProperty(key, prop.PropertyValue.ToString());

                var origKey = "_orig_" + key;
                blueprint.Properties.SetProperty(origKey, prop.OriginalPropertyValue.ToString());
            }
        }

        /// <summary>
        /// Builds a fresh PropertyBag on a new blueprint from API properties.
        /// Stub implementation — full logic will be added in task 5.2.
        /// </summary>
        /// <param name="blueprint">The new blueprint to populate.</param>
        /// <param name="apiProperties">The API properties to set.</param>
        private void BuildPropertyBag(Blueprint blueprint, List<GameApiAssetItemProperty> apiProperties)
        {
            // Full implementation in task 5.2
            blueprint.Properties = new PropertyBag();
            foreach (var prop in apiProperties)
            {
                var key = prop.FriendlyPropertyName;
                if (string.IsNullOrEmpty(key))
                {
                    key = prop.PropertyName;
                }

                blueprint.Properties.SetProperty(key, prop.PropertyValue.ToString());

                var origKey = "_orig_" + key;
                blueprint.Properties.SetProperty(origKey, prop.OriginalPropertyValue.ToString());
            }
        }

        /// <summary>
        /// Registers property type metadata from API properties into the global registry.
        /// Stub implementation — full logic will be added in task 5.2.
        /// </summary>
        /// <param name="apiProperties">The API properties containing type metadata.</param>
        private void RegisterPropertyTypes(List<GameApiAssetItemProperty> apiProperties)
        {
            // Full implementation in task 5.2
            foreach (var prop in apiProperties)
            {
                var definition = new PropertyTypeDefinition
                {
                    ModTypeId = prop.ModTypeId,
                    PropertyName = prop.PropertyName ?? string.Empty,
                    FriendlyPropertyName = prop.FriendlyPropertyName ?? string.Empty,
                    Unit = prop.Unit ?? string.Empty,
                    ResearchPositive = prop.ResearchPositive,
                    CanResearch = prop.CanResearch,
                };

                _empireContext.UpsertPropertyType(definition);
            }
        }
    }
}
