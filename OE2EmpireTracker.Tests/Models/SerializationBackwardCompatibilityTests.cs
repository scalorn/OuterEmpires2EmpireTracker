using System;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class SerializationBackwardCompatibilityTests
    {
        // Feature: player-profile-import, Property 6: Serialization backward compatibility
        /// <summary>
        /// Property 6: For any PlayerProfile serialized to JSON, removing the new fields
        /// (CitizenId, RegistrationDate, ActiveTime) and deserializing back should produce
        /// an object where those fields are string.Empty (not null).
        /// Similarly for PlayerRank.Title.
        /// **Validates: Requirements 9.7**
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerProfile_MissingNewFields_DeserializeToStringEmpty()
        {
            var profileGen = from name in Arb.Generate<NonNull<string>>()
                             from faction in Arb.Generate<NonNull<string>>()
                             from credits in Gen.Choose(0, 999999).Select(x => (decimal)x)
                             from skillPoints in Gen.Choose(0, 1000)
                             select new PlayerProfile
                             {
                                 UUID = Guid.NewGuid().ToString(),
                                 Name = name.Get,
                                 Faction = faction.Get,
                                 TotalCredits = credits,
                                 SkillPoints = skillPoints,
                                 CitizenId = "some-id",
                                 RegistrationDate = "2223-01-06",
                                 ActiveTime = "1Mn 2W"
                             };

            return Prop.ForAll(
                profileGen.ToArbitrary(),
                profile =>
                {
                    var json = JsonConvert.SerializeObject(profile);
                    var jObj = JObject.Parse(json);

                    // Remove the new fields to simulate old save data
                    jObj.Remove("CitizenId");
                    jObj.Remove("RegistrationDate");
                    jObj.Remove("ActiveTime");

                    var deserialized = JsonConvert.DeserializeObject<PlayerProfile>(jObj.ToString());

                    return (deserialized.CitizenId == string.Empty
                            && deserialized.RegistrationDate == string.Empty
                            && deserialized.ActiveTime == string.Empty
                            && deserialized.CitizenId != null
                            && deserialized.RegistrationDate != null
                            && deserialized.ActiveTime != null)
                        .Label("New PlayerProfile fields should default to string.Empty when missing from JSON");
                });
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property PlayerRank_MissingTitleField_DeserializesToStringEmpty()
        {
            var rankGen = from rank in Gen.Choose(0, 100)
                          from currentXP in Gen.Choose(0, 999999).Select(x => (long)x)
                          from nextXP in Gen.Choose(0, 999999).Select(x => (long)x)
                          select new PlayerRank
                          {
                              Rank = rank,
                              CurrentXP = currentXP,
                              NextXP = nextXP,
                              Title = "Some Title"
                          };

            return Prop.ForAll(
                rankGen.ToArbitrary(),
                rank =>
                {
                    var json = JsonConvert.SerializeObject(rank);
                    var jObj = JObject.Parse(json);

                    // Remove the new field to simulate old save data
                    jObj.Remove("Title");

                    var deserialized = JsonConvert.DeserializeObject<PlayerRank>(jObj.ToString());

                    return (deserialized.Title == string.Empty
                            && deserialized.Title != null)
                        .Label("PlayerRank.Title should default to string.Empty when missing from JSON");
                });
        }
    }
}
