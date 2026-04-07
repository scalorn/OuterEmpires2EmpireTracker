using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.ViewModels;
using System;
using System.IO;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class ColonyStructureTests
    {
        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_AssignedPropertiesAreNotNull()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.Properties, Is.Not.Null);
            Assert.That(structure.AssignedWorkers, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_InitializedCollectionsAreEmpty()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.Statuses.Count, Is.EqualTo(0));
            Assert.That(structure.Properties.Count, Is.EqualTo(0));
            Assert.That(structure.AssignedWorkers.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // UUID and FlatpackBlueprintUUID
        // -----------------------------------------------------------------------

        [Test]
        public void Set_UUIDAndFlatpackBlueprintUUID_CanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.UUID = "test-uuid-123";
            structure.FlatpackBlueprintUUID = "blueprint-uuid-456";

            Assert.That(structure.UUID, Is.EqualTo("test-uuid-123"));
            Assert.That(structure.FlatpackBlueprintUUID, Is.EqualTo("blueprint-uuid-456"));
        }

        // -----------------------------------------------------------------------
        // gameSequence and buildQueueSequence
        // -----------------------------------------------------------------------

        [Test]
        public void Set_GameSequenceAndBuildQueueSequence_CanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.gameSequence = 5;
            structure.buildQueueSequence = 3;

            Assert.That(structure.gameSequence, Is.EqualTo(5));
            Assert.That(structure.buildQueueSequence, Is.EqualTo(3));
        }

        // -----------------------------------------------------------------------
        // Properties Dictionary Operations
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_AddingToPropertiesDictionary_Succeeds()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("CustomProp1", "Value1");

            Assert.That(structure.Properties.ContainsKey("CustomProp1"), Is.True);

            structure.Properties.getString("CustomProp1", null, out string value);
            Assert.That(value, Is.EqualTo("Value1"));
        }

        [Test]
        public void SetProperty_RemovalFromPropertiesDictionary_RemovesKey()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("ToRemove", "OldValue");

            Assert.That(structure.Properties.ContainsKey("ToRemove"), Is.True);
            bool result = structure.Properties.Remove("ToRemove");
            Assert.That(result, Is.True);
            Assert.That(structure.Properties.ContainsKey("ToRemove"), Is.False);
        }

        [Test]
        public void SetProperty_ClearRemovesAllProperties()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Prop1", "Value1");
            structure.Properties.setProperty("Prop2", "Value2");

            Assert.That(structure.Properties.Count, Is.EqualTo(2));
            structure.Properties.Clear();
            Assert.That(structure.Properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void Properties_gdoubleMethod_ParsesValidDouble()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "123.45");

            bool success = structure.Properties.getDouble("Value", -1.0, out double result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(123.45).Within(0.01));
        }

        [Test]
        public void Properties_gdoubleMethod_ReturnsFalseForInvalidDouble()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "not-a-number");

            bool success = structure.Properties.getDouble("Value", -1.0, out double result);
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(-1.0).Within(0.0));
        }

        [Test]
        public void Properties_gdoubleMethod_ReturnsDefaultValueOnMissingKey()
        {
            var structure = new ColonyStructure();
            bool success = structure.Properties.getDouble("Missing", -1.0, out double result);
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(-1.0).Within(0.0));
        }

        [Test]
        public void Properties_glongMethod_ParsesValidLong()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "999");

            bool success = structure.Properties.getLong("Value", -1, out long result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(999));
        }

        [Test]
        public void Properties_gbooleanMethod_ParsesValidBoolean()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "true");

            bool success = structure.Properties.getBoolean("Value", false, out bool result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(true));
        }

        [Test]
        public void Properties_gbooleanMethod_ReturnsFalseForInvalidBoolean()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "not-a-boolean");

            bool success = structure.Properties.getBoolean("Value", false, out bool result);
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(false));
        }

        [Test]
        public void Properties_gstringMethod_ReturnsExistingString()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Name", "MyValue");

            bool success = structure.Properties.getString("Name", "", out string result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("MyValue"));
        }

        [Test]
        public void Properties_gstringMethod_ReturnsDefaultValueOnMissingKey()
        {
            var structure = new ColonyStructure();
            bool success = structure.Properties.getString("Missing", "", out string result);
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(""));
        }

        // -----------------------------------------------------------------------
        // AssignedWorkers Dictionary Operations
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_AddingToAssignedWorkersDictionary_Succeeds()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("Worker1", "Engineer");

            Assert.That(structure.AssignedWorkers.ContainsKey("Worker1"), Is.True);
            structure.AssignedWorkers.getString("Worker1", null, out string value);
            Assert.That(value, Is.EqualTo("Engineer"));
        }

        [Test]
        public void AssignedWorkers_ClearRemovesAllEntries()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("W1", "A");
            structure.AssignedWorkers.setProperty("W2", "B");

            Assert.That(structure.AssignedWorkers.Count, Is.EqualTo(2));
            structure.AssignedWorkers.Clear();
            Assert.That(structure.AssignedWorkers.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Statuses Dictionary Operations
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_AddingToStatusesDictionary_Succeeds()
        {
            var structure = new ColonyStructure();
            var status = new ColonyStructureStatus();
            status.PowerProvided = 100.0;
            structure.Statuses["Power"] = status;

            Assert.That(structure.Statuses.ContainsKey("Power"), Is.True);
            Assert.That(structure.Statuses["Power"].PowerProvided, Is.EqualTo(100.0));
        }

        [Test]
        public void Statuses_ClearRemovesAllEntries()
        {
            var structure = new ColonyStructure();
            var status1 = new ColonyStructureStatus();
            var status2 = new ColonyStructureStatus();
            structure.Statuses["Status1"] = status1;
            structure.Statuses["Status2"] = status2;

            Assert.That(structure.Statuses.Count, Is.EqualTo(2));
            structure.Statuses.Clear();
            Assert.That(structure.Statuses.Count, Is.EqualTo(0));
        }

        [Test]
        public void Statuses_StatusPropertiesAreAccessible()
        {
            var structure = new ColonyStructure();
            var status = new ColonyStructureStatus();
            status.PowerProvided = 50.0;
            status.HabitationProvision = 25.0;
            status.FoodProvision = 75.0;
            status.EntertainmentProvided = 30.0;

            structure.Statuses["Power"] = status;

            Assert.That(structure.Statuses["Power"].PowerProvided, Is.EqualTo(50.0));
            Assert.That(structure.Statuses["Power"].HabitationProvision, Is.EqualTo(25.0));
            Assert.That(structure.Statuses["Power"].FoodProvision, Is.EqualTo(75.0));
            Assert.That(structure.Statuses["Power"].EntertainmentProvided, Is.EqualTo(30.0));
        }

        // -----------------------------------------------------------------------
        // Statuses are [JsonIgnore] — not serialized (AMB-029)
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_Statuses_AreNotSerialized()
        {
            var structure = new ColonyStructure();
            structure.Statuses["Actual"] = new ColonyStructureStatus { PowerProvided = 100.0 };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            // Statuses are [JsonIgnore] so they should be empty after deserialization
            Assert.That(restored.Statuses.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // JSON Round-Trip Tests
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_CoreProperties_Preserved()
        {
            var structure = new ColonyStructure
            {
                UUID = "test-uuid",
                FlatpackBlueprintUUID = "blueprint-uuid",
                gameSequence = 5,
                buildQueueSequence = 3,
                CurrentAttitude = "Happy",
                ContentmentIndex = 80,
                WageLevel = 2
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.UUID, Is.EqualTo(structure.UUID));
            Assert.That(restored.FlatpackBlueprintUUID, Is.EqualTo(structure.FlatpackBlueprintUUID));
            Assert.That(restored.gameSequence, Is.EqualTo(structure.gameSequence));
            Assert.That(restored.buildQueueSequence, Is.EqualTo(structure.buildQueueSequence));
            Assert.That(restored.CurrentAttitude, Is.EqualTo(structure.CurrentAttitude));
            Assert.That(restored.ContentmentIndex, Is.EqualTo(structure.ContentmentIndex));
            Assert.That(restored.WageLevel, Is.EqualTo(structure.WageLevel));
        }

        [Test]
        public void JsonRoundTrip_WithPropertiesDictionary_Preserved()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("CustomProp1", "Value1");
            structure.Properties.setProperty("CustomProp2", "Value2");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.Properties.ContainsKey("CustomProp1"), Is.True);
            restored.Properties.getString("CustomProp1", null, out string value1);
            Assert.That(value1, Is.EqualTo("Value1"));
            Assert.That(restored.Properties.ContainsKey("CustomProp2"), Is.True);
            restored.Properties.getString("CustomProp2", null, out string value2);
            Assert.That(value2, Is.EqualTo("Value2"));
        }

        [Test]
        public void JsonRoundTrip_WithAssignedWorkersDictionary_Preserved()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("Worker1", "Engineer");
            structure.AssignedWorkers.setProperty("Worker2", "Scout");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.AssignedWorkers.ContainsKey("Worker1"), Is.True);
            restored.AssignedWorkers.getString("Worker1", null, out string value1);
            Assert.That(value1, Is.EqualTo("Engineer"));
            Assert.That(restored.AssignedWorkers.ContainsKey("Worker2"), Is.True);
            restored.AssignedWorkers.getString("Worker2", null, out string value2);
            Assert.That(value2, Is.EqualTo("Scout"));
        }

        [Test]
        public void JsonRoundTrip_CompleteStructure_Preserved()
        {
            var structure = new ColonyStructure
            {
                UUID = "full-uuid-test",
                FlatpackBlueprintUUID = "bp-full-test",
                gameSequence = 10,
                buildQueueSequence = 2,
                CurrentAttitude = "Satisfied",
                ContentmentIndex = 75,
                WageLevel = 3,
                MiningSurvey = "survey-uuid",
                MiningSurveyResource = "Iron",
                MiningLeftOvers = 0.75m
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.UUID, Is.EqualTo(structure.UUID));
            Assert.That(restored.FlatpackBlueprintUUID, Is.EqualTo(structure.FlatpackBlueprintUUID));
            Assert.That(restored.gameSequence, Is.EqualTo(structure.gameSequence));
            Assert.That(restored.buildQueueSequence, Is.EqualTo(structure.buildQueueSequence));
            Assert.That(restored.CurrentAttitude, Is.EqualTo(structure.CurrentAttitude));
            Assert.That(restored.ContentmentIndex, Is.EqualTo(structure.ContentmentIndex));
            Assert.That(restored.WageLevel, Is.EqualTo(structure.WageLevel));
            Assert.That(restored.MiningSurvey, Is.EqualTo(structure.MiningSurvey));
            Assert.That(restored.MiningSurveyResource, Is.EqualTo(structure.MiningSurveyResource));
            Assert.That(restored.MiningLeftOvers, Is.EqualTo(structure.MiningLeftOvers));
        }

        // -----------------------------------------------------------------------
        // Edge Cases and Defaults
        // -----------------------------------------------------------------------

        [Test]
        public void NewStructure_HasEmptyStringForAttitude()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.CurrentAttitude, Is.EqualTo(""));
        }

        [Test]
        public void NewStructure_HasZeroContentmentIndex()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.ContentmentIndex, Is.EqualTo(0));
        }

        [Test]
        public void NewStructure_HasZeroMiningLeftOvers()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.MiningLeftOvers, Is.EqualTo(Decimal.Zero));
        }

        [Test]
        public void NewStructure_HasNullMiningSurvey()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.MiningSurvey, Is.Null);
        }

        // -----------------------------------------------------------------------
        // ColonyStructureStatus — GetUnallocatedPresent / SetUnallocatedPresent
        // -----------------------------------------------------------------------

        [Test]
        public void GetUnallocatedPresent_DefaultsToFalse()
        {
            var status = new ColonyStructureStatus();
            Assert.That(status.GetUnallocatedPresent("BlueCollarDetail"), Is.False);
            Assert.That(status.GetUnallocatedPresent("WhiteCollarDetail"), Is.False);
            Assert.That(status.GetUnallocatedPresent("SpecialistDetail"), Is.False);
        }

        [TestCase("BlueCollarDetail")]
        [TestCase("WhiteCollarDetail")]
        [TestCase("SpecialistDetail")]
        public void SetThenGetUnallocatedPresent_RoundTrips(string detailKey)
        {
            var status = new ColonyStructureStatus();
            status.SetUnallocatedPresent(detailKey, true);
            Assert.That(status.GetUnallocatedPresent(detailKey), Is.True);

            status.SetUnallocatedPresent(detailKey, false);
            Assert.That(status.GetUnallocatedPresent(detailKey), Is.False);
        }

        [Test]
        public void GetUnallocatedPresent_UnknownKey_ReturnsFalse()
        {
            var status = new ColonyStructureStatus();
            Assert.That(status.GetUnallocatedPresent("UnknownWorkerDetail"), Is.False);
        }

        // -----------------------------------------------------------------------
        // StagingResources — Property 5: StagingResources serialization round-trip
        // Validates: Requirements 4.1, 4.2
        // -----------------------------------------------------------------------

        [Test]
        public void StagingResources_DefaultsToFalse()
        {
            var structure = new ColonyStructure();
            Assert.That(structure.StagingResources, Is.False);
        }

        [Test]
        public void StagingResources_SetTrueViaViewModel_ReadBackTrue()
        {
            PlayerContext.Reset();
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            var pc = PlayerContext.getInstance();

            var structure = new ColonyStructure();
            var vm = new ColonyStructureViewModel(structure, pc);
            vm.StagingResources = true;

            Assert.That(vm.StagingResources, Is.True);
            Assert.That(structure.StagingResources, Is.True);
        }

        [Test]
        public void StagingResources_ViewModelPassThrough()
        {
            PlayerContext.Reset();
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            var pc = PlayerContext.getInstance();

            var structure = new ColonyStructure();
            var vm = new ColonyStructureViewModel(structure, pc);

            // Default is false
            Assert.That(vm.StagingResources, Is.False);

            // Set true via ViewModel, read back via ViewModel and underlying model
            vm.StagingResources = true;
            Assert.That(vm.StagingResources, Is.True);
            Assert.That(structure.StagingResources, Is.True);

            // Set false via ViewModel, read back
            vm.StagingResources = false;
            Assert.That(vm.StagingResources, Is.False);
            Assert.That(structure.StagingResources, Is.False);
        }

        [Test]
        public void StagingResources_JsonRoundTrip_PreservesTrue()
        {
            var structure = new ColonyStructure();
            structure.StagingResources = true;

            string json = JsonConvert.SerializeObject(structure);
            var restored = JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.StagingResources, Is.True);
        }

        [Test]
        public void StagingResources_JsonRoundTrip_PreservesFalse()
        {
            var structure = new ColonyStructure();
            structure.StagingResources = false;

            string json = JsonConvert.SerializeObject(structure);
            var restored = JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.That(restored.StagingResources, Is.False);
        }
    }
}
