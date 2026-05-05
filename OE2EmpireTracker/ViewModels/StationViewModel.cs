// -----------------------------------------------------------------------
// <copyright file="StationViewModel.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Disconnected edit buffer for Station entities. Holds local copies of all station fields.
    /// Changes accumulate here until Save routes them through StationService.
    /// </summary>
    public class StationViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyStation _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private StationType _stationType = StationType.Station;
        private StationOwnership _ownership = StationOwnership.Government;
        private string _stationBlueprintUUID = string.Empty;
        private int _hullCurrentHP;
        private int _hullMaxHP;
        private decimal _hullMaxRepairPercent;
        private List<ShipComponentSlot> _components = new List<ShipComponentSlot>();
        private ItemBag _hold = new ItemBag();
        private ItemBag _munitionsHold = new ItemBag();

        /// <summary>Gets the station UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the owner UUID.</summary>
        public string OwnerUUID => _ownerUUID;

        /// <summary>Gets the original ReadOnlyStation snapshot.</summary>
        public ReadOnlyStation Original => _original;

        /// <summary>Gets a value indicating whether this is a new unsaved station.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets a value indicating whether any field has been modified.</summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name);
                }

                if (_name != (_original.Name ?? string.Empty))
                {
                    return true;
                }

                if (_stationType != _original.StationType)
                {
                    return true;
                }

                if (_ownership != _original.Ownership)
                {
                    return true;
                }

                if (_stationBlueprintUUID != (_original.StationBlueprintUUID ?? string.Empty))
                {
                    return true;
                }

                if (_hullCurrentHP != _original.HullCurrentHP)
                {
                    return true;
                }

                if (_hullMaxHP != _original.HullMaxHP)
                {
                    return true;
                }

                if (_hullMaxRepairPercent != _original.HullMaxRepairPercent)
                {
                    return true;
                }

                if (!ComponentsMatch(_components, _original.Components))
                {
                    return true;
                }

                if (!ItemBagMatch(_hold, _original.Holds, _ownerUUID))
                {
                    return true;
                }

                if (!ItemBagMatchDirect(_munitionsHold, _original.MunitionsHold))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>Gets or sets the station name.</summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>Gets or sets the station type.</summary>
        public StationType StationType
        {
            get => _stationType;
            set => _stationType = value;
        }

        /// <summary>Gets or sets the ownership type.</summary>
        public StationOwnership Ownership
        {
            get => _ownership;
            set => _ownership = value;
        }

        /// <summary>Gets or sets the station blueprint UUID.</summary>
        public string StationBlueprintUUID
        {
            get => _stationBlueprintUUID;
            set => _stationBlueprintUUID = value;
        }

        /// <summary>Gets or sets the hull current HP.</summary>
        public int HullCurrentHP
        {
            get => _hullCurrentHP;
            set => _hullCurrentHP = value;
        }

        /// <summary>Gets or sets the hull max HP.</summary>
        public int HullMaxHP
        {
            get => _hullMaxHP;
            set => _hullMaxHP = value;
        }

        /// <summary>Gets or sets the hull max repair percent.</summary>
        public decimal HullMaxRepairPercent
        {
            get => _hullMaxRepairPercent;
            set => _hullMaxRepairPercent = value;
        }

        /// <summary>Gets the local components list.</summary>
        public List<ShipComponentSlot> Components => _components;

        /// <summary>Gets the local hold bag.</summary>
        public ItemBag Hold => _hold;

        /// <summary>Gets the local munitions hold bag.</summary>
        public ItemBag MunitionsHold => _munitionsHold;

        /// <summary>Copies all fields from a ReadOnlyStation into local properties.</summary>
        public void LoadFrom(ReadOnlyStation ro, string playerUUID)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _stationType = ro.StationType;
            _ownership = ro.Ownership;
            _stationBlueprintUUID = ro.StationBlueprintUUID ?? string.Empty;
            _hullCurrentHP = ro.HullCurrentHP;
            _hullMaxHP = ro.HullMaxHP;
            _hullMaxRepairPercent = ro.HullMaxRepairPercent;
            _components = DeepCopyComponents(ro.Components);

            // Deep-copy the current player's hold from the station's Holds dictionary
            if (ro.Holds != null && !string.IsNullOrEmpty(playerUUID) && ro.Holds.TryGetValue(playerUUID, out var roHold))
            {
                _hold = DeepCopyReadOnlyItemBag(roHold);
            }
            else
            {
                _hold = new ItemBag();
            }

            _munitionsHold = DeepCopyReadOnlyItemBag(ro.MunitionsHold);
        }

        /// <summary>Clears all fields to defaults and sets original to null.</summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _stationType = StationType.Station;
            _ownership = StationOwnership.Government;
            _stationBlueprintUUID = string.Empty;
            _hullCurrentHP = 0;
            _hullMaxHP = 0;
            _hullMaxRepairPercent = 0m;
            _components = new List<ShipComponentSlot>();
            _hold = new ItemBag();
            _munitionsHold = new ItemBag();
        }

        /// <summary>Creates a StationUpdateRequest from local state.</summary>
        public StationUpdateRequest BuildUpdateRequest()
        {
            return new StationUpdateRequest
            {
                Original = _original,
                Name = _name,
                StationType = _stationType,
                Ownership = _ownership,
                StationBlueprintUUID = _stationBlueprintUUID,
                HullCurrentHP = _hullCurrentHP,
                HullMaxHP = _hullMaxHP,
                HullMaxRepairPercent = _hullMaxRepairPercent,
                Components = DeepCopyComponents(_components),
                Hold = DeepCopyItemBag(_hold),
                MunitionsHold = DeepCopyItemBag(_munitionsHold),
            };
        }

        /// <summary>Creates a StationCreateRequest from local state.</summary>
        public StationCreateRequest BuildCreateRequest()
        {
            return new StationCreateRequest
            {
                Name = _name,
            };
        }

        /// <summary>Updates hull HP fields.</summary>
        public void SetHullComponent(int currentHP, decimal maxRepairPercent)
        {
            _hullCurrentHP = currentHP;
            _hullMaxRepairPercent = maxRepairPercent;
        }

        /// <summary>Updates component HP fields.</summary>
        public void SetComponentCondition(string slotType, int slotIndex, int currentHP, decimal maxRepairPercent)
        {
            var existing = _components
                .FirstOrDefault(c => c.SlotType == slotType && c.SlotIndex == slotIndex);

            if (existing != null)
            {
                existing.CurrentHP = currentHP;
                existing.MaxRepairPercent = maxRepairPercent;
            }
        }

        /// <summary>Clears the local Components list.</summary>
        public void ClearComponents()
        {
            _components.Clear();
        }

        /// <summary>Adds an item to the local Hold bag.</summary>
        public void AddHoldItem(Item item)
        {
            _hold.AddItem(item);
        }

        /// <summary>Removes an item from the local Hold bag.</summary>
        public void RemoveHoldItem(string uuid)
        {
            _hold.Remove(uuid);
        }

        /// <summary>Adds an item to the local MunitionsHold bag.</summary>
        public void AddMunitionsItem(Item item)
        {
            _munitionsHold.AddItem(item);
        }

        /// <summary>Removes an item from the local MunitionsHold bag.</summary>
        public void RemoveMunitionsItem(string uuid)
        {
            _munitionsHold.Remove(uuid);
        }

        private static List<ShipComponentSlot> DeepCopyComponents(IReadOnlyList<ReadOnlyShipComponentSlot> source)
        {
            var copy = new List<ShipComponentSlot>();
            if (source == null)
            {
                return copy;
            }

            foreach (var c in source)
            {
                copy.Add(new ShipComponentSlot
                {
                    SlotType = c.SlotType ?? string.Empty,
                    SlotIndex = c.SlotIndex,
                    BlueprintUUID = c.BlueprintUUID ?? string.Empty,
                    CurrentHP = c.CurrentHP,
                    MaxHP = c.MaxHP,
                    MaxRepairPercent = c.MaxRepairPercent,
                });
            }

            return copy;
        }

        private static List<ShipComponentSlot> DeepCopyComponents(List<ShipComponentSlot> source)
        {
            var copy = new List<ShipComponentSlot>(source.Count);
            foreach (var c in source)
            {
                copy.Add(new ShipComponentSlot
                {
                    SlotType = c.SlotType ?? string.Empty,
                    SlotIndex = c.SlotIndex,
                    BlueprintUUID = c.BlueprintUUID ?? string.Empty,
                    CurrentHP = c.CurrentHP,
                    MaxHP = c.MaxHP,
                    MaxRepairPercent = c.MaxRepairPercent,
                });
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

        private static ItemBag DeepCopyReadOnlyItemBag(ReadOnlyItemBag source)
        {
            var copy = new ItemBag();
            if (source == null)
            {
                return copy;
            }

            foreach (var roItem in source.GetAllItems())
            {
                copy.AddItem(DeepCopyReadOnlyItem(roItem));
            }

            return copy;
        }

        private static Item DeepCopyReadOnlyItem(ReadOnlyItem source)
        {
            var copy = new Item
            {
                UUID = source.UUID,
                ItemType = source.ItemType,
                BaseItemTypeID = source.BaseItemTypeID ?? string.Empty,
                Name = source.Name ?? string.Empty,
                NickName = source.NickName ?? string.Empty,
                Description = source.Description ?? string.Empty,
                Quantity = source.Quantity,
                ResourcePurity = source.ResourcePurity ?? string.Empty,
                Volume = source.Volume,
                CurrentHP = source.CurrentHP,
                MaxHP = source.MaxHP,
                MaxRepairPercent = source.MaxRepairPercent,
            };

            if (source.Contents != null)
            {
                copy.Contents = DeepCopyReadOnlyItemBag(source.Contents);
            }

            return copy;
        }

        private static Item DeepCopyItem(Item source)
        {
            var copy = new Item
            {
                UUID = source.UUID,
                ItemType = source.ItemType,
                BaseItemTypeID = source.BaseItemTypeID ?? string.Empty,
                Name = source.Name ?? string.Empty,
                NickName = source.NickName ?? string.Empty,
                Description = source.Description ?? string.Empty,
                Quantity = source.Quantity,
                ResourcePurity = source.ResourcePurity ?? string.Empty,
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

        private static bool ComponentsMatch(List<ShipComponentSlot> local, IReadOnlyList<ReadOnlyShipComponentSlot> original)
        {
            if (original == null)
            {
                return local.Count == 0;
            }

            if (local.Count != original.Count)
            {
                return false;
            }

            for (int i = 0; i < local.Count; i++)
            {
                var lc = local[i];
                var orig = original[i];
                if (lc.SlotType != orig.SlotType
                    || lc.SlotIndex != orig.SlotIndex
                    || lc.BlueprintUUID != orig.BlueprintUUID
                    || lc.CurrentHP != orig.CurrentHP
                    || lc.MaxHP != orig.MaxHP
                    || lc.MaxRepairPercent != orig.MaxRepairPercent)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ItemBagMatch(ItemBag local, IReadOnlyDictionary<string, ReadOnlyItemBag> holds, string playerUUID)
        {
            if (holds == null || string.IsNullOrEmpty(playerUUID) || !holds.TryGetValue(playerUUID, out var originalBag))
            {
                return local.Count() == 0;
            }

            return ItemBagMatchDirect(local, originalBag);
        }

        private static bool ItemBagMatchDirect(ItemBag local, ReadOnlyItemBag original)
        {
            if (original == null)
            {
                return local.Count() == 0;
            }

            if (local.Count() != original.Count())
            {
                return false;
            }

            foreach (var kvp in local.Items)
            {
                if (!original.ContainsKey(kvp.Key))
                {
                    return false;
                }
            }

            return true;
        }
    }
}