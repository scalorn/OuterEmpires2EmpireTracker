# Requirements Document — Empire Systems

## Introduction

Empire Systems is a unified requirements document covering the major interconnected features planned for OE2 Empire Tracker. These features share data models and cross-reference each other, so designing them together ensures the data model is built once rather than refactored with each addition.

This document consolidates the following backlog items:
- BL-001: Ship cargo capacity for delivery planning
- BL-002: Space station hubs for delivery routes
- BL-003: Delivery auto-fill time horizon parameter
- BL-011: Manufacturing queue / Build Planner
- BL-012: Ships data model
- BL-013: Ship-aware delivery execution
- BL-014: Stations
- BL-015: Station destinations in delivery routes
- BL-017: Market (transaction tracking)
- BL-032: Build queue calculator (folded into BL-011)
- BL-055: Empire-wide production queue (mining/refining/manufacturing/research)
- BL-059: Auto-create manufacturing orders from fill levels

### Document Structure

Each feature area has its own requirements section. Requirements are tagged with iteration markers indicating when they're expected to be implemented. The data model section covers the shared models that span multiple features.

### Iteration Plan

- **Iteration 1:** Build Planner (BL-011 + BL-032) — manufactory + commodity manufacturing, structure allocation, resource checking, delivery generation, queue calculator
- **Iteration 2:** Ships (BL-012) — ship data model, designer form, ownership
- **Iteration 3:** Ship-Aware Delivery (BL-001 + BL-013) — cargo capacity, trip splitting
- **Iteration 4:** Stations (BL-002 + BL-014 + BL-015) — station model, route destinations
- **Iteration 5:** Market (BL-017) — transaction tracking, pricing plan integration
- **Iteration 6:** Full Production Queue (BL-055) — mining, refining, research queue
- **Iteration 7:** Fill-Level Automation (BL-059) — auto-create orders from stock targets
- **Iteration 8:** Delivery Auto-Fill Time Horizon (BL-003) — flatpack time window filter

Iterations can be reordered based on priorities. The data model is designed to support all iterations from the start.

## Glossary

### Build Planner Terms
- **Build_Plan**: A named, per-player container holding Build_Items. Represents a batch of manufacturing work.
- **Build_Item**: A line item specifying what to produce (blueprint or commodity), quantity (items for manufactory, runs for commodity), where (colony + structure), and status.
- **Item_Type**: Category of production: Manufactory, Commodity, Mining, Refining, or Research.
- **Recipient**: Free-text field recording who items are for or what to do with them once complete.
- **Allocation**: Assignment of a Build_Item to a specific ColonyStructure at a Colony.
- **Resource_Check**: Comparison of a Build_Item's resource requirements against the target colony's warehouse.
- **Shortfall**: Resources required but not available in the target colony's warehouse.
- **Queue_Calculator**: Computes how many items/runs to queue to keep a structure busy for a target duration.
- **Target_Duration**: Time span in countdown format (e.g. "2d 12h 0m 0s") for the queue calculator.
- **Item_Status**: Lifecycle state: Staged → Delivering → Ready → In_Progress → Completed.
- **Stock_Target**: A persistent rule specifying a minimum quantity of an item to maintain, scoped to empire-wide, a specific colony, or a specific station (BL-059).
- **Supply_Chain**: A defined sequence of resource flow stages (mine → collect → refine → deliver) with production rates and accumulation thresholds that drive automatic delivery plan generation.

### Ship Terms
- **Ship_Template**: A reusable configuration defining a hull blueprint and all component blueprints for a specific ship build. Used to order ship builds.
- **Ship**: An actual built ship instance owned by a player, with a name, hull, installed components, and computed stats.
- **Ship_Component**: An installed item on a ship (reactor, drive, weapons, cargo pods, etc.) — each references a blueprint.
- **Cargo_Capacity**: Total cargo volume a ship can carry, determined by hull + cargo pod components.
- **Ship_Class**: The hull type (Clipper, Freighter, Destroyer, etc.) — already modeled in blueprint data.
- **Assembly_Location**: The station or colony where ship components are delivered for final assembly. Class 6/7/8 ships have station type restrictions.

