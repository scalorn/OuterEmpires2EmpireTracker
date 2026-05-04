# Requirements Document

## Introduction

BL-115 restructures FormShipTemplate so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a ShipTemplateService that applies changes atomically. The form never directly mutates a ShipTemplate --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), and BL-123 (pricing plans).

### Key Differences from BL-112 (DeliveryRoute)

1. **More complex entity** --- ShipTemplate has 2 scalar fields (Name, HullBlueprintUUID) vs DeliveryRoute's 1 (Name). The edit buffer covers both scalars plus the Components list.
2. **Component slots are hull-driven** --- Components are determined by the hull blueprint's slot definitions. Changing the hull clears all components. This is a local edit buffer operation, not an immediate service call.
3. **No stop reordering** --- Unlike DeliveryRoute's ordered stops with add/remove/reorder, ShipTemplate components are slot-based (SlotType + SlotIndex). Components are added/removed/changed by slot position, not reordered.
4. **Order Build feature** --- FormShipTemplate has an Order Build button that creates BuildItems from the template. This feature reads from the template but does not mutate it (it mutates BuildPlan). Order Build remains as-is.
5. **Simpler reference counting** --- ShipTemplateReferenceCounter checks ships, build items, and stock targets (3 source types) vs DeliveryRouteReferenceCounter's 3 source types.
6. **No plan tab** --- Unlike FormDeliveryRoute which has a separate Plan tab, FormShipTemplate is a single-panel form.
7. **ReadOnly wrappers already complete** --- ReadOnlyShipTemplate and ReadOnlyShipComponentSlot are fully implemented with no gaps.
8. **No write locks** --- No ReaderWriterLockSlim on ShipTemplate. The service mutates directly without lock acquisition.

### Similarities to BL-112

1. **Scalar fields** --- Name and HullBlueprintUUID are simple strings managed by the ViewModel edit buffer.
2. **Single form** --- One form (FormShipTemplate) manages the full CRUD lifecycle for templates.
3. **List view with filters** --- Template list has a text filter.
4. **Always player-scoped** --- Templates are always owned by the current player.
5. **Delete reference protection** --- ShipTemplateReferenceCounter checks ships, build items, and stock targets before allowing deletion.
6. **Service as sole mutator** --- Same pattern: form -> ViewModel -> service -> entity.
7. **Components are part of the edit buffer** --- Like DeliveryRoute stops, component add/remove/change accumulates in the ViewModel until Save.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers:
- **Template-level scalar fields:** Name, HullBlueprintUUID
- **Components list:** ShipComponentSlot add/remove/change operations accumulate in the edit buffer until Save

This is because:
- Template name is edited via text box (buffered until Save)
- Hull selection is via combo box (buffered until Save)
- Component slot changes are via grid combo cells (buffered until Save)
- This matches the target pattern: all changes accumulate locally, then save atomically

### Scoping Decision: Order Build

