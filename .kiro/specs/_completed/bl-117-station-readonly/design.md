# BL-117 Design: Station Immutable Data Model with Service Layer

## Overview

BL-117 applies the same immutable data model pattern established in BL-108 (blueprints), BL-109 (colonies), BL-110 (surveys), BL-111 (player profiles), BL-112 (delivery routes), BL-115 (ship templates), BL-116 (ships), and BL-123 (pricing plans) to the FormStation form. The form stops directly mutating Station entities. The ViewModel becomes a disconnected edit buffer for all station fields including Components, Hold, and MunitionsHold. A new StationService is the sole mutator of Station entities.

### Key Differences from BL-116 (Ship)

1. **Holds instead of Cargo** --- Station has a Dictionary of ItemBag Holds (keyed by player UUID) instead of Ship's single Cargo ItemBag. The ViewModel buffers only the current player's hold.
2. **MunitionsHold instead of Hopper** --- Station has a single MunitionsHold ItemBag instead of Ship's Hopper. Similar pattern but different name.
3. **StationType and Ownership** --- Station has StationType (enum) and Ownership (enum) fields that Ship does not have.
4. **StationBlueprintUUID** --- Station uses StationBlueprintUUID instead of Ship's HullBlueprintUUID and TemplateUUID.
5. **No LocationType/LocationUUID** --- Station does not have location fields.
6. **No TemplateUUID** --- Station does not have a template reference.
7. **No Create from Template** --- StationService does not have a CreateFromTemplate method.
8. **StationDataChanged event** --- Fires OnStationDataChanged instead of OnShipDataChanged.
9. **No write locks** --- No ReaderWriterLockSlim. The service mutates directly.

### Similarities to BL-116

1. **Components list** --- Both have List of ShipComponentSlot managed by the ViewModel edit buffer.
2. **Hull HP fields** --- Both have HullCurrentHP, HullMaxHP, HullMaxRepairPercent.
3. **Single form** --- FormStation manages the full CRUD lifecycle.
4. **List view with filters** --- Station list has a text filter.
5. **Service as sole mutator** --- form -> ViewModel -> service -> entity.
6. **ReadOnly wrappers already complete** --- ReadOnlyStation, ReadOnlyShipComponentSlot, ReadOnlyItem, and ReadOnlyItemBag are fully implemented.
7. **Delete reference protection** --- StationReferenceCounter for delete protection.

## Architecture

### Current Architecture

The form holds a direct mutable Station reference (_selectedStation). Every control change writes directly to the entity:
- TxtName_TextChanged sets _selectedStation.Name
- CmbStationType_SelectedIndexChanged sets _selectedStation.StationType
- CmbOwnership_SelectedIndexChanged sets _selectedStation.Ownership
- CmbStationBlueprint_SelectedIndexChanged sets _selectedStation.StationBlueprintUUID
- DgvComponents_CellEndEdit sets _selectedStation.HullCurrentHP, HullMaxRepairPercent, or slot CurrentHP/MaxRepairPercent
- DgvHold_CellEndEdit sets item CurrentHP/MaxRepairPercent in the hold
- CmdHoldAdd_Click adds items to the current player's hold in _selectedStation.Holds
- CmdHoldRemove_Click removes items from the current player's hold
- CmdMunAdd_Click adds items to _selectedStation.MunitionsHold
- CmdMunRemove_Click removes items from _selectedStation.MunitionsHold
- CmdSave_Click sets _selectedStation.Name and calls WriteContext()
- CmdNew_Click creates a new Station, adds to PlayerContext, calls WriteContext()
- CmdDelete_Click removes from PlayerContext, calls WriteContext()
### Target Architecture

The form never touches the entity. The ViewModel is a disconnected edit buffer for all station fields. The service is the only code that mutates the entity. All operations accumulate in the ViewModel's local state until Save.

### Data Flow

Load Flow:
1. Form calls PlayerContext.GetCurrentPlayerReadOnlyStations()
2. Form stores ReadOnlyStation in ListViewItem Tags
3. On selection, Form calls ViewModel.LoadFrom(ReadOnlyStation)
4. ViewModel copies all scalar fields, deep-copies Components, Hold, and MunitionsHold into local state

