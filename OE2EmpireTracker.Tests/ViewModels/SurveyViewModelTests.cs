using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for SurveyViewModel edit buffer.
    /// Feature: bl-110-survey-readonly
    /// Validates: Requirements 4.1, 4.2, 7.1, 7.5, 7.6
    /// </summary>
    [TestFixture]
    public class SurveyViewModelTests
    {
        // -----------------------------------------------------------------------
        // Reset
        // -----------------------------------------------------------------------

        [Test]
        public void Reset_ClearsAllFieldsToDefaults()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.UUID, Is.Null);
            Assert.That(vm.OwnerUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.PlanetName, Is.EqualTo(string.Empty));
            Assert.That(vm.SystemName, Is.EqualTo(string.Empty));
            Assert.That(vm.SurveyID, Is.EqualTo(string.Empty));
            Assert.That(vm.NickName, Is.EqualTo(string.Empty));
            Assert.That(vm.ScannedBy, Is.EqualTo(string.Empty));
            Assert.That(vm.DateTime, Is.EqualTo(string.Empty));
            Assert.That(vm.ScannerBlueprintUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.AsteroidUUID, Is.EqualTo(string.Empty));
            Assert.That(vm.SurveyTypeValue, Is.EqualTo(SurveyType.Planet));
            Assert.That(vm.Resources, Is.Empty);
            Assert.That(vm.Properties, Is.Empty);
            Assert.That(vm.Original, Is.Null);
        }

        // -----------------------------------------------------------------------
        // IsNew
        // -----------------------------------------------------------------------

        [Test]
        public void IsNew_ReturnsTrue_AfterReset()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.Reset();

            Assert.That(vm.IsNew, Is.True);
        }

        [Test]
        public void IsNew_ReturnsFalse_AfterLoadFrom()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            Assert.That(vm.IsNew, Is.False);
        }

        // -----------------------------------------------------------------------
        // IsDirty
        // -----------------------------------------------------------------------

        [Test]
        public void IsDirty_ReturnsTrue_ForNewSurveyWithNonDefaultPlanetName()
        {
            var vm = new SurveyViewModel();
            vm.Reset();
            vm.PlanetName = "Test Planet";

            Assert.That(vm.IsDirty, Is.True);
        }

        // -----------------------------------------------------------------------
        // BuildUpdateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildUpdateRequest_CopiesAllFieldsIncludingResourcesAndProperties()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            vm.PlanetName = "Updated Planet";
            vm.SystemName = "Updated System";
            vm.SurveyID = "UPD-001";
            vm.NickName = "Updated Nick";
            vm.ScannedBy = "Updated Scanner";
            vm.DateTime = "2025-01-01T00:00:00Z";
            vm.ScannerBlueprintUUID = "scanner-upd";
            vm.AsteroidUUID = "asteroid-upd";
            vm.SurveyTypeValue = SurveyType.Asteroid;
            vm.Resources["Iron"] = new SurveyResource("Iron", "High", "500");
            vm.Properties["ScanLevel"] = "5";

            var request = vm.BuildUpdateRequest();

            Assert.That(request.Original, Is.SameAs(vm.Original));
            Assert.That(request.PlanetName, Is.EqualTo("Updated Planet"));
            Assert.That(request.SystemName, Is.EqualTo("Updated System"));
            Assert.That(request.SurveyID, Is.EqualTo("UPD-001"));
            Assert.That(request.NickName, Is.EqualTo("Updated Nick"));
            Assert.That(request.ScannedBy, Is.EqualTo("Updated Scanner"));
            Assert.That(request.DateTime, Is.EqualTo("2025-01-01T00:00:00Z"));
            Assert.That(request.ScannerBlueprintUUID, Is.EqualTo("scanner-upd"));
            Assert.That(request.AsteroidUUID, Is.EqualTo("asteroid-upd"));
            Assert.That(request.SurveyType, Is.EqualTo(SurveyType.Asteroid));
            Assert.That(request.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(request.Resources["Iron"].Resource, Is.EqualTo("Iron"));
            Assert.That(request.Resources["Iron"].Purity, Is.EqualTo("High"));
            Assert.That(request.Resources["Iron"].Amount, Is.EqualTo("500"));
            Assert.That(request.Properties.ContainsKey("ScanLevel"), Is.True);
            Assert.That(request.Properties["ScanLevel"], Is.EqualTo("5"));
        }

        // -----------------------------------------------------------------------
        // BuildCreateRequest
        // -----------------------------------------------------------------------

        [Test]
        public void BuildCreateRequest_CopiesAllFieldsIncludingResourcesAndProperties()
        {
            var vm = new SurveyViewModel();
            vm.Reset();
            vm.PlanetName = "New Planet";
            vm.SystemName = "New System";
            vm.SurveyID = "NEW-001";
            vm.NickName = "New Nick";
            vm.ScannedBy = "New Scanner";
            vm.DateTime = "2025-06-15T12:00:00Z";
            vm.ScannerBlueprintUUID = "scanner-new";
            vm.AsteroidUUID = "asteroid-new";
            vm.SurveyTypeValue = SurveyType.Asteroid;
            vm.Resources["Copper"] = new SurveyResource("Copper", "Medium", "250");
            vm.Properties["SensorAbundance"] = "High";

            var request = vm.BuildCreateRequest();

            Assert.That(request.PlanetName, Is.EqualTo("New Planet"));
            Assert.That(request.SystemName, Is.EqualTo("New System"));
            Assert.That(request.SurveyID, Is.EqualTo("NEW-001"));
            Assert.That(request.NickName, Is.EqualTo("New Nick"));
            Assert.That(request.ScannedBy, Is.EqualTo("New Scanner"));
            Assert.That(request.DateTime, Is.EqualTo("2025-06-15T12:00:00Z"));
            Assert.That(request.ScannerBlueprintUUID, Is.EqualTo("scanner-new"));
            Assert.That(request.AsteroidUUID, Is.EqualTo("asteroid-new"));
            Assert.That(request.SurveyType, Is.EqualTo(SurveyType.Asteroid));
            Assert.That(request.Resources.ContainsKey("Copper"), Is.True);
            Assert.That(request.Resources["Copper"].Resource, Is.EqualTo("Copper"));
            Assert.That(request.Resources["Copper"].Purity, Is.EqualTo("Medium"));
            Assert.That(request.Resources["Copper"].Amount, Is.EqualTo("250"));
            Assert.That(request.Properties.ContainsKey("SensorAbundance"), Is.True);
            Assert.That(request.Properties["SensorAbundance"], Is.EqualTo("High"));
        }

        // -----------------------------------------------------------------------
        // Deep copy independence
        // -----------------------------------------------------------------------

        [Test]
        public void BuildUpdateRequest_ResourcesDeepCopyIsIndependent()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            var request = vm.BuildUpdateRequest();
            request.Resources["Alkali Metals"] = new SurveyResource("Alkali Metals", "Low", "9999");

            Assert.That(vm.Resources["Alkali Metals"].Purity, Is.EqualTo("High"));
            Assert.That(vm.Resources["Alkali Metals"].Amount, Is.EqualTo("100"));
        }

        [Test]
        public void BuildUpdateRequest_PropertiesDeepCopyIsIndependent()
        {
            var vm = new SurveyViewModel();
            vm.LoadFrom(CreateSampleReadOnly());

            var request = vm.BuildUpdateRequest();
            request.Properties["SensorAbundance"] = "MODIFIED";

            Assert.That(vm.Properties["SensorAbundance"], Is.EqualTo("Abundant"));
        }

        [Test]
        public void BuildCreateRequest_ResourcesDeepCopyIsIndependent()
        {
            var vm = new SurveyViewModel();
            vm.Reset();
            vm.Resources["Iron"] = new SurveyResource("Iron", "High", "500");

            var request = vm.BuildCreateRequest();
            request.Resources["Iron"] = new SurveyResource("Iron", "Low", "1");

            Assert.That(vm.Resources["Iron"].Purity, Is.EqualTo("High"));
            Assert.That(vm.Resources["Iron"].Amount, Is.EqualTo("500"));
        }

        [Test]
        public void BuildCreateRequest_PropertiesDeepCopyIsIndependent()
        {
            var vm = new SurveyViewModel();
            vm.Reset();
            vm.Properties["PurityModifier"] = "OriginalValue";

            var request = vm.BuildCreateRequest();
            request.Properties["PurityModifier"] = "MODIFIED";

            Assert.That(vm.Properties["PurityModifier"], Is.EqualTo("OriginalValue"));
        }

        // -----------------------------------------------------------------------
        // Sensor reading convenience accessors
        // -----------------------------------------------------------------------

        [Test]
        public void SensorAbundance_ReadsAndWritesPropertiesDictionary()
        {
            var vm = new SurveyViewModel();
            vm.Reset();

            Assert.That(vm.SensorAbundance, Is.EqualTo(string.Empty));

            vm.SensorAbundance = "Abundant";
            Assert.That(vm.SensorAbundance, Is.EqualTo("Abundant"));
            Assert.That(vm.Properties["SensorAbundance"], Is.EqualTo("Abundant"));
        }

        [Test]
        public void PurityModifier_ReadsAndWritesPropertiesDictionary()
        {
            var vm = new SurveyViewModel();
            vm.Reset();

            Assert.That(vm.PurityModifier, Is.EqualTo(string.Empty));

            vm.PurityModifier = "+10%";
            Assert.That(vm.PurityModifier, Is.EqualTo("+10%"));
            Assert.That(vm.Properties["PurityModifier"], Is.EqualTo("+10%"));
        }

        [Test]
        public void ScanLevel_ReadsAndWritesPropertiesDictionary()
        {
            var vm = new SurveyViewModel();
            vm.Reset();

            Assert.That(vm.ScanLevel, Is.EqualTo(string.Empty));

            vm.ScanLevel = "3";
            Assert.That(vm.ScanLevel, Is.EqualTo("3"));
            Assert.That(vm.Properties["ScanLevel"], Is.EqualTo("3"));
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static ReadOnlySurvey CreateSampleReadOnly()
        {
            var survey = new Survey
            {
                UUID = "survey-001",
                PlanetName = "Zeh Vazoran II",
                SystemName = "Zeh Vazoran",
                SurveyID = "ZV2-001",
                NickName = "Main Survey",
                OwnerUUID = "owner-001",
                ScannedBy = "Scanner Alpha",
                DateTime = "2024-12-01T10:30:00Z",
                ScannerBlueprintUUID = "bp-scanner-001",
                AsteroidUUID = string.Empty,
                SurveyType = SurveyType.Planet,
            };

            survey.Resources["Alkali Metals"] = new SurveyResource("Alkali Metals", "High", "100");
            survey.Resources["Noble Gases"] = new SurveyResource("Noble Gases", "Medium", "50");
            survey.Properties["SensorAbundance"] = "Abundant";
            survey.Properties["PurityModifier"] = "+5%";

            return new ReadOnlySurvey(survey);
        }
    }
}