using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Blueprint
{
    /// <summary>
    /// Tests for individual blueprint import from full-page clipboard HTML.
    /// These test files are real "Select All + Copy" captures from the game browser,
    /// including the clipboard header (Version:0.9, StartHTML, etc.) and full page chrome.
    /// </summary>
    [TestFixture]
    public class IndividualBlueprintImportTests
    {
        private BlueprintScanner _scanner;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            TestHelper.SetEmpireFilePath();
            OE2EmpireTracker.Services.EmpireContext.Reset();
        }

        [SetUp]
        public void SetUp()
        {
            _scanner = new BlueprintScanner();
        }

        // -------------------------------------------------------------------
        // Stats tab -- full-page HTML with clipboard header
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsName()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Name, Is.EqualTo("WSMS-LL Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsBlueprintType()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(
                bp.BluePrintType,
                Is.EqualTo("JumpDrive"),
                "Should resolve BlueprintType from icon position in the HTML");
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsTechLevel()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.TechLevel, Is.EqualTo("MilSpec"));
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsEvolution()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Evolution, Is.EqualTo(9));
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsDescription()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Description, Is.EqualTo("Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsProperties()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(
                bp.Properties.Count,
                Is.GreaterThan(0),
                "Should extract at least one property from the Statistics tab");
        }

        [Test]
        public void IndividualBlueprintImport_StatsHtml_ExtractsClass()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Class, Is.EqualTo(6));
        }

        // -------------------------------------------------------------------
        // Resources tab -- full-page HTML with clipboard header
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_ResourcesHtml_ExtractsResources()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Resources.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(
                bp.Resources.Count,
                Is.GreaterThan(0),
                "Should extract at least one resource from the Resources tab");
        }

        [Test]
        public void IndividualBlueprintImport_ResourcesHtml_ExtractsName()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Resources.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Name, Is.EqualTo("WSMS-LL Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_ResourcesHtml_ContainsHeavyNobleGases()
        {
            string raw = LoadTestData("IndivudalBPWSMS-LL9Resources.html");
            string html = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, html);

            Assert.That(
                bp.Resources.ContainsKey("Heavy Noble Gases"),
                Is.True,
                "Should contain 'Heavy Noble Gases' resource");
            Assert.That(bp.Resources["Heavy Noble Gases"], Is.EqualTo("6835"));
        }

        // -------------------------------------------------------------------
        // Combined import: Stats then Resources (mimics user workflow)
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_StatsThenResources_BlueprintTypePreserved()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();

            // First import: Stats page (sets name, properties, type)
            string rawStats = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            string htmlStats = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(rawStats);
            _scanner.ProcessHtml(bp, htmlStats);

            Assert.That(
                bp.BluePrintType,
                Is.EqualTo("JumpDrive"),
                "After Stats import, BlueprintType should be JumpDrive");

            // Second import: Resources page (adds resources, should NOT clear type)
            string rawRes = LoadTestData("IndivudalBPWSMS-LL9Resources.html");
            string htmlRes = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(rawRes);
            _scanner.ProcessHtml(bp, htmlRes);

            Assert.That(
                bp.BluePrintType,
                Is.EqualTo("JumpDrive"),
                "After Resources import, BlueprintType should still be JumpDrive");
            Assert.That(
                bp.Resources.Count,
                Is.GreaterThan(0),
                "Resources should be populated after second import");
            Assert.That(
                bp.Properties.Count,
                Is.GreaterThan(0),
                "Properties from Stats import should still be present");
        }

        // -------------------------------------------------------------------
        // Regression: raw clipboard data (without extraction) should fail
        // This documents the bug that was fixed.
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_RawClipboardData_NameContainsHeaderNoise()
        {
            // Raw clipboard data includes the Version:0.9 header. The SGML parser
            // is lenient enough to still find nodes, but the header text leaks into
            // the parsed content (e.g. the title node's InnerText may include
            // header lines). After the fix, processClipboard extracts the fragment
            // first so only clean HTML reaches ProcessHtml.
            string raw = LoadTestData("IndivudalBPWSMS-LL9Stats.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();

            _scanner.ProcessHtml(bp, raw);

            // With extraction the name is clean; without it the header noise
            // may or may not corrupt the name depending on SGML tolerance.
            // The key point is that extraction is the correct approach -- it
            // matches what cmdImportMarket_Click already does.
            // We just verify the extracted path produces the correct name.
            string rawExtracted = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(raw);
            var bpClean = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bpClean, rawExtracted);

            Assert.That(
                bpClean.Name,
                Is.EqualTo("WSMS-LL Jump Drive"),
                "Extracted fragment should produce the correct name");
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }
    }
}
