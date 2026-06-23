# Requirements Document

## Introduction

BL-113 restructures DeliveryPlan management so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a DeliveryPlanService that applies changes atomically. Neither FormDeliveryRoute (Plan tab) nor FormDeliveryExecution directly mutates a DeliveryPlan --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), and BL-123 (pricing plans).

### Key Differences from BL-112 (DeliveryRoute)

1. **Cross-form mutation** --- Two forms mutate DeliveryPlan: FormDeliveryRoute (Plan tab) for CRUD and item management, and FormDeliveryExecution for marking items delivered, completing stops, completing plans, and assigning ships.
2. **Execution operations are immediate** --- Execution operations (mark delivered, complete stop, complete plan, assign ship) persist immediately because they have side effects (commodity fulfillment, flatpack staging, resource delivery, worker delivery, station hold updates).
3. **Plan tab operations persist immediately** --- Plan CRUD (create, delete, rename) and item management (add/remove drop-off/pick-up) persist immediately via the service (matching current behavior where Save() is called after each modification).
4. **AutoFill operations** --- Complex AutoFill methods (commodities, flatpacks, resources, workers) scan colonies and add items, persisting immediately via the service.
5. **Trip splitting** --- FormDeliveryExecution can split a plan into multiple trip plans, creating new DeliveryPlan entities via the service.
6. **ReadOnly wrappers already complete** --- ReadOnlyDeliveryPlan, ReadOnlyDeliveryPlanStop, and ReadOnlyDeliveryItem are fully implemented.
7. **No write locks** --- No ReaderWriterLockSlim on DeliveryPlan. Single-threaded UI access only.

### Similarities to BL-112

1. **Service as sole mutator** --- Same pattern: form -> ViewModel/Service -> entity.
2. **ReadOnly wrappers in list/display paths** --- Plan dropdown and execution display use ReadOnly types.
3. **Delete with confirmation** --- Plan deletion prompts before proceeding.
4. **PlayerContext accessors** --- GetCurrentPlayerReadOnlyPlans already exists. FindMutableDeliveryPlan needs to be added.
5. **DeliveryDataChanged event** --- Service fires this event after mutations.

### Scoping Decision: ViewModel Scope

The DeliveryPlanViewModel is rewritten as a disconnected edit buffer. It retains its current role as a plan manipulation helper (GetOrCreateStop, AddDropOffItem, AddPickUpItem, RemoveDropOffItems, RemovePickUpItems, AutoFill methods) but loses direct entity access. It operates on a local deep-copy of the plan data and builds request DTOs for the service.

### Scoping Decision: Execution Operations

Execution operations (MarkItemDelivered, MarkStopComplete, MarkPlanComplete, SetShipUUID) bypass the ViewModel entirely. FormDeliveryExecution calls the service directly for these operations because they have immediate side effects on other entities.

## Glossary

- **DeliveryPlan**: The mutable entity representing a delivery plan with UUID, Name, OwnerUUID, RouteUUID, ShipUUID, Completed flag, and a list of DeliveryPlanStop entries.
- **DeliveryPlanStop**: A nested object within DeliveryPlan representing a single stop with ColonyUUID, Sequence, StopCompleted, DestinationType, DestinationUUID, and DropOff/PickUp item lists.
- **DeliveryItem**: A nested object within DeliveryPlanStop representing a single delivery item with ItemType, BaseItemTypeID, Name, ResourcePurity, Quantity, and Delivered flag.
- **ReadOnlyDeliveryPlan**: An immutable wrapper around DeliveryPlan exposing only getter properties and read-only Stops list.
- **ReadOnlyDeliveryPlanStop**: An immutable wrapper around DeliveryPlanStop exposing all fields as read-only plus read-only DropOff/PickUp lists.
- **ReadOnlyDeliveryItem**: An immutable wrapper around DeliveryItem exposing all fields as read-only.
- **DeliveryPlanViewModel**: The ViewModel class that holds a local edit buffer of plan data, disconnected from the entity.
- **DeliveryPlanService**: A new service class that is the sole mutator of DeliveryPlan entities (CRUD, item operations, execution operations).
- **FormDeliveryRoute**: The WinForms form for viewing and editing delivery routes and plans (Plan tab).
- **FormDeliveryExecution**: The WinForms form for executing delivery plans (marking items delivered, completing stops/plans).
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **DeliveryPlanCreateRequest**: A DTO carrying Name and RouteUUID for creating a new plan.
- **DeliveryPlanUpdateRequest**: A DTO carrying the current plan state from the ViewModel to the service for an update operation.
- **Immediate_Operation**: An operation that persists immediately because it has side effects on other entities (colonies, stations).

