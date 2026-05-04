# Requirements Document

## Introduction

BL-116 restructures FormShipInstance so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a ShipService that applies changes atomically. The form never directly mutates a Ship --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), BL-115 (ship templates), and BL-123 (pricing plans).

### Key Differences from BL-115 (ShipTemplate)

1. **More complex entity** --- Ship has 5 scalar fields (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID) plus hull HP fields (HullCurrentHP, HullMaxHP, HullMaxRepairPercent) vs ShipTemplate's 2 (Name, HullBlueprintUUID). The edit buffer covers all scalars, hull HP, Components, Cargo, and Hopper.
2. **Cargo and Hopper** --- Ship has two ItemBag collections (Cargo and Hopper) that ShipTemplate does not have. The ViewModel must buffer item add/remove operations.
3. **Location fields** --- Ship has LocationType (enum) and LocationUUID (string) that ShipTemplate does not have.
4. **Hull HP fields** --- Ship has HullCurrentHP, HullMaxHP, and HullMaxRepairPercent that ShipTemplate does not have.
5. **Create from Template** --- FormShipInstance has a Create from Template button that creates a Ship from a ShipTemplate. This is a service-level operation.
6. **No Order Build** --- Unlike FormShipTemplate, FormShipInstance does not have an Order Build feature.
7. **ShipDataChanged event** --- Fires OnShipDataChanged instead of OnShipTemplateDataChanged.
8. **ShipReferenceCounter** --- Checks delivery plans and build items (2 source types) vs ShipTemplateReferenceCounter's 3 source types.
9. **No write locks** --- No ReaderWriterLockSlim on Ship. The service mutates directly.

### Similarities to BL-115

1. **Scalar fields** --- Name and HullBlueprintUUID managed by the ViewModel edit buffer.
2. **Single form** --- FormShipInstance manages the full CRUD lifecycle.
3. **List view with filters** --- Ship list has a text filter.
4. **Always player-scoped** --- Ships owned by the current player.
5. **Delete reference protection** --- ShipReferenceCounter checks delivery plans and build items.
6. **Service as sole mutator** --- form -> ViewModel -> service -> entity.
7. **Components are part of the edit buffer** --- Component add/remove/change accumulates in the ViewModel until Save.
8. **ReadOnly wrappers already complete** --- ReadOnlyShip, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are fully implemented.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers:
- **Ship-level scalar fields:** Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID
- **Hull HP fields:** HullCurrentHP, HullMaxHP, HullMaxRepairPercent
- **Components list:** ShipComponentSlot add/remove/change operations accumulate in the edit buffer until Save
- **Cargo and Hopper:** Item add/remove operations accumulate in the edit buffer until Save

This is because:
- Ship name is edited via text box (buffered until Save)
- Hull selection is via combo box (buffered until Save)
- Location type and UUID are via combo boxes (buffered until Save)
- Component slot changes are via grid combo cells (buffered until Save)
- Cargo/hopper item add/remove are via buttons (buffered until Save)
- Hull HP and repair percent are edited via grid cells (buffered until Save)
- This matches the target pattern: all changes accumulate locally, then save atomically

### Scoping Decision: Create from Template

Create from Template reads from a ShipTemplate to populate a new Ship. After migration, this becomes a ShipService.CreateFromTemplate method that takes a template UUID, creates a new Ship entity with fields copied from the template, and returns a ReadOnlyShip.

## Glossary

