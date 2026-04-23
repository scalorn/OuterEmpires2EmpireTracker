using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class BlueprintTests
    {
        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_ItemTypeIsBlueprint()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.That(bp.ItemType, Is.EqualTo(ItemType.ItemTypeEnum.Blueprint));
        }

        [Test]
        public void DefaultConstructor_NameIsEmpty()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.That(bp.Name, Is.EqualTo(string.Empty));
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.That(bp.Properties, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_ResourcesIsNotNull()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.That(bp.Resources, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_NumericDefaultsAreZero()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint();
            Assert.That(bp.Evolution, Is.EqualTo(0));
            Assert.That(bp.Class, Is.EqualTo(0));
            Assert.That(bp.CopyCost, Is.EqualTo(0));
        }

        [Test]
        public void NamedConstructor_SetsName()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint("Pulse Cannon");
            Assert.That(bp.Name, Is.EqualTo("Pulse Cannon"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- UUID null guard
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NullUUID_ReturnsEmpty()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = null, Name = "Test" };
            Assert.That(bp.ExtendedName, Is.EqualTo(string.Empty));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Name only
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NameOnly_ReturnsName()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Pulse Cannon" };
            Assert.That(bp.ExtendedName, Is.EqualTo("Pulse Cannon"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Class prefix
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithClass_PrependsClassPrefix()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Thruster", Class = 3 };
            Assert.That(bp.ExtendedName, Does.StartWith("C3 "));
        }

        [Test]
        public void ExtendedName_ClassZero_NoClassPrefix()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Thruster", Class = 0 };
            Assert.That(bp.ExtendedName, Does.Not.Contain("C0"));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- Evolution
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithEvolution_ContainsEvSegment()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Shield", Evolution = 2 };
            Assert.That(bp.ExtendedName, Does.Contain("Ev(2)"));
        }

        [Test]
        public void ExtendedName_EvolutionZero_NoEvSegment()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Shield", Evolution = 0 };
            Assert.That(bp.ExtendedName, Does.Not.Contain("Ev("));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- TechLevel
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithTechLevel_ContainsTechLevelInParentheses()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Cannon", TechLevel = "MilSpec" };
            Assert.That(bp.ExtendedName, Does.Contain("(MilSpec)"));
        }

        [Test]
        public void ExtendedName_NullTechLevel_NoTechLevelSegment()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Cannon", TechLevel = null };
            Assert.That(bp.ExtendedName, Does.Not.Contain("("));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- NickName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithNickName_ContainsNickNameInSquareBrackets()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Drive", NickName = "Fast One" };
            Assert.That(bp.ExtendedName, Does.Contain("[Fast One]"));
        }

        [Test]
        public void ExtendedName_EmptyNickName_NoSquareBrackets()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Drive", NickName = string.Empty };
            Assert.That(bp.ExtendedName, Does.Not.Contain("["));
        }

        // -----------------------------------------------------------------------
        // ExtendedName -- full combination
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_AllSegments_CorrectOrder()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = "uuid-1",
                Name = "Cannon",
                Class = 2,
                Evolution = 3,
                TechLevel = "MilSpec",
                NickName = "Big Gun"
            };

            string name = bp.ExtendedName;
            // Order: C{Class} Ev({Evolution}) Name (TechLevel) [NickName]
            Assert.That(
                name.IndexOf("C2") < name.IndexOf("Ev(3)"),
                Is.True,
                "Class before Evolution");
            Assert.That(
                name.IndexOf("Ev(3)") < name.IndexOf("Cannon"),
                Is.True,
                "Evolution before Name");
            Assert.That(
                name.IndexOf("Cannon") < name.IndexOf("(MilSpec)"),
                Is.True,
                "Name before TechLevel");
            Assert.That(
                name.IndexOf("(MilSpec)") < name.IndexOf("[Big Gun]"),
                Is.True,
                "TechLevel before NickName");
        }

        [Test]
        public void ExtendedName_IsTrimmed()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Drive" };
            Assert.That(bp.ExtendedName.Trim(), Is.EqualTo(bp.ExtendedName));
        }

        // -----------------------------------------------------------------------
        // JSON serialization -- ExtendedName is ignored
        // -----------------------------------------------------------------------

        [Test]
        public void JsonSerialization_ExtendedNameIsNotIncluded()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint { UUID = "uuid-1", Name = "Cannon", Evolution = 1 };
            string json = JsonConvert.SerializeObject(bp);
            Assert.That(json, Does.Not.Contain("ExtendedName"));
        }

        [Test]
        public void JsonRoundTrip_CoreProperties_Preserved()
        {
            var bp = new OE2EmpireTracker.Models.Blueprint
            {
                UUID = "uuid-1",
                Name = "Cannon",
                Evolution = 2,
                Class = 3,
                TechLevel = "MilSpec",
                NickName = "Big Gun",
                CopyCost = 500
            };

            string json = JsonConvert.SerializeObject(bp);
            var restored = JsonConvert.DeserializeObject<OE2EmpireTracker.Models.Blueprint>(json);

            Assert.That(restored.UUID, Is.EqualTo(bp.UUID));
            Assert.That(restored.Name, Is.EqualTo(bp.Name));
            Assert.That(restored.Evolution, Is.EqualTo(bp.Evolution));
            Assert.That(restored.Class, Is.EqualTo(bp.Class));
            Assert.That(restored.TechLevel, Is.EqualTo(bp.TechLevel));
            Assert.That(restored.NickName, Is.EqualTo(bp.NickName));
            Assert.That(restored.CopyCost, Is.EqualTo(bp.CopyCost));
        }
    }
}
