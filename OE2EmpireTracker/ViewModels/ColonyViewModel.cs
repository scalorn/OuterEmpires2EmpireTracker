using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.ViewModels
{
    /// <summary>
    /// Wraps a Colony data object and exposes typed operations,
    /// hiding direct list and bag manipulation from the UI layer.
    /// </summary>
    public class ColonyViewModel
    {
        private readonly Colony _colony;
        private readonly PlayerContext _playerContext;
        private readonly ColonyStatusCalculator _calculator;
        private List<ColonyStructureViewModel> _cachedStructureVMs;

        public Colony Data => _colony;

        public ColonyStatusCalculator Calculator => _calculator;

        public ColonyViewModel(Colony colony, PlayerContext playerContext)
        {
            _colony = colony ?? throw new ArgumentNullException(nameof(colony));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _calculator = new ColonyStatusCalculator(_colony);
        }

        // -----------------------------------------------------------------------
        // Colony identity
        // -----------------------------------------------------------------------

        public string PlanetName
        {
            get => _colony.PlanetName;
            set => _colony.PlanetName = value;
        }

        public string ColonyName
        {
            get => _colony.ColonyName;
            set => _colony.ColonyName = value;
        }

        public string UUID => _colony.UUID;

        // -----------------------------------------------------------------------
        // Structure management
        // -----------------------------------------------------------------------

        public IReadOnlyList<ColonyStructureViewModel> StructureViewModels
        {
            get
            {
                if (_cachedStructureVMs == null)
                {
                    _cachedStructureVMs = _colony.Structures
                        .Select(s => new ColonyStructureViewModel(s, _playerContext))
                        .ToList();
                }
                return _cachedStructureVMs.AsReadOnly();
            }
        }

        /// <summary>
        /// Clears the cached StructureViewModels so the next access rebuilds from the colony's Structures list.
        /// Call after adding, removing, or reordering structures.
        /// </summary>
        public void InvalidateStructureViewModels()
        {
            _cachedStructureVMs = null;
        }

        public ColonyStructureViewModel AddStructure(string flatpackBlueprintUUID)
        {
            int existingCount = _colony.Structures
                .Count(s => s.FlatpackBlueprintUUID == flatpackBlueprintUUID);

            var structure = new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackBlueprintUUID,
                displaySequence = existingCount + 1
            };
            _colony.Structures.Add(structure);
            InvalidateStructureViewModels();
            return new ColonyStructureViewModel(structure, _playerContext);
        }

        public void RecalculateStatus()
        {
            _calculator.CalculateBuilt();
            _calculator.CalculateIdeal();
        }

        // -----------------------------------------------------------------------
        // Item management
        // -----------------------------------------------------------------------

        public void AddItem(Item item)
        {
            _colony.Items.AddItem(item);
        }

        public void RemoveItem(string uuid)
        {
            _colony.Items.Remove(uuid);
        }

        public IEnumerable<KeyValuePair<string, Item>> GetItems() => _colony.Items.Items;

        // -----------------------------------------------------------------------
        // Commodity request management
        // -----------------------------------------------------------------------

        public CommodityRequested AddCommodityRequest(string commodityName, int requested = 0, DateTime? needBy = null)
        {
            var request = new CommodityRequested
            {
                Name = commodityName,
                Requested = requested,
                Delivered = 0,
                NeedBy = needBy ?? DateTime.MinValue
            };
            _colony.Commodities.Add(request);
            return request;
        }

        public void RemoveCommodityRequest(CommodityRequested request)
        {
            _colony.Commodities.Remove(request);
        }

        /// <summary>
        /// Removes fulfilled commodity requests that are more than 3 days past their NeedBy date.
        /// </summary>
        public int CleanupExpiredCommodityRequests()
        {
            var now = SystemClock.UtcNow;
            var expired = _colony.Commodities
                .Where(cr => cr.Fulfilled && cr.NeedBy != DateTime.MinValue && (now - cr.NeedBy).TotalDays > 3)
                .ToList();
            foreach (var cr in expired)
                _colony.Commodities.Remove(cr);
            return expired.Count;
        }

        public IReadOnlyList<CommodityRequested> GetCommodityRequests() =>
            _colony.Commodities.AsReadOnly();

        // -----------------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------------

        public void EnsureUUID()
        {
            if (_colony.UUID == null)
                _colony.UUID = Guid.NewGuid().ToString();
        }

        public void Save()
        {
            EnsureUUID();
            if (!_playerContext.ColonyList.Contains(_colony))
                _playerContext.AddColony(_colony);
            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(_colony.UUID);
        }
    }
}
