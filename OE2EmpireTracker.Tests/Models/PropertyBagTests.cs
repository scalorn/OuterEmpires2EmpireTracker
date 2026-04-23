using Newtonsoft.Json;
using NUnit.Framework;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Tests.Models
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
            Assert.That(_bag.Properties.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // ContainsKey
        // -----------------------------------------------------------------------

        [Test]
        public void ContainsKey_AfterSet_ReturnsTrue()
        {
            _bag.SetProperty("Key", "Value");
            Assert.That(_bag.ContainsKey("Key"), Is.True);
        }

        [Test]
        public void ContainsKey_UnknownKey_ReturnsFalse()
        {
            Assert.That(_bag.ContainsKey("Missing"), Is.False);
        }

        // -----------------------------------------------------------------------
        // SetProperty -- string overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_String_StoresValue()
        {
            _bag.SetProperty("Name", "Iron");
            string val;
            _bag.GetString("Name", null, out val);
            Assert.That(val, Is.EqualTo("Iron"));
        }

        [Test]
        public void SetProperty_String_OverwritesExistingValue()
        {
            _bag.SetProperty("Name", "Iron");
            _bag.SetProperty("Name", "Gold");
            string val;
            _bag.GetString("Name", null, out val);
            Assert.That(val, Is.EqualTo("Gold"));
        }

        [Test]
        public void SetProperty_String_ReturnsTrue()
        {
            Assert.That(_bag.SetProperty("Key", "Value"), Is.True);
        }

        // -----------------------------------------------------------------------
        // SetProperty -- decimal overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Double_StoresAndRetrievesValue()
        {
            _bag.SetProperty("Power", 42.5m);
            decimal val;
            bool found = _bag.GetDecimal("Power", 0, out val);
            Assert.That(found, Is.True);
            Assert.That(val, Is.EqualTo(42.5m));
        }

        // -----------------------------------------------------------------------
        // SetProperty -- bool overload
        // -----------------------------------------------------------------------

        [Test]
        public void SetProperty_Bool_True_StoresAndRetrievesTrue()
        {
            _bag.SetProperty("Online", true);
            bool val;
            bool found = _bag.GetBoolean("Online", false, out val);
            Assert.That(found, Is.True);
            Assert.That(val, Is.True);
        }

        [Test]
        public void SetProperty_Bool_False_StoresAndRetrievesFalse()
        {
            _bag.SetProperty("Built", false);
            bool val;
            _bag.GetBoolean("Built", true, out val);
            Assert.That(val, Is.False);
        }

        // -----------------------------------------------------------------------
        // GetDecimal
        // -----------------------------------------------------------------------

        [Test]
        public void GetDecimal_MissingKey_ReturnsFalseAndDefault()
        {
            decimal val;
            bool found = _bag.GetDecimal("Missing", 99.0m, out val);
            Assert.That(found, Is.False);
            Assert.That(val, Is.EqualTo(99.0m));
        }

        [Test]
        public void GetDecimal_NonNumericValue_ReturnsFalse()
        {
            _bag.SetProperty("Bad", "notanumber");
            decimal val;
            bool found = _bag.GetDecimal("Bad", 0, out val);
            Assert.That(found, Is.False);
        }

        // -----------------------------------------------------------------------
        // GetLong
        // -----------------------------------------------------------------------

        [Test]
        public void GetLong_StoredValue_ReturnsCorrectValue()
        {
            _bag.SetProperty("Count", "12345");
            long val;
            bool found = _bag.GetLong("Count", 0, out val);
            Assert.That(found, Is.True);
            Assert.That(val, Is.EqualTo(12345L));
        }

        [Test]
        public void GetLong_MissingKey_ReturnsFalseAndDefault()
        {
            long val;
            bool found = _bag.GetLong("Missing", 7L, out val);
            Assert.That(found, Is.False);
            Assert.That(val, Is.EqualTo(7L));
        }

        [Test]
        public void GetLong_NonNumericValue_ReturnsFalse()
        {
            _bag.SetProperty("Bad", "notanumber");
            long val;
            bool found = _bag.GetLong("Bad", 0, out val);
            Assert.That(found, Is.False);
        }

        // -----------------------------------------------------------------------
        // GetBoolean
        // -----------------------------------------------------------------------

        [Test]
        public void GetBoolean_MissingKey_ReturnsFalseAndDefault()
        {
            bool val;
            bool found = _bag.GetBoolean("Missing", true, out val);
            Assert.That(found, Is.False);
            Assert.That(val, Is.True); // default returned
        }

        [Test]
        public void GetBoolean_NonBoolValue_ReturnsFalse()
        {
            _bag.SetProperty("Bad", "notabool");
            bool val;
            bool found = _bag.GetBoolean("Bad", false, out val);
            Assert.That(found, Is.False);
        }

        // -----------------------------------------------------------------------
        // GetString
        // -----------------------------------------------------------------------

        [Test]
        public void GetString_StoredValue_ReturnsCorrectValue()
        {
            _bag.SetProperty("Label", "Hello");
            string val;
            bool found = _bag.GetString("Label", null, out val);
            Assert.That(found, Is.True);
            Assert.That(val, Is.EqualTo("Hello"));
        }

        [Test]
        public void GetString_MissingKey_ReturnsFalseAndDefault()
        {
            string val;
            bool found = _bag.GetString("Missing", "default", out val);
            Assert.That(found, Is.False);
            Assert.That(val, Is.EqualTo("default"));
        }

        // -----------------------------------------------------------------------
        // Remove
        // -----------------------------------------------------------------------

        [Test]
        public void Remove_ExistingKey_ReturnsTrueAndRemoves()
        {
            _bag.SetProperty("Key", "Value");
            bool result = _bag.Remove("Key");
            Assert.That(result, Is.True);
            Assert.That(_bag.ContainsKey("Key"), Is.False);
        }

        [Test]
        public void Remove_UnknownKey_ReturnsFalse()
        {
            bool result = _bag.Remove("Missing");
            Assert.That(result, Is.False);
        }

        // -----------------------------------------------------------------------
        // Clear
        // -----------------------------------------------------------------------

        [Test]
        public void Clear_RemovesAllProperties()
        {
            _bag.SetProperty("A", "1");
            _bag.SetProperty("B", "2");
            _bag.Clear();
            Assert.That(_bag.Properties.Count, Is.EqualTo(0));
        }

        // -----------------------------------------------------------------------
        // JSON round-trip
        // -----------------------------------------------------------------------

        [Test]
        public void JsonRoundTrip_StringValues_Preserved()
        {
            _bag.SetProperty("Name", "Iron");
            _bag.SetProperty("Type", "Resource");

            string json = JsonConvert.SerializeObject(_bag);
            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);

            string name, type;
            restored.GetString("Name", null, out name);
            restored.GetString("Type", null, out type);
            Assert.That(name, Is.EqualTo("Iron"));
            Assert.That(type, Is.EqualTo("Resource"));
        }

        [Test]
        public void JsonRoundTrip_NumericAndBoolValues_Preserved()
        {
            _bag.SetProperty("Power", 100.5m);
            _bag.SetProperty("Online", true);

            string json = JsonConvert.SerializeObject(_bag);
            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);

            decimal power;
            bool online;
            restored.GetDecimal("Power", 0, out power);
            restored.GetBoolean("Online", false, out online);
            Assert.That(power, Is.EqualTo(100.5m));
            Assert.That(online, Is.True);
        }

        [Test]
        public void JsonRoundTrip_EmptyBag_ProducesEmptyObject()
        {
            string json = JsonConvert.SerializeObject(_bag);
            Assert.That(json, Is.EqualTo("{}"));

            var restored = JsonConvert.DeserializeObject<PropertyBag>(json);
            Assert.That(restored.Properties.Count, Is.EqualTo(0));
        }
    }
}
