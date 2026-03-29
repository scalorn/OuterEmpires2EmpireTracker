using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using OE2EmpireTracker.Data;

namespace OE2EmpireTracker.Tests.Data
{
    [TestFixture]
    public class PropertyBagTests
    {
        private PropertyBag _bag;

        [SetUp]
        public void SetUp()
        {
            _bag = new PropertyBag();
        }

        // -----------------------------------------------------------------------
        // Constructor
        // -----------------------------------------------------------------------

        [Test]
        public void DefaultConstructor_PropertiesDictionaryIsEmpty()
        {
            Assert.AreEqual(0, _bag.Properties.Count);
        }

        // -----------------------------------------------------------------------
        // ContainsKey
        // -----------------------------------------------------------------------

        [Test]
        public void ContainsKey_AfterSet_ReturnsTrue()
        {
            _bag.setProperty("Key", "Value");
            Assert.IsTrue(_bag.ContainsKey("Key"));
        }

        [Test]
        public void ContainsKey_UnknownKey_ReturnsFalse()
        {
            Assert.IsFalse(_bag.ContainsKey("Missing"));
        }

        // -----------------------------------------------------------------------
        // setProperty — string overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_String_StoresValue()
        {
            _bag.setProperty("Name", "Iron");
            string val;
            _bag.getString("Name", null, out val);
            Assert.AreEqual("Iron", val);
        }

        [Test]
        public void SetProperty_String_OverwritesExistingValue()
        {
            _bag.setProperty("Name", "Iron");
            _bag.setProperty("Name", "Gold");
            string val;
            _bag.getString("Name", null, out val);
            Assert.AreEqual("Gold", val);
        }

        [Test]
        public void SetProperty_String_ReturnsTrue()
        {
            Assert.IsTrue(_bag.setProperty("Key", "Value"));
        }

        // -----------------------------------------------------------------------
        // setProperty — double overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Double_StoresAndRetrievesValue()
        {
            _bag.setProperty("Power", 42.5);
            double val;
            bool found = _bag.getDouble("Power", 0, out val);
            Assert.IsTrue(found);
            Assert.AreEqual(42.5, val, 0.0001);
        }

        // -----------------------------------------------------------------------
        // setProperty — bool overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Bool_True_StoresAndRetrievesTrue()
        {
            _bag.setProperty("Online", true);
            bool val;
            bool found = _bag.getBoolean("Online", false, out val);
            Assert.IsTrue(found);
            Assert.IsTrue(val);
        }

        [Test]
        public void SetProperty_Bool_False_StoresAndRetrievesFalse()
        {
            _bag.setProperty("Built", false);
            bool val;
            _bag.getBoolean("Built", true, out val);
            Assert.IsFalse(val);
        }

        // -----------------------------------------------------------------------
        // getDouble
        // -----------------------------------------------------------------------

        [Test]
        public void GetDouble_MissingKey_ReturnsFalseAndDefault()
        {
            double val;
            bool found = _bag.getDouble("Missing", 99.0, out val);
            Assert.IsFalse(found);
            Assert.AreEqual(99.0, val, 0.0001);
        }

        [Test]
        public void GetDouble_NonNumericValue_ReturnsFalse()
        {
            _bag.setProperty("Bad", "notanumber");
            double val;
            bool found = _bag.getDouble("Bad", 0, out val);
            Assert.IsFalse(found);
        }

        // -----------------------------------------------------------------------
        // getLong
        // -----------------------------------------------------------------------

        [Test]
        public void GetLong_StoredValue_ReturnsCorrectValue()
        {
            _bag.setProperty("Count", "12345");
            long val;
            bool found = _bag.getLong("Count", 0, out val);
            Assert.IsTrue(found);
            Assert.AreEqual(12345L, val);
        }

        [Test]
        public void GetLong_MissingKey_ReturnsFalseAndDefault()
        {
            long val;
            bool found = _bag.getLong("Missing", 7L, out val);
            Assert.IsFalse(found);
            Assert.AreEqual(7L, val);
        }

        [Test]
        public void GetLong_NonNumericValue_ReturnsFalse()
        {
            _bag.setProperty("Bad", "notanumber");
            long val;
            bool found = _bag.getLong("Bad", 0, out val);
            Assert.IsFalse(found);
        }

        // -----------------------------------------------------------------------
        // getBoolean
        // -----------------------------------------------------------------------

        [Test]
        public void GetBoolean_MissingKey_ReturnsFalseAndDefault()
        {
            bool val;
            bool found = _bag.getBoolean("Missing", true, out val);
            Assert.IsFalse(found);
            Assert.IsTrue(val); // default returned
        }

        [Test]
        public void GetBoolean_NonBoolValue_ReturnsFalse()
        {
            _bag.setProperty("Bad", "notabool");
            bool val;
            bool found = _bag.getBoolean("Bad", false, out val);
            Assert.IsFalse(found);
        }

        // -----------------------------------------------------------------------
        // getString
        // -----------------------------------------------------------------------

        [Test]
        public void GetString_StoredValue_ReturnsCorrectValue()
        {
            _bag.setProperty("Label", "Hello");
            string val;
            bool found = _bag.getString("Label", null, out val);
            Assert.IsTrue(found);
            Assert.AreEqual("Hello", val);
        }

        [Test]
        public void GetString_MissingKey_ReturnsFalseAndDefault()
        {
            string val;
            bool found = _bag.getString("Missing", "default", out val);
            Assert.IsFalse(found);
            Assert.AreEqual("default", val);
        }

        // -----------------------------------------------------------------------
        // Remove
        // -----------------------------------------------------------------------

        [Test]
        public void Remove_ExistingKey_ReturnsTrueAndRemoves()
        {
            _bag.setProperty("Key", "Value");
            bool result = _bag.Remove("Key");
            Assert.IsTrue(result);
            Assert.IsFalse(_bag.ContainsKey("Key"));
        }

        [Test]
        public void Remove_UnknownKey_ReturnsFalse()
        {
            bool result = _bag.Remove("Missing");
            Assert.IsFalse(result);
        }

        // -----------------------------------------------------------------------
        // Clear
        // -----------------------------------------------------------------------

        [Test]
        public void Clear_RemovesAllProperties()
        {
            _bag.setProperty("A", "1");
            _bag.setProperty("B", "2");
            _bag.Clear();
            Assert.AreEqual(0, _bag.Properties.Count);
        }

        // -----------------------------------------------------------------------
        // JSON round-trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_StringValues_Preserved()
        {
            _bag.setProperty("Name", "Iron");
            _bag.setProperty("Type", "Resource");

            string json = JsonConvert.SerializeObject(_bag);
            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);

            string name, type;
            restored.getString("Name", null, out name);
            restored.getString("Type", null, out type);
            Assert.AreEqual("Iron", name);
            Assert.AreEqual("Resource", type);
        }

        [Test]
        public void JsonRoundTrip_NumericAndBoolValues_Preserved()
        {
            _bag.setProperty("Power", 100.5);
            _bag.setProperty("Online", true);

            string json = JsonConvert.SerializeObject(_bag);
            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);

            double power;
            bool online;
            restored.getDouble("Power", 0, out power);
            restored.getBoolean("Online", false, out online);
            Assert.AreEqual(100.5, power, 0.0001);
            Assert.IsTrue(online);
        }

        [Test]
        public void JsonRoundTrip_EmptyBag_ProducesEmptyObject()
        {
            string json = JsonConvert.SerializeObject(_bag);
            Assert.AreEqual("{}", json);

            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);
            Assert.AreEqual(0, restored.Properties.Count);
        }
    }
}