### Station Terms
- **Station**: A location that stores items. Can be government-owned or player-owned. Three types: Outpost, Station, Starbase — which determines ship assembly class limits.
- **Station_Hold**: The inventory/storage at a station, modeled as an ItemBag (same as colony warehouse) with no capacity limit.

### Market Terms
- **Market_Listing**: A record of items listed for sale at a station — item, quantity available, price per unit, station location.
- **Market_Transaction**: A record of a buy or sell event: item, quantity, price, counterparty, station, timestamp. Sell transactions decrement the associated listing quantity.
- **Transaction_Type**: Buy or Sell.

### Delivery Terms
- **Trip**: A single ship journey along a delivery route. Cargo capacity may require multiple trips to fulfill a plan.
- **Time_Horizon**: A configurable window for flatpack auto-fill — only include structures expected to be built within this period.

### Shared Terms
- **Manufacturing_Time**: Time to manufacture one item, from blueprint's "Manufacture Run Time" property.
- **Commodity_Cycle_Time**: Time for one commodity production cycle (GameConstants.CommodityCycleSeconds). Each cycle produces GameConstants.CommoditiesPerCycle items.
- **CountdownFormat**: Human-readable time string "Xd Yh Zm Ws" parsed by CountdownFormatParser.
- **PlayerData**: JSON file where player-specific data is persisted.

---

## Section 1: Build Planner (BL-011 + BL-032)

**Iteration:** 1
**Backlog Items:** BL-011 (Manufacturing Queue), BL-032 (Build Queue Calculator)

### Resolved Questions

#### OQ-1: Scope of Production Types
**Decision:** Iteration 1 covers Manufactory blueprints and Commodity Factory commodities. Mining, refining, and research are added in Iteration 6 (BL-055). The data model supports all types from the start.

#### OQ-2: Recipient Field
**Decision:** Free-text field, not linked to player profiles. User records whatever they need.

#### OQ-3: Resource Staging
**Decision:** Planner checks resource requirements against colony warehouse and flags shortfalls. Uses existing StagingResources flag on ColonyStructure.

#### OQ-4: Delivery Plan Generation
**Decision:** User picks an existing delivery route. Planner creates/updates a delivery plan on that route for missing resources.

#### OQ-5: BL-032 Integration
**Decision:** Queue calculator is part of the Build Planner form, not a separate feature.

#### OQ-6: Status Tracking
**Decision:** Manual transitions. User marks items started/completed. Activity form shows completion times. Inactivity form shows items needing action.

#### OQ-7: Structure Eligibility
**Decision:** All built-and-online structures of the correct type are shown. Busy indicator on each. Checkbox to filter to idle-only. Multiple items can share a structure — user manages sequencing.

#### OQ-8: Delivery Route Reuse
**Decision:** User picks an existing route. Delivery plan is specific to this build plan. Plan merging is a future feature.

#### OQ-9: Activity/Inactivity Display
**Decision:** Build plan items piggyback on existing Manufacturing activity type with plan name annotation.

#### OQ-10: Build Plan Deletion
**Decision:** Deleting a build plan leaves associated delivery plans/routes in place.

#### OQ-11: Commodity Quantity Unit
**Decision:** Commodity items specify quantity in runs (cycles), not individual items. Total output displayed for reference.

### Requirement 1.1: Build Plan CRUD

**User Story:** As a player, I want to create, edit, and delete build plans, so that I can organize manufacturing work into logical batches.

#### Acceptance Criteria

1. THE Build_Plan SHALL have a UUID, name, OwnerUUID, optional description, and optional DeliveryPlanUUID reference.
2. WHEN the user creates a new Build_Plan, THE Application SHALL persist it with a generated UUID associated with the current player profile.
3. WHEN the user edits a Build_Plan name or description, THE Application SHALL persist the changes.
4. WHEN the user deletes a Build_Plan, THE Application SHALL remove the plan and all Build_Items. Associated delivery plans/routes SHALL be left in place.
5. THE Application SHALL allow multiple Build_Plans per player profile.
6. IF a Build_Plan name is empty or whitespace, THEN THE Application SHALL reject the save.

### Requirement 1.2: Build Item Management