## Requirements

## Phase 1: Read-Only Consumer Migration

### Requirement 1: Plan Dropdown Uses ReadOnly Wrappers

**User Story:** As a developer, I want the plan dropdown on FormDeliveryRoute to use ReadOnlyDeliveryPlan, so that no mutable entity references leak into the display path.

#### Acceptance Criteria

1. WHEN the FormDeliveryRoute populates the plan dropdown, THE FormDeliveryRoute SHALL obtain plan data from PlayerContext.GetCurrentPlayerReadOnlyPlans().
2. WHEN the user selects a plan in the dropdown, THE FormDeliveryRoute SHALL extract the ReadOnlyDeliveryPlan UUID and use it to initialize the ViewModel or call the service.
3. THE FormDeliveryRoute plan dropdown SHALL NOT hold a direct reference to a mutable DeliveryPlan entity.

### Requirement 2: Execution Form Uses ReadOnly for Display

**User Story:** As a developer, I want FormDeliveryExecution to use ReadOnlyDeliveryPlan for display purposes, so that the display path does not hold mutable references.

#### Acceptance Criteria

1. WHEN FormDeliveryExecution displays plan information (stop titles, item lists, load list), THE FormDeliveryExecution SHALL read from ReadOnlyDeliveryPlan or from service return values.
2. THE FormDeliveryExecution SHALL NOT hold a direct mutable DeliveryPlan reference for display-only code paths.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable DeliveryPlan, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormDeliveryRoute Plan tab SHALL NOT hold a direct reference to a mutable DeliveryPlan in any display-only code path (plan dropdown, plan name display, item grid display).
2. AFTER Phase 1 migration, THE FormDeliveryExecution SHALL NOT hold a direct reference to a mutable DeliveryPlan in any display-only code path (stop titles, item checkboxes, load list grid).
3. THE DeliveryPlanViewModel SHALL NOT expose the mutable DeliveryPlan entity via a public Data property or equivalent accessor.

## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyDeliveryPlan into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN a plan is loaded, THE DeliveryPlanViewModel SHALL copy the Name field from the ReadOnlyDeliveryPlan into a local property.
2. WHEN a plan is loaded, THE DeliveryPlanViewModel SHALL deep-copy the Stops list into local DeliveryPlanStop instances (preserving all fields per stop including DropOff and PickUp item lists).
3. THE DeliveryPlanViewModel SHALL retain the original ReadOnlyDeliveryPlan snapshot for reference.
4. THE DeliveryPlanViewModel SHALL store the UUID, OwnerUUID, RouteUUID, ShipUUID, and Completed from the original snapshot.
5. THE DeliveryPlanViewModel SHALL NOT hold a reference to the mutable DeliveryPlan entity.

### Requirement 5: ViewModel Item Operations

**User Story:** As a developer, I want the ViewModel to support add/remove operations on drop-off and pick-up items locally, so that item management works on the disconnected buffer.

#### Acceptance Criteria

