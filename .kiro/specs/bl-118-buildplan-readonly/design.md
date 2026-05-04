# BL-118 Design: BuildPlan Immutable Data Model with Service Layer

## Overview

BL-118 applies the immutable data model pattern to the BuildPlan form. The form stops directly mutating BuildPlan and BuildItem entities. The ViewModel becomes a disconnected edit buffer for plan Name, Description, IsActive, and the Items list. A new BuildPlanMutationService is the sole mutator of BuildPlan entities.

### Key Differences from BL-112 (DeliveryRoute)

1. **More complex entity** --- BuildPlan has 3 scalar fields (Name, Description, IsActive) plus a nested Items list where each BuildItem has 20+ fields.
2. **Item operations are buffered** --- Like DeliveryRoute stops, build item add/remove/modify accumulates in the ViewModel until Save.
3. **Execution features** --- Start Manufacturing, Queue Calc, Generate Delivery, Auto-Assign read from ViewModel local state but mutate other entities. They remain as-is.
4. **Structure allocation dialog** --- FormStructureAllocation reads from PlayerContext, returns colony/structure UUIDs. Does not mutate BuildPlan.
5. **Reference counting** --- BuildPlanReferenceCounter checks stock plans before deletion.
6. **No write locks** --- No ReaderWriterLockSlim.
7. **ReadOnly wrappers already complete** --- ReadOnlyBuildPlan and ReadOnlyBuildItem are fully implemented.

## Architecture

### Current Architecture

The form holds a direct mutable BuildPlan reference (_selectedPlan). Every control change writes directly to the entity. CmdNew_Click creates a new BuildPlan and adds to PlayerContext. CmdSave_Click sets properties and calls WriteContext(). Item add/remove mutates the entity Items list directly.

### Target Architecture

The form never touches the entity. The ViewModel is a disconnected edit buffer for Name, Description, IsActive, and Items. The service is the only code that mutates the entity. All item operations accumulate in the ViewModel local Items list until Save.

## Components and Interfaces

### ReadOnlyBuildPlan (Existing --- No Changes)

Already complete. Exposes UUID, Name, OwnerUUID, Description, DeliveryPlanUUID, IsActive as read-only. Items as IReadOnlyList of ReadOnlyBuildItem.

### ReadOnlyBuildItem (Existing --- No Changes)

Already complete. Exposes all fields as read-only: UUID, ItemType, Status, BlueprintUUID, ItemName, CommodityName, ShipTemplateUUID, Quantity, BuildLocationType, BuildLocationUUID, StructureUUID, AssemblyLocationType, AssemblyLocationUUID, ParentBuildItemUUID, Recipient, Notes, SequenceInStructure, DependsOnUUID, MiningResource, MiningSurveyUUID, RefiningResource, RefiningPurity.

### PlayerContext: FindMutableBuildPlan (New)

A new internal method following the same cache-based lookup pattern as other FindMutable methods. Marked internal so only the service can access it.

### BuildPlanViewModel (Edit Buffer)

Private fields: _original (ReadOnlyBuildPlan), _uuid, _ownerUUID, local Name, Description, IsActive, local _items (List of BuildItem).

Public properties: Name (get/set), Description (get/set), IsActive (get/set), Items (List of BuildItem), UUID, OwnerUUID, Original, IsNew, IsDirty.

Methods:
- LoadFrom(ReadOnlyBuildPlan): copies all scalar fields, deep-copies Items, stores original snapshot
- Reset(): clears all fields to defaults
- BuildUpdateRequest(): creates BuildPlanUpdateRequest from local state
- BuildCreateRequest(): creates BuildPlanCreateRequest from local state
- AddItem(BuildItem): adds to local Items list
- RemoveItem(string uuid): removes from local Items list
- FindItem(string uuid): finds item in local list by UUID
- IsDirty: compares all fields against original snapshot

### BuildPlanMutationService

Constructor takes PlayerContext dependency.

Methods:
- Update(string uuid, BuildPlanUpdateRequest): looks up mutable entity, applies fields, replaces Items, persists, fires event, returns ReadOnlyBuildPlan
- Create(BuildPlanCreateRequest): creates new entity with UUID, OwnerUUID, fields from request, adds to PlayerContext, persists, fires event, returns ReadOnlyBuildPlan
- Delete(string uuid): removes from PlayerContext, persists, fires event. No-op if UUID empty or not found.

### Unsaved Changes Prompt

Same pattern as BL-112: checks viewModel.IsDirty before selection change, New, form close, and application exit. Three-button dialog: Save, Discard, Cancel.

### Delete Flow with Reference Protection

1. Check BuildPlanReferenceCounter for stock plan references.
2. If references > 0: show warning with count, allow deletion with confirmation.
3. If no references: prompt for confirmation.
4. On confirm: call service.Delete, clear form, refresh list.

## Data Models

### BuildPlanUpdateRequest (New)

- Original (ReadOnlyBuildPlan)
- Name (string)
- Description (string)
- IsActive (bool)
- Items (List of BuildItem)

### BuildPlanCreateRequest (New)

- Name (string)
- Description (string)
- IsActive (bool)
- Items (List of BuildItem)

## Correctness Properties

### Property 1: LoadFrom Round-Trip Preserves All Fields
### Property 2: IsDirty False Immediately After LoadFrom
### Property 3: IsDirty Detects Scalar Field Change
### Property 4: IsDirty Detects Items Change
### Property 5: Service.Update Round-Trip
### Property 6: Service.Create Round-Trip
### Property 7: Service.Delete Removes Plan
### Property 8: No Direct Mutation Outside Service

## Error Handling

- Empty plan name: Save handler validates non-empty before calling service.
- Update with non-existent UUID: throws InvalidOperationException.
- Delete with empty/non-existent UUID: returns silently.
- Null request: throws ArgumentNullException.

## Testing Strategy

### Property-Based Tests

Generator: ValidBuildPlanGen() producing random BuildPlan entities with random Name, Description, IsActive, UUID, OwnerUUID, and 0-5 BuildItem entries.

Test files:
1. OE2EmpireTracker.Tests/ViewModels/BuildPlanViewModelPropertyTests.cs (Properties 1-4)
2. OE2EmpireTracker.Tests/Services/BuildPlanMutationServicePropertyTests.cs (Properties 5-7)

### Unit Tests

- OE2EmpireTracker.Tests/ViewModels/BuildPlanViewModelTests.cs
- OE2EmpireTracker.Tests/Services/BuildPlanMutationServiceTests.cs

### Mutation Guard Test

OE2EmpireTracker.Tests/Services/BuildPlanMutationGuardTests.cs --- scans for direct BuildPlan/BuildItem property sets outside allowed files.
