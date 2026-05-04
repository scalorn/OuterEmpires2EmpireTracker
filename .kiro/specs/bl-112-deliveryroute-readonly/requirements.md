# Requirements Document

## Introduction

BL-112 restructures FormDeliveryRoute so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a DeliveryRouteService that applies changes atomically. The form never directly mutates a DeliveryRoute --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), and BL-123 (pricing plans).

### Key Differences from BL-109 (Colony)

1. **Simpler entity** --- DeliveryRoute has only 1 scalar field (Name) vs Colony's 3 (PlanetName, ColonyName, SystemName). The edit buffer is correspondingly simpler.
2. **No import feature** --- No clipboard import. No Import method on the service.
3. **No background processing** --- No concurrency locks needed. No ReaderWriterLockSlim.
4. **Simpler nested structure** --- RouteStop has 6 fields vs ColonyStructure's 30+. Stop operations (add, remove, reorder) are buffered in the ViewModel edit buffer, not immediate service calls.
5. **Dual entity management** --- DeliveryPlan is a separate entity managed by the Plan tab. Plan management is OUT OF SCOPE for BL-112 and remains as-is.
6. **ReadOnly wrapper already complete** --- ReadOnlyDeliveryRoute and ReadOnlyRouteStop are fully implemented with no gaps.
7. **Stops are part of the edit buffer** --- Unlike Colony where structure operations are immediate, route stop add/remove/reorder accumulates in the ViewModel until Save. This matches how the form works: stops are built up, then the whole route is saved.

### Similarities to BL-109

1. **Single scalar field** --- Name is a simple string managed by the ViewModel edit buffer.
2. **Single form** --- One form (FormDeliveryRoute) manages the full CRUD lifecycle for routes.
3. **List view with filters** --- Route list has a text filter.
4. **Always player-scoped** --- Routes are always owned by the current player.
5. **Delete reference protection** --- DeliveryRouteReferenceCounter checks delivery plans, overflow rules, and supply chain stages before allowing deletion.
6. **Service as sole mutator** --- Same pattern: form -> ViewModel -> service -> entity.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers:
- **Route-level scalar field:** Name (the only scalar field)
- **Stops list:** RouteStop add/remove/reorder operations accumulate in the edit buffer until Save

This is because:
- Route name is edited via text box (buffered until Save)
- Stop operations (add, remove, reorder) accumulate in the edit buffer until Save
- This matches how the form currently works: stops are added/removed/reordered, then the whole route is saved at once

### Scoping Decision: Plan Management

DeliveryPlan is a separate entity. Plan CRUD (create, delete, rename) and item management (add/remove drop-off/pick-up items) remain as-is for BL-112. Plan management migration is OUT OF SCOPE and will be addressed in a separate BL item.

## Glossary

- **DeliveryRoute**: The mutable entity representing a delivery route with a Name and an ordered list of RouteStop entries.
- **RouteStop**: A nested object within DeliveryRoute representing a single stop with ColonyUUID, Sequence, DestinationType, DestinationUUID, Purpose, and FuelEstimate fields.
- **ReadOnlyDeliveryRoute**: An immutable wrapper around DeliveryRoute that exposes only getter properties and a read-only Stops list.
- **ReadOnlyRouteStop**: An immutable wrapper around RouteStop exposing all fields as read-only.
- **DeliveryRouteViewModel**: The ViewModel class that holds a local edit buffer of route Name and Stops, disconnected from the entity.
- **DeliveryRouteService**: A new service class that is the sole mutator of DeliveryRoute entities (create, update, delete).
- **FormDeliveryRoute**: The WinForms form for viewing and editing delivery routes and plans.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of Name and Stops in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether Name or Stops have been modified since the last load or save.
- **DeliveryRouteUpdateRequest**: A DTO carrying the current Name and Stops state from the ViewModel to the service for an update operation.
- **DeliveryRouteCreateRequest**: A DTO carrying Name and Stops values for creating a new route.
- **DeliveryRouteReferenceCounter**: A utility that counts how many delivery plans, warehouse overflow rules, and supply chain stages reference a given route UUID, used for delete protection.
- **DeliveryPlan**: A separate entity (not nested in DeliveryRoute) representing a delivery plan. Plan management is out of scope for BL-112.
- **Immediate_Operation**: Not applicable for BL-112 --- all route mutations (Name + Stops) are buffered in the ViewModel until Save.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the route list view to store ReadOnlyDeliveryRoute in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormDeliveryRoute populates the route list view, THE FormDeliveryRoute SHALL create ListViewItem Tags containing ReadOnlyDeliveryRoute instances obtained from PlayerContext.GetCurrentPlayerReadOnlyRoutes().
2. WHEN the user selects a route in the list view, THE FormDeliveryRoute SHALL extract the ReadOnlyDeliveryRoute from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormDeliveryRoute filters the route list, THE FormDeliveryRoute SHALL use ReadOnlyDeliveryRoute properties for the filter comparison.