1. THE DeliveryPlanViewModel SHALL provide a GetOrCreateStop method that finds or creates a local DeliveryPlanStop by destination.
2. THE DeliveryPlanViewModel SHALL provide AddDropOffItem and AddPickUpItem methods that add DeliveryItem entries to a local stop.
3. THE DeliveryPlanViewModel SHALL provide RemoveDropOffItems and RemovePickUpItems methods that remove items by index from a local stop.
4. WHEN item operations are performed, THE DeliveryPlanViewModel SHALL modify only local state (no entity mutation).

### Requirement 6: ViewModel AutoFill Operations

**User Story:** As a developer, I want the ViewModel to retain AutoFill methods that scan colonies and add items to the local plan buffer, so that auto-fill functionality continues to work.

#### Acceptance Criteria

1. THE DeliveryPlanViewModel SHALL provide AutoFillCommodities, AutoFillFlatpacks, AutoFillManufacturingResources, and AutoFillWorkers methods.
2. WHEN AutoFill methods are called, THE DeliveryPlanViewModel SHALL add items to local stops only (no entity mutation).
3. THE AutoFill methods SHALL accept route stops and finder delegates as parameters (same interface as current).

### Requirement 7: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the DeliveryPlan entity directly, so that the entity remains unchanged until the service is called.

#### Acceptance Criteria

1. THE DeliveryPlanViewModel SHALL NOT write Name changes to the DeliveryPlan entity directly.
2. THE DeliveryPlanViewModel SHALL NOT mutate the DeliveryPlan entity Stops list when items are added or removed.
3. THE current write-through pattern (ViewModel methods directly mutate entity Stops and items, Save() persists directly) SHALL be replaced with service calls.

## Phase 3: DeliveryPlanService

### Requirement 8: DeliveryPlanService.Create

**User Story:** As a developer, I want a service method that creates a new plan, so that plan creation goes through a controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a Create method accepting a name string and a route UUID string.
2. WHEN Create is called, THE DeliveryPlanService SHALL create a new DeliveryPlan entity with a generated UUID.
3. WHEN Create is called, THE DeliveryPlanService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE DeliveryPlanService SHALL set the RouteUUID from the parameter.
5. WHEN Create is called, THE DeliveryPlanService SHALL set the Name from the parameter.
6. WHEN Create is called, THE DeliveryPlanService SHALL add the plan to PlayerContext via AddDeliveryPlan.
7. WHEN Create is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
8. WHEN Create is called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
9. WHEN Create is called, THE DeliveryPlanService SHALL return the new ReadOnlyDeliveryPlan.

### Requirement 9: DeliveryPlanService.Delete

**User Story:** As a developer, I want a service method that deletes a plan, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE DeliveryPlanService SHALL remove the DeliveryPlan from PlayerContext via RemoveDeliveryPlan.
3. WHEN Delete is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
5. IF the UUID is empty or the plan is not found, THEN THE DeliveryPlanService SHALL return without error.

### Requirement 10: DeliveryPlanService.UpdatePlan

**User Story:** As a developer, I want a service method that applies plan changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide an UpdatePlan method accepting a UUID string and a DeliveryPlanUpdateRequest.
2. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL look up the mutable DeliveryPlan by UUID via PlayerContext.FindMutableDeliveryPlan.
3. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL apply the Name field from the request to the entity.
4. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL replace the entity Stops list with the request Stops list (deep copy).
5. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
6. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
7. WHEN UpdatePlan is called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.
8. IF the UUID is not found, THEN THE DeliveryPlanService SHALL throw an InvalidOperationException.

### Requirement 11: DeliveryPlanService.AddDropOffItem and AddPickUpItem

**User Story:** As a developer, I want service methods that add items to a plan stop, so that item addition goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide AddDropOffItem and AddPickUpItem methods accepting a plan UUID, stop destination info, and item details.
2. WHEN AddDropOffItem is called, THE DeliveryPlanService SHALL find or create the stop and add the item to the DropOff list.
3. WHEN AddPickUpItem is called, THE DeliveryPlanService SHALL find or create the stop and add the item to the PickUp list.
4. WHEN item methods are called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
5. WHEN item methods are called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
6. WHEN item methods are called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 12: DeliveryPlanService.RemoveDropOffItems and RemovePickUpItems

