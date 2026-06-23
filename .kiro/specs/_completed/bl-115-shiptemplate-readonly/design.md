# BL-115 Design: ShipTemplate Immutable Data Model with Service Layer

## Overview

BL-115 applies the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), and BL-123 (pricing plans) to the ShipTemplate form. The form stops directly mutating ShipTemplate entities. The ViewModel becomes a disconnected edit buffer for the template Name, HullBlueprintUUID, and Components list. A new ShipTemplateService is the sole mutator of ShipTemplate entities.

### Key Differences from BL-112 (DeliveryRoute)

1. **More complex entity** --- ShipTemplate has 2 scalar fields (Name, HullBlueprintUUID) vs DeliveryRoute's 1 (Name). The edit buffer covers both scalars plus the Components list.
2. **Component slots are hull-driven** --- Components are determined by the hull blueprint's slot definitions. Changing the hull clears all components in the local edit buffer.
3. **No stop reordering** --- Components are slot-based (SlotType + SlotIndex), not ordered like RouteStops.
4. **Order Build feature** --- Reads from the ViewModel's local state to generate BuildItems. Does not mutate ShipTemplate.
5. **No plan tab** --- Single-panel form unlike FormDeliveryRoute's dual-tab layout.
6. **ReadOnly wrappers already complete** --- ReadOnlyShipTemplate and ReadOnlyShipComponentSlot are fully implemented.
7. **No write locks** --- No ReaderWriterLockSlim. The service mutates directly.

### Similarities to BL-112

1. **Scalar fields** --- Name and HullBlueprintUUID managed by the ViewModel edit buffer.
2. **Single form** --- FormShipTemplate manages the full CRUD lifecycle.
3. **List view with filters** --- Template list has a text filter.
4. **Always player-scoped** --- Templates owned by the current player.
5. **Delete reference protection** --- ShipTemplateReferenceCounter checks ships, build items, and stock targets.
6. **Service as sole mutator** --- form -> ViewModel -> service -> entity.
7. **Components are part of the edit buffer** --- Like stops, component changes accumulate until Save.

## Architecture

### Current Architecture

The form holds a direct mutable ShipTemplate reference (_selectedTemplate). Every control change writes directly to the entity:
- TxtName_TextChanged sets _selectedTemplate.Name
- CmbHull_SelectedItemChanged sets _selectedTemplate.HullBlueprintUUID and clears _selectedTemplate.Components
- DgvSlots_CellValueChanged adds/removes ShipComponentSlot entries in _selectedTemplate.Components
- CmdSave_Click sets _selectedTemplate.Name and calls WriteContext()
- CmdNew_Click creates a new ShipTemplate, adds to PlayerContext, calls WriteContext()
- CmdDelete_Click removes from PlayerContext, calls WriteContext()

### Target Architecture

The form never touches the entity. The ViewModel is a disconnected edit buffer for Name, HullBlueprintUUID, and Components. The service is the only code that mutates the entity. All component operations accumulate in the ViewModel's local Components list until Save.

### Data Flow

Load Flow:
1. Form calls PlayerContext.GetCurrentPlayerReadOnlyShipTemplates()
2. Form stores ReadOnlyShipTemplate in ListViewItem Tags
3. On selection, Form calls ViewModel.LoadFrom(ReadOnlyShipTemplate)
4. ViewModel copies Name, HullBlueprintUUID, deep-copies Components into local state

Edit Flow (Name):
1. Form sets ViewModel.Name (local property)
2. ViewModel updates local Name only (no entity mutation)

Edit Flow (Hull):
1. Form sets ViewModel.HullBlueprintUUID (local property)
2. Form calls ViewModel.ClearComponents()
3. ViewModel updates local state only (no entity mutation)

Edit Flow (Components):
1. Form calls ViewModel.SetComponent(slotType, slotIndex, blueprintUUID) or ViewModel.RemoveComponent(slotType, slotIndex)
2. ViewModel modifies local Components list only (no entity mutation)

Save Flow (existing template):
1. Form calls ViewModel.BuildUpdateRequest()
2. Form calls Service.Update(uuid, request)
3. Service looks up mutable entity via FindMutableShipTemplate
4. Service applies Name, HullBlueprintUUID, replaces Components list
5. Service persists via WriteContext(), fires ShipTemplateDataChanged
6. Service returns ReadOnlyShipTemplate
7. Form reloads ViewModel from fresh ReadOnlyShipTemplate

Save Flow (new template):
1. Form calls ViewModel.BuildCreateRequest()
2. Form calls Service.Create(request)
3. Service creates new ShipTemplate with UUID, OwnerUUID, Name, HullBlueprintUUID, Components
4. Service adds to PlayerContext, persists, fires event
5. Service returns ReadOnlyShipTemplate
6. Form reloads ViewModel from fresh ReadOnlyShipTemplate

