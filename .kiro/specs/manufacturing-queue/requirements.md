# Requirements Document — BL-011 + BL-032: Build Planner

## Introduction

The Build Planner is a central work order system for planning and tracking manufacturing across the player's empire. A build plan contains line items — blueprints to manufacture or commodities to produce — each assigned to a specific colony and structure. The planner integrates with the existing staging and delivery systems: when a structure needs resources that aren't in the colony warehouse, the planner can auto-generate a delivery route and plan to supply them.

The build queue calculator (BL-032) is folded into this feature. When adding items to a plan, the user can enter a target duration (e.g. "2d 12h 0m 0s") and the planner computes how many items to queue to keep the structure busy for that long.

This iteration covers Manufactory blueprints and Commodity Factory commodities. Mining, refining, and research are deferred to a future iteration when an API makes the manual overhead worthwhile.

### Iteration Scope

**In scope:**
- Build plan data model (named, per-player)
- Build plan line items (blueprint or commodity, quantity, colony/structure allocation)
- Resource availability checking against colony warehouse
- Auto-generation of delivery route and delivery plan for missing resources
- Build queue calculator (quantity from target duration)
- Status tracking per line item (Staged, Delivering, Ready, In Progress, Completed)
- Build Planner form (MDI child)
- Integration with Activity form (show when manufacturing should complete)
- Integration with Inactivity form (show plans needing action)
- Persistence to PlayerData.json

**Out of scope (future work):**
- Auto-create orders from fill levels (BL-055)
- Automatic submission via game API (BL-047)
- Mining, refining, and research line items
- Priority ordering and dependency tracking between plans

## Glossary

- **Build_Plan**: A named, per-player container holding a set of Build_Items. Represents a batch of manufacturing work the player wants to accomplish.
- **Build_Item**: A single line item in a Build_Plan specifying what to produce (blueprint or commodity), how many, where (colony + structure), and the current status. Each Build_Item maps to one structure at one colony.
- **Item_Type**: The category of production for a Build_Item: Manufactory (blueprint-based item manufacturing) or Commodity (commodity factory production).
- **Recipient**: A free-text field on a Build_Item for the user to record who the items are for or what to do with them once complete. Not linked to player profiles.
- **Allocation**: The assignment of a Build_Item to a specific ColonyStructure at a specific Colony. Determines where the item will be produced.
- **Resource_Check**: The process of comparing a Build_Item's resource requirements against the target colony's warehouse inventory to identify shortfalls.
- **Shortfall**: Resources required by a Build_Item that are not available in the target colony's warehouse. Shortfalls drive delivery plan generation.
- **Queue_Calculator**: A function that computes how many items to queue to keep a structure busy for a target duration, based on the item's manufacturing time or commodity cycle time.
- **Target_Duration**: A time span entered in countdown format (e.g. "2d 12h 0m 0s") used by the Queue_Calculator.
- **Manufacturing_Time**: The time to manufacture one item, from the blueprint's "Manufacture Run Time" property.
- **Commodity_Cycle_Time**: The time for one commodity production cycle (GameConstants.CommodityCycleSeconds). Each cycle produces GameConstants.CommoditiesPerCycle items.
- **Item_Status**: The lifecycle state of a Build_Item: Staged (allocated, resources being arranged), Delivering (delivery plan created, resources in transit), Ready (resources available, waiting to start in-game), In_Progress (manufacturing started in-game), Completed (production finished).
- **PlayerData**: The JSON file (PlayerData.json) where player-specific data including build plans is persisted.
- **CountdownFormat**: A human-readable time string in the format "Xd Yh Zm Ws" parsed by CountdownFormatParser.

## Resolved Questions

> These questions were raised during requirements drafting and resolved with the user.

### OQ-1: Scope of Production Types — RESOLVED
**Decision:** This iteration covers Manufactory blueprints and Commodity Factory commodities only. Mining, refining, and research are deferred — too much manual overhead without an API.

### OQ-2: Recipient Field — RESOLVED
**Decision:** Free-text field. Not linked to player profiles. User records whatever they need to know about what to do with the items once complete.