Order Build reads from the current template state (which after migration will be the ViewModel's local state) to generate BuildItems. It does NOT mutate the ShipTemplate. Order Build remains as-is for BL-115 --- it reads from the ViewModel's local Components list instead of the entity's Components list.

## Glossary

- **ShipTemplate**: The mutable entity representing a ship template with UUID, Name, OwnerUUID, HullBlueprintUUID, and a list of ShipComponentSlot entries.
- **ShipComponentSlot**: A nested object within ShipTemplate representing a single component slot with SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent fields.
- **ReadOnlyShipTemplate**: An immutable wrapper around ShipTemplate that exposes only getter properties and a read-only Components list.
- **ReadOnlyShipComponentSlot**: An immutable wrapper around ShipComponentSlot exposing all fields as read-only.
- **ShipTemplateViewModel**: The new ViewModel class that holds a local edit buffer of template Name, HullBlueprintUUID, and Components, disconnected from the entity.
- **ShipTemplateService**: A new service class that is the sole mutator of ShipTemplate entities (create, update, delete).
- **FormShipTemplate**: The WinForms form for viewing and editing ship templates.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of Name, HullBlueprintUUID, and Components in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether Name, HullBlueprintUUID, or Components have been modified since the last load or save.
- **ShipTemplateUpdateRequest**: A DTO carrying the current Name, HullBlueprintUUID, and Components state from the ViewModel to the service for an update operation.
- **ShipTemplateCreateRequest**: A DTO carrying Name, HullBlueprintUUID, and Components values for creating a new template.
- **ShipTemplateReferenceCounter**: A utility that counts how many ships, build items, and stock targets reference a given template UUID, used for delete protection.
- **Immediate_Operation**: Not applicable for BL-115 --- all template mutations (Name + HullBlueprintUUID + Components) are buffered in the ViewModel until Save.

## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the template list view to store ReadOnlyShipTemplate in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormShipTemplate populates the template list view, THE FormShipTemplate SHALL create ListViewItem Tags containing ReadOnlyShipTemplate instances obtained from PlayerContext.GetCurrentPlayerReadOnlyShipTemplates().
2. WHEN the user selects a template in the list view, THE FormShipTemplate SHALL extract the ReadOnlyShipTemplate from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormShipTemplate filters the template list, THE FormShipTemplate SHALL use ReadOnlyShipTemplate properties for the filter comparison.

### Requirement 2: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable ShipTemplate, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormShipTemplate SHALL NOT hold a direct reference to a mutable ShipTemplate in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE ShipTemplateViewModel SHALL NOT expose the mutable ShipTemplate entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 3: ViewModel Copies Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy field values from a ReadOnlyShipTemplate into local properties, so that the ViewModel is a disconnected edit buffer.

#### Acceptance Criteria

1. WHEN the user selects a template, THE ShipTemplateViewModel SHALL copy the Name field from the ReadOnlyShipTemplate into a local property.
2. WHEN the user selects a template, THE ShipTemplateViewModel SHALL copy the HullBlueprintUUID field from the ReadOnlyShipTemplate into a local property.
3. WHEN the user selects a template, THE ShipTemplateViewModel SHALL deep-copy the Components list into a local List<ShipComponentSlot> with new ShipComponentSlot instances (preserving all 6 fields per slot: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).
4. THE ShipTemplateViewModel SHALL retain the original ReadOnlyShipTemplate snapshot for dirty comparison.
5. THE ShipTemplateViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
6. THE ShipTemplateViewModel SHALL NOT hold a reference to the mutable ShipTemplate entity.

### Requirement 4: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want the template name text box, hull combo, and component grid to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormShipTemplate text box (txtName) SHALL read from and write to the ShipTemplateViewModel local Name field.
2. WHEN the user types in txtName, THE FormShipTemplate SHALL update the ShipTemplateViewModel Name local field only (no entity mutation).
3. THE FormShipTemplate hull combo (cmbHull) SHALL read from and write to the ShipTemplateViewModel local HullBlueprintUUID field.
4. WHEN the user selects a hull, THE FormShipTemplate SHALL update the ShipTemplateViewModel HullBlueprintUUID local field and clear the local Components list (no entity mutation).
5. THE FormShipTemplate component grid (dgvSlots) SHALL display components from the ShipTemplateViewModel local Components list.
6. WHEN the user changes a component slot, THE FormShipTemplate SHALL update the ShipTemplateViewModel local Components list only (no entity mutation).

### Requirement 5: No Write-Through

**User Story:** As a developer, I want the ViewModel to stop writing changes to the ShipTemplate entity on every keystroke or control change, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE ShipTemplateViewModel SHALL NOT write Name changes to the ShipTemplate entity on every keystroke.
2. THE ShipTemplateViewModel SHALL NOT write HullBlueprintUUID changes to the ShipTemplate entity on hull selection.
3. THE ShipTemplateViewModel SHALL NOT mutate the ShipTemplate entity Components list when components are added, removed, or changed.
4. THE current write-through pattern (form directly sets entity properties on control change) SHALL be replaced with local-only state changes in the ViewModel.

### Requirement 6: Dirty Tracking

**User Story:** As a developer, I want the ViewModel to track whether Name, HullBlueprintUUID, or Components have been modified since the last load or save, so that the form can detect unsaved changes.

#### Acceptance Criteria

1. THE ShipTemplateViewModel SHALL expose an IsDirty property that returns true when the local Name, HullBlueprintUUID, or Components differ from the original ReadOnlyShipTemplate snapshot.
2. THE IsDirty check SHALL compare the Name field (string equality).
3. THE IsDirty check SHALL compare the HullBlueprintUUID field (string equality).
4. THE IsDirty check SHALL compare the Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values.
5. WHEN the ViewModel is loaded from a ReadOnlyShipTemplate, THE IsDirty property SHALL return false.
6. WHEN the ViewModel represents a new unsaved template (original is null), THE IsDirty property SHALL return true once Name has a non-empty value.

### Requirement 7: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different template, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different template in the list view and the ShipTemplateViewModel is dirty, THE FormShipTemplate SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormShipTemplate SHALL call ShipTemplateService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormShipTemplate SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormShipTemplate SHALL cancel the selection change and keep the current template selected.

### Requirement 8: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the ship template form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormShipTemplate (X button or MDI close) and the ShipTemplateViewModel is dirty, THE FormShipTemplate SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormShipTemplate SHALL save and then close.
3. WHEN the user chooses Discard, THE FormShipTemplate SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormShipTemplate SHALL cancel the close and keep the form open.

### Requirement 9: Unsaved Changes Prompt on New Template

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the ShipTemplateViewModel is dirty, THE FormShipTemplate SHALL prompt before clearing the form for the new template.
2. WHEN the user chooses Save, THE FormShipTemplate SHALL save the current template, then reset the form for a new template.
3. WHEN the user chooses Discard, THE FormShipTemplate SHALL discard changes and reset the form for a new template.
4. WHEN the user chooses Cancel, THE FormShipTemplate SHALL cancel the New operation and keep the current template.

### Requirement 10: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormShipTemplate has unsaved changes, THE FormShipTemplate OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormShipTemplate SHALL cancel the application exit by setting e.Cancel to true.

## Phase 3: ShipTemplateService

### Requirement 11: ShipTemplateService.Update

**User Story:** As a developer, I want a service method that applies template changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE ShipTemplateService SHALL provide an Update method accepting a UUID string and a ShipTemplateUpdateRequest.
2. WHEN Update is called, THE ShipTemplateService SHALL look up the mutable ShipTemplate by UUID via PlayerContext.FindMutableShipTemplate.
3. WHEN Update is called, THE ShipTemplateService SHALL apply the Name and HullBlueprintUUID fields from the request to the entity.
4. WHEN Update is called, THE ShipTemplateService SHALL replace the entity Components list with the request Components list (deep copy).
5. WHEN Update is called, THE ShipTemplateService SHALL persist via PlayerContext.WriteContext().
6. WHEN Update is called, THE ShipTemplateService SHALL fire ShipTemplateDataChanged event.
7. WHEN Update is called, THE ShipTemplateService SHALL return the updated ReadOnlyShipTemplate.
8. IF the UUID is not found, THEN THE ShipTemplateService SHALL throw an InvalidOperationException.

### Requirement 12: ShipTemplateService.Create

**User Story:** As a developer, I want a service method that creates a new template, so that template creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE ShipTemplateService SHALL provide a Create method accepting a ShipTemplateCreateRequest.
2. WHEN Create is called, THE ShipTemplateService SHALL create a new ShipTemplate entity with a generated UUID.
3. WHEN Create is called, THE ShipTemplateService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE ShipTemplateService SHALL populate the Name, HullBlueprintUUID, and Components fields from the request.
5. WHEN Create is called, THE ShipTemplateService SHALL add the template to PlayerContext via AddShipTemplate.
6. WHEN Create is called, THE ShipTemplateService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE ShipTemplateService SHALL fire ShipTemplateDataChanged event.
8. WHEN Create is called, THE ShipTemplateService SHALL return the new ReadOnlyShipTemplate.

### Requirement 13: ShipTemplateService.Delete

**User Story:** As a developer, I want a service method that deletes a template, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE ShipTemplateService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE ShipTemplateService SHALL remove the ShipTemplate from PlayerContext via RemoveShipTemplate.
3. WHEN Delete is called, THE ShipTemplateService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE ShipTemplateService SHALL fire ShipTemplateDataChanged event.
5. IF the UUID is empty or the template is not found, THEN THE ShipTemplateService SHALL return without error.

### Requirement 14: PlayerContext.FindMutableShipTemplate

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable ShipTemplate entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableShipTemplate internal method accepting a UUID string.
2. THE FindMutableShipTemplate method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint, FindMutableSurvey, FindMutableColony, and FindMutableDeliveryRoute.
3. THE FindMutableShipTemplate method SHALL be marked internal so only the service project can access it.

### Requirement 15: Save Flow

**User Story:** As a developer, I want the Save button to route through the service, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new template (IsNew is true), THE FormShipTemplate SHALL call ShipTemplateService.Create with a ShipTemplateCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing template, THE FormShipTemplate SHALL call ShipTemplateService.Update with the UUID and a ShipTemplateUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyShipTemplate, THE FormShipTemplate SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyShipTemplate.
4. AFTER a successful save, THE ShipTemplateViewModel IsDirty property SHALL return false.

### Requirement 16: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that templates in use by ships or build items cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormShipTemplate SHALL check ShipTemplateReferenceCounter for references to the current template.
2. IF the template has references (CountReferences > 0), THEN THE FormShipTemplate SHALL display a warning message listing the reference count and prevent deletion.
3. IF the template has no references, THE FormShipTemplate SHALL prompt for confirmation before calling ShipTemplateService.Delete.
4. AFTER successful deletion, THE FormShipTemplate SHALL clear the form and refresh the list view.

### Requirement 17: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the ShipTemplate entity is only mutated by the service, deserialization, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE ShipTemplate entity SHALL only be mutated by ShipTemplateService methods (Update, Create, Delete), JSON deserialization (loading from file), and migration code.
2. THE FormShipTemplate SHALL NOT directly set properties on a ShipTemplate.
3. THE ShipTemplateViewModel SHALL NOT directly set properties on a ShipTemplate.

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

**User Story:** As a developer, I want to verify that no code outside the service directly mutates ShipTemplate entities, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct ShipTemplate.Name property sets SHALL only find matches in ShipTemplateService, JSON deserialization, migration code, and the ShipTemplate class itself.
2. AFTER migration, a grep for direct ShipTemplate.Components list mutation (Add, Remove, Clear, index assignment) SHALL only find matches in ShipTemplateService, JSON deserialization, and the ShipTemplate class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Fields

FOR ALL valid ShipTemplate entities, wrapping in ReadOnlyShipTemplate and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, HullBlueprintUUID, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).