**User Story:** As a player, I want to add, edit, and remove items from a build plan, so that I can specify what needs to be manufactured.

#### Acceptance Criteria

1. THE Build_Item SHALL have a UUID, Item_Type (Manufactory or Commodity), quantity, optional Recipient, optional notes, and Item_Status.
2. WHEN Item_Type is Manufactory, THE Build_Item SHALL reference a Blueprint by UUID and store the output item name.
3. WHEN Item_Type is Commodity, THE Build_Item SHALL reference a Commodity by name.
4. WHEN adding a Build_Item, THE Application SHALL present eligible blueprints or commodities based on Item_Type.
5. Manufactory quantity represents items to manufacture (≥ 1).
6. Commodity quantity represents production runs (≥ 1). Total output (runs × CommoditiesPerCycle) displayed for reference.
7. IF quantity < 1, THEN THE Application SHALL reject the save.
8. Removing a Build_Item removes it from the plan and persists the change.

### Requirement 1.3: Structure Allocation

**User Story:** As a player, I want to assign each build item to a specific structure at a colony, so that I know where each item will be produced.

#### Acceptance Criteria

1. THE Application SHALL present colonies and eligible structures with a filter text box.
2. Manufactory items filter to built-and-online Manufactories.
3. Commodity items filter to built-and-online Commodity Factories.
4. THE Application SHALL indicate which structures are currently busy.
5. THE Application SHALL provide a toggle to filter to idle structures only.
6. Allocation stores Colony UUID and ColonyStructure UUID on the Build_Item.
7. Allocation sets StagingResources, ManufacturingBlueprintUUID/ManufacturingCommodityName, and ManufacturingQuantity on the target structure.
8. Build_Items can exist without allocation.
9. Removing allocation clears structure references and resets StagingResources.
10. Multiple Build_Items can be allocated to the same structure.

### Requirement 1.4: Resource Availability Check

**User Story:** As a player, I want to see which items have sufficient resources at their target colony.

#### Acceptance Criteria

1. THE Application SHALL compute total resource requirements from blueprint Resources or commodity ConstructionResources × quantity.
2. THE Application SHALL compare against colony warehouse inventory (Refined purity for natural resources, appropriate purity for synthetics).
3. WHEN all resources available, indicate the item is ready to start.
4. WHEN resources are short, display shortfall quantities per resource.
5. Resource check updates when the plan is opened and when colony data changes.

### Requirement 1.5: Delivery Plan Generation

**User Story:** As a player, I want the planner to generate a delivery plan for missing resources.

#### Acceptance Criteria

1. THE Application SHALL identify all allocated Build_Items with resource shortfalls.
2. THE Application SHALL present existing DeliveryRoutes for the user to select.
3. THE Application SHALL create a DeliveryPlan on the selected route with drop-off items for each shortfall.
4. IF the Build_Plan already has an associated DeliveryPlan, update it rather than creating a new one.
5. Name the plan based on the Build_Plan name (e.g. "Build: {PlanName}").
6. Update Item_Status of affected Build_Items to Delivering.
7. Store the DeliveryPlan UUID on the Build_Plan.

### Requirement 1.6: Build Queue Calculator

**User Story:** As a player, I want to calculate how many items to queue to keep a structure busy for a target duration.

#### Acceptance Criteria

1. For Manufactory: quantity = ceiling(Target_Duration_seconds / Manufacturing_Time_seconds).
2. For Commodity: runs = ceiling(Target_Duration_seconds / Commodity_Cycle_Time_seconds).
3. IF Target_Duration cannot be parsed, do not compute.
4. IF blueprint has no "Manufacture Run Time", indicate time is unknown.
5. Computed value populates the Build_Item quantity field.
6. Calculator accessible from the Build_Item add/edit UI.

### Requirement 1.7: Item Status Tracking

**User Story:** As a player, I want to track each item through the manufacturing pipeline.

#### Acceptance Criteria

1. New items without allocation: Staged.
2. After delivery plan generated: Delivering.
3. Resources available: user can set to Ready.
4. Manufacturing started in-game: user sets to In_Progress.
5. Production finished: user sets to Completed.
6. Status displayed visually in the plan.
7. User can manually transition between any status.

