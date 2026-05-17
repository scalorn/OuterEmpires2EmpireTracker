using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class BuildPlanViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlyBuildPlan _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = true;
        private List<BuildItem> _items = new List<BuildItem>();

        public string UUID => _uuid;

        public string OwnerUUID => _ownerUUID;

        public ReadOnlyBuildPlan Original => _original;

        public bool IsNew => _original == null;

        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name) || !string.IsNullOrEmpty(_description)
                        || !_isActive || _items.Count > 0;
                }

                if (_name != (_original.Name ?? string.Empty))
                {
                    return true;
                }

                if (_description != (_original.Description ?? string.Empty))
                {
                    return true;
                }

                if (_isActive != _original.IsActive)
                {
                    return true;
                }

                var originalItems = _original.Items;
                if (_items.Count != originalItems.Count)
                {
                    return true;
                }

                for (int i = 0; i < _items.Count; i++)
                {
                    var local = _items[i];
                    var orig = originalItems[i];
                    if (local.UUID != orig.UUID) return true;
                    if (local.ItemType != orig.ItemType) return true;
                    if (local.Status != orig.Status) return true;
                    if (local.BlueprintUUID != orig.BlueprintUUID) return true;
                    if (local.ItemName != orig.ItemName) return true;
                    if (local.CommodityName != orig.CommodityName) return true;
                    if (local.ShipTemplateUUID != orig.ShipTemplateUUID) return true;
                    if (local.Quantity != orig.Quantity) return true;
                    if (local.BuildLocationType != orig.BuildLocationType) return true;
                    if (local.BuildLocationUUID != orig.BuildLocationUUID) return true;
                    if (local.StructureUUID != orig.StructureUUID) return true;
                    if (local.AssemblyLocationType != orig.AssemblyLocationType) return true;
                    if (local.AssemblyLocationUUID != orig.AssemblyLocationUUID) return true;
                    if (local.ParentBuildItemUUID != orig.ParentBuildItemUUID) return true;
                    if (local.Recipient != orig.Recipient) return true;
                    if (local.Notes != orig.Notes) return true;
                    if (local.SequenceInStructure != orig.SequenceInStructure) return true;
                    if (local.DependsOnUUID != orig.DependsOnUUID) return true;
                    if (local.MiningResource != orig.MiningResource) return true;
                    if (local.MiningSurveyUUID != orig.MiningSurveyUUID) return true;
                    if (local.RefiningResource != orig.RefiningResource) return true;
                    if (local.RefiningPurity != orig.RefiningPurity) return true;
                }

                return false;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public List<BuildItem> Items => _items;
        public void LoadFrom(ReadOnlyBuildPlan ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _description = ro.Description ?? string.Empty;
            _isActive = ro.IsActive;
            _items = new List<BuildItem>();
            foreach (var item in ro.Items)
            {
                _items.Add(DeepCopyItem(item));
            }
        }

        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _description = string.Empty;
            _isActive = true;
            _items = new List<BuildItem>();
        }

        public BuildPlanUpdateRequest BuildUpdateRequest()
        {
            return new BuildPlanUpdateRequest
            {
                Original = _original,
                Name = _name,
                Description = _description,
                IsActive = _isActive,
                Items = DeepCopyItems(_items),
            };
        }

        public BuildPlanCreateRequest BuildCreateRequest()
        {
            return new BuildPlanCreateRequest
            {
                Name = _name,
                Description = _description,
                IsActive = _isActive,
                Items = DeepCopyItems(_items),
            };
        }

        public void AddItem(BuildItem item)
        {
            _items.Add(item);
        }

        public void RemoveItem(string uuid)
        {
            var item = _items.FirstOrDefault(i => i.UUID == uuid);
            if (item != null)
            {
                _items.Remove(item);
            }
        }

        public BuildItem FindItem(string uuid)
        {
            return _items.FirstOrDefault(i => i.UUID == uuid);
        }

        private static BuildItem DeepCopyItem(ReadOnlyBuildItem source)
        {
            return new BuildItem
            {
                UUID = source.UUID,
                ItemType = source.ItemType,
                Status = source.Status,
                BlueprintUUID = source.BlueprintUUID,
                ItemName = source.ItemName,
                CommodityName = source.CommodityName,
                ShipTemplateUUID = source.ShipTemplateUUID,
                Quantity = source.Quantity,
                BuildLocationType = source.BuildLocationType,
                BuildLocationUUID = source.BuildLocationUUID,
                StructureUUID = source.StructureUUID,
                AssemblyLocationType = source.AssemblyLocationType,
                AssemblyLocationUUID = source.AssemblyLocationUUID,
                ParentBuildItemUUID = source.ParentBuildItemUUID,
                Recipient = source.Recipient,
                Notes = source.Notes,
                SequenceInStructure = source.SequenceInStructure,
                DependsOnUUID = source.DependsOnUUID,
                MiningResource = source.MiningResource,
                MiningSurveyUUID = source.MiningSurveyUUID,
                RefiningResource = source.RefiningResource,
                RefiningPurity = source.RefiningPurity,
            };
        }

        private static List<BuildItem> DeepCopyItems(List<BuildItem> source)
        {
            var copy = new List<BuildItem>(source.Count);
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

            return copy;
        }
    }
}