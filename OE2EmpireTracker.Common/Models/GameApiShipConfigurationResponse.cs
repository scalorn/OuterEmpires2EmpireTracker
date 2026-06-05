// <copyright file="GameApiShipConfigurationResponse.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OE2EmpireTracker.Client
{
    /// <summary>
    /// DTO representing the ship configuration response from the game API.
    /// </summary>
    public class GameApiShipConfigurationResponse
    {
        /// <summary>
        /// Gets or sets the ship identifier.
        /// </summary>
        [JsonProperty("shipId")]
        public int ShipId { get; set; }

        /// <summary>
        /// Gets or sets the ship summary information.
        /// </summary>
        [JsonProperty("summary")]
        public GameApiShipSummary Summary { get; set; }

        /// <summary>
        /// Gets or sets the list of fitted components.
        /// </summary>
        [JsonProperty("components")]
        public List<GameApiShipComponent> Components { get; set; } = new List<GameApiShipComponent>();
    }

    /// <summary>
    /// DTO representing the ship summary within the ship configuration response.
    /// </summary>
    public class GameApiShipSummary
    {
        /// <summary>
        /// Gets or sets the ship asset name.
        /// </summary>
        [JsonProperty("shipAssetName")]
        public string ShipAssetName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ship name.
        /// </summary>
        [JsonProperty("shipName")]
        public string ShipName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ship type identifier.
        /// </summary>
        [JsonProperty("shipTypeId")]
        public int ShipTypeId { get; set; }

        /// <summary>
        /// Gets or sets the ship type name.
        /// </summary>
        [JsonProperty("shipType")]
        public string ShipType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base model type.
        /// </summary>
        [JsonProperty("baseModelType")]
        public string BaseModelType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transponder code.
        /// </summary>
        [JsonProperty("transponder")]
        public string Transponder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ship type description.
        /// </summary>
        [JsonProperty("shipTypeDescription")]
        public string ShipTypeDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fuel capacity.
        /// </summary>
        [JsonProperty("fuelCap")]
        public int FuelCap { get; set; }

        /// <summary>
        /// Gets or sets the current fuel level.
        /// </summary>
        [JsonProperty("currentFuel")]
        public int CurrentFuel { get; set; }

        /// <summary>
        /// Gets or sets the engineering capacity.
        /// </summary>
        [JsonProperty("engCap")]
        public int EngCap { get; set; }

        /// <summary>
        /// Gets or sets the available engineering capacity.
        /// </summary>
        [JsonProperty("availableEngCap")]
        public int AvailableEngCap { get; set; }

        /// <summary>
        /// Gets or sets the current cargo amount.
        /// </summary>
        [JsonProperty("currentCargo")]
        public int CurrentCargo { get; set; }

        /// <summary>
        /// Gets or sets the cargo capacity.
        /// </summary>
        [JsonProperty("cargoCap")]
        public int CargoCap { get; set; }

        /// <summary>
        /// Gets or sets the current hopper amount.
        /// </summary>
        [JsonProperty("currentHopper")]
        public int CurrentHopper { get; set; }

        /// <summary>
        /// Gets or sets the hopper capacity.
        /// </summary>
        [JsonProperty("hopperCap")]
        public int HopperCap { get; set; }

        /// <summary>
        /// Gets or sets the power capacity.
        /// </summary>
        [JsonProperty("powerCap")]
        public int PowerCap { get; set; }

        /// <summary>
        /// Gets or sets the available power.
        /// </summary>
        [JsonProperty("availablePower")]
        public int AvailablePower { get; set; }

        /// <summary>
        /// Gets or sets the power regeneration per second.
        /// </summary>
        [JsonProperty("powerRegenPerSecond")]
        public double PowerRegenPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the jump range.
        /// </summary>
        [JsonProperty("jumpRange")]
        public double JumpRange { get; set; }

        /// <summary>
        /// Gets or sets the maximum speed.
        /// </summary>
        [JsonProperty("maxSpeed")]
        public double MaxSpeed { get; set; }

        /// <summary>
        /// Gets or sets the number of small weapon mounts.
        /// </summary>
        [JsonProperty("smallWeaponMounts")]
        public int SmallWeaponMounts { get; set; }

        /// <summary>
        /// Gets or sets the number of medium weapon mounts.
        /// </summary>
        [JsonProperty("mediumWeaponMounts")]
        public int MediumWeaponMounts { get; set; }

        /// <summary>
        /// Gets or sets the number of large weapon mounts.
        /// </summary>
        [JsonProperty("largeWeaponMounts")]
        public int LargeWeaponMounts { get; set; }

        /// <summary>
        /// Gets or sets the location identifier. Nullable when in transit.
        /// </summary>
        [JsonProperty("locationId")]
        public int? LocationId { get; set; }

        /// <summary>
        /// Gets or sets the location name.
        /// </summary>
        [JsonProperty("locationName")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system name.
        /// </summary>
        [JsonProperty("systemName")]
        public string SystemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the ship is in transition.
        /// </summary>
        [JsonProperty("transitionCheck")]
        public bool TransitionCheck { get; set; }

        /// <summary>
        /// Gets or sets the system identifier.
        /// </summary>
        [JsonProperty("systemId")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the transition text.
        /// </summary>
        [JsonProperty("transitionText")]
        public string TransitionText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ship size.
        /// </summary>
        [JsonProperty("shipSize")]
        public int ShipSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ship is damaged.
        /// </summary>
        [JsonProperty("isDamaged")]
        public bool IsDamaged { get; set; }

        /// <summary>
        /// Gets or sets the overall integrity percentage.
        /// </summary>
        [JsonProperty("overallIntegrity")]
        public double OverallIntegrity { get; set; }

        /// <summary>
        /// Gets or sets the total mass.
        /// </summary>
        [JsonProperty("totalMass")]
        public double TotalMass { get; set; }

        /// <summary>
        /// Gets or sets the cargo mass.
        /// </summary>
        [JsonProperty("cargoMass")]
        public double CargoMass { get; set; }

        /// <summary>
        /// Gets or sets the maximum acceleration.
        /// </summary>
        [JsonProperty("maxAcceleration")]
        public double MaxAcceleration { get; set; }

        /// <summary>
        /// Gets or sets the maximum turn rate.
        /// </summary>
        [JsonProperty("maxTurnRate")]
        public double MaxTurnRate { get; set; }

        /// <summary>
        /// Gets or sets the jump fuel consumption per second.
        /// </summary>
        [JsonProperty("jumpFuelConsumptionPerSecond")]
        public double JumpFuelConsumptionPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the maximum jump range.
        /// </summary>
        [JsonProperty("maxJumpRange")]
        public double MaxJumpRange { get; set; }

        /// <summary>
        /// Gets or sets the X coordinate.
        /// </summary>
        [JsonProperty("xCoord")]
        public double XCoord { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate.
        /// </summary>
        [JsonProperty("yCoord")]
        public double YCoord { get; set; }

        /// <summary>
        /// Gets or sets the angle.
        /// </summary>
        [JsonProperty("angle")]
        public double Angle { get; set; }

        /// <summary>
        /// Gets or sets the ship identifier.
        /// </summary>
        [JsonProperty("shipId")]
        public int ShipId { get; set; }
    }

    /// <summary>
    /// DTO representing a fitted ship component within the ship configuration.
    /// </summary>
    public class GameApiShipComponent
    {
        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component evolution level.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the blueprint type of the component.
        /// </summary>
        [JsonProperty("blueprintType")]
        public string BlueprintType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier. Nullable.
        /// </summary>
        [JsonProperty("Id")]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the health percentage. Nullable.
        /// </summary>
        [JsonProperty("healthPercentage")]
        public double? HealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the last repair health percentage.
        /// </summary>
        [JsonProperty("lastRepairHealthPercentage")]
        public double LastRepairHealthPercentage { get; set; }

        /// <summary>
        /// Gets or sets the list of component properties.
        /// </summary>
        [JsonProperty("properties")]
        public List<GameApiShipComponentProperty> Properties { get; set; } = new List<GameApiShipComponentProperty>();

        /// <summary>
        /// Gets or sets the munition details. Null if the component has no munitions loaded.
        /// </summary>
        [JsonProperty("munitionDetails")]
        public GameApiShipComponentMunition MunitionDetails { get; set; }
    }

    /// <summary>
    /// DTO representing a property of a ship component.
    /// </summary>
    public class GameApiShipComponentProperty
    {
        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [JsonProperty("propertyName")]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly display name of the property.
        /// </summary>
        [JsonProperty("friendlyPropertyName")]
        public string FriendlyPropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [JsonProperty("propertyValue")]
        public double PropertyValue { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO representing munition details loaded in a ship component.
    /// </summary>
    public class GameApiShipComponentMunition
    {
        /// <summary>
        /// Gets or sets the munition name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the munition evolution level.
        /// </summary>
        [JsonProperty("evolution")]
        public int Evolution { get; set; }

        /// <summary>
        /// Gets or sets the amount of munition contained.
        /// </summary>
        [JsonProperty("containingAmount")]
        public int ContainingAmount { get; set; }

        /// <summary>
        /// Gets or sets the munition containing type identifier. Nullable.
        /// </summary>
        [JsonProperty("containingType")]
        public int? ContainingType { get; set; }

        /// <summary>
        /// Gets or sets the munition icon identifier.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of munition properties.
        /// </summary>
        [JsonProperty("munitionProperties")]
        public List<GameApiShipComponentProperty> MunitionProperties { get; set; } = new List<GameApiShipComponentProperty>();
    }
}
