using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class SurveyImportHelperPropertyTests
    {
        #region Helpers

        private static Survey MakeSurvey(string uuid, string planetName, string surveyId,
            string systemName = "System", string scannedBy = "Scanner", string dateTime = "2025-01-01",
            string nickName = "")
        {
            var survey = new Survey();
            survey.UUID = uuid;
            survey.OwnerUUID = "owner-" + uuid;
            survey.PlanetName = planetName;
            survey.SurveyID = surveyId;
            survey.SystemName = systemName;
            survey.ScannedBy = scannedBy;
            survey.DateTime = dateTime;
            survey.NickName = nickName;
            return survey;
        }

        private static string ShuffleCase(string input, int seed)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var rng = new System.Random(seed);
            var chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = rng.Next(2) == 0 ? char.ToUpper(chars[i]) : char.ToLower(chars[i]);
            }
            return new string(chars);
        }

        private static Survey SetSurveyType(Survey survey, bool isAsteroid)
        {
            if (isAsteroid)
            {
                survey.SurveyType = SurveyType.Asteroid;
                survey.AsteroidUUID = "ast-" + survey.UUID;
            }
            return survey;
        }

        private static Gen<string> NonEmptyStringGen()
        {
            return Arb.Default.NonEmptyString().Generator.Select(s => s.Get);
        }

        private static Gen<Survey> SurveyGen()
        {
            return from uuid in NonEmptyStringGen()
                   from planetName in NonEmptyStringGen()
                   from surveyId in NonEmptyStringGen()
                   from systemName in NonEmptyStringGen()
                   from scannedBy in NonEmptyStringGen()
                   from dateTime in NonEmptyStringGen()
                   from nickName in NonEmptyStringGen()
                   from isAsteroid in Arb.Default.Bool().Generator
                   let survey = MakeSurvey(uuid, planetName, surveyId, systemName, scannedBy, dateTime, nickName)
                   select SetSurveyType(survey, isAsteroid);
        }

        #endregion

        #region Property 1: FindByKey returns correct match by PlanetName+SurveyID (case-insensitive)

        /// <summary>
        /// Property 1: Case-insensitive PlanetName+SurveyID search.
        /// For any list of surveys and for any PlanetName+SurveyID pair that exists in the list
        /// (under any case variation), FindByKey shall return a survey whose PlanetName and SurveyID
        /// equal the search values under case-insensitive comparison. For any pair that does not
        /// exist, FindByKey shall return null.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property FindByKeyReturnsCaseInsensitiveMatch()
        {
            var gen = from surveys in Gen.ListOf(SurveyGen())
                      from seed in Gen.Choose(0, 10000)
                      from extraPlanet in NonEmptyStringGen()
                      from extraId in NonEmptyStringGen()
                      select new { Surveys = surveys.ToList(), Seed = seed, ExtraPlanet = extraPlanet, ExtraId = extraId };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Test match case: pick an existing survey and shuffle case of its key fields
                if (data.Surveys.Count > 0)
                {
                    var target = data.Surveys[data.Seed % data.Surveys.Count];
                    var shuffledPlanet = ShuffleCase(target.PlanetName, data.Seed);
                    var shuffledId = ShuffleCase(target.SurveyID, data.Seed + 1);
                    var found = SurveyImportHelper.FindByKey(data.Surveys, shuffledPlanet, shuffledId);

                    if (found == null)
                        return false.Label($"Expected to find survey '{target.PlanetName}/{target.SurveyID}' with search '{shuffledPlanet}/{shuffledId}' but got null");

                    if (!string.Equals(found.PlanetName, shuffledPlanet, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(found.SurveyID, shuffledId, StringComparison.OrdinalIgnoreCase))
                        return false.Label($"Found survey does not match search key case-insensitively");
                }

                // Test no-match case: use a key guaranteed not in the list
                var uniquePlanet = data.ExtraPlanet + "_NOTINLIST_" + Guid.NewGuid().ToString("N");
                var uniqueId = data.ExtraId + "_NOTINLIST_" + Guid.NewGuid().ToString("N");
                var noMatch = SurveyImportHelper.FindByKey(data.Surveys, uniquePlanet, uniqueId);

                return (noMatch == null)
                    .Label($"Expected null for non-existent key '{uniquePlanet}/{uniqueId}' but got a survey");
            });
        }

        #endregion

        #region Property 2: CreateFromTemp produces a valid survey with all parsed data

        /// <summary>
        /// Property 2: CreateFromTemp produces a valid survey with all parsed data.
        /// For any temporary survey and any owner UUID, CreateFromTemp shall return a survey
        /// where UUID is non-empty, OwnerUUID matches, and all data fields match the temp survey.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property CreateFromTempProducesValidSurvey()
        {
            var gen = from temp in SurveyGen()
                      from ownerUuid in NonEmptyStringGen()
                      from resourceCount in Gen.Choose(0, 5)
                      select new { Temp = temp, OwnerUuid = ownerUuid, ResourceCount = resourceCount };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Add resources to the temp survey
                for (int i = 0; i < data.ResourceCount; i++)
                {
                    data.Temp.Resources["Resource" + i] = new SurveyResource("Resource" + i, "High", (10 + i).ToString());
                }
                data.Temp.Properties["TestProp"] = "TestValue";

                var result = SurveyImportHelper.CreateFromTemp(data.Temp, data.OwnerUuid);

                var uuidNonEmpty = (!string.IsNullOrEmpty(result.UUID))
                    .Label("UUID should be non-null and non-empty");
                var ownerMatch = (result.OwnerUUID == data.OwnerUuid)
                    .Label($"OwnerUUID: expected '{data.OwnerUuid}', got '{result.OwnerUUID}'");
                var planetMatch = (result.PlanetName == data.Temp.PlanetName)
                    .Label($"PlanetName: expected '{data.Temp.PlanetName}', got '{result.PlanetName}'");
                var systemMatch = (result.SystemName == data.Temp.SystemName)
                    .Label($"SystemName: expected '{data.Temp.SystemName}', got '{result.SystemName}'");
                var surveyIdMatch = (result.SurveyID == data.Temp.SurveyID)
                    .Label($"SurveyID: expected '{data.Temp.SurveyID}', got '{result.SurveyID}'");
                var scannedByMatch = (result.ScannedBy == data.Temp.ScannedBy)
                    .Label($"ScannedBy: expected '{data.Temp.ScannedBy}', got '{result.ScannedBy}'");
                var dateTimeMatch = (result.DateTime == data.Temp.DateTime)
                    .Label($"DateTime: expected '{data.Temp.DateTime}', got '{result.DateTime}'");
                var scannerMatch = (result.ScannerBlueprintUUID == data.Temp.ScannerBlueprintUUID)
                    .Label($"ScannerBlueprintUUID mismatch");
                var resourceMatch = (result.Resources.Count == data.Temp.Resources.Count)
                    .Label($"Resources.Count: expected {data.Temp.Resources.Count}, got {result.Resources.Count}");
                var propsMatch = (result.Properties.Count == data.Temp.Properties.Count)
                    .Label($"Properties.Count: expected {data.Temp.Properties.Count}, got {result.Properties.Count}");
                var surveyTypeMatch = (result.SurveyType == data.Temp.SurveyType)
                    .Label($"SurveyType: expected '{data.Temp.SurveyType}', got '{result.SurveyType}'");

                return uuidNonEmpty
                    .And(ownerMatch)
                    .And(planetMatch)
                    .And(systemMatch)
                    .And(surveyIdMatch)
                    .And(scannedByMatch)
                    .And(dateTimeMatch)
                    .And(scannerMatch)
                    .And(resourceMatch)
                    .And(propsMatch)
                    .And(surveyTypeMatch);
            });
        }

        #endregion

        #region Property 3: MergeData updates data while preserving UUID, OwnerUUID, NickName

        /// <summary>
        /// Property 3: MergeData updates data while preserving UUID, OwnerUUID, NickName.
        /// For any existing survey (with UUID, OwnerUUID, NickName) and any temp survey,
        /// after MergeData the data fields are updated but UUID, OwnerUUID, and NickName
        /// remain unchanged.
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public Property MergeDataUpdatesFieldsPreservesIdentity()
        {
            var gen = from existing in SurveyGen()
                      from source in SurveyGen()
                      select new { Existing = existing, Source = source };

            return Prop.ForAll(gen.ToArbitrary(), data =>
            {
                // Add some resources/properties to source
                data.Source.Resources["Iron"] = new SurveyResource("Iron", "High", "42");
                data.Source.Properties["TestKey"] = "TestVal";

                // Capture identity before merge
                var originalUuid = data.Existing.UUID;
                var originalOwnerUuid = data.Existing.OwnerUUID;
                var originalNickName = data.Existing.NickName;

                SurveyImportHelper.MergeData(data.Existing, data.Source);

                // Data fields should be updated
                var planetMatch = (data.Existing.PlanetName == data.Source.PlanetName)
                    .Label($"PlanetName: expected '{data.Source.PlanetName}', got '{data.Existing.PlanetName}'");
                var systemMatch = (data.Existing.SystemName == data.Source.SystemName)
                    .Label($"SystemName mismatch");
                var surveyIdMatch = (data.Existing.SurveyID == data.Source.SurveyID)
                    .Label($"SurveyID mismatch");
                var scannedByMatch = (data.Existing.ScannedBy == data.Source.ScannedBy)
                    .Label($"ScannedBy mismatch");
                var dateTimeMatch = (data.Existing.DateTime == data.Source.DateTime)
                    .Label($"DateTime mismatch");
                var scannerMatch = (data.Existing.ScannerBlueprintUUID == data.Source.ScannerBlueprintUUID)
                    .Label($"ScannerBlueprintUUID mismatch");
                var resourceMatch = (ReferenceEquals(data.Existing.Resources, data.Source.Resources))
                    .Label("Resources reference should be updated from source");
                var propsMatch = (ReferenceEquals(data.Existing.Properties, data.Source.Properties))
                    .Label("Properties reference should be updated from source");
                var surveyTypeMatch = (data.Existing.SurveyType == data.Source.SurveyType)
                    .Label($"SurveyType: expected '{data.Source.SurveyType}', got '{data.Existing.SurveyType}'");

                // Identity fields should be preserved
                var uuidPreserved = (data.Existing.UUID == originalUuid)
                    .Label($"UUID changed from '{originalUuid}' to '{data.Existing.UUID}'");
                var ownerPreserved = (data.Existing.OwnerUUID == originalOwnerUuid)
                    .Label($"OwnerUUID changed from '{originalOwnerUuid}' to '{data.Existing.OwnerUUID}'");
                var nickNamePreserved = (data.Existing.NickName == originalNickName)
                    .Label($"NickName changed from '{originalNickName}' to '{data.Existing.NickName}'");

                return planetMatch
                    .And(systemMatch)
                    .And(surveyIdMatch)
                    .And(scannedByMatch)
                    .And(dateTimeMatch)
                    .And(scannerMatch)
                    .And(resourceMatch)
                    .And(propsMatch)
                    .And(surveyTypeMatch)
                    .And(uuidPreserved)
                    .And(ownerPreserved)
                    .And(nickNamePreserved);
            });
        }

        #endregion
    }
}
