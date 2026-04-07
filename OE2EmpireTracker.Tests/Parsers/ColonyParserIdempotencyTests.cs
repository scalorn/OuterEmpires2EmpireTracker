using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Models;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Parsers
{
    [TestFixture]
    public class ColonyParserIdempotencyTests
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
            _empireContext = EmpireContext.getInstance();
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
            return OE2EmpireTracker.Forms.Blueprint.BlueprintScanner
                .ExtractHtmlFragmentFromClipboardData(clipboardData);
        }

        private Colony ParseColonyFromFile(string filename)
        {
            string clipboardData = LoadTestData(filename);
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();
            _parser.ProcessHtml(colony, html, _empireContext);
            return colony;
        }

        // -------------------------------------------------------------------
        // M1 structure idempotency tests (Requirements 2.1, 2.2, 2.3)
        // -------------------------------------------------------------------

        [Test]
        public void M1_ParseTwice_StructureCountUnchanged()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            int countAfterFirst = colony.Structures.Count;

            _parser.ProcessHtml(colony, html, _empireContext);
            Assert.That(colony.Structures.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void M1_ParseThreeTimes_StructureCountUnchanged()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            int countAfterFirst = colony.Structures.Count;

            _parser.ProcessHtml(colony, html, _empireContext);
            _parser.ProcessHtml(colony, html, _empireContext);
            Assert.That(colony.Structures.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void M1_ParseTwice_StructureValuesPreserved()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM1.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            // Snapshot values after first parse
            var snapshot = colony.Structures
                .Select(s => new { s.FlatpackBlueprintUUID, s.gameSequence })
                .OrderBy(s => s.gameSequence)
                .ToList();

            _parser.ProcessHtml(colony, html, _empireContext);

            // Verify values unchanged after second parse
            var afterSecond = colony.Structures
                .Select(s => new { s.FlatpackBlueprintUUID, s.gameSequence })
                .OrderBy(s => s.gameSequence)
                .ToList();

            Assert.That(afterSecond.Count, Is.EqualTo(snapshot.Count));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Assert.That(afterSecond[i].FlatpackBlueprintUUID, Is.EqualTo(snapshot[i].FlatpackBlueprintUUID),
                    $"FlatpackBlueprintUUID mismatch at index {i}");
                Assert.That(afterSecond[i].gameSequence, Is.EqualTo(snapshot[i].gameSequence),
                    $"gameSequence mismatch at index {i}");
            }
        }

        // -------------------------------------------------------------------
        // M2-2 commodity idempotency tests (Requirements 3.1, 3.2, 3.3)
        // -------------------------------------------------------------------

        [Test]
        public void M2_2_ParseTwice_CommodityCountUnchanged()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            int countAfterFirst = colony.Commodities.Count;

            _parser.ProcessHtml(colony, html, _empireContext);
            Assert.That(colony.Commodities.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void M2_2_ParseThreeTimes_CommodityCountUnchanged()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);
            int countAfterFirst = colony.Commodities.Count;

            _parser.ProcessHtml(colony, html, _empireContext);
            _parser.ProcessHtml(colony, html, _empireContext);
            Assert.That(colony.Commodities.Count, Is.EqualTo(countAfterFirst));
        }

        [Test]
        public void M2_2_ParseTwice_CommodityValuesPreserved()
        {
            string clipboardData = LoadTestData("ClnyHexAdministrationTabZehVazoranIIM2-2.html");
            string html = ExtractFragment(clipboardData);
            var colony = new Colony();

            _parser.ProcessHtml(colony, html, _empireContext);

            // Snapshot values after first parse
            var snapshot = colony.Commodities
                .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                .OrderBy(c => c.Name)
                .ToList();

            _parser.ProcessHtml(colony, html, _empireContext);

            // Verify values unchanged after second parse
            var afterSecond = colony.Commodities
                .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                .OrderBy(c => c.Name)
                .ToList();

            Assert.That(afterSecond.Count, Is.EqualTo(snapshot.Count));
            for (int i = 0; i < snapshot.Count; i++)
            {
                Assert.That(afterSecond[i].Name, Is.EqualTo(snapshot[i].Name),
                    $"Commodity name mismatch at index {i}");
                Assert.That(afterSecond[i].Requested, Is.EqualTo(snapshot[i].Requested),
                    $"Requested mismatch for {snapshot[i].Name}");
                Assert.That(afterSecond[i].Fulfilled, Is.EqualTo(snapshot[i].Fulfilled),
                    $"Fulfilled mismatch for {snapshot[i].Name}");
            }
        }

        // -------------------------------------------------------------------
        // AllColonyFiles sweep property tests (Requirements 2.1–2.4, 3.1–3.4)
        // -------------------------------------------------------------------

        private static readonly string[] ColonyFiles = new[]
        {
            "ClnyHexAdministrationTabZehVazoranIIM1.html",
            "ClnyHexAdministrationTabZehVazoranIIM2.html",
            "ClnyHexAdministrationTabZehVazoranIIM2-2.html",
            "ClnyHexAdministrationTabZehVazoranIIM2-3.html",
            "ClnyHexAdministrationTabZehVazoranVI-1.html"
        };

        [Test]
        public void AllColonyFiles_ParseTwice_StructureCountUnchanged()
        {
            // Feature: parser-idempotency-tests, Property 3: Colony structure count idempotency
            // Feature: parser-idempotency-tests, Property 4: Colony structure values stability
            // Validates: Requirements 2.1, 2.2, 2.3, 2.4
            foreach (string filename in ColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);
                int countAfterFirst = colony.Structures.Count;

                // Some colony files may legitimately have 0 structures;
                // idempotency still holds (0 == 0), but skip value checks.

                // Snapshot structure values after first parse
                var snapshot = colony.Structures
                    .Select(s => new { s.FlatpackBlueprintUUID, s.gameSequence })
                    .OrderBy(s => s.gameSequence)
                    .ToList();

                _parser.ProcessHtml(colony, html, _empireContext);
                Assert.That(colony.Structures.Count, Is.EqualTo(countAfterFirst),
                    $"Structure count changed after second parse of {filename}");

                // Verify structure values unchanged (Property 4)
                var afterSecond = colony.Structures
                    .Select(s => new { s.FlatpackBlueprintUUID, s.gameSequence })
                    .OrderBy(s => s.gameSequence)
                    .ToList();

                for (int i = 0; i < snapshot.Count; i++)
                {
                    Assert.That(afterSecond[i].FlatpackBlueprintUUID, Is.EqualTo(snapshot[i].FlatpackBlueprintUUID),
                        $"FlatpackBlueprintUUID mismatch in {filename} at index {i}");
                    Assert.That(afterSecond[i].gameSequence, Is.EqualTo(snapshot[i].gameSequence),
                        $"gameSequence mismatch in {filename} at index {i}");
                }
            }
        }

        [Test]
        public void AllColonyFiles_ParseTwice_CommodityCountUnchanged()
        {
            // Feature: parser-idempotency-tests, Property 5: Colony commodity count idempotency
            // Feature: parser-idempotency-tests, Property 6: Colony commodity values stability
            // Validates: Requirements 3.1, 3.2, 3.3, 3.4
            int filesWithCommodities = 0;

            foreach (string filename in ColonyFiles)
            {
                string clipboardData = LoadTestData(filename);
                string html = ExtractFragment(clipboardData);
                var colony = new Colony();

                _parser.ProcessHtml(colony, html, _empireContext);

                if (colony.Commodities.Count == 0)
                    continue;

                filesWithCommodities++;
                int countAfterFirst = colony.Commodities.Count;

                // Snapshot commodity values after first parse
                var snapshot = colony.Commodities
                    .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                    .OrderBy(c => c.Name)
                    .ToList();

                _parser.ProcessHtml(colony, html, _empireContext);
                Assert.That(colony.Commodities.Count, Is.EqualTo(countAfterFirst),
                    $"Commodity count changed after second parse of {filename}");

                // Verify commodity values unchanged (Property 6)
                var afterSecond = colony.Commodities
                    .Select(c => new { c.Name, c.Requested, c.Fulfilled })
                    .OrderBy(c => c.Name)
                    .ToList();

                for (int i = 0; i < snapshot.Count; i++)
                {
                    Assert.That(afterSecond[i].Name, Is.EqualTo(snapshot[i].Name),
                        $"Commodity name mismatch in {filename} at index {i}");
                    Assert.That(afterSecond[i].Requested, Is.EqualTo(snapshot[i].Requested),
                        $"Requested mismatch in {filename} for {snapshot[i].Name}");
                    Assert.That(afterSecond[i].Fulfilled, Is.EqualTo(snapshot[i].Fulfilled),
                        $"Fulfilled mismatch in {filename} for {snapshot[i].Name}");
                }
            }

            Assert.That(filesWithCommodities, Is.GreaterThan(0),
                "At least one colony file should have commodity demands");
        }
    }
}