- **Ship**: The mutable entity representing a ship instance with UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Cargo, and Hopper.
- **ShipComponentSlot**: A nested object within Ship representing a single component slot with SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent fields.
- **Item**: An item in Cargo or Hopper with UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, and optional Contents (nested ItemBag for crates).
- **ItemBag**: A dictionary-based container of Items keyed by UUID, with add/remove/query methods.
- **ReadOnlyShip**: An immutable wrapper around Ship that exposes only getter properties and read-only collections.
- **ReadOnlyShipComponentSlot**: An immutable wrapper around ShipComponentSlot exposing all fields as read-only.
- **ReadOnlyItem**: An immutable wrapper around Item exposing all fields as read-only.
- **ReadOnlyItemBag**: An immutable wrapper around ItemBag exposing only query methods.
- **ShipViewModel**: The new ViewModel class that holds a local edit buffer of all ship fields, disconnected from the entity.
- **ShipService**: A new service class that is the sole mutator of Ship entities (create, update, delete, create from template).
- **FormShipInstance**: The WinForms form for viewing and editing ship instances.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of all ship fields in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any field has been modified since the last load or save.
- **ShipUpdateRequest**: A DTO carrying the current state from the ViewModel to the service for an update operation.
- **ShipCreateRequest**: A DTO carrying field values for creating a new blank ship.
- **ShipReferenceCounter**: A utility that counts how many delivery plans and build items reference a given ship UUID, used for delete protection.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the ship list view to store ReadOnlyShip in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormShipInstance populates the ship list view, THE FormShipInstance SHALL create ListViewItem Tags containing ReadOnlyShip instances obtained from PlayerContext.GetCurrentPlayerReadOnlyShips().
2. WHEN the user selects a ship in the list view, THE FormShipInstance SHALL extract the ReadOnlyShip from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormShipInstance filters the ship list, THE FormShipInstance SHALL use ReadOnlyShip properties for the filter comparison.

### Requirement 2: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable Ship, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormShipInstance SHALL NOT hold a direct reference to a mutable Ship in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE ShipViewModel SHALL NOT expose the mutable Ship entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 3: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyShip into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a ship, THE ShipViewModel SHALL copy the Name field from the ReadOnlyShip into a local property.
2. WHEN the user selects a ship, THE ShipViewModel SHALL copy the TemplateUUID field from the ReadOnlyShip into a local property.
3. WHEN the user selects a ship, THE ShipViewModel SHALL copy the HullBlueprintUUID field from the ReadOnlyShip into a local property.
4. WHEN the user selects a ship, THE ShipViewModel SHALL copy the LocationType field from the ReadOnlyShip into a local property.
5. WHEN the user selects a ship, THE ShipViewModel SHALL copy the LocationUUID field from the ReadOnlyShip into a local property.
6. WHEN the user selects a ship, THE ShipViewModel SHALL copy the HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields from the ReadOnlyShip into local properties.
7. WHEN the user selects a ship, THE ShipViewModel SHALL deep-copy the Components list into a local List<ShipComponentSlot> with new ShipComponentSlot instances (preserving all 6 fields per slot: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).
8. WHEN the user selects a ship, THE ShipViewModel SHALL deep-copy the Cargo ItemBag into a local ItemBag with new Item instances.
9. WHEN the user selects a ship, THE ShipViewModel SHALL deep-copy the Hopper ItemBag into a local ItemBag with new Item instances.
10. THE ShipViewModel SHALL retain the original ReadOnlyShip snapshot for dirty comparison.
11. THE ShipViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
12. THE ShipViewModel SHALL NOT hold a reference to the mutable Ship entity.

