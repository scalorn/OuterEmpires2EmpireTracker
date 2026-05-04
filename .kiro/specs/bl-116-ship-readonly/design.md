# BL-116 Design: Ship Immutable Data Model with Service Layer

## Overview

BL-116 applies the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), BL-115 (ship templates), and BL-123 (pricing plans) to the FormShipInstance form. The form stops directly mutating Ship entities. The ViewModel becomes a disconnected edit buffer for all ship fields including Components, Cargo, and Hopper. A new ShipService is the sole mutator of Ship entities.

### Key Differences from BL-115 (ShipTemplate)

1. **More complex entity** --- Ship has 5 scalar fields (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID) plus hull HP fields (HullCurrentHP, HullMaxHP, HullMaxRepairPercent) vs ShipTemplate's 2 (Name, HullBlueprintUUID). The edit buffer covers all scalars, hull HP, Components, Cargo, and Hopper.
2. **Cargo and Hopper** --- Ship has two ItemBag collections. The ViewModel deep-copies these on load and buffers add/remove operations.
3. **Location fields** --- LocationType (enum) and LocationUUID (string) are additional scalar fields in the edit buffer.
4. **Hull HP fields** --- HullCurrentHP, HullMaxHP, HullMaxRepairPercent are additional numeric fields in the edit buffer.
5. **Create from Template** --- ShipService.CreateFromTemplate creates a Ship from a ShipTemplate.
6. **No Order Build** --- FormShipInstance does not have an Order Build feature.
7. **ShipDataChanged event** --- Fires OnShipDataChanged instead of OnShipTemplateDataChanged.
8. **No write locks** --- No ReaderWriterLockSlim. The service mutates directly.

### Similarities to BL-115

1. **Scalar fields** --- Name and HullBlueprintUUID managed by the ViewModel edit buffer.
2. **Single form** --- FormShipInstance manages the full CRUD lifecycle.
3. **List view with filters** --- Ship list has a text filter.
4. **Always player-scoped** --- Ships owned by the current player.
5. **Delete reference protection** --- ShipReferenceCounter checks delivery plans and build items.
6. **Service as sole mutator** --- form -> ViewModel -> service -> entity.
7. **Components are part of the edit buffer** --- Like ShipTemplate, component changes accumulate until Save.
8. **ReadOnly wrappers already complete** --- ReadOnlyShip, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are fully implemented.

## Architecture

### Current Architecture

The form holds a direct mutable Ship reference (_selectedShip). Every control change writes directly to the entity:
- TxtName_TextChanged sets _selectedShip.Name
- CmbHull_SelectedItemChanged sets _selectedShip.HullBlueprintUUID and clears _selectedShip.Components
- CmbLocationType_SelectedIndexChanged sets _selectedShip.LocationType
- DgvComponents_CellEndEdit sets _selectedShip.HullCurrentHP, HullMaxRepairPercent, or slot CurrentHP/MaxRepairPercent
- DgvComponents_CellValueChanged adds/removes ShipComponentSlot entries in _selectedShip.Components
- CmdAddItem_Click adds items to _selectedShip.Cargo or _selectedShip.Hopper
- CmdRemoveItem_Click removes items from _selectedShip.Cargo or _selectedShip.Hopper
- CmdSave_Click sets _selectedShip.Name, _selectedShip.LocationUUID, and calls WriteContext()
- CmdNew_Click creates a new Ship, adds to PlayerContext, calls WriteContext()
- CmdFromTemplate_Click creates a Ship from a template, adds to PlayerContext, calls WriteContext()
- CmdDelete_Click removes from PlayerContext, calls WriteContext()

### Target Architecture

The form never touches the entity. The ViewModel is a disconnected edit buffer for all ship fields. The service is the only code that mutates the entity. All operations accumulate in the ViewModel's local state until Save.

### Data Flow

