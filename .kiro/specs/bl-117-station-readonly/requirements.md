# Requirements Document

## Introduction

BL-117 restructures FormStation so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a StationService that applies changes atomically. The form never directly mutates a Station --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), BL-115 (ship templates), BL-116 (ships), and BL-123 (pricing plans).

### Key Differences from BL-116 (Ship)

1. **Holds instead of Cargo** --- Station has a Dictionary of ItemBag Holds (keyed by player UUID) instead of Ship's single Cargo ItemBag.
2. **MunitionsHold instead of Hopper** --- Station has a single MunitionsHold ItemBag instead of Ship's Hopper.
3. **StationType and Ownership** --- Station has StationType and Ownership enum fields that Ship does not have.
4. **StationBlueprintUUID** --- Station uses StationBlueprintUUID instead of Ship's HullBlueprintUUID and TemplateUUID.
5. **No LocationType/LocationUUID** --- Station does not have location fields.
6. **No TemplateUUID** --- Station does not have a template reference.
7. **No Create from Template** --- Station does not support creation from a template.
8. **StationDataChanged event** --- Fires OnStationDataChanged instead of OnShipDataChanged.
9. **StationReferenceCounter** --- Checks 10 source types vs Ship's 2 source types.
10. **Ownership-gated tabs** --- Components and Munitions tabs are only enabled for PlayerOwned stations.
11. **Hold is player-scoped** --- The hold displayed is the current player's hold within the station's Holds dictionary.
### Similarities to BL-116

1. **Components list** --- Both have List of ShipComponentSlot managed by the ViewModel edit buffer.
2. **Hull HP fields** --- Both have HullCurrentHP, HullMaxHP, HullMaxRepairPercent.
3. **Single form** --- FormStation manages the full CRUD lifecycle.
4. **List view with filters** --- Station list has a text filter.
5. **Service as sole mutator** --- form -> ViewModel -> service -> entity.
6. **ReadOnly wrappers already complete** --- ReadOnlyStation, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are fully implemented.
7. **Delete reference protection** --- StationReferenceCounter for delete protection.
8. **No write locks** --- No ReaderWriterLockSlim on Station. The service mutates directly.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers:
- **Station-level scalar fields:** Name, StationType, Ownership, OwnerUUID, StationBlueprintUUID
- **Hull HP fields:** HullCurrentHP, HullMaxHP, HullMaxRepairPercent
- **Components list:** ShipComponentSlot add/remove/change operations accumulate in the edit buffer until Save
- **Hold:** Item add/remove operations for the current player's hold accumulate in the edit buffer until Save
- **MunitionsHold:** Item add/remove operations accumulate in the edit buffer until Save
This is because:
- Station name is edited via text box (buffered until Save)
- Station type and ownership are via combo boxes (buffered until Save)
- Station blueprint is via combo box (buffered until Save)
- Component slot changes are via grid combo cells (buffered until Save)
- Hold item add/remove are via buttons (buffered until Save)
- Munitions item add/remove are via buttons (buffered until Save)
- Hull HP and repair percent are edited via grid cells (buffered until Save)
- This matches the target pattern: all changes accumulate locally, then save atomically

## Glossary

