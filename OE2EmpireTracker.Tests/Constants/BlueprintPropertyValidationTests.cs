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
            Assert.AreEqual(PropertyValueType.ComboBox,
                BlueprintPropertyValidation.GetPropertyType("CommodityIndustry"));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — CheckBox
        // -----------------------------------------------------------------------

        [TestCase("CanManufacture")]
        [TestCase("CanResearch")]
        [TestCase("Consumable")]
        public void GetPropertyType_CheckBoxProperties_ReturnsCheckBox(string propertyName)
        {
            Assert.AreEqual(PropertyValueType.CheckBox,
                BlueprintPropertyValidation.GetPropertyType(propertyName));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Integer
        // -----------------------------------------------------------------------

        [TestCase("BlueCollarDetail")]
        [TestCase("PowerProvided")]
        [TestCase("WarehouseCapacity")]
        public void GetPropertyType_IntegerProperties_ReturnsInteger(string propertyName)
        {
            Assert.AreEqual(PropertyValueType.Integer,
                BlueprintPropertyValidation.GetPropertyType(propertyName));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Decimal
        // -----------------------------------------------------------------------

        [TestCase("CooldownTime")]
        [TestCase("MaxJumpDistance")]
        public void GetPropertyType_DecimalProperties_ReturnsDecimal(string propertyName)
        {
            Assert.AreEqual(PropertyValueType.Decimal,
                BlueprintPropertyValidation.GetPropertyType(propertyName));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Time
        // -----------------------------------------------------------------------

        [Test]
        public void GetPropertyType_ManufactureTime_ReturnsTime()
        {
            Assert.AreEqual(PropertyValueType.Time,
                BlueprintPropertyValidation.GetPropertyType("ManufactureTime"));
        }

        // -----------------------------------------------------------------------
        // GetPropertyType — Unknown
        // -----------------------------------------------------------------------

        [Test]
        public void GetPropertyType_UnknownProperty_ReturnsUnknown()
        {
            Assert.AreEqual(PropertyValueType.Unknown,
                BlueprintPropertyValidation.GetPropertyType("SomeRandomProperty"));
        }

        // -----------------------------------------------------------------------
        // GetValidationPattern
        // -----------------------------------------------------------------------

        [Test]
        public void GetValidationPattern_IntegerProperty_ReturnsIntegerPattern()
        {
            Assert.AreEqual(BlueprintPropertyValidation.INTEGER_PATTERN,
                BlueprintPropertyValidation.GetValidationPattern("BlueCollarDetail"));
        }

        [Test]
        public void GetValidationPattern_DecimalProperty_ReturnsDecimalPattern()
        {
            Assert.AreEqual(BlueprintPropertyValidation.DECIMAL_PATTERN,
                BlueprintPropertyValidation.GetValidationPattern("CooldownTime"));
        }

        [Test]
        public void GetValidationPattern_BooleanProperty_ReturnsNull()
        {
            // CheckBox properties use a different rendering — no regex pattern needed
            Assert.IsNull(BlueprintPropertyValidation.GetValidationPattern("CanManufacture"));
        }

        [Test]
        public void GetValidationPattern_TimeProperty_ReturnsTimePattern()
        {
            Assert.AreEqual(BlueprintPropertyValidation.TIME_PATTERN,
                BlueprintPropertyValidation.GetValidationPattern("ManufactureTime"));
        }

        [Test]
        public void GetValidationPattern_UnknownProperty_ReturnsNull()
        {
            Assert.IsNull(BlueprintPropertyValidation.GetValidationPattern("UnknownProp"));
        }

        // -----------------------------------------------------------------------
        // GetComboBoxDataSource
        // -----------------------------------------------------------------------

        [Test]
        public void GetComboBoxDataSource_CommodityIndustry_ReturnsNonNullList()
        {
            IList result = BlueprintPropertyValidation.GetComboBoxDataSource("CommodityIndustry");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void GetComboBoxDataSource_UnknownProperty_ReturnsNull()
        {
            Assert.IsNull(BlueprintPropertyValidation.GetComboBoxDataSource("UnknownProp"));
        }

        [Test]
        public void GetComboBoxDataSource_CommodityIndustry_ContainsKnownIndustryNames()
        {
            var result = BlueprintPropertyValidation.GetComboBoxDataSource("CommodityIndustry") as List<string>;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Engineering Block"));
            Assert.IsTrue(result.Contains("Agridome"));
            Assert.IsTrue(result.Contains("Science Centre"));
        }
    }
}