Load Flow:
1. Form calls PlayerContext.GetCurrentPlayerReadOnlyShips()
2. Form stores ReadOnlyShip in ListViewItem Tags
3. On selection, Form calls ViewModel.LoadFrom(ReadOnlyShip)
4. ViewModel copies all scalar fields, deep-copies Components, Cargo, and Hopper into local state

Edit Flow (Name):
1. Form sets ViewModel.Name (local property)
2. ViewModel updates local Name only (no entity mutation)

Edit Flow (Hull):
1. Form sets ViewModel.HullBlueprintUUID (local property)
2. Form calls ViewModel.ClearComponents()
3. ViewModel updates local state only (no entity mutation)

Edit Flow (Location):
1. Form sets ViewModel.LocationType and/or ViewModel.LocationUUID
2. ViewModel updates local state only (no entity mutation)

Edit Flow (Components):
1. Form calls ViewModel.SetComponent(slotType, slotIndex, blueprintUUID) or ViewModel.RemoveComponent(slotType, slotIndex)
2. ViewModel modifies local Components list only (no entity mutation)

Edit Flow (Hull HP):
1. Form sets ViewModel.HullCurrentHP or ViewModel.HullMaxRepairPercent
2. ViewModel updates local state only (no entity mutation)

Edit Flow (Cargo/Hopper):
1. Form calls ViewModel.AddCargoItem(item) or ViewModel.RemoveCargoItem(uuid) (or Hopper equivalents)
2. ViewModel modifies local Cargo or Hopper only (no entity mutation)

Save Flow (existing ship):
1. Form calls ViewModel.BuildUpdateRequest()
2. Form calls Service.Update(uuid, request)
3. Service looks up mutable entity via FindMutableShip
4. Service applies all scalar fields, replaces Components, Cargo, and Hopper
5. Service persists via WriteContext(), fires ShipDataChanged
6. Service returns ReadOnlyShip
7. Form reloads ViewModel from fresh ReadOnlyShip

Save Flow (new ship):
1. Form calls ViewModel.BuildCreateRequest()
2. Form calls Service.Create(request)
3. Service creates new Ship with UUID, OwnerUUID, Name
4. Service adds to PlayerContext, persists, fires event
5. Service returns ReadOnlyShip
6. Form reloads ViewModel from fresh ReadOnlyShip

Create from Template Flow:
1. Form shows template selection dialog
2. Form calls Service.CreateFromTemplate(templateUUID)
3. Service looks up ShipTemplate, creates Ship with copied fields
4. Service adds to PlayerContext, persists, fires event
5. Service returns ReadOnlyShip
6. Form reloads ViewModel from fresh ReadOnlyShip

Delete Flow:
1. Form checks ShipReferenceCounter
2. If references exist, show warning and block
3. If no references, prompt for confirmation
4. Form calls Service.Delete(uuid)
5. Service removes from PlayerContext, persists, fires event

## Components and Interfaces

### ReadOnlyShip (Existing --- No Changes)

Already complete. Exposes UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent as read-only properties. Components exposed as IReadOnlyList<ReadOnlyShipComponentSlot>. Cargo and Hopper exposed as ReadOnlyItemBag. Includes Equals/GetHashCode based on UUID and ToString returning Name.

### ReadOnlyShipComponentSlot (Existing --- No Changes)

Already complete. Exposes all 6 fields as read-only: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent.

### ReadOnlyItem (Existing --- No Changes)

Already complete. Exposes UUID, ItemType, BaseItemTypeID, Name, NickName, Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, Contents, ExtendedName as read-only.

### ReadOnlyItemBag (Existing --- No Changes)

Already complete. Exposes Count, ContainsKey, CountByType, FindByType, FindResource as read-only query methods. Does not expose AddItem, Remove, Clear, or the Items dictionary.

### PlayerContext: FindMutableShip (New)

A new internal method reusing the existing _shipCache. Follows the same pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute. Marked internal so only the service project can access it. The existing public FindShip remains for other consumers. Reuses the same _shipCache that FindShip already builds and maintains.

