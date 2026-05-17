using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Dual-entity ViewModel for FormStockTargets. Holds disconnected edit buffers
    /// for both StockPlan (Plans tab) and StockProfile (Profiles tab).
    /// The form reads from and writes to this ViewModel; only the service mutates entities.
    /// </summary>
    public class StockTargetViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // === Plan Section ===
        private ReadOnlyStockPlan _planOriginal;
        private string _planUUID;
        private string _planOwnerUUID = string.Empty;
        private string _planName = string.Empty;
        private bool _planIsActive = true;
        private string _replenishmentBuildPlanUUID = string.Empty;
        private List<StockTarget> _targets = new List<StockTarget>();

        // === Profile Section ===
        private ReadOnlyStockProfile _profileOriginal;
        private string _profileUUID;
        private string _profileOwnerUUID = string.Empty;
        private string _profileName = string.Empty;
        private bool _profileIsActive = true;
        private List<StockProfileEntry> _entries = new List<StockProfileEntry>();

        // === Plan Properties ===
        public string PlanUUID => _planUUID;

        public string PlanOwnerUUID => _planOwnerUUID;

        public ReadOnlyStockPlan PlanOriginal => _planOriginal;

        public bool IsPlanNew => _planOriginal == null;

        public string PlanName
        {
            get => _planName;
            set => _planName = value;
        }

        public bool PlanIsActive
        {
            get => _planIsActive;
            set => _planIsActive = value;
        }

        public string ReplenishmentBuildPlanUUID
        {
            get => _replenishmentBuildPlanUUID;
            set => _replenishmentBuildPlanUUID = value;
        }

        public List<StockTarget> Targets => _targets;

        public bool IsPlanDirty
        {
            get
            {
                if (_planOriginal == null)
                {
                    return !string.IsNullOrEmpty(_planName)
                        || !_planIsActive
                        || !string.IsNullOrEmpty(_replenishmentBuildPlanUUID)
                        || _targets.Count > 0;
                }

                if (_planName != (_planOriginal.Name ?? string.Empty))
                {
                    return true;
                }

                if (_planIsActive != _planOriginal.IsActive)
                {
                    return true;
                }

                if (_replenishmentBuildPlanUUID != (_planOriginal.ReplenishmentBuildPlanUUID ?? string.Empty))
                {
                    return true;
                }

                var originalTargets = _planOriginal.Targets;
                if (_targets.Count != originalTargets.Count)
                {
                    return true;
                }

                for (int i = 0; i < _targets.Count; i++)
                {
                    var local = _targets[i];
                    var orig = originalTargets[i];
                    if (local.UUID != orig.UUID) return true;
                    if (local.ItemType != orig.ItemType) return true;
                    if (local.ItemReferenceID != orig.ItemReferenceID) return true;
                    if (local.ItemName != orig.ItemName) return true;
                    if (local.ShipTemplateUUID != orig.ShipTemplateUUID) return true;
                    if (local.TargetQuantity != orig.TargetQuantity) return true;
                    if (local.CriticalThreshold != orig.CriticalThreshold) return true;
                    if (local.Scope != orig.Scope) return true;
                    if (local.LocationUUID != orig.LocationUUID) return true;
                }

                return false;
            }
        }

        // === Profile Properties ===
        public string ProfileUUID => _profileUUID;

        public string ProfileOwnerUUID => _profileOwnerUUID;

        public ReadOnlyStockProfile ProfileOriginal => _profileOriginal;

        public bool IsProfileNew => _profileOriginal == null;

        public string ProfileName
        {
            get => _profileName;
            set => _profileName = value;
        }

        public bool ProfileIsActive
        {
            get => _profileIsActive;
            set => _profileIsActive = value;
        }

        public List<StockProfileEntry> Entries => _entries;

        public bool IsProfileDirty
        {
            get
            {
                if (_profileOriginal == null)
                {
                    return !string.IsNullOrEmpty(_profileName)
                        || !_profileIsActive
                        || _entries.Count > 0;
                }

                if (_profileName != (_profileOriginal.Name ?? string.Empty))
                {
                    return true;
                }

                if (_profileIsActive != _profileOriginal.IsActive)
                {
                    return true;
                }

                var originalEntries = _profileOriginal.Entries;
                if (_entries.Count != originalEntries.Count)
                {
                    return true;
                }

                for (int i = 0; i < _entries.Count; i++)
                {
                    var local = _entries[i];
                    var orig = originalEntries[i];
                    if (local.GroupID != orig.GroupID) return true;
                    if (local.StockPlanUUID != orig.StockPlanUUID) return true;
                }

                return false;
            }
        }

        // === Plan Methods ===
        public void LoadPlanFrom(ReadOnlyStockPlan ro)
        {
            _planOriginal = ro;
            _planUUID = ro.UUID;
            _planOwnerUUID = ro.OwnerUUID ?? string.Empty;
            _planName = ro.Name ?? string.Empty;
            _planIsActive = ro.IsActive;
            _replenishmentBuildPlanUUID = ro.ReplenishmentBuildPlanUUID ?? string.Empty;
            _targets = new List<StockTarget>();
            foreach (var t in ro.Targets)
            {
                _targets.Add(DeepCopyTarget(t));
            }
        }

        public void ResetPlan()
        {
            _planOriginal = null;
            _planUUID = null;
            _planOwnerUUID = string.Empty;
            _planName = string.Empty;
            _planIsActive = true;
            _replenishmentBuildPlanUUID = string.Empty;
            _targets = new List<StockTarget>();
        }

        public StockPlanUpdateRequest BuildPlanUpdateRequest()
        {
            return new StockPlanUpdateRequest
            {
                Original = _planOriginal,
                Name = _planName,
                IsActive = _planIsActive,
                ReplenishmentBuildPlanUUID = _replenishmentBuildPlanUUID,
                Targets = DeepCopyTargets(_targets),
            };
        }

        public StockPlanCreateRequest BuildPlanCreateRequest()
        {
            return new StockPlanCreateRequest
            {
                Name = _planName,
                IsActive = _planIsActive,
                ReplenishmentBuildPlanUUID = _replenishmentBuildPlanUUID,
                Targets = DeepCopyTargets(_targets),
            };
        }

        public void AddTarget(StockTarget target)
        {
            _targets.Add(target);
        }

        public void RemoveTarget(string uuid)
        {
            var target = _targets.FirstOrDefault(t => t.UUID == uuid);
            if (target != null)
            {
                _targets.Remove(target);
            }
        }

        // === Profile Methods ===
        public void LoadProfileFrom(ReadOnlyStockProfile ro)
        {
            _profileOriginal = ro;
            _profileUUID = ro.UUID;
            _profileOwnerUUID = ro.OwnerUUID ?? string.Empty;
            _profileName = ro.Name ?? string.Empty;
            _profileIsActive = ro.IsActive;
            _entries = new List<StockProfileEntry>();
            foreach (var e in ro.Entries)
            {
                _entries.Add(new StockProfileEntry
                {
                    GroupID = e.GroupID ?? string.Empty,
                    StockPlanUUID = e.StockPlanUUID ?? string.Empty,
                });
            }
        }

        public void ResetProfile()
        {
            _profileOriginal = null;
            _profileUUID = null;
            _profileOwnerUUID = string.Empty;
            _profileName = string.Empty;
            _profileIsActive = true;
            _entries = new List<StockProfileEntry>();
        }

        public StockProfileUpdateRequest BuildProfileUpdateRequest()
        {
            return new StockProfileUpdateRequest
            {
                Original = _profileOriginal,
                Name = _profileName,
                IsActive = _profileIsActive,
                Entries = DeepCopyEntries(_entries),
            };
        }

        public StockProfileCreateRequest BuildProfileCreateRequest()
        {
            return new StockProfileCreateRequest
            {
                Name = _profileName,
                IsActive = _profileIsActive,
                Entries = DeepCopyEntries(_entries),
            };
        }

        public void AddEntry(StockProfileEntry entry)
        {
            _entries.Add(entry);
        }

        public void RemoveEntry(StockProfileEntry entry)
        {
            _entries.Remove(entry);
        }

        // === Private Helpers ===
        private static StockTarget DeepCopyTarget(ReadOnlyStockTarget source)
        {
            return new StockTarget
            {
                UUID = source.UUID,
                ItemType = source.ItemType,
                ItemReferenceID = source.ItemReferenceID,
                ItemName = source.ItemName,
                ShipTemplateUUID = source.ShipTemplateUUID,
                TargetQuantity = source.TargetQuantity,
                CriticalThreshold = source.CriticalThreshold,
                Scope = source.Scope,
                LocationUUID = source.LocationUUID,
            };
        }

        private static List<StockTarget> DeepCopyTargets(List<StockTarget> source)
        {
            var copy = new List<StockTarget>(source.Count);
            foreach (var s in source)
            {
                copy.Add(new StockTarget
                {
                    UUID = s.UUID,
                    ItemType = s.ItemType,
                    ItemReferenceID = s.ItemReferenceID,
                    ItemName = s.ItemName,
                    ShipTemplateUUID = s.ShipTemplateUUID,
                    TargetQuantity = s.TargetQuantity,
                    CriticalThreshold = s.CriticalThreshold,
                    Scope = s.Scope,
                    LocationUUID = s.LocationUUID,
                });
            }

            return copy;
        }

        private static List<StockProfileEntry> DeepCopyEntries(List<StockProfileEntry> source)
        {
            var copy = new List<StockProfileEntry>(source.Count);
            foreach (var s in source)
            {
                copy.Add(new StockProfileEntry
                {
                    GroupID = s.GroupID,
                    StockPlanUUID = s.StockPlanUUID,
                });
            }

            return copy;
        }
    }
}