Edit Flow (Name):
1. Form sets ViewModel.Name (local property)
2. ViewModel updates local Name only (no entity mutation)

Edit Flow (StationType):
1. Form sets ViewModel.StationType (local property)
2. ViewModel updates local state only (no entity mutation)

Edit Flow (Ownership):
1. Form sets ViewModel.Ownership (local property)
2. ViewModel updates local state only (no entity mutation)
3. Form updates tab enabled states based on Ownership value

Edit Flow (StationBlueprint):
1. Form sets ViewModel.StationBlueprintUUID (local property)
2. Form calls ViewModel.ClearComponents()
3. ViewModel updates local state only (no entity mutation)

Edit Flow (Components):
1. Form calls ViewModel.SetComponentCondition(slotType, slotIndex, currentHP, maxRepairPercent)
2. ViewModel modifies local Components list only (no entity mutation)

Edit Flow (Hull HP):
1. Form sets ViewModel.HullCurrentHP or ViewModel.HullMaxRepairPercent
2. ViewModel updates local state only (no entity mutation)

Edit Flow (Hold):
1. Form calls ViewModel.AddHoldItem(item) or ViewModel.RemoveHoldItem(uuid)
2. ViewModel modifies local Hold only (no entity mutation)

Edit Flow (MunitionsHold):
1. Form calls ViewModel.AddMunitionsItem(item) or ViewModel.RemoveMunitionsItem(uuid)
2. ViewModel modifies local MunitionsHold only (no entity mutation)
Save Flow (existing station):
1. Form calls ViewModel.BuildUpdateRequest()
2. Form calls Service.Update(uuid, request)
3. Service looks up mutable entity via FindMutableStation
4. Service applies all scalar fields, replaces Components, Hold, and MunitionsHold
5. Service persists via WriteContext(), fires StationDataChanged
6. Service returns ReadOnlyStation
7. Form reloads ViewModel from fresh ReadOnlyStation

Save Flow (new station):
1. Form calls ViewModel.BuildCreateRequest()
2. Form calls Service.Create(request)
3. Service creates new Station with UUID, OwnerUUID, Name
4. Service adds to PlayerContext, persists, fires event
5. Service returns ReadOnlyStation
6. Form reloads ViewModel from fresh ReadOnlyStation

Delete Flow:
1. Form checks StationReferenceCounter
2. If references exist, show warning and block
3. If no references, prompt for confirmation
4. Form calls Service.Delete(uuid)
5. Service removes from PlayerContext, persists, fires event

## Components and Interfaces

### ReadOnlyStation (Existing --- No Changes)

Already complete. Exposes UUID, Name, OwnerUUID, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent as read-only properties. Components exposed as IReadOnlyList of ReadOnlyShipComponentSlot. Holds exposed as IReadOnlyDictionary of ReadOnlyItemBag. MunitionsHold exposed as ReadOnlyItemBag. Includes Equals/GetHashCode based on UUID and ToString returning Name.

### ReadOnlyShipComponentSlot (Existing --- No Changes)

Already complete. Exposes all 6 fields as read-only: SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent.

### ReadOnlyItem (Existing --- No Changes)

Already complete. Exposes UUID, ItemType, BaseItemTypeID, Name, NickName, Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, Contents, ExtendedName as read-only.

### ReadOnlyItemBag (Existing --- No Changes)

Already complete. Exposes Count, ContainsKey, CountByType, FindByType, FindResource as read-only query methods. Does not expose AddItem, Remove, Clear, or the Items dictionary.
### PlayerContext: FindMutableStation (New)

A new internal method reusing the existing _stationCache. Follows the same pattern as FindMutableBlueprint, FindMutableColony, and FindMutableDeliveryRoute. Marked internal so only the service project can access it. The existing public FindStation remains for other consumers. Reuses the same _stationCache that FindStation already builds and maintains.

### StationViewModel (Edit Buffer)

The ViewModel is similar to ShipViewModel but with different fields. The edit buffer covers Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, Components, Hold, and MunitionsHold.

