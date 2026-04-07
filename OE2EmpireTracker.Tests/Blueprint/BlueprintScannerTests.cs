using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Forms.Blueprint;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OE2EmpireTracker.Tests.Blueprint
{
    [TestFixture]
    public class BlueprintScannerTests
    {
        private BlueprintScanner _scanner;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            string baselineDataPath = System.IO.Path.Combine(baseDir, @"..\..\..\OE2EmpireTracker\BaselineData.json");
            OE2EmpireTracker.Services.EmpireContext.FilePath = System.IO.Path.GetFullPath(baselineDataPath);
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
        // ProcessHtml — name and tech level
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
        // ProcessHtml — evolution
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
        // ProcessHtml — description
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Description_IsPopulated()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            _scanner.ProcessHtml(bp, Html(DescDiv("A powerful weapon system.")));

            Assert.That(bp.Description, Is.EqualTo("A powerful weapon system."));
        }

        // -----------------------------------------------------------------------
        // ProcessHtml — resources
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
        // ProcessHtml — properties
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
            _scanner.ProcessHtml(bp, Html(PropRow("Power", "1200 (▲ 435)")));

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
        // ProcessHtml — robustness
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
        // AMX-LL Milspec — full integration from external files
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
            string equipClass;
            bp.Properties.getString("Class", null, out equipClass);
            Assert.That(equipClass, Is.EqualTo("6"));
            Assert.That(bp.Class, Is.EqualTo(6));

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
        // Corvette Ship Hull — statistics page
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
        // Corvette Ship Hull — resources page
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
        // Market HTML — bulk import exploration
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessMarketHtml_ExtractsMultipleBlueprints()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            Assert.That(blueprints.Count, Is.GreaterThanOrEqualTo(4),
                "Should extract at least 4 blueprints from market listing");

            // Log what we got for exploration
            foreach (var bp in blueprints)
            {
                TestContext.WriteLine($"Name: {bp.Name}, Evo: {bp.Evolution}, Class: {bp.Class}, " +
                    $"Props: {bp.Properties.Count}, Resources: {bp.Resources.Count}");
            }
        }

        [Test]
        public void ProcessMarketHtml_FirstBlueprint_HasNameAndEvolution()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            Assert.That(blueprints.Count, Is.GreaterThan(0));
            var first = blueprints[0];
            Assert.That(first.Name, Is.Not.Null.And.Not.Empty);
            Assert.That(first.Evolution, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ProcessMarketHtml_Blueprints_HaveProperties()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            foreach (var bp in blueprints)
            {
                Assert.That(bp.Properties.Count, Is.GreaterThan(0),
                    $"Blueprint '{bp.Name}' should have properties");

                // All hull blueprints should have Class
                string cls;
                bp.Properties.getString("Class", null, out cls);
                Assert.That(cls, Is.Not.Null,
                    $"Blueprint '{bp.Name}' should have Class property");
            }
        }

        [Test]
        public void ProcessMarketHtml_Blueprints_HaveResources()
        {
            string html = LoadTestData("BlueprintMarketHulls.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            foreach (var bp in blueprints)
            {
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
            var blueprints = _scanner.ProcessMarketHtml(html);

            var first = blueprints[0];

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
            var blueprints = _scanner.ProcessMarketHtml(html);

            // All hull blueprints should resolve to BluePrintType "Hull"
            foreach (var bp in blueprints)
            {
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
            // Exploratory test — dumps all extracted data for review
            string html = LoadTestData("BlueprintMarketHulls.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            for (int i = 0; i < blueprints.Count; i++)
            {
                var bp = blueprints[i];
                string iconPos;
                bp.Properties.getString("_IconPosition", null, out iconPos);
                TestContext.WriteLine($"\n=== Blueprint {i + 1}: {bp.Name} (Ev{bp.Evolution}, Class {bp.Class}, Icon: {iconPos ?? "none"}) ===");

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
            // Exploratory test — dumps mixed blueprint types with resolved types
            string html = LoadTestData("BlueprintMarketMixed.html");
            var blueprints = _scanner.ProcessMarketHtml(html);

            TestContext.WriteLine($"Total blueprints: {blueprints.Count}");

            // Group by resolved BluePrintType
            var typeGroups = new Dictionary<string, List<string>>();
            foreach (var bp in blueprints)
            {
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
    }
}
