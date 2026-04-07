using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;
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
                PropRow("PowerRequired", "150")));

            string mass, health, power;
            bp.Properties.getString("Mass", null, out mass);
            bp.Properties.getString("Health", null, out health);
            bp.Properties.getString("PowerRequired", null, out power);

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
            bp.Properties.getString("ManufactureTime", null, out manuTime);
            Assert.That(manuTime, Is.EqualTo("9h"));

            string mass;
            bp.Properties.getString("Mass", null, out mass);
            Assert.That(mass, Is.EqualTo("861"));

            string cargoVolumeSize;
            bp.Properties.getString("CargoVolumeSize", null, out cargoVolumeSize);
            Assert.That(cargoVolumeSize, Is.EqualTo("360"));

            string health;
            bp.Properties.getString("Health", null, out health);
            Assert.That(health, Is.EqualTo("4824"));

            string engCap;
            bp.Properties.getString("EngCapacityRequired", null, out engCap);
            Assert.That(engCap, Is.EqualTo("1080"));

            string powerRegenRate;
            bp.Properties.getString("PowerRegenerationRate", null, out powerRegenRate);
            Assert.That(powerRegenRate, Is.EqualTo("31.5"));

            string wearRate;
            bp.Properties.getString("WearAndTearRate", null, out wearRate);
            Assert.That(wearRate, Is.EqualTo("2.959"));

            string dmgRate;
            bp.Properties.getString("MaximumDamageRepairRate", null, out dmgRate);
            Assert.That(dmgRate, Is.EqualTo("86.57"));

            Assert.That(bp.Resources["Alkaline Earth Metals"], Is.EqualTo("9366"));
            Assert.That(bp.Resources["Acidic Inorganics"], Is.EqualTo("1927"));
            Assert.That(bp.Resources["Heavy Trans-Metals"], Is.EqualTo("2121"));
            Assert.That(bp.Resources["Complex Non-Metallics"], Is.EqualTo("2036"));
            Assert.That(bp.Resources["Heavy Alkaline Earth Metals"], Is.EqualTo("2440"));
            Assert.That(bp.Resources["S1. Translivermoric Exotics"], Is.EqualTo("699"));
        }
    }
}
