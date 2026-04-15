using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BlueprintImportHandler.MergeAndPersist.
    /// Validates: Requirement 6.6, 6.7 (additive merge, empty props skipped,
    /// new blueprint creation, resources merge)
    /// </summary>
    [TestFixture]
    public class BlueprintImportHandlerMergeTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private static readonly string TestPlayerUUID = "test-player-uuid-merge";

        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;
        private string _tempBaselineDataPath;
        private string _tempPlayerDataPath;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            _tempBaselineDataPath = Path.Combine(Path.GetTempPath(), "MergeTest_BaselineData.json");
            _tempPlayerDataPath = Path.Combine(Path.GetTempPath(), "MergeTest_PlayerData.json");
            File.Copy(TestHelper.TestDataPath("BaselineData.json"), _tempBaselineDataPath, true);
            File.Copy(TestHelper.TestDataPath("PlayerData.json"), _tempPlayerDataPath, true);

            EmpireContext.FilePath = _tempBaselineDataPath;
            PlayerContext.FilePath = _tempPlayerDataPath;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;

            if (File.Exists(_tempBaselineDataPath)) File.Delete(_tempBaselineDataPath);
            if (File.Exists(_tempPlayerDataPath)) File.Delete(_tempPlayerDataPath);
            if (File.Exists(_tempBaselineDataPath + ".bak")) File.Delete(_tempBaselineDataPath + ".bak");
            if (File.Exists(_tempPlayerDataPath + ".bak")) File.Delete(_tempPlayerDataPath + ".bak");
        }

        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            PlayerContext.Reset();
            empireContext = EmpireContext.GetInstance();
            playerContext = PlayerContext.GetInstance();

            empireContext.GlobalBlueprintList.Clear();
            playerContext.BlueprintList.Clear();
            playerContext.CurrentPlayerUUID = TestPlayerUUID;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static BpModel MakeBlueprint(
            string name, string uuid = null, string bpType = "Reactor",
            int evolution = 0, int cls = 1, string techLevel = null)
        {
            var bp = new BpModel(name);
            bp.UUID = uuid ?? Guid.NewGuid().ToString();
            bp.BluePrintType = bpType;
            bp.Evolution = evolution;
            bp.Class = cls;
            bp.TechLevel = techLevel;
            return bp;
        }

        // -----------------------------------------------------------------------
        // Additive Merge Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Additive merge: incoming properties overwrite existing keys,
        /// but existing keys not in incoming are preserved.
        /// </summary>
        [Test]
        public void MergeAndPersist_ExistingTarget_AdditivePropertyMerge()
        {
            var existing = MakeBlueprint("AMX-SS Reactor Core", "existing-uuid-1", "Reactor", 0);
            existing.Properties.setProperty("Power Output", "1200");
            existing.Properties.setProperty("Efficiency", "85");
            existing.Properties.setProperty("Weight", "50");
            empireContext.GlobalBlueprintList.Add(existing);

            var incoming = new BpModel("AMX-SS Reactor Core");
            incoming.BluePrintType = "Reactor";
            incoming.Evolution = 0;
            incoming.Properties = new PropertyBag();
            incoming.Properties.setProperty("Power Output", "1500"); // overwrite
            incoming.Properties.setProperty("Durability", "200");    // new key
            // "Efficiency" and "Weight" not in incoming — should be preserved

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = existing,
                IsGlobal = true,
                IsSelectedMatch = true
            };

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result, Is.SameAs(existing));
            Assert.That(result.Properties.Properties["Power Output"], Is.EqualTo("1500"), "Incoming overwrites existing key");
            Assert.That(result.Properties.Properties["Durability"], Is.EqualTo("200"), "New key added from incoming");
            Assert.That(result.Properties.Properties["Efficiency"], Is.EqualTo("85"), "Existing key not in incoming is preserved");
            Assert.That(result.Properties.Properties["Weight"], Is.EqualTo("50"), "Existing key not in incoming is preserved");
            Assert.That(result.Properties.Count, Is.EqualTo(4));
        }

        // -----------------------------------------------------------------------
        // Empty Props Skipped Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Empty props skipped: when incoming has zero properties,
        /// existing properties are fully preserved.
        /// </summary>
        [Test]
        public void MergeAndPersist_ExistingTarget_EmptyIncomingProps_PreservesExisting()
        {
            var existing = MakeBlueprint("Fighter Hull", "existing-uuid-2", "Hull", 3);
            existing.Properties.setProperty("Health", "500");
            existing.Properties.setProperty("Armor", "300");
            playerContext.BlueprintList.Add(existing);

            var incoming = new BpModel("Fighter Hull");
            incoming.BluePrintType = "Hull";
            incoming.Evolution = 3;
            incoming.Properties = new PropertyBag(); // empty — no properties
            incoming.Resources = new Dictionary<string, string>();

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = existing,
                IsGlobal = false,
                IsSelectedMatch = true
            };

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result, Is.SameAs(existing));
            Assert.That(result.Properties.Properties["Health"], Is.EqualTo("500"), "Existing property preserved when incoming is empty");
            Assert.That(result.Properties.Properties["Armor"], Is.EqualTo("300"), "Existing property preserved when incoming is empty");
            Assert.That(result.Properties.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Null incoming properties should also preserve existing properties.
        /// </summary>
        [Test]
        public void MergeAndPersist_ExistingTarget_NullIncomingProps_PreservesExisting()
        {
            var existing = MakeBlueprint("Shield Gen", "existing-uuid-3", "Shield", 0);
            existing.Properties.setProperty("Shield Strength", "800");
            empireContext.GlobalBlueprintList.Add(existing);

            var incoming = new BpModel("Shield Gen");
            incoming.BluePrintType = "Shield";
            incoming.Properties = null; // null properties
            incoming.Resources = new Dictionary<string, string>();

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = existing,
                IsGlobal = true,
                IsSelectedMatch = false
            };

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result, Is.SameAs(existing));
            Assert.That(result.Properties.Properties["Shield Strength"], Is.EqualTo("800"), "Existing property preserved when incoming props are null");
        }

        // -----------------------------------------------------------------------
        // New Blueprint Creation Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// New global blueprint: when target is null and IsGlobal is true,
        /// a new blueprint is created with a deterministic UUID and added to the global list.
        /// </summary>
        [Test]
        public void MergeAndPersist_NewGlobalBlueprint_AssignsDeterministicUUID()
        {
            var incoming = new BpModel("Brand New Reactor");
            incoming.BluePrintType = "Reactor";
            incoming.Evolution = 0;
            incoming.Class = 1;
            incoming.Properties = new PropertyBag();
            incoming.Properties.setProperty("Power Output", "1000");
            incoming.Resources = new Dictionary<string, string> { { "Iron", "100" } };

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = null,
                IsGlobal = true,
                IsSelectedMatch = false
            };

            int globalCountBefore = empireContext.GlobalBlueprintList.Count;

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result, Is.SameAs(incoming));
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty, "UUID should be assigned");
            Assert.That(result.OwnerUUID, Is.Null.Or.Empty, "Global blueprints should not have OwnerUUID");
            Assert.That(empireContext.GlobalBlueprintList.Count, Is.EqualTo(globalCountBefore + 1));
            Assert.That(empireContext.GlobalBlueprintList.Contains(result), Is.True);
        }

        /// <summary>
        /// New player blueprint: when target is null and IsGlobal is false,
        /// a new blueprint is created with a random UUID and OwnerUUID set.
        /// </summary>
        [Test]
        public void MergeAndPersist_NewPlayerBlueprint_AssignsRandomUUID_SetsOwner()
        {
            var incoming = new BpModel("Player Shield");
            incoming.BluePrintType = "Shield";
            incoming.Evolution = 3;
            incoming.Class = 2;
            incoming.Properties = new PropertyBag();
            incoming.Properties.setProperty("Shield Strength", "600");
            incoming.Resources = new Dictionary<string, string> { { "Copper", "50" } };

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = null,
                IsGlobal = false,
                IsSelectedMatch = false
            };

            int playerCountBefore = playerContext.BlueprintList.Count;

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result, Is.SameAs(incoming));
            Assert.That(result.UUID, Is.Not.Null.And.Not.Empty, "UUID should be assigned");
            Assert.That(result.OwnerUUID, Is.EqualTo(TestPlayerUUID), "OwnerUUID should be set to current player");
            Assert.That(playerContext.BlueprintList.Count, Is.EqualTo(playerCountBefore + 1));
            Assert.That(playerContext.BlueprintList.Contains(result), Is.True);
        }

        // -----------------------------------------------------------------------
        // Resources Merge Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Resources merge: incoming resources overwrite existing keys,
        /// but existing keys not in incoming are preserved.
        /// </summary>
        [Test]
        public void MergeAndPersist_ExistingTarget_AdditiveResourceMerge()
        {
            var existing = MakeBlueprint("AMX-SS Reactor Core", "existing-uuid-4", "Reactor", 0);
            existing.Resources = new Dictionary<string, string>
            {
                { "Iron", "500" },
                { "Copper", "200" },
                { "Gold", "50" }
            };
            empireContext.GlobalBlueprintList.Add(existing);

            var incoming = new BpModel("AMX-SS Reactor Core");
            incoming.BluePrintType = "Reactor";
            incoming.Properties = new PropertyBag();
            incoming.Resources = new Dictionary<string, string>
            {
                { "Iron", "600" },     // overwrite
                { "Titanium", "100" }  // new key
            };
            // "Copper" and "Gold" not in incoming — should be preserved

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = existing,
                IsGlobal = true,
                IsSelectedMatch = true
            };

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result.Resources["Iron"], Is.EqualTo("600"), "Incoming overwrites existing resource");
            Assert.That(result.Resources["Titanium"], Is.EqualTo("100"), "New resource added from incoming");
            Assert.That(result.Resources["Copper"], Is.EqualTo("200"), "Existing resource not in incoming is preserved");
            Assert.That(result.Resources["Gold"], Is.EqualTo("50"), "Existing resource not in incoming is preserved");
            Assert.That(result.Resources.Count, Is.EqualTo(4));
        }

        /// <summary>
        /// Empty incoming resources should preserve existing resources.
        /// </summary>
        [Test]
        public void MergeAndPersist_ExistingTarget_EmptyIncomingResources_PreservesExisting()
        {
            var existing = MakeBlueprint("Mining Laser", "existing-uuid-5", "MiningLaser", 0);
            existing.Resources = new Dictionary<string, string>
            {
                { "Iron", "300" },
                { "Silicon", "150" }
            };
            empireContext.GlobalBlueprintList.Add(existing);

            var incoming = new BpModel("Mining Laser");
            incoming.BluePrintType = "MiningLaser";
            incoming.Properties = new PropertyBag();
            incoming.Properties.setProperty("Mining Rate", "10");
            incoming.Resources = new Dictionary<string, string>(); // empty

            var findResult = new BlueprintImportHandler.FindTargetResult
            {
                Target = existing,
                IsGlobal = true,
                IsSelectedMatch = false
            };

            var result = BlueprintImportHandler.MergeAndPersist(findResult, incoming, playerContext, empireContext);

            Assert.That(result.Resources["Iron"], Is.EqualTo("300"), "Existing resource preserved when incoming is empty");
            Assert.That(result.Resources["Silicon"], Is.EqualTo("150"), "Existing resource preserved when incoming is empty");
            Assert.That(result.Resources.Count, Is.EqualTo(2));
        }
    }
}
