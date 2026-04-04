using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.Forms.Blueprint;
using System.IO;

namespace OE2EmpireTracker.Tests.Blueprint
{
    [TestFixture]
    public class BlueprintScannerTests
    {
        private BlueprintScanner _scanner;

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
        // processHtml — name and tech level
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_NameWithTechLevel_ParsesBoth()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Pulse Cannon (MilSpec)")));

            Assert.AreEqual("Pulse Cannon", bp.Name);
            Assert.AreEqual("MilSpec", bp.TechLevel);
        }

        [Test]
        public void ProcessHtml_NameWithoutTechLevel_SetsNameOnly()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Basic Thruster")));

            Assert.AreEqual("Basic Thruster", bp.Name);
            Assert.IsNull(bp.TechLevel);
        }

        [Test]
        public void ProcessHtml_NameWithLeadingTrailingWhitespace_IsTrimmed()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("  Cargo Pod  ")));

            Assert.AreEqual("Cargo Pod", bp.Name);
        }

        // -----------------------------------------------------------------------
        // processHtml — evolution
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EvolutionNumber_ParsedAsInt()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(EvoDiv("3") + TitleDiv("Pulse Cannon3")));

            Assert.AreEqual(3, bp.Evolution);
        }

        [Test]
        public void ProcessHtml_EvolutionRemovedFromTitle()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(EvoDiv("2") + TitleDiv("Jump Drive2")));

            Assert.AreEqual("Jump Drive", bp.Name);
        }

        [Test]
        public void ProcessHtml_NoEvolutionNode_EvolutionRemainsDefault()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Shield Generator")));

            Assert.AreEqual(0, bp.Evolution);
        }

        // -----------------------------------------------------------------------
        // processHtml — description
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_Description_IsPopulated()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(DescDiv("A powerful weapon system.")));

            Assert.AreEqual("A powerful weapon system.", bp.Description);
        }

        // -----------------------------------------------------------------------
        // processHtml — resources
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleResource_IsExtracted()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(ResourceRow("Iron", "500")));

            Assert.IsTrue(bp.Resources.ContainsKey("Iron"));
            Assert.AreEqual("500", bp.Resources["Iron"]);
        }

        [Test]
        public void ProcessHtml_MultipleResources_AllExtracted()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(
                ResourceRow("Iron", "500") +
                ResourceRow("Carbon", "250") +
                ResourceRow("Titanium", "100")));

            Assert.AreEqual("500", bp.Resources["Iron"]);
            Assert.AreEqual("250", bp.Resources["Carbon"]);
            Assert.AreEqual("100", bp.Resources["Titanium"]);
        }

        [Test]
        public void ProcessHtml_ResourceQuantityWithCommas_StripsNonDigits()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(ResourceRow("Iron", "1,500")));

            Assert.AreEqual("1500", bp.Resources["Iron"]);
        }

        [Test]
        public void ProcessHtml_NoResources_ResourcesDictionaryIsEmpty()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(TitleDiv("Empty Blueprint")));

            Assert.IsNotNull(bp.Resources);
            Assert.AreEqual(0, bp.Resources.Count);
        }

        // -----------------------------------------------------------------------
        // processHtml — properties
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_SingleProperty_IsExtracted()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(PropRow("Mass", "450")));

            string val;
            bp.Properties.getString("Mass", null, out val);
            Assert.AreEqual("450", val);
        }

        [Test]
        public void ProcessHtml_PropertyWithDeltaText_DeltaIsStripped()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(PropRow("Power", "1200 (▲ 435)")));

            string val;
            bp.Properties.getString("Power", null, out val);
            Assert.AreEqual("1200", val);
        }

        [Test]
        public void ProcessHtml_MultipleProperties_AllExtracted()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, Html(
                PropRow("Mass", "450") +
                PropRow("Health", "2000") +
                PropRow("PowerRequired", "150")));

            string mass, health, power;
            bp.Properties.getString("Mass", null, out mass);
            bp.Properties.getString("Health", null, out health);
            bp.Properties.getString("PowerRequired", null, out power);

            Assert.AreEqual("450", mass);
            Assert.AreEqual("2000", health);
            Assert.AreEqual("150", power);
        }

        // -----------------------------------------------------------------------
        // processHtml — robustness
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_EmptyHtml_DoesNotThrow()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.DoesNotThrow(() => _scanner.processHtml(bp, Html("")));
        }

        [Test]
        public void ProcessHtml_MalformedHtml_DoesNotThrow()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.DoesNotThrow(() => _scanner.processHtml(bp, "<div unclosed"));
        }

        // -----------------------------------------------------------------------
        // AMX-LL Milspec — full integration from external files
        // -----------------------------------------------------------------------

        [Test]
        public void ProcessHtml_AMX_LL_Milspec()
        {
            string page1 = LoadTestData("BP_AMX_LL_Milspec_Page1.html");
            string page2 = LoadTestData("BP_AMX_LL_Milspec_Page2.html");

            var bp = new OE2EmpireTracker.Data.Blueprint();
            _scanner.processHtml(bp, page1);
            _scanner.processHtml(bp, page2);

            Assert.AreEqual("Reactor that generates power for the ship", bp.Description);
            string equipClass;
            bp.Properties.getString("Class", null, out equipClass);
            Assert.AreEqual("6", equipClass);
            Assert.AreEqual(6, bp.Class);

            string manuTime;
            bp.Properties.getString("ManufactureTime", null, out manuTime);
            Assert.AreEqual("9h", manuTime);

            string mass;
            bp.Properties.getString("Mass", null, out mass);
            Assert.AreEqual("861", mass);

            string cargoVolumeSize;
            bp.Properties.getString("CargoVolumeSize", null, out cargoVolumeSize);
            Assert.AreEqual("360", cargoVolumeSize);

            string health;
            bp.Properties.getString("Health", null, out health);
            Assert.AreEqual("4824", health);

            string engCap;
            bp.Properties.getString("EngCapacityRequired", null, out engCap);
            Assert.AreEqual("1080", engCap);

            string powerRegenRate;
            bp.Properties.getString("PowerRegenerationRate", null, out powerRegenRate);
            Assert.AreEqual("31.5", powerRegenRate);

            string wearRate;
            bp.Properties.getString("WearAndTearRate", null, out wearRate);
            Assert.AreEqual("2.959", wearRate);

            string dmgRate;
            bp.Properties.getString("MaximumDamageRepairRate", null, out dmgRate);
            Assert.AreEqual("86.57", dmgRate);

            Assert.AreEqual("9366", bp.Resources["Alkaline Earth Metals"]);
            Assert.AreEqual("1927", bp.Resources["Acidic Inorganics"]);
            Assert.AreEqual("2121", bp.Resources["Heavy Trans-Metals"]);
            Assert.AreEqual("2036", bp.Resources["Complex Non-Metallics"]);
            Assert.AreEqual("2440", bp.Resources["Heavy Alkaline Earth Metals"]);
            Assert.AreEqual("699",  bp.Resources["S1. Translivermoric Exotics"]);
        }
    }
}
