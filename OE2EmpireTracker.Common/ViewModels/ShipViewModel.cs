// -----------------------------------------------------------------------
// <copyright file="ShipViewModel.cs" company="OE2EmpireTracker">
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
    /// Disconnected edit buffer for Ship entities. Holds local copies of all ship fields.
    /// Changes accumulate here until Save routes them through ShipService.
    /// </summary>
    public class ShipViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyShip _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private string _templateUUID = string.Empty;
        private string _hullBlueprintUUID = string.Empty;
        private DestinationType _locationType = DestinationType.Station;
        private string _locationUUID = string.Empty;
        private int _hullCurrentHP;
        private int _hullMaxHP;
        private decimal _hullMaxRepairPercent;
        private List<ShipComponentSlot> _components = new List<ShipComponentSlot>();
        private ItemBag _cargo = new ItemBag();
        private ItemBag _hopper = new ItemBag();

        /// <summary>Gets the ship UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the owner UUID.</summary>
        public string OwnerUUID => _ownerUUID;

        /// <summary>Gets the original ReadOnlyShip snapshot.</summary>
        public ReadOnlyShip Original => _original;

        /// <summary>Gets a value indicating whether this is a new unsaved ship.</summary>
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

                if (_templateUUID != (_original.TemplateUUID ?? string.Empty))
                {
                    return true;
                }

                if (_hullBlueprintUUID != (_original.HullBlueprintUUID ?? string.Empty))
                {
                    return true;
                }

                if (_locationType != _original.LocationType)
                {
                    return true;
                }

                if (_locationUUID != (_original.LocationUUID ?? string.Empty))
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

                if (!ItemBagMatch(_cargo, _original.Cargo))
                {
                    return true;
                }

                if (!ItemBagMatch(_hopper, _original.Hopper))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>Gets or sets the ship name.</summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>Gets or sets the template UUID.</summary>
        public string TemplateUUID
        {
            get => _templateUUID;
            set => _templateUUID = value;
        }

        /// <summary>Gets or sets the hull blueprint UUID.</summary>
        public string HullBlueprintUUID
        {
            get => _hullBlueprintUUID;
            set => _hullBlueprintUUID = value;
        }

        /// <summary>Gets or sets the location type.</summary>
        public DestinationType LocationType
        {
            get => _locationType;
            set => _locationType = value;
        }

        /// <summary>Gets or sets the location UUID.</summary>
        public string LocationUUID
        {
            get => _locationUUID;
            set => _locationUUID = value;
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

        /// <summary>Gets the local cargo bag.</summary>
        public ItemBag Cargo => _cargo;

        /// <summary>Gets the local hopper bag.</summary>
        public ItemBag Hopper => _hopper;

        /// <summary>Copies all fields from a ReadOnlyShip into local properties.</summary>
        public void LoadFrom(ReadOnlyShip ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _templateUUID = ro.TemplateUUID ?? string.Empty;
            _hullBlueprintUUID = ro.HullBlueprintUUID ?? string.Empty;
            _locationType = ro.LocationType;
            _locationUUID = ro.LocationUUID ?? string.Empty;
            _hullCurrentHP = ro.HullCurrentHP;
            _hullMaxHP = ro.HullMaxHP;
            _hullMaxRepairPercent = ro.HullMaxRepairPercent;
            _components = DeepCopyComponents(ro.Components);
            _cargo = DeepCopyReadOnlyItemBag(ro.Cargo);
            _hopper = DeepCopyReadOnlyItemBag(ro.Hopper);
        }

        /// <summary>Clears all fields to defaults and sets original to null.</summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _templateUUID = string.Empty;
            _hullBlueprintUUID = string.Empty;
            _locationType = DestinationType.Station;
            _locationUUID = string.Empty;
            _hullCurrentHP = 0;
            _hullMaxHP = 0;
            _hullMaxRepairPercent = 0m;
            _components = new List<ShipComponentSlot>();
            _cargo = new ItemBag();
            _hopper = new ItemBag();
        }

        /// <summary>Creates a ShipUpdateRequest from local state.</summary>
        public ShipUpdateRequest BuildUpdateRequest()
        {
            return new ShipUpdateRequest
            {
                Original = _original,
                Name = _name,
                TemplateUUID = _templateUUID,
                HullBlueprintUUID = _hullBlueprintUUID,
                LocationType = _locationType,
                LocationUUID = _locationUUID,
                HullCurrentHP = _hullCurrentHP,
                HullMaxHP = _hullMaxHP,
                HullMaxRepairPercent = _hullMaxRepairPercent,
                Components = DeepCopyComponents(_components),
                Cargo = DeepCopyItemBag(_cargo),
                Hopper = DeepCopyItemBag(_hopper),
            };
        }

        /// <summary>Creates a ShipCreateRequest from local state.</summary>
        public ShipCreateRequest BuildCreateRequest()
        {
            return new ShipCreateRequest
            {
                Name = _name,
            };
        }

        /// <summary>Adds or updates a component in the local list.</summary>
        public void SetComponent(string slotType, int slotIndex, string blueprintUUID)
        {
            var existing = _components
                .FirstOrDefault(c => c.SlotType == slotType && c.SlotIndex == slotIndex);

            if (existing != null)
            {
                existing.BlueprintUUID = blueprintUUID;
            }
            else
            {
                _components.Add(new ShipComponentSlot
                {
                    SlotType = slotType,
                    SlotIndex = slotIndex,
                    BlueprintUUID = blueprintUUID,
                });
            }
        }

        /// <summary>Removes a component from the local list.</summary>
        public void RemoveComponent(string slotType, int slotIndex)
        {
            var existing = _components
                .FirstOrDefault(c => c.SlotType == slotType && c.SlotIndex == slotIndex);

            if (existing != null)
            {
                _components.Remove(existing);
            }
        }

        /// <summary>Clears the local Components list.</summary>
        public void ClearComponents()
        {
            _components.Clear();
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

        /// <summary>Adds an item to the local Cargo bag.</summary>
        public void AddCargoItem(Item item)
        {
            _cargo.AddItem(item);
        }

        /// <summary>Removes an item from the local Cargo bag.</summary>
        public void RemoveCargoItem(string uuid)
        {
            _cargo.Remove(uuid);
        }

        /// <summary>Adds an item to the local Hopper bag.</summary>
        public void AddHopperItem(Item item)
        {
            _hopper.AddItem(item);
        }

        /// <summary>Removes an item from the local Hopper bag.</summary>
        public void RemoveHopperItem(string uuid)
        {
            _hopper.Remove(uuid);
        }

        /// <summary>Returns Cargo or Hopper based on flag.</summary>
        public ItemBag GetSelectedBag(bool isHopper)
        {
            return isHopper ? _hopper : _cargo;
        }

        private static List<ShipComponentSlot> DeepCopyComponents(IEnumerable<ReadOnlyShipComponentSlot> source)
        {
            var copy = new List<ShipComponentSlot>();
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

        private static bool ItemBagMatch(ItemBag local, ReadOnlyItemBag original)
        {
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
