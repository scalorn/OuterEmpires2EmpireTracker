using NUnit.Framework;
using OE2EmpireTracker.Parsers;
using System.IO;
using System.Linq;
using System.Text;

namespace OE2EmpireTracker.Tests.Services
{
    [TestFixture]
    public class BughuntDiagnosticTest
    {
        [Test]
        public void Diagnostic_BughuntHtml_ExtractFragment_ThenParse()
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            string rawClipboard = File.ReadAllText(Path.Combine(baseDir, "TestData", "BUGHUNT.html"));

            string fragment = BlueprintScanner.ExtractHtmlFragmentFromClipboardData(rawClipboard);

            Assert.That(fragment, Does.Not.StartWith("ERROR"), "Fragment extraction failed");
            Assert.That(fragment.Length, Is.GreaterThan(1000), "Fragment too short");
            Assert.That(fragment, Does.Contain("MarketListingRow"), "Fragment missing MarketListingRow");

            // Parse the extracted fragment through BlueprintScanner
            var scanner = new BlueprintScanner();
            var results = scanner.ProcessMarketHtml(fragment);

            TestContext.WriteLine($"Fragment length: {fragment.Length}");
            TestContext.WriteLine($"Results count: {results.Count}");
            foreach (var mb in results)
            {
                TestContext.WriteLine($"  Blueprint: {mb.Blueprint.Name}");
            }

            // Assert the fix produces actual results
            Assert.That(results.Count, Is.GreaterThan(0), "ProcessMarketHtml should return results from BUGHUNT.html");

            // Assert known blueprint names are present
            var names = results.Select(r => r.Blueprint.Name).ToList();
            Assert.That(names, Does.Contain("Habitation Block Flatpack"), "Missing 'Habitation Block Flatpack'");
            Assert.That(names, Does.Contain("Manufactory Flatpack"), "Missing 'Manufactory Flatpack'");
            Assert.That(names, Does.Contain("Entertainment Centre Flatpack"), "Missing 'Entertainment Centre Flatpack'");
        }

        [Test]
        public void Diagnostic_BughuntHtml_DirectParse_NoFragmentExtraction()
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            string rawClipboard = File.ReadAllText(Path.Combine(baseDir, "TestData", "BUGHUNT.html"));

            // Skip fragment extraction -- pass raw clipboard data directly
            var scanner = new BlueprintScanner();
            var results = scanner.ProcessMarketHtml(rawClipboard);

            TestContext.WriteLine($"Raw clipboard length: {rawClipboard.Length}");
            TestContext.WriteLine($"Results count (direct): {results.Count}");

            // Debug: parse with SGML and check what we get
            var reader = new System.IO.StringReader(rawClipboard);
            var sgmlReader = new Sgml.SgmlReader()
            {
                DocType = "HTML",
                WhitespaceHandling = System.Xml.WhitespaceHandling.All,
                CaseFolding = Sgml.CaseFolding.ToLower,
                InputStream = reader
            };
            var doc = new System.Xml.XmlDocument() { PreserveWhitespace = true, XmlResolver = null };
            doc.Load(sgmlReader);

            var allNodes = doc.SelectNodes("//*");
            TestContext.WriteLine($"Total DOM nodes: {allNodes?.Count}");

            var allTr = doc.SelectNodes("//tr");
            TestContext.WriteLine($"Total TR elements: {allTr?.Count}");

            if (allTr != null)
            {
                foreach (System.Xml.XmlNode tr in allTr)
                {
                    string cls = tr.Attributes?["class"]?.Value ?? "(no class)";
                    TestContext.WriteLine($"  TR class: {cls}");
                }
            }

            // Check for table elements
            var allTables = doc.SelectNodes("//table");
            TestContext.WriteLine($"Total TABLE elements: {allTables?.Count}");

            // Check for divs with MarketListingRow in class
            var marketDivs = doc.SelectNodes("//*[contains(@class,'MarketListingRow')]");
            TestContext.WriteLine($"Any element with MarketListingRow class: {marketDivs?.Count}");

            TestContext.WriteLine($"Direct parse found {results.Count} blueprints");
        }
    }
}
