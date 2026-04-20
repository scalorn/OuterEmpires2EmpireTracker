using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Parsers
{
    /// <summary>
    /// Bug condition exploration tests for colony structure deduplication.
    /// These tests verify that manually-added structures (displaySequence=0) are
    /// merged with parsed buildings rather than duplicated on import.
    ///
    /// **Validates: Requirements 1.1, 1.2, 1.3, 1.4**
    /// Property 1: Bug Condition -- Manual structures duplicated on import
    /// </summary>
    [TestFixture]
    public class ColonyParserDedupeTests
    {
        private ColonyParser _parser;
        private EmpireContext _empireContext;
        private string _originalEmpireFilePath;
        private string _originalPlayerFilePath;

        [SetUp]
        public void SetUp()
        {
            _parser = new ColonyParser();
            _originalEmpireFilePath = EmpireContext.FilePath;
            _originalPlayerFilePath = PlayerContext.FilePath;

            EmpireContext.Reset();
            TestHelper.SetEmpireFilePath();
            PlayerContext.FilePath = "nonexistent_player_data.json";
            _empireContext = EmpireContext.GetInstance();
        }

        [TearDown]
        public void TearDown()
        {
            EmpireContext.Reset();
            EmpireContext.FilePath = _originalEmpireFilePath;
            PlayerContext.FilePath = _originalPlayerFilePath;
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }

        private static string ExtractFragment(string clipboardData)
        {
            return OE2EmpireTracker.Parsers.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        /// <summary>
        /// Parses M1 HTML into a fresh colony to discover the baseline structure set.
        /// </summary>
        private Colony ParseM1Fresh()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }

        // -------------------------------------------------------------------
        // Test case 1 -- Full overlap: manual structures for every blueprint
        // type present in M1, then import M1 HTML
        // -------------------------------------------------------------------

        [Test]
        public void FullOverlap_ManualStructures()
        {
            // First, parse M1 into a fresh colony to discover the structure set
            var reference = ParseM1Fresh();
            Assert.That(reference.Structures.Count, Is.EqualTo(43),
                "Baseline: M1 should have 43 structures");

            // Group by FlatpackBlueprintUUID to get counts per blueprint type
            var countsByBlueprint = reference.Structures
                .GroupBy(s => s.FlatpackBlueprintUUID)
                .ToDictionary(g => g.Key, g => g.Count());

            // Create a new colony with manually-added structures (displaySequence=0)
            // matching the exact counts from M1
            var colony = new Colony();
            foreach (var kvp in countsByBlueprint)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    colony.Structures.Add(new ColonyStructure
                    {
                        UUID = Guid.NewGuid().ToString(),
                        FlatpackBlueprintUUID = kvp.Key,
                        displaySequence = 0  // Manual structures have displaySequence=0
                    });
                }
            }

            Assert.That(colony.Structures.Count, Is.EqualTo(43),
                "Pre-import: manual structures should match M1 count");

            // Now import M1 HTML -- on fixed code, structures should merge (count stays 43)
            // On UNFIXED code, this will FAIL because manual structures (displaySequence=0)
            // never match parsed buildings (buildingID > 0), so all 43 are appended as duplicates
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            _parser.ProcessHtml(colony, html, _empireContext);

            Assert.That(colony.Structures.Count, Is.EqualTo(43),
                $"After import: expected 43 structures (merged), but got {colony.Structures.Count}. " +
                "Bug: manual structures (displaySequence=0) were not matched to parsed buildings.");
        }

        // -------------------------------------------------------------------
        // Test case 2 -- Partial overlap: 1 Mining Rig manually, then import M1
        // -------------------------------------------------------------------

        [Test]
        public void PartialOverlap_SingleMiningRig()
        {
            // Parse M1 fresh to discover the Mining Rig flatpack UUID and count
            var reference = ParseM1Fresh();
            var miningRigUUID = reference.Structures
                .Where(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                .Select(s => new { s.FlatpackBlueprintUUID, Name = _empireContext.FindGlobalBlueprint(s.FlatpackBlueprintUUID)?.Name })
                .FirstOrDefault(x => x.Name == "Mining Rig Flatpack")?.FlatpackBlueprintUUID;

            Assert.That(miningRigUUID, Is.Not.Null, "M1 should contain Mining Rig structures");

            int miningRigCountInM1 = reference.Structures
                .Count(s => s.FlatpackBlueprintUUID == miningRigUUID);

            Assert.That(miningRigCountInM1, Is.GreaterThan(0),
                "M1 should have at least one Mining Rig");

            // Create a colony with 1 manually-added Mining Rig (displaySequence=0)
            var colony = new Colony();
            colony.Structures.Add(new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = miningRigUUID,
                displaySequence = 0  // Manual structure
            });

            // Import M1 HTML
            // On fixed code: the manual Mining Rig merges with the first parsed one,
            // total Mining Rig count = miningRigCountInM1
            // On UNFIXED code: manual Mining Rig (displaySequence=0) doesn't match any
            // parsed Mining Rig (buildingID > 0), so total = miningRigCountInM1 + 1
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            _parser.ProcessHtml(colony, html, _empireContext);

            int miningRigCountAfterImport = colony.Structures
                .Count(s => s.FlatpackBlueprintUUID == miningRigUUID);

            Assert.That(miningRigCountAfterImport, Is.EqualTo(miningRigCountInM1),
                $"After import: expected {miningRigCountInM1} Mining Rigs (merged), " +
                $"but got {miningRigCountAfterImport}. " +
                "Bug: manual Mining Rig (displaySequence=0) was not matched to parsed Mining Rig.");
        }

        // -------------------------------------------------------------------
        // Test case 3 -- Control: no manual structures, import M1 into empty colony
        // -------------------------------------------------------------------

        [Test]
        public void NoManualStructures_Control()
        {
            // Import M1 into an empty colony -- this is the non-bug path
            // Should produce exactly 43 structures on both fixed and unfixed code
            var colony = ParseM1Fresh();

            Assert.That(colony.Structures.Count, Is.EqualTo(43),
                "Control: importing M1 into empty colony should produce 43 structures");
        }

        // ===================================================================
        // PRESERVATION PROPERTY TESTS
        // Property 2: Preservation -- Empty colony and idempotent import
        // behavior unchanged
        // **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
        //
        // These tests capture baseline behavior on UNFIXED code so we can
        // verify the fix does not regress any existing functionality.
        // ===================================================================

        // -------------------------------------------------------------------
        // Preservation: M1 empty colony import produces 43 structures with
        // correct FlatpackBlueprintUUIDs, displaySequence > 0, properties,
        // mining resources, and worker assignments
        // Validates: Requirements 3.1, 3.5
        // -------------------------------------------------------------------

        [Test]
        public void Preservation_M1_EmptyColonyImport_StructureCount()
        {
            var colony = ParseM1Fresh();
            Assert.That(colony.Structures.Count, Is.EqualTo(43),
                "M1 import into empty colony should produce 43 structures");
        }

        [Test]
        public void Preservation_M1_EmptyColonyImport_AllHaveFlatpackUUIDs()
        {
            var colony = ParseM1Fresh();
            foreach (var s in colony.Structures)
            {
                Assert.That(s.FlatpackBlueprintUUID, Is.Not.Null.And.Not.Empty,
                    $"Structure with displaySequence={s.displaySequence} should have a FlatpackBlueprintUUID");
            }
        }

        [Test]
        public void Preservation_M1_EmptyColonyImport_AllHaveDisplaySequenceGtZero()
        {
            var colony = ParseM1Fresh();
            foreach (var s in colony.Structures)
            {
                Assert.That(s.displaySequence, Is.GreaterThan(0),
                    $"Structure with UUID={s.UUID} should have displaySequence > 0");
            }
        }

        [Test]
        public void Preservation_M1_EmptyColonyImport_AllHaveProperties()
        {
            var colony = ParseM1Fresh();
            foreach (var s in colony.Structures)
            {
                Assert.That(s.Properties.Count, Is.GreaterThan(0),
                    $"Structure displaySequence={s.displaySequence} should have at least one property");
            }
        }

        [Test]
        public void Preservation_M1_EmptyColonyImport_MiningRigsHaveResources()
        {
            var colony = ParseM1Fresh();
            var miningRigs = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.MiningSurveyResource))
                .ToList();

            Assert.That(miningRigs.Count, Is.GreaterThan(0),
                "M1 should have at least one structure with mining resource info");

            foreach (var rig in miningRigs)
            {
                Assert.That(rig.MiningSurveyResource, Is.Not.Null.And.Not.Empty,
                    $"Mining rig displaySequence={rig.displaySequence} should have MiningSurveyResource");
            }
        }

        [Test]
        public void Reimport_M1_MiningResourceUpdatedFromGame()
        {
            // First import
            var colony = ParseM1Fresh();
            var miningRigs = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.MiningSurveyResource))
                .ToList();
            Assert.That(miningRigs.Count, Is.GreaterThan(0),
                "M1 should have at least one mining rig");

            // Tamper: clear mining resource on all rigs to simulate stale data
            foreach (var rig in miningRigs)
            {
                rig.MiningSurveyResource = null;
                rig.RefiningResourcePurity = null;
            }

            // Reimport same HTML -- game is authoritative, should restore resources
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            _parser.ProcessHtml(colony, html, _empireContext);

            var rigsAfter = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.MiningSurveyResource))
                .ToList();

            Assert.That(rigsAfter.Count, Is.EqualTo(miningRigs.Count),
                "After reimport, all mining rigs should have their resource restored");
        }

        [Test]
        public void Reimport_M1_MiningResourceOverwrittenByGame()
        {
            // First import
            var colony = ParseM1Fresh();
            var miningRigs = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.MiningSurveyResource))
                .ToList();
            Assert.That(miningRigs.Count, Is.GreaterThan(0));

            // Tamper: set a fake resource to simulate the player reassigning in-game
            string originalResource = miningRigs[0].MiningSurveyResource;
            miningRigs[0].MiningSurveyResource = "Fake Resource";
            miningRigs[0].RefiningResourcePurity = "Low";

            // Reimport -- game value should overwrite the fake
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            _parser.ProcessHtml(colony, html, _empireContext);

            Assert.That(miningRigs[0].MiningSurveyResource, Is.EqualTo(originalResource),
                "After reimport, mining resource should be overwritten by game value");
        }

        [Test]
        public void Preservation_M1_EmptyColonyImport_WorkerAssignments()
        {
            var colony = ParseM1Fresh();
            var withWorkers = colony.Structures
                .Where(s => s.AssignedWorkers.Count > 0)
                .ToList();

            Assert.That(withWorkers.Count, Is.GreaterThan(0),
                "M1 should have at least one structure with worker assignments");
        }

        // -------------------------------------------------------------------
        // Preservation: M1 idempotency -- importing twice produces same 43
        // structures with identical values
        // Validates: Requirements 3.2
        // -------------------------------------------------------------------

        [Test]
        public void Preservation_M1_Idempotency_StructureCountUnchanged()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            int countAfterFirst = colony.Structures.Count;

            _parser.ProcessHtml(colony, html, _empireContext);
            Assert.That(colony.Structures.Count, Is.EqualTo(countAfterFirst),
                "M1 idempotency: structure count should not change after second import");
        }

        [Test]
        public void Preservation_M1_Idempotency_FlatpackUUIDsPreserved()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            var snapshotUUIDs = colony.Structures
                .Select(s => s.FlatpackBlueprintUUID)
                .OrderBy(u => u)
                .ToList();

            _parser.ProcessHtml(colony, html, _empireContext);
            var afterUUIDs = colony.Structures
                .Select(s => s.FlatpackBlueprintUUID)
                .OrderBy(u => u)
                .ToList();

            Assert.That(afterUUIDs, Is.EqualTo(snapshotUUIDs),
                "M1 idempotency: FlatpackBlueprintUUIDs should be identical after second import");
        }

        [Test]
        public void Preservation_M1_Idempotency_DisplaySequenceValuesPreserved()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            var snapshotSeqs = colony.Structures
                .Select(s => s.displaySequence)
                .OrderBy(g => g)
                .ToList();

            _parser.ProcessHtml(colony, html, _empireContext);
            var afterSeqs = colony.Structures
                .Select(s => s.displaySequence)
                .OrderBy(g => g)
                .ToList();

            Assert.That(afterSeqs, Is.EqualTo(snapshotSeqs),
                "M1 idempotency: displaySequence values should be identical after second import");
        }

        // -------------------------------------------------------------------
        // Preservation: VI-1 non-local colony workers fallback produces
        // structures with correct flatpack UUIDs
        // Validates: Requirements 3.3
        // -------------------------------------------------------------------

        [Test]
        public void Preservation_VI1_WorkersFallback_StructuresCreated()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranVI-1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            Assert.That(colony.Structures.Count, Is.GreaterThan(0),
                "VI-1 import should create at least one structure via workers fallback");
        }

        [Test]
        public void Preservation_VI1_WorkersFallback_FlatpackUUIDs()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranVI-1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            var withUUIDs = colony.Structures
                .Where(s => !string.IsNullOrEmpty(s.FlatpackBlueprintUUID))
                .ToList();

            Assert.That(withUUIDs.Count, Is.GreaterThan(0),
                "VI-1 workers fallback should produce structures with FlatpackBlueprintUUIDs");
        }

        // -------------------------------------------------------------------
        // Preservation: M2-2 commodity demands with correct names, amounts,
        // and fulfilled status
        // Validates: Requirements 3.4
        // -------------------------------------------------------------------

        [Test]
        public void Preservation_M2_2_CommodityDemandsExist()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            Assert.That(colony.Commodities.Count, Is.GreaterThan(0),
                "M2-2 import should produce commodity demands");
        }

        [Test]
        public void Preservation_M2_2_CommodityDemandValues()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            foreach (var c in colony.Commodities)
            {
                Assert.That(c.Name, Is.Not.Null.And.Not.Empty,
                    "Each commodity demand should have a name");
                Assert.That(c.Requested, Is.GreaterThan(0),
                    $"Commodity '{c.Name}' should have Requested > 0");
            }
        }

        // -------------------------------------------------------------------
        // Property-based preservation: for ALL colony HTML test files,
        // importing into an empty colony and then re-importing produces
        // identical structure counts, FlatpackBlueprintUUIDs, and property
        // values
        // Validates: Requirements 3.1, 3.2, 3.5
        // -------------------------------------------------------------------

        private static readonly string[] AllColonyFiles = new[]
        {
            "ClnyHexAdministrationTabZehVazoranIIM1.html",
            "ClnyHexAdministrationTabZehVazoranIIM2.html",
            "ClnyHexAdministrationTabZehVazoranIIM2-2.html",
            "ClnyHexAdministrationTabZehVazoranIIM2-3.html",
            "ClnyHexAdministrationTabZehVazoranVI-1.html"
        };

        [Test]
        public void Preservation_AllFiles_ReimportPreservesStructureCounts()
        {
            // **Validates: Requirements 3.1, 3.2**
            // Property: for all colony HTML test files, importing into an empty
            // colony and then re-importing produces identical structure counts
            foreach (string filename in AllColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);
                int countAfterFirst = colony.Structures.Count;

                _parser.ProcessHtml(colony, html, _empireContext);
                Assert.That(colony.Structures.Count, Is.EqualTo(countAfterFirst),
                    $"[{filename}] Structure count changed after re-import: " +
                    $"first={countAfterFirst}, second={colony.Structures.Count}");
            }
        }

        [Test]
        public void Preservation_AllFiles_ReimportPreservesFlatpackUUIDs()
        {
            // **Validates: Requirements 3.1, 3.2**
            // Property: for all colony HTML test files, re-importing preserves
            // the exact set of FlatpackBlueprintUUIDs
            foreach (string filename in AllColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);
                var snapshotUUIDs = colony.Structures
                    .Select(s => s.FlatpackBlueprintUUID)
                    .OrderBy(u => u)
                    .ToList();

                _parser.ProcessHtml(colony, html, _empireContext);
                var afterUUIDs = colony.Structures
                    .Select(s => s.FlatpackBlueprintUUID)
                    .OrderBy(u => u)
                    .ToList();

                Assert.That(afterUUIDs, Is.EqualTo(snapshotUUIDs),
                    $"[{filename}] FlatpackBlueprintUUIDs changed after re-import");
            }
        }

        [Test]
        public void Preservation_AllFiles_ReimportPreservesPropertyValues()
        {
            // **Validates: Requirements 3.5**
            // Property: for all colony HTML test files, re-importing preserves
            // all structure property key-value pairs
            foreach (string filename in AllColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);

                // Snapshot: for each structure, capture sorted property key-value pairs
                var snapshot = colony.Structures
                    .OrderBy(s => s.displaySequence)
                    .ThenBy(s => s.FlatpackBlueprintUUID)
                    .Select(s => s.Properties.Properties
                        .OrderBy(p => p.Key)
                        .Select(p => p.Key + "=" + p.Value)
                        .ToList())
                    .ToList();

                _parser.ProcessHtml(colony, html, _empireContext);

                var afterProps = colony.Structures
                    .OrderBy(s => s.displaySequence)
                    .ThenBy(s => s.FlatpackBlueprintUUID)
                    .Select(s => s.Properties.Properties
                        .OrderBy(p => p.Key)
                        .Select(p => p.Key + "=" + p.Value)
                        .ToList())
                    .ToList();

                Assert.That(afterProps.Count, Is.EqualTo(snapshot.Count),
                    $"[{filename}] Structure count changed after re-import");

                for (int i = 0; i < snapshot.Count; i++)
                {
                    Assert.That(afterProps[i], Is.EqualTo(snapshot[i]),
                        $"[{filename}] Properties changed at structure index {i} after re-import");
                }
            }
        }

        // -------------------------------------------------------------------
        // Property-based preservation: for ALL colony HTML test files with
        // commodity demands, re-importing preserves commodity count, names,
        // requested amounts, and fulfilled status
        // Validates: Requirements 3.4
        // -------------------------------------------------------------------

        [Test]
        public void Preservation_AllFiles_ReimportPreservesCommodityDemands()
        {
            // **Validates: Requirements 3.4**
            // Property: for all colony HTML test files with commodity demands,
            // re-importing preserves commodity count, names, requested amounts,
            // and fulfilled status
            int filesWithCommodities = 0;

            foreach (string filename in AllColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);

                if (colony.Commodities.Count == 0)
                    continue;

                filesWithCommodities++;

                // Snapshot commodity values
                var snapshot = colony.Commodities
                    .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                    .OrderBy(c => c.Name)
                    .ToList();

                _parser.ProcessHtml(colony, html, _empireContext);

                var afterCommodities = colony.Commodities
                    .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                    .OrderBy(c => c.Name)
                    .ToList();

                Assert.That(afterCommodities.Count, Is.EqualTo(snapshot.Count),
                    $"[{filename}] Commodity count changed after re-import: " +
                    $"first={snapshot.Count}, second={afterCommodities.Count}");

                for (int i = 0; i < snapshot.Count; i++)
                {
                    Assert.That(afterCommodities[i].Name, Is.EqualTo(snapshot[i].Name),
                        $"[{filename}] Commodity name mismatch at index {i}");
                    Assert.That(afterCommodities[i].Requested, Is.EqualTo(snapshot[i].Requested),
                        $"[{filename}] Requested mismatch for '{snapshot[i].Name}'");
                    Assert.That(afterCommodities[i].Fulfilled, Is.EqualTo(snapshot[i].Fulfilled),
                        $"[{filename}] Fulfilled mismatch for '{snapshot[i].Name}'");
                }
            }

            Assert.That(filesWithCommodities, Is.GreaterThan(0),
                "At least one colony file should have commodity demands for this test to be meaningful");
        }
    }
}
