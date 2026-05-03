using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Property-based tests for SurveyViewModel edit buffer.
    /// Feature: bl-110-survey-readonly
    /// Validates: LoadFrom round-trip, IsDirty false after LoadFrom, IsDirty detects changes.
    /// </summary>
    [TestFixture]
    public class SurveyViewModelPropertyTests
    {
        // -----------------------------------------------------------------------
        // Shared generator: builds a random Survey with all fields populated
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

        // -----------------------------------------------------------------------
        // Property 1: LoadFrom round-trip preserves all fields
        // Feature: bl-110-survey-readonly, Property 1
        // **Validates: Requirements 4.1, 4.2, 4.3**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property LoadFrom_RoundTrip_PreservesAllFields()
        {
            return Prop.ForAll(ValidSurveyGen().ToArbitrary(), survey =>
            {
                var ro = new ReadOnlySurvey(survey);
                var vm = new SurveyViewModel();
                vm.LoadFrom(ro);

                bool planetMatch = vm.PlanetName == (ro.PlanetName ?? string.Empty);
                bool systemMatch = vm.SystemName == (ro.SystemName ?? string.Empty);
                bool surveyIdMatch = vm.SurveyID == (ro.SurveyID ?? string.Empty);
                bool nickMatch = vm.NickName == (ro.NickName ?? string.Empty);
                bool scannedByMatch = vm.ScannedBy == (ro.ScannedBy ?? string.Empty);
                bool dateTimeMatch = vm.DateTime == (ro.DateTime ?? string.Empty);
                bool scannerMatch = vm.ScannerBlueprintUUID == (ro.ScannerBlueprintUUID ?? string.Empty);
                bool asteroidMatch = vm.AsteroidUUID == (ro.AsteroidUUID ?? string.Empty);
                bool typeMatch = vm.SurveyTypeValue == ro.SurveyType;

                var roResources = ro.Resources;
                bool resourcesMatch = vm.Resources.Count == roResources.Count
                    && roResources.All(kv =>
                        vm.Resources.ContainsKey(kv.Key)
                        && vm.Resources[kv.Key].Resource == kv.Value.Resource
                        && vm.Resources[kv.Key].Purity == kv.Value.Purity
                        && vm.Resources[kv.Key].Amount == kv.Value.Amount);

                var roProperties = ro.Properties;
                bool propertiesMatch = vm.Properties.Count == roProperties.Count
                    && roProperties.All(kv =>
                        vm.Properties.ContainsKey(kv.Key)
                        && vm.Properties[kv.Key] == kv.Value);

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
        // Property 2: IsDirty false immediately after LoadFrom
        // Feature: bl-110-survey-readonly, Property 2
        // **Validates: Requirements 7.1, 7.5**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 25)]
        public Property IsDirty_FalseImmediatelyAfterLoadFrom()
        {
            return Prop.ForAll(ValidSurveyGen().ToArbitrary(), survey =>
            {
                var ro = new ReadOnlySurvey(survey);
                var vm = new SurveyViewModel();
                vm.LoadFrom(ro);

                return (!vm.IsDirty).Label(
                    vm.IsDirty ? "IsDirty was true after LoadFrom" : "OK");
            });
        }

        // -----------------------------------------------------------------------
        // Property 3: IsDirty detects any single field change
        // Feature: bl-110-survey-readonly, Property 3
        // **Validates: Requirements 7.1, 7.2, 7.3, 7.4**
        // -----------------------------------------------------------------------

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public Property IsDirty_DetectsAnySingleFieldChange()
        {
            // 8 scalar fields + SurveyType + resource change + property change = 11 cases
            var fieldIndexGen = Gen.Choose(0, 10);

            return Prop.ForAll(
                ValidSurveyGen().ToArbitrary(),
                Arb.From(fieldIndexGen),
                (survey, fieldIndex) =>
                {
                    var ro = new ReadOnlySurvey(survey);
                    var vm = new SurveyViewModel();
                    vm.LoadFrom(ro);

                    string changedField = MutateSingleField(vm, ro, fieldIndex);

                    return vm.IsDirty.Label(
                        vm.IsDirty ? "OK" : "IsDirty was false after changing " + changedField);
                });
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string MutateSingleField(
            SurveyViewModel vm,
            ReadOnlySurvey ro,
            int fieldIndex)
        {
            switch (fieldIndex % 11)
            {
                case 0:
                    vm.PlanetName = (ro.PlanetName ?? string.Empty) + "X";
                    return "PlanetName";
                case 1:
                    vm.SystemName = (ro.SystemName ?? string.Empty) + "X";
                    return "SystemName";
                case 2:
                    vm.SurveyID = (ro.SurveyID ?? string.Empty) + "X";
                    return "SurveyID";
                case 3:
                    vm.NickName = (ro.NickName ?? string.Empty) + "X";
                    return "NickName";
                case 4:
                    vm.ScannedBy = (ro.ScannedBy ?? string.Empty) + "X";
                    return "ScannedBy";
                case 5:
                    vm.DateTime = (ro.DateTime ?? string.Empty) + "X";
                    return "DateTime";
                case 6:
                    vm.ScannerBlueprintUUID = (ro.ScannerBlueprintUUID ?? string.Empty) + "X";
                    return "ScannerBlueprintUUID";
                case 7:
                    vm.SurveyTypeValue = ro.SurveyType == SurveyType.Planet
                        ? SurveyType.Asteroid
                        : SurveyType.Planet;
                    return "SurveyType";
                case 8:
                    vm.AsteroidUUID = (ro.AsteroidUUID ?? string.Empty) + "X";
                    return "AsteroidUUID";
                case 9:
                    // Add a new resource entry to make Resources differ
                    vm.Resources["__test_resource__"] = new SurveyResource("TestRes", "High", "999");
                    return "Resources";
                case 10:
                    // Add a new property entry to make Properties differ
                    vm.Properties["__test_property__"] = "TestValue";
                    return "Properties";
                default:
                    vm.PlanetName = (ro.PlanetName ?? string.Empty) + "X";
                    return "PlanetName(default)";
            }
        }
    }
}