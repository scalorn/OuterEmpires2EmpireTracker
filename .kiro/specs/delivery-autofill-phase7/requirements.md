# Requirements Document

## Introduction

Delivery Auto-Fill Phase 7 extends the existing auto-fill system (which currently handles Commodities only) to support three additional request types: Flatpacks, Manufacturing Resources, and Workers. It also introduces flatpack staging on delivery execution and a new "Stage Resources" flag for manufacturing structures. All auto-fill operations are additive and produce drop-off items only, matching the existing commodity auto-fill pattern.

## Glossary

- **Auto_Fill_System**: The subsystem on the delivery planning tab that scans route stops and generates drop-off DeliveryItems based on colony needs. Currently supports Commodities; this feature adds Flatpacks, Manufacturing Resources, and Workers.
- **FormAutoFill**: The dialog form (`FormAutoFill`) that presents checkboxes for each auto-fill request type. The user selects which types to include before the system generates items.
- **DeliveryPlanViewModel**: The ViewModel class that wraps a DeliveryPlan and exposes auto-fill methods (e.g., `AutoFillCommodities`). New methods are added here for each request type.
- **ColonyStructure**: A structure placed on a colony, identified by a FlatpackBlueprintUUID. Has PropertyBag-based state flags (Built, Staged, Online) and manufacturing configuration fields.
- **ColonyStructureViewModel**: The ViewModel wrapping ColonyStructure, exposing typed boolean properties for PropertyBag flags (IsBuilt, IsStaged, IsOnline).
- **ColonyStatusCalculator**: The class that computes actual and ideal colony status by iterating structures and accumulating worker counts, power, habitation, food, entertainment, and warehouse metrics.
- **StagingResources**: A new boolean flag on ColonyStructure indicating that a manufacturing run is planned and waiting for resource delivery. Only applicable to Manufactory and CommodityFactory structure types.
- **Warehouse**: The colony's ItemBag (`Colony.Items`) containing all stored items including Resources, Commodities, Flatpacks, WorkDetails, etc.
- **Shortfall**: The difference between total resources needed and what is currently available in the colony warehouse. Shortfall = max(0, needed - warehouse quantity).
- **Blueprint_Resources**: The `Dictionary<string, string>` on a Blueprint object mapping resource names to per-unit quantities required for manufacturing.
- **Commodity_ConstructionResources**: The `Dictionary<string, string>` on a Commodity object mapping resource names to per-cycle quantities consumed during commodity production.
- **Worker_Gap**: The difference between ideal worker count (all worker slots filled) and actual worker count for a given worker type (BlueCollar, WhiteCollar, Specialist) on a colony.

## Requirements

### Requirement 1: Enable Flatpack Auto-Fill Checkbox

**User Story:** As a player, I want the Flatpacks checkbox on FormAutoFill to be enabled, so that I can auto-fill flatpack deliveries for unbuilt structures.

#### Acceptance Criteria

1. THE FormAutoFill SHALL display the Flatpacks checkbox as enabled with the text "Flatpacks" (removing the "(Future)" suffix).
2. THE FormAutoFill SHALL expose an `IncludeFlatpacks` boolean property that returns the checked state of the Flatpacks checkbox.

### Requirement 2: Flatpack Auto-Fill Logic

**User Story:** As a player, I want the auto-fill system to scan each stop's colony for unbuilt and unstaged structures, so that flatpack delivery items are generated automatically.

#### Acceptance Criteria

1. WHEN the Flatpacks option is selected, THE DeliveryPlanViewModel SHALL scan each route stop's colony for structures where IsBuilt equals false AND IsStaged equals false.
2. WHEN an unbuilt and unstaged structure is found, THE DeliveryPlanViewModel SHALL add a drop-off DeliveryItem with ItemType set to Flatpack, BaseItemTypeID set to the structure's FlatpackBlueprintUUID, Name set to the blueprint's ExtendedName, and Quantity set to 1.
3. THE DeliveryPlanViewModel SHALL expose an `AutoFillFlatpacks` method accepting route stops and a colony finder delegate, returning the count of items added.
4. THE Auto_Fill_System SHALL add flatpack items additively to existing plan items without removing prior entries.

### Requirement 3: Flatpack Staging on Delivery Execution