**User Story:** As a developer, I want service methods that remove items from a plan stop, so that item removal goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide RemoveDropOffItems and RemovePickUpItems methods accepting a plan UUID, stop destination info, and item indices.
2. WHEN remove methods are called, THE DeliveryPlanService SHALL remove items at the specified indices from the appropriate list.
3. WHEN remove methods are called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
4. WHEN remove methods are called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
5. WHEN remove methods are called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 13: DeliveryPlanService.MarkItemDelivered

**User Story:** As a developer, I want a service method that marks a delivery item as delivered with side effects, so that execution operations go through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a MarkItemDelivered method accepting a plan UUID, stop sequence, item index, item list type (DropOff or PickUp), and delivered flag.
2. WHEN MarkItemDelivered is called, THE DeliveryPlanService SHALL set the Delivered flag on the specified item.
3. WHEN MarkItemDelivered is called with a Commodity item, THE DeliveryPlanService SHALL call DeliveryFulfillment.FulfillCommodity on the target colony.
4. WHEN MarkItemDelivered is called with a Flatpack item, THE DeliveryPlanService SHALL call DeliveryFulfillment.StageFlatpack on the target colony.
5. WHEN MarkItemDelivered is called with a WorkDetail item, THE DeliveryPlanService SHALL call DeliveryFulfillment.DeliverWorkers on the target colony.
6. WHEN MarkItemDelivered is called with a Resource item, THE DeliveryPlanService SHALL call DeliveryFulfillment.DeliverResource on the target colony.
7. WHEN MarkItemDelivered is called at a Station stop, THE DeliveryPlanService SHALL update the station hold (add/remove items).
8. WHEN MarkItemDelivered is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
9. IF all items in the plan are delivered after marking, THEN THE DeliveryPlanService SHALL set Completed to true.
10. WHEN MarkItemDelivered is called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 14: DeliveryPlanService.MarkStopComplete

**User Story:** As a developer, I want a service method that marks a stop as complete, so that stop completion goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a MarkStopComplete method accepting a plan UUID and stop sequence.
2. WHEN MarkStopComplete is called, THE DeliveryPlanService SHALL set StopCompleted to true on the specified stop.
3. WHEN MarkStopComplete is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
4. IF all items in the plan are delivered after completing the stop, THEN THE DeliveryPlanService SHALL set Completed to true.
5. WHEN MarkStopComplete is called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 15: DeliveryPlanService.MarkPlanComplete

**User Story:** As a developer, I want a service method that marks a plan as complete, so that plan completion goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a MarkPlanComplete method accepting a plan UUID.
2. WHEN MarkPlanComplete is called, THE DeliveryPlanService SHALL set Completed to true on the plan.
3. WHEN MarkPlanComplete is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
4. WHEN MarkPlanComplete is called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
5. WHEN MarkPlanComplete is called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 16: DeliveryPlanService.SetShipUUID

**User Story:** As a developer, I want a service method that assigns a ship to a plan, so that ship assignment goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a SetShipUUID method accepting a plan UUID and ship UUID string.
2. WHEN SetShipUUID is called, THE DeliveryPlanService SHALL set the ShipUUID field on the plan.
3. WHEN SetShipUUID is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
4. WHEN SetShipUUID is called, THE DeliveryPlanService SHALL return the updated ReadOnlyDeliveryPlan.

### Requirement 17: DeliveryPlanService.SplitTrips

