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
            { "Accuracy", PropertyValueType.Integer },
            { "Amount Manufactured", PropertyValueType.Integer },
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
            { "Magazine Size", PropertyValueType.Integer },
            { "Manufacture Slot", PropertyValueType.Integer },
            { "Mass", PropertyValueType.Integer },
            { "Max Allowed On Ship", PropertyValueType.Integer },
            { "Max Mining Grapples", PropertyValueType.Integer },
            { "Max Mining Lasers", PropertyValueType.Integer },
            { "Max Ore Hoppers", PropertyValueType.Integer },
            { "Max Per Colony", PropertyValueType.Integer },
            { "Max Range", PropertyValueType.Integer },
            { "Medium Weapon Mounts", PropertyValueType.Integer },
            { "Min Range", PropertyValueType.Integer },
            { "Mining Cycle Time", PropertyValueType.Integer },
            { "Mining Slot", PropertyValueType.Integer },
            { "Power Generated", PropertyValueType.Integer },
            { "Power Provided", PropertyValueType.Integer },
            { "Power Required", PropertyValueType.Integer },
            { "Raw Material Capacity", PropertyValueType.Integer },
            { "Refining Rate", PropertyValueType.Integer },
            { "Refining Slot", PropertyValueType.Integer },
            { "Remote Operations", PropertyValueType.Integer },
            { "Research Slot", PropertyValueType.Integer },
            { "Shield Hitpoints", PropertyValueType.Integer },
            { "Small Weapon Mounts", PropertyValueType.Integer },
            { "Specialist Detail", PropertyValueType.Integer },
            { "Structural Integrity", PropertyValueType.Integer },
            { "Unassigned Specialist Detail", PropertyValueType.Integer },
            { "Unassigned White Collar Detail", PropertyValueType.Integer },
            { "Warehouse Capacity", PropertyValueType.Integer },
            { "Weapon Slot Size", PropertyValueType.Integer },

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
            { "Mining Capable", PropertyValueType.CheckBox },
            { "PDT Capable", PropertyValueType.CheckBox },

            // ComboBox properties (rendered as ComboBox in grid)
            { "Commodity Industry", PropertyValueType.ComboBox },

            // String properties (Unknown — no validation, free-form text)
            // "Ammo Type", "License Career", "Material Focus", "Material Focus Bonus"
            // These are intentionally left as Unknown (default) — no validation applied.

            // Time properties
            { "Manufacture Run Time", PropertyValueType.Time },

            // Decimal properties
            { "Acceleration Rate", PropertyValueType.Decimal },
            { "Cooldown Time", PropertyValueType.Decimal },
            { "Deploy Time", PropertyValueType.Decimal },
            { "Energy Damage Rating", PropertyValueType.Decimal },
            { "Energy Defence", PropertyValueType.Decimal },
            { "Energy Defence Rating", PropertyValueType.Decimal },
            { "Fuel Transfer Rate", PropertyValueType.Decimal },
            { "Fuel Used", PropertyValueType.Decimal },
            { "Fuel Used / JAS / Mass", PropertyValueType.Decimal },
            { "HP Percent Restored", PropertyValueType.Decimal },
            { "HP% restored for a part", PropertyValueType.Decimal },
            { "Hull HP Percent Restored", PropertyValueType.Decimal },
            { "Hull Manufacture Time Modification", PropertyValueType.Decimal },
            { "Increased Hull HP %", PropertyValueType.Decimal },
            { "Jump Charge Time", PropertyValueType.Decimal },
            { "Kinetic Damage Defence", PropertyValueType.Decimal },
            { "Kinetic Damage Rating", PropertyValueType.Decimal },
            { "Kinetic Defence Rating", PropertyValueType.Decimal },
            { "Launch Velocity", PropertyValueType.Decimal },
            { "Manufacture Time Reduction", PropertyValueType.Decimal },
            { "Max Effective Range", PropertyValueType.Decimal },
            { "Max Jump Distance", PropertyValueType.Decimal },
            { "Maximum Damage Repair", PropertyValueType.Decimal },
            { "Mining Yield", PropertyValueType.Decimal },
            { "Mining Yield Increase", PropertyValueType.Decimal },
            { "Mining Yield Modification", PropertyValueType.Decimal },
            { "Missile Damage Defence", PropertyValueType.Decimal },
            { "Missile Damage Rating", PropertyValueType.Decimal },
            { "Missile Defence Rating", PropertyValueType.Decimal },
            { "Power Draw Per Second", PropertyValueType.Decimal },
            { "Power Draw Per Shot", PropertyValueType.Decimal },
            { "Power Regeneration Rate", PropertyValueType.Decimal },
            { "Purity Modifier", PropertyValueType.Decimal },
            { "Rate of Fire", PropertyValueType.Decimal },
            { "Refining Time Modification", PropertyValueType.Decimal },
            { "Reload Time", PropertyValueType.Decimal },
            { "Research Time Modification", PropertyValueType.Decimal },
            { "Rotational Thrust", PropertyValueType.Decimal },
            { "Scan Level", PropertyValueType.Decimal },
            { "Sensor Abundance Factor", PropertyValueType.Decimal },
            { "Shield regen", PropertyValueType.Decimal },
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
