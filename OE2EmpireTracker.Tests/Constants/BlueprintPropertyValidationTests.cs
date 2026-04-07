using NUnit.Framework;
using OE2EmpireTracker.Constants;
using System.Collections;
using System.Collections.Generic;

namespace OE2EmpireTracker.Tests.Constants
{
    [TestFixture]
    public class BlueprintPropertyValidationTests
    {
        // -----------------------------------------------------------------------
        // GetPropertyType — ComboBox
        // -----------------------------------------------------------------------

        [Test]
        public void GetPropertyType_CommodityIndustry_ReturnsComboBox()
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType("Commodity Industry"), Is.EqualTo(PropertyValueType.ComboBox));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — CheckBox
        // -----------------------------------------------------------------------

        [TestCase("Can Manufacture")]
        [TestCase("Can Research")]
        [TestCase("Consumable")]
        public void GetPropertyType_CheckBoxProperties_ReturnsCheckBox(string propertyName)
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType(propertyName), Is.EqualTo(PropertyValueType.CheckBox));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Integer
        // -----------------------------------------------------------------------

        [TestCase("Blue Collar Detail")]
        [TestCase("Power Provided")]
        [TestCase("Warehouse Capacity")]
        public void GetPropertyType_IntegerProperties_ReturnsInteger(string propertyName)
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType(propertyName), Is.EqualTo(PropertyValueType.Integer));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Decimal
        // -----------------------------------------------------------------------

        [TestCase("Cooldown Time")]
        [TestCase("Max Jump Distance")]
        public void GetPropertyType_DecimalProperties_ReturnsDecimal(string propertyName)
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType(propertyName), Is.EqualTo(PropertyValueType.Decimal));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Time
        // -----------------------------------------------------------------------

        [Test]
        public void GetPropertyType_ManufactureTime_ReturnsTime()
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType("Manufacture Run Time"), Is.EqualTo(PropertyValueType.Time));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Unknown
        // -----------------------------------------------------------------------

        [Test]
        public void GetPropertyType_UnknownProperty_ReturnsUnknown()
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType("SomeRandomProperty"), Is.EqualTo(PropertyValueType.Unknown));
        }

        // -----------------------------------------------------------------------
        // GetValidationPattern
        // -----------------------------------------------------------------------

        [Test]
        public void GetValidationPattern_IntegerProperty_ReturnsIntegerPattern()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("Blue Collar Detail"), Is.EqualTo(BlueprintPropertyValidation.INTEGER_PATTERN));
        }

        [Test]
        public void GetValidationPattern_DecimalProperty_ReturnsDecimalPattern()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("Cooldown Time"), Is.EqualTo(BlueprintPropertyValidation.DECIMAL_PATTERN));
        }

        [Test]
        public void GetValidationPattern_BooleanProperty_ReturnsNull()
        {
            // CheckBox properties use a different rendering — no regex pattern needed
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("Can Manufacture"), Is.Null);
        }

        [Test]
        public void GetValidationPattern_TimeProperty_ReturnsTimePattern()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("Manufacture Run Time"), Is.EqualTo(BlueprintPropertyValidation.TIME_PATTERN));
        }

        [Test]
        public void GetValidationPattern_UnknownProperty_ReturnsNull()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("UnknownProp"), Is.Null);
        }

        // -----------------------------------------------------------------------
        // GetComboBoxDataSource
        // -----------------------------------------------------------------------

        [Test]
        public void GetComboBoxDataSource_CommodityIndustry_ReturnsNonNullList()
        {
            IList result = BlueprintPropertyValidation.GetComboBoxDataSource("Commodity Industry");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count > 0, Is.True);
        }

        [Test]
        public void GetComboBoxDataSource_UnknownProperty_ReturnsNull()
        {
            Assert.That(BlueprintPropertyValidation.GetComboBoxDataSource("UnknownProp"), Is.Null);
        }

        [Test]
        public void GetComboBoxDataSource_CommodityIndustry_ContainsKnownIndustryNames()
        {
            var result = BlueprintPropertyValidation.GetComboBoxDataSource("Commodity Industry") as List<string>;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Contains("Engineering Block"), Is.True);
            Assert.That(result.Contains("Agridome"), Is.True);
            Assert.That(result.Contains("Science Centre"), Is.True);
        }
    }
}
