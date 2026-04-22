using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OE2EmpireTracker.Tests;

namespace OE2EmpireTracker.Tests.Blueprint
{
    [TestFixture]
    public class BlueprintScannerTests
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

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string Html(string body) =>
            $"<html><body>{body}</body></html>";

        private static string TitleDiv(string text) =>
            $"<div class='SmallSlideOut_Form_Row_Text_Bold'>{text}</div>";

        private static string EvoDiv(string number) =>
            $"<div class='EvolutionNumber'>{number}</div>";

        private static string DescDiv(string text) =>
            $"<div class='SmallSlideOut_Form_Row_Description'>{text}</div>";

        private static string ResourceRow(string name, string qty) =>
            $"<div class='ScanDetailOutputResourceName'>{name}</div>" +
            $"<div class='ScanDetailOutputResourceDetail'>{qty}</div>";

        private static string PropRow(string label, string value) =>
            $"<div class='ShipComponentProperty'>" +
            $"<div class='CargoInfoDialogue'>{label}</div>" +
            $"<div class='div_block ui_text_blue_light'>{value}</div>" +
            $"</div>";

        private static string LoadTestData(string filename)
        {
            string baseDir = TestContext.CurrentContext.TestDirectory;
            return File.ReadAllText(Path.Combine(baseDir, "TestData", filename));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- name and tech level
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_NameWithTechLevel_ParsesBoth()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(TitleDiv("Pulse Cannon (MilSpec)")));

