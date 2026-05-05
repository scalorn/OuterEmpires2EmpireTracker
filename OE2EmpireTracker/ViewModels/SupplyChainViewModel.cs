using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    public class SupplyChainViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ReadOnlySupplyChain _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _name = string.Empty;
        private bool _isActive = true;
        private List<SupplyChainStage> _stages = new List<SupplyChainStage>();

        public string UUID => _uuid;

        public string OwnerUUID => _ownerUUID;

        public ReadOnlySupplyChain Original => _original;

        public bool IsNew => _original == null;

        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    return !string.IsNullOrEmpty(_name) || !_isActive || _stages.Count > 0;
                }

                if (_name != (_original.Name ?? string.Empty))
                {
                    return true;
                }

                if (_isActive != _original.IsActive)
                {
                    return true;
                }

                var originalStages = _original.Stages;
                if (_stages.Count != originalStages.Count)
                {
                    return true;
                }

                for (int i = 0; i < _stages.Count; i++)
                {
                    var local = _stages[i];
                    var orig = originalStages[i];
                    if (local.Sequence != orig.Sequence) return true;
                    if (local.StageType != orig.StageType) return true;
                    if (local.LocationType != orig.LocationType) return true;
                    if (local.LocationUUID != orig.LocationUUID) return true;
                    if (local.ResourceName != orig.ResourceName) return true;
                    if (local.ResourcePurity != orig.ResourcePurity) return true;
                    if (local.AccumulationThreshold != orig.AccumulationThreshold) return true;
                    if (local.ProductionRatePerHour != orig.ProductionRatePerHour) return true;
                    if (local.DeliveryRouteUUID != orig.DeliveryRouteUUID) return true;
                }

                return false;
            }
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public List<SupplyChainStage> Stages => _stages;

        public void LoadFrom(ReadOnlySupplyChain ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _name = ro.Name ?? string.Empty;
            _isActive = ro.IsActive;
            _stages = new List<SupplyChainStage>();
            foreach (var stage in ro.Stages)
            {
                _stages.Add(DeepCopyStage(stage));
            }
        }

        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _name = string.Empty;
            _isActive = true;
            _stages = new List<SupplyChainStage>();
        }

        public SupplyChainUpdateRequest BuildUpdateRequest()
        {
            return new SupplyChainUpdateRequest
            {
                Original = _original,
                Name = _name,
                IsActive = _isActive,
                Stages = DeepCopyStages(_stages),
            };
        }

        public SupplyChainCreateRequest BuildCreateRequest()
        {
            return new SupplyChainCreateRequest
            {
                Name = _name,
                IsActive = _isActive,
                Stages = DeepCopyStages(_stages),
            };
        }

        public void AddStage(SupplyChainStage stage)
        {
            _stages.Add(stage);
        }

        public void RemoveStage(int index)
        {
            if (index >= 0 && index < _stages.Count)
            {
                _stages.RemoveAt(index);
            }
        }

        public void UpdateStage(int index, SupplyChainStage stage)
        {
            if (index >= 0 && index < _stages.Count)
            {
                _stages[index] = stage;
            }
        }

        public void MoveStageUp(int index)
        {
            if (index <= 0 || index >= _stages.Count) return;
            var temp = _stages[index];
            _stages[index] = _stages[index - 1];
            _stages[index - 1] = temp;
        }

        public void MoveStageDown(int index)
        {
            if (index < 0 || index >= _stages.Count - 1) return;
            var temp = _stages[index];
            _stages[index] = _stages[index + 1];
            _stages[index + 1] = temp;
        }

        public void RenumberStages()
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                _stages[i].Sequence = i + 1;
            }
        }

        private static SupplyChainStage DeepCopyStage(ReadOnlySupplyChainStage source)
        {
            return new SupplyChainStage
            {
                Sequence = source.Sequence,
                StageType = source.StageType,
                LocationType = source.LocationType,
                LocationUUID = source.LocationUUID,
                ResourceName = source.ResourceName,
                ResourcePurity = source.ResourcePurity,
                AccumulationThreshold = source.AccumulationThreshold,
                ProductionRatePerHour = source.ProductionRatePerHour,
                DeliveryRouteUUID = source.DeliveryRouteUUID,
            };
        }

        private static List<SupplyChainStage> DeepCopyStages(List<SupplyChainStage> source)
        {
            var copy = new List<SupplyChainStage>(source.Count);
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

            return copy;
        }
    }
}