### Requirement 1.8: Activity/Inactivity Integration

**User Story:** As a player, I want build plan items to appear in Activity and Inactivity forms.

#### Acceptance Criteria

1. In_Progress items with active timers appear in Activity under Manufacturing type with plan name.
2. Staged items with shortfalls appear in Inactivity indicating resources needed.
3. Ready items appear in Inactivity indicating manufacturing needs to start.
4. Delivering items appear in Inactivity indicating delivery in progress.
5. Items identified by plan name + item name.

---

## Section 2: Ships (BL-012)

**Iteration:** 2
**Backlog Items:** BL-012 (Ships)

### Resolved Questions

#### OQ-14: Ship Template vs Ship Instance
**Decision:** A Ship_Template defines a reusable configuration (hull blueprint + component blueprints). A Ship is an actual built instance. Ordering a ship build from a template generates individual Build_Items for the hull and each component, which flow through the build planner (stock check → manufacturing → resource delivery → component delivery to assembly point).

#### OQ-15: Ship Build Restrictions
**Decision:** Station types are Outpost, Station, and Starbase. Assembly restrictions by ship class: Class 2-5 can be built at any station type. Class 6 requires Station or Starbase. Class 7-8 requires Starbase only. There are no class 1 ships.

### Requirement 2.1: Ship Template

**User Story:** As a player, I want to define reusable ship templates specifying a hull and all components, so that I can order builds of a specific ship configuration.

#### Acceptance Criteria

1. THE Ship_Template SHALL have a UUID, Name, OwnerUUID, and a hull Blueprint UUID.
2. THE Ship_Template SHALL have a list of component slots, each referencing a Blueprint UUID for the installed component.
3. THE hull blueprint SHALL define available slot counts per component type (reactors, cargo pods, fuel pods, weapons, etc.). The template SHALL enforce these limits.
4. THE Application SHALL persist Ship_Templates to PlayerData.json.
5. THE Application SHALL allow creating, editing, and deleting templates.
6. Ship_Templates belong to a player profile (OwnerUUID). Cascade delete on player deletion.

### Requirement 2.2: Ship Template Designer Form

**User Story:** As a player, I want a form to create and edit ship templates by selecting a hull and filling component slots.

#### Acceptance Criteria

1. THE Application SHALL provide a Ship Template Designer MDI child form.
2. THE form SHALL allow selecting a hull blueprint, which determines available component slots.
3. THE form SHALL allow installing/removing component blueprints into slots, enforcing the hull's slot limits per component type.
4. THE form SHALL display computed stats: cargo capacity, mass, power, etc.
5. THE form SHALL show all templates for the current player in a list with filter.

### Requirement 2.3: Ship Build Order

**User Story:** As a player, I want to order a build of a ship template, which generates build items for the hull and every component.

#### Acceptance Criteria

1. WHEN the user orders a build from a Ship_Template, THE Application SHALL create a Build_Plan (or add to an existing one) with individual Build_Items for the hull and each component blueprint.
2. Each Build_Item SHALL be a Manufactory item referencing the component's blueprint.
3. THE Application SHALL check stock levels for each component — if the item already exists in a warehouse or station hold, it does not need to be manufactured.
4. THE Application SHALL generate manufacturing Build_Items only for components not already in stock.
5. THE ship build order SHALL specify an assembly location (station or colony) where all components will be delivered once manufactured.
6. THE Application SHALL enforce assembly location restrictions based on ship class and station type: Class 2-5 at any station type (Outpost, Station, Starbase), Class 6 at Station or Starbase only, Class 7-8 at Starbase only.
7. AFTER all components are manufactured, THE Application SHALL generate a delivery plan to bring all components to the assembly location.

### Requirement 2.4: Ship Instance Tracking

**User Story:** As a player, I want to record ships I own with their actual configuration and current location, so that I can reference them in delivery plans and track their cargo.

#### Acceptance Criteria

