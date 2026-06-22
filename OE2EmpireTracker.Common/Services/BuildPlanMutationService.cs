using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all BuildPlan mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus JSON deserialization and migration code) mutates BuildPlan objects.
    /// </summary>
    public class BuildPlanMutationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public BuildPlanMutationService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing build plan.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyBuildPlan Update(string uuid, BuildPlanUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var plan = _playerContext.FindMutableBuildPlan(uuid);
            if (plan == null)
            {
                throw new InvalidOperationException("BuildPlan not found: " + uuid);
            }

            Log.Info(
                "BuildPlanMutationService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid,
                plan.Name,
                request.Name);

            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.IsActive = request.IsActive;
            plan.Items = DeepCopyItems(request.Items);

            _playerContext.MarkDirty<BuildPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnBuildPlanDataChanged(uuid);
            return new ReadOnlyBuildPlan(plan);
        }

        /// <summary>
        /// Creates a new build plan, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyBuildPlan Create(BuildPlanCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var plan = new BuildPlan();
            plan.UUID = Guid.NewGuid().ToString();
            plan.OwnerUUID = _playerContext.CurrentPlayerUUID;
            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.IsActive = request.IsActive;
            plan.Items = DeepCopyItems(request.Items);

            Log.Info(
                "BuildPlanMutationService.Create: name='{0}' UUID={1}",
                plan.Name,
                plan.UUID);

            _playerContext.AddBuildPlan(plan);
            _playerContext.MarkDirty<BuildPlan>(plan.UUID);
            _playerContext.WriteContext();
            _playerContext.OnBuildPlanDataChanged(plan.UUID);
            return new ReadOnlyBuildPlan(plan);
        }

        /// <summary>
        /// Removes a build plan. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var plan = _playerContext.FindMutableBuildPlan(uuid);
            if (plan == null)
            {
                return;
            }

            Log.Info("BuildPlanMutationService.Delete: UUID={0} name='{1}'", uuid, plan.Name);

            _playerContext.RemoveBuildPlan(plan);
            _playerContext.MarkDeleted<BuildPlan>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnBuildPlanDataChanged(uuid);
        }

        private static List<BuildItem> DeepCopyItems(List<BuildItem> source)
        {
            var copy = new List<BuildItem>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new BuildItem
                    {
                        UUID = s.UUID,
                        ItemType = s.ItemType,
                        Status = s.Status,
                        BlueprintUUID = s.BlueprintUUID,
                        ItemName = s.ItemName,
                        CommodityName = s.CommodityName,
                        ShipTemplateUUID = s.ShipTemplateUUID,
                        Quantity = s.Quantity,
                        BuildLocationType = s.BuildLocationType,
                        BuildLocationUUID = s.BuildLocationUUID,
                        StructureUUID = s.StructureUUID,
                        AssemblyLocationType = s.AssemblyLocationType,
                        AssemblyLocationUUID = s.AssemblyLocationUUID,
                        ParentBuildItemUUID = s.ParentBuildItemUUID,
                        Recipient = s.Recipient,
                        Notes = s.Notes,
                        SequenceInStructure = s.SequenceInStructure,
                        DependsOnUUID = s.DependsOnUUID,
                        MiningResource = s.MiningResource,
                        MiningSurveyUUID = s.MiningSurveyUUID,
                        RefiningResource = s.RefiningResource,
                        RefiningPurity = s.RefiningPurity,
                    });
                }
            }

            return copy;
        }
    }
}
