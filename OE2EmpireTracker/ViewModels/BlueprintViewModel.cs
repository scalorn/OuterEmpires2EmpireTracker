using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for blueprint data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only BlueprintService mutates the actual entity.
    /// </summary>
    public class BlueprintViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        // Snapshot loaded from — kept for dirty comparison
        private ReadOnlyBlueprint _original;

        // Local edit state — disconnected from entity
        private string _uuid;
        private string _name;
        private string _nickName;
        private string _description;
        private string _bluePrintType;
        private int _evolution;
        private string _techLevel;
        private int _class;
        private int _copyCost;
        private string _baseBlueprintUUID;
        private string _ownerUUID;
        private bool _isGlobal;
        private Dictionary<string, string> _properties;
        private Dictionary<string, string> _resources;

        public BlueprintViewModel(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _properties = new Dictionary<string, string>();
            _resources = new Dictionary<string, string>();
        }

        /// <summary>
        /// Backwards-compatible constructor. The Blueprint parameter is ignored —
        /// call LoadFrom() to populate from a ReadOnlyBlueprint.
        /// </summary>
        public BlueprintViewModel(Blueprint blueprint, PlayerContext playerContext)
            : this(playerContext)
        {
            // blueprint parameter kept for compile compatibility during migration.
            // No fields are copied — LoadFrom or Reset should be called.
        }

        // -----------------------------------------------------------------------
        // Read-only state
        // -----------------------------------------------------------------------

        /// <summary>True if this is a new blueprint not yet saved.</summary>
        public bool IsNew => _original == null;

        public string UUID => _uuid;

        public ReadOnlyBlueprint Original => _original;

        /// <summary>
        /// Returns true if this blueprint is in the global list.
        /// Uses the local _isGlobal field set by LoadFrom.
        /// </summary>
        public bool IsGlobal
        {
            get => _isGlobal;
            set => _isGlobal = value;
        }

        // -----------------------------------------------------------------------
        // Identity — local edit state
        // -----------------------------------------------------------------------

        public string Name { get => _name; set => _name = value; }

        public string NickName { get => _nickName; set => _nickName = value; }

        public string Description { get => _description; set => _description = value; }

        public string BluePrintType
        {
            get => _bluePrintType;
            set
            {
                string old = _bluePrintType;
                _bluePrintType = value;
                if (old != value)
                {
                    Log.Info(
                        "BlueprintViewModel.BluePrintType changed: '{0}' -> '{1}' for '{2}' UUID={3}",
                        old ?? "(null)", value ?? "(null)",
                        _name ?? "(null)", _uuid ?? "(null)");
                }
            }
        }

        public int Class { get => _class; set => _class = value; }

        public string TechLevel { get => _techLevel; set => _techLevel = value; }

        public int Evolution { get => _evolution; set => _evolution = value; }

        public int CopyCost { get => _copyCost; set => _copyCost = value; }

        public string BaseBlueprintUUID { get => _baseBlueprintUUID; set => _baseBlueprintUUID = value; }

        public string OwnerUUID { get => _ownerUUID; set => _ownerUUID = value; }

        // -----------------------------------------------------------------------
        // Properties — local dictionary (properties)
        // -----------------------------------------------------------------------

        public int PropertyCount => _properties.Count;

        public IEnumerable<string> PropertyKeys => _properties.Keys;

        public IReadOnlyDictionary<string, string> Properties => _properties;

        // -----------------------------------------------------------------------
        // Resources — local dictionary (properties)
        // -----------------------------------------------------------------------

        public int ResourceCount => _resources.Count;

        public IReadOnlyDictionary<string, string> Resources => _resources;

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        /// <summary>
        /// True if any local field differs from the original snapshot.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null) return _uuid != null; // new blueprint
                return _name != _original.Name
                    || _nickName != _original.NickName
                    || _description != _original.Description
                    || _bluePrintType != _original.BluePrintType
                    || _evolution != _original.Evolution
                    || _techLevel != _original.TechLevel
                    || _class != _original.Class
                    || _copyCost != _original.CopyCost
                    || _baseBlueprintUUID != _original.BaseBlueprintUUID
                    || !PropertiesEqual(_properties, _original.Properties)
                    || !ResourcesEqual(_resources, _original.Resources);
            }
        }

        // -----------------------------------------------------------------------
        // Properties — local dictionary (methods)
        // -----------------------------------------------------------------------

        public void ClearProperties() => _properties.Clear();

        public void SetProperty(string key, string value) => _properties[key] = value;

        public bool GetProperty(string key, string defaultValue, out string value)
        {
            if (_properties.TryGetValue(key, out value))
                return true;
            value = defaultValue;
            return false;
        }

        public void RemoveProperty(string key) => _properties.Remove(key);

        public bool PropertyContainsKey(string key) => _properties.ContainsKey(key);

        // -----------------------------------------------------------------------
        // Resources — local dictionary (methods)
        // -----------------------------------------------------------------------

        public void ClearResources() => _resources.Clear();

        public void SetResource(string name, string amount) => _resources[name] = amount;

        public void RemoveResource(string name) => _resources.Remove(name);

        public IEnumerable<KeyValuePair<string, string>> GetResources() => _resources;

        // -----------------------------------------------------------------------
        // LoadFrom / Reset
        // -----------------------------------------------------------------------

        /// <summary>
        /// Loads field values from a ReadOnlyBlueprint snapshot.
        /// Retains the original for dirty comparison.
        /// </summary>
        public void LoadFrom(ReadOnlyBlueprint ro)
        {
            if (ro == null) throw new ArgumentNullException(nameof(ro));

            _original = ro;
            _uuid = ro.UUID;
            _name = ro.Name;
            _nickName = ro.NickName;
            _description = ro.Description;
            _bluePrintType = ro.BluePrintType;
            _evolution = ro.Evolution;
            _techLevel = ro.TechLevel;
            _class = ro.Class;
            _copyCost = ro.CopyCost;
            _baseBlueprintUUID = ro.BaseBlueprintUUID;
            _ownerUUID = ro.OwnerUUID;

            // Determine global status
            var ec = EmpireContext.GetInstance();
            _isGlobal = ec?.FindReadOnlyGlobalBlueprint(ro.UUID) != null;

            // Copy properties
            _properties = new Dictionary<string, string>();
            foreach (var key in ro.Properties.Keys)
            {
                ro.Properties.GetString(key, string.Empty, out string val);
                _properties[key] = val;
            }

            // Copy resources
            _resources = new Dictionary<string, string>();
            foreach (var kvp in ro.Resources)
                _resources[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// Resets to empty state for a new blueprint.
        /// </summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _name = string.Empty;
            _nickName = string.Empty;
            _description = string.Empty;
            _bluePrintType = null;
            _evolution = 0;
            _techLevel = null;
            _class = 0;
            _copyCost = 0;
            _baseBlueprintUUID = null;
            _ownerUUID = string.Empty;
            _isGlobal = false;
            _properties = new Dictionary<string, string>();
            _resources = new Dictionary<string, string>();
        }

        // -----------------------------------------------------------------------
        // Build request DTOs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds an update request carrying both the original snapshot
        /// and the current local state.
        /// </summary>
        public BlueprintUpdateRequest BuildUpdateRequest()
        {
            return new BlueprintUpdateRequest
            {
                Original = _original,
                Name = _name,
                NickName = _nickName,
                Description = _description,
                BluePrintType = _bluePrintType,
                Evolution = _evolution,
                TechLevel = _techLevel,
                Class = _class,
                CopyCost = _copyCost,
                BaseBlueprintUUID = _baseBlueprintUUID,
                Properties = new Dictionary<string, string>(_properties),
                Resources = new Dictionary<string, string>(_resources),
            };
        }

        /// <summary>
        /// Builds a create request for a new blueprint.
        /// </summary>
        public BlueprintCreateRequest BuildCreateRequest()
        {
            return new BlueprintCreateRequest
            {
                Name = _name,
                NickName = _nickName,
                Description = _description,
                BluePrintType = _bluePrintType,
                Evolution = _evolution,
                TechLevel = _techLevel,
                Class = _class,
                CopyCost = _copyCost,
                BaseBlueprintUUID = _baseBlueprintUUID,
                Properties = new Dictionary<string, string>(_properties),
                Resources = new Dictionary<string, string>(_resources),
            };
        }

        // -----------------------------------------------------------------------
        // Base blueprint candidates
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns blueprints that could be the evolution predecessor of the current blueprint.
        /// Matches on BluePrintType, Name, Class, TechLevel, and only includes lower evolutions.
        /// Results are ordered by Evolution descending so the best match (N-1) is first.
        /// The current blueprint is excluded.
        /// </summary>
        public IReadOnlyList<ReadOnlyBlueprint> GetBaseBlueprintCandidates(string nameFilter = null)
        {
            var all = new List<Blueprint>(_playerContext.GetAllBlueprints());

            // Filter by matching BluePrintType
            if (!string.IsNullOrEmpty(_bluePrintType))
            {
                all = all.Where(b => b.BluePrintType == _bluePrintType).ToList();
            }

            // Filter by matching Name (exact, case-insensitive)
            if (!string.IsNullOrEmpty(_name))
            {
                all = all.Where(b => string.Equals(b.Name, _name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by matching Class
            if (_class > 0)
            {
                all = all.Where(b => b.Class == _class).ToList();
            }

            // Filter by matching TechLevel
            if (!string.IsNullOrEmpty(_techLevel))
            {
                all = all.Where(b => string.Equals(b.TechLevel, _techLevel, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Only show lower evolutions (0 to current-1)
            all = all.Where(b => b.Evolution < _evolution).ToList();

            // Exclude current blueprint
            if (_uuid != null)
            {
                all = all.Where(b => b.UUID != _uuid).ToList();
            }

            // Apply text filter on ExtendedName
            if (!string.IsNullOrEmpty(nameFilter))
            {
                all = all.Where(b => b.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Wrap as ReadOnlyBlueprint and order by evolution descending (best match first)
            var readOnly = all.Select(b => new ReadOnlyBlueprint(b));
            return CollectionSortHelper.OrderReadOnlyBlueprintsByEvolutionDescending(readOnly);
        }

        // -----------------------------------------------------------------------
        // List filtering
        // -----------------------------------------------------------------------

        public IReadOnlyList<ReadOnlyBlueprint> GetFilteredBlueprints(string nameFilter)
        {
            return GetFilteredBlueprints(nameFilter, null);
        }

        public IReadOnlyList<ReadOnlyBlueprint> GetFilteredBlueprints(string nameFilter, BlueprintFilterCriteria criteria)
        {
            // Merge global + current player blueprints
            var list = new List<Blueprint>(_playerContext.GetCurrentPlayerBlueprints());
            var ec = EmpireContext.GetInstance();
            if (ec?.GlobalBlueprintList != null)
            {
                list.AddRange(ec.GlobalBlueprintList);
            }

            // Text filter
            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(b => b.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0
                             || (!string.IsNullOrEmpty(b.BluePrintType) && b.BluePrintType.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList();
            }

            // Structured filter criteria
            if (criteria != null)
            {
                if (criteria.BlueprintTypeId != null)
                {
                    list = list.Where(b => b.BluePrintType == criteria.BlueprintTypeId).ToList();
                }

                if (criteria.ShipClassId.HasValue)
                {
                    list = list.Where(b => b.Class == criteria.ShipClassId.Value).ToList();
                }

                if (criteria.TechLevelName != null)
                {
                    list = list.Where(b => b.TechLevel == criteria.TechLevelName).ToList();
                }

                if (criteria.Evolution.HasValue)
                {
                    if (criteria.EvolutionAndAbove)
                        list = list.Where(b => b.Evolution >= criteria.Evolution.Value).ToList();
                    else
                        list = list.Where(b => b.Evolution == criteria.Evolution.Value).ToList();
                }
            }

            var sorted = CollectionSortHelper.OrderBlueprints(list);
            return sorted.Select(b => new ReadOnlyBlueprint(b)).ToList();
        }

        // -----------------------------------------------------------------------
        // Persistence (stubs — will be replaced by BlueprintService in Task 9)
        // -----------------------------------------------------------------------

        public void Save()
        {
            Save(false);
        }

        /// <summary>
        /// Temporary stub — saves by looking up the mutable entity and applying local state.
        /// Will be replaced by BlueprintService.Update/Create in Task 9/10.
        /// </summary>
        public void Save(bool isGlobal)
        {
            var ec = EmpireContext.GetInstance();
            Blueprint bp;

            if (_original == null)
            {
                // New blueprint
                bp = new Blueprint();
                bp.UUID = isGlobal
                    ? DeterministicUUID.Generate(bp)
                    : Guid.NewGuid().ToString();
                _uuid = bp.UUID;
            }
            else
            {
                // Existing blueprint — look up mutable entity
                bp = _playerContext.FindBlueprint(_uuid)
                  ?? ec?.FindGlobalBlueprint(_uuid);
                if (bp == null)
                {
                    Log.Error("Save: could not find mutable blueprint for UUID={0}", _uuid);
                    return;
                }
            }

            // Apply local state to entity
            bp.Name = _name;
            bp.NickName = _nickName;
            bp.Description = _description;
            bp.BluePrintType = _bluePrintType;
            bp.Evolution = _evolution;
            bp.TechLevel = _techLevel;
            bp.Class = _class;
            bp.CopyCost = _copyCost;
            bp.BaseBlueprintUUID = _baseBlueprintUUID;

            // Apply properties
            bp.Properties = new PropertyBag();
            foreach (var kvp in _properties)
                bp.Properties.SetProperty(kvp.Key, kvp.Value);

            // Apply resources
            bp.Resources = new Dictionary<string, string>(_resources);

            // Handle list membership
            bool wasGlobal = ec.GlobalBlueprintList.Contains(bp);
            bool wasPlayer = _playerContext.BlueprintList.Contains(bp);

            if (isGlobal)
            {
                bp.OwnerUUID = string.Empty;
                if (wasPlayer) _playerContext.RemoveBlueprint(bp);
                if (!wasGlobal) ec.AddGlobalBlueprint(bp);
                ec.WriteContext();
                if (wasPlayer) _playerContext.WriteContext();
            }
            else
            {
                if (string.IsNullOrEmpty(bp.OwnerUUID))
                {
                    bp.OwnerUUID = _playerContext.CurrentPlayerUUID;
                }

                if (wasGlobal) ec.RemoveGlobalBlueprint(bp);
                if (!wasPlayer) _playerContext.AddBlueprint(bp);
                _playerContext.WriteContext();
                if (wasGlobal) ec.WriteContext();
            }

            _playerContext.OnBlueprintDataChanged(bp.UUID);

            // Reload from fresh snapshot so IsDirty resets
            _original = new ReadOnlyBlueprint(bp);
            _isGlobal = isGlobal;
        }

        public void Delete()
        {
            if (_uuid == null) return;
            string deletedUUID = _uuid;

            var bp = _playerContext.FindBlueprint(_uuid);
            if (bp != null)
            {
                _playerContext.RemoveBlueprint(bp);
                _playerContext.WriteContext();
            }
            else
            {
                var ec = EmpireContext.GetInstance();
                var globalBp = ec?.FindGlobalBlueprint(_uuid);
                if (globalBp != null)
                {
                    ec.RemoveGlobalBlueprint(globalBp);
                    ec.WriteContext();
                }
            }

            _playerContext.OnBlueprintDataChanged(deletedUUID);
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        private static bool PropertiesEqual(Dictionary<string, string> local, ReadOnlyPropertyBag original)
        {
            if (original == null) return local.Count == 0;
            if (local.Count != original.Count) return false;

            foreach (var kvp in local)
            {
                if (!original.GetString(kvp.Key, null, out string origVal))
                    return false;
                if (kvp.Value != origVal)
                    return false;
            }

            return true;
        }

        private static bool ResourcesEqual(Dictionary<string, string> local, IReadOnlyDictionary<string, string> original)
        {
            if (original == null) return local.Count == 0;
            if (local.Count != original.Count) return false;

            foreach (var kvp in local)
            {
                if (!original.TryGetValue(kvp.Key, out string origVal))
                    return false;
                if (kvp.Value != origVal)
                    return false;
            }

            return true;
        }
    }
}
