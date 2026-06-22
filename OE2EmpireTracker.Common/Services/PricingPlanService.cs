using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all PricingPlan mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus deserialization and migration) mutates PricingPlan objects.
    /// </summary>
    public class PricingPlanService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public PricingPlanService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing pricing plan, persists, and fires the change event.
        /// </summary>
        public ReadOnlyPricingPlan Update(string uuid, PricingPlanUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var plan = _playerContext.FindMutablePricingPlan(uuid);
            if (plan == null) throw new InvalidOperationException("PricingPlan not found: " + uuid);

            Log.Info(
                "PricingPlanService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid, plan.Name, request.Name);

            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.FixedCostPerItem = request.FixedCostPerItem;
            plan.HourlyCostRate = request.HourlyCostRate;
            plan.ResourcePrices = new Dictionary<string, decimal>(request.ResourcePrices);

            _playerContext.MarkDirty<PricingPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnPricingDataChanged();
            return new ReadOnlyPricingPlan(plan);
        }

        /// <summary>
        /// Creates a new pricing plan, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyPricingPlan Create(PricingPlanCreateRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var plan = new PricingPlan();
            plan.UUID = Guid.NewGuid().ToString();
            plan.OwnerUUID = _playerContext.CurrentPlayerUUID;
            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.FixedCostPerItem = request.FixedCostPerItem;
            plan.HourlyCostRate = request.HourlyCostRate;
            plan.ResourcePrices = new Dictionary<string, decimal>(request.ResourcePrices);

            Log.Info(
                "PricingPlanService.Create: name='{0}' UUID={1}",
                plan.Name, plan.UUID);

            _playerContext.AddPricingPlan(plan);
            _playerContext.MarkDirty<PricingPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnPricingDataChanged();
            return new ReadOnlyPricingPlan(plan);
        }

        /// <summary>
        /// Removes a pricing plan. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;

            var plan = _playerContext.FindMutablePricingPlan(uuid);
            if (plan == null) return;

            Log.Info("PricingPlanService.Delete: UUID={0} name='{1}'", uuid, plan.Name);

            _playerContext.RemovePricingPlan(plan);
            _playerContext.MarkDeleted<PricingPlan>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnPricingDataChanged();
        }
    }
}
