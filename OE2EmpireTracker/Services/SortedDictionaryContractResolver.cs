using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// A contract resolver that ensures all Dictionary&lt;string, T&gt; properties
    /// are serialized with keys in ascending ordinal string order.
    /// This produces deterministic JSON output for git-friendly diffs.
    /// </summary>
    public class SortedDictionaryContractResolver : DefaultContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
        {
            var contract = base.CreateDictionaryContract(objectType);

            // Only wrap dictionaries with string keys
            if (HasStringKey(objectType))
            {
                contract.Converter = new SortedDictionaryConverter();
            }

            return contract;
        }

        private static bool HasStringKey(Type type)
        {
            // Check the type itself and all interfaces for IDictionary<string, T>
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                    iface.GetGenericArguments()[0] == typeof(string))
                {
                    return true;
                }
            }

            // Also check the type directly (for Dictionary<string, T>)
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                type.GetGenericArguments()[0] == typeof(string))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// A JSON converter that serializes dictionary entries with keys sorted
    /// in ascending ordinal string order. Only affects serialization;
    /// deserialization uses the default behavior.
    /// </summary>
    internal class SortedDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (IDictionary)value;
            var sortedKeys = dict.Keys.Cast<string>()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            writer.WriteStartObject();
            foreach (var key in sortedKeys)
            {
                writer.WritePropertyName(key);
                serializer.Serialize(writer, dict[key]);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // This should never be called because CanRead returns false
            throw new NotImplementedException("SortedDictionaryConverter only handles serialization.");
        }

        /// <summary>
        /// Return false so deserialization uses the default behavior.
        /// Only serialization (WriteJson) is customized.
        /// </summary>
        public override bool CanRead => false;
    }
}