### Requirement 2: PlayerContext ReadOnly Route Accessor

**User Story:** As a developer, I want PlayerContext to expose a method returning ReadOnlyDeliveryRoute wrappers, so that the form can populate the list view without accessing mutable entities.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a GetCurrentPlayerReadOnlyRoutes method returning a List<ReadOnlyDeliveryRoute>.
2. WHEN GetCurrentPlayerReadOnlyRoutes is called, THE PlayerContext SHALL return ReadOnlyDeliveryRoute wrappers for all routes owned by the current player.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable DeliveryRoute, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormDeliveryRoute SHALL NOT hold a direct reference to a mutable DeliveryRoute in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE DeliveryRouteViewModel SHALL NOT expose the mutable DeliveryRoute entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyDeliveryRoute into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a route, THE DeliveryRouteViewModel SHALL copy the Name field from the ReadOnlyDeliveryRoute into a local property.
2. WHEN the user selects a route, THE DeliveryRouteViewModel SHALL deep-copy the Stops list into a local List<RouteStop> with new RouteStop instances (preserving all 6 fields per stop).
3. THE DeliveryRouteViewModel SHALL retain the original ReadOnlyDeliveryRoute snapshot for dirty comparison.
4. THE DeliveryRouteViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
5. THE DeliveryRouteViewModel SHALL NOT hold a reference to the mutable DeliveryRoute entity.

### Requirement 5: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want the route name text box and stops grid to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormDeliveryRoute text box (txtRouteName) SHALL read from and write to the DeliveryRouteViewModel local Name field.
2. WHEN the user types in txtRouteName, THE FormDeliveryRoute SHALL update the DeliveryRouteViewModel Name local field only (no entity mutation).
3. THE FormDeliveryRoute stops grid (dgvStops) SHALL display stops from the DeliveryRouteViewModel local Stops list.
4. WHEN the user adds, removes, or reorders stops, THE FormDeliveryRoute SHALL update the DeliveryRouteViewModel local Stops list only (no entity mutation).

### Requirement 6: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the DeliveryRoute entity on every keystroke or stop operation, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE DeliveryRouteViewModel SHALL NOT write Name changes to the DeliveryRoute entity on every keystroke.
2. THE DeliveryRouteViewModel SHALL NOT mutate the DeliveryRoute entity Stops list when stops are added, removed, or reordered.
3. THE current write-through pattern (ViewModel property setters directly set entity properties, ViewModel methods directly mutate entity Stops list) SHALL be replaced with local-only state changes in the ViewModel.

### Requirement 7: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether Name or Stops have been modified since the last load or save, so that the form can detect unsaved changes.

#### Acceptance Criteria

1. THE DeliveryRouteViewModel SHALL expose an IsDirty property that returns true when the local Name or Stops differ from the original ReadOnlyDeliveryRoute snapshot.
2. THE IsDirty check SHALL compare the Name field (string equality).
3. THE IsDirty check SHALL compare the Stops list: same count, and for each index the same ColonyUUID, Sequence, DestinationType, DestinationUUID, Purpose, and FuelEstimate values.
4. WHEN the ViewModel is loaded from a ReadOnlyDeliveryRoute, THE IsDirty property SHALL return false.
5. WHEN the ViewModel represents a new unsaved route (original is null), THE IsDirty property SHALL return true once Name has a non-empty value or Stops has at least one entry.