1. THE Ship SHALL have a UUID, Name, OwnerUUID, Ship_Template UUID (optional — may be built without a template), a list of installed components, a current location (Station UUID or Colony UUID with DestinationType), and a cargo hold (ItemBag).
2. THE Ship SHALL have a computed Cargo_Capacity (volume) derived from hull + cargo pod components.
3. THE Ship's cargo hold tracks what is currently loaded, constrained by Cargo_Capacity volume.
4. THE Application SHALL persist Ships to PlayerData.json.
5. Ships belong to a player profile (OwnerUUID). Cascade delete on player deletion.
6. THE Application SHALL allow creating ships from a template (copies the configuration) or manually.
7. THE Ship Template Designer and Ship Instance forms SHALL be separate MDI child forms.

---

## Section 3: Ship-Aware Delivery (BL-001 + BL-013)

**Iteration:** 3
**Backlog Items:** BL-001 (Ship Cargo Capacity), BL-013 (Ship-Aware Delivery Execution)

### Resolved Questions

#### OQ-16: Cargo Capacity Unit
**Decision:** Ship cargo capacity is measured by volume. Items have both volume and mass (from blueprint properties). Resources have a fixed volume; mass varies by resource. Mass affects ship speed but does not prevent loading — volume is the hard limit.

### Requirement 3.1: Ship Assignment to Delivery Plans

**User Story:** As a player, I want to assign a ship to a delivery plan, so that cargo capacity is factored into execution.

#### Acceptance Criteria

1. THE DeliveryPlan SHALL have an optional ShipUUID reference.
2. WHEN a ship is assigned, THE Application SHALL display the ship's cargo capacity on the execution form.
3. THE Application SHALL present the player's ships for selection when assigning.

### Requirement 3.2: Cargo Capacity Enforcement

**User Story:** As a player, I want to see when a delivery plan exceeds my ship's cargo volume, so that I can split trips or prioritize.

#### Acceptance Criteria

1. WHEN a ship is assigned, THE Application SHALL compute total cargo volume for the load-before-departure list using item volumes from blueprint properties and resource volumes.
2. IF total volume exceeds the ship's Cargo_Capacity, THE Application SHALL display a warning with the overage amount.
3. THE Application SHALL display total mass for informational purposes (affects travel speed but does not block loading).
4. THE Application SHALL allow the user to proceed anyway (volume warning is advisory, not blocking).

### Requirement 3.3: Trip Splitting

**User Story:** As a player, I want the system to suggest splitting a delivery into multiple trips when cargo exceeds capacity.

#### Acceptance Criteria

1. WHEN cargo exceeds capacity, THE Application SHALL offer to split the plan into multiple trips.
2. Each trip SHALL respect the ship's cargo volume limit.
3. THE Application SHALL compute per-trip volume and mass totals.
3. Trip splitting SHALL maintain stop order from the route.
4. THE user SHALL be able to manually adjust trip contents.

---

## Section 4: Stations (BL-002 + BL-014 + BL-015)

**Iteration:** 4
**Backlog Items:** BL-002 (Station Hubs), BL-014 (Stations), BL-015 (Station Destinations)

### Requirement 4.1: Station Data Model

**User Story:** As a player, I want to record stations I use, so that I can include them in delivery routes and track inventory there.

#### Acceptance Criteria

1. THE Station SHALL have a UUID, Name, StationType (Outpost, Station, or Starbase), an ownership flag (Government or PlayerOwned), and an optional OwnerUUID (for player-owned).
2. THE Station SHALL have a Station_Hold modeled as an ItemBag (same as colony warehouse), with no capacity limit.
3. THE Application SHALL persist Stations to PlayerData.json.
4. Player-owned stations belong to a player profile. Government stations are shared.

### Requirement 4.2: Station Management Form

**User Story:** As a player, I want a form to create, edit, and delete station records.

#### Acceptance Criteria

1. THE Application SHALL provide a Station management MDI child form.
2. THE form SHALL allow creating government and player-owned stations.
3. THE form SHALL display station inventory (hold contents).
4. THE form SHALL allow manual inventory editing (add/remove items, adjust quantities).

### Requirement 4.3: Station Destinations in Routes

**User Story:** As a player, I want to add stations as stops in delivery routes, so that I can pick up and drop off cargo at stations.

#### Acceptance Criteria

