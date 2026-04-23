using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Models
{
    [JsonConverter(typeof(PropertyBagJSONConverter))]
    public class PropertyBag
    {
        private readonly object _syncRoot = new object();

        public Dictionary<string, string> Properties { get; set; }

        public PropertyBag()
        {
            Properties = new Dictionary<string, string>();
        }

        public bool ContainsKey(string name)
        {
            lock (_syncRoot)
            {
                return Properties.ContainsKey(name);
            }
        }

        public bool GetDecimal(string name, decimal defaultValue, out decimal value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = string.Empty;
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = decimal.TryParse(valueStr, out value);
                }

                if (!ret)
                {
                    value = defaultValue;
                }

                return ret;
            }
        }

        public bool GetLong(string name, long defaultValue, out long value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = string.Empty;
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = long.TryParse(valueStr, out value);
                }

                return ret;
            }
        }

        public bool GetBoolean(string name, bool defaultValue, out bool value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = string.Empty;
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = bool.TryParse(valueStr, out value);
                }

                return ret;
            }
        }

        public bool GetString(string name, string defaultValue, out string value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                bool ret = Properties.TryGetValue(name, out value);
                if (!ret)
                {
                    value = defaultValue;
                }

                return ret;
            }
        }

        public bool SetProperty(string name, decimal value)
        {
            lock (_syncRoot)
            {
                return SetProperty_Internal(name, string.Empty + value);
            }
        }

        public bool SetProperty(string name, bool value)
        {
            lock (_syncRoot)
            {
                return SetProperty_Internal(name, string.Empty + value);
            }
        }

        public bool SetProperty(string name, string value)
        {
            lock (_syncRoot)
            {
                Properties[name] = value;
                return true;
            }
        }

        private bool SetProperty_Internal(string name, string value)
        {
            Properties[name] = value;
            return true;
        }

        public bool Remove(string name)
        {
            lock (_syncRoot)
            {
                bool present = false;
                // Write the value so the UI knows what to do.
                if (Properties.ContainsKey(name))
                {
                    present = true;
                    Properties.Remove(name);
                }

                return present;
            }
        }

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return Properties.Count;
                }
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                Properties.Clear();
            }
        }
    }

    public class PropertyBagJSONConverter : JsonConverter<PropertyBag>
    {
        public override void WriteJson(JsonWriter writer, PropertyBag value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var item in value.Properties)
            {
                writer.WritePropertyName(item.Key);
                writer.WriteValue(item.Value);
            }

            writer.WriteEndObject();
        }

        // ReadJson implementation required if deserialization is needed
        public override PropertyBag ReadJson(JsonReader reader, Type objectType, PropertyBag existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            PropertyBag bag = existingValue;
            if (bag == null)
            {
                bag = new PropertyBag();
            }

            string name = string.Empty;
            string value = string.Empty;
            // reader.Read();
            JsonToken token = JsonToken.None;
            do
            {
                reader.Read();
                token = reader.TokenType;
                if (token == JsonToken.PropertyName)
                {
                    name = reader.Value as string;
                }

                if (token == JsonToken.String)
                {
                    value = reader.Value as string;
                    bag.Properties.Add(name, value);
                }
            }
            while (token != JsonToken.EndObject);

            return bag;
        }
    }
}