**User Story:** As a developer, I want a service method that splits a plan into multiple trip plans, so that trip splitting goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryPlanService SHALL provide a SplitTrips method accepting a plan UUID, cargo capacity, and a blueprint finder delegate.
2. WHEN SplitTrips is called, THE DeliveryPlanService SHALL create new DeliveryPlan entities for each additional trip.
3. WHEN SplitTrips is called, THE DeliveryPlanService SHALL rename the original plan with a trip number suffix.
4. WHEN SplitTrips is called, THE DeliveryPlanService SHALL add new plans to PlayerContext via AddDeliveryPlan.
5. WHEN SplitTrips is called, THE DeliveryPlanService SHALL persist via PlayerContext.WriteContext().
6. WHEN SplitTrips is called, THE DeliveryPlanService SHALL fire DeliveryDataChanged event.
7. WHEN SplitTrips is called, THE DeliveryPlanService SHALL return the list of new ReadOnlyDeliveryPlan instances.

### Requirement 18: PlayerContext.FindMutableDeliveryPlan

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable DeliveryPlan entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableDeliveryPlan internal method accepting a UUID string.
2. THE FindMutableDeliveryPlan method SHALL follow the same cache-based lookup pattern as FindMutableDeliveryRoute and FindMutableColony.
3. THE FindMutableDeliveryPlan method SHALL be marked internal so only the service project can access it.

## Phase 4: Form Migration

### Requirement 19: FormDeliveryRoute Plan Tab Migration

**User Story:** As a developer, I want the Plan tab on FormDeliveryRoute to route all plan mutations through DeliveryPlanService, so that the form never directly mutates a DeliveryPlan.

#### Acceptance Criteria

1. WHEN the user clicks New Plan, THE FormDeliveryRoute SHALL call DeliveryPlanService.Create with the route UUID and a generated name.
2. WHEN the user clicks Delete Plan, THE FormDeliveryRoute SHALL call DeliveryPlanService.Delete with the plan UUID.
3. WHEN the user changes the plan name, THE FormDeliveryRoute SHALL call DeliveryPlanService.UpdatePlan to persist the name change.
4. WHEN the user adds a drop-off item, THE FormDeliveryRoute SHALL call DeliveryPlanService.AddDropOffItem.
5. WHEN the user adds a pick-up item, THE FormDeliveryRoute SHALL call DeliveryPlanService.AddPickUpItem.
6. WHEN the user removes drop-off items, THE FormDeliveryRoute SHALL call DeliveryPlanService.RemoveDropOffItems.
7. WHEN the user removes pick-up items, THE FormDeliveryRoute SHALL call DeliveryPlanService.RemovePickUpItems.
8. WHEN the user clicks AutoFill, THE FormDeliveryRoute SHALL use the ViewModel AutoFill methods then call DeliveryPlanService.UpdatePlan to persist.

### Requirement 20: FormDeliveryExecution Migration

**User Story:** As a developer, I want FormDeliveryExecution to route all plan mutations through DeliveryPlanService, so that the form never directly mutates a DeliveryPlan.

#### Acceptance Criteria

1. WHEN the user checks a delivery item checkbox, THE FormDeliveryExecution SHALL call DeliveryPlanService.MarkItemDelivered.
2. WHEN the user clicks Complete Stop, THE FormDeliveryExecution SHALL call DeliveryPlanService.MarkStopComplete.
3. WHEN the user clicks Complete Plan, THE FormDeliveryExecution SHALL call DeliveryPlanService.MarkPlanComplete.
4. WHEN the user selects a ship, THE FormDeliveryExecution SHALL call DeliveryPlanService.SetShipUUID.
5. WHEN the user clicks Split Trips and confirms, THE FormDeliveryExecution SHALL call DeliveryPlanService.SplitTrips.
6. WHEN the user clicks Delete Plan, THE FormDeliveryExecution SHALL call DeliveryPlanService.Delete.
7. AFTER any service call, THE FormDeliveryExecution SHALL refresh its display from the returned ReadOnlyDeliveryPlan.

