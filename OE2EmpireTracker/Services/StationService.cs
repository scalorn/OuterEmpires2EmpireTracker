// -----------------------------------------------------------------------
// <copyright file="StationService.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Station mutation. The form and ViewModel never touch the entity directly.
    /// </summary>
    public class StationService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public StationService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>Applies changes from the update request to an existing station.</summary>
        public ReadOnlyStation Update(string uuid, StationUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var station = _playerContext.FindMutableStation(uuid);
            if (station == null)
            {
                throw new InvalidOperationException("Station not found: " + uuid);
            }

            Log.Info("StationService.Update: UUID={0} name='{1}' -> '{2}'", uuid, station.Name, request.Name);

            station.Name = request.Name;
            station.StationType = request.StationType;
            station.Ownership = request.Ownership;
            station.StationBlueprintUUID = request.StationBlueprintUUID;
            station.HullCurrentHP = request.HullCurrentHP;
            station.HullMaxHP = request.HullMaxHP;
            station.HullMaxRepairPercent = request.HullMaxRepairPercent;
            station.Components = DeepCopyComponents(request.Components);

            // Replace the current player's hold in the Holds dictionary
            string playerUUID = _playerContext.CurrentPlayerUUID;
            if (!string.IsNullOrEmpty(playerUUID))
            {
                station.Holds[playerUUID] = DeepCopyItemBag(request.Hold);
            }

            station.MunitionsHold = DeepCopyItemBag(request.MunitionsHold);

            _playerContext.WriteContext();
            _playerContext.OnStationDataChanged();
            return new ReadOnlyStation(station);
        }

        /// <summary>Creates a new station with a generated UUID.</summary>
        public ReadOnlyStation Create(StationCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var station = new Station();
            station.UUID = Guid.NewGuid().ToString();
            station.OwnerUUID = _playerContext.CurrentPlayerUUID;
            station.Name = string.IsNullOrWhiteSpace(request.Name) ? "New Station" : request.Name;

            Log.Info("StationService.Create: name='{0}' UUID={1}", station.Name, station.UUID);

            _playerContext.AddStation(station);
            _playerContext.WriteContext();
            _playerContext.OnStationDataChanged();
            return new ReadOnlyStation(station);
        }

        /// <summary>Removes a station. No-op if UUID is empty or not found.</summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var station = _playerContext.FindMutableStation(uuid);
            if (station == null)
            {
                return;
            }

            Log.Info("StationService.Delete: UUID={0} name='{1}'", uuid, station.Name);

            _playerContext.RemoveStation(station);
            _playerContext.WriteContext();
            _playerContext.OnStationDataChanged();
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
