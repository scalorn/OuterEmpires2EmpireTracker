using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;

namespace OE2EmpireTracker.Tests.Baseline
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
            Assert.IsNotNull(structure.Properties);
            Assert.IsNotNull(structure.AssignedWorkers);
        }

        [Test]
        public void DefaultConstructor_InitializedCollectionsAreEmpty()
        {
            var structure = new ColonyStructure();
            Assert.AreEqual(0, structure.Statuses.Count);
            Assert.AreEqual(0, structure.Properties.Count);
            Assert.AreEqual(0, structure.AssignedWorkers.Count);
        }

        // -----------------------------------------------------------------------
        // Basic Property Setting and Getting
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_PowerCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.Power = 100.5;
            Assert.AreEqual(100.5, structure.Power);
        }

        [Test]
        public void SetProperty_HabitationCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.Habitation = 50.0;
            Assert.AreEqual(50.0, structure.Habitation, 0.0);
        }

        [Test]
        public void SetProperty_FoodCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.Food = 75.25;
            Assert.AreEqual(75.25, structure.Food, 0.0);
        }

        [Test]
        public void SetProperty_EntertainmentCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.Entertainment = 123.456;
            Assert.AreEqual(123.456, structure.Entertainment, 0.0);
        }

        [Test]
        public void SetProperty_WarehouseCapacityCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.WarehouseCapacity = 1000;
            Assert.AreEqual(1000, structure.WarehouseCapacity);
        }

        [Test]
        public void SetProperty_WorkersAssignedCanBeSetAndRead()
        {
            var structure = new ColonyStructure();
            structure.WorkersAssigned = 5;
            Assert.AreEqual(5, structure.WorkersAssigned);
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

            Assert.AreEqual("test-uuid-123", structure.UUID);
            Assert.AreEqual("blueprint-uuid-456", structure.FlatpackBlueprintUUID);
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

            Assert.AreEqual(5, structure.gameSequence);
            Assert.AreEqual(3, structure.buildQueueSequence);
        }

        // -----------------------------------------------------------------------
        // Properties Dictionary Operations
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_AddingToPropertiesDictionary_Succeeds()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("CustomProp1", "Value1");

            Assert.IsTrue(structure.Properties.ContainsKey("CustomProp1"));

            structure.Properties.getString("CustomProp1", null, out string value);
            Assert.AreEqual("Value1", value);
        }

        [Test]
        public void SetProperty_RemovalFromPropertiesDictionary_RemovesKey()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("ToRemove", "OldValue");

            Assert.IsTrue(structure.Properties.ContainsKey("ToRemove"));
            bool result = structure.Properties.Remove("ToRemove");
            Assert.IsTrue(result);
            Assert.IsFalse(structure.Properties.ContainsKey("ToRemove"));
        }

        [Test]
        public void SetProperty_ClearRemovesAllProperties()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Prop1", "Value1");
            structure.Properties.setProperty("Prop2", "Value2");

            Assert.AreEqual(2, structure.Properties.Count);
            structure.Properties.Clear();
            Assert.AreEqual(0, structure.Properties.Count);
        }

        [Test]
        public void Properties_gdoubleMethod_ParsesValidDouble()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "123.45");

            bool success = structure.Properties.getDouble("Value", -1.0, out double result);
            Assert.IsTrue(success);
            Assert.AreEqual(123.45, result, 0.01);
        }

        [Test]
        public void Properties_gdoubleMethod_ReturnsFalseForInvalidDouble()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "not-a-number");

            bool success = structure.Properties.getDouble("Value", -1.0, out double result);
            Assert.IsFalse(success);
            Assert.AreEqual(-1.0, result, 0.0);
        }

        [Test]
        public void Properties_gdoubleMethod_ReturnsDefaultValueOnMissingKey()
        {
            var structure = new ColonyStructure();
            bool success = structure.Properties.getDouble("Missing", -1.0, out double result);
            Assert.IsFalse(success);
            Assert.AreEqual(-1.0, result, 0.0);
        }

        [Test]
        public void Properties_glongMethod_ParsesValidLong()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "999");

            bool success = structure.Properties.getLong("Value", -1, out long result);
            Assert.IsTrue(success);
            Assert.AreEqual(999, result);
        }

        [Test]
        public void Properties_gbooleanMethod_ParsesValidBoolean()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "true");

            bool success = structure.Properties.getBoolean("Value", false, out bool result);
            Assert.IsTrue(success);
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Properties_gbooleanMethod_ReturnsFalseForInvalidBoolean()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Value", "not-a-boolean");

            bool success = structure.Properties.getBoolean("Value", false, out bool result);
            Assert.IsFalse(success);
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Properties_gstringMethod_ReturnsExistingString()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("Name", "MyValue");

            bool success = structure.Properties.getString("Name", "", out string result);
            Assert.IsTrue(success);
            Assert.AreEqual("MyValue", result);
        }

        [Test]
        public void Properties_gstringMethod_ReturnsDefaultValueOnMissingKey()
        {
            var structure = new ColonyStructure();
            bool success = structure.Properties.getString("Missing", "", out string result);
            Assert.IsFalse(success);
            Assert.AreEqual("", result);
        }

        // -----------------------------------------------------------------------
        // AssignedWorkers Dictionary Operations
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_AddingToAssignedWorkersDictionary_Succeeds()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("Worker1", "Engineer");

            Assert.IsTrue(structure.AssignedWorkers.ContainsKey("Worker1"));
            structure.AssignedWorkers.getString("Worker1", null, out string value);
            Assert.AreEqual("Engineer", value);
        }

        [Test]
        public void AssignedWorkers_ClearRemovesAllEntries()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("W1", "A");
            structure.AssignedWorkers.setProperty("W2", "B");

            Assert.AreEqual(2, structure.AssignedWorkers.Count);
            structure.AssignedWorkers.Clear();
            Assert.AreEqual(0, structure.AssignedWorkers.Count);
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

            Assert.IsTrue(structure.Statuses.ContainsKey("Power"));
            Assert.AreEqual(100.0, structure.Statuses["Power"].PowerProvided);
        }

        [Test]
        public void Statuses_ClearRemovesAllEntries()
        {
            var structure = new ColonyStructure();
            var status1 = new ColonyStructureStatus();
            var status2 = new ColonyStructureStatus();
            structure.Statuses["Status1"] = status1;
            structure.Statuses["Status2"] = status2;

            Assert.AreEqual(2, structure.Statuses.Count);
            structure.Statuses.Clear();
            Assert.AreEqual(0, structure.Statuses.Count);
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

            Assert.AreEqual(50.0, structure.Statuses["Power"].PowerProvided);
            Assert.AreEqual(25.0, structure.Statuses["Power"].HabitationProvision);
            Assert.AreEqual(75.0, structure.Statuses["Power"].FoodProvision);
            Assert.AreEqual(30.0, structure.Statuses["Power"].EntertainmentProvided);
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
                Power = 100.5,
                Habitation = 50.0,
                Food = 75.25,
                Entertainment = 123.456,
                WarehouseCapacity = 1000,
                WorkersAssigned = 5,
                CurrentAttitude = "Happy",
                ContentmentIndex = 80,
                WageLevel = 2
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.AreEqual(structure.UUID, restored.UUID);
            Assert.AreEqual(structure.FlatpackBlueprintUUID, restored.FlatpackBlueprintUUID);
            Assert.AreEqual(structure.gameSequence, restored.gameSequence);
            Assert.AreEqual(structure.buildQueueSequence, restored.buildQueueSequence);
            Assert.AreEqual(structure.Power, restored.Power, 0.01);
            Assert.AreEqual(structure.Habitation, restored.Habitation, 0.0);
            Assert.AreEqual(structure.Food, restored.Food, 0.01);
            Assert.AreEqual(structure.Entertainment, restored.Entertainment, 0.01);
            Assert.AreEqual(structure.WarehouseCapacity, restored.WarehouseCapacity);
            Assert.AreEqual(structure.WorkersAssigned, restored.WorkersAssigned);
        }

        [Test]
        public void JsonRoundTrip_WithStatusEntries_Preserved()
        {
            var structure = new ColonyStructure();
            var status = new ColonyStructureStatus();
            status.PowerProvided = 50.0;
            status.HabitationProvision = 25.0;
            structure.Statuses["Power"] = status;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.AreEqual(1, restored.Statuses.Count);
            Assert.IsTrue(restored.Statuses.ContainsKey("Power"));
            Assert.AreEqual(50.0, restored.Statuses["Power"].PowerProvided, 0.01);
            Assert.AreEqual(25.0, restored.Statuses["Power"].HabitationProvision, 0.01);
        }

        [Test]
        public void JsonRoundTrip_WithPropertiesDictionary_Preserved()
        {
            var structure = new ColonyStructure();
            structure.Properties.setProperty("CustomProp1", "Value1");
            structure.Properties.setProperty("CustomProp2", "Value2");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.IsTrue(restored.Properties.ContainsKey("CustomProp1"));
            restored.Properties.getString("CustomProp1", null, out string value1);
            Assert.AreEqual("Value1", value1);
            Assert.IsTrue(restored.Properties.ContainsKey("CustomProp2"));
            restored.Properties.getString("CustomProp2", null, out string value2);
            Assert.AreEqual("Value2", value2);
        }

        [Test]
        public void JsonRoundTrip_WithAssignedWorkersDictionary_Preserved()
        {
            var structure = new ColonyStructure();
            structure.AssignedWorkers.setProperty("Worker1", "Engineer");
            structure.AssignedWorkers.setProperty("Worker2", "Scout");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.IsTrue(restored.AssignedWorkers.ContainsKey("Worker1"));
            restored.AssignedWorkers.getString("Worker1", null, out string value1);
            Assert.AreEqual("Engineer", value1);
            Assert.IsTrue(restored.AssignedWorkers.ContainsKey("Worker2"));
            restored.AssignedWorkers.getString("Worker2", null, out string value2);
            Assert.AreEqual("Scout", value2);
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
                Power = 250.75,
                Habitation = 125.5,
                Food = 88.25,
                Entertainment = 456.789,
                WarehouseCapacity = 5000,
                WorkersAssigned = 12,
                CurrentAttitude = "Satisfied",
                ContentmentIndex = 75,
                WageLevel = 3
            };

            structure.Statuses["Power"] = new ColonyStructureStatus { PowerProvided = 200.0, PowerRequired = 180.0 };
            structure.Statuses["Habitation"] = new ColonyStructureStatus { HabitationProvision = 100.0, HabitationRequired = 95.0 };
            structure.Statuses["Food"] = new ColonyStructureStatus { FoodProvision = 75.0, FoodRequired = 80.0 };
            structure.Statuses["Entertainment"] = new ColonyStructureStatus { EntertainmentProvided = 35.0, EntertainmentRequired = 30.0 };
            structure.Statuses["Warehouse"] = new ColonyStructureStatus { WarehouseCapacity = 4500.0, WarehouseRequired = 200.0 };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(structure);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<ColonyStructure>(json);

            Assert.AreEqual(structure.UUID, restored.UUID);
            Assert.AreEqual(structure.FlatpackBlueprintUUID, restored.FlatpackBlueprintUUID);
            Assert.AreEqual(structure.gameSequence, restored.gameSequence);
            Assert.AreEqual(structure.buildQueueSequence, restored.buildQueueSequence);
            Assert.AreEqual(structure.Power, restored.Power, 0.01);
            Assert.AreEqual(structure.Habitation, restored.Habitation, 0.0);
            Assert.AreEqual(structure.Food, restored.Food, 0.01);
            Assert.AreEqual(structure.Entertainment, restored.Entertainment, 0.01);
            Assert.AreEqual(structure.WarehouseCapacity, restored.WarehouseCapacity);
            Assert.AreEqual(structure.WorkersAssigned, restored.WorkersAssigned);

            Assert.AreEqual(5, restored.Statuses.Count);
            Assert.AreEqual(200.0, restored.Statuses["Power"].PowerProvided, 0.01);
            Assert.AreEqual(100.0, restored.Statuses["Habitation"].HabitationProvision, 0.0);
        }

        // -----------------------------------------------------------------------
        // Edge Cases and Defaults
        // -----------------------------------------------------------------------

        [Test]
        public void NewStructure_HasEmptyStringForAttitude()
        {
            var structure = new ColonyStructure();
            Assert.AreEqual("", structure.CurrentAttitude);
        }

        [Test]
        public void NewStructure_HasZeroContentmentIndex()
        {
            var structure = new ColonyStructure();
            Assert.AreEqual(0, structure.ContentmentIndex);
        }

        [Test]
        public void NewStructure_HasZeroWorkersAssigned()
        {
            var structure = new ColonyStructure();
            Assert.AreEqual(0, structure.WorkersAssigned);
        }

        [Test]
        public void NewStructure_HasZeroWarehouseCapacity()
        {
            var structure = new ColonyStructure();
            Assert.AreEqual(0.0, structure.WarehouseCapacity);
        }
    }
}