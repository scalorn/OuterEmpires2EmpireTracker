using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for colony data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only ColonyService mutates the actual entity.
    /// </summary>
    public class ColonyViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Edit buffer state
        private ReadOnlyColony _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _planetName = string.Empty;
        private string _colonyName = string.Empty;
        private string _systemName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColonyViewModel"/> class.
        /// </summary>
        public ColonyViewModel()
        {
        }

        // -----------------------------------------------------------------------
        // Editable scalar fields
        // -----------------------------------------------------------------------

        /// <summary>Gets or sets the planet name.</summary>
        public string PlanetName
        {
            get => _planetName;
            set => _planetName = value;
        }

        /// <summary>Gets or sets the colony name.</summary>
        public string ColonyName
        {
            get => _colonyName;
            set => _colonyName = value;
        }

        /// <summary>Gets or sets the system name.</summary>
        public string SystemName
        {
            get => _systemName;
            set => _systemName = value;
        }

        // -----------------------------------------------------------------------
        // Read-only state
        // -----------------------------------------------------------------------

        /// <summary>Gets a value indicating whether this is a new colony not yet saved.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets the colony UUID.</summary>
        public string UUID => _uuid;

        /// <summary>Gets the owner UUID.</summary>
        public string OwnerUUID => _ownerUUID;

        /// <summary>Gets the original snapshot this edit buffer was loaded from.</summary>
        public ReadOnlyColony Original => _original;

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether any local field differs from the original snapshot.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (_original == null)
                {
                    // New colony - dirty once any field has a non-default value
                    return !string.IsNullOrEmpty(_planetName)
                        || !string.IsNullOrEmpty(_colonyName)
                        || !string.IsNullOrEmpty(_systemName);
                }

                if (_planetName != (_original.PlanetName ?? string.Empty))
                {
                    return true;
                }

                if (_colonyName != (_original.ColonyName ?? string.Empty))
                {
                    return true;
                }

                if (_systemName != (_original.SystemName ?? string.Empty))
                {
                    return true;
                }

                return false;
            }
        }

        // -----------------------------------------------------------------------
        // LoadFrom / Reset
        // -----------------------------------------------------------------------

        /// <summary>
        /// Loads field values from a ReadOnlyColony snapshot.
        /// Retains the original for dirty comparison.
        /// </summary>
        public void LoadFrom(ReadOnlyColony ro)
        {
            _original = ro;
            _uuid = ro.UUID;
            _ownerUUID = ro.OwnerUUID ?? string.Empty;
            _planetName = ro.PlanetName ?? string.Empty;
            _colonyName = ro.ColonyName ?? string.Empty;
            _systemName = ro.SystemName ?? string.Empty;
        }

        /// <summary>
        /// Resets to empty state for a new colony.
        /// </summary>
        public void Reset()
        {
            _original = null;
            _uuid = null;
            _ownerUUID = string.Empty;
            _planetName = string.Empty;
            _colonyName = string.Empty;
            _systemName = string.Empty;
        }

        // -----------------------------------------------------------------------
        // Build request DTOs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds an update request carrying both the original snapshot
        /// and the current local state.
        /// </summary>
        public ColonyUpdateRequest BuildUpdateRequest()
        {
            return new ColonyUpdateRequest
            {
                Original = _original,
                PlanetName = _planetName,
                ColonyName = _colonyName,
                SystemName = _systemName,
            };
        }

        /// <summary>
        /// Builds a create request for a new colony.
        /// </summary>
        public ColonyCreateRequest BuildCreateRequest()
        {
            return new ColonyCreateRequest
            {
                PlanetName = _planetName,
                ColonyName = _colonyName,
                SystemName = _systemName,
            };
        }
    }
}
