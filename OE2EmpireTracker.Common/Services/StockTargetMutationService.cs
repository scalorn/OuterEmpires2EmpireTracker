using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all StockPlan and StockProfile mutation. The form and ViewModel never touch
    /// the entities directly. Only this service (plus JSON deserialization and migration code)
    /// mutates StockPlan and StockProfile objects.
    /// </summary>
    public class StockTargetMutationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public StockTargetMutationService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // === Plan CRUD ===

        /// <summary>
        /// Applies changes from the update request to an existing stock plan.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyStockPlan UpdatePlan(string uuid, StockPlanUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var plan = _playerContext.FindMutableStockPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("StockPlan not found: " + uuid);
            }

            Log.Info(
                "StockTargetMutationService.UpdatePlan: UUID={0} name='{1}' -> '{2}'",
                uuid,
                plan.Name,
                request.Name);

            plan.Name = request.Name;
            plan.IsActive = request.IsActive;
            plan.ReplenishmentBuildPlanUUID = request.ReplenishmentBuildPlanUUID;
            plan.Targets = DeepCopyTargets(request.Targets);

            _playerContext.MarkDirty<StockPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(uuid);
            return new ReadOnlyStockPlan(plan);
        }

        /// <summary>
        /// Creates a new stock plan, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyStockPlan CreatePlan(StockPlanCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var plan = new StockPlan();
            plan.UUID = Guid.NewGuid().ToString();
            plan.OwnerUUID = _playerContext.CurrentPlayerUUID;
            plan.Name = request.Name;
            plan.IsActive = request.IsActive;
            plan.ReplenishmentBuildPlanUUID = request.ReplenishmentBuildPlanUUID;
            plan.Targets = DeepCopyTargets(request.Targets);

            Log.Info(
                "StockTargetMutationService.CreatePlan: name='{0}' UUID={1}",
                plan.Name,
                plan.UUID);

            _playerContext.AddStockPlan(plan);
            _playerContext.MarkDirty<StockPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(plan.UUID);
            return new ReadOnlyStockPlan(plan);
        }

        /// <summary>
        /// Removes a stock plan. No-op if UUID is empty or not found.
        /// </summary>
        public void DeletePlan(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var plan = _playerContext.FindMutableStockPlan(uuid);
            if (plan == null)
            {
                return;
            }

            Log.Info("StockTargetMutationService.DeletePlan: UUID={0} name='{1}'", uuid, plan.Name);

            _playerContext.RemoveStockPlan(plan);
            _playerContext.MarkDeleted<StockPlan>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(uuid);
        }

        // === Profile CRUD ===

        /// <summary>
        /// Applies changes from the update request to an existing stock profile.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyStockProfile UpdateProfile(string uuid, StockProfileUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = _playerContext.FindMutableStockProfile(uuid);
            if (profile == null)
            {
                throw new InvalidOperationException("StockProfile not found: " + uuid);
            }

            Log.Info(
                "StockTargetMutationService.UpdateProfile: UUID={0} name='{1}' -> '{2}'",
                uuid,
                profile.Name,
                request.Name);

            profile.Name = request.Name;
            profile.IsActive = request.IsActive;
            profile.Entries = DeepCopyEntries(request.Entries);

            _playerContext.MarkDirty<StockProfile>(profile.UUID);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(uuid);
            return new ReadOnlyStockProfile(profile);
        }

        /// <summary>
        /// Creates a new stock profile, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyStockProfile CreateProfile(StockProfileCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var profile = new StockProfile();
            profile.UUID = Guid.NewGuid().ToString();
            profile.OwnerUUID = _playerContext.CurrentPlayerUUID;
            profile.Name = request.Name;
            profile.IsActive = request.IsActive;
            profile.Entries = DeepCopyEntries(request.Entries);

            Log.Info(
                "StockTargetMutationService.CreateProfile: name='{0}' UUID={1}",
                profile.Name,
                profile.UUID);

            _playerContext.AddStockProfile(profile);
            _playerContext.MarkDirty<StockProfile>(profile.UUID);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(profile.UUID);
            return new ReadOnlyStockProfile(profile);
        }

        /// <summary>
        /// Removes a stock profile. No-op if UUID is empty or not found.
        /// </summary>
        public void DeleteProfile(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var profile = _playerContext.FindMutableStockProfile(uuid);
            if (profile == null)
            {
                return;
            }

            Log.Info("StockTargetMutationService.DeleteProfile: UUID={0} name='{1}'", uuid, profile.Name);

            _playerContext.RemoveStockProfile(profile);
            _playerContext.MarkDeleted<StockProfile>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnStockDataChanged(uuid);
        }

        // === Private Helpers ===

        private static List<StockTarget> DeepCopyTargets(List<StockTarget> source)
        {
            var copy = new List<StockTarget>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new StockTarget
                    {
                        UUID = s.UUID,
                        ItemType = s.ItemType,
                        ItemReferenceID = s.ItemReferenceID,
                        ItemName = s.ItemName,
                        ShipTemplateUUID = s.ShipTemplateUUID,
                        TargetQuantity = s.TargetQuantity,
                        CriticalThreshold = s.CriticalThreshold,
                        Scope = s.Scope,
                        LocationUUID = s.LocationUUID,
                    });
                }
            }

            return copy;
        }

        private static List<StockProfileEntry> DeepCopyEntries(List<StockProfileEntry> source)
        {
            var copy = new List<StockProfileEntry>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new StockProfileEntry
                    {
                        GroupID = s.GroupID,
                        StockPlanUUID = s.StockPlanUUID,
                    });
                }
            }

            return copy;
        }
    }
}