### OQ-3: Resource Staging — RESOLVED
**Decision:** The planner checks blueprint/commodity resource requirements against the target colony's warehouse inventory and flags shortfalls. This uses the existing staging concept (StagingResources flag on ColonyStructure).

### OQ-4: Delivery Plan Generation — RESOLVED
**Decision:** Auto-generate a delivery route and delivery plan for missing resources. Leverages the existing AutoFillManufacturingResources pattern.

### OQ-5: BL-032 Integration — RESOLVED
**Decision:** The build queue calculator is part of the Build Planner form. When adding items, the user can enter a target duration and the planner computes the quantity.

### OQ-6: Status Tracking — RESOLVED
**Decision:** Manual transitions. The user marks items as started/completed. The Activity form shows when manufacturing should complete based on structure timers. The Inactivity form shows items needing action.

### OQ-7: Structure Eligibility — RESOLVED
**Decision:** All built-and-online structures of the correct type are shown when allocating a build item (not restricted to idle). Uses the standard filter text box pattern for searching. Multiple build items can be allocated to the same structure — the user manages the sequencing manually until an API is available. The structure list should indicate which structures are currently busy so the user can make informed choices.

### OQ-8: Delivery Route Reuse — RESOLVED
**Decision:** The user picks an existing delivery route (like the commodity auto-fill pattern) rather than always creating a new one. The delivery plan is specific to this build plan. Merging plans together is a future feature.

### OQ-9: Activity/Inactivity Display — RESOLVED
**Decision:** Build plan items piggyback on the existing Manufacturing activity type rather than introducing a new type. The display should indicate the item is part of a build plan (plan name + item name).

### OQ-10: Build Plan Deletion and Delivery Plans — RESOLVED
**Decision:** Deleting a build plan leaves its associated delivery plan and route in place. The user may have already partially executed the delivery.

### OQ-11: Commodity Quantity Unit — RESOLVED
**Decision:** Commodity build items specify quantity in runs (cycles), not individual items. Each run produces CommoditiesPerCycle items. The queue calculator computes runs from a target duration: ceiling(Target_Duration_seconds / Commodity_Cycle_Time_seconds). The total item output (runs × CommoditiesPerCycle) is displayed for reference but the quantity field stores runs.

## Requirements

### Requirement 1: Build Plan CRUD

**User Story:** As a player, I want to create, edit, and delete build plans, so that I can organize manufacturing work into logical batches.

#### Acceptance Criteria

1. THE Build_Plan SHALL have a unique identifier (UUID), a name, an OwnerUUID (current player profile), and an optional description.
2. WHEN the user creates a new Build_Plan, THE Application SHALL persist the plan to PlayerData with a generated UUID, associated with the current player profile.
3. WHEN the user edits a Build_Plan name or description, THE Application SHALL persist the changes to PlayerData.
4. WHEN the user deletes a Build_Plan, THE Application SHALL remove the plan and all associated Build_Items from PlayerData. Any associated DeliveryPlan and DeliveryRoute SHALL be left in place.
5. THE Application SHALL allow multiple Build_Plans to exist simultaneously per player profile.
6. IF a Build_Plan name is empty or whitespace, THEN THE Application SHALL reject the save and display a validation message.

### Requirement 2: Build Item Management

**User Story:** As a player, I want to add, edit, and remove items from a build plan, so that I can specify exactly what needs to be manufactured.

#### Acceptance Criteria

1. THE Build_Item SHALL have a unique identifier (UUID), an Item_Type (Manufactory or Commodity), a quantity, an optional Recipient (free-text), an optional notes field, and an Item_Status.
2. WHEN the Item_Type is Manufactory, THE Build_Item SHALL reference a Blueprint by UUID and store the blueprint's output item name.
3. WHEN the Item_Type is Commodity, THE Build_Item SHALL reference a Commodity by name.
4. WHEN the user adds a Build_Item, THE Application SHALL present a list of eligible blueprints (manufacturable types) or commodities based on the selected Item_Type.
5. WHEN the Item_Type is Manufactory, THE Application SHALL accept a quantity of one or more representing the number of items to manufacture.
6. WHEN the Item_Type is Commodity, THE Application SHALL accept a quantity of one or more representing the number of production runs. THE Application SHALL display the total item output (runs × CommoditiesPerCycle) for reference.
7. IF a Build_Item has a quantity less than one, THEN THE Application SHALL reject the save and display a validation message.
7. WHEN the user removes a Build_Item, THE Application SHALL remove it from the Build_Plan and persist the change.

