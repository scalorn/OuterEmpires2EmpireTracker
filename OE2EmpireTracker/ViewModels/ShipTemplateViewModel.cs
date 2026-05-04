using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class ShipTemplateViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyShipTemplate _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private string _hullBlueprintUUID = string.Empty;
        private List<ShipComponentSlot> _components = new List<ShipComponentSlot>();

        public string UUID => _uuid;

        public string OwnerUUID => _ownerUUID;

        public ReadOnlyShipTemplate Original => _original;

        public bool IsNew => _original == null;

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

                if (_hullBlueprintUUID != (_original.HullBlueprintUUID ?? string.Empty))
                {
                    return true;
                }

                var originalComponents = _original.Components;
                if (_components.Count != originalComponents.Count)
                {
                    return true;
                }

                for (int i = 0; i < _components.Count; i++)
                {
                    var local = _components[i];
                    var orig = originalComponents[i];
                    if (local.SlotType != orig.SlotType
                        || local.SlotIndex != orig.SlotIndex
                        || local.BlueprintUUID != orig.BlueprintUUID
                        || local.CurrentHP != orig.CurrentHP
                        || local.MaxHP != orig.MaxHP
                        || local.MaxRepairPercent != orig.MaxRepairPercent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string HullBlueprintUUID
        {
            get => _hullBlueprintUUID;
            set => _hullBlueprintUUID = value;
        }

        public List<ShipComponentSlot> Components => _components;

        public void LoadFrom(ReadOnlyShipTemplate ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _hullBlueprintUUID = ro.HullBlueprintUUID ?? string.Empty;
            _components = new List<ShipComponentSlot>();
            foreach (var c in ro.Components)
            {
                _components.Add(new ShipComponentSlot
                {
                    SlotType = c.SlotType ?? string.Empty,
                    SlotIndex = c.SlotIndex,
                    BlueprintUUID = c.BlueprintUUID ?? string.Empty,
                    CurrentHP = c.CurrentHP,
                    MaxHP = c.MaxHP,
                    MaxRepairPercent = c.MaxRepairPercent,
                });
            }
        }

        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _hullBlueprintUUID = string.Empty;
            _components = new List<ShipComponentSlot>();
        }

        public ShipTemplateUpdateRequest BuildUpdateRequest()
        {
            return new ShipTemplateUpdateRequest
            {
                Original = _original,
                Name = _name,
                HullBlueprintUUID = _hullBlueprintUUID,
                Components = DeepCopyComponents(_components),
            };
        }

        public ShipTemplateCreateRequest BuildCreateRequest()
        {
            return new ShipTemplateCreateRequest
            {
                Name = _name,
                HullBlueprintUUID = _hullBlueprintUUID,
                Components = DeepCopyComponents(_components),
            };
        }

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

        public void RemoveComponent(string slotType, int slotIndex)
        {
            var existing = _components
                .FirstOrDefault(c => c.SlotType == slotType && c.SlotIndex == slotIndex);

            if (existing != null)
            {
                _components.Remove(existing);
            }
        }

        public void ClearComponents()
        {
            _components.Clear();
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
    }
}