- **Station**: The mutable entity representing a station with UUID, Name, OwnerUUID, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Holds, and MunitionsHold.
- **ShipComponentSlot**: A nested object within Station representing a single component slot with SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent fields.
- **Item**: An item in Holds or MunitionsHold with UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, and optional Contents (nested ItemBag for crates).
- **ItemBag**: A dictionary-based container of Items keyed by UUID, with add/remove/query methods.
- **ReadOnlyStation**: An immutable wrapper around Station that exposes only getter properties and read-only collections.
- **ReadOnlyShipComponentSlot**: An immutable wrapper around ShipComponentSlot exposing all fields as read-only.
- **ReadOnlyItem**: An immutable wrapper around Item exposing all fields as read-only.
- **ReadOnlyItemBag**: An immutable wrapper around ItemBag exposing only query methods.- **StationViewModel**: The new ViewModel class that holds a local edit buffer of all station fields, disconnected from the entity.
- **StationService**: A new service class that is the sole mutator of Station entities (create, update, delete).
- **FormStation**: The WinForms form for viewing and editing stations.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of all station fields in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any field has been modified since the last load or save.
- **StationUpdateRequest**: A DTO carrying the current state from the ViewModel to the service for an update operation.
- **StationCreateRequest**: A DTO carrying field values for creating a new blank station.
- **StationReferenceCounter**: A utility that counts how many entities reference a given station UUID, used for delete protection.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the station list view to store ReadOnlyStation in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormStation populates the station list view, THE FormStation SHALL create ListViewItem Tags containing ReadOnlyStation instances obtained from PlayerContext.GetCurrentPlayerReadOnlyStations().
2. WHEN the user selects a station in the list view, THE FormStation SHALL extract the ReadOnlyStation from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormStation filters the station list, THE FormStation SHALL use ReadOnlyStation properties for the filter comparison.
### Requirement 2: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable Station, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormStation SHALL NOT hold a direct reference to a mutable Station in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE StationViewModel SHALL NOT expose the mutable Station entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 3: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyStation into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a station, THE StationViewModel SHALL copy the Name field from the ReadOnlyStation into a local property.
2. WHEN the user selects a station, THE StationViewModel SHALL copy the StationType field from the ReadOnlyStation into a local property.
3. WHEN the user selects a station, THE StationViewModel SHALL copy the Ownership field from the ReadOnlyStation into a local property.
4. WHEN the user selects a station, THE StationViewModel SHALL copy the StationBlueprintUUID field from the ReadOnlyStation into a local property.
5. WHEN the user selects a station, THE StationViewModel SHALL copy the HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields from the ReadOnlyStation into local properties.
6. WHEN the user selects a station, THE StationViewModel SHALL deep-copy the Components list into a local List of ShipComponentSlot with new ShipComponentSlot instances (preserving all 6 fields per slot: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).
7. WHEN the user selects a station, THE StationViewModel SHALL deep-copy the current player's Hold ItemBag into a local ItemBag with new Item instances.
8. WHEN the user selects a station, THE StationViewModel SHALL deep-copy the MunitionsHold ItemBag into a local ItemBag with new Item instances.
9. THE StationViewModel SHALL retain the original ReadOnlyStation snapshot for dirty comparison.
10. THE StationViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
11. THE StationViewModel SHALL NOT hold a reference to the mutable Station entity.
### Requirement 4: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want the station name text box, type combo, ownership combo, blueprint combo, component grid, hold grid, and munitions grid to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormStation text box (txtName) SHALL read from and write to the StationViewModel local Name field.
2. WHEN the user types in txtName, THE FormStation SHALL update the StationViewModel Name local field only (no entity mutation).
3. THE FormStation station type combo (cmbStationType) SHALL read from and write to the StationViewModel local StationType field.
4. THE FormStation ownership combo (cmbOwnership) SHALL read from and write to the StationViewModel local Ownership field.
5. THE FormStation blueprint combo (cmbStationBlueprint) SHALL read from and write to the StationViewModel local StationBlueprintUUID field.
6. THE FormStation component grid (dgvComponents) SHALL display components from the StationViewModel local Components list.
7. WHEN the user changes a component slot, THE FormStation SHALL update the StationViewModel local Components list only (no entity mutation).
8. THE FormStation hold grid (dgvHold) SHALL display items from the StationViewModel local Hold.
9. WHEN the user adds or removes a hold item, THE FormStation SHALL update the StationViewModel local Hold only (no entity mutation).
10. THE FormStation munitions grid (dgvMunitions) SHALL display items from the StationViewModel local MunitionsHold.
11. WHEN the user adds or removes a munitions item, THE FormStation SHALL update the StationViewModel local MunitionsHold only (no entity mutation).
12. WHEN the user edits hull HP or repair percent in the component grid, THE FormStation SHALL update the StationViewModel local HullCurrentHP or HullMaxRepairPercent only (no entity mutation).

### Requirement 5: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the Station entity on every keystroke or control change, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE StationViewModel SHALL NOT write Name changes to the Station entity on every keystroke.
2. THE StationViewModel SHALL NOT write StationType changes to the Station entity on combo selection.
3. THE StationViewModel SHALL NOT write Ownership changes to the Station entity on combo selection.
4. THE StationViewModel SHALL NOT write StationBlueprintUUID changes to the Station entity on combo selection.
5. THE StationViewModel SHALL NOT mutate the Station entity Components list when components are changed.
6. THE StationViewModel SHALL NOT mutate the Station entity Holds when items are added or removed.
7. THE StationViewModel SHALL NOT mutate the Station entity MunitionsHold when items are added or removed.
8. THE StationViewModel SHALL NOT write HullCurrentHP or HullMaxRepairPercent changes to the Station entity on grid cell edit.
9. THE current write-through pattern (form directly sets entity properties on control change) SHALL be replaced with local-only state changes in the ViewModel.
### Requirement 6: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether any field has been modified since the last load or save, so that the form can detect unsaved changes.

