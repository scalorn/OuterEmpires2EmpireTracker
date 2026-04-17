using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public bool getDecimal(string name, decimal defaultValue, out decimal value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = "";
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = Decimal.TryParse(valueStr, out value);
                }
                if (!ret)
                {
                    value = defaultValue;
                }
                return ret;
            }
        }
        public bool getLong(string name, long defaultValue, out long value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = "";
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = long.TryParse(valueStr, out value);
                }
                return ret;
            }
        }

        public bool getBoolean(string name, bool defaultValue, out bool value)
        {
            lock (_syncRoot)
            {
                value = defaultValue;
                string valueStr = "";
                bool ret = Properties.TryGetValue(name, out valueStr);
                if (ret)
                {
                    ret = Boolean.TryParse(valueStr, out value);
                }
                return ret;
            }
        }
        public bool getString(string name, string defaultValue, out string value)
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

        public bool setProperty(string name, decimal value)
        {
            lock (_syncRoot)
            {
                return setProperty_Internal(name, "" + value);
            }
        }
        public bool setProperty(string name, bool value)
        {
            lock (_syncRoot)
            {
                return setProperty_Internal(name, "" + value);
            }
        }
        public bool setProperty(string name, string value)
        {
            lock (_syncRoot)
            {
                Properties[name] = value;
                return true;
            }
        }

        private bool setProperty_Internal(string name, string value)
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
            foreach(var item in value.Properties)
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
            string name = "";
            string value = "";
            //reader.Read();
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
            } while (token != JsonToken.EndObject);

            return bag;
        }
    }

}