### Requirement 21: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the DeliveryPlan entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE DeliveryPlan entity SHALL only be mutated by DeliveryPlanService methods, JSON deserialization (loading from file), and migration code.
2. THE FormDeliveryRoute SHALL NOT directly set properties on a DeliveryPlan or its nested objects.
3. THE FormDeliveryExecution SHALL NOT directly set properties on a DeliveryPlan or its nested objects.
4. THE DeliveryPlanViewModel SHALL NOT directly set properties on a DeliveryPlan entity.

## Phase 5: Verification

### Requirement 22: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 23: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 24: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates DeliveryPlan entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct DeliveryPlan property sets (Name, ShipUUID, Completed, Stops) SHALL only find matches in DeliveryPlanService, JSON deserialization, migration code, and the DeliveryPlan class itself.
2. AFTER migration, a grep for direct DeliveryPlanStop mutation (StopCompleted, DropOff.Add, PickUp.Add) SHALL only find matches in DeliveryPlanService and the DeliveryPlanStop class itself.
3. AFTER migration, a grep for direct DeliveryItem.Delivered sets SHALL only find matches in DeliveryPlanService.

## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: Service.Create Round-Trip

FOR ALL valid name and routeUUID values, calling DeliveryPlanService.Create SHALL produce a ReadOnlyDeliveryPlan whose Name matches the input name, whose RouteUUID matches the input, and whose UUID is non-empty.

**Validates:** Requirements 8.2, 8.4, 8.5, 8.9

### Property 2: Service.UpdatePlan Round-Trip

FOR ALL valid existing DeliveryPlan entities and valid DeliveryPlanUpdateRequest values, calling Service.UpdatePlan SHALL produce a ReadOnlyDeliveryPlan whose Name matches the request Name and whose Stops match the request Stops (same count, same field values per stop including DropOff and PickUp items).

**Validates:** Requirements 10.3, 10.4, 10.7

### Property 3: Service.Delete Removes Plan

FOR ALL valid existing DeliveryPlan entities, calling Service.Delete with the plan UUID SHALL cause the plan to no longer be findable via PlayerContext.

**Validates:** Requirements 9.1, 9.2

### Property 4: Service.MarkItemDelivered Sets Flag

FOR ALL valid existing DeliveryPlan entities with at least one undelivered item, calling Service.MarkItemDelivered with delivered=true SHALL produce a ReadOnlyDeliveryPlan where the specified item has Delivered=true.

**Validates:** Requirements 13.1, 13.2

### Property 5: Service.MarkStopComplete Sets Flag

FOR ALL valid existing DeliveryPlan entities with at least one incomplete stop, calling Service.MarkStopComplete SHALL produce a ReadOnlyDeliveryPlan where the specified stop has StopCompleted=true.

**Validates:** Requirements 14.1, 14.2

### Property 6: Service.MarkPlanComplete Sets Flag

FOR ALL valid existing DeliveryPlan entities, calling Service.MarkPlanComplete SHALL produce a ReadOnlyDeliveryPlan where Completed=true.

**Validates:** Requirements 15.1, 15.2

### Property 7: AddDropOffItem Increases Item Count

FOR ALL valid existing DeliveryPlan entities, calling Service.AddDropOffItem SHALL produce a ReadOnlyDeliveryPlan where the target stop has one more DropOff item than before.

**Validates:** Requirements 11.2, 11.6

### Property 8: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct DeliveryPlan property sets SHALL only find matches in DeliveryPlanService, JSON deserialization, migration code, and the DeliveryPlan class itself.

**Validates:** Requirements 21.1, 21.2, 21.3, 21.4, 24.1, 24.2, 24.3

## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the DeliveryRouteReferenceCounter logic --- it continues to work as-is.
- Modifying the Route tab behavior --- route management was migrated in BL-112.
- Changing the AutoFill dialog (FormAutoFill) --- it remains a pure options dialog.
- Changing DeliveryFulfillment logic --- it continues to work as-is, just called from the service instead of the form.
- Changing CargoVolumeService logic --- it continues to work as-is.
- Adding unsaved changes prompts to the Plan tab --- plan operations persist immediately (no buffering).