### Requirement 4: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want the ship name text box, hull combo, location combos, component grid, and cargo grid to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormShipInstance text box (txtName) SHALL read from and write to the ShipViewModel local Name field.
2. WHEN the user types in txtName, THE FormShipInstance SHALL update the ShipViewModel Name local field only (no entity mutation).
3. THE FormShipInstance hull combo (cmbHull) SHALL read from and write to the ShipViewModel local HullBlueprintUUID field.
4. WHEN the user selects a hull, THE FormShipInstance SHALL update the ShipViewModel HullBlueprintUUID local field and clear the local Components list (no entity mutation).
5. THE FormShipInstance location type combo (cmbLocationType) SHALL read from and write to the ShipViewModel local LocationType field.
6. THE FormShipInstance location UUID combo (cmbLocationUUID) SHALL read from and write to the ShipViewModel local LocationUUID field.
7. THE FormShipInstance component grid (dgvComponents) SHALL display components from the ShipViewModel local Components list.
8. WHEN the user changes a component slot, THE FormShipInstance SHALL update the ShipViewModel local Components list only (no entity mutation).
9. THE FormShipInstance cargo grid (dgvCargo) SHALL display items from the ShipViewModel local Cargo or Hopper.
10. WHEN the user adds or removes a cargo/hopper item, THE FormShipInstance SHALL update the ShipViewModel local Cargo or Hopper only (no entity mutation).
11. WHEN the user edits hull HP or repair percent in the component grid, THE FormShipInstance SHALL update the ShipViewModel local HullCurrentHP or HullMaxRepairPercent only (no entity mutation).

### Requirement 5: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the Ship entity on every keystroke or control change, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE ShipViewModel SHALL NOT write Name changes to the Ship entity on every keystroke.
2. THE ShipViewModel SHALL NOT write HullBlueprintUUID changes to the Ship entity on hull selection.
3. THE ShipViewModel SHALL NOT write LocationType or LocationUUID changes to the Ship entity on combo selection.
4. THE ShipViewModel SHALL NOT mutate the Ship entity Components list when components are added, removed, or changed.
5. THE ShipViewModel SHALL NOT mutate the Ship entity Cargo or Hopper when items are added or removed.
6. THE ShipViewModel SHALL NOT write HullCurrentHP or HullMaxRepairPercent changes to the Ship entity on grid cell edit.
7. THE current write-through pattern (form directly sets entity properties on control change) SHALL be replaced with local-only state changes in the ViewModel.

### Requirement 6: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether any field has been modified since the last load or save, so that the form can detect unsaved changes.

#### Acceptance Criteria

1. THE ShipViewModel SHALL expose an IsDirty property that returns true when any local field differs from the original ReadOnlyShip snapshot.
2. THE IsDirty check SHALL compare the Name field (string equality).
3. THE IsDirty check SHALL compare the TemplateUUID field (string equality).
4. THE IsDirty check SHALL compare the HullBlueprintUUID field (string equality).
5. THE IsDirty check SHALL compare the LocationType field (enum equality).
6. THE IsDirty check SHALL compare the LocationUUID field (string equality).
7. THE IsDirty check SHALL compare the HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields.
8. THE IsDirty check SHALL compare the Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values.
9. THE IsDirty check SHALL compare the Cargo ItemBag: same item count, and for each item the same UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent.
10. THE IsDirty check SHALL compare the Hopper ItemBag: same item count, and for each item the same UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent.
11. WHEN the ViewModel is loaded from a ReadOnlyShip, THE IsDirty property SHALL return false.
12. WHEN the ViewModel represents a new unsaved ship (original is null), THE IsDirty property SHALL return true once Name has a non-empty value.

### Requirement 7: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different ship, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different ship in the list view and the ShipViewModel is dirty, THE FormShipInstance SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormShipInstance SHALL call ShipService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormShipInstance SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormShipInstance SHALL cancel the selection change and keep the current ship selected.

### Requirement 8: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the ship form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormShipInstance (X button or MDI close) and the ShipViewModel is dirty, THE FormShipInstance SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormShipInstance SHALL save and then close.
3. WHEN the user chooses Discard, THE FormShipInstance SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormShipInstance SHALL cancel the close and keep the form open.

### Requirement 9: Unsaved Changes Prompt on New Ship

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the ShipViewModel is dirty, THE FormShipInstance SHALL prompt before clearing the form for the new ship.
2. WHEN the user chooses Save, THE FormShipInstance SHALL save the current ship, then reset the form for a new ship.
3. WHEN the user chooses Discard, THE FormShipInstance SHALL discard changes and reset the form for a new ship.
4. WHEN the user chooses Cancel, THE FormShipInstance SHALL cancel the New operation and keep the current ship.