### Requirement 3: Structure Allocation

**User Story:** As a player, I want to assign each build item to a specific structure at a specific colony, so that I know where each item will be produced.

#### Acceptance Criteria

1. WHEN the user allocates a Build_Item, THE Application SHALL present a list of the current player's colonies and eligible structures within each colony, with a filter text box for searching.
2. WHEN the Item_Type is Manufactory, THE Application SHALL filter eligible structures to built-and-online Manufactories.
3. WHEN the Item_Type is Commodity, THE Application SHALL filter eligible structures to built-and-online Commodity Factories.
4. THE Application SHALL indicate which structures are currently busy (have an active manufacturing timer or existing allocation) so the user can make informed choices.
5. THE Application SHALL provide a checkbox or toggle to filter the structure list to idle structures only.
4. WHEN the user selects a colony and structure, THE Application SHALL store the Colony UUID and ColonyStructure UUID on the Build_Item.
5. WHEN a Build_Item is allocated, THE Application SHALL set the StagingResources flag on the target ColonyStructure, set the ManufacturingBlueprintUUID or ManufacturingCommodityName, and set the ManufacturingQuantity.
6. THE Application SHALL allow a Build_Item to exist without an allocation.
7. WHEN the user removes an allocation from a Build_Item, THE Application SHALL clear the colony and structure references and reset the StagingResources flag on the previously allocated structure.
8. THE Application SHALL allow multiple Build_Items (from the same or different plans) to be allocated to the same structure. The user manages the sequencing of builds manually.

### Requirement 4: Resource Availability Check

**User Story:** As a player, I want to see which build items have sufficient resources at their target colony, so that I can identify what needs to be delivered before manufacturing can start.

#### Acceptance Criteria

1. WHEN a Build_Item is allocated to a colony and structure, THE Application SHALL compute the total resource requirements based on the blueprint's Resources dictionary (for Manufactory) or the commodity's ConstructionResources dictionary (for Commodity) multiplied by the quantity.
2. THE Application SHALL compare the required resources against the target colony's warehouse inventory (Refined purity for natural resources, appropriate purity for synthetics).
3. WHEN all required resources are available in the warehouse, THE Application SHALL indicate the Build_Item is ready to start.
4. WHEN one or more resources are short, THE Application SHALL display the shortfall quantities per resource.
5. THE Resource_Check SHALL update when the build plan is opened and when colony data changes.

### Requirement 5: Delivery Plan Auto-Generation

**User Story:** As a player, I want the planner to auto-generate a delivery route and plan for missing resources, so that I can efficiently supply my manufacturing colonies.

#### Acceptance Criteria

1. WHEN the user requests delivery generation for a Build_Plan, THE Application SHALL identify all allocated Build_Items with resource shortfalls.
2. THE Application SHALL present the user with a list of existing DeliveryRoutes to select from, following the same pattern as commodity auto-fill.
3. THE Application SHALL create a new DeliveryPlan on the selected route with drop-off items for each resource shortfall at each colony on the route.
4. IF the Build_Plan already has an associated DeliveryPlan, THE Application SHALL update the existing plan rather than creating a new one.
5. THE generated DeliveryPlan SHALL follow the existing data model patterns (UUID, Name, OwnerUUID, Stops with Sequence).
6. THE Application SHALL name the generated plan based on the Build_Plan name (e.g. "Build: {PlanName}").
7. AFTER generating the delivery plan, THE Application SHALL update the Item_Status of affected Build_Items to Delivering.
8. THE Build_Plan SHALL store a reference to its associated DeliveryPlan UUID (if one has been generated).

### Requirement 6: Build Queue Calculator

**User Story:** As a player, I want to calculate how many items to queue to keep a structure busy for a target duration, so that I can plan production efficiently without manual math.

#### Acceptance Criteria

