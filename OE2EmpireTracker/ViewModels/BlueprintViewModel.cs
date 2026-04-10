using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a Blueprint data object and exposes typed properties,
    /// hiding all direct data access from the UI layer.
    /// </summary>
    public class BlueprintViewModel
    {
        private readonly PlayerContext _playerContext;
        private Blueprint _blueprint;

        public Blueprint Data => _blueprint;

        public BlueprintViewModel(Blueprint blueprint, PlayerContext playerContext)
        {
            _blueprint = blueprint ?? throw new ArgumentNullException(nameof(blueprint));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        // -----------------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------------

        public string Name { get => _blueprint.Name; set => _blueprint.Name = value; }
        public string NickName { get => _blueprint.NickName; set => _blueprint.NickName = value; }
        public string Description { get => _blueprint.Description; set => _blueprint.Description = value; }
        public string BluePrintType { get => _blueprint.BluePrintType; set => _blueprint.BluePrintType = value; }
        public int Class { get => _blueprint.Class; set => _blueprint.Class = value; }
        public string TechLevel { get => _blueprint.TechLevel; set => _blueprint.TechLevel = value; }
        public int Evolution { get => _blueprint.Evolution; set => _blueprint.Evolution = value; }
        public int CopyCost { get => _blueprint.CopyCost; set => _blueprint.CopyCost = value; }
        public string BaseBlueprintUUID { get => _blueprint.BaseBlueprintUUID; set => _blueprint.BaseBlueprintUUID = value; }
        public string UUID => _blueprint.UUID;

        /// <summary>
        /// Returns true if this blueprint is in the global list (BaselineData.json).
        /// </summary>
        public bool IsGlobal
        {
            get
            {
                var ec = EmpireContext.getInstance();
                return ec?.globalBlueprintList?.Contains(_blueprint) == true;
            }
        }

        // -----------------------------------------------------------------------
        // Properties
        // -----------------------------------------------------------------------

        public void ClearProperties() => _blueprint.Properties.Clear();

        public void SetProperty(string key, string value) => _blueprint.Properties.setProperty(key, value);

        public bool GetProperty(string key, string defaultValue, out string value) =>
            _blueprint.Properties.getString(key, defaultValue, out value);

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
            return all.OrderByDescending(b => b.Evolution).ToList().AsReadOnly();
        }

        // -----------------------------------------------------------------------
        // List filtering
        // -----------------------------------------------------------------------

        public IReadOnlyList<Blueprint> GetFilteredBlueprints(string nameFilter)
        {
            return GetFilteredBlueprints(nameFilter, null);
        }

        public IReadOnlyList<Blueprint> GetFilteredBlueprints(string nameFilter, BlueprintFilterCriteria criteria)
        {
            // Merge global + current player blueprints
            var list = new List<Blueprint>(_playerContext.GetCurrentPlayerBlueprints());
            var ec = EmpireContext.getInstance();
            if (ec?.globalBlueprintList != null)
            {
                list.AddRange(ec.globalBlueprintList);
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

            list.Sort((a, b) => string.Compare(a.ExtendedName, b.ExtendedName, StringComparison.OrdinalIgnoreCase));
            return list.AsReadOnly();
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
                _blueprint.UUID = Guid.NewGuid().ToString();
            }

            var ec = EmpireContext.getInstance();
            bool wasGlobal = ec.globalBlueprintList.Contains(_blueprint);
            bool wasPlayer = _playerContext.blueprintList.Contains(_blueprint);

            if (isGlobal)
            {
                _blueprint.OwnerUUID = string.Empty;
                if (wasPlayer) _playerContext.blueprintList.Remove(_blueprint);
                if (!wasGlobal) ec.globalBlueprintList.Add(_blueprint);
                ec.writeContext();
                if (wasPlayer) _playerContext.writeContext();
            }
            else
            {
                if (string.IsNullOrEmpty(_blueprint.OwnerUUID))
                {
                    _blueprint.OwnerUUID = _playerContext.CurrentPlayerUUID;
                }
                if (wasGlobal) ec.globalBlueprintList.Remove(_blueprint);
                if (!wasPlayer) _playerContext.blueprintList.Add(_blueprint);
                _playerContext.writeContext();
                if (wasGlobal) ec.writeContext();
            }

            _playerContext.OnBlueprintDataChanged(_blueprint.UUID);
        }

        public void Delete()
        {
            if (_blueprint.UUID == null) return;
            string deletedUUID = _blueprint.UUID;
            if (_playerContext.blueprintList.Remove(_blueprint))
            {
                _playerContext.writeContext();
            }
            else
            {
                var ec = EmpireContext.getInstance();
                if (ec.globalBlueprintList.Remove(_blueprint))
                {
                    ec.writeContext();
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