Delete Flow:
1. Form checks ShipTemplateReferenceCounter
2. If references exist, show warning and block
3. If no references, prompt for confirmation
4. Form calls Service.Delete(uuid)
5. Service removes from PlayerContext, persists, fires event

## Components and Interfaces

### ReadOnlyShipTemplate (Existing --- No Changes)

Already complete. Exposes UUID, Name, OwnerUUID, HullBlueprintUUID as read-only properties. Components exposed as IReadOnlyList<ReadOnlyShipComponentSlot> (creates new wrappers on each access). Includes Equals/GetHashCode based on UUID and ToString returning Name.

### ReadOnlyShipComponentSlot (Existing --- No Changes)

Already complete. Exposes all 6 fields as read-only: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent. Equals uses reference equality on the underlying entity. ToString returns SlotType [SlotIndex].

### PlayerContext: FindMutableShipTemplate (New)

A new internal method reusing the existing _shipTemplateCache. Follows the same pattern as FindMutableBlueprint, FindMutableSurvey, FindMutableColony, and FindMutableDeliveryRoute. Marked internal so only the service project can access it. The existing public FindShipTemplate remains for other consumers. Reuses the same _shipTemplateCache that FindShipTemplate already builds and maintains.

### ShipTemplateViewModel (Edit Buffer)

The ViewModel is similar to DeliveryRouteViewModel but with 2 scalar fields instead of 1. The edit buffer covers Name, HullBlueprintUUID, and the Components list. Unlike Colony where structure operations are immediate service calls, all component operations accumulate in the ViewModel until Save.

