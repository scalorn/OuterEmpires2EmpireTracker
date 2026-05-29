# Requirements Document

## Introduction

Expand the colony warehouse overflow rule system to support multiple rule types beyond the current single-resource threshold. Introduce a `RuleType` enum that categorizes rules (starting with `SpecificResource` for existing behavior and `TotalWarehouse` for aggregate volume checks). All thresholds become volume-based rather than quantity-based, aligning with the existing `CargoVolumeService` volume model where resources are 1 volume/unit, commodities are 10, workers are 50, and manufactured items use their blueprint's Cargo Volume Size.

## Glossary

- **Overflow_Rule**: A `WarehouseOverflowRule` entity that defines when and how excess items should be moved out of a colony warehouse.
- **Rule_Type**: An enum (`OverflowRuleType`) categorizing the kind of threshold check an Overflow_Rule performs.
- **Specific_Resource_Rule**: A Rule_Type that triggers when the total volume of a specific resource+purity combination exceeds the threshold.
- **Total_Warehouse_Rule**: A Rule_Type that triggers when the sum of all item volumes in the colony warehouse exceeds the threshold.
- **Volume_Threshold**: A decimal value representing the maximum allowed volume before the rule triggers.
- **Background_Processor**: The `BackgroundProcessor` service that periodically evaluates Overflow_Rules.
- **Cargo_Volume_Service**: The `CargoVolumeService` static class that computes per-unit volumes for all item types.
- **Colony_Warehouse**: The `ItemBag` (colony.Items) containing all items stored at a colony.
- **Overflow_Delivery**: A delivery generated when an Overflow_Rule triggers, moving excess items to the configured destination.
- **Colony_Form**: The WinForms colony management form with an Overflow tab displaying rules in a DataGridView.

## Requirements

### Requirement 1: RuleType Enum

**User Story:** As a player, I want overflow rules to support different trigger categories, so that I can control overflow based on individual resources or total warehouse capacity.

#### Acceptance Criteria

1. THE Overflow_Rule model SHALL include a `RuleType` property of type `OverflowRuleType`.
2. THE `OverflowRuleType` enum SHALL define the values `SpecificResource` and `TotalWarehouse`.
3. WHEN an Overflow_Rule is deserialized without a `RuleType` value, THE system SHALL default to `SpecificResource`.
4. THE `OverflowRuleType` enum SHALL be serialized as a string using `JsonStringEnumConverter`.


### Requirement 2: Volume-Based Thresholds

**User Story:** As a player, I want overflow thresholds expressed in volume units, so that rules work correctly for commodities, flatpacks, and ship parts that have different volumes per unit.

#### Acceptance Criteria

1. THE Overflow_Rule model SHALL use a `decimal` type for the `TriggerThreshold` property (replacing the current `int`).
2. THE Background_Processor SHALL compare the computed volume (not raw quantity) against the Volume_Threshold when evaluating a Specific_Resource_Rule.
3. WHEN evaluating a Specific_Resource_Rule, THE Background_Processor SHALL compute volume as the sum of (quantity × per-unit volume) for all matching items, using Cargo_Volume_Service volume constants.
4. WHEN a Specific_Resource_Rule targets a resource (volume = 1 per unit), THE system SHALL produce numerically identical results to the previous quantity-based behavior.

### Requirement 3: SpecificResource Rule Evaluation

**User Story:** As a player, I want the existing per-resource overflow behavior preserved under the new model, so that my current rules continue to work without reconfiguration.

#### Acceptance Criteria

1. WHEN evaluating a Specific_Resource_Rule, THE Background_Processor SHALL filter colony warehouse items by `ResourceName` and `ResourcePurity`.
2. WHEN the computed volume of matching items exceeds the Volume_Threshold, THE Background_Processor SHALL calculate excess volume as (computed volume − threshold).
3. WHEN excess volume is detected, THE Background_Processor SHALL log the colony name, resource name, purity, current volume, threshold, and excess volume.
4. THE Specific_Resource_Rule SHALL require both `ResourceName` and `ResourcePurity` to be non-empty.


### Requirement 4: TotalWarehouse Rule Evaluation

**User Story:** As a player, I want to set a total warehouse volume cap so that overflow triggers when my colony's combined storage exceeds a threshold, regardless of which items are stored.

#### Acceptance Criteria

