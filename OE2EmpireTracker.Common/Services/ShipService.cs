// -----------------------------------------------------------------------
// <copyright file="ShipService.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Ship mutation. The form and ViewModel never touch the entity directly.
    /// </summary>
    public class ShipService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public ShipService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>Applies changes from the update request to an existing ship.</summary>
        public ReadOnlyShip Update(string uuid, ShipUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var ship = _playerContext.FindMutableShip(uuid);
            if (ship == null)
            {
                throw new InvalidOperationException("Ship not found: " + uuid);
            }

            Log.Info("ShipService.Update: UUID={0} name='{1}' -> '{2}'", uuid, ship.Name, request.Name);

            ship.Name = request.Name;
            ship.TemplateUUID = request.TemplateUUID;
            ship.HullBlueprintUUID = request.HullBlueprintUUID;
            ship.LocationType = request.LocationType;
            ship.LocationUUID = request.LocationUUID;
            ship.HullCurrentHP = request.HullCurrentHP;
            ship.HullMaxHP = request.HullMaxHP;
            ship.HullMaxRepairPercent = request.HullMaxRepairPercent;
            ship.Components = DeepCopyComponents(request.Components);
            ship.Cargo = DeepCopyItemBag(request.Cargo);
            ship.Hopper = DeepCopyItemBag(request.Hopper);

            _playerContext.MarkDirty<Ship>(ship.UUID);
            _playerContext.WriteContext();
            _playerContext.OnShipDataChanged(uuid);
            return new ReadOnlyShip(ship);
        }

        /// <summary>Creates a new ship with a generated UUID.</summary>
        public ReadOnlyShip Create(ShipCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var ship = new Ship();
            ship.UUID = Guid.NewGuid().ToString();
            ship.OwnerUUID = _playerContext.CurrentPlayerUUID;
            ship.Name = string.IsNullOrWhiteSpace(request.Name) ? "New Ship" : request.Name;

            Log.Info("ShipService.Create: name='{0}' UUID={1}", ship.Name, ship.UUID);

            _playerContext.AddShip(ship);
            _playerContext.MarkDirty<Ship>(ship.UUID);
            _playerContext.WriteContext();
            _playerContext.OnShipDataChanged(ship.UUID);
            return new ReadOnlyShip(ship);
        }

        /// <summary>Removes a ship. No-op if UUID is empty or not found.</summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var ship = _playerContext.FindMutableShip(uuid);
            if (ship == null)
            {
                return;
            }

            Log.Info("ShipService.Delete: UUID={0} name='{1}'", uuid, ship.Name);

            _playerContext.RemoveShip(ship);
            _playerContext.MarkDeleted<Ship>(uuid);
            _playerContext.WriteContext();
            _playerContext.OnShipDataChanged(uuid);
        }

        /// <summary>Creates a ship from a template.</summary>
        public ReadOnlyShip CreateFromTemplate(string templateUUID)
        {
            if (string.IsNullOrEmpty(templateUUID))
            {
                throw new ArgumentNullException(nameof(templateUUID));
            }

            var template = _playerContext.FindShipTemplate(templateUUID);
            if (template == null)
            {
                throw new InvalidOperationException("ShipTemplate not found: " + templateUUID);
            }

            var ship = new Ship();
            ship.UUID = Guid.NewGuid().ToString();
            ship.OwnerUUID = _playerContext.CurrentPlayerUUID;
            ship.Name = template.Name;
            ship.TemplateUUID = template.UUID;
            ship.HullBlueprintUUID = template.HullBlueprintUUID;
            ship.Components = DeepCopyComponents(template.Components);

            Log.Info("ShipService.CreateFromTemplate: name='{0}' UUID={1} template={2}", ship.Name, ship.UUID, templateUUID);

            _playerContext.AddShip(ship);
            _playerContext.MarkDirty<Ship>(ship.UUID);
            _playerContext.WriteContext();
            _playerContext.OnShipDataChanged(ship.UUID);
            return new ReadOnlyShip(ship);
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

        private static ItemBag DeepCopyItemBag(ItemBag source)
        {
            var copy = new ItemBag();
            if (source == null)
            {
                return copy;
            }

            foreach (var kvp in source.Items)
            {
                copy.AddItem(DeepCopyItem(kvp.Value));
            }

            return copy;
        }

        private static Item DeepCopyItem(Item source)
        {
            var copy = new Item
            {
                UUID = source.UUID,
                ItemType = source.ItemType,
                BaseItemTypeID = source.BaseItemTypeID,
                Name = source.Name,
                NickName = source.NickName,
                Description = source.Description,
                Quantity = source.Quantity,
                ResourcePurity = source.ResourcePurity,
                Volume = source.Volume,
                CurrentHP = source.CurrentHP,
                MaxHP = source.MaxHP,
                MaxRepairPercent = source.MaxRepairPercent,
            };

            if (source.Contents != null)
            {
                copy.Contents = DeepCopyItemBag(source.Contents);
            }

            return copy;
        }
    }
}