### Requirement 8: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different route, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different route in the list view and the DeliveryRouteViewModel is dirty, THE FormDeliveryRoute SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormDeliveryRoute SHALL call DeliveryRouteService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormDeliveryRoute SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormDeliveryRoute SHALL cancel the selection change and keep the current route selected.

### Requirement 9: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the delivery route form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormDeliveryRoute (X button or MDI close) and the DeliveryRouteViewModel is dirty, THE FormDeliveryRoute SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormDeliveryRoute SHALL save and then close.
3. WHEN the user chooses Discard, THE FormDeliveryRoute SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormDeliveryRoute SHALL cancel the close and keep the form open.

### Requirement 10: Unsaved Changes Prompt on New Route

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the DeliveryRouteViewModel is dirty, THE FormDeliveryRoute SHALL prompt before clearing the form for the new route.
2. WHEN the user chooses Save, THE FormDeliveryRoute SHALL save the current route, then reset the form for a new route.
3. WHEN the user chooses Discard, THE FormDeliveryRoute SHALL discard changes and reset the form for a new route.
4. WHEN the user chooses Cancel, THE FormDeliveryRoute SHALL cancel the New operation and keep the current route.

### Requirement 11: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormDeliveryRoute has unsaved changes, THE FormDeliveryRoute OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormDeliveryRoute SHALL cancel the application exit by setting e.Cancel to true.

## Phase 3: DeliveryRouteService

### Requirement 12: DeliveryRouteService.Update

**User Story:** As a developer, I want a service method that applies route changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE DeliveryRouteService SHALL provide an Update method accepting a UUID string and a DeliveryRouteUpdateRequest.
2. WHEN Update is called, THE DeliveryRouteService SHALL look up the mutable DeliveryRoute by UUID via PlayerContext.FindMutableDeliveryRoute.
3. WHEN Update is called, THE DeliveryRouteService SHALL apply the Name field from the request to the entity.
4. WHEN Update is called, THE DeliveryRouteService SHALL replace the entity Stops list with the request Stops list (deep copy with correct Sequence numbering).
5. WHEN Update is called, THE DeliveryRouteService SHALL persist via PlayerContext.WriteContext().
6. WHEN Update is called, THE DeliveryRouteService SHALL fire DeliveryDataChanged event.
7. WHEN Update is called, THE DeliveryRouteService SHALL return the updated ReadOnlyDeliveryRoute.
8. IF the UUID is not found, THEN THE DeliveryRouteService SHALL throw an InvalidOperationException.

### Requirement 13: DeliveryRouteService.Create

**User Story:** As a developer, I want a service method that creates a new route, so that route creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE DeliveryRouteService SHALL provide a Create method accepting a DeliveryRouteCreateRequest.
2. WHEN Create is called, THE DeliveryRouteService SHALL create a new DeliveryRoute entity with a generated UUID.
3. WHEN Create is called, THE DeliveryRouteService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE DeliveryRouteService SHALL populate the Name field and Stops list from the request.
5. WHEN Create is called, THE DeliveryRouteService SHALL add the route to PlayerContext via AddDeliveryRoute.
6. WHEN Create is called, THE DeliveryRouteService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE DeliveryRouteService SHALL fire DeliveryDataChanged event.
8. WHEN Create is called, THE DeliveryRouteService SHALL return the new ReadOnlyDeliveryRoute.

### Requirement 14: DeliveryRouteService.Delete

**User Story:** As a developer, I want a service method that deletes a route, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE DeliveryRouteService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE DeliveryRouteService SHALL remove the DeliveryRoute from PlayerContext via RemoveDeliveryRoute.
3. WHEN Delete is called, THE DeliveryRouteService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE DeliveryRouteService SHALL fire DeliveryDataChanged event.
5. IF the UUID is empty or the route is not found, THEN THE DeliveryRouteService SHALL return without error.

### Requirement 15: PlayerContext.FindMutableDeliveryRoute

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable DeliveryRoute entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableDeliveryRoute internal method accepting a UUID string.
2. THE FindMutableDeliveryRoute method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableSurvey, and FindMutableColony.
3. THE FindMutableDeliveryRoute method SHALL be marked internal so only the service project can access it.

