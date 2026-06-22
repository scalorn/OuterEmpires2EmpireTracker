using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all SupplyChain mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus JSON deserialization and migration code) mutates SupplyChain objects.
    /// </summary>
    public class SupplyChainMutationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public SupplyChainMutationService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing supply chain.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlySupplyChain Update(string uuid, SupplyChainUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var chain = _playerContext.FindMutableSupplyChain(uuid);
            if (chain == null)
            {
                throw new InvalidOperationException("SupplyChain not found: " + uuid);
            }

            Log.Info(
                "SupplyChainMutationService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid,
                chain.Name,
                request.Name);

            chain.Name = request.Name;
            chain.IsActive = request.IsActive;
            chain.Stages = DeepCopyStages(request.Stages);
            RenumberStages(chain.Stages);

            _playerContext.MarkDirty<SupplyChain>(chain.UUID);
            _playerContext.WriteContext();
            _playerContext.OnSupplyChainDataChanged(uuid);
            return new ReadOnlySupplyChain(chain);
        }

        /// <summary>
        /// Creates a new supply chain, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlySupplyChain Create(SupplyChainCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var chain = new SupplyChain();
            chain.UUID = Guid.NewGuid().ToString();
            chain.OwnerUUID = _playerContext.CurrentPlayerUUID;
            chain.Name = request.Name;
            chain.IsActive = request.IsActive;
            chain.Stages = DeepCopyStages(request.Stages);
            RenumberStages(chain.Stages);

            Log.Info(
                "SupplyChainMutationService.Create: name='{0}' UUID={1}",
                chain.Name,
                chain.UUID);

            _playerContext.AddSupplyChain(chain);
            _playerContext.MarkDirty<SupplyChain>(chain.UUID);
            _playerContext.WriteContext();
            _playerContext.OnSupplyChainDataChanged(chain.UUID);
            return new ReadOnlySupplyChain(chain);
        }

        /// <summary>
        /// Removes a supply chain. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var chain = _playerContext.FindMutableSupplyChain(uuid);
            if (chain == null)
            {
                return;
            }

            Log.Info("SupplyChainMutationService.Delete: UUID={0} name='{1}'", uuid, chain.Name);

            _playerContext.RemoveSupplyChain(chain);
            _playerContext.MarkDeleted<SupplyChain>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnSupplyChainDataChanged(uuid);
        }

        private static void RenumberStages(List<SupplyChainStage> stages)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                stages[i].Sequence = i + 1;
            }
        }

        private static List<SupplyChainStage> DeepCopyStages(List<SupplyChainStage> source)
        {
            var copy = new List<SupplyChainStage>();
            if (source != null)
            {
                foreach (var s in source)
                {
                    copy.Add(new SupplyChainStage
                    {
                        Sequence = s.Sequence,
                        StageType = s.StageType,
                        LocationType = s.LocationType,
                        LocationUUID = s.LocationUUID,
                        ResourceName = s.ResourceName,
                        ResourcePurity = s.ResourcePurity,
                        AccumulationThreshold = s.AccumulationThreshold,
                        ProductionRatePerHour = s.ProductionRatePerHour,
                        DeliveryRouteUUID = s.DeliveryRouteUUID,
                    });
                }
            }

            return copy;
        }
    }
}