### ShipViewModel (Edit Buffer)

The ViewModel is similar to ShipTemplateViewModel but with many more fields. The edit buffer covers Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Cargo, and Hopper.

Key differences from ShipTemplateViewModel:
- **More scalar fields**: Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent (8 fields vs ShipTemplate's 2).
- **Cargo and Hopper**: Two ItemBag collections that need deep-copy on load and dirty tracking.
- **Location fields**: LocationType (DestinationType enum) and LocationUUID (string).
- **Hull HP fields**: HullCurrentHP (int), HullMaxHP (int), HullMaxRepairPercent (decimal).
- **No Data property**: No public property exposing the mutable Ship.
- **No PlayerContext dependency**: The ViewModel is a plain edit buffer with no service dependencies.
- **Deep-copy on load**: LoadFrom creates new ShipComponentSlot instances and new ItemBag/Item instances.

Public interface:
- Properties: Name (get/set), TemplateUUID (get/set), HullBlueprintUUID (get/set), LocationType (get/set), LocationUUID (get/set), HullCurrentHP (get/set), HullMaxHP (get/set), HullMaxRepairPercent (get/set), Components (List<ShipComponentSlot>), Cargo (ItemBag), Hopper (ItemBag), UUID, OwnerUUID, Original, IsNew, IsDirty
- LoadFrom(ReadOnlyShip): copies all fields, deep-copies Components/Cargo/Hopper, stores original snapshot
- Reset(): clears all fields to defaults, sets original to null
- BuildUpdateRequest(): creates ShipUpdateRequest from local state with deep-copied collections
- BuildCreateRequest(): creates ShipCreateRequest from local state
- SetComponent(slotType, slotIndex, blueprintUUID): adds or updates a component in the local list
- RemoveComponent(slotType, slotIndex): removes a component from the local list
- ClearComponents(): clears the local Components list (used when hull changes)
- SetHullComponent(currentHP, maxRepairPercent): updates hull HP fields
- SetComponentCondition(slotType, slotIndex, currentHP, maxRepairPercent): updates component HP fields
- AddCargoItem(item): adds an item to the local Cargo bag
- RemoveCargoItem(uuid): removes an item from the local Cargo bag
- AddHopperItem(item): adds an item to the local Hopper bag
- RemoveHopperItem(uuid): removes an item from the local Hopper bag
- GetSelectedBag(isHopper): returns Cargo or Hopper based on flag

#### Dirty Tracking

IsDirty compares local state against the original ReadOnlyShip snapshot:
- Name string equality
- TemplateUUID string equality
- HullBlueprintUUID string equality
- LocationType enum equality
- LocationUUID string equality
- HullCurrentHP, HullMaxHP, HullMaxRepairPercent numeric equality
- Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values
- Cargo ItemBag: same item count, and for each item the same key fields
- Hopper ItemBag: same item count, and for each item the same key fields
- For new ships (original is null): returns true once Name has a non-empty value

#### ItemBag Deep Copy

Deep-copying an ItemBag for the edit buffer requires creating a new ItemBag and adding new Item instances with all fields copied. For items with Contents (crates), the Contents ItemBag is also deep-copied recursively. This ensures the ViewModel's local Cargo/Hopper are fully disconnected from the entity.

### ShipService

Centralizes all Ship mutation. The form and ViewModel never touch the entity directly. Only this service (plus JSON deserialization and migration code) mutates Ship objects.

Key differences from ShipTemplateService:
- **More scalar fields**: Applies Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent.
- **Cargo and Hopper**: Deep-copies ItemBag collections (clear existing, add new items).
- **CreateFromTemplate**: Additional method that creates a Ship from a ShipTemplate.
- **No write locks**: No ReaderWriterLockSlim. Single-threaded UI access only.
- **ShipDataChanged event**: Fires OnShipDataChanged instead of OnShipTemplateDataChanged.

Public interface:
- Update(string uuid, ShipUpdateRequest): looks up mutable entity, applies all fields, replaces Components/Cargo/Hopper, persists, fires event, returns ReadOnlyShip. Throws InvalidOperationException if UUID not found.
- Create(ShipCreateRequest): creates new entity with generated UUID, sets OwnerUUID from current player, populates Name, adds to PlayerContext, persists, fires event, returns ReadOnlyShip.
- Delete(string uuid): looks up mutable entity, removes from PlayerContext, persists, fires event. No-op if UUID is empty or not found.
- CreateFromTemplate(string templateUUID): looks up ShipTemplate, creates new Ship with copied fields, adds to PlayerContext, persists, fires event, returns ReadOnlyShip. Throws InvalidOperationException if template not found.

### Unsaved Changes Prompt

The form checks viewModel.IsDirty before any operation that would discard the current edit buffer:

- **Selection change** --- user selects a different ship in the list view
- **New** --- user clicks the New button
- **Create from Template** --- user clicks the Create from Template button
- **Form close** --- user closes the form (X button or MDI close)
- **Application exit** --- MainWindow closing propagates to all MDI children

All paths use the same three-button dialog: **Save** | **Discard** | **Cancel**.

- **Save**: calls the appropriate service method (Create or Update), then proceeds with the original action.
- **Discard**: discards local changes and proceeds.
- **Cancel**: cancels the original action and keeps the current ship selected.

For form close and application exit, Cancel sets e.Cancel = true to prevent the close.

### Delete Flow with Reference Protection

1. Check ShipReferenceCounter for references (delivery plans, build items).
2. If CountReferences > 0: show warning message with reference count, prevent deletion.
3. If CountReferences == 0: prompt for confirmation.
4. On confirm: call ShipService.Delete, clear form, refresh list.

## Data Models

### ShipUpdateRequest (New)

A plain DTO carrying the original snapshot and the current state:

- Original (ReadOnlyShip): The original snapshot the edit was based on.
- Name (string): The ship name.
- TemplateUUID (string): The template UUID.
- HullBlueprintUUID (string): The hull blueprint UUID.
- LocationType (DestinationType): The location type.
- LocationUUID (string): The location UUID.
- HullCurrentHP (int): The hull current HP.
- HullMaxHP (int): The hull max HP.
- HullMaxRepairPercent (decimal): The hull max repair percent.
- Components (List<ShipComponentSlot>): The component slots.
- Cargo (ItemBag): The cargo items.
- Hopper (ItemBag): The hopper items.

### ShipCreateRequest (New)

A DTO for creating a new blank ship. No Original snapshot. No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).