Key differences from DeliveryRouteViewModel:
- **Two scalar fields**: Name and HullBlueprintUUID (vs DeliveryRoute's 1: Name).
- **Components instead of Stops**: ShipComponentSlot has 6 fields (SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).
- **Slot-based operations**: SetComponent and RemoveComponent by SlotType+SlotIndex, not ordered add/remove/reorder.
- **Hull change clears components**: ClearComponents method for when the hull changes.
- **No Data property**: No public property exposing the mutable ShipTemplate.
- **No PlayerContext dependency**: The ViewModel is a plain edit buffer with no service dependencies.
- **Deep-copy components on load**: LoadFrom creates new ShipComponentSlot instances to avoid sharing references with the entity.

Public interface:
- Properties: Name (get/set), HullBlueprintUUID (get/set), Components (List<ShipComponentSlot>), UUID, OwnerUUID, Original, IsNew, IsDirty
- LoadFrom(ReadOnlyShipTemplate): copies all fields, deep-copies Components, stores original snapshot
- Reset(): clears all fields to defaults, sets original to null
- BuildUpdateRequest(): creates ShipTemplateUpdateRequest from local state with deep-copied Components
- BuildCreateRequest(): creates ShipTemplateCreateRequest from local state with deep-copied Components
- SetComponent(slotType, slotIndex, blueprintUUID): adds or updates a component in the local list
- RemoveComponent(slotType, slotIndex): removes a component from the local list
- ClearComponents(): clears the local Components list (used when hull changes)

#### Dirty Tracking

IsDirty compares local state against the original ReadOnlyShipTemplate snapshot:
- Name string equality
- HullBlueprintUUID string equality
- Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values
- For new templates (original is null): returns true once Name has a non-empty value

### ShipTemplateService

Centralizes all ShipTemplate mutation. The form and ViewModel never touch the entity directly. Only this service (plus JSON deserialization and migration code) mutates ShipTemplate objects.

Key differences from DeliveryRouteService:
- **Two scalar fields**: Applies Name and HullBlueprintUUID (vs DeliveryRoute's Name only).
- **Components instead of Stops**: Deep-copies ShipComponentSlot list instead of RouteStop list.
- **No Sequence renumbering**: Components use SlotType+SlotIndex, not sequential Sequence numbers.
- **No write locks**: No ReaderWriterLockSlim. Single-threaded UI access only.
- **ShipTemplateDataChanged event**: Fires OnShipTemplateDataChanged instead of OnDeliveryDataChanged.

Public interface:
- Update(string uuid, ShipTemplateUpdateRequest): looks up mutable entity, applies fields, replaces Components, persists, fires event, returns ReadOnlyShipTemplate. Throws InvalidOperationException if UUID not found.
- Create(ShipTemplateCreateRequest): creates new entity with generated UUID, sets OwnerUUID from current player, populates fields from request, adds to PlayerContext, persists, fires event, returns ReadOnlyShipTemplate.
- Delete(string uuid): looks up mutable entity, removes from PlayerContext, persists, fires event. No-op if UUID is empty or not found.

### Unsaved Changes Prompt

The form checks viewModel.IsDirty before any operation that would discard the current edit buffer:

- **Selection change** --- user selects a different template in the list view
- **New** --- user clicks the New button
- **Form close** --- user closes the form (X button or MDI close)
- **Application exit** --- MainWindow closing propagates to all MDI children

All four paths use the same three-button dialog: **Save** | **Discard** | **Cancel**.

- **Save**: calls the appropriate service method (Create or Update), then proceeds with the original action.
- **Discard**: discards local changes and proceeds.
- **Cancel**: cancels the original action and keeps the current template selected.

For form close and application exit, Cancel sets e.Cancel = true to prevent the close.

### Delete Flow with Reference Protection

1. Check ShipTemplateReferenceCounter for references (ships, build items, stock targets).
2. If CountReferences > 0: show warning message with reference count, prevent deletion.
3. If CountReferences == 0: prompt for confirmation.
4. On confirm: call ShipTemplateService.Delete, clear form, refresh list.

### Order Build (Unchanged)

Order Build reads from the ViewModel's local state instead of the entity. Specifically:
- _viewModel.HullBlueprintUUID instead of _selectedTemplate.HullBlueprintUUID
- _viewModel.Components instead of _selectedTemplate.Components
- _viewModel.UUID instead of _selectedTemplate.UUID
- _viewModel.Name instead of _selectedTemplate.Name

Order Build does NOT mutate the ShipTemplate. It creates BuildItems and adds them to a BuildPlan. This behavior is unchanged.

## Data Models

### ShipTemplateUpdateRequest (New)

A plain DTO carrying the original snapshot and the current Name, HullBlueprintUUID, and Components state:

- Original (ReadOnlyShipTemplate): The original snapshot the edit was based on. Enables field-level dirty detection and optimistic concurrency.
- Name (string): The template name.
- HullBlueprintUUID (string): The hull blueprint UUID.
- Components (List<ShipComponentSlot>): The component slots.

### ShipTemplateCreateRequest (New)

A DTO for creating a new template. No Original snapshot (it does not exist yet). No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).

- Name (string): The template name.
- HullBlueprintUUID (string): The hull blueprint UUID.
- Components (List<ShipComponentSlot>): The component slots.

### ShipTemplate (Existing --- No Changes)

The existing ShipTemplate model is unchanged. It has scalar fields (UUID, Name, OwnerUUID, HullBlueprintUUID) and a nested List<ShipComponentSlot> Components collection. No concurrency locks. The service is the only code that mutates it after migration (except deserialization and migration code).

### ShipComponentSlot (Existing --- No Changes)

The existing ShipComponentSlot model is unchanged. Fields: SlotType (string), SlotIndex (int), BlueprintUUID (string), CurrentHP (int), MaxHP (int), MaxRepairPercent (decimal).

### ReadOnlyShipTemplate (Existing --- No Changes)

Already complete. No gap fill needed. Exposes UUID, Name, OwnerUUID, HullBlueprintUUID as read-only. Components as IReadOnlyList<ReadOnlyShipComponentSlot>.

### ReadOnlyShipComponentSlot (Existing --- No Changes)

Already complete. Exposes all 6 fields as read-only: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent.

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All Fields

*For any* valid ShipTemplate entity, wrapping in ReadOnlyShipTemplate and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, HullBlueprintUUID, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent).

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

### Property 2: IsDirty False Immediately After LoadFrom

*For any* valid ShipTemplate entity, wrapping in ReadOnlyShipTemplate and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates: Requirements 6.1, 6.5**

### Property 3: IsDirty Detects Name Change

*For any* valid ShipTemplate entity, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.2**

### Property 4: IsDirty Detects HullBlueprintUUID Change

*For any* valid ShipTemplate entity, after LoadFrom, changing the HullBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.3**

### Property 5: IsDirty Detects Components Change

*For any* valid ShipTemplate entity with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.4**

### Property 6: Service.Update Round-Trip

*For any* valid existing ShipTemplate entity and valid ShipTemplateUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyShipTemplate whose Name and HullBlueprintUUID match the request and whose Components match the request Components (same count, same field values per slot).

**Validates: Requirements 11.3, 11.4, 11.7**

### Property 7: Service.Create Round-Trip

*For any* valid ShipTemplateCreateRequest values, calling Service.Create SHALL produce a ReadOnlyShipTemplate whose Name and HullBlueprintUUID match the request, whose Components match the request Components, and whose UUID is non-empty.

**Validates: Requirements 12.2, 12.4, 12.8**

### Property 8: Service.Delete Removes Template

*For any* valid existing ShipTemplate entity, calling Service.Delete with the template UUID SHALL cause the template to no longer be findable via PlayerContext.

**Validates: Requirements 13.1, 13.2**

### Property 9: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct ShipTemplate property sets SHALL only find matches in ShipTemplateService, JSON deserialization, migration code, and the ShipTemplate class itself.

**Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2**

## Error Handling

### Validation

- **Empty template name**: The Save handler validates that Name is not empty or whitespace before calling the service. Shows a MessageBox warning.

### Service Errors

- **Update with non-existent UUID**: ShipTemplateService.Update throws InvalidOperationException. The form catches this and shows an error dialog.
- **Delete with empty/non-existent UUID**: ShipTemplateService.Delete returns silently (no error). Matches the existing service pattern.
- **Null request**: Both Update and Create throw ArgumentNullException for null requests.

### Unsaved Changes Edge Cases

- **Save fails during unsaved changes prompt**: If the service throws during the Save path of the unsaved changes dialog, the form catches the exception, shows an error, and cancels the original action (same as Cancel).
- **Concurrent modification**: Not applicable --- ShipTemplate has no background processing and no write locks. Single-threaded access from the UI thread only.

### Reference Protection

- **Delete with references**: ShipTemplateReferenceCounter checks ships, build items, and stock targets. If CountReferences > 0, deletion is blocked with a warning message showing the reference count.
- **Reference counter construction**: Takes IEnumerable of ships, build plans, and stock plans. Null-safe.

## Testing Strategy

### Dual Testing Approach

- **Property-based tests** (FsCheck + NUnit): Verify universal properties across randomly generated ShipTemplate inputs. Minimum 25 iterations per property test, following the established pattern.
- **Unit tests** (NUnit): Verify specific examples, edge cases, and error conditions.

### Property-Based Tests

**Library**: FsCheck 2.x with FsCheck.NUnit integration (already in the project).

**Generator**: A ValidShipTemplateGen() generator that produces random ShipTemplate entities with:
- Random string Name
- Random UUID, OwnerUUID, HullBlueprintUUID
- Random number of ShipComponentSlot entries (0 to 10), each with random SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent

**Test files**:

1. OE2EmpireTracker.Tests/ViewModels/ShipTemplateViewModelPropertyTests.cs
   - Property 1: LoadFrom round-trip preserves all fields
   - Property 2: IsDirty false immediately after LoadFrom
   - Property 3: IsDirty detects Name change
   - Property 4: IsDirty detects HullBlueprintUUID change
   - Property 5: IsDirty detects Components change

2. OE2EmpireTracker.Tests/Services/ShipTemplateServicePropertyTests.cs
   - Property 6: Service.Update round-trip
   - Property 7: Service.Create round-trip
   - Property 8: Service.Delete removes template

**Configuration**: [FsCheck.NUnit.Property(MaxTest = 25)] for round-trip and IsDirty-false tests, [FsCheck.NUnit.Property(MaxTest = 50)] for the field-change and components-change tests.

### Unit Tests

**Test file**: OE2EmpireTracker.Tests/Services/ShipTemplateServiceTests.cs

- Update with non-existent UUID throws InvalidOperationException
- Delete with empty UUID returns without error
- Delete with non-existent UUID returns without error
- Create assigns non-empty UUID
- Create sets OwnerUUID to current player UUID
- Update fires ShipTemplateDataChanged event
- Create fires ShipTemplateDataChanged event
- Delete fires ShipTemplateDataChanged event
- Update replaces Components list
- Create populates Components list from request

**Test file**: OE2EmpireTracker.Tests/ViewModels/ShipTemplateViewModelTests.cs

- Reset clears all fields to defaults
- IsNew returns true after Reset
- IsNew returns false after LoadFrom
- IsDirty returns true for new template with non-empty Name
- BuildUpdateRequest copies Name, HullBlueprintUUID, and Components
- BuildCreateRequest copies Name, HullBlueprintUUID, and Components
- UUID and OwnerUUID are preserved from LoadFrom
- SetComponent adds new component to local list
- SetComponent updates existing component in local list
- RemoveComponent removes component from local list
- ClearComponents empties the local Components list

### Mutation Guard Test

**Test file**: OE2EmpireTracker.Tests/Services/ShipTemplateMutationGuardTests.cs

A static analysis test (following the pattern of BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests, etc.) that greps the codebase for direct ShipTemplate property sets and asserts they only appear in:

- ShipTemplateService.cs (the sole mutator)
- ShipTemplate.cs (the model class itself, default values)
- PlayerContext.cs (deserialization/migration)
- Test files (test setup)

Checks for:
- ShipTemplate.Name = (property set)
- ShipTemplate.UUID = (property set)
- ShipTemplate.OwnerUUID = (property set)
- ShipTemplate.HullBlueprintUUID = (property set)
- ShipTemplate.Components mutation (Components.Add, Components.Remove, Components.Clear, Components =)
- ShipComponentSlot property sets outside allowed files

This validates Property 9 and Requirements 17.1, 17.2, 17.3, 20.1, 20.2.
