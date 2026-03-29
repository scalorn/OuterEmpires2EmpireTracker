using Newtonsoft.Json;
using Sgml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OE2EmpireTracker.Data
{
    [JsonConverter(typeof(ItemBagJSONConverter))]
    public class ItemBag
    {
        public Dictionary<string, Item> Items { get; set; }

        public ItemBag()
        {
            Items = new Dictionary<string, Item>();
        }

        public bool ContainsKey(string uuid)
        {
            return Items.ContainsKey(uuid);
        }

        public void AddItem(Item item)
        {
            Items.Add(item.UUID, item);
        }

        public int Count()
        {
            return Items.Count;
        }

        /// <summary>
        /// Returns the total quantity of all items in the bag that match
        /// the given ItemType and BaseItemTypeID. Sums across multiple stacks.
        /// </summary>
        public int CountByType(ItemType.ItemTypeEnum itemType, string baseItemTypeID)
        {
            return Items.Values
                .Where(i => i.ItemType == itemType &&
                            string.Equals(i.BaseItemTypeID, baseItemTypeID, StringComparison.Ordinal))
                .Sum(i => i.Quantity);
        }

        public bool Remove(string uuid)
        {
            bool present = false;
            // Write the value so the UI knows what to do.
            if (Items.ContainsKey(uuid))
            {
                present = true;
                Items.Remove(uuid);
            }

            return present;
        }
        public void Clear()
        {
            Items.Clear();
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
                String text = JsonConvert.SerializeObject(entry.Value,Formatting.Indented);
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

            string name = "";
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
