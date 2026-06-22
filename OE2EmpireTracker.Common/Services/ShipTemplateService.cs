using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all ShipTemplate mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus JSON deserialization and migration code) mutates ShipTemplate objects.
    /// </summary>
    public class ShipTemplateService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public ShipTemplateService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing ship template, persists, and fires the change event.
        /// Throws InvalidOperationException if UUID not found.
        /// </summary>
        public ReadOnlyShipTemplate Update(string uuid, ShipTemplateUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var template = _playerContext.FindMutableShipTemplate(uuid);
            if (template == null)
            {
                throw new InvalidOperationException("ShipTemplate not found: " + uuid);
            }

            Log.Info(
                "ShipTemplateService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid,
                template.Name,
                request.Name);

            template.Name = request.Name;
            template.HullBlueprintUUID = request.HullBlueprintUUID;
            template.Components = DeepCopyComponents(request.Components);

            _playerContext.MarkDirty<ShipTemplate>(template.UUID);
            _playerContext.WriteContext();
            _playerContext.OnShipTemplateDataChanged(uuid);
            return new ReadOnlyShipTemplate(template);
        }

        /// <summary>
        /// Creates a new ship template, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyShipTemplate Create(ShipTemplateCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var template = new ShipTemplate();
            template.UUID = Guid.NewGuid().ToString();
            template.OwnerUUID = _playerContext.CurrentPlayerUUID;
            template.Name = request.Name;
            template.HullBlueprintUUID = request.HullBlueprintUUID;
            template.Components = DeepCopyComponents(request.Components);

            Log.Info(
                "ShipTemplateService.Create: name='{0}' UUID={1}",
                template.Name,
                template.UUID);

            _playerContext.AddShipTemplate(template);
            _playerContext.MarkDirty<ShipTemplate>(template.UUID);
            _playerContext.WriteContext();
            _playerContext.OnShipTemplateDataChanged(template.UUID);
            return new ReadOnlyShipTemplate(template);
        }

        /// <summary>
        /// Removes a ship template. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var template = _playerContext.FindMutableShipTemplate(uuid);
            if (template == null)
            {
                return;
            }

            Log.Info("ShipTemplateService.Delete: UUID={0} name='{1}'", uuid, template.Name);

            _playerContext.RemoveShipTemplate(template);
            _playerContext.MarkDeleted<ShipTemplate>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnShipTemplateDataChanged(uuid);
        }

        private static List<ShipComponentSlot> DeepCopyComponents(List<ShipComponentSlot> source)
        {
            var copy = new List<ShipComponentSlot>();
            if (source != null)
            {
                foreach (var c in source)
                {
                    copy.Add(new ShipComponentSlot
                    {
                        SlotType = c.SlotType,
                        SlotIndex = c.SlotIndex,
                        BlueprintUUID = c.BlueprintUUID,
                        CurrentHP = c.CurrentHP,
                        MaxHP = c.MaxHP,
                        MaxRepairPercent = c.MaxRepairPercent,
                    });
                }
            }

            return copy;
        }
    }
}