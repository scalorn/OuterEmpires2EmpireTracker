using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
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
        public string BaseBlueprintUUID { get => _blueprint.baseBlueprintUUID; set => _blueprint.baseBlueprintUUID = value; }
        public string UUID => _blueprint.UUID;

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
        // List filtering
        // -----------------------------------------------------------------------

        public IReadOnlyList<Blueprint> GetFilteredBlueprints(string nameFilter)
        {
            var list = _playerContext.GetCurrentPlayerBlueprints();
            if (!string.IsNullOrEmpty(nameFilter))
            {
                list = list
                    .Where(b => b.ExtendedName.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            return list.AsReadOnly();
        }

        // -----------------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------------

        public void Save()
        {
            if (string.IsNullOrEmpty(_blueprint.UUID))
            {
                _blueprint.UUID = Guid.NewGuid().ToString();
                _playerContext.blueprintList.Add(_blueprint);
            }
            if (string.IsNullOrEmpty(_blueprint.OwnerUUID))
            {
                _blueprint.OwnerUUID = _playerContext.CurrentPlayerUUID;
            }
            _playerContext.writeContext();
        }

        public void Delete()
        {
            if (_blueprint.UUID == null) return;
            _playerContext.blueprintList.Remove(_blueprint);
            _playerContext.writeContext();
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