#### Acceptance Criteria

1. THE StationViewModel SHALL expose an IsDirty property that returns true when any local field differs from the original ReadOnlyStation snapshot.
2. THE IsDirty check SHALL compare the Name field (string equality).
3. THE IsDirty check SHALL compare the StationType field (enum equality).
4. THE IsDirty check SHALL compare the Ownership field (enum equality).
5. THE IsDirty check SHALL compare the StationBlueprintUUID field (string equality).
6. THE IsDirty check SHALL compare the HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields.
7. THE IsDirty check SHALL compare the Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values.
8. THE IsDirty check SHALL compare the Hold ItemBag: same item count, and for each item the same UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent.
9. THE IsDirty check SHALL compare the MunitionsHold ItemBag: same item count, and for each item the same UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent.
10. WHEN the ViewModel is loaded from a ReadOnlyStation, THE IsDirty property SHALL return false.
11. WHEN the ViewModel represents a new unsaved station (original is null), THE IsDirty property SHALL return true once Name has a non-empty value.

### Requirement 7: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different station, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different station in the list view and the StationViewModel is dirty, THE FormStation SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormStation SHALL call StationService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormStation SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormStation SHALL cancel the selection change and keep the current station selected.
### Requirement 8: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the station form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormStation (X button or MDI close) and the StationViewModel is dirty, THE FormStation SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormStation SHALL save and then close.
3. WHEN the user chooses Discard, THE FormStation SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormStation SHALL cancel the close and keep the form open.

### Requirement 9: Unsaved Changes Prompt on New Station

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the StationViewModel is dirty, THE FormStation SHALL prompt before clearing the form for the new station.
2. WHEN the user chooses Save, THE FormStation SHALL save the current station, then reset the form for a new station.
3. WHEN the user chooses Discard, THE FormStation SHALL discard changes and reset the form for a new station.
4. WHEN the user chooses Cancel, THE FormStation SHALL cancel the New operation and keep the current station.

### Requirement 10: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormStation has unsaved changes, THE FormStation OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormStation SHALL cancel the application exit by setting e.Cancel to true.
## Phase 3: StationService

### Requirement 11: StationService.Update

**User Story:** As a developer, I want a service method that applies station changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE StationService SHALL provide an Update method accepting a UUID string and a StationUpdateRequest.
2. WHEN Update is called, THE StationService SHALL look up the mutable Station by UUID via PlayerContext.FindMutableStation.
3. WHEN Update is called, THE StationService SHALL apply the Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields from the request to the entity.
4. WHEN Update is called, THE StationService SHALL replace the entity Components list with the request Components list (deep copy).
5. WHEN Update is called, THE StationService SHALL replace the current player's Hold in the entity Holds dictionary with the request Hold (deep copy).
6. WHEN Update is called, THE StationService SHALL replace the entity MunitionsHold with the request MunitionsHold (deep copy).
7. WHEN Update is called, THE StationService SHALL persist via PlayerContext.WriteContext().
8. WHEN Update is called, THE StationService SHALL fire StationDataChanged event.
9. WHEN Update is called, THE StationService SHALL return the updated ReadOnlyStation.
10. IF the UUID is not found, THEN THE StationService SHALL throw an InvalidOperationException.

### Requirement 12: StationService.Create

**User Story:** As a developer, I want a service method that creates a new blank station, so that station creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE StationService SHALL provide a Create method accepting a StationCreateRequest.
2. WHEN Create is called, THE StationService SHALL create a new Station entity with a generated UUID.
3. WHEN Create is called, THE StationService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE StationService SHALL populate the Name field from the request (defaulting to 'New Station' if empty).
5. WHEN Create is called, THE StationService SHALL add the station to PlayerContext via AddStation.
6. WHEN Create is called, THE StationService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE StationService SHALL fire StationDataChanged event.
8. WHEN Create is called, THE StationService SHALL return the new ReadOnlyStation.
### Requirement 13: StationService.Delete