1. THE DeliveryRoute RouteStop model SHALL have a DestinationUUID and a DestinationType (Colony or Station) to support both destination types.
2. Stations SHALL have deterministic UUIDs generated from a common namespace using the station name as the seed (same pattern as DeterministicUUID). Station names cannot currently be renamed in-game; if renaming becomes possible, a migration will be needed.
3. WHEN adding a stop, THE Application SHALL present both colonies and stations for selection.
4. Drop-off and pick-up at station stops SHALL follow the same patterns as colony stops.
5. WHEN a delivery is executed at a station stop (items marked as delivered), THE Application SHALL update the station's hold inventory — drop-offs add items, pick-ups remove items.
6. Auto-fill SHALL consider station inventory when computing shortfalls.

---

## Section 5: Market (BL-017)

**Iteration:** 5
**Backlog Items:** BL-017 (Market)

### Resolved Questions

#### OQ-12: Sale Recording Workflow
**Decision:** In-game, when something sells on the market the player receives a mail. The player then comes into the tool to record what sold, where, and to whom. This single action of recording a sale is the trigger point for the entire cascade: listing quantity decrements → stock target check fires → shortfall detected → build items created → delivery plan generated. The tool does not connect to the game — all market data is manually entered.

#### OQ-13: Inventory Mutation Rules
**Decision:** Selling manufactured items (commodities, munitions, etc.) decrements the listing quantity — you lose what you sold. Selling a blueprint copy does not remove the blueprint from your collection (blueprints are copyable). Selling a survey removes it from your collection (surveys cannot be copied), but the survey data is retained for reference since you can always rescan.

### Requirement 5.1: Market Listings

**User Story:** As a player, I want to record items I have listed for sale at stations, so that I can track my active market inventory.

#### Acceptance Criteria

1. THE Market_Listing SHALL have a UUID, item reference (Blueprint UUID, Commodity name, or other item type + name), quantity listed, price per unit, Station UUID (where listed), and OwnerUUID.
2. THE Application SHALL persist Market_Listings to PlayerData.json.
3. THE Application SHALL allow creating, editing, and deleting listings.
4. THE listing quantity represents how many of that item are currently available for sale at that station.

### Requirement 5.2: Market Transaction Recording

**User Story:** As a player, I want to record market transactions, so that I can track my trading history and evaluate profitability.

#### Acceptance Criteria

1. THE Market_Transaction SHALL have a UUID, Transaction_Type (Buy or Sell), item reference (Blueprint UUID, Commodity name, or other item type + name), quantity, price per unit, total price, counterparty (free-text), timestamp, Station UUID (where the transaction occurred), and optional notes.
2. THE Application SHALL persist transactions to PlayerData.json with OwnerUUID.
3. THE Application SHALL allow creating, editing, and deleting transactions.

### Requirement 5.3: Inventory Mutation on Sale

**User Story:** As a player, I want recording a sale to automatically decrement my listing quantity, so that my market inventory stays accurate.

#### Acceptance Criteria

1. WHEN a Sell transaction is recorded against a Market_Listing, THE Application SHALL decrement the listing quantity by the transaction quantity.
2. IF the listing quantity reaches zero, THE Application SHALL mark the listing as sold out (or remove it, per user preference).
3. AFTER decrementing a listing quantity, THE Application SHALL trigger a Stock_Target check for the affected item. If the new quantity falls below a target, the stock target system (BL-059) can generate build orders and delivery plans.
4. Blueprint copies can be sold without removing the blueprint from the player's collection (blueprints are copyable).
5. Surveys cannot be copied — selling a survey removes it from the player's collection, but the survey data is retained for reference.

### Requirement 5.4: Market Form

**User Story:** As a player, I want a form to view and manage my market listings and transactions.

#### Acceptance Criteria

1. THE Application SHALL provide a Market MDI child form.
2. THE form SHALL display active listings and transaction history in separate views.
3. THE form SHALL support filtering by item, transaction type, counterparty, station, and date range.
4. THE form SHALL display running totals (total bought, total sold, net profit/loss).

### Requirement 5.5: Pricing Plan Integration

**User Story:** As a player, I want to see the pricing plan valuation alongside market prices, so that I can evaluate whether trades are profitable.

#### Acceptance Criteria