Key differences from ShipViewModel:
- **StationType and Ownership**: Two enum fields (StationType, StationOwnership) instead of Ship's LocationType/LocationUUID.
- **StationBlueprintUUID**: Single blueprint field instead of Ship's HullBlueprintUUID + TemplateUUID.
- **Hold instead of Cargo**: The hold is the current player's ItemBag from the station's Holds dictionary.
- **MunitionsHold instead of Hopper**: Same pattern, different name.
- **No LocationType/LocationUUID**: Station does not have location fields.
- **No TemplateUUID**: Station does not have a template reference.
- **No Data property**: No public property exposing the mutable Station.
- **No PlayerContext dependency**: The ViewModel is a plain edit buffer with no service dependencies.
- **Deep-copy on load**: LoadFrom creates new ShipComponentSlot instances and new ItemBag/Item instances.

Public interface:
- Properties: Name (get/set), StationType (get/set), Ownership (get/set), StationBlueprintUUID (get/set), HullCurrentHP (get/set), HullMaxHP (get/set), HullMaxRepairPercent (get/set), Components (List of ShipComponentSlot), Hold (ItemBag), MunitionsHold (ItemBag), UUID, OwnerUUID, Original, IsNew, IsDirty
- LoadFrom(ReadOnlyStation, string playerUUID): copies all fields, deep-copies Components/Hold/MunitionsHold, stores original snapshot
- Reset(): clears all fields to defaults, sets original to null
- BuildUpdateRequest(): creates StationUpdateRequest from local state with deep-copied collections
- BuildCreateRequest(): creates StationCreateRequest from local state
- SetComponentCondition(slotType, slotIndex, currentHP, maxRepairPercent): updates component HP fields
- SetHullComponent(currentHP, maxRepairPercent): updates hull HP fields
- ClearComponents(): clears the local Components list (used when blueprint changes)
- AddHoldItem(item): adds an item to the local Hold bag
- RemoveHoldItem(uuid): removes an item from the local Hold bag
- AddMunitionsItem(item): adds an item to the local MunitionsHold bag
- RemoveMunitionsItem(uuid): removes an item from the local MunitionsHold bag
#### Dirty Tracking

IsDirty compares local state against the original ReadOnlyStation snapshot:
- Name string equality
- StationType enum equality
- Ownership enum equality
- StationBlueprintUUID string equality
- HullCurrentHP, HullMaxHP, HullMaxRepairPercent numeric equality
- Components list: same count, and for each index the same SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, and MaxRepairPercent values
- Hold ItemBag: same item count, and for each item the same key fields
- MunitionsHold ItemBag: same item count, and for each item the same key fields
- For new stations (original is null): returns true once Name has a non-empty value

#### ItemBag Deep Copy

Deep-copying an ItemBag for the edit buffer requires creating a new ItemBag and adding new Item instances with all fields copied. For items with Contents (crates), the Contents ItemBag is also deep-copied recursively. This ensures the ViewModel's local Hold/MunitionsHold are fully disconnected from the entity.

### StationService

Centralizes all Station mutation. The form and ViewModel never touch the entity directly. Only this service (plus JSON deserialization and migration code) mutates Station objects.

Key differences from ShipService:
- **StationType and Ownership**: Applies StationType and Ownership enum fields instead of LocationType/LocationUUID.
- **StationBlueprintUUID**: Single blueprint field instead of HullBlueprintUUID + TemplateUUID.
- **Hold instead of Cargo**: Replaces the current player's hold in the Holds dictionary.
- **MunitionsHold instead of Hopper**: Same pattern, different name.
- **No CreateFromTemplate**: Station does not support creation from a template.
- **No write locks**: No ReaderWriterLockSlim. Single-threaded UI access only.
- **StationDataChanged event**: Fires OnStationDataChanged instead of OnShipDataChanged.

