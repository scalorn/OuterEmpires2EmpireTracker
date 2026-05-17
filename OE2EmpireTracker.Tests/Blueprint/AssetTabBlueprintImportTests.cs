using System.IO;
using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Blueprint
{
    /// <summary>
    /// Tests for individual blueprint import from the asset-tab view.
    /// The asset tab HTML contains a list of blueprints with evolution numbers
    /// in addition to the detail panel, which caused the parser to pick the
    /// wrong (empty) EvolutionNumber div and crash on string.Replace(string.Empty,string.Empty).
    /// </summary>
    [TestFixture]
    public class AssetTabBlueprintImportTests
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
        // Stats tab -- asset tab view
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsName()
        {
            var bp = ParseAssetTabStats();
            Assert.That(bp.Name, Is.EqualTo("WSMS-LL Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsBlueprintType()
        {
            var bp = ParseAssetTabStats();
            Assert.That(
                bp.BluePrintType,
                Is.EqualTo("JumpDrive"),
                "Should resolve BlueprintType from icon position in the HTML");
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsEvolution()
        {
            var bp = ParseAssetTabStats();
            Assert.That(bp.Evolution, Is.EqualTo(6));
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsTechLevel()
        {
            var bp = ParseAssetTabStats();
            Assert.That(bp.TechLevel, Is.EqualTo("MilSpec"));
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsDescription()
        {
            var bp = ParseAssetTabStats();
            Assert.That(bp.Description, Is.EqualTo("Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsProperties()
        {
            var bp = ParseAssetTabStats();
            Assert.That(
                bp.Properties.Count,
                Is.GreaterThan(0),
                "Should extract at least one property from the Statistics tab");
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabStats_ExtractsClass()
        {
            var bp = ParseAssetTabStats();
            Assert.That(bp.Class, Is.EqualTo(6));
        }

        // -------------------------------------------------------------------
        // Resources tab -- asset tab view
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_AssetTabResources_ExtractsResources()
        {
            var bp = ParseAssetTabResources();
            Assert.That(
                bp.Resources.Count,
                Is.GreaterThan(0),
                "Should extract at least one resource from the Resources tab");
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabResources_ExtractsName()
        {
            var bp = ParseAssetTabResources();
            Assert.That(bp.Name, Is.EqualTo("WSMS-LL Jump Drive"));
        }

        [Test]
        public void IndividualBlueprintImport_AssetTabResources_ExtractsEvolution()
        {
            var bp = ParseAssetTabResources();
            Assert.That(bp.Evolution, Is.EqualTo(6));
        }

        // -------------------------------------------------------------------
        // Combined import: Stats then Resources (mimics user workflow)
        // -------------------------------------------------------------------

        [Test]
        public void IndividualBlueprintImport_AssetTab_StatsThenResources_Preserved()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();

            // First import: Stats page
            string rawStats = LoadTestData("IndivudalBPAssetTabWSMS-LL6Stats.html");
            string htmlStats = ClipboardHelper.ExtractHtmlFragment(rawStats);
            _scanner.ProcessHtml(bp, htmlStats);

            Assert.That(bp.Name, Is.EqualTo("WSMS-LL Jump Drive"));
            Assert.That(bp.BluePrintType, Is.EqualTo("JumpDrive"));
            Assert.That(bp.Properties.Count, Is.GreaterThan(0));

            // Second import: Resources page
            string rawRes = LoadTestData("IndivudalBPAssetTabWSMS-LL6Resources.html");
            string htmlRes = ClipboardHelper.ExtractHtmlFragment(rawRes);
            _scanner.ProcessHtml(bp, htmlRes);

            Assert.That(
                bp.BluePrintType,
                Is.EqualTo("JumpDrive"),
                "BlueprintType should still be JumpDrive after Resources import");
            Assert.That(
                bp.Resources.Count,
                Is.GreaterThan(0),
                "Resources should be populated after second import");
            Assert.That(
                bp.Properties.Count,
                Is.GreaterThan(0),
                "Properties from Stats import should still be present");
        }

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }

        private OE2EmpireTracker.Models.Blueprint ParseAssetTabStats()
        {
            string raw = LoadTestData("IndivudalBPAssetTabWSMS-LL6Stats.html");
            string html = ClipboardHelper.ExtractHtmlFragment(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);
            return bp;
        }

        private OE2EmpireTracker.Models.Blueprint ParseAssetTabResources()
        {
            string raw = LoadTestData("IndivudalBPAssetTabWSMS-LL6Resources.html");
            string html = ClipboardHelper.ExtractHtmlFragment(raw);
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);
            return bp;
        }
    }
}
