using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;

namespace OE2EmpireTracker.Tests.Data
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
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.AreEqual(ItemType.ItemTypeEnum.Blueprint, bp.ItemType);
        }

        [Test]
        public void DefaultConstructor_NameIsEmpty()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.AreEqual(string.Empty, bp.Name);
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.IsNotNull(bp.Properties);
        }

        [Test]
        public void DefaultConstructor_ResourcesIsNotNull()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.IsNotNull(bp.Resources);
        }

        [Test]
        public void DefaultConstructor_NumericDefaultsAreZero()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint();
            Assert.AreEqual(0, bp.Evolution);
            Assert.AreEqual(0, bp.Class);
            Assert.AreEqual(0, bp.CopyCost);
        }

        [Test]
        public void NamedConstructor_SetsName()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint("Pulse Cannon");
            Assert.AreEqual("Pulse Cannon", bp.Name);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — UUID null guard
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NullUUID_ReturnsEmpty()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = null, Name = "Test" };
            Assert.AreEqual(string.Empty, bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Name only
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_NameOnly_ReturnsName()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Pulse Cannon" };
            Assert.AreEqual("Pulse Cannon", bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Class prefix
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithClass_PrependsClassPrefix()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Thruster", Class = 3 };
            StringAssert.StartsWith("C3 ", bp.ExtendedName);
        }

        [Test]
        public void ExtendedName_ClassZero_NoClassPrefix()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Thruster", Class = 0 };
            StringAssert.DoesNotContain("C0", bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — Evolution
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithEvolution_ContainsEvSegment()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Shield", Evolution = 2 };
            StringAssert.Contains("Ev(2)", bp.ExtendedName);
        }

        [Test]
        public void ExtendedName_EvolutionZero_NoEvSegment()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Shield", Evolution = 0 };
            StringAssert.DoesNotContain("Ev(", bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — TechLevel
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithTechLevel_ContainsTechLevelInParentheses()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Cannon", TechLevel = "MilSpec" };
            StringAssert.Contains("(MilSpec)", bp.ExtendedName);
        }

        [Test]
        public void ExtendedName_NullTechLevel_NoTechLevelSegment()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Cannon", TechLevel = null };
            StringAssert.DoesNotContain("(", bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — NickName
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_WithNickName_ContainsNickNameInSquareBrackets()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Drive", NickName = "Fast One" };
            StringAssert.Contains("[Fast One]", bp.ExtendedName);
        }

        [Test]
        public void ExtendedName_EmptyNickName_NoSquareBrackets()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Drive", NickName = string.Empty };
            StringAssert.DoesNotContain("[", bp.ExtendedName);
        }

        // -----------------------------------------------------------------------
        // ExtendedName — full combination
        // -----------------------------------------------------------------------

        [Test]
        public void ExtendedName_AllSegments_CorrectOrder()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint
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
            Assert.IsTrue(name.IndexOf("C2") < name.IndexOf("Ev(3)"),   "Class before Evolution");
            Assert.IsTrue(name.IndexOf("Ev(3)") < name.IndexOf("Cannon"), "Evolution before Name");
            Assert.IsTrue(name.IndexOf("Cannon") < name.IndexOf("(MilSpec)"), "Name before TechLevel");
            Assert.IsTrue(name.IndexOf("(MilSpec)") < name.IndexOf("[Big Gun]"), "TechLevel before NickName");
        }

        [Test]
        public void ExtendedName_IsTrimmed()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Drive" };
            Assert.AreEqual(bp.ExtendedName, bp.ExtendedName.Trim());
        }

        // -----------------------------------------------------------------------
        // JSON serialization — ExtendedName is ignored
        // -----------------------------------------------------------------------

        [Test]
        public void JsonSerialization_ExtendedNameIsNotIncluded()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint { UUID = "uuid-1", Name = "Cannon", Evolution = 1 };
            string json = JsonConvert.SerializeObject(bp);
            StringAssert.DoesNotContain("ExtendedName", json);
        }

        [Test]
        public void JsonRoundTrip_CoreProperties_Preserved()
        {
            var bp = new OE2EmpireTracker.Data.Blueprint
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
            var restored = JsonConvert.DeserializeObject<OE2EmpireTracker.Data.Blueprint>(json);

            Assert.AreEqual(bp.UUID, restored.UUID);
            Assert.AreEqual(bp.Name, restored.Name);
            Assert.AreEqual(bp.Evolution, restored.Evolution);
            Assert.AreEqual(bp.Class, restored.Class);
            Assert.AreEqual(bp.TechLevel, restored.TechLevel);
            Assert.AreEqual(bp.NickName, restored.NickName);
            Assert.AreEqual(bp.CopyCost, restored.CopyCost);
        }
    }
}
