using NUnit.Framework;
using OE2EmpireTracker.Baseline;

namespace OE2EmpireTracker.Tests.Baseline
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
            Assert.IsNull(bt.Id);
        }

        [Test]
        public void DefaultConstructor_NameIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.IsNull(bt.Name);
        }

        [Test]
        public void DefaultConstructor_PropertiesIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.IsNotNull(bt.Properties);
        }

        [Test]
        public void DefaultConstructor_ResearchablePropertiesIsNotNull()
        {
            var bt = new BlueprintType();
            Assert.IsNotNull(bt.ResearchableProperties);
        }

        // -----------------------------------------------------------------------
        // Property Setting and Getting
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Id_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Id = "Reactor";
            Assert.AreEqual("Reactor", bt.Id);
        }

        [Test]
        public void SetProperty_Name_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Name = "Reactor";
            Assert.AreEqual("Reactor", bt.Name);
        }

        [Test]
        public void SetProperty_Universal_CanBeSetAndRead()
        {
            var bt = new BlueprintType();
            bt.Universal = true;
            Assert.IsTrue(bt.Universal);

            bt.Universal = false;
            Assert.IsFalse(bt.Universal);
        }

        // -----------------------------------------------------------------------
        // Properties array operations
        // -----------------------------------------------------------------------

        [Test]
        public void Properties_SetToArray_PropertiesAreAccessible()
        {
            var bt = new BlueprintType();
            bt.Properties = new string[] { "Mass", "Health" };

            Assert.AreEqual(2, bt.Properties.Length);
            Assert.AreEqual("Mass", bt.Properties[0]);
            Assert.AreEqual("Health", bt.Properties[1]);
        }

        [Test]
        public void Properties_EmptyArray_IsSupported()
        {
            var bt = new BlueprintType();
            bt.Properties = new string[] { };

            Assert.AreEqual(0, bt.Properties.Length);
        }

        // -----------------------------------------------------------------------
        // ResearchableProperties array operations
        // -----------------------------------------------------------------------

        [Test]
        public void ResearchableProperties_SetToArray_PropertiesAreAccessible()
        {
            var bt = new BlueprintType();
            bt.ResearchableProperties = new string[] { "Mass", "Health" };

            Assert.AreEqual(2, bt.ResearchableProperties.Length);
            Assert.AreEqual("Mass", bt.ResearchableProperties[0]);
            Assert.AreEqual("Health", bt.ResearchableProperties[1]);
        }

        [Test]
        public void ResearchableProperties_EmptyArray_IsSupported()
        {
            var bt = new BlueprintType();
            bt.ResearchableProperties = new string[] { };

            Assert.AreEqual(0, bt.ResearchableProperties.Length);
        }

        // -----------------------------------------------------------------------
        // Universal property
        // -----------------------------------------------------------------------

        [Test]
        public void Universal_DefaultIsFalse()
        {
            var bt = new BlueprintType();
            Assert.IsFalse(bt.Universal);
        }

        [Test]
        public void Universal_CanBeSetToTrue()
        {
            var bt = new BlueprintType();
            bt.Universal = true;
            Assert.IsTrue(bt.Universal);
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

            Assert.AreEqual(bt.Id, restored.Id);
            Assert.AreEqual(bt.Name, restored.Name);
            Assert.AreEqual(bt.Universal, restored.Universal);
            Assert.AreEqual(bt.Properties.Length, restored.Properties.Length);
            Assert.AreEqual(bt.ResearchableProperties.Length, restored.ResearchableProperties.Length);
        }

        // -----------------------------------------------------------------------
        // BlueprintFieldEnum tests
        // -----------------------------------------------------------------------

        [Test]
        public void BlueprintFieldEnum_None_Is0()
        {
            Assert.AreEqual(0, (int)BlueprintField.BlueprintFieldEnum.None);
        }

        [Test]
        public void BlueprintFieldEnum_FuelUsed_Is1()
        {
            Assert.AreEqual(1, (int)BlueprintField.BlueprintFieldEnum.FuelUsed);
        }

        [Test]
        public void BlueprintFieldEnum_MaxJumpDistance_Is2()
        {
            Assert.AreEqual(2, (int)BlueprintField.BlueprintFieldEnum.MaxJumpDistance);
        }
    }
}