1. WHEN evaluating a Total_Warehouse_Rule, THE Background_Processor SHALL compute the total volume of all items in the Colony_Warehouse by summing (quantity × per-unit volume) for every item.
2. WHEN the total warehouse volume exceeds the Volume_Threshold, THE Background_Processor SHALL calculate excess volume as (total volume − threshold).
3. WHEN excess volume is detected for a Total_Warehouse_Rule, THE Background_Processor SHALL log the colony name, total volume, threshold, and excess volume.
4. THE Total_Warehouse_Rule SHALL NOT require `ResourceName` or `ResourcePurity` (those fields are ignored for this rule type).
5. WHEN computing per-unit volume for warehouse items, THE Background_Processor SHALL use the same volume constants as Cargo_Volume_Service: resources = 1, commodities = 10, workers = 50, blueprints/surveys = 0, manufactured items = blueprint Cargo Volume Size.

### Requirement 5: Model Backward Compatibility

**User Story:** As a player with existing saved rules, I want my data to load correctly after the upgrade without manual migration.

#### Acceptance Criteria

1. WHEN deserializing existing Overflow_Rule JSON that lacks a `RuleType` field, THE system SHALL treat the rule as `SpecificResource`.
2. WHEN deserializing existing Overflow_Rule JSON with an integer `TriggerThreshold`, THE system SHALL accept it as a decimal value.
3. THE serialized JSON format SHALL remain backward-compatible: existing fields (`ResourceName`, `ResourcePurity`, `TriggerThreshold`, `DestinationType`, `DestinationUUID`, `DeliveryRouteUUID`) SHALL retain their current JSON property names exactly as-is, regardless of any serialization configuration changes.


### Requirement 6: Colony Form Overflow Tab Updates

**User Story:** As a player, I want the Overflow tab to display the rule type and allow creating TotalWarehouse rules, so that I can manage both rule categories from the UI.

#### Acceptance Criteria

1. THE Colony_Form Overflow tab DataGridView SHALL display a "Rule Type" column showing the `OverflowRuleType` value for each rule.
2. WHEN a Total_Warehouse_Rule is displayed, THE DataGridView SHALL show the Resource and Purity columns as empty or "N/A".
3. THE Colony_Form SHALL provide a mechanism to create a new Total_Warehouse_Rule (selecting rule type, entering volume threshold, choosing destination and route).
4. THE Colony_Form SHALL provide a mechanism to create a new Specific_Resource_Rule (current behavior: selecting resource, purity, threshold, destination, route).
5. WHEN the user selects `TotalWarehouse` as the rule type during creation, THE Colony_Form SHALL hide or disable the resource name and purity inputs.

### Requirement 7: Validation Rules

**User Story:** As a player, I want the system to reject invalid overflow rules, so that misconfigured rules do not cause unexpected behavior.

#### Acceptance Criteria

1. IF a Specific_Resource_Rule has an empty `ResourceName`, THEN THE system SHALL reject the rule with a validation error.
2. IF a Specific_Resource_Rule has an empty `ResourcePurity`, THEN THE system SHALL reject the rule with a validation error.
3. IF an Overflow_Rule has a `TriggerThreshold` less than or equal to zero, THEN THE system SHALL reject the rule with a validation error.
4. IF an Overflow_Rule has an empty `DestinationUUID`, THEN THE system SHALL reject the rule with a validation error.
5. IF an Overflow_Rule has an empty `DeliveryRouteUUID`, THEN THE system SHALL reject the rule with a validation error.
6. THE Total_Warehouse_Rule SHALL NOT be rejected due to validation of `ResourceName` or `ResourcePurity` fields (those fields are not applicable); validation logic MAY execute but SHALL NOT cause rejection for this rule type.

### Requirement 8: Future Extensibility

**User Story:** As a developer, I want the RuleType enum designed for future expansion, so that adding new rule categories (TotalCommodities, TotalShipComponents) requires minimal changes.

#### Acceptance Criteria

1. THE Background_Processor evaluation logic SHALL use a switch/dispatch pattern on `RuleType`, so that adding a new rule type requires adding a new case without modifying existing evaluation paths.
2. IF an Overflow_Rule has an unrecognized `RuleType` value, THEN THE Background_Processor SHALL log a warning and skip the rule without throwing an exception.
3. THE `OverflowRuleType` enum SHALL be defined in the Models namespace alongside other model enums, following existing project conventions.
