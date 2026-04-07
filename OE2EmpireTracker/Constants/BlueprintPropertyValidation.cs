using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Constants
{
    public enum PropertyValueType
    {
        Unknown,
        Integer,
        Decimal,
        Boolean,
        Time,
        ComboBox,
        CheckBox
    }

    /// <summary>
    /// Defines validation rules for blueprint property values.
    /// Unknown properties are allowed free-form but logged.
    /// </summary>
    public static class BlueprintPropertyValidation
    {
        private static readonly Dictionary<string, PropertyValueType> _propertyTypes = new Dictionary<string, PropertyValueType>
        {
            // Integer properties (game names with spaces)
            { "Blue Collar Detail", PropertyValueType.Integer },
            { "Cargo Capacity", PropertyValueType.Integer },
            { "Cargo Volume Size", PropertyValueType.Integer },
            { "Crew Supported", PropertyValueType.Integer },
            { "Eng Capacity Required", PropertyValueType.Integer },
            { "Entertainment Provided", PropertyValueType.Integer },
            { "Extraction Efficiency", PropertyValueType.Integer },
            { "Food Provision", PropertyValueType.Integer },
            { "Fuel Capacity", PropertyValueType.Integer },
            { "Habitation Provision", PropertyValueType.Integer },
            { "Health", PropertyValueType.Integer },
            { "Large Weapon Mounts", PropertyValueType.Integer },
            { "Manufacture Slot", PropertyValueType.Integer },
            { "Mass", PropertyValueType.Integer },
            { "Max Allowed On Ship", PropertyValueType.Integer },
            { "Max Per Colony", PropertyValueType.Integer },
            { "Medium Weapon Mounts", PropertyValueType.Integer },
            { "Mining Slot", PropertyValueType.Integer },
            { "Power Generated", PropertyValueType.Integer },
            { "Power Provided", PropertyValueType.Integer },
            { "Power Required", PropertyValueType.Integer },
            { "Refining Rate", PropertyValueType.Integer },
            { "Refining Slot", PropertyValueType.Integer },
            { "Remote Operations", PropertyValueType.Integer },
            { "Research Slot", PropertyValueType.Integer },
            { "Small Weapon Mounts", PropertyValueType.Integer },
            { "Specialist Detail", PropertyValueType.Integer },
            { "Structural Integrity", PropertyValueType.Integer },
            { "Unassigned Specialist Detail", PropertyValueType.Integer },
            { "Unassigned White Collar Detail", PropertyValueType.Integer },
            { "Warehouse Capacity", PropertyValueType.Integer },

            // Ship hull integer properties
            { "Eng Capacity Available", PropertyValueType.Integer },
            { "License Level", PropertyValueType.Integer },
            { "Max Hull Plating", PropertyValueType.Integer },
            { "Max Hull Reinforcement", PropertyValueType.Integer },
            { "Max Hull Sealant Units", PropertyValueType.Integer },
            { "Reactor Slots", PropertyValueType.Integer },
            { "Main Drive Slots", PropertyValueType.Integer },
            { "Thruster Slots", PropertyValueType.Integer },
            { "Jump Drive Slots", PropertyValueType.Integer },
            { "Nav Comp Slots", PropertyValueType.Integer },
            { "Scanner Slots", PropertyValueType.Integer },
            { "Shield Slots", PropertyValueType.Integer },
            { "Cargo Pod Slots", PropertyValueType.Integer },
            { "Fuel Tank Slots", PropertyValueType.Integer },
            { "Coupler Slots", PropertyValueType.Integer },
            { "GERTY Slots", PropertyValueType.Integer },

            // Boolean properties (rendered as CheckBox in grid)
            { "Can Manufacture", PropertyValueType.CheckBox },
            { "Can Research", PropertyValueType.CheckBox },
            { "Consumable", PropertyValueType.CheckBox },

            // ComboBox properties (rendered as ComboBox in grid)
            { "Commodity Industry", PropertyValueType.ComboBox },

            // Time properties
            { "Manufacture Run Time", PropertyValueType.Time },

            // Decimal properties
            { "Cooldown Time", PropertyValueType.Decimal },
            { "Energy Defence", PropertyValueType.Decimal },
            { "Fuel Used", PropertyValueType.Decimal },
            { "HP Percent Restored", PropertyValueType.Decimal },
            { "Hull HP Percent Restored", PropertyValueType.Decimal },
            { "Hull Manufacture Time Modification", PropertyValueType.Decimal },
            { "Kinetic Damage Defence", PropertyValueType.Decimal },
            { "Manufacture Time Reduction", PropertyValueType.Decimal },
            { "Maximum Damage Repair", PropertyValueType.Decimal },
            { "Max Jump Distance", PropertyValueType.Decimal },
            { "Mining Yield Modification", PropertyValueType.Decimal },
            { "Missile Damage Defence", PropertyValueType.Decimal },
            { "Power Regeneration Rate", PropertyValueType.Decimal },
            { "Purity Modifier", PropertyValueType.Decimal },
            { "Refining Time Modification", PropertyValueType.Decimal },
            { "Research Time Modification", PropertyValueType.Decimal },
            { "Scan Level", PropertyValueType.Decimal },
            { "Sensor Abundance Factor", PropertyValueType.Decimal },
            { "Wear and Tear Rate", PropertyValueType.Decimal },
        };

        public static readonly string INTEGER_PATTERN = @"^[+-]?\d+$";
        public static readonly string DECIMAL_PATTERN = @"^[+-]?\d+(\.\d+)?$";
        public static readonly string BOOLEAN_PATTERN = @"^(true|false|True|False)$";
        public static readonly string TIME_PATTERN = @"^(\d+d\s*)?(\d+h\s*)?(\d+m\s*)?(\d+s\s*)?$";

        public static PropertyValueType GetPropertyType(string propertyName)
        {
            PropertyValueType type;
            if (_propertyTypes.TryGetValue(propertyName, out type))
                return type;
            return PropertyValueType.Unknown;
        }

        public static string GetValidationPattern(string propertyName)
        {
            switch (GetPropertyType(propertyName))
            {
                case PropertyValueType.Integer: return INTEGER_PATTERN;
                case PropertyValueType.Decimal: return DECIMAL_PATTERN;
                case PropertyValueType.Boolean: return BOOLEAN_PATTERN;
                case PropertyValueType.Time: return TIME_PATTERN;
                default: return null; // Unknown — no validation
            }
        }

        /// <summary>
        /// Returns the data source (IList) for ComboBox properties, or null if not applicable.
        /// </summary>
        public static IList GetComboBoxDataSource(string propertyName)
        {
            switch (propertyName)
            {
                case "Commodity Industry":
                    return new List<string>(
                        Models.CommodityIndustry.Groups
                            .Select(ci => ci.Name));
                default:
                    return null;
            }
        }
    }
}
