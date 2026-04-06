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
            Assert.That(BlueprintPropertyValidation.GetPropertyType("CommodityIndustry"), Is.EqualTo(PropertyValueType.ComboBox));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — CheckBox
        // -----------------------------------------------------------------------

        [TestCase("CanManufacture")]
        [TestCase("CanResearch")]
        [TestCase("Consumable")]
        public void GetPropertyType_CheckBoxProperties_ReturnsCheckBox(string propertyName)
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType(propertyName), Is.EqualTo(PropertyValueType.CheckBox));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Integer
        // -----------------------------------------------------------------------

        [TestCase("BlueCollarDetail")]
        [TestCase("PowerProvided")]
        [TestCase("WarehouseCapacity")]
        public void GetPropertyType_IntegerProperties_ReturnsInteger(string propertyName)
        {
            Assert.That(BlueprintPropertyValidation.GetPropertyType(propertyName), Is.EqualTo(PropertyValueType.Integer));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Decimal
        // -----------------------------------------------------------------------

        [TestCase("CooldownTime")]
        [TestCase("MaxJumpDistance")]
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
            Assert.That(BlueprintPropertyValidation.GetPropertyType("ManufactureTime"), Is.EqualTo(PropertyValueType.Time));
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
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("BlueCollarDetail"), Is.EqualTo(BlueprintPropertyValidation.INTEGER_PATTERN));
        }

        [Test]
        public void GetValidationPattern_DecimalProperty_ReturnsDecimalPattern()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("CooldownTime"), Is.EqualTo(BlueprintPropertyValidation.DECIMAL_PATTERN));
        }

        [Test]
        public void GetValidationPattern_BooleanProperty_ReturnsNull()
        {
            // CheckBox properties use a different rendering — no regex pattern needed
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("CanManufacture"), Is.Null);
        }

        [Test]
        public void GetValidationPattern_TimeProperty_ReturnsTimePattern()
        {
            Assert.That(BlueprintPropertyValidation.GetValidationPattern("ManufactureTime"), Is.EqualTo(BlueprintPropertyValidation.TIME_PATTERN));
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
            IList result = BlueprintPropertyValidation.GetComboBoxDataSource("CommodityIndustry");
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
            var result = BlueprintPropertyValidation.GetComboBoxDataSource("CommodityIndustry") as List<string>;
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Contains("Engineering Block"), Is.True);
            Assert.That(result.Contains("Agridome"), Is.True);
            Assert.That(result.Contains("Science Centre"), Is.True);
        }
    }
}
