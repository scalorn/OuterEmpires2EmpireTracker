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
            // Integer properties
            { "BlueCollarDetail", PropertyValueType.Integer },
            { "CargoCapacity", PropertyValueType.Integer },
            { "CargoVolumeSize", PropertyValueType.Integer },
            { "CrewSupported", PropertyValueType.Integer },
            { "EngCapacityRequired", PropertyValueType.Integer },
            { "EntertainmentProvided", PropertyValueType.Integer },
            { "ExtractionEfficiency", PropertyValueType.Integer },
            { "FoodProvision", PropertyValueType.Integer },
            { "FuelCapacity", PropertyValueType.Integer },
            { "HabitationProvision", PropertyValueType.Integer },
            { "Health", PropertyValueType.Integer },
            { "LargeWeaponMounts", PropertyValueType.Integer },
            { "ManufactureSlot", PropertyValueType.Integer },
            { "Mass", PropertyValueType.Integer },
            { "MaxAllowedOnShip", PropertyValueType.Integer },
            { "MaxPerColony", PropertyValueType.Integer },
            { "MediumWeaponMounts", PropertyValueType.Integer },
            { "MiningSlot", PropertyValueType.Integer },
            { "PowerGenerated", PropertyValueType.Integer },
            { "PowerProvided", PropertyValueType.Integer },
            { "PowerRequired", PropertyValueType.Integer },
            { "RefiningRate", PropertyValueType.Integer },
            { "RefiningSlot", PropertyValueType.Integer },
            { "RemoteOperations", PropertyValueType.Integer },
            { "ResearchSlot", PropertyValueType.Integer },
            { "SmallWeaponMounts", PropertyValueType.Integer },
            { "SpecialistDetail", PropertyValueType.Integer },
            { "StructuralIntegrity", PropertyValueType.Integer },
            { "UnassignedSpecialistDetail", PropertyValueType.Integer },
            { "UnassignedWhiteCollarDetail", PropertyValueType.Integer },
            { "WarehouseCapacity", PropertyValueType.Integer },

            // Boolean properties (rendered as CheckBox in grid)
            { "CanManufacture", PropertyValueType.CheckBox },
            { "CanResearch", PropertyValueType.CheckBox },
            { "Consumable", PropertyValueType.CheckBox },

            // ComboBox properties (rendered as ComboBox in grid)
            { "CommodityIndustry", PropertyValueType.ComboBox },

            // Time properties
            { "ManufactureTime", PropertyValueType.Time },

            // Decimal properties
            { "CooldownTime", PropertyValueType.Decimal },
            { "EnergyDefence", PropertyValueType.Decimal },
            { "FuelUsed", PropertyValueType.Decimal },
            { "HPPercentRestored", PropertyValueType.Decimal },
            { "HullHPPercentRestored", PropertyValueType.Decimal },
            { "HullManufactureTimeModification", PropertyValueType.Decimal },
            { "KineticDamageDefence", PropertyValueType.Decimal },
            { "ManufactureTimeReduction", PropertyValueType.Decimal },
            { "MaximumDamageRepairRate", PropertyValueType.Decimal },
            { "MaxJumpDistance", PropertyValueType.Decimal },
            { "MiningYieldModification", PropertyValueType.Decimal },
            { "MissileDamageDefence", PropertyValueType.Decimal },
            { "PowerRegenerationRate", PropertyValueType.Decimal },
            { "PurityModifier", PropertyValueType.Decimal },
            { "RefiningTimeModification", PropertyValueType.Decimal },
            { "ResearchTimeModification", PropertyValueType.Decimal },
            { "ScanLevel", PropertyValueType.Decimal },
            { "SensorAbundanceFactor", PropertyValueType.Decimal },
            { "WearAndTearRate", PropertyValueType.Decimal },
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
                case "CommodityIndustry":
                    return new List<string>(
                        Models.CommodityIndustry.Groups
                            .Select(ci => ci.Name));
                default:
                    return null;
            }
        }
    }
}