- Name (string): The ship name (defaults to 'New Ship' if empty).

### Ship (Existing --- No Changes)

The existing Ship model is unchanged. It has scalar fields (UUID, Name, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent), a nested List<ShipComponentSlot> Components collection, and two ItemBag collections (Cargo, Hopper). No concurrency locks. The service is the only code that mutates it after migration.

### ShipComponentSlot (Existing --- No Changes)

The existing ShipComponentSlot model is unchanged. Fields: SlotType (string), SlotIndex (int), BlueprintUUID (string), CurrentHP (int), MaxHP (int), MaxRepairPercent (decimal).

### Item (Existing --- No Changes)

The existing Item model is unchanged. Fields: UUID, ItemType, BaseItemTypeID, Name, NickName, Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, Contents (optional nested ItemBag for crates).

### ItemBag (Existing --- No Changes)

The existing ItemBag model is unchanged. Dictionary-based container keyed by UUID with AddItem, Remove, Clear, Count, ContainsKey, CountByType, FindByType, FindResource methods.

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All Fields

*For any* valid Ship entity, wrapping in ReadOnlyShip and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, TemplateUUID, HullBlueprintUUID, LocationType, LocationUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent, and every Item in Cargo and Hopper with matching UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent).

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9**

### Property 2: IsDirty False Immediately After LoadFrom