**User Story:** As a developer, I want a service method that deletes a station, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE StationService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE StationService SHALL remove the Station from PlayerContext via RemoveStation.
3. WHEN Delete is called, THE StationService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE StationService SHALL fire StationDataChanged event.
5. IF the UUID is empty or the station is not found, THEN THE StationService SHALL return without error.

### Requirement 14: PlayerContext.FindMutableStation

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable Station entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableStation internal method accepting a UUID string.
2. THE FindMutableStation method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute.
3. THE FindMutableStation method SHALL be marked internal so only the service project can access it.

### Requirement 15: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new station (IsNew is true), THE FormStation SHALL call StationService.Create with a StationCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing station, THE FormStation SHALL call StationService.Update with the UUID and a StationUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyStation, THE FormStation SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyStation.
4. AFTER a successful save, THE StationViewModel IsDirty property SHALL return false.

### Requirement 16: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that stations in use cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormStation SHALL check StationReferenceCounter for references to the current station.
2. IF the station has references (CountReferences > 0), THEN THE FormStation SHALL display a warning message listing the reference count and prevent deletion.
3. IF the station has no references, THE FormStation SHALL prompt for confirmation before calling StationService.Delete.
4. AFTER successful deletion, THE FormStation SHALL clear the form and refresh the list view.
### Requirement 17: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the Station entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE Station entity SHALL only be mutated by StationService methods (Update, Create, Delete), JSON deserialization (loading from file), and migration code.
2. THE FormStation SHALL NOT directly set properties on a Station.
3. THE StationViewModel SHALL NOT directly set properties on a Station.

## Phase 4: Verification

### Requirement 18: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 19: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 20: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates Station entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct Station property sets (Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent) SHALL only find matches in StationService, JSON deserialization, migration code, and the Station class itself.
2. AFTER migration, a grep for direct Station.Components list mutation (Add, Remove, Clear, index assignment) SHALL only find matches in StationService, JSON deserialization, and the Station class itself.
3. AFTER migration, a grep for direct Station.Holds and Station.MunitionsHold mutation (AddItem, Remove, Clear) SHALL only find matches in StationService, JSON deserialization, and the Station class itself.

## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid Station entities, wrapping in ReadOnlyStation and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent, and every Item in Hold and MunitionsHold with matching UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent).

**Validates:** Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid Station entities, wrapping in ReadOnlyStation and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 6.1, 6.10

### Property 3: IsDirty Detects Name Change

FOR ALL valid Station entities, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.2
### Property 4: IsDirty Detects StationBlueprintUUID Change

FOR ALL valid Station entities, after LoadFrom, changing the StationBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.5

### Property 5: IsDirty Detects Components Change

FOR ALL valid Station entities with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.7

### Property 6: IsDirty Detects StationType Change

FOR ALL valid Station entities, after LoadFrom, changing the StationType field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.3

### Property 7: IsDirty Detects Hold Change

FOR ALL valid Station entities with at least one hold item, after LoadFrom, adding or removing a hold item SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.8

### Property 8: Service.Update Round-Trip

FOR ALL valid existing Station entities and valid StationUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyStation whose scalar fields match the request and whose Components, Hold, and MunitionsHold match the request collections.

**Validates:** Requirements 11.3, 11.4, 11.5, 11.6, 11.9

### Property 9: Service.Create Round-Trip

FOR ALL valid StationCreateRequest values, calling Service.Create SHALL produce a ReadOnlyStation whose Name matches the request and whose UUID is non-empty.

**Validates:** Requirements 12.2, 12.4, 12.8

### Property 10: Service.Delete Removes Station

FOR ALL valid existing Station entities, calling Service.Delete with the station UUID SHALL cause the station to no longer be findable via PlayerContext.

**Validates:** Requirements 13.1, 13.2

### Property 11: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Station property sets SHALL only find matches in StationService, JSON deserialization, migration code, and the Station class itself.

**Validates:** Requirements 17.1, 17.2, 17.3, 20.1, 20.2, 20.3

## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the StationReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- Ship (FormShipInstance) migration --- separate BL-116 item.
- BuildPlan mutation --- BuildPlan migration is a separate BL item.
- Crate contents editing --- crate contents are displayed read-only in the hold grid. Editing crate contents is not in scope.
- Editing holds for players other than the current player --- only the current player's hold is editable.