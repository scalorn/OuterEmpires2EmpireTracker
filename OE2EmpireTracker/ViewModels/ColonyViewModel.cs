using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Edit buffer for colony data. Holds local field copies disconnected from the entity.
    /// The form reads/writes these local fields. Only ColonyService mutates the actual entity.
    /// Legacy shims (constructor taking Colony + PlayerContext, Data, Save, etc.) are included
    /// for FormColonyV2 backward compatibility until task 8 migration removes them.
    /// </summary>
    public class ColonyViewModel
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Legacy fields (used by old FormColonyV2 API - removed in task 8)
        private Colony _colony;
        private PlayerContext _playerContext;
        private ColonyStatusCalculator _calculator;
        private List<ColonyStructureViewModel> _cachedStructureVMs;

        // Edit buffer state (new API)
        private ReadOnlyColony _original;
        private string _uuid;
        private string _ownerUUID = string.Empty;
        private string _planetName = string.Empty;
        private string _colonyName = string.Empty;
        private string _systemName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColonyViewModel"/> class.
        /// Legacy constructor for FormColonyV2 compatibility. Removed in task 8.
        /// </summary>
        public ColonyViewModel(Colony colony, PlayerContext playerContext)
        {
            _colony = colony ?? throw new ArgumentNullException(nameof(colony));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _calculator = new ColonyStatusCalculator(_colony);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColonyViewModel"/> class.
        /// New parameterless constructor for the edit buffer pattern.
        /// </summary>
        public ColonyViewModel()
        {
        }

        // -----------------------------------------------------------------------
        // Colony identity - dual mode (legacy write-through / new edit buffer)
        // -----------------------------------------------------------------------

        /// <summary>Gets or sets the planet name.</summary>
        public string PlanetName
        {
            get => _colony != null ? _colony.PlanetName : _planetName;
            set
            {
                if (_colony != null)
                {
                    _colony.PlanetName = value;
                }

                _planetName = value;
            }
        }

        /// <summary>Gets or sets the colony name.</summary>
        public string ColonyName
        {
            get => _colony != null ? _colony.ColonyName : _colonyName;
            set
            {
                if (_colony != null)
                {
                    _colony.ColonyName = value;
                }

                _colonyName = value;
            }
        }

        /// <summary>Gets or sets the system name.</summary>
        public string SystemName
        {
            get => _colony != null ? _colony.SystemName : _systemName;
            set
            {
                if (_colony != null)
                {
                    _colony.SystemName = value;
                }

                _systemName = value;
            }
        }

        // -----------------------------------------------------------------------
        // Read-only state
        // -----------------------------------------------------------------------

        /// <summary>Gets a value indicating whether this is a new colony not yet saved.</summary>
        public bool IsNew => _original == null;

        /// <summary>Gets the colony UUID.</summary>
        public string UUID => _colony != null ? _colony.UUID : _uuid;

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

        // -----------------------------------------------------------------------
        // Legacy API (FormColonyV2 backward compatibility - removed in task 8)
        // -----------------------------------------------------------------------

        /// <summary>Gets the underlying colony entity. Legacy - removed in task 8.</summary>
        public Colony Data => _colony;

        /// <summary>Gets the status calculator. Legacy - removed in task 8.</summary>
        public ColonyStatusCalculator Calculator => _calculator;

        /// <summary>Gets the cached structure view models. Legacy - removed in task 8.</summary>
        public IReadOnlyList<ColonyStructureViewModel> StructureViewModels
        {
            get
            {
                if (_cachedStructureVMs == null)
                {
                    _cachedStructureVMs = CollectionSortHelper.OrderStructures(_colony.Structures)
                        .Select(s => new ColonyStructureViewModel(s, _playerContext))
                        .ToList();
                }

                return _cachedStructureVMs.AsReadOnly();
            }
        }

        /// <summary>Clears cached structure VMs. Legacy - removed in task 8.</summary>
        public void InvalidateStructureViewModels()
        {
            _cachedStructureVMs = null;
        }

        /// <summary>Adds a structure. Legacy - removed in task 8.</summary>
        public ColonyStructureViewModel AddStructure(string flatpackBlueprintUUID)
        {
            int existingCount = _colony.Structures
                .Count(s => s.FlatpackBlueprintUUID == flatpackBlueprintUUID);

            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackBlueprintUUID,
                DisplaySequence = existingCount + 1,
            };

            _colony.Structures.Add(structure);

            int maxSeq = 0;
            foreach (var s in _colony.Structures)
            {
                if (s.BuildQueueSequence > maxSeq)
                {
                    maxSeq = s.BuildQueueSequence;
                }
            }

            structure.BuildQueueSequence = maxSeq + 1;
            InvalidateStructureViewModels();
            return new ColonyStructureViewModel(structure, _playerContext);
        }

        /// <summary>Recalculates colony status. Legacy - removed in task 8.</summary>
        public void RecalculateStatus()
        {
            _calculator.CalculateBuilt();
            _calculator.CalculateIdeal();
        }

        /// <summary>Adds an item. Legacy - removed in task 8.</summary>
        public void AddItem(Item item)
        {
            _colony.Items.AddItem(item);
        }

        /// <summary>Removes an item. Legacy - removed in task 8.</summary>
        public void RemoveItem(string uuid)
        {
            _colony.Items.Remove(uuid);
        }

        /// <summary>Gets items. Legacy - removed in task 8.</summary>
        public IEnumerable<KeyValuePair<string, Item>> GetItems() => _colony.Items.Items;

        /// <summary>Adds a commodity request. Legacy - removed in task 8.</summary>
        public CommodityRequested AddCommodityRequest(string commodityName, int requested = 0, DateTime? needBy = null)
        {
            var request = new CommodityRequested
            {
                Name = commodityName,
                Requested = requested,
                Delivered = 0,
                NeedBy = needBy ?? DateTime.MinValue,
            };

            _colony.Commodities.Add(request);
            return request;
        }

        /// <summary>Removes a commodity request. Legacy - removed in task 8.</summary>
        public void RemoveCommodityRequest(CommodityRequested request)
        {
            _colony.Commodities.Remove(request);
        }

        /// <summary>Cleans up expired commodity requests. Legacy - removed in task 8.</summary>
        public int CleanupExpiredCommodityRequests()
        {
            var now = SystemClock.UtcNow;
            var expired = _colony.Commodities
                .Where(cr => cr.Fulfilled && cr.NeedBy != DateTime.MinValue && (now - cr.NeedBy).TotalDays > 3)
                .ToList();
            foreach (var cr in expired)
            {
                _colony.Commodities.Remove(cr);
            }

            return expired.Count;
        }

        /// <summary>Gets commodity requests. Legacy - removed in task 8.</summary>
        public IReadOnlyList<CommodityRequested> GetCommodityRequests() =>
            _colony.Commodities.AsReadOnly();

        /// <summary>Ensures the colony has a UUID. Legacy - removed in task 8.</summary>
        public void EnsureUUID()
        {
            if (_colony.UUID == null)
            {
                _colony.UUID = Guid.NewGuid().ToString();
            }
        }

        /// <summary>Saves the colony. Legacy - removed in task 8.</summary>
        public void Save()
        {
            EnsureUUID();
            if (!_playerContext.ColonyList.Contains(_colony))
            {
                _playerContext.AddColony(_colony);
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(_colony.UUID);
        }
    }
}