*For any* valid Ship entity, wrapping in ReadOnlyShip and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates: Requirements 6.1, 6.11**

### Property 3: IsDirty Detects Name Change

*For any* valid Ship entity, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.2**

### Property 4: IsDirty Detects HullBlueprintUUID Change

*For any* valid Ship entity, after LoadFrom, changing the HullBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.4**

### Property 5: IsDirty Detects Components Change

*For any* valid Ship entity with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.8**

### Property 6: IsDirty Detects Location Change

*For any* valid Ship entity, after LoadFrom, changing the LocationType or LocationUUID field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.5, 6.6**

### Property 7: IsDirty Detects Cargo Change

*For any* valid Ship entity with at least one cargo item, after LoadFrom, adding or removing a cargo item SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.9**

### Property 8: Service.Update Round-Trip

*For any* valid existing Ship entity and valid ShipUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyShip whose scalar fields match the request and whose Components, Cargo, and Hopper match the request collections.

**Validates: Requirements 11.3, 11.4, 11.5, 11.6, 11.9**

### Property 9: Service.Create Round-Trip

*For any* valid ShipCreateRequest values, calling Service.Create SHALL produce a ReadOnlyShip whose Name matches the request and whose UUID is non-empty.

**Validates: Requirements 12.2, 12.4, 12.8**

### Property 10: Service.Delete Removes Ship

*For any* valid existing Ship entity, calling Service.Delete with the ship UUID SHALL cause the ship to no longer be findable via PlayerContext.

**Validates: Requirements 13.1, 13.2**

### Property 11: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Ship property sets SHALL only find matches in ShipService, JSON deserialization, migration code, and the Ship class itself.

**Validates: Requirements 18.1, 18.2, 18.3, 21.1, 21.2, 21.3**

## Error Handling

### Validation

- **Empty ship name**: The Save handler validates that Name is not empty or whitespace before calling the service. Shows a MessageBox warning.

### Service Errors

- **Update with non-existent UUID**: ShipService.Update throws InvalidOperationException. The form catches this and shows an error dialog.
- **Delete with empty/non-existent UUID**: ShipService.Delete returns silently (no error). Matches the existing service pattern.
- **CreateFromTemplate with non-existent template UUID**: ShipService.CreateFromTemplate throws InvalidOperationException.
- **Null request**: Both Update and Create throw ArgumentNullException for null requests.

### Unsaved Changes Edge Cases

- **Save fails during unsaved changes prompt**: If the service throws during the Save path of the unsaved changes dialog, the form catches the exception, shows an error, and cancels the original action (same as Cancel).
- **Concurrent modification**: Not applicable --- Ship has no background processing and no write locks. Single-threaded access from the UI thread only.

### Reference Protection

- **Delete with references**: ShipReferenceCounter checks delivery plans and build items. If CountReferences > 0, deletion is blocked with a warning message showing the reference count.
- **Reference counter construction**: Takes IEnumerable of delivery plans and build plans. Null-safe.

## Testing Strategy

### Dual Testing Approach

- **Property-based tests** (FsCheck + NUnit): Verify universal properties across randomly generated Ship inputs. Minimum 25 iterations per property test, following the established pattern.
- **Unit tests** (NUnit): Verify specific examples, edge cases, and error conditions.

### Property-Based Tests

**Library**: FsCheck 2.x with FsCheck.NUnit integration (already in the project).

**Generator**: A ValidShipGen() generator that produces random Ship entities with:
- Random string Name, UUID, OwnerUUID, TemplateUUID, HullBlueprintUUID, LocationUUID
- Random DestinationType LocationType
- Random int HullCurrentHP, HullMaxHP, decimal HullMaxRepairPercent
- Random number of ShipComponentSlot entries (0 to 10), each with random SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent
- Random Cargo ItemBag with 0 to 5 items, each with random UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent
- Random Hopper ItemBag with 0 to 5 items (same structure)

