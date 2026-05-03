# Requirements Document

## Introduction

BL-109 restructures FormColonyV2 so that all data access goes through ReadOnly wrappers, the ViewModel becomes a local edit buffer disconnected from the entity, and saves go through a ColonyService that applies changes atomically. The form never directly mutates a Colony --- only the service does. This follows the same immutable data model pattern established in BL-108 (blueprints), BL-110 (surveys), BL-111 (player profiles), and BL-123 (pricing plans).

### Key Differences from BL-110 (Survey)

1. **Nested structure management** --- Colony has a List<ColonyStructure> where each structure has complex state (~30+ fields: timers, manufacturing queues, worker assignments, mining survey assignments, status tracking). Structure add/remove/configure operations are immediate service calls, NOT part of the ViewModel edit buffer.
2. **Background processing** --- Colony.ProcessColony() is a 7-step processing pipeline (building, mining, refining tiers 0-2, manufacturing, research) that mutates structures, items, and timers on a background thread with ReaderWriterLockSlim concurrency control.
3. **Concurrency** --- Colony uses ReaderWriterLockSlim for thread-safe access. The service must acquire write locks for mutations.
4. **Item inventory** --- Colony manages an ItemBag with resources, blueprints, commodities. Item add/remove operations go through the service as immediate operations.
5. **Commodity requests** --- Colony tracks commodity demand (List<CommodityRequested>). Commodity request add/remove/edit operations go through the service as immediate operations.
6. **Import complexity** --- ColonyParser parses HTML from game browser, creates/updates colony with structures, mining assignments, commodity demands. Import is a merge operation that preserves existing structure UUIDs and locally-configured state.
7. **Reference counting** --- ColonyReferenceCounter checks delivery routes, delivery plans, build plans, supply chains, and overflow rules (5 source types vs Survey's 2).
8. **Status calculation** --- ColonyStatusCalculator computes power/habitation/food/entertainment/warehouse status from structure state.
9. **ReadOnlyColony already complete** --- No gap fill needed (unlike Survey which needed 4 new properties).
10. **Scoped ViewModel edit buffer** --- The ViewModel edit buffer covers ONLY colony-level scalar fields (PlanetName, ColonyName, SystemName). Structure, item, and commodity operations are immediate service calls with no dirty tracking.

### Similarities to BL-110

1. **Flat scalar fields** --- PlanetName, SystemName, ColonyName, OwnerUUID, UUID are simple strings managed by the ViewModel edit buffer.
2. **Single form** --- One form (FormColonyV2) manages the full CRUD lifecycle.
3. **List view with filters** --- Colony list has text filter and column sorting.
4. **Always player-scoped** --- Colonies are always owned by the current player.
5. **Clipboard import** --- Both have HTML clipboard import that goes through the service.
6. **Delete reference protection** --- Both check reference counts before allowing deletion.
7. **Service as sole mutator** --- Same pattern: form -> ViewModel -> service -> entity.

### Scoping Decision: ViewModel Edit Buffer

The ViewModel edit buffer covers ONLY colony-level scalar fields (PlanetName, ColonyName, SystemName). This is because:

- Colony scalar fields are edited via text boxes with a Save button (same as Survey/PricingPlan).
- Structure operations (add, remove, configure mining/manufacturing/research) are immediate actions that save through the service instantly --- they do not accumulate in an edit buffer.
- Item operations (add, remove, edit quantity) are immediate actions through the service.
- Commodity request operations (add, remove, edit) are immediate actions through the service.

This matches how the form actually works: the user edits colony name/planet name in text boxes (buffered), but structure/item/commodity changes are immediate operations.

## Glossary

- **Colony**: The mutable entity representing a player's colony with identity fields, structures, items, commodity requests, and processing logic.
- **ColonyStructure**: A nested entity within Colony representing a single building (mining rig, refinery, manufactory, etc.) with complex state.
- **ReadOnlyColony**: An immutable wrapper around Colony that exposes only getter properties and read-only nested collections.
- **ReadOnlyColonyStructure**: An immutable wrapper around ColonyStructure exposing read-only properties.
- **ColonyViewModel**: The ViewModel class that holds a local edit buffer of colony-level scalar fields, disconnected from the entity.
- **ColonyService**: A new service class that is the sole mutator of Colony entities (create, update, delete, import, structure operations, item operations, commodity operations).
- **FormColonyV2**: The WinForms form for viewing and editing colonies.
- **PlayerContext**: The singleton service that manages player data persistence and provides read-only accessors.
- **Edit_Buffer**: A local copy of scalar field values in the ViewModel, disconnected from the entity, that accumulates changes until Save is clicked.
- **Dirty_Tracking**: The mechanism by which the ViewModel detects whether any scalar field has been modified since the last load or save.
- **ColonyUpdateRequest**: A DTO carrying the current scalar field state from the ViewModel to the service for an update operation.
- **ColonyCreateRequest**: A DTO carrying scalar field values for creating a new colony.
- **ColonyReferenceCounter**: A utility that counts how many delivery routes, delivery plans, build plans, supply chains, and overflow rules reference a given colony UUID, used for delete protection.
- **ColonyParser**: The HTML parser that extracts colony data from game clipboard content into a temporary Colony object.
- **ColonyStatusCalculator**: A calculator that computes power/habitation/food/entertainment/warehouse status from colony structure state.
- **ItemBag**: The inventory container within Colony that holds resources, blueprints, commodities, and manufactured items.
- **CommodityRequested**: A nested entity within Colony representing a commodity demand with name, quantity requested, quantity delivered, and need-by date.
- **Immediate_Operation**: A service call that mutates the entity and persists immediately, without going through the ViewModel edit buffer or dirty tracking.
- **FilteredTextComboSet**: A custom combo control used for flatpack blueprint selection with type-ahead filtering.
## Requirements


## Phase 1: Read-Only Consumer Migration

### Requirement 1: List View Uses ReadOnly Wrappers

**User Story:** As a developer, I want the colony list view to store ReadOnlyColony in Tags, so that no mutable entity references leak into the list view.

#### Acceptance Criteria

1. WHEN the FormColonyV2 populates the list view, THE FormColonyV2 SHALL create ListViewItem Tags containing ReadOnlyColony instances obtained from PlayerContext.GetCurrentPlayerReadOnlyColonies().
2. WHEN the user selects a colony in the list view, THE FormColonyV2 SHALL extract the ReadOnlyColony from the selected item Tag and pass it to the ViewModel LoadFrom method.
3. WHEN the FormColonyV2 filters the colony list, THE FormColonyV2 SHALL use ReadOnlyColony properties for the filter comparison.

### Requirement 2: PlayerContext ReadOnly Colony Accessor

**User Story:** As a developer, I want PlayerContext to expose a method returning ReadOnlyColony wrappers, so that the form can populate the list view without accessing mutable entities.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a GetCurrentPlayerReadOnlyColonies method returning a List<ReadOnlyColony>.
2. WHEN GetCurrentPlayerReadOnlyColonies is called, THE PlayerContext SHALL return ReadOnlyColony wrappers for all colonies owned by the current player.

### Requirement 3: No Mutable Entity in Read-Only Paths

**User Story:** As a developer, I want to ensure no read-only code path holds a direct reference to a mutable Colony, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER Phase 1 migration, THE FormColonyV2 SHALL NOT hold a direct reference to a mutable Colony in any read-only code path (list view Tags, display-only fields, filter logic, reference counter display).
2. THE ColonyViewModel SHALL NOT expose the mutable Colony entity via a public Data property or equivalent accessor.


## Phase 2: ViewModel as Local Edit Buffer

### Requirement 4: ViewModel Copies Scalar Fields from ReadOnly

**User Story:** As a developer, I want the ViewModel to copy scalar field values from a ReadOnlyColony into local properties, so that the ViewModel is a disconnected edit buffer for colony identity fields.

#### Acceptance Criteria

1. WHEN the user selects a colony, THE ColonyViewModel SHALL copy all scalar field values from the ReadOnlyColony into local properties: PlanetName, ColonyName, SystemName.
2. THE ColonyViewModel SHALL retain the original ReadOnlyColony snapshot for dirty comparison.
3. THE ColonyViewModel SHALL store the UUID and OwnerUUID from the original snapshot.
4. THE ColonyViewModel SHALL NOT hold a reference to the mutable Colony entity.

### Requirement 5: Controls Bind to ViewModel Local State

**User Story:** As a developer, I want the colony identity text boxes to read from and write to the ViewModel local fields, so that changes live in the ViewModel only until Save.

#### Acceptance Criteria

1. THE FormColonyV2 text boxes (txtPlanetName, txtColonyName, txtSystemName) SHALL read from and write to the ColonyViewModel local fields.
2. WHEN the user types in txtPlanetName, THE FormColonyV2 SHALL update the ColonyViewModel PlanetName local field only (no entity mutation).
3. WHEN the user types in txtColonyName, THE FormColonyV2 SHALL update the ColonyViewModel ColonyName local field only (no entity mutation).
4. WHEN the user types in txtSystemName, THE FormColonyV2 SHALL update the ColonyViewModel SystemName local field only (no entity mutation).

### Requirement 6: No Write-Through for Scalar Fields

**User Story:** As a developer, I want the ViewModel to stop writing scalar field changes to the Colony entity on every keystroke, so that the entity remains unchanged until Save.

#### Acceptance Criteria

1. THE ColonyViewModel SHALL NOT write PlanetName, ColonyName, or SystemName changes to the Colony entity on every keystroke or control change.
2. THE current write-through pattern (TextChanged handler sets viewModel property which sets entity property) SHALL be replaced with local-only state changes in the ViewModel.

### Requirement 7: Dirty Tracking for Scalar Fields

**User Story:** As a developer, I want the ViewModel to track whether any scalar field has been modified since the last load or save, so that the Save button enables only when changes exist.

#### Acceptance Criteria

1. THE ColonyViewModel SHALL expose an IsDirty property that returns true when any scalar field differs from the original ReadOnlyColony snapshot.
2. THE IsDirty check SHALL compare all scalar fields: PlanetName, ColonyName, SystemName.
3. WHEN the ViewModel is loaded from a ReadOnlyColony, THE IsDirty property SHALL return false.
4. WHEN the ViewModel represents a new unsaved colony (original is null), THE IsDirty property SHALL return true once any field has a non-default value.
5. THE FormColonyV2 SHALL enable the Save button only when the ColonyViewModel IsDirty property returns true.

### Requirement 8: Unsaved Changes Prompt on Selection Change

**User Story:** As a user, I want to be prompted about unsaved changes when I select a different colony, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user selects a different colony in the list view and the ColonyViewModel is dirty, THE FormColonyV2 SHALL prompt with Save, Discard, and Cancel options.
2. WHEN the user chooses Save, THE FormColonyV2 SHALL call ColonyService.Update (or Create if new), then load the new selection.
3. WHEN the user chooses Discard, THE FormColonyV2 SHALL discard local changes and load the new selection.
4. WHEN the user chooses Cancel, THE FormColonyV2 SHALL cancel the selection change and keep the current colony selected.

### Requirement 9: Unsaved Changes Prompt on Form Close

**User Story:** As a user, I want to be prompted about unsaved changes when I close the colony form, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user closes the FormColonyV2 (X button or MDI close) and the ColonyViewModel is dirty, THE FormColonyV2 SHALL prompt with the same Save, Discard, Cancel dialog.
2. WHEN the user chooses Save, THE FormColonyV2 SHALL save and then close.
3. WHEN the user chooses Discard, THE FormColonyV2 SHALL close without saving.
4. WHEN the user chooses Cancel, THE FormColonyV2 SHALL cancel the close and keep the form open.

### Requirement 10: Unsaved Changes Prompt on New Colony

**User Story:** As a user, I want to be prompted about unsaved changes when I click New, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks New while the ColonyViewModel is dirty, THE FormColonyV2 SHALL prompt before clearing the form for the new colony.
2. WHEN the user chooses Save, THE FormColonyV2 SHALL save the current colony, then reset the form for a new colony.
3. WHEN the user chooses Discard, THE FormColonyV2 SHALL discard changes and reset the form for a new colony.
4. WHEN the user chooses Cancel, THE FormColonyV2 SHALL cancel the New operation and keep the current colony.

### Requirement 11: Unsaved Changes Prompt on Import

**User Story:** As a user, I want to be prompted about unsaved changes when I click Import, so that I do not accidentally lose my edits.

#### Acceptance Criteria

1. WHEN the user clicks Import while the ColonyViewModel is dirty, THE FormColonyV2 SHALL prompt before proceeding with the import.
2. WHEN the user chooses Save, THE FormColonyV2 SHALL save the current colony, then proceed with the import.
3. WHEN the user chooses Discard, THE FormColonyV2 SHALL discard changes and proceed with the import.
4. WHEN the user chooses Cancel, THE FormColonyV2 SHALL cancel the Import operation and keep the current colony.

### Requirement 12: Unsaved Changes Prompt on Application Exit

**User Story:** As a user, I want to be prompted about unsaved changes when the application exits, so that I do not lose edits on shutdown.

#### Acceptance Criteria

1. WHEN the application exits (MainWindow closing) and the FormColonyV2 has unsaved changes, THE FormColonyV2 OnFormClosing handler SHALL trigger the same Save, Discard, Cancel prompt.
2. IF the user chooses Cancel, THEN THE FormColonyV2 SHALL cancel the application exit by setting e.Cancel to true.

## Phase 3: ColonyService

### Requirement 13: ColonyService.Update (Scalar Fields)

**User Story:** As a developer, I want a service method that applies colony scalar field changes atomically, so that the entity is only mutated through a controlled gate.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an Update method accepting a UUID string and a ColonyUpdateRequest.
2. WHEN Update is called, THE ColonyService SHALL look up the mutable Colony by UUID via PlayerContext.FindMutableColony.
3. WHEN Update is called, THE ColonyService SHALL acquire the colony write lock before mutating.
4. WHEN Update is called, THE ColonyService SHALL apply all changed scalar fields from the request to the entity: PlanetName, ColonyName, SystemName.
5. WHEN Update is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
6. WHEN Update is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.
7. WHEN Update is called, THE ColonyService SHALL return the updated ReadOnlyColony.
8. IF the UUID is not found, THEN THE ColonyService SHALL throw an InvalidOperationException.

### Requirement 14: ColonyService.Create

**User Story:** As a developer, I want a service method that creates a new colony, so that colony creation goes through the same controlled gate.

#### Acceptance Criteria

1. THE ColonyService SHALL provide a Create method accepting a ColonyCreateRequest.
2. WHEN Create is called, THE ColonyService SHALL create a new Colony entity with a generated UUID.
3. WHEN Create is called, THE ColonyService SHALL set the OwnerUUID to the current player UUID.
4. WHEN Create is called, THE ColonyService SHALL populate all scalar fields from the request: PlanetName, ColonyName, SystemName.
5. WHEN Create is called, THE ColonyService SHALL add the colony to PlayerContext via AddColony.
6. WHEN Create is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
7. WHEN Create is called, THE ColonyService SHALL fire ColonyDataChanged event with the new colony UUID.
8. WHEN Create is called, THE ColonyService SHALL return the new ReadOnlyColony.

### Requirement 15: ColonyService.Delete

**User Story:** As a developer, I want a service method that deletes a colony, so that deletion goes through the controlled gate.

#### Acceptance Criteria

1. THE ColonyService SHALL provide a Delete method accepting a UUID string.
2. WHEN Delete is called, THE ColonyService SHALL remove the Colony from PlayerContext via RemoveColony.
3. WHEN Delete is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
4. WHEN Delete is called, THE ColonyService SHALL fire ColonyDataChanged event with the deleted colony UUID.
5. IF the UUID is empty or the colony is not found, THEN THE ColonyService SHALL return without error.

### Requirement 16: ColonyService.Import

**User Story:** As a developer, I want a service method that handles clipboard import, so that import goes through the controlled gate instead of the form directly mutating entities.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an Import method accepting a parsed temporary Colony object (from ColonyParser) and an EmpireContext reference.
2. WHEN Import is called and an existing colony matches by PlanetName (case-insensitive), THE ColonyService SHALL merge the parsed data into the existing colony using ColonyParser.ProcessHtml logic, preserving the existing UUID and locally-configured state.
3. WHEN Import is called and no existing colony matches, THE ColonyService SHALL create a new colony with a generated UUID and the current player OwnerUUID.
4. WHEN Import is called, THE ColonyService SHALL acquire the colony write lock before mutating.
5. WHEN Import is called, THE ColonyService SHALL set LastImportDateTime to the current UTC time.
6. WHEN Import is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
7. WHEN Import is called, THE ColonyService SHALL fire ColonyDataChanged event with the imported colony UUID.
8. WHEN Import is called, THE ColonyService SHALL return the ReadOnlyColony of the imported or updated colony.

### Requirement 17: ColonyService.AddStructure

**User Story:** As a developer, I want a service method that adds a structure to a colony, so that structure addition goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an AddStructure method accepting a colony UUID and a flatpack blueprint UUID.
2. WHEN AddStructure is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN AddStructure is called, THE ColonyService SHALL create a new ColonyStructure with a generated UUID, the flatpack blueprint UUID, and the next DisplaySequence for that type.
4. WHEN AddStructure is called, THE ColonyService SHALL add the structure to the colony Structures list.
5. WHEN AddStructure is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
6. WHEN AddStructure is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 18: ColonyService.RemoveStructure

**User Story:** As a developer, I want a service method that removes a structure from a colony, so that structure removal goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide a RemoveStructure method accepting a colony UUID and a structure UUID.
2. WHEN RemoveStructure is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN RemoveStructure is called, THE ColonyService SHALL remove the structure from the colony Structures list.
4. WHEN RemoveStructure is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN RemoveStructure is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 19: ColonyService.AddItem

**User Story:** As a developer, I want a service method that adds an item to a colony warehouse, so that item addition goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an AddItem method accepting a colony UUID and an Item object.
2. WHEN AddItem is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN AddItem is called, THE ColonyService SHALL add the item to the colony ItemBag.
4. WHEN AddItem is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN AddItem is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 20: ColonyService.RemoveItem

**User Story:** As a developer, I want a service method that removes an item from a colony warehouse, so that item removal goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide a RemoveItem method accepting a colony UUID and an item UUID.
2. WHEN RemoveItem is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN RemoveItem is called, THE ColonyService SHALL remove the item from the colony ItemBag.
4. WHEN RemoveItem is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN RemoveItem is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 21: ColonyService.UpdateItem

**User Story:** As a developer, I want a service method that updates an item quantity in a colony warehouse, so that item edits go through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an UpdateItem method accepting a colony UUID, an item UUID, and a new quantity.
2. WHEN UpdateItem is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN UpdateItem is called, THE ColonyService SHALL update the item quantity in the colony ItemBag.
4. WHEN UpdateItem is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN UpdateItem is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 22: ColonyService.AddCommodityRequest

**User Story:** As a developer, I want a service method that adds a commodity request to a colony, so that commodity request addition goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an AddCommodityRequest method accepting a colony UUID, commodity name, requested quantity, and optional need-by date.
2. WHEN AddCommodityRequest is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN AddCommodityRequest is called, THE ColonyService SHALL add the CommodityRequested to the colony Commodities list.
4. WHEN AddCommodityRequest is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN AddCommodityRequest is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 23: ColonyService.RemoveCommodityRequest

**User Story:** As a developer, I want a service method that removes a commodity request from a colony, so that commodity request removal goes through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide a RemoveCommodityRequest method accepting a colony UUID and a commodity request identifier.
2. WHEN RemoveCommodityRequest is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN RemoveCommodityRequest is called, THE ColonyService SHALL remove the CommodityRequested from the colony Commodities list.
4. WHEN RemoveCommodityRequest is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN RemoveCommodityRequest is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 24: ColonyService.UpdateCommodityRequest

**User Story:** As a developer, I want a service method that updates a commodity request in a colony, so that commodity request edits go through the controlled gate as an immediate operation.

#### Acceptance Criteria

1. THE ColonyService SHALL provide an UpdateCommodityRequest method accepting a colony UUID and updated CommodityRequested values.
2. WHEN UpdateCommodityRequest is called, THE ColonyService SHALL acquire the colony write lock before mutating.
3. WHEN UpdateCommodityRequest is called, THE ColonyService SHALL update the CommodityRequested fields (Requested, Delivered, NeedBy).
4. WHEN UpdateCommodityRequest is called, THE ColonyService SHALL persist via PlayerContext.WriteContext().
5. WHEN UpdateCommodityRequest is called, THE ColonyService SHALL fire ColonyDataChanged event with the colony UUID.

### Requirement 25: PlayerContext.FindMutableColony

**User Story:** As a developer, I want an internal method on PlayerContext that returns the mutable Colony entity, so that only the service can access it.

#### Acceptance Criteria

1. THE PlayerContext SHALL provide a FindMutableColony internal method accepting a UUID string.
2. THE FindMutableColony method SHALL follow the same cache-based lookup pattern as FindMutableBlueprint and FindMutableSurvey.
3. THE FindMutableColony method SHALL be marked internal so only the service project can access it.

### Requirement 26: Save Flow

**User Story:** As a developer, I want the Save button to route through the service for scalar field changes, so that the form never directly mutates the entity.

#### Acceptance Criteria

1. WHEN the user clicks Save and the ViewModel represents a new colony (IsNew is true), THE FormColonyV2 SHALL call ColonyService.Create with a ColonyCreateRequest built from the ViewModel.
2. WHEN the user clicks Save and the ViewModel represents an existing colony, THE FormColonyV2 SHALL call ColonyService.Update with the UUID and a ColonyUpdateRequest built from the ViewModel.
3. WHEN the service returns the updated ReadOnlyColony, THE FormColonyV2 SHALL refresh the list view and reload the ViewModel from the fresh ReadOnlyColony.
4. AFTER a successful save, THE ColonyViewModel IsDirty property SHALL return false.

### Requirement 27: Delete Flow with Reference Protection

**User Story:** As a developer, I want the Delete button to check references before deleting, so that colonies in use by delivery routes, delivery plans, build plans, supply chains, or overflow rules cannot be deleted.

#### Acceptance Criteria

1. WHEN the user clicks Delete, THE FormColonyV2 SHALL check ColonyReferenceCounter for references to the current colony.
2. IF the colony has references (TotalCount > 0), THEN THE FormColonyV2 SHALL display a warning message listing reference counts by type (routes, plans, build items, supply chains, overflow rules) and prevent deletion.
3. IF the colony has no references, THE FormColonyV2 SHALL prompt for confirmation before calling ColonyService.Delete.
4. AFTER successful deletion, THE FormColonyV2 SHALL clear the form and refresh the list view.

### Requirement 28: Import Flow Through Service

**User Story:** As a developer, I want the Import button to route through the service, so that clipboard import never directly mutates entities from the form.

#### Acceptance Criteria

1. WHEN the user clicks Import, THE FormColonyV2 SHALL validate clipboard content (HTML present, correct content type, player selected).
2. WHEN clipboard validation passes, THE FormColonyV2 SHALL call ColonyParser.ParseClipboardToTemp to get a temporary Colony object.
3. WHEN parsing succeeds, THE FormColonyV2 SHALL call ColonyService.Import with the temporary Colony object and EmpireContext.
4. WHEN the service returns the ReadOnlyColony, THE FormColonyV2 SHALL refresh the list view, select the imported colony, and load it into the ViewModel.

### Requirement 29: Service Is the Only Mutator

**User Story:** As a developer, I want to ensure the Colony entity is only mutated by the service, deserialization, background processing, and migration code, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, THE Colony entity SHALL only be mutated by ColonyService methods, ColonyParser (called by the service during import), Colony.ProcessColony (background processing), JSON deserialization (loading from file), and migration code.
2. THE FormColonyV2 SHALL NOT directly set properties on a Colony.
3. THE ColonyViewModel SHALL NOT directly set properties on a Colony.

## Phase 4: Verification

### Requirement 30: Existing Tests Pass

**User Story:** As a developer, I want all existing tests to continue passing after the migration, so that no regressions are introduced.

#### Acceptance Criteria

1. AFTER migration, THE test suite SHALL pass with zero failures.

### Requirement 31: Audit Clean

**User Story:** As a developer, I want the audit to report no new findings, so that the migration does not introduce code quality regressions.

#### Acceptance Criteria

1. AFTER migration, THE audit (node .kiro/tools/audit.js) SHALL report no new findings beyond the accepted baseline.

### Requirement 32: No Direct Mutation Outside Service

**User Story:** As a developer, I want to verify that no code outside the service directly mutates Colony scalar fields, so that the immutable data model is enforced.

#### Acceptance Criteria

1. AFTER migration, a grep for direct Colony.PlanetName, Colony.ColonyName, and Colony.SystemName property sets SHALL only find matches in ColonyService, ColonyParser (populating temp objects or called by service), Colony.ProcessColony (background processing), JSON deserialization, migration code, and the Colony class itself.
2. AFTER migration, a grep for direct Colony.Structures list mutation (Add, Remove, Clear) SHALL only find matches in ColonyService, ColonyParser (called by service during import), JSON deserialization, and the Colony class itself.


## Correctness Properties

These properties define universal invariants that property-based tests validate across randomly generated inputs.

### Property 1: LoadFrom Round-Trip Preserves All Scalar Fields

FOR ALL valid Colony entities, wrapping in ReadOnlyColony and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity scalar fields (PlanetName, ColonyName, SystemName).

**Validates:** Requirements 4.1, 4.2, 4.3

### Property 2: IsDirty False Immediately After LoadFrom

FOR ALL valid Colony entities, wrapping in ReadOnlyColony and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates:** Requirements 7.1, 7.3

### Property 3: IsDirty Detects Any Single Scalar Field Change

FOR ALL valid Colony entities, after LoadFrom, changing any single scalar field (PlanetName, ColonyName, SystemName) to a different value SHALL cause IsDirty to return true.

**Validates:** Requirements 7.1, 7.2

### Property 4: Service.Update Round-Trip

FOR ALL valid existing Colony entities and valid ColonyUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyColony whose scalar fields match the request values (PlanetName, ColonyName, SystemName).

**Validates:** Requirements 13.4, 13.7

### Property 5: Service.Create Round-Trip

FOR ALL valid ColonyCreateRequest values, calling Service.Create SHALL produce a ReadOnlyColony whose scalar fields match the request values and whose UUID is non-empty.

**Validates:** Requirements 14.2, 14.4, 14.8

### Property 6: Service.Delete Removes Colony

FOR ALL valid existing Colony entities, calling Service.Delete with the colony UUID SHALL cause the colony to no longer be findable via PlayerContext.

**Validates:** Requirements 15.1, 15.2

### Property 7: Service.AddStructure Increases Structure Count

FOR ALL valid existing Colony entities and valid flatpack blueprint UUIDs, calling Service.AddStructure SHALL increase the colony Structures count by exactly one and the new structure SHALL have the specified flatpack blueprint UUID.

**Validates:** Requirements 17.3, 17.4

### Property 8: Service.RemoveStructure Decreases Structure Count

FOR ALL valid existing Colony entities with at least one structure, calling Service.RemoveStructure with a valid structure UUID SHALL decrease the colony Structures count by exactly one.

**Validates:** Requirements 18.3

### Property 9: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Colony scalar property sets SHALL only find matches in ColonyService, ColonyParser, Colony.ProcessColony, JSON deserialization, and the Colony class itself.

**Validates:** Requirements 29.1, 29.2, 29.3, 32.1


## Out of Scope

- Changing other entity types to the service pattern --- separate BL items (BL-108, BL-110, BL-111, BL-123).
- Modifying Colony.ProcessColony background processing logic --- it continues to mutate the entity directly under write lock. The service pattern does not wrap ProcessColony.
- Modifying ColonyParser HTML parsing logic --- it continues to parse into temp Colony objects. The service handles the merge.
- Modifying ColonyStatusCalculator logic --- it continues to work as-is, called from the ViewModel or form for display purposes.
- Modifying ColonyReferenceCounter logic --- it continues to work as-is, just called from the form before invoking the service.
- Undo/redo --- future enhancement on top of the edit buffer pattern.
- Multi-colony import (batch import) --- future enhancement.
- Changing the list view filter logic beyond switching from mutable to read-only wrappers.
- Changing the structure control visual design or adding new structure types.
- Changing the warehouse tab visual design or adding new item types.
- Changing the commodity request grid visual design.
- Making structure configuration (mining survey assignment, manufacturing blueprint assignment, research assignment) part of the ViewModel edit buffer --- these remain immediate service operations.
- Wrapping ColonyStructure mutations in a StructureService --- structure mutations go through ColonyService methods that operate on the parent colony.