### Requirement 10: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormShipInstance has unsaved changes, THE FormShipInstance OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormShipInstance SHALL cancel the application exit by setting e.Cancel to true.

## Phase 3: ShipService

### Requirement 11: ShipService.Update

**User Story:** As a developer, I want a service method that applies ship changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE ShipService SHALL provide an Update method accepting a UUID string and a ShipUpdateRequest.
2. WHEN Update is called, THE ShipService SHALL look up the mutable Ship by UUID via PlayerContext.FindMutableShip.
3. WHEN Update is called, THE ShipService SHALL apply the Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, and HullMaxRepairPercent fields from the request to the entity.
4. WHEN Update is called, THE ShipService SHALL replace the entity Components list with the request Components list (deep copy).
5. WHEN Update is called, THE ShipService SHALL replace the entity Cargo ItemBag with the request Cargo (deep copy).
6. WHEN Update is called, THE ShipService SHALL replace the entity Hopper ItemBag with the request Hopper (deep copy).
7. WHEN Update is called, THE ShipService SHALL persist via PlayerContext.WriteContext().
8. WHEN Update is called, THE ShipService SHALL fire ShipDataChanged event.
9. WHEN Update is called, THE ShipService SHALL return the updated ReadOnlyShip.
10. IF the UUID is not found, THEN THE ShipService SHALL throw an InvalidOperationException.

### Requirement 12: ShipService.Create

**User Story:** As a developer, I want a service method that creates a new blank ship, so that ship creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE ShipService SHALL provide a Create method accepting a ShipCreateRequest.
2. WHEN Create is called, THE ShipService SHALL create a new Ship entity with a generated UUID.
3. WHEN Create is called, THE ShipService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE ShipService SHALL populate the Name field from the request (defaulting to 'New Ship' if empty).
5. WHEN Create is called, THE ShipService SHALL add the ship to PlayerContext via AddShip.
6. WHEN Create is called, THE ShipService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE ShipService SHALL fire ShipDataChanged event.
8. WHEN Create is called, THE ShipService SHALL return the new ReadOnlyShip.

### Requirement 13: ShipService.Delete

**User Story:** As a developer, I want a service method that deletes a ship, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE ShipService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE ShipService SHALL remove the Ship from PlayerContext via RemoveShip.
3. WHEN Delete is called, THE ShipService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE ShipService SHALL fire ShipDataChanged event.
5. IF the UUID is empty or the ship is not found, THEN THE ShipService SHALL return without error.

### Requirement 14: ShipService.CreateFromTemplate

**User Story:** As a developer, I want a service method that creates a ship from a template, so that template-based creation goes through the controlled gate.

#### Acceptance Criteria

1. THE ShipService SHALL provide a CreateFromTemplate method accepting a template UUID string.
2. WHEN CreateFromTemplate is called, THE ShipService SHALL look up the ShipTemplate by UUID via PlayerContext.FindShipTemplate.
3. WHEN CreateFromTemplate is called, THE ShipService SHALL create a new Ship entity with a generated UUID, OwnerUUID from current player, Name from template, TemplateUUID from template UUID, HullBlueprintUUID from template, and Components deep-copied from template.
4. WHEN CreateFromTemplate is called, THE ShipService SHALL add the ship to PlayerContext, persist, and fire ShipDataChanged event.
5. WHEN CreateFromTemplate is called, THE ShipService SHALL return the new ReadOnlyShip.
6. IF the template UUID is not found, THEN THE ShipService SHALL throw an InvalidOperationException.

### Requirement 15: PlayerContext.FindMutableShip

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable Ship entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableShip internal method accepting a UUID string.
2. THE FindMutableShip method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute.
3. THE FindMutableShip method SHALL be marked internal so only the service project can access it.

