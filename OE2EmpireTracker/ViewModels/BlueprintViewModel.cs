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
    /// Wraps a Blueprint data object and exposes typed properties,
    /// hiding all direct data access from the UI layer.
    /// </summary>
    public class BlueprintViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        private Blueprint _blueprint;

        public BlueprintViewModel(Blueprint blueprint, PlayerContext playerContext)
        {
            _blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Returns true if this blueprint is in the global list (BaselineData.json).
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                var ec = EmpireContext.GetInstance();
                return ec?.GlobalBlueprintList?.Contains(_blueprint) == true;
            }
        }

        public Blueprint Data => _blueprint;

        // -----------------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------------

        public string Name { get => _blueprint.Name; set => _blueprint.Name = value; }

        public string NickName { get => _blueprint.NickName; set => _blueprint.NickName = value; }

        public string Description { get => _blueprint.Description; set => _blueprint.Description = value; }

        public string BluePrintType
        {
            get => _blueprint.BluePrintType;
            set
            {
                string old = _blueprint.BluePrintType;
                _blueprint.BluePrintType = value;
                if (old != value)
                {
                    Log.Info(
                        "BlueprintViewModel.BluePrintType changed: '{0}' -> '{1}' for '{2}' UUID={3}",
                        old ?? "(null)", value ?? "(null)",
                        _blueprint.Name ?? "(null)", _blueprint.UUID ?? "(null)");
                }
            }
        }

        public int Class { get => _blueprint.Class; set => _blueprint.Class = value; }

        public string TechLevel { get => _blueprint.TechLevel; set => _blueprint.TechLevel = value; }

        public int Evolution { get => _blueprint.Evolution; set => _blueprint.Evolution = value; }

        public int CopyCost { get => _blueprint.CopyCost; set => _blueprint.CopyCost = value; }

        public string BaseBlueprintUUID { get => _blueprint.BaseBlueprintUUID; set => _blueprint.BaseBlueprintUUID = value; }

        public string UUID => _blueprint.UUID;

        // -----------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------

        public void ClearProperties() => _blueprint.Properties.Clear();

        public void SetProperty(string key, string value) => _blueprint.Properties.SetProperty(key, value);

        public bool GetProperty(string key, string defaultValue, out string value) =>
            _blueprint.Properties.GetString(key, defaultValue, out value);

        // -----------------------------------------------------------------------
        // Resources
        // -----------------------------------------------------------------------

        public void ClearResources() => _blueprint.Resources.Clear();

        public void SetResource(string name, string amount) => _blueprint.Resources[name] = amount;

        public IEnumerable<KeyValuePair<string, string>> GetResources() => _blueprint.Resources;

        // -----------------------------------------------------------------------
        // Base blueprint candidates
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns blueprints that could be the evolution predecessor of the current blueprint.
        /// Matches on BluePrintType, Name, Class, TechLevel, and only includes lower evolutions.
        /// Results are ordered by Evolution descending so the best match (N-1) is first.
        /// The current blueprint is excluded.
        /// </summary>
        public IReadOnlyList<Blueprint> GetBaseBlueprintCandidates(string nameFilter = null)
        {
            var current = _blueprint;
            var all = new List<Blueprint>(_playerContext.GetAllBlueprints());

            // Filter by matching BluePrintType
            if (!string.IsNullOrEmpty(current.BluePrintType))
            {
                all = all.Where(b => b.BluePrintType == current.BluePrintType).ToList();
            }

            // Filter by matching Name (exact, case-insensitive)
            if (!string.IsNullOrEmpty(current.Name))
            {
                all = all.Where(b => string.Equals(b.Name, current.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by matching Class
            if (current.Class > 0)
            {
                all = all.Where(b => b.Class == current.Class).ToList();
            }

            // Filter by matching TechLevel
            if (!string.IsNullOrEmpty(current.TechLevel))
            {
                all = all.Where(b => string.Equals(b.TechLevel, current.TechLevel, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Only show lower evolutions (0 to current-1)
            all = all.Where(b => b.Evolution < current.Evolution).ToList();

            // Exclude current blueprint
            if (current.UUID != null)
            {
                all = all.Where(b => b.UUID != current.UUID).ToList();
            }

            // Apply text filter on ExtendedName
            if (!string.IsNullOrEmpty(nameFilter))
            {
                all = all.Where(b => b.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Order by evolution descending (best match first)
            return CollectionSortHelper.OrderBlueprintsByEvolutionDescending(all);
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
        // Persistence
        // -----------------------------------------------------------------------

        public void Save()
        {
            Save(false);
        }

        /// <summary>
        /// Saves the blueprint. If isGlobal is true, moves it to the global list
        /// in BaselineData.json. If false, moves it to the current player's list.
        /// </summary>
        public void Save(bool isGlobal)
        {
            if (string.IsNullOrEmpty(_blueprint.UUID))
            {
                _blueprint.UUID = isGlobal
                    ? DeterministicUUID.Generate(_blueprint)
                    : Guid.NewGuid().ToString();
            }

            var ec = EmpireContext.GetInstance();
            bool wasGlobal = ec.GlobalBlueprintList.Contains(_blueprint);
            bool wasPlayer = _playerContext.BlueprintList.Contains(_blueprint);

            if (isGlobal)
            {
                _blueprint.OwnerUUID = string.Empty;
                if (wasPlayer) _playerContext.RemoveBlueprint(_blueprint);
                if (!wasGlobal) ec.AddGlobalBlueprint(_blueprint);
                ec.WriteContext();
                if (wasPlayer) _playerContext.WriteContext();
            }
            else
            {
                if (string.IsNullOrEmpty(_blueprint.OwnerUUID))
                {
                    _blueprint.OwnerUUID = _playerContext.CurrentPlayerUUID;
                }

                if (wasGlobal) ec.RemoveGlobalBlueprint(_blueprint);
                if (!wasPlayer) _playerContext.AddBlueprint(_blueprint);
                _playerContext.WriteContext();
                if (wasGlobal) ec.WriteContext();
            }

            _playerContext.OnBlueprintDataChanged(_blueprint.UUID);
        }

        public void Delete()
        {
            if (_blueprint.UUID == null) return;
            string deletedUUID = _blueprint.UUID;
            if (_playerContext.BlueprintList.Contains(_blueprint))
            {
                _playerContext.RemoveBlueprint(_blueprint);
                _playerContext.WriteContext();
            }
            else
            {
                var ec = EmpireContext.GetInstance();
                if (ec.GlobalBlueprintList.Contains(_blueprint))
                {
                    ec.RemoveGlobalBlueprint(_blueprint);
                    ec.WriteContext();
                }
            }

            _playerContext.OnBlueprintDataChanged(deletedUUID);
        }

        /// <summary>
        /// Resets the ViewModel to point at a new blank blueprint.
        /// </summary>
        public void Reset()
        {
            _blueprint = new Blueprint();
        }

        /// <summary>
        /// Switches the ViewModel to point at a different blueprint.
        /// </summary>
        public void SelectBlueprint(Blueprint blueprint)
        {
            _blueprint = blueprint ?? new Blueprint();
        }
    }
}