            Assert.That(bp.Name, Is.EqualTo("Pulse Cannon"));
            Assert.That(bp.TechLevel, Is.EqualTo("MilSpec"));
        }

        [Test]
        public void ProcessHtml_NameWithoutTechLevel_SetsNameOnly()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(TitleDiv("Basic Thruster")));

            Assert.That(bp.Name, Is.EqualTo("Basic Thruster"));
            Assert.That(bp.TechLevel, Is.Null);
        }

        [Test]
        public void ProcessHtml_NameWithLeadingTrailingWhitespace_IsTrimmed()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(TitleDiv("  Cargo Pod  ")));

            Assert.That(bp.Name, Is.EqualTo("Cargo Pod"));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- evolution
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EvolutionNumber_ParsedAsInt()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(EvoDiv("3") + TitleDiv("Pulse Cannon3")));

            Assert.That(bp.Evolution, Is.EqualTo(3));
        }

        [Test]
        public void ProcessHtml_EvolutionRemovedFromTitle()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(EvoDiv("2") + TitleDiv("Jump Drive2")));

            Assert.That(bp.Name, Is.EqualTo("Jump Drive"));
        }

        [Test]
        public void ProcessHtml_NoEvolutionNode_EvolutionRemainsDefault()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(TitleDiv("Shield Generator")));

            Assert.That(bp.Evolution, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- description
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Description_IsPopulated()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(DescDiv("A powerful weapon system.")));

            Assert.That(bp.Description, Is.EqualTo("A powerful weapon system."));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- resources
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleResource_IsExtracted()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(ResourceRow("Iron", "500")));

            Assert.That(bp.Resources.ContainsKey("Iron"), Is.True);
            Assert.That(bp.Resources["Iron"], Is.EqualTo("500"));
        }

        [Test]
        public void ProcessHtml_MultipleResources_AllExtracted()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(
                ResourceRow("Iron", "500") +
                ResourceRow("Carbon", "250") +
                ResourceRow("Titanium", "100")));

            Assert.That(bp.Resources["Iron"], Is.EqualTo("500"));
            Assert.That(bp.Resources["Carbon"], Is.EqualTo("250"));
            Assert.That(bp.Resources["Titanium"], Is.EqualTo("100"));
        }

        [Test]
        public void ProcessHtml_ResourceQuantityWithCommas_StripsNonDigits()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(ResourceRow("Iron", "1,500")));

            Assert.That(bp.Resources["Iron"], Is.EqualTo("1500"));
        }

        [Test]
        public void ProcessHtml_NoResources_ResourcesDictionaryIsEmpty()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(TitleDiv("Empty Blueprint")));

            Assert.That(bp.Resources, Is.Not.Null);
            Assert.That(bp.Resources.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- properties
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleProperty_IsExtracted()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(PropRow("Mass", "450")));

            string val;
            bp.Properties.getString("Mass", null, out val);
            Assert.That(val, Is.EqualTo("450"));
        }

        [Test]
        public void ProcessHtml_PropertyWithDeltaText_DeltaIsStripped()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(PropRow("Power", "1200 (^ 435)")));

            string val;
            bp.Properties.getString("Power", null, out val);
            Assert.That(val, Is.EqualTo("1200"));
        }

        [Test]
        public void ProcessHtml_MultipleProperties_AllExtracted()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(
                PropRow("Mass", "450") +
                PropRow("Health", "2000") +
                PropRow("Power Required", "150")));

            string mass, health, power;
            bp.Properties.getString("Mass", null, out mass);
            bp.Properties.getString("Health", null, out health);
            bp.Properties.getString("Power Required", null, out power);

            Assert.That(mass, Is.EqualTo("450"));
            Assert.That(health, Is.EqualTo("2000"));
            Assert.That(power, Is.EqualTo("150"));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml -- robustness
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotThrow()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.DoesNotThrow(() => _scanner.ProcessHtml(bp, Html("")));
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotThrow()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.DoesNotThrow(() => _scanner.ProcessHtml(bp, "<div unclosed"));
        }

        // -----------------------------------------------------------------------
        // AMX-LL Milspec -- full integration from external files
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_AMX_LL_Milspec()
        {
            string page1 = LoadTestData("BP_AMX_LL_Milspec_Page1.html");
            string page2 = LoadTestData("BP_AMX_LL_Milspec_Page2.html");

            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, page1);
            _scanner.ProcessHtml(bp, page2);

            Assert.That(bp.Description, Is.EqualTo("Reactor that generates power for the ship"));
            Assert.That(bp.Class, Is.EqualTo(6));
            Assert.That(bp.Properties.ContainsKey("Class"), Is.False,
                "Class should be extracted to Blueprint.Class and removed from PropertyBag");

            string manuTime;
            bp.Properties.getString("Manufacture Run Time", null, out manuTime);
            Assert.That(manuTime, Is.EqualTo("9h"));

            string mass;
            bp.Properties.getString("Mass", null, out mass);
            Assert.That(mass, Is.EqualTo("861"));

            string cargoVolumeSize;
            bp.Properties.getString("Cargo Volume Size", null, out cargoVolumeSize);
            Assert.That(cargoVolumeSize, Is.EqualTo("360"));

            string health;
            bp.Properties.getString("Health", null, out health);
            Assert.That(health, Is.EqualTo("4824"));

            string engCap;
            bp.Properties.getString("Eng Capacity Required", null, out engCap);
            Assert.That(engCap, Is.EqualTo("1080"));

            string powerRegenRate;
            bp.Properties.getString("Power Regeneration Rate", null, out powerRegenRate);
            Assert.That(powerRegenRate, Is.EqualTo("31.5"));

            string wearRate;
            bp.Properties.getString("Wear and Tear Rate", null, out wearRate);
            Assert.That(wearRate, Is.EqualTo("2.959"));

            string dmgRate;
            bp.Properties.getString("Maximum Damage Repair", null, out dmgRate);
            Assert.That(dmgRate, Is.EqualTo("86.57"));

            Assert.That(bp.Resources["Alkaline Earth Metals"], Is.EqualTo("9366"));
            Assert.That(bp.Resources["Acidic Inorganics"], Is.EqualTo("1927"));
            Assert.That(bp.Resources["Heavy Trans-Metals"], Is.EqualTo("2121"));
            Assert.That(bp.Resources["Complex Non-Metallics"], Is.EqualTo("2036"));
            Assert.That(bp.Resources["Heavy Alkaline Earth Metals"], Is.EqualTo("2440"));
            Assert.That(bp.Resources["S1. Translivermoric Exotics"], Is.EqualTo("699"));
        }

        // -----------------------------------------------------------------------
        // Corvette Ship Hull -- statistics page
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesNameAndMetadata()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Name, Is.EqualTo("Corvette"));
            Assert.That(bp.Evolution, Is.EqualTo(0));
            Assert.That(bp.Description, Is.EqualTo("Ship Hull"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesClassAndManufactureTime()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Class, Is.EqualTo(3));

            string manuTime;
            bp.Properties.getString("Manufacture Run Time", null, out manuTime);
            Assert.That(manuTime, Is.EqualTo("13h"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesCoreProperties()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            string val;
            bp.Properties.getString("Mass", null, out val);
            Assert.That(val, Is.EqualTo("3200"));

            bp.Properties.getString("Cargo Volume Size", null, out val);
            Assert.That(val, Is.EqualTo("2000"));

            bp.Properties.getString("Health", null, out val);
            Assert.That(val, Is.EqualTo("1100"));

            bp.Properties.getString("Power Required", null, out val);
            Assert.That(val, Is.EqualTo("0"));

            bp.Properties.getString("Cargo Capacity", null, out val);
            Assert.That(val, Is.EqualTo("450"));

            bp.Properties.getString("Fuel Capacity", null, out val);
            Assert.That(val, Is.EqualTo("850"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesShipHullProperties()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            string val;
            bp.Properties.getString("License Level", null, out val);
            Assert.That(val, Is.EqualTo("8"));

            bp.Properties.getString("License Career", null, out val);
            Assert.That(val, Is.EqualTo("Military"));

            bp.Properties.getString("Eng Capacity Available", null, out val);
            Assert.That(val, Is.EqualTo("3000"));

            bp.Properties.getString("Max Hull Plating", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Max Hull Reinforcement", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Max Hull Sealant Units", null, out val);
            Assert.That(val, Is.EqualTo("2"));

            bp.Properties.getString("Crew Supported", null, out val);
            Assert.That(val, Is.EqualTo("2"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesWeaponMounts()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            string val;
            bp.Properties.getString("Large Weapon Mounts", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Medium Weapon Mounts", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Small Weapon Mounts", null, out val);
            Assert.That(val, Is.EqualTo("0"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesDefenceAndRepair()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            string val;
            bp.Properties.getString("Wear and Tear Rate", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Maximum Damage Repair", null, out val);
            Assert.That(val, Is.EqualTo("80"));

            bp.Properties.getString("Kinetic Damage Defence", null, out val);
            Assert.That(val, Is.EqualTo("16"));

            bp.Properties.getString("Missile Damage Defence", null, out val);
            Assert.That(val, Is.EqualTo("33"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_ParsesComponentSlots()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            string val;
            bp.Properties.getString("Reactor Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Main Drive Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Thruster Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Jump Drive Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Nav Comp Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Scanner Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Shield Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Cargo Pod Slots", null, out val);
            Assert.That(val, Is.EqualTo("3"));

            bp.Properties.getString("Fuel Tank Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("Coupler Slots", null, out val);
            Assert.That(val, Is.EqualTo("1"));

            bp.Properties.getString("GERTY Slots", null, out val);
            Assert.That(val, Is.EqualTo("3"));
        }

        [Test]
        public void ProcessHtml_Corvette_Statistics_NoResources()
        {
            string html = LoadTestData("BlueprintCorvetteStatistics.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Resources.Count, Is.EqualTo(0),
                "Statistics page should not contain resources");
        }

        // -----------------------------------------------------------------------
        // Corvette Ship Hull -- resources page
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Corvette_Resources_ParsesResources()
        {
            string html = LoadTestData("BlueprintCorvetteResources.html");
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, html);

            Assert.That(bp.Resources["Halogens"], Is.EqualTo("630"));
            Assert.That(bp.Resources["Non-Metallics"], Is.EqualTo("63"));
            Assert.That(bp.Resources["Heavy Post-Trans Metals"], Is.EqualTo("161"));
        }

        [Test]
        public void ProcessHtml_Corvette_FullImport_BothPages()
        {
            string statsHtml = LoadTestData("BlueprintCorvetteStatistics.html");
            string resourcesHtml = LoadTestData("BlueprintCorvetteResources.html");

            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, statsHtml);
            _scanner.ProcessHtml(bp, resourcesHtml);

            // Metadata from statistics page
            Assert.That(bp.Name, Is.EqualTo("Corvette"));
            Assert.That(bp.Class, Is.EqualTo(3));
            Assert.That(bp.Description, Is.EqualTo("Ship Hull"));

            // Properties from statistics page
            string manuTime;
            bp.Properties.getString("Manufacture Run Time", null, out manuTime);
            Assert.That(manuTime, Is.EqualTo("13h"));

            // Resources from resources page
            Assert.That(bp.Resources.Count, Is.EqualTo(3));
            Assert.That(bp.Resources["Halogens"], Is.EqualTo("630"));
            Assert.That(bp.Resources["Non-Metallics"], Is.EqualTo("63"));
            Assert.That(bp.Resources["Heavy Post-Trans Metals"], Is.EqualTo("161"));
        }

        // -----------------------------------------------------------------------
        // Market HTML -- bulk import exploration
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessMarketHtml_ExtractsMultipleBlueprints()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.GreaterThanOrEqualTo(4),
                "Should extract at least 4 blueprints from market listing");

            // Log what we got for exploration
            foreach (var mb in results)
            {
                var bp = mb.Blueprint;
                TestContext.WriteLine($"Name: {bp.Name}, Evo: {bp.Evolution}, Class: {bp.Class}, " +
                    $"Props: {bp.Properties.Count}, Resources: {bp.Resources.Count}, Seller: {mb.SellerName}");
            }
        }

        [Test]
        public void ProcessMarketHtml_FirstBlueprint_HasNameAndEvolution()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.GreaterThan(0));
            var first = results[0].Blueprint;
            Assert.That(first.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(first.Evolution, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ProcessMarketHtml_Blueprints_HaveProperties()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            foreach (var mb in results)
            {
                var bp = mb.Blueprint;
                Assert.That(bp.Properties.Count, Is.GreaterThan(0),
                    $"Blueprint '{bp.Name}' should have properties");

                // All hull blueprints should have Class extracted to Blueprint.Class
                Assert.That(bp.Class, Is.GreaterThan(0),
                    $"Blueprint '{bp.Name}' should have Class > 0");
                Assert.That(bp.Properties.ContainsKey("Class"), Is.False,
                    $"Blueprint '{bp.Name}' should not have Class in PropertyBag (extracted to Blueprint.Class)");
            }
        }

        [Test]
        public void ProcessMarketHtml_Blueprints_HaveResources()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            foreach (var mb in results)
            {
                var bp = mb.Blueprint;
                Assert.That(bp.Resources.Count, Is.GreaterThan(0),
                    $"Blueprint '{bp.Name}' should have resources");

                // All resource quantities should be numeric
                foreach (var kvp in bp.Resources)
                {
                    Assert.That(int.TryParse(kvp.Value, out _), Is.True,
                        $"Resource '{kvp.Key}' on '{bp.Name}' should have numeric quantity, got '{kvp.Value}'");
                }
            }
        }

        [Test]
        public void ProcessMarketHtml_PropertyValuesAreNormalized()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            var first = results[0].Blueprint;

            // Wear and Tear Rate should be stripped of % (decimal normalization)
            string wearRate;
            first.Properties.getString("Wear and Tear Rate", null, out wearRate);
            Assert.That(wearRate, Does.Not.Contain("%"),
                "Wear and Tear Rate should have % stripped by decimal normalization");

            // Maximum Damage Repair should be stripped of %
            string dmgRepair;
            first.Properties.getString("Maximum Damage Repair", null, out dmgRepair);
            Assert.That(dmgRepair, Does.Not.Contain("%"),
                "Maximum Damage Repair should have % stripped by decimal normalization");
        }

        [Test]
        public void ProcessMarketHtml_ExtractsBlueprintTypeIcon()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            // All hull blueprints should resolve to BluePrintType "Hull"
            foreach (var mb in results)
            {
                var bp = mb.Blueprint;
                if (bp.Properties.Count > 0) // skip unexpanded listings
                {
                    Assert.That(bp.BluePrintType, Is.EqualTo("Hull"),
                        $"Blueprint '{bp.Name}' should be type Hull");
                }
            }
        }

        [Test]
        public void ProcessMarketHtml_DumpAllData()
        {
            // Exploratory test -- dumps all extracted data for review
            string html = LoadTestData("BlueprintMarketHulls.html");
            var results = _scanner.ProcessMarketHtml(html);

            for (int i = 0; i < results.Count; i++)
            {
                var bp = results[i].Blueprint;
                string iconPos;
                bp.Properties.getString("_IconPosition", null, out iconPos);
                TestContext.WriteLine($"\n=== Blueprint {i + 1}: {bp.Name} (Ev{bp.Evolution}, Class {bp.Class}, Icon: {iconPos ?? "none"}, TechLevel: {bp.TechLevel ?? "null"}, Seller: {results[i].SellerName}) ===");

                TestContext.WriteLine("Properties:");
                string[] knownProps = { "Class", "Mass", "Cargo Volume Size", "License Level",
                    "License Career", "Health", "Eng Capacity Required", "Max Hull Plating",
                    "Max Hull Reinforcement", "Max Hull Sealant Units", "Cargo Capacity",
                    "Fuel Capacity", "Large Weapon Mounts", "Medium Weapon Mounts",
                    "Small Weapon Mounts", "Wear and Tear Rate", "Maximum Damage Repair",
                    "Energy Defence", "Kinetic Damage Defence", "Missile Damage Defence",
                    "Crew Supported", "Reactor Slots", "Main Drive Slots", "Thruster Slots",
                    "Jump Drive Slots", "Nav Comp Slots", "Scanner Slots", "Shield Slots",
                    "Cargo Pod Slots", "Fuel Tank Slots", "Coupler Slots", "GERTY Slots" };
                foreach (var prop in knownProps)
                {
                    string val;
                    bp.Properties.getString(prop, null, out val);
                    if (val != null) TestContext.WriteLine($"  {prop} = {val}");
                }

                TestContext.WriteLine("Resources:");
                foreach (var kvp in bp.Resources)
                {
                    TestContext.WriteLine($"  {kvp.Key} = {kvp.Value}");
                }
            }
        }

        [Test]
        public void ProcessMarketHtml_Mixed_DumpAllData()
        {
            // Exploratory test -- dumps mixed blueprint types with resolved types
            string html = LoadTestData("BlueprintMarketMixed.html");
            var results = _scanner.ProcessMarketHtml(html);

            TestContext.WriteLine($"Total blueprints: {results.Count}");

            // Group by resolved BluePrintType
            var typeGroups = new Dictionary<string, List<string>>();
            foreach (var mb in results)
            {
                var bp = mb.Blueprint;
                string key = bp.BluePrintType ?? "UNKNOWN";
                if (!typeGroups.ContainsKey(key))
                    typeGroups[key] = new List<string>();
                typeGroups[key].Add($"{bp.Name} (Ev{bp.Evolution}, Class {bp.Class}, Props: {bp.Properties.Count}, Res: {bp.Resources.Count})");
            }

            TestContext.WriteLine("\n=== Blueprints grouped by resolved BluePrintType ===");
            foreach (var group in typeGroups.OrderBy(g => g.Key))
            {
                TestContext.WriteLine($"\n{group.Key} ({group.Value.Count} blueprints):");
                foreach (var name in group.Value)
                {
                    TestContext.WriteLine($"  {name}");
                }
            }
        }

        [Test]
        public void ProcessAllMarketSamples_DiscoverIconsAndProperties()
        {
            // Parse every MarketSample file and collect icon positions + property sets per type
            string baseDir = TestContext.CurrentContext.TestDirectory;
            var sampleFiles = System.IO.Directory.GetFiles(
                System.IO.Path.Combine(baseDir, "TestData"), "MarketSample*.html");

            Assert.That(sampleFiles.Length, Is.GreaterThan(0), "No MarketSample files found");

            // Collect all blueprints across all files
            var allBlueprints = new List<OE2EmpireTracker.Models.Blueprint>();
            foreach (var file in sampleFiles.OrderBy(f => f))
            {
                string html = System.IO.File.ReadAllText(file);
                var results = _scanner.ProcessMarketHtml(html);
                string fileName = System.IO.Path.GetFileName(file);
                TestContext.WriteLine($"{fileName}: {results.Count} blueprints");
                allBlueprints.AddRange(results.Select(mb => mb.Blueprint));
            }

            TestContext.WriteLine($"\nTotal blueprints across all files: {allBlueprints.Count}");

            // Group by icon position to discover new types
            var iconGroups = new Dictionary<string, List<OE2EmpireTracker.Models.Blueprint>>();
            foreach (var bp in allBlueprints)
            {
                string iconPos;
                bp.Properties.getString("_IconPosition", null, out iconPos);
                string key = iconPos ?? "no-icon";
                if (!iconGroups.ContainsKey(key))
                    iconGroups[key] = new List<OE2EmpireTracker.Models.Blueprint>();
                iconGroups[key].Add(bp);
            }

            TestContext.WriteLine("\n=== Icon positions and resolved types ===");
            foreach (var group in iconGroups.OrderBy(g => g.Key))
            {
                var first = group.Value[0];
                string resolvedType = first.BluePrintType ?? "UNMAPPED";
                TestContext.WriteLine($"\nIcon: {group.Key} -> {resolvedType} ({group.Value.Count} blueprints)");
                foreach (var bp in group.Value.Take(3))
                {
                    TestContext.WriteLine($"  {bp.Name} (Ev{bp.Evolution}, Class {bp.Class})");
                }

                if (group.Value.Count > 3)
                    TestContext.WriteLine($"  ... and {group.Value.Count - 3} more");

                // Dump all unique property keys for this icon group
                var allProps = new SortedSet<string>();
                foreach (var bp in group.Value)
                {
                    foreach (var key in bp.Properties.Properties.Keys)
                    {
                        if (!key.StartsWith("_")) allProps.Add(key);
                    }
                }

                if (allProps.Count > 0)
                {
                    TestContext.WriteLine($"  Properties ({allProps.Count}): {string.Join(", ", allProps)}");
                }
            }
        }

        // -----------------------------------------------------------------------
        // Market HTML helpers for seller name / TechLevel tests
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a minimal market HTML snippet with a listing row + detail row.
        /// </summary>
        private static string MarketHtml(string bpName, string sellerSpan, string evoNumber = "0")
        {
            // Seller span is optional -- pass empty string for no seller
            string descContent = bpName + sellerSpan;
            return "<html><body><table><tbody>"
                + $"<tr class='MarketListingRow'>"
                + $"<td><div class='EvolutionNumber'>{evoNumber}</div></td>"
                + $"<td><div class='div_block MarketListingRowDetailDescription'>{descContent}</div></td>"
                + "</tr>"
                + "<tr class='MarketListingRowDetail'><td colspan='6'>"
                + "<div class='Market_ShipComponentProperty'>"
                + "<div class='Market_ShipComponentProperty_Label'>Class</div>"
                + "<div class='ui_text_blue_light'>1</div>"
                + "</div>"
                + "</td></tr>"
                + "</tbody></table></body></html>";
        }

        private static string SellerSpan(string seller) =>
            $"<span class='ui_text_light_grey'><br/>{seller}</span>";

        // -----------------------------------------------------------------------
        // ProcessMarketHtml -- TechLevel extraction
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessMarketHtml_NameWithMilSpec_TechLevelExtracted()
        {
            string html = MarketHtml("AMX-SS Reactor Core (MilSpec)", SellerSpan("Government"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Blueprint.Name, Is.EqualTo("AMX-SS Reactor Core"));
            Assert.That(results[0].Blueprint.TechLevel, Is.EqualTo("MilSpec"));
        }

        [Test]
        public void ProcessMarketHtml_NameWithRugged_TechLevelExtracted()
        {
            string html = MarketHtml("Navi-Comp v1.0 (Rugged)", SellerSpan("Government"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Blueprint.Name, Is.EqualTo("Navi-Comp v1.0"));
            Assert.That(results[0].Blueprint.TechLevel, Is.EqualTo("Rugged"));
        }

        [Test]
        public void ProcessMarketHtml_NameWithoutParentheses_TechLevelIsNull()
        {
            string html = MarketHtml("Fighter Bomber", SellerSpan("Government"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Blueprint.Name, Is.EqualTo("Fighter Bomber"));
            Assert.That(results[0].Blueprint.TechLevel, Is.Null);
        }

        [Test]
        public void ProcessMarketHtml_NameWithNonTechLevelParentheses_TechLevelIsNull()
        {
            string html = MarketHtml("Some Widget (Ev0)", SellerSpan("Government"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Blueprint.Name, Is.EqualTo("Some Widget (Ev0)"));
            Assert.That(results[0].Blueprint.TechLevel, Is.Null);
        }

        // -----------------------------------------------------------------------
        // ProcessMarketHtml -- seller name extraction
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessMarketHtml_GovernmentSeller_ExtractedCorrectly()
        {
            string html = MarketHtml("Scout", SellerSpan("Government"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].SellerName, Is.EqualTo("Government"));
        }

        [Test]
        public void ProcessMarketHtml_PlayerSeller_ExtractedCorrectly()
        {
            string html = MarketHtml("Pulse Cannon (MilSpec)", SellerSpan("Scalorn Scorpus"));
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].SellerName, Is.EqualTo("Scalorn Scorpus"));
            Assert.That(results[0].Blueprint.Name, Is.EqualTo("Pulse Cannon"));
            Assert.That(results[0].Blueprint.TechLevel, Is.EqualTo("MilSpec"));
        }

        [Test]
        public void ProcessMarketHtml_NoSellerSpan_SellerNameIsEmpty()
        {
            string html = MarketHtml("Basic Thruster", "");
            var results = _scanner.ProcessMarketHtml(html);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].SellerName, Is.EqualTo(""));
        }
    }
}