### Requirement 16: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new ship (IsNew is true), THE FormShipInstance SHALL call ShipService.Create with a ShipCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing ship, THE FormShipInstance SHALL call ShipService.Update with the UUID and a ShipUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyShip, THE FormShipInstance SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyShip.
4. AFTER a successful save, THE ShipViewModel IsDirty property SHALL return false.

### Requirement 17: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that ships in use by delivery plans or build items cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormShipInstance SHALL check ShipReferenceCounter for references to the current ship.
2. IF the ship has references (CountReferences > 0), THEN THE FormShipInstance SHALL display a warning message listing the reference count and prevent deletion.
3. IF the ship has no references, THE FormShipInstance SHALL prompt for confirmation before calling ShipService.Delete.
4. AFTER successful deletion, THE FormShipInstance SHALL clear the form and refresh the list view.

### Requirement 18: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the Ship entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE Ship entity SHALL only be mutated by ShipService methods (Update, Create, Delete, CreateFromTemplate), JSON deserialization (loading from file), and migration code.
2. THE FormShipInstance SHALL NOT directly set properties on a Ship.
3. THE ShipViewModel SHALL NOT directly set properties on a Ship.

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

**User Story:** As a developer, I want to verify that no code outside the service directly mutates Ship entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct Ship property sets (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent) SHALL only find matches in ShipService, JSON deserialization, migration code, and the Ship class itself.
2. AFTER migration, a grep for direct Ship.Components list mutation (Add, Remove, Clear, index assignment) SHALL only find matches in ShipService, JSON deserialization, and the Ship class itself.
3. AFTER migration, a grep for direct Ship.Cargo and Ship.Hopper mutation (AddItem, Remove, Clear) SHALL only find matches in ShipService, JSON deserialization, and the Ship class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid Ship entities, wrapping in ReadOnlyShip and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent, and every Item in Cargo and Hopper with matching UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent).

**Validates:** Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid Ship entities, wrapping in ReadOnlyShip and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 6.1, 6.11

### Property 3: IsDirty Detects Name Change

FOR ALL valid Ship entities, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.2

### Property 4: IsDirty Detects HullBlueprintUUID Change

FOR ALL valid Ship entities, after LoadFrom, changing the HullBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.4

### Property 5: IsDirty Detects Components Change

FOR ALL valid Ship entities with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.8

### Property 6: IsDirty Detects Location Change

FOR ALL valid Ship entities, after LoadFrom, changing the LocationType or LocationUUID field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.5, 6.6

### Property 7: IsDirty Detects Cargo Change

FOR ALL valid Ship entities with at least one cargo item, after LoadFrom, adding or removing a cargo item SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.9

### Property 8: Service.Update Round-Trip

FOR ALL valid existing Ship entities and valid ShipUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyShip whose scalar fields match the request and whose Components, Cargo, and Hopper match the request collections.

**Validates:** Requirements 11.3, 11.4, 11.5, 11.6, 11.9

### Property 9: Service.Create Round-Trip

FOR ALL valid ShipCreateRequest values, calling Service.Create SHALL produce a ReadOnlyShip whose Name matches the request and whose UUID is non-empty.

**Validates:** Requirements 12.2, 12.4, 12.8

### Property 10: Service.Delete Removes Ship

FOR ALL valid existing Ship entities, calling Service.Delete with the ship UUID SHALL cause the ship to no longer be findable via PlayerContext.

**Validates:** Requirements 13.1, 13.2

### Property 11: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Ship property sets SHALL only find matches in ShipService, JSON deserialization, migration code, and the Ship class itself.

**Validates:** Requirements 18.1, 18.2, 18.3, 21.1, 21.2, 21.3


## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the ShipReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- ShipTemplate (FormShipTemplate) migration --- separate BL-115 item.
- BuildPlan mutation --- BuildPlan migration is a separate BL item.
- Crate contents editing --- crate contents are displayed read-only in the cargo grid. Editing crate contents is not in scope.
