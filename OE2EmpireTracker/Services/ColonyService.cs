using System;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services.Migration;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all Colony mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus ColonyParser called by the service during import,
    /// Colony.ProcessColony for background processing, JSON deserialization, and migration code)
    /// mutates Colony objects.
    /// </summary>
    public class ColonyService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        private readonly ColonyParser _colonyParser;

        public ColonyService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
            _colonyParser = new ColonyParser();
        }

        /// <summary>
        /// Applies changes from the update request to an existing colony, persists, and fires the change event.
        /// Throws InvalidOperationException if UUID not found.
        /// Throws TimeoutException if write lock times out.
        /// </summary>
        public ReadOnlyColony Update(string uuid, ColonyUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                throw new ArgumentNullException(nameof(uuid));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var colony = _playerContext.FindMutableColony(uuid);
            if (colony == null)
            {
                throw new InvalidOperationException("Colony not found: " + uuid);
            }

            Log.Info(
                "ColonyService.Update: UUID={0} planet='{1}' -> '{2}'",
                uuid,
                colony.PlanetName,
                request.PlanetName);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + uuid);
            }

            try
            {
                colony.PlanetName = request.PlanetName;
                colony.ColonyName = request.ColonyName;
                colony.SystemName = request.SystemName;
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(uuid);
            return new ReadOnlyColony(colony);
        }

        /// <summary>
        /// Creates a new colony, assigns a UUID, adds to the list, persists, and fires the change event.
        /// </summary>
        public ReadOnlyColony Create(ColonyCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var colony = new Colony();
            colony.UUID = Guid.NewGuid().ToString();
            colony.OwnerUUID = _playerContext.CurrentPlayerUUID;
            colony.PlanetName = request.PlanetName;
            colony.ColonyName = request.ColonyName;
            colony.SystemName = request.SystemName;

            Log.Info(
                "ColonyService.Create: planet='{0}' UUID={1}",
                colony.PlanetName,
                colony.UUID);

            _playerContext.AddColony(colony);
            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colony.UUID);
            return new ReadOnlyColony(colony);
        }

        /// <summary>
        /// Removes a colony. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                return;
            }

            var colony = _playerContext.FindMutableColony(uuid);
            if (colony == null)
            {
                return;
            }

            Log.Info("ColonyService.Delete: UUID={0} planet='{1}'", uuid, colony.PlanetName);

            _playerContext.RemoveColony(colony);
            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(uuid);
        }

        /// <summary>
        /// Imports a parsed temporary colony. Deduplicates by PlanetName (case-insensitive),
        /// merges into existing or creates new, persists, and fires event.
        /// </summary>
        public ReadOnlyColony Import(Colony tempColony, string extractedHtml, EmpireContext empireContext)
        {
            if (tempColony == null)
            {
                throw new ArgumentNullException(nameof(tempColony));
            }

            var existingColonies = _playerContext.GetCurrentPlayerColonies();
            var existingColony = ColonyImportHelper.FindByPlanet(
                existingColonies, tempColony.PlanetName, tempColony.SystemName);

            Colony colony;
            if (existingColony != null)
            {
                Log.Info(
                    "ColonyService.Import: merging into existing UUID={0} planet='{1}'",
                    existingColony.UUID,
                    existingColony.PlanetName);

                if (!existingColony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
                {
                    throw new TimeoutException("Write lock timeout for colony: " + existingColony.UUID);
                }

                try
                {
                    ColonyImportHelper.MergeIdentity(existingColony, tempColony);

                    // Save ColonyName before ProcessHtml -- the parser's ParsePlanetOverview
                    // overwrites ColonyName with the game's (potentially truncated) value.
                    string preservedColonyName = existingColony.ColonyName;
                    _colonyParser.ProcessHtml(existingColony, extractedHtml, empireContext);
                    existingColony.ColonyName = preservedColonyName;

                    existingColony.LastImportDateTime = SurveyDateTimeParser.ToIsoString(SystemClock.UtcNow);
                    existingColony.StampBuildQueueSequence();
                }
                finally
                {
                    existingColony.ColonyLock.ExitWriteLock();
                }

                colony = existingColony;
            }
            else
            {
                colony = ColonyImportHelper.CreateFromTemp(tempColony, _playerContext.CurrentPlayerUUID);

                Log.Info(
                    "ColonyService.Import: created new UUID={0} planet='{1}'",
                    colony.UUID,
                    colony.PlanetName);

                colony.StampBuildQueueSequence();
                _playerContext.AddColony(colony);
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colony.UUID);
            return new ReadOnlyColony(colony);
        }

        /// <summary>
        /// Adds a new structure to the colony.
        /// </summary>
        public void AddStructure(string colonyUUID, string flatpackBlueprintUUID)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                var structure = new ColonyStructure();
                structure.UUID = Guid.NewGuid().ToString();
                structure.FlatpackBlueprintUUID = flatpackBlueprintUUID;

                // DisplaySequence = count of existing structures with same flatpack UUID + 1
                int sameTypeCount = colony.Structures
                    .Count(s => string.Equals(s.FlatpackBlueprintUUID, flatpackBlueprintUUID, StringComparison.Ordinal));
                structure.DisplaySequence = sameTypeCount + 1;

                // BuildQueueSequence = max existing + 1 (or 1 if none)
                int maxBuildQueue = colony.Structures.Count > 0
                    ? colony.Structures.Max(s => s.BuildQueueSequence)
                    : 0;
                structure.BuildQueueSequence = maxBuildQueue + 1;

                colony.Structures.Add(structure);

                Log.Info(
                    "ColonyService.AddStructure: colony={0} structure={1} flatpack={2} displaySeq={3} buildQueue={4}",
                    colonyUUID,
                    structure.UUID,
                    flatpackBlueprintUUID,
                    structure.DisplaySequence,
                    structure.BuildQueueSequence);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Removes a structure from the colony. No-op if structure not found.
        /// </summary>
        public void RemoveStructure(string colonyUUID, string structureUUID)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                var structure = colony.Structures.FirstOrDefault(
                    s => string.Equals(s.UUID, structureUUID, StringComparison.Ordinal));

                if (structure != null)
                {
                    colony.Structures.Remove(structure);
                    Log.Info(
                        "ColonyService.RemoveStructure: colony={0} structure={1}",
                        colonyUUID,
                        structureUUID);
                }
                else
                {
                    Log.Debug(
                        "ColonyService.RemoveStructure: structure {0} not found in colony {1}",
                        structureUUID,
                        colonyUUID);
                }
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Adds an item to the colony's item bag.
        /// </summary>
        public void AddItem(string colonyUUID, Item item)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                colony.Items.AddItem(item);
                Log.Info(
                    "ColonyService.AddItem: colony={0} item={1}",
                    colonyUUID,
                    item.UUID);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Removes an item from the colony's item bag.
        /// </summary>
        public void RemoveItem(string colonyUUID, string itemUUID)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                colony.Items.Remove(itemUUID);
                Log.Info(
                    "ColonyService.RemoveItem: colony={0} item={1}",
                    colonyUUID,
                    itemUUID);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Updates the quantity of an item in the colony's item bag.
        /// </summary>
        public void UpdateItem(string colonyUUID, string itemUUID, int newQuantity)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                if (colony.Items.ContainsKey(itemUUID))
                {
                    colony.Items.Items[itemUUID].Quantity = newQuantity;
                    Log.Info(
                        "ColonyService.UpdateItem: colony={0} item={1} newQty={2}",
                        colonyUUID,
                        itemUUID,
                        newQuantity);
                }
                else
                {
                    Log.Debug(
                        "ColonyService.UpdateItem: item {0} not found in colony {1}",
                        itemUUID,
                        colonyUUID);
                }
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Adds a commodity request to the colony.
        /// </summary>
        public void AddCommodityRequest(string colonyUUID, string commodityName, int requested, DateTime? needBy)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                var commodityRequested = new CommodityRequested
                {
                    Name = commodityName,
                    Requested = requested,
                    NeedBy = needBy ?? default(DateTime),
                };
                colony.Commodities.Add(commodityRequested);
                Log.Info(
                    "ColonyService.AddCommodityRequest: colony={0} commodity='{1}' requested={2}",
                    colonyUUID,
                    commodityName,
                    requested);
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Removes a commodity request from the colony by name (case-insensitive).
        /// </summary>
        public void RemoveCommodityRequest(string colonyUUID, string commodityName)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                var existing = colony.Commodities.FirstOrDefault(
                    c => string.Equals(c.Name, commodityName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    colony.Commodities.Remove(existing);
                    Log.Info(
                        "ColonyService.RemoveCommodityRequest: colony={0} commodity='{1}'",
                        colonyUUID,
                        commodityName);
                }
                else
                {
                    Log.Debug(
                        "ColonyService.RemoveCommodityRequest: commodity '{0}' not found in colony {1}",
                        commodityName,
                        colonyUUID);
                }
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Updates a commodity request in the colony by name (case-insensitive).
        /// </summary>
        public void UpdateCommodityRequest(string colonyUUID, string commodityName, int requested, int delivered, DateTime needBy, bool fulfilled)
        {
            var colony = GetMutableColonyOrThrow(colonyUUID);

            if (!colony.ColonyLock.TryEnterWriteLock(Colony.WriteLockTimeoutMs))
            {
                throw new TimeoutException("Write lock timeout for colony: " + colonyUUID);
            }

            try
            {
                var existing = colony.Commodities.FirstOrDefault(
                    c => string.Equals(c.Name, commodityName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Requested = requested;
                    existing.Delivered = delivered;
                    existing.NeedBy = needBy;
                    existing.Fulfilled = fulfilled;
                    Log.Info(
                        "ColonyService.UpdateCommodityRequest: colony={0} commodity='{1}' requested={2} delivered={3} fulfilled={4}",
                        colonyUUID,
                        commodityName,
                        requested,
                        delivered,
                        fulfilled);
                }
                else
                {
                    Log.Debug(
                        "ColonyService.UpdateCommodityRequest: commodity '{0}' not found in colony {1}",
                        commodityName,
                        colonyUUID);
                }
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            _playerContext.WriteContext();
            _playerContext.OnColonyDataChanged(colonyUUID);
        }

        /// <summary>
        /// Looks up the mutable colony or throws InvalidOperationException.
        /// </summary>
        private Colony GetMutableColonyOrThrow(string colonyUUID)
        {
            if (string.IsNullOrEmpty(colonyUUID))
            {
                throw new ArgumentNullException(nameof(colonyUUID));
            }

            var colony = _playerContext.FindMutableColony(colonyUUID);
            if (colony == null)
            {
                throw new InvalidOperationException("Colony not found: " + colonyUUID);
            }

            return colony;
        }
    }
}