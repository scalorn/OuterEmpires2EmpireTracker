using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    /// <summary>
    /// Backward compatibility tests for PlayerData.json loading.
    /// Validates that existing save files with old field names, missing new fields,
    /// or mixed old/new fields deserialize correctly into the enriched PlayerProfile model.
    /// Validates: Requirements 1.4, 2.5, 3.6, 4.2, 5.3, 6.4, 7.5, 12.1, 12.4
    /// </summary>
    [TestFixture]
    public class BackwardCompatibilityTests
    {
        // -------------------------------------------------------------------
        // Test 1: Old PlayerRank field names map to new properties
        // Validates: Requirements 4.2, 5.3, 12.2
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_OldRankFieldNames_MapsToNewProperties()
        {
            string json = @"{
                ""UUID"": ""player-1"",
                ""Name"": ""TestPlayer"",
                ""Faction"": ""TestFaction"",
                ""FactionUUID"": ""faction-1"",
                ""TotalCredits"": 1000,
                ""Public"": { ""Rank"": 5, ""Title"": ""Commander"", ""NextXP"": 8000, ""CurrentXP"": 3500 },
                ""Private"": { ""Rank"": 3, ""Title"": ""Trader"", ""NextXP"": 4000, ""CurrentXP"": 1200 },
                ""Military"": { ""Rank"": 7, ""Title"": ""Admiral"", ""NextXP"": 15000, ""CurrentXP"": 9000 },
                ""SkillPoints"": 10,
                ""Skills"": {}
            }";

            var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.That(profile.Public.RankName, Is.EqualTo("Commander"));
            Assert.That(profile.Public.XpToNextLevel, Is.EqualTo(8000L));
            Assert.That(profile.Public.CurrentXp, Is.EqualTo(3500L));
            Assert.That(profile.Public.Rank, Is.EqualTo(5));

            Assert.That(profile.Private.RankName, Is.EqualTo("Trader"));
            Assert.That(profile.Private.XpToNextLevel, Is.EqualTo(4000L));
            Assert.That(profile.Private.CurrentXp, Is.EqualTo(1200L));

            Assert.That(profile.Military.RankName, Is.EqualTo("Admiral"));
            Assert.That(profile.Military.XpToNextLevel, Is.EqualTo(15000L));
            Assert.That(profile.Military.CurrentXp, Is.EqualTo(9000L));
        }

        // -------------------------------------------------------------------
        // Test 2: Both old and new PlayerRank field names — new wins
        // Validates: Requirements 4.2, 12.3
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_BothOldAndNewRankFieldNames_NewNamesWin()
        {
            string json = @"{
                ""UUID"": ""player-2"",
                ""Name"": ""MixedPlayer"",
                ""Faction"": ""TestFaction"",
                ""FactionUUID"": ""faction-2"",
                ""TotalCredits"": 500,
                ""Public"": {
                    ""Rank"": 4,
                    ""Title"": ""OldPublicTitle"",
                    ""RankName"": ""NewPublicRank"",
                    ""NextXP"": 1000,
                    ""XpToNextLevel"": 2000,
                    ""CurrentXP"": 300,
                    ""CurrentXp"": 600
                },
                ""Private"": {
                    ""Rank"": 2,
                    ""Title"": ""OldPrivateTitle"",
                    ""RankName"": ""NewPrivateRank"",
                    ""NextXP"": 500,
                    ""XpToNextLevel"": 900,
                    ""CurrentXP"": 100,
                    ""CurrentXp"": 250
                },
                ""Military"": {
                    ""Rank"": 6,
                    ""Title"": ""OldMilitaryTitle"",
                    ""RankName"": ""NewMilitaryRank"",
                    ""NextXP"": 7000,
                    ""XpToNextLevel"": 12000,
                    ""CurrentXP"": 4000,
                    ""CurrentXp"": 8500
                },
                ""SkillPoints"": 5,
                ""Skills"": {}
            }";

            var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);

            Assert.That(profile.Public.RankName, Is.EqualTo("NewPublicRank"));
            Assert.That(profile.Public.XpToNextLevel, Is.EqualTo(2000L));
            Assert.That(profile.Public.CurrentXp, Is.EqualTo(600L));

            Assert.That(profile.Private.RankName, Is.EqualTo("NewPrivateRank"));
            Assert.That(profile.Private.XpToNextLevel, Is.EqualTo(900L));
            Assert.That(profile.Private.CurrentXp, Is.EqualTo(250L));

            Assert.That(profile.Military.RankName, Is.EqualTo("NewMilitaryRank"));
            Assert.That(profile.Military.XpToNextLevel, Is.EqualTo(12000L));
            Assert.That(profile.Military.CurrentXp, Is.EqualTo(8500L));
        }

        // -------------------------------------------------------------------
        // Test 3: Missing all new fields — defaults applied
        // Validates: Requirements 1.4, 2.5, 3.6, 6.4, 7.5, 12.1, 12.4
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_MissingAllNewFields_DefaultsApplied()
        {
            // Simulates a pre-enrichment PlayerData.json — no CharacterId, FirstName,
            // LastName, ActiveTimeMinutes, and skills lack metadata/training fields
            string json = @"{
                ""UUID"": ""player-3"",
                ""Name"": ""OldFormatPlayer"",
                ""Faction"": ""OldFaction"",
                ""FactionUUID"": ""faction-3"",
                ""TotalCredits"": 2500,
                ""Public"": { ""Rank"": 2, ""Title"": ""Ensign"", ""NextXP"": 3000, ""CurrentXP"": 1000 },
                ""Private"": { ""Rank"": 1, ""Title"": ""Novice"", ""NextXP"": 1500, ""CurrentXP"": 500 },
                ""Military"": { ""Rank"": 0, ""Title"": """", ""NextXP"": 0, ""CurrentXP"": 0 },
                ""SkillPoints"": 3,
                ""CitizenId"": ""CIT-001"",
                ""RegistrationDate"": ""2223-01-01"",
                ""ActiveTime"": ""2W 3D"",
                ""Skills"": {
                    ""Human Resources"": { ""Level"": 5, ""TrainingStarted"": false },
                    ""Foreman"": { ""Level"": 3, ""TrainingStarted"": true }
                }
            }";

            var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);

            // New PlayerProfile fields default to 0 / empty string
            Assert.That(profile.CharacterId, Is.EqualTo(0));
            Assert.That(profile.FirstName, Is.EqualTo(string.Empty));
            Assert.That(profile.LastName, Is.EqualTo(string.Empty));
            Assert.That(profile.ActiveTimeMinutes, Is.EqualTo(0));

            // Existing fields still load correctly
            Assert.That(profile.UUID, Is.EqualTo("player-3"));
            Assert.That(profile.Name, Is.EqualTo("OldFormatPlayer"));
            Assert.That(profile.CitizenId, Is.EqualTo("CIT-001"));
            Assert.That(profile.ActiveTime, Is.EqualTo("2W 3D"));

            // PlayerRank old names still map correctly
            Assert.That(profile.Public.RankName, Is.EqualTo("Ensign"));
            Assert.That(profile.Public.XpToNextLevel, Is.EqualTo(3000L));
            Assert.That(profile.Public.CurrentXp, Is.EqualTo(1000L));

            // Skills: metadata and training progress fields default
            var hrSkill = profile.Skills["Human Resources"];
            Assert.That(hrSkill.Level, Is.EqualTo(5));
            Assert.That(hrSkill.SkillId, Is.EqualTo(0));
            Assert.That(hrSkill.EffectDescription, Is.EqualTo(string.Empty));
            Assert.That(hrSkill.AmountPerLevel, Is.EqualTo(0));
            Assert.That(hrSkill.SkillGroupName, Is.EqualTo(string.Empty));
            Assert.That(hrSkill.IsUnlocked, Is.False);
            Assert.That(hrSkill.TargetLevel, Is.EqualTo(0));
            Assert.That(hrSkill.TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(hrSkill.RemainingMinutes, Is.EqualTo(0));

            var foremanSkill = profile.Skills["Foreman"];
            Assert.That(foremanSkill.Level, Is.EqualTo(3));
            Assert.That(foremanSkill.TrainingStarted, Is.True);
            Assert.That(foremanSkill.SkillId, Is.EqualTo(0));
            Assert.That(foremanSkill.EffectDescription, Is.EqualTo(string.Empty));
            Assert.That(foremanSkill.AmountPerLevel, Is.EqualTo(0));
            Assert.That(foremanSkill.SkillGroupName, Is.EqualTo(string.Empty));
            Assert.That(foremanSkill.IsUnlocked, Is.False);
            Assert.That(foremanSkill.TargetLevel, Is.EqualTo(0));
            Assert.That(foremanSkill.TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(foremanSkill.RemainingMinutes, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Test 4: All new fields present — load correctly
        // Validates: Requirements 1.4, 2.5, 3.6, 6.4, 7.5, 12.4
        // -------------------------------------------------------------------

        [Test]
        public void Deserialize_AllNewFieldsPresent_LoadsCorrectly()
        {
            string json = @"{
                ""UUID"": ""player-4"",
                ""Name"": ""FullPlayer"",
                ""Faction"": ""FullFaction"",
                ""FactionUUID"": ""faction-4"",
                ""TotalCredits"": 50000,
                ""Public"": { ""Rank"": 10, ""RankName"": ""Grand Admiral"", ""XpToNextLevel"": 100000, ""CurrentXp"": 85000 },
                ""Private"": { ""Rank"": 8, ""RankName"": ""Magnate"", ""XpToNextLevel"": 60000, ""CurrentXp"": 45000 },
                ""Military"": { ""Rank"": 6, ""RankName"": ""General"", ""XpToNextLevel"": 40000, ""CurrentXp"": 30000 },
                ""SkillPoints"": 25,
                ""CharacterId"": 12345,
                ""FirstName"": ""John"",
                ""LastName"": ""Doe"",
                ""ActiveTimeMinutes"": 9876,
                ""CitizenId"": ""CIT-999"",
                ""RegistrationDate"": ""2224-06-15"",
                ""ActiveTime"": ""6Mn 3W"",
                ""Skills"": {
                    ""Human Resources"": {
                        ""Level"": 8,
                        ""TrainingStarted"": false,
                        ""SkillId"": 101,
                        ""EffectDescription"": ""Increases worker capacity"",
                        ""AmountPerLevel"": 5,
                        ""SkillGroupName"": ""Colony Director"",
                        ""IsUnlocked"": true,
                        ""TargetLevel"": 0,
                        ""TrainingPercentageComplete"": 0,
                        ""RemainingMinutes"": 0
                    },
                    ""Foreman"": {
                        ""Level"": 4,
                        ""TrainingStarted"": true,
                        ""SkillId"": 102,
                        ""EffectDescription"": ""Boosts production speed"",
                        ""AmountPerLevel"": 3,
                        ""SkillGroupName"": ""Colony Operations"",
                        ""IsUnlocked"": true,
                        ""TargetLevel"": 5,
                        ""TrainingPercentageComplete"": 67,
                        ""RemainingMinutes"": 1440
                    }
                }
            }";

            var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);

            // New PlayerProfile scalar fields
            Assert.That(profile.CharacterId, Is.EqualTo(12345));
            Assert.That(profile.FirstName, Is.EqualTo("John"));
            Assert.That(profile.LastName, Is.EqualTo("Doe"));
            Assert.That(profile.ActiveTimeMinutes, Is.EqualTo(9876));

            // Existing fields
            Assert.That(profile.UUID, Is.EqualTo("player-4"));
            Assert.That(profile.Name, Is.EqualTo("FullPlayer"));
            Assert.That(profile.CitizenId, Is.EqualTo("CIT-999"));

            // Ranks with new canonical names
            Assert.That(profile.Public.Rank, Is.EqualTo(10));
            Assert.That(profile.Public.RankName, Is.EqualTo("Grand Admiral"));
            Assert.That(profile.Public.XpToNextLevel, Is.EqualTo(100000L));
            Assert.That(profile.Public.CurrentXp, Is.EqualTo(85000L));

            Assert.That(profile.Private.RankName, Is.EqualTo("Magnate"));
            Assert.That(profile.Military.RankName, Is.EqualTo("General"));

            // Skills with all metadata and training progress
            var hrSkill = profile.Skills["Human Resources"];
            Assert.That(hrSkill.Level, Is.EqualTo(8));
            Assert.That(hrSkill.SkillId, Is.EqualTo(101));
            Assert.That(hrSkill.EffectDescription, Is.EqualTo("Increases worker capacity"));
            Assert.That(hrSkill.AmountPerLevel, Is.EqualTo(5));
            Assert.That(hrSkill.SkillGroupName, Is.EqualTo("Colony Director"));
            Assert.That(hrSkill.IsUnlocked, Is.True);
            Assert.That(hrSkill.TargetLevel, Is.EqualTo(0));
            Assert.That(hrSkill.TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(hrSkill.RemainingMinutes, Is.EqualTo(0));

            var foremanSkill = profile.Skills["Foreman"];
            Assert.That(foremanSkill.Level, Is.EqualTo(4));
            Assert.That(foremanSkill.SkillId, Is.EqualTo(102));
            Assert.That(foremanSkill.EffectDescription, Is.EqualTo("Boosts production speed"));
            Assert.That(foremanSkill.AmountPerLevel, Is.EqualTo(3));
            Assert.That(foremanSkill.SkillGroupName, Is.EqualTo("Colony Operations"));
            Assert.That(foremanSkill.IsUnlocked, Is.True);
            Assert.That(foremanSkill.TargetLevel, Is.EqualTo(5));
            Assert.That(foremanSkill.TrainingPercentageComplete, Is.EqualTo(67));
            Assert.That(foremanSkill.RemainingMinutes, Is.EqualTo(1440));
        }

        // -------------------------------------------------------------------
        // Test 5: Round-trip — serialize full PlayerProfile, deserialize, verify all fields
        // Validates: Requirements 4.4, 5.5, 12.5
        // -------------------------------------------------------------------

        [Test]
        public void RoundTrip_FullPlayerProfile_AllFieldsPreserved()
        {
            var original = new PlayerProfile
            {
                UUID = "player-rt",
                Name = "RoundTripPlayer",
                Faction = "RTFaction",
                FactionUUID = "faction-rt",
                TotalCredits = 77777m,
                SkillPoints = 15,
                CitizenId = "CIT-RT",
                RegistrationDate = "2224-03-20",
                ActiveTime = "4Mn 1W",
                CharacterId = 54321,
                FirstName = "Jane",
                LastName = "Smith",
                ActiveTimeMinutes = 5432,
                Public = new PlayerRank
                {
                    Rank = 9,
                    RankName = "Fleet Admiral",
                    CurrentXp = 72000,
                    XpToNextLevel = 90000
                },
                Private = new PlayerRank
                {
                    Rank = 6,
                    RankName = "Tycoon",
                    CurrentXp = 35000,
                    XpToNextLevel = 50000
                },
                Military = new PlayerRank
                {
                    Rank = 4,
                    RankName = "Colonel",
                    CurrentXp = 18000,
                    XpToNextLevel = 25000
                }
            };

            original.Skills["Human Resources"] = new PlayerSkill
            {
                Level = 7,
                TrainingStarted = false,
                SkillId = 201,
                EffectDescription = "More workers",
                AmountPerLevel = 4,
                SkillGroupName = "Colony Director",
                IsUnlocked = true,
                TargetLevel = 0,
                TrainingPercentageComplete = 0,
                RemainingMinutes = 0
            };

            original.Skills["Foreman"] = new PlayerSkill
            {
                Level = 5,
                TrainingStarted = true,
                SkillId = 202,
                EffectDescription = "Faster builds",
                AmountPerLevel = 2,
                SkillGroupName = "Colony Operations",
                IsUnlocked = true,
                TargetLevel = 6,
                TrainingPercentageComplete = 45,
                RemainingMinutes = 720
            };

            string json = JsonConvert.SerializeObject(original, Formatting.Indented);
            var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(json);

            // PlayerProfile scalar fields
            Assert.That(deserialized.UUID, Is.EqualTo(original.UUID));
            Assert.That(deserialized.Name, Is.EqualTo(original.Name));
            Assert.That(deserialized.Faction, Is.EqualTo(original.Faction));
            Assert.That(deserialized.FactionUUID, Is.EqualTo(original.FactionUUID));
            Assert.That(deserialized.TotalCredits, Is.EqualTo(original.TotalCredits));
            Assert.That(deserialized.SkillPoints, Is.EqualTo(original.SkillPoints));
            Assert.That(deserialized.CitizenId, Is.EqualTo(original.CitizenId));
            Assert.That(deserialized.RegistrationDate, Is.EqualTo(original.RegistrationDate));
            Assert.That(deserialized.ActiveTime, Is.EqualTo(original.ActiveTime));
            Assert.That(deserialized.CharacterId, Is.EqualTo(original.CharacterId));
            Assert.That(deserialized.FirstName, Is.EqualTo(original.FirstName));
            Assert.That(deserialized.LastName, Is.EqualTo(original.LastName));
            Assert.That(deserialized.ActiveTimeMinutes, Is.EqualTo(original.ActiveTimeMinutes));

            // PlayerRank fields (serialized with new canonical names)
            Assert.That(deserialized.Public.Rank, Is.EqualTo(original.Public.Rank));
            Assert.That(deserialized.Public.RankName, Is.EqualTo(original.Public.RankName));
            Assert.That(deserialized.Public.CurrentXp, Is.EqualTo(original.Public.CurrentXp));
            Assert.That(deserialized.Public.XpToNextLevel, Is.EqualTo(original.Public.XpToNextLevel));

            Assert.That(deserialized.Private.Rank, Is.EqualTo(original.Private.Rank));
            Assert.That(deserialized.Private.RankName, Is.EqualTo(original.Private.RankName));
            Assert.That(deserialized.Private.CurrentXp, Is.EqualTo(original.Private.CurrentXp));
            Assert.That(deserialized.Private.XpToNextLevel, Is.EqualTo(original.Private.XpToNextLevel));

            Assert.That(deserialized.Military.Rank, Is.EqualTo(original.Military.Rank));
            Assert.That(deserialized.Military.RankName, Is.EqualTo(original.Military.RankName));
            Assert.That(deserialized.Military.CurrentXp, Is.EqualTo(original.Military.CurrentXp));
            Assert.That(deserialized.Military.XpToNextLevel, Is.EqualTo(original.Military.XpToNextLevel));

            // Skills preserved
            Assert.That(deserialized.Skills.Count, Is.EqualTo(2));

            var hrSkill = deserialized.Skills["Human Resources"];
            Assert.That(hrSkill.Level, Is.EqualTo(7));
            Assert.That(hrSkill.SkillId, Is.EqualTo(201));
            Assert.That(hrSkill.EffectDescription, Is.EqualTo("More workers"));
            Assert.That(hrSkill.AmountPerLevel, Is.EqualTo(4));
            Assert.That(hrSkill.SkillGroupName, Is.EqualTo("Colony Director"));
            Assert.That(hrSkill.IsUnlocked, Is.True);
            Assert.That(hrSkill.TargetLevel, Is.EqualTo(0));
            Assert.That(hrSkill.TrainingPercentageComplete, Is.EqualTo(0));
            Assert.That(hrSkill.RemainingMinutes, Is.EqualTo(0));

            var foremanSkill = deserialized.Skills["Foreman"];
            Assert.That(foremanSkill.Level, Is.EqualTo(5));
            Assert.That(foremanSkill.TrainingStarted, Is.True);
            Assert.That(foremanSkill.SkillId, Is.EqualTo(202));
            Assert.That(foremanSkill.EffectDescription, Is.EqualTo("Faster builds"));
            Assert.That(foremanSkill.AmountPerLevel, Is.EqualTo(2));
            Assert.That(foremanSkill.SkillGroupName, Is.EqualTo("Colony Operations"));
            Assert.That(foremanSkill.IsUnlocked, Is.True);
            Assert.That(foremanSkill.TargetLevel, Is.EqualTo(6));
            Assert.That(foremanSkill.TrainingPercentageComplete, Is.EqualTo(45));
            Assert.That(foremanSkill.RemainingMinutes, Is.EqualTo(720));
        }
    }
}