**User Story:** As a player, I want checking a flatpack delivery item on the execution form to mark the matching colony structure as staged, so that the structure state updates immediately on delivery.

#### Acceptance Criteria

1. WHEN a Flatpack delivery item is checked as delivered on the execution form, THE FormDeliveryExecution SHALL find the matching ColonyStructure on the target colony where FlatpackBlueprintUUID equals the delivery item's BaseItemTypeID.
2. WHEN the matching ColonyStructure is found and the item is checked, THE FormDeliveryExecution SHALL set the structure's Staged property to true via `Properties["Staged"] = "True"`.
3. WHEN a Flatpack delivery item is unchecked on the execution form, THE FormDeliveryExecution SHALL set the matching structure's Staged property to false via `Properties["Staged"] = "False"`.
4. WHEN a flatpack staging change occurs, THE FormDeliveryExecution SHALL fire the ColonyDataChanged event for the affected colony.
5. IF no matching ColonyStructure is found for a Flatpack delivery item, THEN THE FormDeliveryExecution SHALL log a warning and continue without error.

### Requirement 4: Stage Resources Flag on ColonyStructure

**User Story:** As a player, I want a StagingResources boolean flag on ColonyStructure, so that I can mark manufacturing runs as planned and waiting for resource delivery.

#### Acceptance Criteria

1. THE ColonyStructure SHALL have a `StagingResources` boolean property, defaulting to false, serialized to JSON.
2. THE ColonyStructureViewModel SHALL expose a typed `StagingResources` property that reads and writes the ColonyStructure's StagingResources field.
3. WHEN StagingResources is set to true, THE ColonyStructure SHALL have a non-empty ManufacturingBlueprintUUID (for Manufactory) or non-empty ManufacturingCommodityName (for CommodityFactory), and ManufacturingQuantity greater than zero.

### Requirement 5: Stage Resources Checkbox on Colony Structure UI

**User Story:** As a player, I want a "Stage Resources" checkbox on the ColonyStructure UI for Manufactory and CommodityFactory structures, so that I can indicate a manufacturing run is planned and awaiting resources.

#### Acceptance Criteria

1. WHILE a ColonyStructure has a blueprint type of Manufactory or CommodityFactory, THE ColonyStructure UI SHALL display a "Stage Resources" checkbox.
2. THE "Stage Resources" checkbox SHALL appear before the Start button in the manufacturing controls area.
3. WHEN the "Stage Resources" checkbox is checked, THE ColonyStructure UI SHALL set the StagingResources property to true on the underlying ColonyStructure.
4. WHEN the "Stage Resources" checkbox is unchecked, THE ColonyStructure UI SHALL set the StagingResources property to false on the underlying ColonyStructure.
5. WHILE a manufacturing process is actively running (ProcessCompletionTime is not null), THE "Stage Resources" checkbox SHALL be hidden.
6. THE "Stage Resources" checkbox SHALL only be enabled when a blueprint or commodity is selected and ManufacturingQuantity is greater than zero.

### Requirement 6: Enable Manufacturing Resources Auto-Fill Checkbox

**User Story:** As a player, I want the "Resources for Manufacturing" checkbox on FormAutoFill to be enabled, so that I can auto-fill resource deliveries for planned manufacturing runs.

#### Acceptance Criteria

1. THE FormAutoFill SHALL display the Resources checkbox as enabled with the text "Resources for Manufacturing" (removing the "(Future)" suffix).
2. THE FormAutoFill SHALL expose an `IncludeResources` boolean property that returns the checked state of the Resources checkbox.

### Requirement 7: Manufacturing Resources Auto-Fill Logic for Manufactories

**User Story:** As a player, I want the auto-fill system to calculate resource shortfalls for Manufactory structures with StagingResources enabled, so that the correct resource quantities are added to the delivery plan.

#### Acceptance Criteria