Public interface:
- Update(string uuid, StationUpdateRequest): looks up mutable entity, applies all fields, replaces Components/Hold/MunitionsHold, persists, fires event, returns ReadOnlyStation. Throws InvalidOperationException if UUID not found.
- Create(StationCreateRequest): creates new entity with generated UUID, sets OwnerUUID from current player, populates Name, adds to PlayerContext, persists, fires event, returns ReadOnlyStation.
- Delete(string uuid): looks up mutable entity, removes from PlayerContext, persists, fires event. No-op if UUID is empty or not found.
### Unsaved Changes Prompt

The form checks viewModel.IsDirty before any operation that would discard the current edit buffer:

- **Selection change** --- user selects a different station in the list view
- **New** --- user clicks the New button
- **Form close** --- user closes the form (X button or MDI close)
- **Application exit** --- MainWindow closing propagates to all MDI children

All paths use the same three-button dialog: **Save** | **Discard** | **Cancel**.

- **Save**: calls the appropriate service method (Create or Update), then proceeds with the original action.
- **Discard**: discards local changes and proceeds.
- **Cancel**: cancels the original action and keeps the current station selected.

For form close and application exit, Cancel sets e.Cancel = true to prevent the close.

### Delete Flow with Reference Protection

1. Check StationReferenceCounter for references (route stops, delivery plan stops, build items, market listings, market transactions, supply chain stages, stock plan targets, overflow rules, ship locations).
2. If CountReferences > 0: show warning message with reference count, prevent deletion.
3. If CountReferences == 0: prompt for confirmation.
4. On confirm: call StationService.Delete, clear form, refresh list.

## Data Models

### StationUpdateRequest (New)

A plain DTO carrying the original snapshot and the current state:

- Original (ReadOnlyStation): The original snapshot the edit was based on.
- Name (string): The station name.
- StationType (StationType): The station type.
- Ownership (StationOwnership): The ownership type.
- StationBlueprintUUID (string): The station blueprint UUID.
- HullCurrentHP (int): The hull current HP.
- HullMaxHP (int): The hull max HP.
- HullMaxRepairPercent (decimal): The hull max repair percent.
- Components (List of ShipComponentSlot): The component slots.
- Hold (ItemBag): The current player's hold items.
- MunitionsHold (ItemBag): The munitions hold items.
### StationCreateRequest (New)

A DTO for creating a new blank station. No Original snapshot. No UUID (the service assigns it). No OwnerUUID (the service sets it from the current player).

- Name (string): The station name (defaults to 'New Station' if empty).

### Station (Existing --- No Changes)

The existing Station model is unchanged. It has scalar fields (UUID, Name, OwnerUUID, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent), a nested List of ShipComponentSlot Components collection, a Dictionary of ItemBag Holds (keyed by player UUID), and a MunitionsHold ItemBag. No concurrency locks. The service is the only code that mutates it after migration.

### ShipComponentSlot (Existing --- No Changes)

The existing ShipComponentSlot model is unchanged. Fields: SlotType (string), SlotIndex (int), BlueprintUUID (string), CurrentHP (int), MaxHP (int), MaxRepairPercent (decimal).

### Item (Existing --- No Changes)

The existing Item model is unchanged. Fields: UUID, ItemType, BaseItemTypeID, Name, NickName, Description, Quantity, ResourcePurity, Volume, CurrentHP, MaxHP, MaxRepairPercent, Contents (optional nested ItemBag for crates).

### ItemBag (Existing --- No Changes)

The existing ItemBag model is unchanged. Dictionary-based container keyed by UUID with AddItem, Remove, Clear, Count, ContainsKey, CountByType, FindByType, FindResource methods.

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All Fields

*For any* valid Station entity, wrapping in ReadOnlyStation and calling LoadFrom SHALL produce a ViewModel whose local fields exactly match the original entity fields (Name, StationType, Ownership, StationBlueprintUUID, HullCurrentHP, HullMaxHP, HullMaxRepairPercent, and every ShipComponentSlot in Components with matching SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent, and every Item in Hold and MunitionsHold with matching UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent).

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**
### Property 2: IsDirty False Immediately After LoadFrom

*For any* valid Station entity, wrapping in ReadOnlyStation and calling LoadFrom SHALL produce a ViewModel where IsDirty returns false.

