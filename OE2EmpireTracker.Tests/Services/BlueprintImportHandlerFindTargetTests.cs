using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using BpModel = OE2EmpireTracker.Models.Blueprint;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Unit tests for BlueprintImportHandler.FindTarget.
    /// Validates: Requirement 6.4, 6.5 (selected match, dedup match, no match routing)
    /// </summary>
    [TestFixture]
    public class BlueprintImportHandlerFindTargetTests
    {
        private PlayerContext playerContext;
        private EmpireContext empireContext;
        private static readonly string TestPlayerUUID = "test-player-uuid-001";

        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;
        private string _tempBaselineDataPath;
        private string _tempPlayerDataPath;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            _tempBaselineDataPath = Path.Combine(Path.GetTempPath(), "FindTargetTest_BaselineData.json");
            _tempPlayerDataPath = Path.Combine(Path.GetTempPath(), "FindTargetTest_PlayerData.json");
            File.Copy(TestHelper.TestDataPath("BaselineData.json"), _tempBaselineDataPath, true);
            File.Copy(TestHelper.TestDataPath("PlayerData.json"), _tempPlayerDataPath, true);

            EmpireContext.FilePath = _tempBaselineDataPath;
            PlayerContext.FilePath = _tempPlayerDataPath;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            EmpireContext.Reset();
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
        // Selected Match Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Selected match: Name + Evolution match, same BluePrintType.
        /// FindTarget should return the selected blueprint.
        /// </summary>
        [Test]
        public void FindTarget_SelectedMatch_ExactTypeMatch()
        {
            var selected = MakeBlueprint("AMX-SS Reactor Core", "sel-uuid-1", "Reactor", 2);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("AMX-SS Reactor Core", null, "Reactor", 2);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.SameAs(selected));
            Assert.That(result.IsNew, Is.False);
            Assert.That(result.IsSelectedMatch, Is.True);
            Assert.That(result.IsGlobal, Is.True);
        }

        /// <summary>
        /// Selected match: Name + Evolution match, selected has empty BluePrintType.
        /// The relaxed match allows empty type on the existing blueprint.
        /// </summary>
        [Test]
        public void FindTarget_SelectedMatch_EmptyTypeOnSelected()
        {
            var selected = MakeBlueprint("Mining Laser", "sel-uuid-2", null, 0);
            selected.BluePrintType = null;
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("Mining Laser", null, "MiningLaser", 0);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.SameAs(selected));
            Assert.That(result.IsNew, Is.False);
            Assert.That(result.IsSelectedMatch, Is.True);
        }

        /// <summary>
        /// Selected match: selected is in the player list, not global.
        /// IsGlobal should be false.
        /// </summary>
        [Test]
        public void FindTarget_SelectedMatch_PlayerBlueprint_IsGlobalFalse()
        {
            var selected = MakeBlueprint("Fighter Hull", "sel-uuid-3", "Hull", 3);
            playerContext.BlueprintList.Add(selected);

            var incoming = MakeBlueprint("Fighter Hull", null, "Hull", 3);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.SameAs(selected));
            Assert.That(result.IsSelectedMatch, Is.True);
            Assert.That(result.IsGlobal, Is.False);
        }

        /// <summary>
        /// No selected match when Name differs.
        /// Should fall through to dedup/new path.
        /// </summary>
        [Test]
        public void FindTarget_NoSelectedMatch_NameDiffers()
        {
            var selected = MakeBlueprint("Reactor Core", "sel-uuid-4", "Reactor", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("Shield Generator", null, "Shield", 0);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.IsSelectedMatch, Is.False);
            // Should not match the selected blueprint
            Assert.That(result.Target, Is.Not.SameAs(selected));
        }

        /// <summary>
        /// No selected match when Evolution differs.
        /// </summary>
        [Test]
        public void FindTarget_NoSelectedMatch_EvolutionDiffers()
        {
            var selected = MakeBlueprint("AMX-SS Reactor Core", "sel-uuid-5", "Reactor", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("AMX-SS Reactor Core", null, "Reactor", 3);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.IsSelectedMatch, Is.False);
        }

        /// <summary>
        /// No selected match when BluePrintType differs and selected type is not empty.
        /// </summary>
        [Test]
        public void FindTarget_NoSelectedMatch_TypeDiffers()
        {
            var selected = MakeBlueprint("Reactor Core", "sel-uuid-6", "Reactor", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("Reactor Core", null, "Shield", 0);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.IsSelectedMatch, Is.False);
        }

        /// <summary>
        /// No selected match when selected has no UUID (blank/new blueprint).
        /// </summary>
        [Test]
        public void FindTarget_NoSelectedMatch_SelectedHasNoUUID()
        {
            var selected = new BpModel("AMX-SS Reactor Core");
            selected.UUID = null;
            selected.BluePrintType = "Reactor";
            selected.Evolution = 0;

            var incoming = MakeBlueprint("AMX-SS Reactor Core", null, "Reactor", 0);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.IsSelectedMatch, Is.False);
        }

        // -----------------------------------------------------------------------
        // Dedup Match Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Dedup match: no selected match, but FindByDedupKey finds an existing
        /// blueprint in the global list (Evo 0 routes to global).
        /// </summary>
        [Test]
        public void FindTarget_DedupMatch_GlobalList_Evo0()
        {
            var selected = MakeBlueprint("Unrelated Blueprint", "sel-uuid-10", "Hull", 5);
            playerContext.BlueprintList.Add(selected);

            var existing = MakeBlueprint("AMX-SS Reactor Core", "dedup-uuid-1", "Reactor", 0, 1, null);
            empireContext.GlobalBlueprintList.Add(existing);

            var incoming = MakeBlueprint("AMX-SS Reactor Core", null, "Reactor", 0, 1, null);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.SameAs(existing));
            Assert.That(result.IsNew, Is.False);
            Assert.That(result.IsSelectedMatch, Is.False);
            Assert.That(result.IsGlobal, Is.True);
        }

        /// <summary>
        /// Dedup match: no selected match, FindByDedupKey finds an existing
        /// blueprint in the player list (Evo > 0 with current player).
        /// </summary>
        [Test]
        public void FindTarget_DedupMatch_PlayerList_EvoNonZero()
        {
            var selected = MakeBlueprint("Unrelated Blueprint", "sel-uuid-11", "Hull", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var existing = MakeBlueprint("Fighter Hull", "dedup-uuid-2", "Hull", 3, 2, "MilSpec");
            playerContext.BlueprintList.Add(existing);

            var incoming = MakeBlueprint("Fighter Hull", null, "Hull", 3, 2, "MilSpec");

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.SameAs(existing));
            Assert.That(result.IsNew, Is.False);
            Assert.That(result.IsSelectedMatch, Is.False);
            Assert.That(result.IsGlobal, Is.False);
        }

        // -----------------------------------------------------------------------
        // No Match Tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// No match: neither selected nor dedup match exists.
        /// Target should be null (caller creates new).
        /// </summary>
        [Test]
        public void FindTarget_NoMatch_ReturnsNullTarget()
        {
            var selected = MakeBlueprint("Unrelated Blueprint", "sel-uuid-20", "Hull", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("Brand New Reactor", null, "Reactor", 0, 1, null);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.Null);
            Assert.That(result.IsNew, Is.True);
            Assert.That(result.IsSelectedMatch, Is.False);
            Assert.That(result.IsGlobal, Is.True); // Evo 0 -> global route
        }

        /// <summary>
        /// No match for player route: Evo > 0 with current player, no dedup match.
        /// </summary>
        [Test]
        public void FindTarget_NoMatch_PlayerRoute_ReturnsNullTarget()
        {
            var selected = MakeBlueprint("Unrelated Blueprint", "sel-uuid-21", "Hull", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var incoming = MakeBlueprint("Brand New Shield", null, "Shield", 5, 3, "MilSpec");

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            Assert.That(result.Target, Is.Null);
            Assert.That(result.IsNew, Is.True);
            Assert.That(result.IsGlobal, Is.False); // Evo 5 + has player -> player route
        }

        /// <summary>
        /// No match: dedup key partially matches (same name but different evolution).
        /// Should not match.
        /// </summary>
        [Test]
        public void FindTarget_NoMatch_PartialDedupKey_DifferentEvolution()
        {
            var selected = MakeBlueprint("Unrelated Blueprint", "sel-uuid-22", "Hull", 0);
            empireContext.GlobalBlueprintList.Add(selected);

            var existing = MakeBlueprint("AMX-SS Reactor Core", "existing-uuid", "Reactor", 0, 1, null);
            empireContext.GlobalBlueprintList.Add(existing);

            // Same name but different evolution
            var incoming = MakeBlueprint("AMX-SS Reactor Core", null, "Reactor", 3, 1, null);

            var result = BlueprintImportHandler.FindTarget(incoming, selected, playerContext, empireContext);

            // Evo 3 with player -> player route, no match in player list
            Assert.That(result.Target, Is.Null);
            Assert.That(result.IsNew, Is.True);
        }
    }
}
