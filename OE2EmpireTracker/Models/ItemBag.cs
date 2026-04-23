using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using OE2EmpireTracker.Services;
using Sgml;

namespace OE2EmpireTracker.Models
{
    [JsonConverter(typeof(ItemBagJSONConverter))]
    public class ItemBag
    {
        public Dictionary<string, Item> Items { get; set; }

        // Secondary index: (ItemType, BaseItemTypeID) -> list of items
        private Dictionary<(ItemType.ItemTypeEnum, string), List<Item>> _typeIndex;

        // Secondary index: (ItemType, BaseItemTypeID, Purity) -> list of items (Resources only)
        private Dictionary<(ItemType.ItemTypeEnum, string, string), List<Item>> _resourceIndex;

        private readonly object _syncRoot = new object();

        public ItemBag()
        {
            Items = new Dictionary<string, Item>();
        }

        public bool ContainsKey(string uuid)
        {
            lock (_syncRoot)
            {
                return Items.ContainsKey(uuid);
            }
        }

        public void AddItem(Item item)
        {
            lock (_syncRoot)
            {
                Items.Add(item.UUID, item);
                _typeIndex = null;
                _resourceIndex = null;
            }
        }

        public int Count()
        {
            lock (_syncRoot)
            {
                return Items.Count;
            }
        }

        private void EnsureTypeIndex()
        {
            if (_typeIndex != null) return;
            _typeIndex = new Dictionary<(ItemType.ItemTypeEnum, string), List<Item>>();
            foreach (var kvp in Items)
            {
                var key = (kvp.Value.ItemType, kvp.Value.BaseItemTypeID ?? string.Empty);
                if (!_typeIndex.TryGetValue(key, out var list))
                {
                    list = new List<Item>();
                    _typeIndex[key] = list;
                }

                list.Add(kvp.Value);
            }
        }

        private void EnsureResourceIndex()
        {
            if (_resourceIndex != null) return;
            _resourceIndex = new Dictionary<(ItemType.ItemTypeEnum, string, string), List<Item>>();
            foreach (var kvp in Items)
            {
                if (kvp.Value.ItemType != ItemType.ItemTypeEnum.Resource) continue;
                var key = (kvp.Value.ItemType, kvp.Value.BaseItemTypeID ?? string.Empty, kvp.Value.ResourcePurity ?? string.Empty);
                if (!_resourceIndex.TryGetValue(key, out var list))
                {
                    list = new List<Item>();
                    _resourceIndex[key] = list;
                }

                list.Add(kvp.Value);
            }
        }

        /// <summary>
        /// Returns the total quantity of all items in the bag that match
        /// the given ItemType and BaseItemTypeID. Sums across multiple stacks.
        /// </summary>
        public int CountByType(Models.ItemType.ItemTypeEnum itemType, string baseItemTypeID)
        {
            lock (_syncRoot)
            {
                EnsureTypeIndex();
                var key = (itemType, baseItemTypeID ?? string.Empty);
                return _typeIndex.TryGetValue(key, out var list) ? list.Sum(i => i.Quantity) : 0;
            }
        }

        public List<Item> FindByType(Models.ItemType.ItemTypeEnum itemType, string baseItemTypeID)
        {
            lock (_syncRoot)
            {
                EnsureTypeIndex();
                var key = (itemType, baseItemTypeID ?? string.Empty);
                return _typeIndex.TryGetValue(key, out var list) ? new List<Item>(list) : new List<Item>();
            }
        }

        public List<Item> FindResource(string resource, string purity)
        {
            lock (_syncRoot)
            {
                EnsureResourceIndex();
                var key = (ItemType.ItemTypeEnum.Resource, resource ?? string.Empty, purity ?? string.Empty);
                return _resourceIndex.TryGetValue(key, out var list) ? new List<Item>(list) : new List<Item>();
            }
        }

        public bool Remove(string uuid)
        {
            lock (_syncRoot)
            {
                bool present = false;
                // Write the value so the UI knows what to do.
                if (Items.ContainsKey(uuid))
                {
                    present = true;
                    Items.Remove(uuid);
                    _typeIndex = null;
                    _resourceIndex = null;
                }

                return present;
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                Items.Clear();
                _typeIndex = null;
                _resourceIndex = null;
            }
        }
    }

    public class ItemBagJSONConverter : JsonConverter<ItemBag>
    {
        public override void WriteJson(JsonWriter writer, ItemBag value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, Item> entry in value.Items)
            {
                writer.WritePropertyName(entry.Key);
                String text = JsonConvert.SerializeObject(entry.Value, JsonSettings.SerializerSettings);
                writer.WriteRawValue(text);
            }

            writer.WriteEndObject();
        }

        // ReadJson implementation required if deserialization is needed
        public override ItemBag ReadJson(JsonReader reader, Type objectType, ItemBag existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            ItemBag bag = existingValue;
            if (bag == null)
            {
                bag = new ItemBag();
            }

            string name = string.Empty;
            JsonToken token = JsonToken.None;
            do
            {
                reader.Read();
                token = reader.TokenType;
                if (token == JsonToken.PropertyName)
                {
                    name = reader.Value as string;
                }

                    if (token == JsonToken.StartObject)
                    {
                        // Deserialize the Item object using the provided serializer. This will consume
                        // the entire object from the reader.
                        Item item = serializer.Deserialize<Item>(reader);
                        if (item != null && !string.IsNullOrEmpty(item.UUID))
                        {
                            // Use index assignment to replace any existing entry with the same UUID.
                            bag.Items[item.UUID] = item;
                        }
                    }

            } while (token != JsonToken.EndObject);

            return bag;
        }
    }
}