**Validates: Requirements 6.1, 6.10**

### Property 3: IsDirty Detects Name Change

*For any* valid Station entity, after LoadFrom, changing the Name field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.2**

### Property 4: IsDirty Detects StationBlueprintUUID Change

*For any* valid Station entity, after LoadFrom, changing the StationBlueprintUUID field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.5**

### Property 5: IsDirty Detects Components Change

*For any* valid Station entity with at least one component, after LoadFrom, adding a component, removing a component, or changing a component field SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.7**

### Property 6: IsDirty Detects StationType Change

*For any* valid Station entity, after LoadFrom, changing the StationType field to a different value SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.3**

### Property 7: IsDirty Detects Hold Change

*For any* valid Station entity with at least one hold item, after LoadFrom, adding or removing a hold item SHALL cause IsDirty to return true.

**Validates: Requirements 6.1, 6.8**

### Property 8: Service.Update Round-Trip

*For any* valid existing Station entity and valid StationUpdateRequest values, calling Service.Update SHALL produce a ReadOnlyStation whose scalar fields match the request and whose Components, Hold, and MunitionsHold match the request collections.

**Validates: Requirements 11.3, 11.4, 11.5, 11.6, 11.9**

### Property 9: Service.Create Round-Trip

*For any* valid StationCreateRequest values, calling Service.Create SHALL produce a ReadOnlyStation whose Name matches the request and whose UUID is non-empty.

**Validates: Requirements 12.2, 12.4, 12.8**

### Property 10: Service.Delete Removes Station

*For any* valid existing Station entity, calling Service.Delete with the station UUID SHALL cause the station to no longer be findable via PlayerContext.

**Validates: Requirements 13.1, 13.2**
### Property 11: No Direct Mutation Outside Service

AFTER migration, a static analysis grep for direct Station property sets SHALL only find matches in StationService, JSON deserialization, migration code, and the Station class itself.

**Validates: Requirements 17.1, 17.2, 17.3, 20.1, 20.2, 20.3**

## Error Handling

### Validation

- **Empty station name**: The Save handler validates that Name is not empty or whitespace before calling the service. Shows a MessageBox warning.

### Service Errors

- **Update with non-existent UUID**: StationService.Update throws InvalidOperationException. The form catches this and shows an error dialog.
- **Delete with empty/non-existent UUID**: StationService.Delete returns silently (no error). Matches the existing service pattern.
- **Null request**: Both Update and Create throw ArgumentNullException for null requests.

### Unsaved Changes Edge Cases

- **Save fails during unsaved changes prompt**: If the service throws during the Save path of the unsaved changes dialog, the form catches the exception, shows an error, and cancels the original action (same as Cancel).
- **Concurrent modification**: Not applicable --- Station has no background processing and no write locks. Single-threaded access from the UI thread only.

### Reference Protection

- **Delete with references**: StationReferenceCounter checks 10 source types (route stops, delivery plan stops, build item assembly/build locations, market listings, market transactions, supply chain stages, stock plan targets, overflow rules, ship locations). If CountReferences > 0, deletion is blocked with a warning message showing the reference count.
- **Reference counter construction**: Takes IEnumerable of routes, plans, build plans, market listings, market transactions, supply chains, stock plans, overflow rules, and ships. Null-safe.
## Testing Strategy

### Dual Testing Approach

- **Property-based tests** (FsCheck + NUnit): Verify universal properties across randomly generated Station inputs. Minimum 25 iterations per property test, following the established pattern.
- **Unit tests** (NUnit): Verify specific examples, edge cases, and error conditions.

### Property-Based Tests

**Library**: FsCheck 2.x with FsCheck.NUnit integration (already in the project).

**Generator**: A ValidStationGen() generator that produces random Station entities with:
- Random string Name, UUID, OwnerUUID, StationBlueprintUUID
- Random StationType and StationOwnership enum values
- Random int HullCurrentHP, HullMaxHP, decimal HullMaxRepairPercent
- Random number of ShipComponentSlot entries (0 to 10), each with random SlotType, SlotIndex, BlueprintUUID, CurrentHP, MaxHP, MaxRepairPercent
- Random Hold ItemBag with 0 to 5 items, each with random UUID, ItemType, BaseItemTypeID, Name, Quantity, ResourcePurity, CurrentHP, MaxHP, MaxRepairPercent
- Random MunitionsHold ItemBag with 0 to 5 items (same structure)