1. WHEN a pricing plan is selected, THE Application SHALL display the plan's computed price alongside the listing/transaction price.
2. THE Application SHALL compute profit/loss per transaction as (sell price - plan price) or (plan price - buy price).
3. THE pricing plan selector SHALL follow the same pattern as the blueprint form (combo box with plan list).

---

## Section 6: Full Production Queue (BL-055)

**Iteration:** 6
**Backlog Items:** BL-055 (Mining/Refining/Manufacturing/Research Queue)

### Resolved Questions

#### OQ-17: Mining/Refining Supply Chain
**Decision:** Mining and refining are part of multi-hop logistics chains. Colonies mine resources, unrefined resources accumulate and need to be moved to central stations, then delivered to refining colonies, then refined resources flow to manufacturing stations. The data model should support defining these supply chains and automatically generating task lists (delivery plans, manufacturing orders). Without a game API, the generated plans are task lists for the user to execute manually. The model is designed so automation can be added when an API becomes available.

#### OQ-18: Time-Splitting on Structures
**Decision:** A colony may need to split mining/refining time between multiple resources due to flatpack limits. The planner should support scheduling multiple operations on a single structure over time (e.g. mine resource A for 2 days, then switch to resource B for 1 day).

### Requirement 6.1: Extended Item Types

**User Story:** As a player, I want to queue mining, refining, and research operations alongside manufacturing, so that I have a unified view of all production.

#### Acceptance Criteria

1. THE Build_Item Item_Type SHALL be extended to include Mining, Refining, and Research.
2. Mining items specify a resource to mine and a survey assignment.
3. Refining items specify a resource and target purity.
4. Research items reference a Blueprint by UUID for evolution. Quantity is always 1.
5. Allocation filters to appropriate structure types (Mining Rig, Refinery, Research Lab).

### Requirement 6.2: Supply Chain Definition

**User Story:** As a player, I want to define resource flow chains (mine → collect → refine → deliver to manufacturing), so that the system can generate the logistics plans I need.

#### Acceptance Criteria

1. THE Application SHALL support defining a Supply_Chain as a sequence of stages: source locations (mining colonies), collection points (stations), processing locations (refining colonies), and destination locations (manufacturing stations/colonies).
2. Each stage SHALL specify what resource flows in and out, and at what rate or quantity.
3. THE Application SHALL compute accumulation at each stage based on production rates and consumption rates.
4. WHEN accumulation at a stage exceeds a configurable threshold, THE Application SHALL generate a delivery plan to move resources to the next stage.
5. THE Supply_Chain definition SHALL be persisted to PlayerData.json.

### Requirement 6.3: Empire-Wide Production View

**User Story:** As a player, I want to see all production across all colonies in one view.

#### Acceptance Criteria

1. THE Application SHALL provide an empire-wide production view showing all active and queued Build_Items across all plans and colonies.
2. THE view SHALL be filterable by Item_Type, colony, status, and plan.
3. THE view SHALL show estimated completion times for in-progress items.
4. THE view SHALL show production rates for mining and refining operations.

### Requirement 6.4: Dependency Tracking

**User Story:** As a player, I want to specify that one operation depends on another (e.g. refine before manufacture), so that the system can flag when prerequisites aren't met.

#### Acceptance Criteria

1. THE Build_Item SHALL support an optional DependsOnUUID referencing another Build_Item.
2. WHEN a dependency is set, THE Application SHALL flag the item if its prerequisite is not Completed.
3. THE Inactivity form SHALL show items with unmet dependencies.

### Requirement 6.5: Structure Time-Splitting

**User Story:** As a player, I want to schedule multiple operations on a single structure over time, so that I can split mining or refining between resources.

#### Acceptance Criteria

1. THE Application SHALL allow multiple Build_Items to be allocated to the same structure with a sequence order.
2. Each Build_Item SHALL have an optional duration or quantity that determines when the structure switches to the next item.
3. THE Application SHALL display the schedule for a structure showing what runs when.
4. THE Inactivity form SHALL flag when a structure's current operation is complete and the next one needs to be started.

---

## Section 7: Fill-Level Automation (BL-059)

**Iteration:** 7
**Backlog Items:** BL-059 (Auto-Create Orders from Fill Levels)