1. WHEN the Resources option is selected, THE DeliveryPlanViewModel SHALL scan each route stop's colony for structures where StagingResources equals true and the blueprint type is Manufactory.
2. WHEN a staging Manufactory is found, THE DeliveryPlanViewModel SHALL calculate total resources needed by multiplying each Blueprint_Resources entry quantity by ManufacturingQuantity.
3. WHEN total resources are calculated, THE DeliveryPlanViewModel SHALL subtract the quantity of each matching Refined resource currently in the colony Warehouse.
4. WHEN the Shortfall for a resource is greater than zero, THE DeliveryPlanViewModel SHALL add a drop-off DeliveryItem with ItemType set to Resource, BaseItemTypeID set to the resource name, Name set to the resource name, ResourcePurity set to "Refined", and Quantity set to the Shortfall.
5. THE DeliveryPlanViewModel SHALL expose an `AutoFillManufacturingResources` method accepting route stops, a colony finder delegate, and a blueprint finder delegate, returning the count of items added.

### Requirement 8: Manufacturing Resources Auto-Fill Logic for CommodityFactories

**User Story:** As a player, I want the auto-fill system to calculate resource shortfalls for CommodityFactory structures with StagingResources enabled, so that commodity production resource needs are delivered.

#### Acceptance Criteria

1. WHEN the Resources option is selected, THE DeliveryPlanViewModel SHALL scan each route stop's colony for structures where StagingResources equals true and the blueprint type is CommodityFactory.
2. WHEN a staging CommodityFactory is found, THE DeliveryPlanViewModel SHALL calculate total resources needed by multiplying each Commodity_ConstructionResources entry quantity by ManufacturingQuantity (number of cycles).
3. WHEN total resources are calculated, THE DeliveryPlanViewModel SHALL subtract the quantity of each matching Refined resource currently in the colony Warehouse.
4. WHEN the Shortfall for a resource is greater than zero, THE DeliveryPlanViewModel SHALL add a drop-off DeliveryItem with ItemType set to Resource, BaseItemTypeID set to the resource name, Name set to the resource name, ResourcePurity set to "Refined", and Quantity set to the Shortfall.

### Requirement 9: Enable Workers Auto-Fill Checkbox

**User Story:** As a player, I want the Workers checkbox on FormAutoFill to be enabled, so that I can auto-fill worker deliveries based on colony staffing gaps.

#### Acceptance Criteria

1. THE FormAutoFill SHALL display the Workers checkbox as enabled with the text "Workers" (removing the "(Future)" suffix).
2. THE FormAutoFill SHALL expose an `IncludeWorkers` boolean property that returns the checked state of the Workers checkbox.

### Requirement 10: Workers Auto-Fill Logic

**User Story:** As a player, I want the auto-fill system to calculate worker gaps between ideal and actual counts, so that the correct worker quantities are added to the delivery plan.

#### Acceptance Criteria

1. WHEN the Workers option is selected, THE DeliveryPlanViewModel SHALL scan each route stop's colony using ColonyStatusCalculator to compute ideal and actual worker statuses.
2. WHEN the ideal HabitationRequired exceeds the actual HabitationRequired for a colony, THE DeliveryPlanViewModel SHALL determine the per-worker-type gap by comparing ideal and actual worker assignments across all structures.
3. WHEN a Worker_Gap for a worker type (BlueCollar, WhiteCollar, or Specialist) is greater than zero, THE DeliveryPlanViewModel SHALL add a drop-off DeliveryItem with ItemType set to WorkDetail, BaseItemTypeID set to the worker detail ID (e.g., "BlueCollarDetail"), Name set to the worker detail name (e.g., "Blue Collar Detail"), and Quantity set to the Worker_Gap.
4. THE DeliveryPlanViewModel SHALL expose an `AutoFillWorkers` method accepting route stops, a colony finder delegate, and a player context reference, returning the count of items added.

### Requirement 11: Auto-Fill Orchestration

**User Story:** As a player, I want the delivery planning tab to invoke the selected auto-fill methods when I confirm the FormAutoFill dialog, so that all selected request types are processed in one action.

#### Acceptance Criteria

1. WHEN the user confirms the FormAutoFill dialog with OK, THE delivery planning tab SHALL invoke each selected auto-fill method (AutoFillCommodities, AutoFillFlatpacks, AutoFillManufacturingResources, AutoFillWorkers) in sequence.
2. THE delivery planning tab SHALL refresh the plan display after all auto-fill methods complete.
3. THE Auto_Fill_System SHALL preserve all existing plan items when adding new auto-fill items.
