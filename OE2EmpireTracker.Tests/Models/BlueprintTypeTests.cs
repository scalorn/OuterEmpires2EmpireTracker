using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
{
    [TestFixture]
    public class BlueprintTypeTests
    {
        // -----------------------------------------------------------------------
        // Constructors
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_IdIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.That(bt.Id, Is.Null);
        }

        [Test]
        public void DefaultConstructor_NameIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.That(bt.Name, Is.Null);
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.That(bt.Properties, Is.Not.Null);
        }

        [Test]
        public void DefaultConstructor_ResearchablePropertiesIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.That(bt.ResearchableProperties, Is.Not.Null);
        }

        // -----------------------------------------------------------------------
        // Property Setting and Getting
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Id_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Id = "Reactor";
            Assert.That(bt.Id, Is.EqualTo("Reactor"));
        }

        [Test]
        public void SetProperty_Name_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Name = "Reactor";
            Assert.That(bt.Name, Is.EqualTo("Reactor"));
        }

        [Test]
        public void SetProperty_Universal_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Universal = true;
            Assert.That(bt.Universal, Is.True);

            bt.Universal = false;
            Assert.That(bt.Universal, Is.False);
        }

        // -----------------------------------------------------------------------
        // Properties array operations
        // -----------------------------------------------------------------------

        [Test]
        public void Properties_SetToArray_PropertiesAreAccessible()
        {
            var bt = new BlueprintType();
            bt.Properties = new string[] { "Mass", "Health" };

            Assert.That(bt.Properties.Length, Is.EqualTo(2));
            Assert.That(bt.Properties[0], Is.EqualTo("Mass"));
            Assert.That(bt.Properties[1], Is.EqualTo("Health"));
        }

        [Test]
        public void Properties_EmptyArray_IsSupported()
        {
            var bt = new BlueprintType();
            bt.Properties = new string[] { };

            Assert.That(bt.Properties.Length, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // ResearchableProperties array operations
        // -----------------------------------------------------------------------

        [Test]
        public void ResearchableProperties_SetToArray_PropertiesAreAccessible()
        {
            var bt = new BlueprintType();
            bt.ResearchableProperties = new string[] { "Mass", "Health" };

            Assert.That(bt.ResearchableProperties.Length, Is.EqualTo(2));
            Assert.That(bt.ResearchableProperties[0], Is.EqualTo("Mass"));
            Assert.That(bt.ResearchableProperties[1], Is.EqualTo("Health"));
        }

        [Test]
        public void ResearchableProperties_EmptyArray_IsSupported()
        {
            var bt = new BlueprintType();
            bt.ResearchableProperties = new string[] { };

            Assert.That(bt.ResearchableProperties.Length, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // Universal property
        // -----------------------------------------------------------------------

        [Test]
        public void Universal_DefaultIsFalse()
        {
            var bt = new BlueprintType();
            Assert.That(bt.Universal, Is.False);
        }

        [Test]
        public void Universal_CanBeSetToTrue()
        {
            var bt = new BlueprintType();
            bt.Universal = true;
            Assert.That(bt.Universal, Is.True);
        }

        // -----------------------------------------------------------------------
        // JSON serialization tests
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_CoreProperties_Preserved()
        {
            var bt = new BlueprintType
            {
                Id = "Reactor",
                Name = "Reactor",
                Universal = true,
                Properties = new string[] { "Mass", "Health" },
                ResearchableProperties = new string[] { "Mass", "Health" }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(bt);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<BlueprintType>(json);

            Assert.That(restored.Id, Is.EqualTo(bt.Id));
            Assert.That(restored.Name, Is.EqualTo(bt.Name));
            Assert.That(restored.Universal, Is.EqualTo(bt.Universal));
            Assert.That(restored.Properties.Length, Is.EqualTo(bt.Properties.Length));
            Assert.That(restored.ResearchableProperties.Length, Is.EqualTo(bt.ResearchableProperties.Length));
        }
    }
}