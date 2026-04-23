using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Identifies a specific item type/base combination as a lock key.
    /// Multiple stacks of the same item type are supported via BaseItemTypeID.
    /// </summary>
    public struct ItemKey : IEquatable<ItemKey>
    {
        public Models.ItemType.ItemTypeEnum ItemType { get; set; }
        public string BaseItemTypeID { get; set; }

        public ItemKey(Models.ItemType.ItemTypeEnum itemType, string baseItemTypeID)
        {
            ItemType = itemType;
            BaseItemTypeID = baseItemTypeID ?? string.Empty;
        }

        /// <summary>
        /// Serializes to "ItemType:BaseItemTypeID" for use as a JSON object key.
        /// </summary>
        public override string ToString() => $"{ItemType}:{BaseItemTypeID}";

        /// <summary>
        /// Parses a key string produced by ToString().
        /// </summary>
        public static ItemKey Parse(string key)
        {
            if (string.IsNullOrEmpty(key))
                return new ItemKey(Models.ItemType.ItemTypeEnum.None, string.Empty);

            int sep = key.IndexOf(':');
            if (sep < 0)
                return new ItemKey(Models.ItemType.ItemTypeEnum.None, key);

            string typePart = key.Substring(0, sep);
            string idPart = key.Substring(sep + 1);

            Models.ItemType.ItemTypeEnum itemType;
            Enum.TryParse(typePart, out itemType);
            return new ItemKey(itemType, idPart);
        }

        public bool Equals(ItemKey other) =>
            ItemType == other.ItemType &&
            string.Equals(BaseItemTypeID, other.BaseItemTypeID, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)ItemType * 397) ^ (BaseItemTypeID?.GetHashCode() ?? 0);
            }
        }
    }

    /// <summary>
    /// Represents a single item lock entry returned by GetLocksForProcess.
    /// </summary>
    public class ItemLock
    {
        public ItemKey Key { get; set; }
        public int Quantity { get; set; }

        public ItemLock(ItemKey key, int quantity)
        {
            Key = key;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Tracks item locks by process UUID. A process (e.g. a structure identified by its UUID)
    /// can lock quantities of items. The same item type can be locked by multiple processes.
    /// </summary>
    [JsonConverter(typeof(LockTrackingJsonConverter))]
    public class LockTracking
    {
        // Outer key: process UUID. Inner key: item key. Value: locked quantity.
        private Dictionary<string, Dictionary<ItemKey, int>> _locks =
            new Dictionary<string, Dictionary<ItemKey, int>>();

        private readonly object _syncRoot = new object();

        /// <summary>
        /// Locks a quantity of a single item for the given process.
        /// Adds to any existing lock for the same process and item.
        /// </summary>
        public void LockItem(string processUUID, Models.ItemType.ItemTypeEnum itemType, string baseItemTypeID, int quantity)
        {
            if (string.IsNullOrEmpty(processUUID)) throw new ArgumentNullException(nameof(processUUID));

            lock (_syncRoot)
            {
                Dictionary<ItemKey, int> processLocks;
                if (!_locks.TryGetValue(processUUID, out processLocks))
                {
                    processLocks = new Dictionary<ItemKey, int>();
                    _locks[processUUID] = processLocks;
                }

                var key = new ItemKey(itemType, baseItemTypeID);
                int existing;
                processLocks.TryGetValue(key, out existing);
                processLocks[key] = existing + quantity;
            }
        }

        /// <summary>
        /// Locks multiple items for the given process in a single call.
        /// </summary>
        public void LockItems(string processUUID, IEnumerable<ItemLock> items)
        {
            lock (_syncRoot)
            {
                foreach (var item in items)
                    LockItem(processUUID, item.Key.ItemType, item.Key.BaseItemTypeID, item.Quantity);
            }
        }

        /// <summary>
        /// Returns the total quantity locked across all processes for the given item.
        /// </summary>
        public int GetLockedQuantity(Models.ItemType.ItemTypeEnum itemType, string baseItemTypeID)
        {
            lock (_syncRoot)
            {
                var key = new ItemKey(itemType, baseItemTypeID);
                return _locks.Values.Sum(processLocks =>
                {
                    int qty;
                    processLocks.TryGetValue(key, out qty);
                    return qty;
                });
            }
        }

        /// <summary>
        /// Returns all item locks held by the given process.
        /// Returns an empty list if the process has no locks.
        /// </summary>
        public IReadOnlyList<ItemLock> GetLocksForProcess(string processUUID)
        {
            lock (_syncRoot)
            {
                Dictionary<ItemKey, int> processLocks;
                if (!_locks.TryGetValue(processUUID, out processLocks))
                    return new List<ItemLock>();

                return processLocks
                    .Select(kv => new ItemLock(kv.Key, kv.Value))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Removes all locks held by the given process.
        /// </summary>
        public void ClearLocksForProcess(string processUUID)
        {
            lock (_syncRoot)
            {
                _locks.Remove(processUUID);
            }
        }

        /// <summary>
        /// Exposes the raw lock data for JSON serialization only.
        /// Use the public API methods for all other access.
        /// </summary>
        [JsonIgnore]
        internal Dictionary<string, Dictionary<ItemKey, int>> RawLocks => _locks;
    }

    public class LockTrackingJsonConverter : JsonConverter<LockTracking>
    {
        public override void WriteJson(JsonWriter writer, LockTracking value, JsonSerializer serializer)
        {
            // { "processUUID": { "ItemType:BaseID": quantity, ... }, ... }
            writer.WriteStartObject();
            foreach (var process in value.RawLocks)
            {
                writer.WritePropertyName(process.Key);
                writer.WriteStartObject();
                foreach (var item in process.Value)
                {
                    writer.WritePropertyName(item.Key.ToString());
                    writer.WriteValue(item.Value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        public override LockTracking ReadJson(
            JsonReader reader,
            Type objectType,
            LockTracking existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var tracking = existingValue ?? new LockTracking();

            if (reader.TokenType == JsonToken.Null) return tracking;

            // Expect outer object: { processUUID: { itemKey: quantity } }
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName) continue;
                string processUUID = reader.Value as string;

                // Read inner object
                reader.Read(); // StartObject
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    string itemKeyStr = reader.Value as string;
                    reader.Read(); // value
                    int quantity = Convert.ToInt32(reader.Value);

                    var itemKey = ItemKey.Parse(itemKeyStr);
                    tracking.LockItem(processUUID, itemKey.ItemType, itemKey.BaseItemTypeID, quantity);
                }
            }

            return tracking;
        }
    }
}