**Test files**:

1. OE2EmpireTracker.Tests/ViewModels/ShipViewModelPropertyTests.cs
   - Property 1: LoadFrom round-trip preserves all fields
   - Property 2: IsDirty false immediately after LoadFrom
   - Property 3: IsDirty detects Name change
   - Property 4: IsDirty detects HullBlueprintUUID change
   - Property 5: IsDirty detects Components change
   - Property 6: IsDirty detects Location change
   - Property 7: IsDirty detects Cargo change

2. OE2EmpireTracker.Tests/Services/ShipServicePropertyTests.cs
   - Property 8: Service.Update round-trip
   - Property 9: Service.Create round-trip
   - Property 10: Service.Delete removes ship

**Configuration**: [FsCheck.NUnit.Property(MaxTest = 25)] for round-trip and IsDirty-false tests, [FsCheck.NUnit.Property(MaxTest = 50)] for the field-change tests.

### Unit Tests

**Test file**: OE2EmpireTracker.Tests/Services/ShipServiceTests.cs

- Update with non-existent UUID throws InvalidOperationException
- Delete with empty UUID returns without error
- Delete with non-existent UUID returns without error
- Create assigns non-empty UUID
- Create sets OwnerUUID to current player UUID
- Update fires ShipDataChanged event
- Create fires ShipDataChanged event
- Delete fires ShipDataChanged event
- Update replaces Components list
- Update replaces Cargo and Hopper
- Create populates Name from request
- CreateFromTemplate copies fields from template
- CreateFromTemplate with non-existent template throws InvalidOperationException

**Test file**: OE2EmpireTracker.Tests/ViewModels/ShipViewModelTests.cs

- Reset clears all fields to defaults
- IsNew returns true after Reset
- IsNew returns false after LoadFrom
- IsDirty returns true for new ship with non-empty Name
- BuildUpdateRequest copies all fields and collections
- BuildCreateRequest copies Name
- UUID and OwnerUUID are preserved from LoadFrom
- SetComponent adds new component to local list
- SetComponent updates existing component in local list
- RemoveComponent removes component from local list
- ClearComponents empties the local Components list
- AddCargoItem adds item to local Cargo
- RemoveCargoItem removes item from local Cargo
- AddHopperItem adds item to local Hopper
- RemoveHopperItem removes item from local Hopper
- LocationType and LocationUUID are preserved from LoadFrom
- HullCurrentHP, HullMaxHP, HullMaxRepairPercent are preserved from LoadFrom

### Mutation Guard Test

**Test file**: OE2EmpireTracker.Tests/Services/ShipMutationGuardTests.cs

A static analysis test (following the pattern of BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests, ShipTemplateMutationGuardTests) that greps the codebase for direct Ship property sets and asserts they only appear in:

- ShipService.cs (the sole mutator)
- Ship.cs (the model class itself, default values)
- PlayerContext.cs (deserialization/migration)
- Test files (test setup)

Checks for:
- Ship.Name = (property set)
- Ship.UUID = (property set)
- Ship.OwnerUUID = (property set)
- Ship.TemplateUUID = (property set)
- Ship.HullBlueprintUUID = (property set)
- Ship.LocationType = (property set)
- Ship.LocationUUID = (property set)
- Ship.HullCurrentHP = (property set)
- Ship.HullMaxHP = (property set)
- Ship.HullMaxRepairPercent = (property set)
- Ship.Components mutation (Components.Add, Components.Remove, Components.Clear, Components =)
- Ship.Cargo mutation (Cargo.AddItem, Cargo.Remove, Cargo.Clear, Cargo =)
- Ship.Hopper mutation (Hopper.AddItem, Hopper.Remove, Hopper.Clear, Hopper =)
- ShipComponentSlot property sets outside allowed files

This validates Property 11 and Requirements 18.1, 18.2, 18.3, 21.1, 21.2, 21.3.