**Validates:** Requirements 3.1, 3.2, 3.3

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid ShipTemplate entities, wrapping in ReadOnlyShipTemplate and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 6.1, 6.5

### Property 3: IsDirty Detects Name Change

FOR ALL valid ShipTemplate entities, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.2

### Property 4: IsDirty Detects HullBlueprintUUID Change

FOR ALL valid ShipTemplate entities, after LoadFrom, changing the HullBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.3

### Property 5: IsDirty Detects Components Change

FOR ALL valid ShipTemplate entities with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates:** Requirements 6.1, 6.4

### Property 6: Service.Update Round-Trip

FOR ALL valid existing ShipTemplate entities and valid ShipTemplateUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyShipTemplate whose Name and HullBlueprintUUID match the request and whose Components match the request Components (same count, same field values per slot).

**Validates:** Requirements 11.3, 11.4, 11.7

### Property 7: Service.Create Round-Trip

FOR ALL valid ShipTemplateCreateRequest values, calling Service.Create SHALL produce a ReadOnlyShipTemplate whose Name and HullBlueprintUUID match the request, whose Components match the request Components, and whose UUID is non-empty.

**Validates:** Requirements 12.2, 12.4, 12.8

### Property 8: Service.Delete Removes Template

FOR ALL valid existing ShipTemplate entities, calling Service.Delete with the template UUID SHALL cause the template to no longer be findable via PlayerContext.

**Validates:** Requirements 13.1, 13.2

### Property 9: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct ShipTemplate property sets SHALL only find matches in ShipTemplateService, JSON deserialization, migration code, and the ShipTemplate class itself.

**Validates:** Requirements 17.1, 17.2, 17.3, 20.1, 20.2


## Out of Scope

- Changing other entity types to the service pattern --- separate BL items.
- Actual remote service calls --- this establishes the local service pattern that can later be swapped for HTTP/gRPC.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Changing the ShipTemplateReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- Order Build feature migration --- Order Build reads from the ViewModel's local state but does not mutate the ShipTemplate. It remains as-is.
- ShipInstance (FormShipInstance) migration --- separate BL item.
- BuildPlan mutation in Order Build --- Order Build mutates BuildPlan, not ShipTemplate. BuildPlan migration is a separate BL item.