1. WHEN the user enters a Target_Duration in countdown format and the Build_Item is a Manufactory blueprint, THE Queue_Calculator SHALL compute the quantity as ceiling(Target_Duration_seconds / Manufacturing_Time_seconds).
2. WHEN the user enters a Target_Duration in countdown format and the Build_Item is a Commodity, THE Queue_Calculator SHALL compute the number of runs as ceiling(Target_Duration_seconds / Commodity_Cycle_Time_seconds) and populate the quantity field with that number of runs.
3. IF the Target_Duration input is empty or cannot be parsed by CountdownFormatParser, THEN THE Queue_Calculator SHALL not compute a quantity.
4. IF the selected blueprint has no "Manufacture Run Time" property, THEN THE Queue_Calculator SHALL display a message indicating the manufacturing time is unknown.
5. WHEN the Queue_Calculator computes a quantity, THE Application SHALL populate the Build_Item's quantity field with the computed value.
6. THE Queue_Calculator SHALL be accessible from the Build_Item add/edit UI.

### Requirement 7: Item Status Tracking

**User Story:** As a player, I want to track the status of each build item through the manufacturing pipeline, so that I can see what needs attention.

#### Acceptance Criteria

1. WHEN a Build_Item is created without an allocation, THE Application SHALL set the Item_Status to Staged.
2. WHEN a Build_Item is allocated and a delivery plan is generated, THE Application SHALL set the Item_Status to Delivering.
3. WHEN all required resources are available at the target colony, THE Application SHALL allow the user to set the Item_Status to Ready.
4. WHEN the user starts manufacturing in-game and marks the item, THE Application SHALL set the Item_Status to In_Progress.
5. WHEN the user marks a Build_Item as completed, THE Application SHALL set the Item_Status to Completed.
6. THE Application SHALL display the Item_Status visually in the build plan so the user can distinguish between states.
7. THE Application SHALL allow the user to manually transition an item between any status.

### Requirement 8: Activity and Inactivity Integration

**User Story:** As a player, I want to see build plan items in the Activity and Inactivity forms, so that I can track manufacturing progress and identify items needing attention alongside my other colony operations.

#### Acceptance Criteria

1. WHEN a Build_Item has Item_Status of In_Progress and is allocated to a structure with an active manufacturing timer, THE Activity form SHALL display the item under the existing Manufacturing activity type with its expected completion time.
2. WHEN a Build_Item has Item_Status of Staged and has resource shortfalls, THE Inactivity form SHALL display the item indicating resources need to be delivered.
3. WHEN a Build_Item has Item_Status of Ready, THE Inactivity form SHALL display the item indicating manufacturing needs to be started in-game.
4. WHEN a Build_Item has Item_Status of Delivering, THE Inactivity form SHALL display the item indicating a delivery is in progress.
5. THE Activity and Inactivity forms SHALL identify build plan items with the plan name and item name so the user can distinguish them from non-plan manufacturing activity.

### Requirement 9: Build Plan Serialization

**User Story:** As a player, I want my build plans to persist across application sessions, so that I do not lose my manufacturing plans.

#### Acceptance Criteria

1. THE Application SHALL serialize Build_Plans and their Build_Items to JSON using Newtonsoft.Json, following the existing PlayerData persistence pattern.
2. THE Application SHALL deserialize Build_Plans from JSON on application load, restoring all plan and item data.
3. FOR ALL valid Build_Plan objects, serializing then deserializing SHALL produce an equivalent object (round-trip property).
4. IF the PlayerData file lacks a BuildPlan section, THEN THE Application SHALL load with an empty build plan list (no error).
5. THE Application SHALL persist Build_Plans within PlayerData.json with per-player ownership (OwnerUUID).

### Requirement 10: Build Plan Ownership

**User Story:** As a player, I want build plans to be associated with my player profile, so that each player's plans are separate.

#### Acceptance Criteria

1. THE Build_Plan SHALL store an OwnerUUID referencing the player profile that created the plan.
2. WHEN the user switches player profiles, THE Application SHALL display only Build_Plans owned by the current player profile.
3. WHEN a player profile is deleted, THE Application SHALL remove all Build_Plans owned by that player profile (cascade delete).
