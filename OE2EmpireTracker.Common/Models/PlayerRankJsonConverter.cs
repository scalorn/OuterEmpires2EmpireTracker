using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OE2EmpireTracker.Models
{
    /// <summary>
    /// Custom JSON converter for <see cref="PlayerRank"/> that provides backward-compatible
    /// deserialization of old field names (Title, NextXP, CurrentXP) while always serializing
    /// using the new canonical names (RankName, XpToNextLevel, CurrentXp).
    /// </summary>
    public class PlayerRankJsonConverter : JsonConverter<PlayerRank>
    {
        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, PlayerRank value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Rank");
            writer.WriteValue(value.Rank);

            writer.WritePropertyName("CurrentXp");
            writer.WriteValue(value.CurrentXp);

            writer.WritePropertyName("XpToNextLevel");
            writer.WriteValue(value.XpToNextLevel);

            writer.WritePropertyName("RankName");
            writer.WriteValue(value.RankName);

            writer.WriteEndObject();
        }

        /// <inheritdoc/>
        public override PlayerRank ReadJson(
            JsonReader reader,
            Type objectType,
            PlayerRank existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return new PlayerRank();
            }

            JObject obj = JObject.Load(reader);
            var rank = new PlayerRank();

            rank.Rank = ReadInt(obj, "Rank");
            rank.CurrentXp = ReadLong(obj, "CurrentXp", "CurrentXP");
            rank.XpToNextLevel = ReadLong(obj, "XpToNextLevel", "NextXP");
            rank.RankName = ReadString(obj, "RankName", "Title");

            return rank;
        }

        private static int ReadInt(JObject obj, string name)
        {
            JToken token = obj[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                return 0;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }

            return 0;
        }

        private static long ReadLong(JObject obj, string newName, string oldName)
        {
            JToken token = obj[newName];
            if (token == null || token.Type == JTokenType.Null)
            {
                token = obj[oldName];
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                return 0;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<long>();
            }

            return 0;
        }

        private static string ReadString(JObject obj, string newName, string oldName)
        {
            JToken token = obj[newName];
            if (token == null || token.Type == JTokenType.Null)
            {
                token = obj[oldName];
            }

            if (token == null || token.Type == JTokenType.Null)
            {
                return string.Empty;
            }

            if (token.Type == JTokenType.String)
            {
                return token.Value<string>();
            }

            return string.Empty;
        }
    }
}