### Requirement 7.1: Stock Targets

**User Story:** As a player, I want to define target stock levels for items at specific locations or empire-wide, so that the system can identify shortfalls and create manufacturing orders.

#### Acceptance Criteria

1. THE Stock_Target SHALL have a UUID, item reference (type + name), target quantity, OwnerUUID, and a scope: Empire-wide, Colony (with Colony UUID), or Station (with Station UUID).
2. An empire-wide target checks total quantity across all colonies and stations.
3. A colony-specific target checks quantity at that colony's warehouse only.
4. A station-specific target checks quantity at that station's hold only.
5. THE Application SHALL persist Stock_Targets to PlayerData.json.
6. THE Application SHALL allow creating, editing, and deleting stock targets.

### Requirement 7.2: Automatic Order Generation

**User Story:** As a player, I want the system to check stock levels and create build items to replenish shortfalls.

#### Acceptance Criteria

1. WHEN the user triggers a stock check, THE Application SHALL scan quantities for each Stock_Target based on its scope (empire-wide, colony, or station).
2. IF current quantity is below the target, THE Application SHALL compute the shortfall.
3. THE Application SHALL create Build_Items in a designated Build_Plan to cover the shortfall.
4. THE Application SHALL not create duplicate orders for the same shortfall if an existing Build_Item already covers it.

---

## Section 8: Delivery Auto-Fill Time Horizon (BL-003)

**Iteration:** 8
**Backlog Items:** BL-003 (Delivery Auto-Fill Time Horizon)

### Requirement 8.1: Time Horizon Parameter

**User Story:** As a player, I want flatpack auto-fill to only include structures expected to be built within a configurable time window, so that I don't deliver flatpacks for structures that won't be built for weeks.

#### Acceptance Criteria

1. THE auto-fill dialog SHALL include a Time_Horizon input in countdown format.
2. WHEN a Time_Horizon is set, auto-fill SHALL only include flatpack requests for structures whose build completion time is within the horizon.
3. IF Time_Horizon is empty or not set, auto-fill SHALL include all flatpack requests (current behavior).
4. THE Time_Horizon SHALL be persisted as a preference so it carries across sessions.

---

## Section 9: Shared Data Model & Serialization

**Iteration:** 1 (data model built to support all iterations)

### Requirement 9.1: Unified Persistence

**User Story:** As a player, I want all empire systems data to persist across sessions.

#### Acceptance Criteria

1. Build_Plans, Ships, Stations, Market_Transactions, and Stock_Targets SHALL be serialized to PlayerData.json using Newtonsoft.Json following existing patterns.
2. Each entity type SHALL be a top-level array in PlayerRoot.
3. FOR ALL entity types, serializing then deserializing SHALL produce equivalent objects (round-trip property).
4. IF PlayerData lacks any of these arrays, THE Application SHALL load with empty lists (no error).
5. All per-player entities SHALL have OwnerUUID and participate in cascade delete and orphan cleanup.

### Requirement 9.2: Cross-Entity References

**User Story:** As a developer, I want entity references to be consistent and resolvable.

#### Acceptance Criteria

1. Build_Items reference Blueprints by UUID, Commodities by name, Colonies by UUID, and ColonyStructures by UUID.
2. Ships reference Blueprints by UUID for components and ShipClass by existing ID.
3. DeliveryPlans optionally reference Ships by UUID.
4. DeliveryRoute stops have a DestinationUUID and DestinationType (Colony or Station).
5. Market_Listings and Market_Transactions reference items by Blueprint UUID or Commodity/item name, and reference Stations by UUID.
6. Sell transactions decrement Market_Listing quantities, which feeds into Stock_Target checks.
6. Build_Items optionally reference other Build_Items by UUID for dependency tracking.
7. Stock_Targets reference items by type + name.

### Requirement 9.3: Player Ownership

**User Story:** As a player, I want all data scoped to my player profile.

#### Acceptance Criteria

1. All entities with OwnerUUID SHALL be filtered by current player on display.
2. Player switch SHALL refresh all forms showing owned data.
3. Player deletion SHALL cascade delete all owned entities.
4. Government stations are shared (no OwnerUUID filter).