### Requirement 16: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new route (IsNew is true), THE FormDeliveryRoute SHALL call DeliveryRouteService.Create with a DeliveryRouteCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing route, THE FormDeliveryRoute SHALL call DeliveryRouteService.Update with the UUID and a DeliveryRouteUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyDeliveryRoute, THE FormDeliveryRoute SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyDeliveryRoute.
4. AFTER a successful save, THE DeliveryRouteViewModel IsDirty property SHALL return false.

### Requirement 17: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that routes in use by delivery plans, overflow rules, or supply chain stages cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormDeliveryRoute SHALL check DeliveryRouteReferenceCounter for references to the current route.
2. IF the route has references (TotalCount > 0), THEN THE FormDeliveryRoute SHALL display a warning message listing reference counts by type (plans, overflow rules, supply chain stages) and prevent deletion.
3. IF the route has no references, THE FormDeliveryRoute SHALL prompt for confirmation before calling DeliveryRouteService.Delete.
4. AFTER successful deletion, THE FormDeliveryRoute SHALL clear the form and refresh the list view.

### Requirement 18: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the DeliveryRoute entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE DeliveryRoute entity SHALL only be mutated by DeliveryRouteService methods (Update, Create, Delete), JSON deserialization (loading from file), and migration code.
2. THE FormDeliveryRoute SHALL NOT directly set properties on a DeliveryRoute.
3. THE DeliveryRouteViewModel SHALL NOT directly set properties on a DeliveryRoute.

## Phase 4: Verification

### Requirement 19: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 20: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 21: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates DeliveryRoute entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct DeliveryRoute.Name property sets SHALL only find matches in DeliveryRouteService, JSON deserialization, migration code, and the DeliveryRoute class itself.
2. AFTER migration, a grep for direct DeliveryRoute.Stops list mutation (Add, Remove, Clear, index assignment) SHALL only find matches in DeliveryRouteService, JSON deserialization, and the DeliveryRoute class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid DeliveryRoute entities, wrapping in ReadOnlyDeliveryRoute and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, and every RouteStop in Stops with matching ColonyUUID, Sequence, DestinationType, DestinationUUID, Purpose, FuelEstimate).

**Validates:** Requirements 4.1, 4.2, 4.3

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid DeliveryRoute entities, wrapping in ReadOnlyDeliveryRoute and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 7.1, 7.4

### Property 3: IsDirty Detects Name Change

FOR ALL valid DeliveryRoute entities, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 7.1, 7.2

### Property 4: IsDirty Detects Stops Change

FOR ALL valid DeliveryRoute entities with at least one stop, after LoadFrom, adding a stop, removing a stop, or reordering stops SHALL cause IsDirty to return true.

**Validates:** Requirements 7.1, 7.3

### Property 5: Service.Update Round-Trip

FOR ALL valid existing DeliveryRoute entities and valid DeliveryRouteUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyDeliveryRoute whose Name matches the request Name and whose Stops match the request Stops (same count, same field values per stop).

**Validates:** Requirements 12.3, 12.4, 12.7

### Property 6: Service.Create Round-Trip

FOR ALL valid DeliveryRouteCreateRequest values, calling Service.Create SHALL produce a ReadOnlyDeliveryRoute whose Name matches the request Name, whose Stops match the request Stops, and whose UUID is non-empty.

**Validates:** Requirements 13.2, 13.4, 13.8

### Property 7: Service.Delete Removes Route

FOR ALL valid existing DeliveryRoute entities, calling Service.Delete with the route UUID SHALL cause the route to no longer be findable via PlayerContext.

**Validates:** Requirements 14.1, 14.2

### Property 8: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct DeliveryRoute property sets SHALL only find matches in DeliveryRouteService, JSON deserialization, migration code, and the DeliveryRoute class itself.

**Validates:** Requirements 18.1, 18.2, 18.3, 21.1


## Out of Scope

- DeliveryPlan management (create, delete, rename, item add/remove) --- separate BL item.
- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the DeliveryRouteReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- Modifying the Plan tab behavior or DeliveryPlanViewModel --- plan management remains as-is.
- RouteDropdownHelper migration --- other forms that use RouteDropdownHelper with mutable routes are out of scope for BL-112.
- FuelEstimate calculation logic --- remains unchanged, just stored in the edit buffer stops.
