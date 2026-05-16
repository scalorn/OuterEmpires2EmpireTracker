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
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Protected property names that are preserved when merging resources into an existing blueprint.
        /// </summary>
        private static readonly HashSet<string> ProtectedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BlueprintPropertyKeys.ManufactureRunTime,
            GameConstants.PropPowerRequired
        };

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