using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Property-based tests for SurveyService.
    /// Feature: bl-110-survey-readonly, Properties 4, 5, 6, 7, 8 from the design document.
    /// </summary>
    [TestFixture]
    public class SurveyServicePropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random Survey with all fields populated
        // (reuses the same pattern as SurveyViewModelPropertyTests)
        // -----------------------------------------------------------------------

        private static readonly string[] SampleResourceNames = new[]
        {
            "Alkali Metals", "Noble Gases", "Halogens", "Metallics",
            "S1. Translanthanic Exotics", "S2. Element 126",
        };

        private static readonly string[] Purities = new[]
        {
            GameConstants.PurityHigh,
            GameConstants.PurityMedium,
            GameConstants.PurityLow,
            GameConstants.PurityRefined,
        };

        private static readonly string[] PropertyKeys = new[]
        {
            "SensorAbundance", "PurityModifier", "ScanLevel",
        };

        private static Gen<string> SafeStringGen()
        {
            return Arb.Generate<NonEmptyString>().Select(s => s.Get);
        }

        private static Gen<string> NumericAmountGen()
        {
            return Gen.Choose(1, 9999).Select(i => i.ToString());
        }

        private static Gen<Survey> ValidSurveyGen()
        {
            return from planetName in SafeStringGen()
                   from systemName in SafeStringGen()
                   from surveyID in SafeStringGen()
                   from nickName in SafeStringGen()
                   from scannedBy in SafeStringGen()
                   from dateTime in SafeStringGen()
                   from scannerBlueprintUUID in SafeStringGen()
                   from asteroidUUID in SafeStringGen()
                   from surveyTypeInt in Gen.Choose(0, 1)
                   from resourceCount in Gen.Choose(0, 5)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from purities in Gen.ListOf(resourceCount, Gen.Elements(Purities))
                   from amounts in Gen.ListOf(resourceCount, NumericAmountGen())
                   from propCount in Gen.Choose(0, 3)
                   from selectedProps in Gen.Shuffle(PropertyKeys).Select(a => a.Take(propCount))
                   from propValues in Gen.ListOf(propCount, SafeStringGen())
                   select BuildSurvey(
                       planetName,
                       systemName,
                       surveyID,
                       nickName,
                       scannedBy,
                       dateTime,
                       scannerBlueprintUUID,
                       asteroidUUID,
                       (SurveyType)surveyTypeInt,
                       selectedResources.ToArray(),
                       purities.ToArray(),
                       amounts.ToArray(),
                       selectedProps.ToArray(),
                       propValues.ToArray());
        }

        private static Survey BuildSurvey(
            string planetName,
            string systemName,
            string surveyID,
            string nickName,
            string scannedBy,
            string dateTime,
            string scannerBlueprintUUID,
            string asteroidUUID,
            SurveyType surveyType,
            string[] resources,
            string[] purities,
            string[] amounts,
            string[] propKeys,
            string[] propValues)
        {
            var survey = new Survey
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = "test-player-uuid",
                PlanetName = planetName,
                SystemName = systemName,
                SurveyID = surveyID,
                NickName = nickName,
                ScannedBy = scannedBy,
                DateTime = dateTime,
                ScannerBlueprintUUID = scannerBlueprintUUID,
                AsteroidUUID = asteroidUUID,
                SurveyType = surveyType,
            };

            for (int i = 0; i < resources.Length; i++)
            {
                var key = resources[i];
                survey.Resources[key] = new SurveyResource(
                    resources[i],
                    purities[i],
                    amounts[i]);
            }

            for (int i = 0; i < propKeys.Length && i < propValues.Length; i++)
            {
                survey.Properties[propKeys[i]] = propValues[i];
            }

            return survey;
        }

        private static Gen<SurveyUpdateRequest> UpdateRequestGen()
        {
            return from planetName in SafeStringGen()
                   from systemName in SafeStringGen()
                   from surveyID in SafeStringGen()
                   from nickName in SafeStringGen()
                   from scannedBy in SafeStringGen()
                   from dateTime in SafeStringGen()
                   from scannerBlueprintUUID in SafeStringGen()
                   from asteroidUUID in SafeStringGen()
                   from surveyTypeInt in Gen.Choose(0, 1)
                   from resourceCount in Gen.Choose(0, 5)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from purities in Gen.ListOf(resourceCount, Gen.Elements(Purities))
                   from amounts in Gen.ListOf(resourceCount, NumericAmountGen())
                   from propCount in Gen.Choose(0, 3)
                   from selectedProps in Gen.Shuffle(PropertyKeys).Select(a => a.Take(propCount))
                   from propValues in Gen.ListOf(propCount, SafeStringGen())
                   select BuildUpdateRequest(
                       planetName,
                       systemName,
                       surveyID,
                       nickName,
                       scannedBy,
                       dateTime,
                       scannerBlueprintUUID,
                       asteroidUUID,
                       (SurveyType)surveyTypeInt,
                       selectedResources.ToArray(),
                       purities.ToArray(),
                       amounts.ToArray(),
                       selectedProps.ToArray(),
                       propValues.ToArray());
        }

        private static SurveyUpdateRequest BuildUpdateRequest(
            string planetName,
            string systemName,
            string surveyID,
            string nickName,
            string scannedBy,
            string dateTime,
            string scannerBlueprintUUID,
            string asteroidUUID,
            SurveyType surveyType,
            string[] resources,
            string[] purities,
            string[] amounts,
            string[] propKeys,
            string[] propValues)
        {
            var req = new SurveyUpdateRequest
            {
                PlanetName = planetName,
                SystemName = systemName,
                SurveyID = surveyID,
                NickName = nickName,
                ScannedBy = scannedBy,
                DateTime = dateTime,
                ScannerBlueprintUUID = scannerBlueprintUUID,
                AsteroidUUID = asteroidUUID,
                SurveyType = surveyType,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };

            for (int i = 0; i < resources.Length; i++)
            {
                req.Resources[resources[i]] = new SurveyResource(resources[i], purities[i], amounts[i]);
            }

            for (int i = 0; i < propKeys.Length && i < propValues.Length; i++)
            {
                req.Properties[propKeys[i]] = propValues[i];
            }

            return req;
        }

        private static Gen<SurveyCreateRequest> CreateRequestGen()
        {
            return from planetName in SafeStringGen()
                   from systemName in SafeStringGen()
                   from surveyID in SafeStringGen()
                   from nickName in SafeStringGen()
                   from scannedBy in SafeStringGen()
                   from dateTime in SafeStringGen()
                   from scannerBlueprintUUID in SafeStringGen()
                   from asteroidUUID in SafeStringGen()
                   from surveyTypeInt in Gen.Choose(0, 1)
                   from resourceCount in Gen.Choose(0, 5)
                   from selectedResources in Gen.Shuffle(SampleResourceNames).Select(a => a.Take(resourceCount))
                   from purities in Gen.ListOf(resourceCount, Gen.Elements(Purities))
                   from amounts in Gen.ListOf(resourceCount, NumericAmountGen())
                   from propCount in Gen.Choose(0, 3)
                   from selectedProps in Gen.Shuffle(PropertyKeys).Select(a => a.Take(propCount))
                   from propValues in Gen.ListOf(propCount, SafeStringGen())
                   select BuildCreateRequest(
                       planetName,
                       systemName,
                       surveyID,
                       nickName,
                       scannedBy,
                       dateTime,
                       scannerBlueprintUUID,
                       asteroidUUID,
                       (SurveyType)surveyTypeInt,
                       selectedResources.ToArray(),
                       purities.ToArray(),
                       amounts.ToArray(),
                       selectedProps.ToArray(),
                       propValues.ToArray());
        }

        private static SurveyCreateRequest BuildCreateRequest(
            string planetName,
            string systemName,
            string surveyID,
            string nickName,
            string scannedBy,
            string dateTime,
            string scannerBlueprintUUID,
            string asteroidUUID,
            SurveyType surveyType,
            string[] resources,
            string[] purities,
            string[] amounts,
            string[] propKeys,
            string[] propValues)
        {
            var req = new SurveyCreateRequest
            {
                PlanetName = planetName,
                SystemName = systemName,
                SurveyID = surveyID,
                NickName = nickName,
                ScannedBy = scannedBy,
                DateTime = dateTime,
                ScannerBlueprintUUID = scannerBlueprintUUID,
                AsteroidUUID = asteroidUUID,
                SurveyType = surveyType,
                Resources = new Dictionary<string, SurveyResource>(),
                Properties = new Dictionary<string, string>(),
            };

            for (int i = 0; i < resources.Length; i++)
            {
                req.Resources[resources[i]] = new SurveyResource(resources[i], purities[i], amounts[i]);
            }

            for (int i = 0; i < propKeys.Length && i < propValues.Length; i++)
            {
                req.Properties[propKeys[i]] = propValues[i];
            }

            return req;
        }

        // -----------------------------------------------------------------------
        // Property 4: Service.Update Round-Trip
        // Feature: bl-110-survey-readonly, Property 4: Service.Update Round-Trip
        // **Validates: Requirements 13.3, 13.4, 13.5, 13.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Update_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(UpdateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new SurveyService(ctx);

                var seed = new Survey
                {
                    UUID = Guid.NewGuid().ToString(),
                    OwnerUUID = "test-player-uuid",
                    PlanetName = "Seed",
                };
                ctx.AddSurvey(seed);

                var result = svc.Update(seed.UUID, request);

                bool planetMatch = result.PlanetName == request.PlanetName;
                bool systemMatch = result.SystemName == request.SystemName;
                bool surveyIdMatch = result.SurveyID == request.SurveyID;
                bool nickMatch = result.NickName == request.NickName;
                bool scannedByMatch = result.ScannedBy == request.ScannedBy;
                bool dateTimeMatch = result.DateTime == request.DateTime;
                bool scannerMatch = result.ScannerBlueprintUUID == request.ScannerBlueprintUUID;
                bool asteroidMatch = result.AsteroidUUID == request.AsteroidUUID;
                bool typeMatch = result.SurveyType == request.SurveyType;

                bool resourcesMatch = result.Resources.Count == request.Resources.Count
                    && request.Resources.All(kv =>
                        result.Resources.ContainsKey(kv.Key)
                        && result.Resources[kv.Key].Resource == kv.Value.Resource
                        && result.Resources[kv.Key].Purity == kv.Value.Purity
                        && result.Resources[kv.Key].Amount == kv.Value.Amount);

                bool propertiesMatch = result.Properties.Count == request.Properties.Count
                    && request.Properties.All(kv =>
                        result.Properties.ContainsKey(kv.Key)
                        && result.Properties[kv.Key] == kv.Value);

                return (planetMatch && systemMatch && surveyIdMatch && nickMatch
                    && scannedByMatch && dateTimeMatch && scannerMatch && asteroidMatch
                    && typeMatch && resourcesMatch && propertiesMatch)
                    .Label(
                        "planet=" + planetMatch + ", system=" + systemMatch + ", surveyId=" + surveyIdMatch +
                        ", nick=" + nickMatch + ", scannedBy=" + scannedByMatch + ", dateTime=" + dateTimeMatch +
                        ", scanner=" + scannerMatch + ", asteroid=" + asteroidMatch + ", type=" + typeMatch +
                        ", resources=" + resourcesMatch + ", properties=" + propertiesMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 5: Service.Create Round-Trip
        // Feature: bl-110-survey-readonly, Property 5: Service.Create Round-Trip
        // **Validates: Requirements 14.2, 14.4, 14.8**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Create_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(Arb.From(CreateRequestGen()), request =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new SurveyService(ctx);

                var result = svc.Create(request);

                if (string.IsNullOrEmpty(result.UUID)) return false.Label("UUID was empty");

                bool planetMatch = result.PlanetName == request.PlanetName;
                bool systemMatch = result.SystemName == request.SystemName;
                bool surveyIdMatch = result.SurveyID == request.SurveyID;
                bool nickMatch = result.NickName == request.NickName;
                bool scannedByMatch = result.ScannedBy == request.ScannedBy;
                bool dateTimeMatch = result.DateTime == request.DateTime;
                bool scannerMatch = result.ScannerBlueprintUUID == request.ScannerBlueprintUUID;
                bool asteroidMatch = result.AsteroidUUID == request.AsteroidUUID;
                bool typeMatch = result.SurveyType == request.SurveyType;

                bool resourcesMatch = result.Resources.Count == request.Resources.Count
                    && request.Resources.All(kv =>
                        result.Resources.ContainsKey(kv.Key)
                        && result.Resources[kv.Key].Resource == kv.Value.Resource
                        && result.Resources[kv.Key].Purity == kv.Value.Purity
                        && result.Resources[kv.Key].Amount == kv.Value.Amount);

                bool propertiesMatch = result.Properties.Count == request.Properties.Count
                    && request.Properties.All(kv =>
                        result.Properties.ContainsKey(kv.Key)
                        && result.Properties[kv.Key] == kv.Value);

                return (planetMatch && systemMatch && surveyIdMatch && nickMatch
                    && scannedByMatch && dateTimeMatch && scannerMatch && asteroidMatch
                    && typeMatch && resourcesMatch && propertiesMatch)
                    .Label(
                        "planet=" + planetMatch + ", system=" + systemMatch + ", surveyId=" + surveyIdMatch +
                        ", nick=" + nickMatch + ", scannedBy=" + scannedByMatch + ", dateTime=" + dateTimeMatch +
                        ", scanner=" + scannerMatch + ", asteroid=" + asteroidMatch + ", type=" + typeMatch +
                        ", resources=" + resourcesMatch + ", properties=" + propertiesMatch);
            });
        }

        // -----------------------------------------------------------------------
        // Property 6: Service.Delete Removes Survey
        // Feature: bl-110-survey-readonly, Property 6: Service.Delete Removes Survey
        // **Validates: Requirements 15.1, 15.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Delete_RemovesSurvey()
        {
            return Prop.ForAll(ValidSurveyGen().ToArbitrary(), survey =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new SurveyService(ctx);

                ctx.AddSurvey(survey);
                string uuid = survey.UUID;

                svc.Delete(uuid);

                var found = ctx.FindMutableSurvey(uuid);
                return (found == null).Label(
                    found == null ? "OK" : "Survey still exists after Delete");
            });
        }

        // -----------------------------------------------------------------------
        // Property 7: Import Dedup --- Existing Survey Preserves UUID and NickName
        // Feature: bl-110-survey-readonly, Property 7
        // **Validates: Requirements 16.2**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Import_Dedup_PreservesUUIDAndNickName()
        {
            return Prop.ForAll(ValidSurveyGen().ToArbitrary(), SafeStringGen().ToArbitrary(), (survey, newScannedBy) =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new SurveyService(ctx);

                // Force Planet type to avoid asteroid auto-creation side effects
                survey.SurveyType = SurveyType.Planet;
                ctx.AddSurvey(survey);

                string originalUUID = survey.UUID;
                string originalNickName = survey.NickName;

                // Create a temp survey with matching PlanetName+SurveyID but different data
                var tempSurvey = new Survey
                {
                    PlanetName = survey.PlanetName,
                    SurveyID = survey.SurveyID,
                    SystemName = survey.SystemName,
                    ScannedBy = newScannedBy,
                    DateTime = "2025-01-01T00:00:00Z",
                    ScannerBlueprintUUID = "temp-scanner",
                    SurveyType = SurveyType.Planet,
                    AsteroidUUID = string.Empty,
                };

                var result = svc.Import(tempSurvey);

                bool uuidPreserved = result.UUID == originalUUID;
                bool nickPreserved = result.NickName == originalNickName;
                bool scannedByUpdated = result.ScannedBy == newScannedBy;

                return (uuidPreserved && nickPreserved && scannedByUpdated)
                    .Label(
                        "uuid=" + uuidPreserved + ", nick=" + nickPreserved +
                        ", scannedBy=" + scannedByUpdated);
            });
        }

        // -----------------------------------------------------------------------
        // Property 8: Import New --- Creates Survey with Non-Empty UUID
        // Feature: bl-110-survey-readonly, Property 8
        // **Validates: Requirements 16.3, 16.7**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property Import_New_CreatesSurveyWithNonEmptyUUID()
        {
            return Prop.ForAll(ValidSurveyGen().ToArbitrary(), survey =>
            {
                TestHelper.ResetWithCachedData();
                var ctx = PlayerContext.GetInstance();
                ctx.CurrentPlayerUUID = "test-player-uuid";
                var svc = new SurveyService(ctx);

                // Force Planet type to avoid asteroid auto-creation side effects
                survey.SurveyType = SurveyType.Planet;

                // Clear UUID so it looks like a temp parsed survey
                survey.UUID = null;

                var result = svc.Import(survey);

                bool hasUUID = !string.IsNullOrEmpty(result.UUID);
                bool planetMatch = result.PlanetName == survey.PlanetName;
                bool systemMatch = result.SystemName == survey.SystemName;
                bool surveyIdMatch = result.SurveyID == survey.SurveyID;

                return (hasUUID && planetMatch && systemMatch && surveyIdMatch)
                    .Label(
                        "hasUUID=" + hasUUID + ", planet=" + planetMatch +
                        ", system=" + systemMatch + ", surveyId=" + surveyIdMatch);
            });
        }
    }
}