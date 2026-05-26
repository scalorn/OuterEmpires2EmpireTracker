using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class PlayerRankJsonConverterTests
    {
        // -------------------------------------------------------------------
        // Deserialization: Old field names map to new properties
        // Validates: Requirements 4.2, 5.3, 12.2
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_OldNames_TitleMapsToRankName()
        {
            string json = @"{ ""Rank"": 3, ""Title"": ""Commander"", ""NextXP"": 5000, ""CurrentXP"": 1200 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.RankName, Is.EqualTo("Commander"));
        }

        [Test]
        public void Deserialize_OldNames_NextXPMapsToXpToNextLevel()
        {
            string json = @"{ ""Rank"": 3, ""Title"": ""Commander"", ""NextXP"": 5000, ""CurrentXP"": 1200 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.XpToNextLevel, Is.EqualTo(5000L));
        }

        [Test]
        public void Deserialize_OldNames_CurrentXPMapsToCurrentXp()
        {
            string json = @"{ ""Rank"": 3, ""Title"": ""Commander"", ""NextXP"": 5000, ""CurrentXP"": 1200 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.CurrentXp, Is.EqualTo(1200L));
        }

        [Test]
        public void Deserialize_OldNames_RankPreserved()
        {
            string json = @"{ ""Rank"": 7, ""Title"": ""Admiral"", ""NextXP"": 9999, ""CurrentXP"": 4500 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.Rank, Is.EqualTo(7));
        }

        // -------------------------------------------------------------------
        // Deserialization: New field names map correctly
        // Validates: Requirements 4.1, 5.1, 5.2
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_NewNames_RankNameMapsCorrectly()
        {
            string json = @"{ ""Rank"": 5, ""RankName"": ""Captain"", ""XpToNextLevel"": 8000, ""CurrentXp"": 3000 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.RankName, Is.EqualTo("Captain"));
        }

        [Test]
        public void Deserialize_NewNames_XpToNextLevelMapsCorrectly()
        {
            string json = @"{ ""Rank"": 5, ""RankName"": ""Captain"", ""XpToNextLevel"": 8000, ""CurrentXp"": 3000 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.XpToNextLevel, Is.EqualTo(8000L));
        }

        [Test]
        public void Deserialize_NewNames_CurrentXpMapsCorrectly()
        {
            string json = @"{ ""Rank"": 5, ""RankName"": ""Captain"", ""XpToNextLevel"": 8000, ""CurrentXp"": 3000 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.CurrentXp, Is.EqualTo(3000L));
        }

        // -------------------------------------------------------------------
        // Deserialization: New names take precedence when both present
        // Validates: Requirements 4.2, 12.3
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_BothOldAndNew_NewRankNameWins()
        {
            string json = @"{ ""Rank"": 2, ""Title"": ""OldTitle"", ""RankName"": ""NewRankName"", ""NextXP"": 100, ""XpToNextLevel"": 200, ""CurrentXP"": 50, ""CurrentXp"": 75 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.RankName, Is.EqualTo("NewRankName"));
        }

        [Test]
        public void Deserialize_BothOldAndNew_NewXpToNextLevelWins()
        {
            string json = @"{ ""Rank"": 2, ""Title"": ""OldTitle"", ""RankName"": ""NewRankName"", ""NextXP"": 100, ""XpToNextLevel"": 200, ""CurrentXP"": 50, ""CurrentXp"": 75 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.XpToNextLevel, Is.EqualTo(200L));
        }

        [Test]
        public void Deserialize_BothOldAndNew_NewCurrentXpWins()
        {
            string json = @"{ ""Rank"": 2, ""Title"": ""OldTitle"", ""RankName"": ""NewRankName"", ""NextXP"": 100, ""XpToNextLevel"": 200, ""CurrentXP"": 50, ""CurrentXp"": 75 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.CurrentXp, Is.EqualTo(75L));
        }

        // -------------------------------------------------------------------
        // Deserialization: Invalid types default gracefully
        // Validates: Requirements 4.5, 5.3, 12.5
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_TitleIsNumber_DefaultsToEmptyString()
        {
            string json = @"{ ""Rank"": 1, ""Title"": 42 }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.RankName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Deserialize_NextXPIsString_DefaultsToZero()
        {
            string json = @"{ ""Rank"": 1, ""NextXP"": ""notanumber"" }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.XpToNextLevel, Is.EqualTo(0L));
        }

        [Test]
        public void Deserialize_CurrentXPIsString_DefaultsToZero()
        {
            string json = @"{ ""Rank"": 1, ""CurrentXP"": ""invalid"" }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.CurrentXp, Is.EqualTo(0L));
        }

        [Test]
        public void Deserialize_RankIsString_DefaultsToZero()
        {
            string json = @"{ ""Rank"": ""bad"", ""RankName"": ""Test"" }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.Rank, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Deserialization: Missing fields default to 0 / empty
        // Validates: Requirements 4.2, 5.3, 12.4
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_EmptyObject_AllDefaults()
        {
            string json = @"{}";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.Rank, Is.EqualTo(0));
            Assert.That(rank.CurrentXp, Is.EqualTo(0L));
            Assert.That(rank.XpToNextLevel, Is.EqualTo(0L));
            Assert.That(rank.RankName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Deserialize_NullFields_DefaultGracefully()
        {
            string json = @"{ ""Rank"": null, ""RankName"": null, ""CurrentXp"": null, ""XpToNextLevel"": null }";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.Rank, Is.EqualTo(0));
            Assert.That(rank.CurrentXp, Is.EqualTo(0L));
            Assert.That(rank.XpToNextLevel, Is.EqualTo(0L));
            Assert.That(rank.RankName, Is.EqualTo(string.Empty));
        }

        // -------------------------------------------------------------------
        // Serialization: Uses new canonical names only
        // Validates: Requirements 4.4, 5.5, 12.5
        // -------------------------------------------------------------------

        [Test]
        public void Serialize_UsesNewCanonicalNames()
        {
            var rank = new PlayerRank
            {
                Rank = 4,
                RankName = "Lieutenant",
                CurrentXp = 2500,
                XpToNextLevel = 6000
            };

            string json = JsonConvert.SerializeObject(rank);

            Assert.That(json, Does.Contain(@"""Rank"":4"));
            Assert.That(json, Does.Contain(@"""RankName"":""Lieutenant"""));
            Assert.That(json, Does.Contain(@"""CurrentXp"":2500"));
            Assert.That(json, Does.Contain(@"""XpToNextLevel"":6000"));
        }

        [Test]
        public void Serialize_DoesNotContainOldNames()
        {
            var rank = new PlayerRank
            {
                Rank = 4,
                RankName = "Lieutenant",
                CurrentXp = 2500,
                XpToNextLevel = 6000
            };

            string json = JsonConvert.SerializeObject(rank);

            Assert.That(json, Does.Not.Contain(@"""Title"""));
            Assert.That(json, Does.Not.Contain(@"""NextXP"""));
            Assert.That(json, Does.Not.Contain(@"""CurrentXP"""));
        }

        // -------------------------------------------------------------------
        // Round-trip: Serialize then deserialize preserves values
        // Validates: Requirements 4.4, 5.5, 12.5
        // -------------------------------------------------------------------

        [Test]
        public void RoundTrip_ValuesPreserved()
        {
            var original = new PlayerRank
            {
                Rank = 10,
                RankName = "Grand Admiral",
                CurrentXp = 99999,
                XpToNextLevel = 150000
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(deserialized.Rank, Is.EqualTo(original.Rank));
            Assert.That(deserialized.RankName, Is.EqualTo(original.RankName));
            Assert.That(deserialized.CurrentXp, Is.EqualTo(original.CurrentXp));
            Assert.That(deserialized.XpToNextLevel, Is.EqualTo(original.XpToNextLevel));
        }

        [Test]
        public void RoundTrip_DefaultValues_Preserved()
        {
            var original = new PlayerRank();

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(deserialized.Rank, Is.EqualTo(0));
            Assert.That(deserialized.RankName, Is.EqualTo(string.Empty));
            Assert.That(deserialized.CurrentXp, Is.EqualTo(0L));
            Assert.That(deserialized.XpToNextLevel, Is.EqualTo(0L));
        }

        // -------------------------------------------------------------------
        // Edge case: Null token at root level
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_NullToken_ReturnsDefaultPlayerRank()
        {
            string json = @"null";

            var rank = JsonConvert.DeserializeObject<PlayerRank>(json);

            Assert.That(rank.Rank, Is.EqualTo(0));
            Assert.That(rank.RankName, Is.EqualTo(string.Empty));
            Assert.That(rank.CurrentXp, Is.EqualTo(0L));
            Assert.That(rank.XpToNextLevel, Is.EqualTo(0L));
        }
    }
}