**Test files**:

1. OE2EmpireTracker.Tests/ViewModels/StationViewModelPropertyTests.cs
   - Property 1: LoadFrom round-trip preserves all fields
   - Property 2: IsDirty false immediately after LoadFrom
   - Property 3: IsDirty detects Name change
   - Property 4: IsDirty detects StationBlueprintUUID change
   - Property 5: IsDirty detects Components change
   - Property 6: IsDirty detects StationType change
   - Property 7: IsDirty detects Hold change

2. OE2EmpireTracker.Tests/Services/StationServicePropertyTests.cs
   - Property 8: Service.Update round-trip
   - Property 9: Service.Create round-trip
   - Property 10: Service.Delete removes station

**Configuration**: [FsCheck.NUnit.Property(MaxTest = 25)] for round-trip and IsDirty-false tests, [FsCheck.NUnit.Property(MaxTest = 50)] for the field-change tests.
### Unit Tests

**Test file**: OE2EmpireTracker.Tests/Services/StationServiceTests.cs

- Update with non-existent UUID throws InvalidOperationException
- Delete with empty UUID returns without error
- Delete with non-existent UUID returns without error
- Create assigns non-empty UUID
- Create sets OwnerUUID to current player UUID
- Update fires StationDataChanged event
- Create fires StationDataChanged event
- Delete fires StationDataChanged event
- Update replaces Components list
- Update replaces Hold and MunitionsHold
- Create populates Name from request

**Test file**: OE2EmpireTracker.Tests/ViewModels/StationViewModelTests.cs

- Reset clears all fields to defaults
- IsNew returns true after Reset
- IsNew returns false after LoadFrom
- IsDirty returns true for new station with non-empty Name
- BuildUpdateRequest copies all fields and collections
- BuildCreateRequest copies Name
- UUID and OwnerUUID are preserved from LoadFrom
- SetComponentCondition updates component HP fields
- SetHullComponent updates hull HP fields
- ClearComponents empties the local Components list
- AddHoldItem adds item to local Hold
- RemoveHoldItem removes item from local Hold
- AddMunitionsItem adds item to local MunitionsHold
- RemoveMunitionsItem removes item from local MunitionsHold
- StationType and Ownership are preserved from LoadFrom
- HullCurrentHP, HullMaxHP, HullMaxRepairPercent are preserved from LoadFrom

### Mutation Guard Test

**Test file**: OE2EmpireTracker.Tests/Services/StationMutationGuardTests.cs

A static analysis test (following the pattern of BlueprintMutationGuardTests, ColonyMutationGuardTests, DeliveryRouteMutationGuardTests, ShipTemplateMutationGuardTests) that greps the codebase for direct Station property sets and asserts they only appear in:

- StationService.cs (the sole mutator)
- Station.cs (the model class itself, default values)
- PlayerContext.cs (deserialization/migration)
- Test files (test setup)

Checks for:
- Station.Name = (property set)
- Station.UUID = (property set)
- Station.OwnerUUID = (property set)
- Station.StationType = (property set)
- Station.Ownership = (property set)
- Station.StationBlueprintUUID = (property set)
- Station.HullCurrentHP = (property set)
- Station.HullMaxHP = (property set)
- Station.HullMaxRepairPercent = (property set)
- Station.Components mutation (Components.Add, Components.Remove, Components.Clear, Components =)
- Station.Holds mutation (Holds[, Holds.Add, Holds.Remove, Holds.Clear, Holds =)
- Station.MunitionsHold mutation (MunitionsHold.AddItem, MunitionsHold.Remove, MunitionsHold.Clear, MunitionsHold =)

This validates Property 11 and Requirements 17.1, 17.2, 17.3, 20.1, 20.2, 20.3.