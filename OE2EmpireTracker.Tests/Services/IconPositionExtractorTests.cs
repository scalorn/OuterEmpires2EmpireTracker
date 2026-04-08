using NUnit.Framework;
using OE2EmpireTracker.Forms.Blueprint;
using OE2EmpireTracker.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Services
{
    /// <summary>
    /// Simple DTO capturing one extracted icon from a MarketSample HTML file.
    /// </summary>
    public class ExtractedIcon
    {
        /// <summary>Blueprint name as it appears in the market listing.</summary>
        public string BlueprintName { get; set; }

        /// <summary>CSS sprite position string (e.g. "-328px -62px").</summary>
        public string IconPosition { get; set; }

        /// <summary>The MarketSample HTML filename this icon was found in.</summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// The BlueprintType Id resolved via EmpireContext.FindBlueprintTypeByIcon(),
        /// or null if no matching BlueprintType exists.
        /// </summary>
        public string ResolvedTypeId { get; set; }
    }

    [TestFixture]
    public class IconPositionExtractorTests
    {
        [SetUp]
        public void SetUp()
        {
            EmpireContext.Reset();
            TestHelper.SetEmpireFilePath();
            EmpireContext.getInstance();
        }

        /// <summary>
        /// Parses every MarketSample*.html file in TestData using
        /// BlueprintScanner.ProcessMarketHtml() and extracts the _IconPosition
        /// property from each parsed blueprint.
        /// </summary>
        private List<ExtractedIcon> ExtractIconsFromAllSamples()
        {
            var results = new List<ExtractedIcon>();
            string testDataDir = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "TestData");

            var sampleFiles = Directory.GetFiles(testDataDir, "MarketSample*.html");
            var scanner = new BlueprintScanner();

            foreach (string filePath in sampleFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string html = File.ReadAllText(filePath);
                List<MarketBlueprint> parsed = scanner.ProcessMarketHtml(html);

                foreach (var mb in parsed)
                {
                    string iconPos = null;
                    mb.Blueprint.Properties.getString("_IconPosition", null, out iconPos);
                    if (string.IsNullOrEmpty(iconPos))
                        continue;

                    results.Add(new ExtractedIcon
                    {
                        BlueprintName = mb.Blueprint.Name,
                        IconPosition = iconPos,
                        SourceFile = fileName,
                        ResolvedTypeId = mb.Blueprint.BluePrintType
                    });
                }
            }

            return results;
        }

        [Test]
        public void ExtractIconsFromAllSamples_FindsIcons()
        {
            var icons = ExtractIconsFromAllSamples();

            TestContext.WriteLine($"Total extracted icons: {icons.Count}");
            foreach (var icon in icons)
            {
                string typeLabel = icon.ResolvedTypeId ?? "UNKNOWN";
                TestContext.WriteLine(
                    $"  {icon.IconPosition} -> {typeLabel} ({icon.BlueprintName}) [{icon.SourceFile}]");
            }

            Assert.That(icons, Is.Not.Empty,
                "Expected at least one icon extracted from MarketSample HTML files");
        }
    }
}
